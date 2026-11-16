using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LithoCreate.Core;
public static class ModelExporter
{
    const string Ns="http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
    public static void Export(string path,Mesh mesh,CancellationToken token=default,IProgress<double>? progress=null)
    {
        if(mesh.IsAssemblyPreview)throw new InvalidDataException("Die Montageansicht ist kein gemeinsames Druckmodell. Körper und Zubehör einzeln oder als getrennte Drucksatzobjekte exportieren.");
        // WPF uses Y as the up axis; slicers use Z. Keep the model upright and place its base at Z=0.
        mesh=InPrintCoordinates(mesh);
        if(!mesh.Validate(token).IsValid) throw new InvalidDataException("Das Modell hat ungültige Flächen oder Kanten. Export wurde gestoppt.");
        string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            if(Path.GetExtension(path).Equals(".3mf",StringComparison.OrdinalIgnoreCase)) Write3Mf(temp,mesh,token,progress);
            else WriteStl(temp,mesh,token,progress);
            var check=Path.GetExtension(path).Equals(".3mf",StringComparison.OrdinalIgnoreCase)?Read3Mf(temp,token):ReadStl(temp,token);
            var report=check.Validate(token);
            if(!report.IsValid || report.Triangles!=mesh.Triangles.Count)throw new InvalidDataException("Die exportierte Datei hat die erneute Geometrieprüfung nicht bestanden.");
            var original=mesh.Bounds();
            if(Vector3.Distance(original.Min,report.Bounds.Min)>.01 || Vector3.Distance(original.Max,report.Bounds.Max)>.01)throw new InvalidDataException("Die exportierten Maße stimmen nicht überein.");
            token.ThrowIfCancellationRequested();File.Move(temp,path,true);
        }
        finally { if(File.Exists(temp)) File.Delete(temp); }
    }
    public static Mesh InPrintCoordinates(Mesh source)
    {
        float bottom=source.Bounds().Min.Y;var result=new Mesh{ActualResolution=source.ActualResolution,IsAssemblyPreview=source.IsAssemblyPreview};
        foreach(var v in source.Vertices)result.Vertices.Add(new(v.X,-v.Z,v.Y-bottom));
        result.Triangles.AddRange(source.Triangles);return result;
    }
    static void WriteStl(string path,Mesh m,CancellationToken token,IProgress<double>? progress)
    {
        using var file=File.Create(path);using var writer=new BinaryWriter(file);
        byte[] header=new byte[80];Encoding.ASCII.GetBytes("LithoCreate Pro | millimeters | offline").CopyTo(header,0);writer.Write(header);writer.Write((uint)m.Triangles.Count);
        for(int i=0;i<m.Triangles.Count;i++)
        {
            if((i&4095)==0) { token.ThrowIfCancellationRequested();progress?.Report(i/(double)m.Triangles.Count); }
            var t=m.Triangles[i];var a=m.Vertices[t.A];var b=m.Vertices[t.B];var c=m.Vertices[t.C];var n=Vector3.Normalize(Vector3.Cross(b-a,c-a));
            Vec(n);Vec(a);Vec(b);Vec(c);writer.Write((ushort)0);
        }
        void Vec(Vector3 v) { writer.Write(v.X);writer.Write(v.Y);writer.Write(v.Z); }
    }
    static void Write3Mf(string path,Mesh m,CancellationToken token,IProgress<double>? progress)
    {
        using var zip=ZipFile.Open(path,ZipArchiveMode.Create);
        Text("[Content_Types].xml","<?xml version=\"1.0\" encoding=\"utf-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"model\" ContentType=\"application/vnd.ms-package.3dmanufacturing-3dmodel+xml\"/></Types>");
        Text("_rels/.rels","<?xml version=\"1.0\" encoding=\"utf-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Target=\"/3D/3dmodel.model\" Id=\"rel0\" Type=\"http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel\"/></Relationships>");
        using var stream=zip.CreateEntry("3D/3dmodel.model",CompressionLevel.Fastest).Open();
        using var x=XmlWriter.Create(stream,new XmlWriterSettings{Encoding=new UTF8Encoding(false)});
        x.WriteStartDocument();x.WriteStartElement("model",Ns);x.WriteAttributeString("unit","millimeter");
        Start("resources");Start("object");x.WriteAttributeString("id","1");x.WriteAttributeString("type","model");Start("mesh");Start("vertices");
        foreach(var v in m.Vertices) { token.ThrowIfCancellationRequested();Start("vertex");Attr("x",v.X);Attr("y",v.Y);Attr("z",v.Z);x.WriteEndElement(); }
        x.WriteEndElement();Start("triangles");
        for(int i=0;i<m.Triangles.Count;i++) { if((i&4095)==0){token.ThrowIfCancellationRequested();progress?.Report(i/(double)m.Triangles.Count);}var t=m.Triangles[i];Start("triangle");Attr("v1",t.A);Attr("v2",t.B);Attr("v3",t.C);x.WriteEndElement(); }
        x.WriteEndElement();x.WriteEndElement();x.WriteEndElement();x.WriteEndElement();Start("build");Start("item");x.WriteAttributeString("objectid","1");x.WriteEndElement();x.WriteEndElement();x.WriteEndElement();x.WriteEndDocument();
        void Start(string name)=>x.WriteStartElement(name,Ns);
        void Attr(string name,float value)=>x.WriteAttributeString(name,value.ToString("R",CultureInfo.InvariantCulture));
        void Text(string name,string text) { using var w=new StreamWriter(zip.CreateEntry(name).Open(),new UTF8Encoding(false));w.Write(text); }
    }
    public static Mesh ReadStl(string path,CancellationToken token=default)
    {
        using var file=File.OpenRead(path);using var reader=new BinaryReader(file);reader.ReadBytes(80);uint count=reader.ReadUInt32();
        if(file.Length!=84L+50L*count || count>6000000)throw new InvalidDataException("Ungültige binäre STL-Datei.");
        var m=new Mesh();var indices=new Dictionary<Vector3,int>();
        for(int i=0;i<count;i++) { if((i&4095)==0)token.ThrowIfCancellationRequested();reader.ReadBytes(12);int a=V(),b=V(),c=V();m.Face(a,b,c);reader.ReadUInt16(); }
        return m;
        int V() { var v=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());if(indices.TryGetValue(v,out int index))return index;index=m.Vertices.Count;m.Vertices.Add(v);indices.Add(v,index);return index; }
    }
    public static Mesh Read3Mf(string path,CancellationToken token=default)
    {
        using var zip=ZipFile.OpenRead(path);using var stream=(zip.GetEntry("3D/3dmodel.model")??throw new InvalidDataException("3MF-Modell fehlt.")).Open();
        using var x=XmlReader.Create(stream,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,MaxCharactersInDocument=512L*1024*1024});var m=new Mesh();
        int objects=0,meshes=0,items=0;
        while(x.Read())
        {
            token.ThrowIfCancellationRequested();if(x.NodeType!=XmlNodeType.Element)continue;
            if(x.LocalName=="model"&&x.GetAttribute("unit") is string unit&&unit!="millimeter")throw new InvalidDataException("Einzelmodell muss Millimeter verwenden.");
            if(x.LocalName=="object"&&++objects>1||x.LocalName=="mesh"&&++meshes>1||x.LocalName=="item"&&++items>1||x.LocalName=="components"||x.LocalName=="item"&&x.GetAttribute("transform")!=null)throw new InvalidDataException("Dieser Leser unterstützt nur einzelne untransformierte Mesh-Objekte; für Drucksätze den Drucksatzleser verwenden.");
            if(x.LocalName=="vertex")m.Vertices.Add(new(F("x"),F("y"),F("z")));
            if(x.LocalName=="triangle")m.Face(I("v1"),I("v2"),I("v3"));
        }
        return m;
        float F(string name)=>float.Parse(x.GetAttribute(name)??"",CultureInfo.InvariantCulture);
        int I(string name)=>int.Parse(x.GetAttribute(name)??"",CultureInfo.InvariantCulture);
    }
}
