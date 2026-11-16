using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using System.Xml;

namespace LithoCreate.Core;
public sealed record PrintSetSource(string Name,Mesh Mesh);
/// <summary>Mesh coordinates are local printer XYZ, with a minimum of zero on all axes.</summary>
public sealed record PrintSetPart(string Name,Mesh Mesh,Vector2 Position,MeshReport Report);
public sealed record PrintSet(double BedX,double BedY,double BuildZ,double Margin,double Gap,IReadOnlyList<PrintSetPart> Parts)
{
    public PrintArrangement Arrangement {get;init;}
    public static PrintSet Create(IReadOnlyList<PrintSetSource> sources,PrinterProfile printer,CancellationToken token=default,PrintSetOptions? options=null)
    {
        printer.Validate();token.ThrowIfCancellationRequested();
        options=(options??new()).Clone();options.Validate();
        if(sources.Count!=2||sources.Any(s=>s is null||string.IsNullOrWhiteSpace(s.Name)||s.Name.Length>256)||sources.Select(s=>s.Name).Distinct().Count()!=2)
            throw new InvalidDataException("Ein Drucksatz benötigt zwei unterschiedlich benannte Einzelteile.");
        double margin=options.Margin,gap=options.Gap;var normalized=new List<(string Name,Mesh Mesh,MeshReport Report)>();
        foreach(var source in sources)
        {
            if(source.Mesh is null||source.Mesh.IsAssemblyPreview)throw new InvalidDataException("Montageansichten können nicht als Druckteil verwendet werden.");
            var report=source.Mesh.Validate(token);if(!report.IsValid||!Finite(report.Bounds.Min)||!Finite(report.Bounds.Max))throw new InvalidDataException("Ein Druckteil hat die Geometrieprüfung nicht bestanden: "+source.Name);
            var bounds=report.Bounds;var mesh=new Mesh{ActualResolution=source.Mesh.ActualResolution};
            for(int i=0;i<source.Mesh.Vertices.Count;i++){if((i&4095)==0)token.ThrowIfCancellationRequested();var v=source.Mesh.Vertices[i];mesh.Vertices.Add(new(v.X-bounds.Min.X,bounds.Max.Z-v.Z,v.Y-bounds.Min.Y));}
            mesh.Triangles.AddRange(source.Mesh.Triangles);
            var size=new Vector3(report.Size.X,report.Size.Z,report.Size.Y);
            if(size.Z>printer.BuildZ+.0001)throw new InvalidDataException($"{source.Name}: Bauhöhe {size.Z:N2} mm überschreitet das Druckerprofil. Einzelteil anders ausrichten.");
            normalized.Add((source.Name,mesh,report with{Bounds=(Vector3.Zero,size)}));
        }
        var a=normalized[0].Report.Size;var b=normalized[1].Report.Size;
        if(options.Arrangement==PrintArrangement.Manual)
        {
            var manual=new PrintSet(printer.BedX,printer.BedY,printer.BuildZ,margin,gap,[new(normalized[0].Name,normalized[0].Mesh,new((float)options.BodyX,(float)options.BodyY),normalized[0].Report),new(normalized[1].Name,normalized[1].Mesh,new((float)options.AccessoryX,(float)options.AccessoryY),normalized[1].Report)]){Arrangement=PrintArrangement.Manual};
            manual.ValidatePlacement();return manual;
        }
        var layouts=new[]{(Row:true,W:(double)a.X+b.X+gap,D:(double)Math.Max(a.Y,b.Y)),(Row:false,W:(double)Math.Max(a.X,b.X),D:(double)a.Y+b.Y+gap)};
        var fits=layouts.Where(o=>(options.Arrangement==PrintArrangement.Automatic||o.Row==(options.Arrangement==PrintArrangement.Row))&&o.W+2*margin<=printer.BedX+.0001&&o.D+2*margin<=printer.BedY+.0001).OrderBy(o=>o.W*o.D).ToArray();
        if(fits.Length==0)throw new InvalidDataException($"Beide Teile passen mit {margin:N2} mm Rand und {gap:N2} mm Teileabstand in der gewählten Anordnung nicht gemeinsam auf dieses Druckbett. Anordnung, Einzelteil-Drucklagen, Maße oder Druckerprofil anpassen; alternativ einzeln exportieren.");
        var fit=fits[0];double x=(printer.BedX-fit.W)/2,y=(printer.BedY-fit.D)/2;
        Vector2 pa=fit.Row?new((float)x,(float)(y+(fit.D-a.Y)/2)):new((float)(x+(fit.W-a.X)/2),(float)y);
        Vector2 pb=fit.Row?new((float)(x+a.X+gap),(float)(y+(fit.D-b.Y)/2)):new((float)(x+(fit.W-b.X)/2),(float)(y+a.Y+gap));
        return new(printer.BedX,printer.BedY,printer.BuildZ,margin,gap,[new(normalized[0].Name,normalized[0].Mesh,pa,normalized[0].Report),new(normalized[1].Name,normalized[1].Mesh,pb,normalized[1].Report)]){Arrangement=fit.Row?PrintArrangement.Row:PrintArrangement.Column};
    }
    static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
    public PrintSet WithPositions(Vector2 body,Vector2 accessory)
    {
        if(Parts is null||Parts.Count!=2||Parts.Any(p=>p is null))throw new InvalidDataException("Ein Drucksatz benötigt zwei Einzelteile.");
        var moved=this with{Parts=[Parts[0] with{Position=body},Parts[1] with{Position=accessory}],Arrangement=PrintArrangement.Manual};
        moved.ValidatePlacement();return moved;
    }
    public PrintSet WithPosition(int index,Vector2 position)
    {
        if(index is < 0 or > 1)throw new InvalidDataException("Ein Druckteil auswählen.");
        if(Parts is null||Parts.Count!=2||Parts.Any(p=>p is null))throw new InvalidDataException("Ein Drucksatz benötigt zwei Einzelteile.");
        return WithPositions(index==0?position:Parts[0].Position,index==1?position:Parts[1].Position);
    }
    /// <summary>Fast placement check for already inspected meshes; full export validation still checks every face.</summary>
    public void ValidatePlacement()
    {
        new PrintSetOptions{Margin=Margin,Gap=Gap,Arrangement=Arrangement}.Validate();
        if(!new[]{BedX,BedY,BuildZ}.All(v=>double.IsFinite(v)&&v>0)||Parts is null||Parts.Count!=2||Parts.Any(p=>p is null||p.Report is null||string.IsNullOrWhiteSpace(p.Name)||p.Name.Length>256)||Parts.Select(p=>p.Name).Distinct().Count()!=2)throw new InvalidDataException("Ungültiger Drucksatz oder Druckraum.");
        foreach(var p in Parts)
        {
            var size=p.Report.Size;
            if(!p.Report.IsValid||!Finite(size)||size.X<=0||size.Y<=0||size.Z<=0||!float.IsFinite(p.Position.X)||!float.IsFinite(p.Position.Y)||p.Position.X<Margin-.001||p.Position.Y<Margin-.001||p.Position.X+size.X>BedX-Margin+.001||p.Position.Y+size.Y>BedY-Margin+.001||size.Z>BuildZ+.001)throw new InvalidDataException($"{p.Name}: Position verletzt Bettrand oder Druckvolumen. X/Y beziehen sich auf die linke untere Ecke des Hüllrechtecks.");
        }
        var a=Parts[0];var b=Parts[1];var sa=a.Report.Size;var sb=b.Report.Size;
        if(!(a.Position.X+sa.X+Gap<=b.Position.X+.001||b.Position.X+sb.X+Gap<=a.Position.X+.001||a.Position.Y+sa.Y+Gap<=b.Position.Y+.001||b.Position.Y+sb.Y+Gap<=a.Position.Y+.001))throw new InvalidDataException("Die Hüllrechtecke überschneiden sich oder unterschreiten den Teileabstand.");
    }
    public void Validate(CancellationToken token=default)
    {
        ValidatePlacement();
        new PrintSetOptions{Margin=Margin,Gap=Gap,Arrangement=Arrangement}.Validate();
        if(!new[]{BedX,BedY,BuildZ,Margin,Gap}.All(double.IsFinite)||BedX<=0||BedY<=0||BuildZ<=0||Margin<0||Gap<0||Parts is null||Parts.Count!=2||Parts.Any(p=>p is null||string.IsNullOrWhiteSpace(p.Name)||p.Name.Length>256)||Parts.Select(p=>p.Name).Distinct().Count()!=2)
            throw new InvalidDataException("Ungültiger Drucksatz oder Druckraum.");
        foreach(var p in Parts)
        {
            token.ThrowIfCancellationRequested();if(p.Mesh is null||p.Mesh.IsAssemblyPreview)throw new InvalidDataException("Eine Montage ist kein Einzelteil.");var report=p.Mesh.Validate(token);
            if(!report.IsValid||!Finite(report.Bounds.Min)||!Finite(report.Bounds.Max)||Vector3.Distance(report.Bounds.Min,Vector3.Zero)>.001||!float.IsFinite(p.Position.X)||!float.IsFinite(p.Position.Y)||p.Position.X<Margin-.001||p.Position.Y<Margin-.001||p.Position.X+report.Size.X>BedX-Margin+.001||p.Position.Y+report.Size.Y>BedY-Margin+.001||report.Size.Z>BuildZ+.001)
                throw new InvalidDataException("Drucksatzgeometrie oder Platzierung ist ungültig: "+p.Name);
            if(report.Triangles!=p.Report.Triangles||Vector3.Distance(report.Size,p.Report.Size)>.001||Math.Abs(report.Volume-p.Report.Volume)>Math.Max(.01,report.Volume*.00001))throw new InvalidDataException("Ein vorbereitetes Druckteil wurde nachträglich verändert.");
        }
        var a=Parts[0];var b=Parts[1];var sa=a.Report.Size;var sb=b.Report.Size;
        if(!(a.Position.X+sa.X+Gap<=b.Position.X+.001||b.Position.X+sb.X+Gap<=a.Position.X+.001||a.Position.Y+sa.Y+Gap<=b.Position.Y+.001||b.Position.Y+sb.Y+Gap<=a.Position.Y+.001))throw new InvalidDataException("Die Druckteile überschneiden sich oder unterschreiten den Teileabstand.");
    }
}
public static class PrintSetExporter
{
    const string Ns="http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
    public static void Export(string path,PrintSet set,CancellationToken token=default,IProgress<double>? progress=null)
    {
        if(!Path.GetExtension(path).Equals(".3mf",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Drucksätze werden als 3MF mit getrennten Objekten gespeichert.");
        set.Validate(token);string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            Write(temp,set,token,progress);var read=Read(temp,token);
            if(read.Count!=set.Parts.Count)throw new InvalidDataException("Der gespeicherte Drucksatz enthält nicht alle Teile.");
            for(int i=0;i<read.Count;i++)
            {
                var a=set.Parts[i];var b=read[i];var check=b.Mesh.Validate(token);
                if(!check.IsValid||a.Name!=b.Name||Vector2.Distance(a.Position,b.Position)>.001||check.Triangles!=a.Report.Triangles||Vector3.Distance(check.Bounds.Min,a.Report.Bounds.Min)>.001||Vector3.Distance(check.Bounds.Max,a.Report.Bounds.Max)>.001||Math.Abs(check.Volume-a.Report.Volume)>Math.Max(.01,a.Report.Volume*.00001))throw new InvalidDataException("Der gespeicherte Drucksatz hat die erneute Prüfung nicht bestanden.");
            }
            token.ThrowIfCancellationRequested();File.Move(temp,path,true);progress?.Report(1);
        }
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    static void Write(string path,PrintSet set,CancellationToken token,IProgress<double>? progress)
    {
        using var zip=ZipFile.Open(path,ZipArchiveMode.Create);
        Text("[Content_Types].xml","<?xml version=\"1.0\" encoding=\"utf-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"model\" ContentType=\"application/vnd.ms-package.3dmanufacturing-3dmodel+xml\"/></Types>");
        Text("_rels/.rels","<?xml version=\"1.0\" encoding=\"utf-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Target=\"/3D/3dmodel.model\" Id=\"rel0\" Type=\"http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel\"/></Relationships>");
        using var stream=zip.CreateEntry("3D/3dmodel.model",CompressionLevel.Fastest).Open();using var x=XmlWriter.Create(stream,new XmlWriterSettings{Encoding=new UTF8Encoding(false)});
        x.WriteStartDocument();Start("model");x.WriteAttributeString("unit","millimeter");Start("resources");long total=set.Parts.Sum(p=>(long)p.Mesh.Vertices.Count+p.Mesh.Triangles.Count),done=0;
        for(int i=0;i<set.Parts.Count;i++)
        {
            var part=set.Parts[i];Start("object");x.WriteAttributeString("id",(i+1).ToString(CultureInfo.InvariantCulture));x.WriteAttributeString("type","model");x.WriteAttributeString("name",part.Name);Start("mesh");Start("vertices");
            foreach(var v in part.Mesh.Vertices){Check();Start("vertex");Attr("x",v.X);Attr("y",v.Y);Attr("z",v.Z);x.WriteEndElement();}x.WriteEndElement();Start("triangles");
            foreach(var t in part.Mesh.Triangles){Check();Start("triangle");x.WriteAttributeString("v1",t.A.ToString(CultureInfo.InvariantCulture));x.WriteAttributeString("v2",t.B.ToString(CultureInfo.InvariantCulture));x.WriteAttributeString("v3",t.C.ToString(CultureInfo.InvariantCulture));x.WriteEndElement();}
            x.WriteEndElement();x.WriteEndElement();x.WriteEndElement();
        }
        x.WriteEndElement();Start("build");for(int i=0;i<set.Parts.Count;i++){var p=set.Parts[i];Start("item");x.WriteAttributeString("objectid",(i+1).ToString(CultureInfo.InvariantCulture));x.WriteAttributeString("transform","1 0 0 0 1 0 0 0 1 "+p.Position.X.ToString("R",CultureInfo.InvariantCulture)+" "+p.Position.Y.ToString("R",CultureInfo.InvariantCulture)+" 0");x.WriteEndElement();}x.WriteEndElement();x.WriteEndElement();x.WriteEndDocument();
        void Start(string name)=>x.WriteStartElement(name,Ns);
        void Attr(string name,float value)=>x.WriteAttributeString(name,value.ToString("R",CultureInfo.InvariantCulture));
        void Check(){if((done++&4095)==0){token.ThrowIfCancellationRequested();progress?.Report(.8*done/Math.Max(1,total));}}
        void Text(string name,string text){using var writer=new StreamWriter(zip.CreateEntry(name).Open(),new UTF8Encoding(false));writer.Write(text);}
    }
    /// <summary>Reads this application's two-mesh, translation-only 3MF subset, not arbitrary 3MF projects.</summary>
    public static IReadOnlyList<PrintSetPart> Read(string path,CancellationToken token=default)
    {
        using var zip=ZipFile.OpenRead(path);var entry=zip.GetEntry("3D/3dmodel.model")??throw new InvalidDataException("3MF-Modell fehlt.");if(entry.Length>512L*1024*1024)throw new InvalidDataException("3MF-Modell ist zu groß.");
        using var stream=entry.Open();using var x=XmlReader.Create(stream,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,MaxCharactersInDocument=512L*1024*1024});
        var objects=new Dictionary<int,(string Name,Mesh Mesh)>();var items=new List<(int Id,Vector2 Position)>();Mesh? current=null;int meshes=0,objectMeshes=0,modelCount=0,builds=0;long vertices=0,triangles=0;
        while(x.Read())
        {
            token.ThrowIfCancellationRequested();if(x.NodeType==XmlNodeType.EndElement&&x.LocalName=="object"){if(current==null||objectMeshes!=1)throw new InvalidDataException("Druckteil enthält nicht genau ein Netz.");current=null;continue;}if(x.NodeType!=XmlNodeType.Element)continue;
            if(x.NamespaceURI!=Ns)throw new InvalidDataException("Unbekannter 3MF-Namensraum.");
            switch(x.LocalName)
            {
                case "model": if(++modelCount!=1||x.GetAttribute("unit")!="millimeter")throw new InvalidDataException("Drucksatz muss Millimeter verwenden.");break;
                case "object": int id=Int("id");string name=x.GetAttribute("name")??"";if(objects.Count>=2||id<=0||string.IsNullOrWhiteSpace(name)||name.Length>256||x.GetAttribute("type")!="model"||current!=null||objects.ContainsKey(id))throw new InvalidDataException("Ungültiges Drucksatzobjekt.");current=new();objectMeshes=0;objects.Add(id,(name,current));break;
                case "mesh": if(current==null||++objectMeshes!=1||++meshes>2)throw new InvalidDataException("Ungültiges Drucksatznetz.");break;
                case "components": throw new InvalidDataException("Drucksatz-Komponenten werden nicht unterstützt.");
                case "vertex": if(current==null||++vertices>3000000)throw new InvalidDataException("Ungültige Drucksatz-Vertices.");current.Vertices.Add(new(Float("x"),Float("y"),Float("z")));break;
                case "triangle": if(current==null||++triangles>6000000)throw new InvalidDataException("Ungültige Drucksatz-Dreiecke.");current.Face(Int("v1"),Int("v2"),Int("v3"));break;
                case "build": if(++builds!=1)throw new InvalidDataException("Ungültiger Drucksatzaufbau.");break;
                case "item":
                    if(items.Count>=2||builds!=1)throw new InvalidDataException("Ungültiger Drucksatzaufbau.");int target=Int("objectid");var matrix=(x.GetAttribute("transform")??"").Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries).Select(v=>float.Parse(v,CultureInfo.InvariantCulture)).ToArray();
                    float[] identity=[1,0,0,0,1,0,0,0,1];if(matrix.Length!=12||matrix.Any(v=>!float.IsFinite(v))||!matrix.Take(9).SequenceEqual(identity)||matrix[9]<0||matrix[10]<0||matrix[11]!=0||items.Any(i=>i.Id==target))throw new InvalidDataException("Drucksatz unterstützt ausschließlich getrennte Objekte mit XY-Verschiebungen auf Z=0.");items.Add((target,new(matrix[9],matrix[10])));break;
            }
        }
        if(modelCount!=1||objects.Count!=2||meshes!=2||items.Count!=2||builds!=1||items.Any(i=>!objects.ContainsKey(i.Id))||objects.Values.Select(p=>p.Name).Distinct().Count()!=2)throw new InvalidDataException("Drucksatz muss genau zwei vollständige Einzelteile enthalten.");
        return items.Select(i=>{var p=objects[i.Id];return new PrintSetPart(p.Name,p.Mesh,i.Position,p.Mesh.Validate(token));}).ToArray();
        int Int(string name)=>int.Parse(x.GetAttribute(name)??"",CultureInfo.InvariantCulture);
        float Float(string name){float v=float.Parse(x.GetAttribute(name)??"",CultureInfo.InvariantCulture);if(!float.IsFinite(v))throw new InvalidDataException("Ungültige 3MF-Koordinate.");return v;}
    }
}
