using System.Numerics;

namespace LithoCreate.Core;
public sealed class MountingHole
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Diameter { get; set; } = 4;
    public double SlotExtension { get; set; }
    public bool Horizontal { get; set; }
    public double SinkDepth { get; set; }
    public MountingHole Clone()=>(MountingHole)MemberwiseClone();
}
/// <summary>Exact stepped cover with disjoint drilled patches, without a mesh boolean.</summary>
public static class BackMounting
{
    sealed record Patch(double X,double Y,double Radius,double MinX,double MaxX,double MinY,double MaxY,double HalfLength,bool Horizontal,double SinkDepth);
    const double Border=1.02;
    public static void Validate(ModelSettings s)
    {
        if(s.BackMountingHoles is null||s.BackMountingHoles.Count>8||s.BackMountingHoles.Any(h=>h is null||!double.IsFinite(h.X)||!double.IsFinite(h.Y)||!double.IsFinite(h.Diameter)||!double.IsFinite(h.SlotExtension)||!double.IsFinite(h.SinkDepth)))
            throw new InvalidDataException("Befestigungsbohrungen: höchstens acht Einträge mit endlichen X-/Y-Koordinaten und Durchmessern.");
        if(!s.SupportsBackPanel||!s.BackEnabled||s.BackMountingHoles.Count==0)return;
        if(s.BackMountingHoles.Any(h=>h.Diameter is < 2 or > 20))throw new InvalidDataException("Befestigungsbohrungen: Durchmesser 2–20 mm.");
        if(s.BackMountingHoles.Any(h=>h.SlotExtension is < 0 or > 40))throw new InvalidDataException("Langloch: Zusatzlänge 0–40 mm; 0 erzeugt eine Rundbohrung. Gesamtlänge = Durchmesser + Zusatzlänge.");
        if(s.BackMountingHoles.Any(h=>h.SinkDepth<0||h.SinkDepth is > 0 and < .1||h.SinkDepth>s.BackThickness-.4))throw new InvalidDataException("Senkung: 0 (aus) oder mindestens 0,1 mm tief; mindestens 0,4 mm geraden Durchgang stehen lassen. Senkung liegt auf der Außenseite der Rückwand.");
        double w=s.Width-2*(s.FrameWidth+s.BackClearance+s.BackRimWidth),h=s.Height-2*(s.FrameWidth+s.BackClearance+s.BackRimWidth);
        var patches=Patches(s);
        foreach(var p in patches)
            if(p.MinX < -w/2||p.MaxX>w/2||p.MinY<(s.Height-h)/2||p.MaxY>(s.Height+h)/2)
                throw new InvalidDataException("Bohrung liegt zu nah am Einsteckrand. X von der Mitte, Y von der Unterkante messen; mindestens 1 mm Material zum Rand lassen.");
        for(int i=0;i<patches.Count;i++)for(int j=0;j<i;j++)
        {
            var a=patches[i];var b=patches[j];
            if(a.MinX<b.MaxX+.05&&a.MaxX>b.MinX-.05&&a.MinY<b.MaxY+.05&&a.MaxY>b.MinY-.05)
                throw new InvalidDataException("Bohrungen liegen zu dicht zusammen. Ihre rechteckigen Freizonen mit jeweils 1 mm Materialzugabe müssen getrennt bleiben; auch die zentrale Kabelöffnung berücksichtigen.");
        }
    }
    static List<Patch> Patches(ModelSettings s)
    {
        var holes=s.BackMountingHoles.ToList();if(s.BackHoleDiameter>0)holes.Add(new(){Y=s.Height/2,Diameter=s.BackHoleDiameter});
        return holes.Select(h=>{double r=h.SinkDepth==0?h.Diameter/(2*Math.Cos(Math.PI/32)):(h.Diameter/2+h.SinkDepth)/Math.Cos(Math.PI/32),half=r+Border,a=h.SlotExtension/2,dx=half+(h.Horizontal?a:0),dy=half+(h.Horizontal?0:a);return new Patch(h.X,h.Y,h.Diameter/2,h.X-dx,h.X+dx,h.Y-dy,h.Y+dy,a,h.Horizontal,h.SinkDepth);}).ToList();
    }
    public static Mesh Generate(ModelSettings s,bool preview,CancellationToken token=default)
        =>GenerateCore(s,preview,token,false);
    public static Mesh GeneratePlate(double width,double height,double thickness,IReadOnlyList<MountingHole> holes,CancellationToken token=default,GrayImage? photo=null,ModelSettings? photoSettings=null)
    {
        if(holes is null||holes.Count is < 1 or > 8||holes.Any(h=>h is null))throw new InvalidDataException("Montageplatte: eine bis acht Bohrungen eingeben.");
        photoSettings?.Validate();
        if(photo!=null&&(photoSettings==null||holes.Any(h=>h.SlotExtension!=0||h.SinkDepth!=0)))throw new InvalidDataException("Fotoplatte: Rundbohrungen ohne Senkung verwenden.");
        var s=new ModelSettings{Shape=Shape.Lightbox,Width=width,Height=height,FrameWidth=1.2,BoxDepth=3,BackEnabled=true,BackThickness=thickness,BackInsertionDepth=1,BackRimWidth=1.2,BackClearance=.1,BackHoleDiameter=0,BackMountingHoles=holes.Select(h=>h.Clone()).ToList(),Resolution=photoSettings?.Resolution??1};
        return GenerateCore(s,false,token,true,photo,photoSettings);
    }
    static Mesh GenerateCore(ModelSettings s,bool preview,CancellationToken token,bool plain,GrayImage? photo=null,ModelSettings? photoSettings=null)
    {
        s.Validate();token.ThrowIfCancellationRequested();if(!s.SupportsBackPanel||!s.BackEnabled||s.BackMountingHoles.Count==0)throw new InvalidDataException("Für diese Rückwand sind keine Befestigungsbohrungen aktiv.");
        double w=s.Width-2*(s.FrameWidth+s.BackClearance),h=s.Height-2*(s.FrameWidth+s.BackClearance),iw=w-2*s.BackRimWidth,ih=h-2*s.BackRimWidth;
        var xs=new List<double>{-s.Width/2,-w/2,-iw/2,iw/2,w/2,s.Width/2};
        var ys=new List<double>{0,(s.Height-h)/2,(s.Height-ih)/2,(s.Height+ih)/2,(s.Height+h)/2,s.Height};
        if(photo!=null){double step=Math.Max(s.Resolution,Math.Sqrt(s.Width*s.Height/18000));int xCount=(int)Math.Ceiling(s.Width/step),yCount=(int)Math.Ceiling(s.Height/step);for(int i=0;i<=xCount;i++)Axis(xs,-s.Width/2+s.Width*i/xCount);for(int j=0;j<=yCount;j++)Axis(ys,s.Height*j/yCount);}
        // Snap only the auxiliary patch bounds, never the exact envelope or fit dimensions.
        var patches=Patches(s).Select(p=>p with{MinX=Axis(xs,p.MinX),MaxX=Axis(xs,p.MaxX),MinY=Axis(ys,p.MinY),MaxY=Axis(ys,p.MaxY)}).ToArray();xs.Sort();ys.Sort();
        var loops=new List<(Patch Patch,Vector2[] Outer,Capsule? Slot,Capsule? Mouth)>();var extra=new List<Vector2>();
        foreach(var p in patches)
        {
            token.ThrowIfCancellationRequested();
            var points=new List<Vector2>();foreach(double x in xs.Where(x=>x>=p.MinX&&x<=p.MaxX)){Point(x,p.MinY);Point(x,p.MaxY);}foreach(double y in ys.Where(y=>y>=p.MinY&&y<=p.MaxY)){Point(p.MinX,y);Point(p.MaxX,y);}
            int segments=preview?32:Math.Clamp((int)Math.Ceiling(Math.Tau*(p.Radius+p.SinkDepth)/s.Resolution/4)*4,32,128);
            var slot=p.HalfLength>0||p.SinkDepth>0?new Capsule(p,segments):null;
            var mouth=p.SinkDepth>0?new Capsule(p,segments,p.SinkDepth):null;
            if(slot!=null&&mouth==null)foreach(var corner in slot.Corners)Ray(corner.X,corner.Y);
            if(mouth!=null)foreach(var corner in mouth.Corners)Ray(corner.X,corner.Y);
            for(int i=0;i<segments;i++)
            {
                double a=i*Math.Tau/segments;Ray(Math.Cos(a),Math.Sin(a));
            }
            var outer=points.OrderBy(v=>Math.Atan2(v.Y-p.Y,v.X-p.X)).ToArray();loops.Add((p,outer,slot,mouth));extra.AddRange(outer);
            void Ray(double c,double d){double tx=Math.Abs(c)<1e-12?double.PositiveInfinity:((c>0?p.MaxX:p.MinX)-p.X)/c,ty=Math.Abs(d)<1e-12?double.PositiveInfinity:((d>0?p.MaxY:p.MinY)-p.Y)/d,t=Math.Min(tx,ty);Point(Snap(p.X+c*t,p.MinX,p.MaxX),Snap(p.Y+d*t,p.MinY,p.MaxY));}
            void Point(double x,double y){var q=new Vector2((float)x,(float)y);if(points.All(v=>Vector2.DistanceSquared(v,q)>1e-6f))points.Add(q);}
        }
        var m=new Mesh();var ids=new Dictionary<(float X,float Y,float Z),int>();
        int nx=xs.Count-1,ny=ys.Count-1;var heights=new double[nx,ny];var cut=new bool[nx,ny];
        for(int j=0;j<ny;j++)for(int i=0;i<nx;i++)
        {
            double x=(xs[i]+xs[i+1])/2,y=(ys[j]+ys[j+1])/2;
            cut[i,j]=patches.Any(p=>x>p.MinX&&x<p.MaxX&&y>p.MinY&&y<p.MaxY);
            heights[i,j]=s.BackThickness+(!plain&&Math.Abs(x)<w/2&&Math.Abs(y-s.Height/2)<h/2&&(Math.Abs(x)>iw/2||Math.Abs(y-s.Height/2)>ih/2)?s.BackInsertionDepth:0);
        }
        for(int j=0;j<ny;j++)for(int i=0;i<nx;i++)
        {
            token.ThrowIfCancellationRequested();if(cut[i,j])continue;
            double x0=xs[i],x1=xs[i+1],y0=ys[j],y1=ys[j+1],cx=(x0+x1)/2,cy=(y0+y1)/2,z=heights[i,j];
            var boundary=new List<Vector2>{new((float)x0,(float)y0),new((float)x1,(float)y0),new((float)x1,(float)y1),new((float)x0,(float)y1)};
            foreach(var q in extra)if((Math.Abs(q.X-x0)<.0001||Math.Abs(q.X-x1)<.0001)&&q.Y>=y0-.0001&&q.Y<=y1+.0001||(Math.Abs(q.Y-y0)<.0001||Math.Abs(q.Y-y1)<.0001)&&q.X>=x0-.0001&&q.X<=x1+.0001)if(!boundary.Contains(q))boundary.Add(q);
            boundary=boundary.OrderBy(q=>Math.Atan2(q.Y-cy,q.X-cx)).ToList();int lower=V(cx,cy,0),upper=V(cx,cy,z);
            for(int k=0;k<boundary.Count;k++)
            {
                var a=boundary[k];var b=boundary[(k+1)%boundary.Count];int al=V(a.X,a.Y,0),bl=V(b.X,b.Y,0),au=V(a.X,a.Y,z),bu=V(b.X,b.Y,z);m.Face(bl,al,lower);m.Face(au,bu,upper);
                double neighbor=Math.Abs(a.X-x0)<.0001&&Math.Abs(b.X-x0)<.0001?Height(i-1,j):Math.Abs(a.X-x1)<.0001&&Math.Abs(b.X-x1)<.0001?Height(i+1,j):Math.Abs(a.Y-y0)<.0001&&Math.Abs(b.Y-y0)<.0001?Height(i,j-1):Height(i,j+1);
                if(z>neighbor+.0001)m.Quad(V(a.X,a.Y,neighbor),V(b.X,b.Y,neighbor),bu,au);
            }
        }
        foreach(var (p,outer,slot,mouth) in loops)
        {
            var angles=outer.Select(v=>Math.Atan2(v.Y-p.Y,v.X-p.X)).ToArray();double gap=Enumerable.Range(0,angles.Length).Max(i=>(angles[(i+1)%angles.Length]-angles[i]+Math.Tau)%Math.Tau);
            double r=p.Radius/Math.Cos(gap/2)*(1+1e-7);var inner=new Vector2[outer.Length];var lowerLoop=mouth==null?inner:new Vector2[outer.Length];
            for(int i=0;i<outer.Length;i++)
            {
                var direction=Vector2.Normalize(outer[i]-new Vector2((float)p.X,(float)p.Y));
                if(mouth==null){double distance=slot?.Distance(direction.X,direction.Y)??r;inner[i]=new((float)(p.X+distance*direction.X),(float)(p.Y+distance*direction.Y));}
                else{var upper=mouth.CorrespondingPoint(slot!,direction.X,direction.Y);inner[i]=new((float)(p.X+upper.X),(float)(p.Y+upper.Y));double bottom=mouth.Distance(direction.X,direction.Y);lowerLoop[i]=new((float)(p.X+bottom*direction.X),(float)(p.Y+bottom*direction.Y));}
            }
            for(int i=0;i<outer.Length;i++)
            {
                token.ThrowIfCancellationRequested();int n=(i+1)%outer.Length;var a=outer[i];var b=outer[n];var c=inner[n];var d=inner[i];
                m.Quad(V(a.X,a.Y,s.BackThickness),V(b.X,b.Y,s.BackThickness),V(c.X,c.Y,s.BackThickness),V(d.X,d.Y,s.BackThickness));
                var lowA=lowerLoop[i];var lowB=lowerLoop[n];
                m.Quad(V(b.X,b.Y,0),V(a.X,a.Y,0),V(lowA.X,lowA.Y,0),V(lowB.X,lowB.Y,0));
                m.Quad(V(d.X,d.Y,s.BackThickness),V(c.X,c.Y,s.BackThickness),V(c.X,c.Y,p.SinkDepth),V(d.X,d.Y,p.SinkDepth));
                if(mouth!=null)m.Quad(V(d.X,d.Y,p.SinkDepth),V(c.X,c.Y,p.SinkDepth),V(lowB.X,lowB.Y,0),V(lowA.X,lowA.Y,0));
                m.ActualResolution=Math.Max(m.ActualResolution,Math.Max(Vector2.Distance(c,d),Vector2.Distance(lowA,lowB)));
            }
        }
        m.OrientOutward();return m;
        double Height(int i,int j)=>i<0||j<0||i>=nx||j>=ny?0:heights[i,j];
        int V(double x,double y,double z){bool front=photo!=null&&z==s.BackThickness;double u=x/s.Width+.5,v=1-y/s.Height;if(front){double gray=photo!.Sample(u,v);if(photoSettings!.Negative)gray=1-gray;z=photoSettings.MinThickness+(1-gray)*(photoSettings.MaxThickness-photoSettings.MinThickness);}var key=((float)x,(float)y,(float)z);if(ids.TryGetValue(key,out int id))return id;id=m.Vertices.Count;ids.Add(key,id);m.Vertices.Add(new(key.Item1,key.Item2,key.Item3));m.TextureCoordinates.Add(new((float)u,(float)v));m.PhotoVertices.Add(front);return id;}
    }
    // Intersection of tangent half-planes contains the exact capsule, preserving its free width.
    // Rays through every polygon corner keep the inner loop exact even with extra patch nodes.
    sealed class Capsule
    {
        readonly (double X,double Y,double H)[] planes;
        public readonly (double X,double Y)[] Corners;
        public Capsule(Patch p,int segments,double offset=0)
        {
            planes=new (double,double,double)[segments];Corners=new (double,double)[segments];
            for(int i=0;i<segments;i++){double a=i*Math.Tau/segments,x=Math.Cos(a),y=Math.Sin(a);planes[i]=(x,y,p.Radius+offset+p.HalfLength*Math.Abs(p.Horizontal?x:y));}
            for(int i=0;i<segments;i++){var a=planes[i];var b=planes[(i+1)%segments];double det=a.X*b.Y-a.Y*b.X;Corners[i]=((a.H*b.Y-a.Y*b.H)/det,(a.X*b.H-a.H*b.X)/det);}
        }
        public double Distance(double x,double y){double distance=double.PositiveInfinity;foreach(var n in planes){double dot=n.X*x+n.Y*y;if(dot>1e-12)distance=Math.Min(distance,n.H/dot);}return distance;}
        public (double X,double Y) CorrespondingPoint(Capsule target,double x,double y)
        {
            // Match the same edge and edge fraction at both radii. Radial matching would
            // twist the bevel on capsule endcaps instead of producing planar 45° faces.
            int edge=0;double distance=double.PositiveInfinity;
            for(int i=0;i<planes.Length;i++){var n=planes[i];double dot=n.X*x+n.Y*y;if(dot>1e-12&&n.H/dot<distance){distance=n.H/dot;edge=i;}}
            int previous=(edge+planes.Length-1)%planes.Length;var a=Corners[previous];var b=Corners[edge];double dx=b.X-a.X,dy=b.Y-a.Y;
            double t=Math.Clamp(((x*distance-a.X)*dx+(y*distance-a.Y)*dy)/(dx*dx+dy*dy),0,1);var c=target.Corners[previous];var d=target.Corners[edge];return(c.X+(d.X-c.X)*t,c.Y+(d.Y-c.Y)*t);
        }
    }
    static double Axis(List<double> axis,double value){foreach(double v in axis)if(Math.Abs(v-value)<.01)return v;axis.Add(value);return value;}
    static double Snap(double value,double a,double b)=>Math.Abs(value-a)<1e-7?a:Math.Abs(value-b)<1e-7?b:value;
}
