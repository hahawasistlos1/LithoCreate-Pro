using System.Numerics;

namespace LithoCreate.Core;

public sealed record SectionPath(IReadOnlyList<Vector2> Points,bool Closed);
public sealed record CrossSection(double Height,IReadOnlyList<SectionPath> Paths,double Area,double Perimeter,int IrregularVertices,Vector2 Min,Vector2 Max)
{
    public int ClosedContours=>Paths.Count(p=>p.Closed);
    public int OpenContours=>Paths.Count(p=>!p.Closed);
    public bool IsRegular=>IrregularVertices==0&&OpenContours==0;
}

/// <summary>Horizontal geometric sections, in printer XY coordinates. No extrusion or toolpaths.</summary>
public sealed class CrossSectionIndex
{
    readonly Mesh mesh;
    readonly float[] lower,upper;
    readonly int[][]? buckets;
    readonly double minY,maxY,tolerance;
    readonly Vector2 min,max;
    public int BucketReferences {get;}
    public double Height=>maxY-minY;
    public double Bottom=>minY;
    public CrossSectionIndex(Mesh mesh,CancellationToken token=default)
    {
        if(mesh.Triangles.Count==0||mesh.Vertices.Count==0)throw new InvalidDataException("Ein leeres Modell hat keine Druckschichten.");
        this.mesh=mesh;var bounds=mesh.Bounds();minY=bounds.Min.Y;maxY=bounds.Max.Y;
        if(!double.IsFinite(minY)||!double.IsFinite(maxY)||maxY<=minY)throw new InvalidDataException("Das Modell hat keine gültige Bauhöhe.");
        min=new(bounds.Min.X,-bounds.Max.Z);max=new(bounds.Max.X,-bounds.Min.Z);
        tolerance=Math.Max(.00001,Math.Max(maxY-minY,Math.Max(max.X-min.X,max.Y-min.Y))*.0000001);
        lower=new float[mesh.Triangles.Count];upper=new float[mesh.Triangles.Count];
        const int binCount=256;long references=0;
        for(int i=0;i<mesh.Triangles.Count;i++)
        {
            if((i&4095)==0)token.ThrowIfCancellationRequested();var t=mesh.Triangles[i];
            lower[i]=Math.Min(mesh.Vertices[t.A].Y,Math.Min(mesh.Vertices[t.B].Y,mesh.Vertices[t.C].Y));
            upper[i]=Math.Max(mesh.Vertices[t.A].Y,Math.Max(mesh.Vertices[t.B].Y,mesh.Vertices[t.C].Y));
            references+=Bin(upper[i]+tolerance)-Bin(lower[i]-tolerance)+1;
        }
        // Bound the acceleration structure; large/steep triangles fall back to the same
        // cancellable scan rather than multiplying the geometry memory budget.
        if(references<=2_000_000)
        {
            var lists=Enumerable.Range(0,binCount).Select(_=>new List<int>()).ToArray();
            for(int i=0;i<lower.Length;i++){if((i&4095)==0)token.ThrowIfCancellationRequested();for(int b=Bin(lower[i]-tolerance);b<=Bin(upper[i]+tolerance);b++)lists[b].Add(i);}
            buckets=lists.Select(l=>l.ToArray()).ToArray();BucketReferences=(int)references;
        }
        int Bin(double y)=>Math.Clamp((int)((y-minY)/(maxY-minY)*binCount),0,binCount-1);
    }
    readonly record struct Edge(int A,int B)
    {
        public static Edge Of(int a,int b)=>a<b?new(a,b):new(b,a);
    }
    public CrossSection Slice(double y,CancellationToken token=default)
    {
        if(!double.IsFinite(y))throw new InvalidDataException("Die Schnitthöhe muss eine endliche Zahl sein.");
        token.ThrowIfCancellationRequested();
        if(y<minY-tolerance||y>maxY+tolerance)return new(y-minY,[],0,0,0,min,max);
        var points=new List<Vector2>();var spatial=new Dictionary<(long,long),List<int>>();
        var edges=new Dictionary<Edge,(int A,int B)>();var coplanar=new Dictionary<Edge,(int Count,int A,int B)>();
        IEnumerable<int> candidates=buckets==null?Enumerable.Range(0,mesh.Triangles.Count):buckets[Math.Clamp((int)((y-minY)/(maxY-minY)*buckets.Length),0,buckets.Length-1)];
        Span<Vector2> cuts=stackalloc Vector2[3];int checkedTriangles=0;
        foreach(int i in candidates)
        {
            if((checkedTriangles++&2047)==0)token.ThrowIfCancellationRequested();if(y<lower[i]-tolerance||y>upper[i]+tolerance)continue;
            var t=mesh.Triangles[i];var a=mesh.Vertices[t.A];var b=mesh.Vertices[t.B];var c=mesh.Vertices[t.C];
            var normal=Vector3.Cross(b-a,c-a);
            if(Math.Abs(a.Y-y)<=tolerance&&Math.Abs(b.Y-y)<=tolerance&&Math.Abs(c.Y-y)<=tolerance)
            {
                var pa=Project(a);var pb=Project(b);var pc=Project(c);
                if(normal.Y<0)(pb,pc)=(pc,pb);
                Coplanar(pa,pb);Coplanar(pb,pc);Coplanar(pc,pa);continue;
            }
            int count=0;CutEdge(a,b,y,tolerance,cuts,ref count);CutEdge(b,c,y,tolerance,cuts,ref count);CutEdge(c,a,y,tolerance,cuts,ref count);
            if(count!=2)continue;
            var start=cuts[0];var end=cuts[1];
            // Consistent outward normals put material on the left of the 2D boundary.
            if((end.X-start.X)*normal.Z+(end.Y-start.Y)*normal.X<0)(start,end)=(end,start);
            int ia=Point(start),ib=Point(end);if(ia!=ib)edges.TryAdd(Edge.Of(ia,ib),(ia,ib));
        }
        foreach(var entry in coplanar)if(entry.Value.Count==1)
        {
            if(edges.TryGetValue(entry.Key,out var existing))
            {
                // An oppositely oriented edge separates a coplanar filled face from an
                // adjoining filled wall. It is internal to the material section.
                if(existing.A!=entry.Value.A)edges.Remove(entry.Key);
            }
            else edges.Add(entry.Key,(entry.Value.A,entry.Value.B));
        }
        var segments=edges.Values.ToArray();var adjacency=new List<int>[points.Count];for(int i=0;i<adjacency.Length;i++)adjacency[i]=[];
        double signedArea=0,perimeter=0;
        for(int i=0;i<segments.Length;i++)
        {
            var edge=segments[i];adjacency[edge.A].Add(i);adjacency[edge.B].Add(i);var a=points[edge.A];var b=points[edge.B];
            signedArea+=((double)a.X*b.Y-(double)b.X*a.Y)/2;perimeter+=Vector2.Distance(a,b);
        }
        int irregular=adjacency.Count(a=>a.Count!=0&&a.Count!=2);var used=new bool[segments.Length];var paths=new List<SectionPath>();
        for(int first=0;first<segments.Length;first++)
        {
            if((first&1023)==0)token.ThrowIfCancellationRequested();if(used[first])continue;
            var edge=segments[first];int start=adjacency[edge.A].Count==1?edge.A:adjacency[edge.B].Count==1?edge.B:edge.A,current=start,nextEdge=first;
            var path=new List<Vector2>{points[start]};bool closed=false;
            while(nextEdge>=0)
            {
                used[nextEdge]=true;var e=segments[nextEdge];current=e.A==current?e.B:e.A;
                if(current==start){closed=true;break;}path.Add(points[current]);
                nextEdge=-1;foreach(int candidate in adjacency[current])if(!used[candidate]){nextEdge=candidate;break;}
            }
            paths.Add(new(path,closed));
        }
        return new(y-minY,paths,Math.Max(0,signedArea),perimeter,irregular,min,max);

        int Point(Vector2 p)
        {
            long x=(long)Math.Floor(p.X/tolerance),z=(long)Math.Floor(p.Y/tolerance);
            for(long dx=-1;dx<=1;dx++)for(long dz=-1;dz<=1;dz++)if(spatial.TryGetValue((x+dx,z+dz),out var nearby))foreach(int id in nearby)if(Vector2.DistanceSquared(points[id],p)<=tolerance*tolerance)return id;
            int index=points.Count;points.Add(p);if(!spatial.TryGetValue((x,z),out var cell)){cell=[];spatial[(x,z)]=cell;}cell.Add(index);return index;
        }
        void Coplanar(Vector2 a,Vector2 b)
        {
            int ia=Point(a),ib=Point(b);if(ia==ib)return;var key=Edge.Of(ia,ib);coplanar.TryGetValue(key,out var value);coplanar[key]=(value.Count+1,ia,ib);
        }
    }
    static Vector2 Project(Vector3 p)=>new(p.X,-p.Z);
    static void CutEdge(Vector3 a,Vector3 b,double y,double epsilon,Span<Vector2> cuts,ref int count)
    {
        double da=a.Y-y,db=b.Y-y;bool onA=Math.Abs(da)<=epsilon,onB=Math.Abs(db)<=epsilon;
        if(onA)AddCut(Project(a),epsilon,cuts,ref count);if(onB)AddCut(Project(b),epsilon,cuts,ref count);
        if(!onA&&!onB&&(da<0)!=(db<0))
        {
            double alpha=da/(da-db);AddCut(new((float)(a.X+(b.X-a.X)*alpha),(float)(-(a.Z+(b.Z-a.Z)*alpha))),epsilon,cuts,ref count);
        }
    }
    static void AddCut(Vector2 point,double epsilon,Span<Vector2> cuts,ref int count)
    {
        for(int i=0;i<count;i++)if(Vector2.DistanceSquared(cuts[i],point)<=epsilon*epsilon)return;
        if(count<cuts.Length)cuts[count++]=point;
    }
}
