using System.Globalization;
using System.Numerics;
using System.Text;

namespace LithoCreate.Core;
public sealed record ImportedModel(Mesh Mesh,MeshReport Report,bool OrientationCorrected,string Format);
public static class StlImport
{
    public static ImportedModel Read(string path,int maxTriangles=300000,CancellationToken token=default)
    {
        if(maxTriangles is < 1 or > 1000000)throw new ArgumentOutOfRangeException(nameof(maxTriangles));
        using var file=File.OpenRead(path);if(file.Length is < 15 or > 64*1024*1024)throw new InvalidDataException("STL: 15 Byte bis 64 MiB zulässig.");
        bool binary=false;if(file.Length>=84){file.Position=80;using var countReader=new BinaryReader(file,Encoding.UTF8,true);uint count=countReader.ReadUInt32();binary=file.Length==84L+50L*count;}
        file.Position=0;var mesh=new Mesh();var indices=new Dictionary<Vector3,int>();
        if(binary)
        {
            using var reader=new BinaryReader(file,Encoding.UTF8,true);reader.ReadBytes(80);uint count=reader.ReadUInt32();if(count==0||count>maxTriangles)throw new InvalidDataException($"STL: höchstens {maxTriangles:N0} Dreiecke zulässig.");
            for(int i=0;i<count;i++){token.ThrowIfCancellationRequested();reader.ReadBytes(12);mesh.Face(V(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()),V(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()),V(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()));reader.ReadUInt16();}
        }
        else
        {
            using var reader=new StreamReader(file,new UTF8Encoding(false,true),true,4096,true);string line=Line();if(!line.StartsWith("solid",StringComparison.OrdinalIgnoreCase)||(line.Length>5&&!char.IsWhiteSpace(line[5])))throw new InvalidDataException("Keine unterstützte ASCII- oder binäre STL-Datei.");
            while(true)
            {
                token.ThrowIfCancellationRequested();line=Line();if(line.Equals("endsolid",StringComparison.OrdinalIgnoreCase)||line.StartsWith("endsolid ",StringComparison.OrdinalIgnoreCase))break;
                var normal=Words(line);if(normal.Length!=5||normal[0]!="facet"||normal[1]!="normal")throw new InvalidDataException("ASCII-STL: Facettenbeginn fehlt.");for(int i=2;i<5;i++)Number(normal[i]);
                Expect("outer loop");int a=Vertex(),b=Vertex(),c=Vertex();Expect("endloop");Expect("endfacet");mesh.Face(a,b,c);if(mesh.Triangles.Count>maxTriangles)throw new InvalidDataException($"STL: höchstens {maxTriangles:N0} Dreiecke zulässig.");
            }
            while(!reader.EndOfStream)if(!string.IsNullOrWhiteSpace(reader.ReadLine()))throw new InvalidDataException("ASCII-STL enthält weitere Modelle oder unerwartete Daten.");
            string Line(){string? s;do{s=reader.ReadLine();if(s is null)throw new InvalidDataException("ASCII-STL ist unvollständig.");if(s.Length>4096||s.Contains('\0'))throw new InvalidDataException("Ungültige ASCII-STL-Zeile.");}while(string.IsNullOrWhiteSpace(s));return s.Trim();}
            void Expect(string expected){if(Line()!=expected)throw new InvalidDataException("ASCII-STL: "+expected+" erwartet.");}
            int Vertex(){var values=Words(Line());if(values.Length!=4||values[0]!="vertex")throw new InvalidDataException("ASCII-STL: Vertex erwartet.");return V(Number(values[1]),Number(values[2]),Number(values[3]));}
        }
        if(mesh.Triangles.Count==0)throw new InvalidDataException("STL enthält keine Dreiecke.");
        bool corrected=false;var initial=mesh.Validate(token);
        if(initial.OpenOrNonManifoldEdges==0&&initial.InconsistentEdges==0&&initial.DegenerateFaces==0)
        {
            var volumes=ComponentVolumes(mesh,token);
            if(volumes.Any(v=>!double.IsFinite(v)||Math.Abs(v)<1e-10)||volumes.Any(v=>v>0)&&volumes.Any(v=>v<0))throw new InvalidDataException("STL-Teilkörper haben gemischte oder unbestimmte Wicklungen. Im CAD/Slicer einzeln korrigieren.");
            if(volumes.All(v=>v<0)){for(int i=0;i<mesh.Triangles.Count;i++){var t=mesh.Triangles[i];mesh.Triangles[i]=new(t.A,t.C,t.B);}corrected=true;}
        }
        // Input uses printer Z-up. The editor uses Y-up; this proper rotation preserves winding and units.
        foreach(var p in mesh.Vertices)if(!float.IsFinite(p.X)||!float.IsFinite(p.Y)||!float.IsFinite(p.Z))throw new InvalidDataException("STL enthält nichtendliche Koordinaten.");
        for(int i=0;i<mesh.Vertices.Count;i++){token.ThrowIfCancellationRequested();var v=mesh.Vertices[i];mesh.Vertices[i]=new(v.X,v.Z,-v.Y);}
        return new(mesh,mesh.Validate(token),corrected,binary?"Binär-STL":"ASCII-STL");
        int V(float x,float y,float z){if(!new[]{x,y,z}.All(v=>float.IsFinite(v)&&Math.Abs(v)<=1000000))throw new InvalidDataException("STL-Koordinaten müssen endlich und innerhalb ±1.000.000 mm liegen.");var p=new Vector3(x==0?0:x,y==0?0:y,z==0?0:z);if(indices.TryGetValue(p,out int id))return id;id=mesh.Vertices.Count;mesh.Vertices.Add(p);indices.Add(p,id);return id;}
        static string[] Words(string value)=>value.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);
        static float Number(string value){if(!float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out float n)||!float.IsFinite(n))throw new InvalidDataException("Ungültige ASCII-STL-Zahl.");return n;}
    }
    static double[] ComponentVolumes(Mesh mesh,CancellationToken token)
    {
        var parent=Enumerable.Range(0,mesh.Triangles.Count).ToArray();var edges=new Dictionary<ulong,int>();
        for(int i=0;i<mesh.Triangles.Count;i++){token.ThrowIfCancellationRequested();var t=mesh.Triangles[i];Join(t.A,t.B,i);Join(t.B,t.C,i);Join(t.C,t.A,i);}
        var origins=new Dictionary<int,Vector3>();var volumes=new Dictionary<int,double>();var vertexComponents=new Dictionary<int,int>();
        for(int i=0;i<mesh.Triangles.Count;i++)
        {
            token.ThrowIfCancellationRequested();int root=Root(i);var t=mesh.Triangles[i];foreach(int index in new[]{t.A,t.B,t.C}){if(vertexComponents.TryGetValue(index,out int other)&&other!=root)throw new InvalidDataException("STL-Teilkörper berühren sich nur an einem Punkt. Nichtmanifolden Vertex im CAD korrigieren.");vertexComponents[index]=root;}
            if(!origins.TryGetValue(root,out var origin))origins[root]=origin=mesh.Vertices[t.A];var a=mesh.Vertices[t.A]-origin;var b=mesh.Vertices[t.B]-origin;var c=mesh.Vertices[t.C]-origin;
            double volume=((double)a.X*((double)b.Y*c.Z-(double)b.Z*c.Y)+(double)a.Y*((double)b.Z*c.X-(double)b.X*c.Z)+(double)a.Z*((double)b.X*c.Y-(double)b.Y*c.X))/6;volumes[root]=volumes.GetValueOrDefault(root)+volume;
        }
        return volumes.Values.ToArray();
        int Root(int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
        void Join(int a,int b,int face){ulong key=((ulong)(uint)Math.Min(a,b)<<32)|(uint)Math.Max(a,b);if(edges.Remove(key,out int previous))parent[Root(face)]=Root(previous);else edges.Add(key,face);}
    }
}
