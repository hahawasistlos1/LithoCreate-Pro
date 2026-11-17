using System.Numerics;

namespace LithoCreate.Core;
public readonly record struct Triangle(int A,int B,int C);
public sealed class Mesh
{
    sealed class EdgeComparer : IEqualityComparer<ulong>
    {
        public bool Equals(ulong a,ulong b)=>a==b;
        public int GetHashCode(ulong value)=>HashCode.Combine((uint)(value>>32),(uint)value);
    }
    public List<Vector3> Vertices { get; } = [];
    public List<Vector2> TextureCoordinates { get; } = [];
    public List<bool> PhotoVertices { get; } = [];
    public bool WrapTextureU { get; set; }
    public bool IsAssemblyPreview { get; set; }
    public List<Triangle> Triangles { get; } = [];
    public double ActualResolution { get; set; }
    public void Face(int a,int b,int c) => Triangles.Add(new(a,b,c));
    public void Quad(int a,int b,int c,int d) { Face(a,b,c); Face(a,c,d); }
    public (Vector3 Min,Vector3 Max) Bounds()
    {
        var min=new Vector3(float.MaxValue); var max=new Vector3(float.MinValue);
        foreach(var v in Vertices) { min=Vector3.Min(min,v); max=Vector3.Max(max,v); }
        return(min,max);
    }
    public MeshReport Validate(CancellationToken token=default)
    {
        var edges=new Dictionary<ulong,(int Count,int Direction)>(Triangles.Count*2,new EdgeComparer());
        int degenerate=0; double volume=0;
        for(int i=0;i<Triangles.Count;i++)
        {
            if((i&4095)==0) token.ThrowIfCancellationRequested();
            var t=Triangles[i];
            if(t.A<0 || t.B<0 || t.C<0 || t.A>=Vertices.Count || t.B>=Vertices.Count || t.C>=Vertices.Count) throw new InvalidDataException("Ungültiger Vertexindex.");
            var a=Vertices[t.A];var b=Vertices[t.B];var c=Vertices[t.C];
            if(!Finite(a)||!Finite(b)||!Finite(c)) throw new InvalidDataException("Ungültige Koordinaten.");
            var cross=Vector3.Cross(b-a,c-a);
            if(cross.LengthSquared()<1e-16f) degenerate++;
            volume+=Vector3.Dot(a,Vector3.Cross(b,c))/6.0;
            Add(t.A,t.B); Add(t.B,t.C); Add(t.C,t.A);
        }
        return new(Vertices.Count,Triangles.Count,edges.Values.Count(v=>v.Count!=2),edges.Values.Count(v=>v.Direction!=0),degenerate,volume,Bounds(),ActualResolution);
        void Add(int a,int b) { ulong key=((ulong)(uint)Math.Min(a,b)<<32)|(uint)Math.Max(a,b); edges.TryGetValue(key,out var v); edges[key]=(v.Count+1,v.Direction+(a<b?1:-1)); }
    }
    static bool Finite(Vector3 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);
    public void OrientOutward()
    {
        double volume=0;
        foreach(var t in Triangles) volume+=Vector3.Dot(Vertices[t.A],Vector3.Cross(Vertices[t.B],Vertices[t.C]));
        if(volume<0) for(int i=0;i<Triangles.Count;i++) { var t=Triangles[i];Triangles[i]=new(t.A,t.C,t.B); }
    }
}
public record MeshReport(int Vertices,int Triangles,int OpenOrNonManifoldEdges,int InconsistentEdges,int DegenerateFaces,double Volume,(Vector3 Min,Vector3 Max) Bounds,double Resolution)
{
    public bool IsValid => Triangles>0 && OpenOrNonManifoldEdges==0 && InconsistentEdges==0 && DegenerateFaces==0 && double.IsFinite(Volume) && Volume>0;
    public Vector3 Size => Bounds.Max-Bounds.Min;
}
public sealed class GrayImage(int width,int height,byte[] pixels)
{
    public int Width { get; }=width;
    public int Height { get; }=height;
    public byte[] Pixels { get; }=pixels;
    public double Sample(double u,double v)
    {
        double x=Math.Clamp(u,0,1)*(Width-1),y=Math.Clamp(v,0,1)*(Height-1);
        int ix=(int)x,iy=(int)y,jx=Math.Min(ix+1,Width-1),jy=Math.Min(iy+1,Height-1);
        double fx=x-ix,fy=y-iy;
        return ((Pixels[iy*Width+ix]*(1-fx)+Pixels[iy*Width+jx]*fx)*(1-fy)+(Pixels[jy*Width+ix]*(1-fx)+Pixels[jy*Width+jx]*fx)*fy)/255;
    }
}
public static class MeshGenerator
{
    public static Mesh Generate(GrayImage image,ModelSettings s,bool preview,CancellationToken token=default,IProgress<double>? progress=null)
    {
        s.Validate();
        if(s.HasDesignParts)
        {
            if(!preview&&s.Shape!=Shape.PhotoHoneycomb)throw new InvalidDataException("Die Einzelteile über die Bauteil-Auswahl exportieren.");
            var design=s.Shape==Shape.PhotoHoneycomb?GeneratePanel(image,s,preview,token,progress):DesignParts.Preview(s,image,[],token);
            if(s.Shape==Shape.PhotoHoneycomb)PrintPlacement.Apply(design,s.PrintOrientation,s.TurnOnBed90,token,s.Angles);
            progress?.Report(1);
            return design;
        }
        return GeneratePanel(image,s,preview,token,progress,applyPlacement:true);
    }
    internal static Mesh GeneratePanel(GrayImage image,ModelSettings s,bool preview,CancellationToken token,IProgress<double>? progress=null,bool applyPlacement=false)
    {
        if(s.IsShade)
        {
            var shade=LampShade.Generate(image,s,preview,token,progress);
            PrintPlacement.Apply(shade,s.PrintOrientation,s.TurnOnBed90,token,s.Angles);
            progress?.Report(1);
            return shade;
        }
        if(s.EffectivePart==ModelPart.Socket){var socket=SphereSocket.Generate(s,preview,token);PrintPlacement.Apply(socket,s.SocketPrintOrientation,s.SocketTurnOnBed90,token,s.SocketAngles);progress?.Report(1);return socket;}
        if(s.SupportsBackPanel&&s.BackEnabled)
        {
            Mesh back;
            if(s.EffectivePart==ModelPart.BackPanel){back=LightboxBack.Generate(s,preview,token);PrintPlacement.Apply(back,s.BackPrintOrientation,s.BackTurnOnBed90,token,s.BackAngles);}
            else{back=LightboxBack.GenerateBody(image,s,preview,token);if(s.EffectivePart==ModelPart.Assembly)back=LightboxBack.Assemble(back,LightboxBack.Generate(s,preview,token),s,token);else PrintPlacement.Apply(back,s.PrintOrientation,s.TurnOnBed90,token,s.Angles);}
            progress?.Report(1);return back;
        }
        var m=new Mesh();
        // The validation edge table and WPF copies dominate memory; reserve 600 bytes per surface cell.
        int budget=preview?22000:(int)Math.Min(1200000,(long)s.MemoryMb*1024*1024/600);
        double step=preview?Math.Max(s.Resolution,Math.Max(s.Width,s.Height)/130):s.Resolution;
        bool sphere=s.Shape is Shape.Sphere or Shape.Moon;
        bool cylinder=s.Shape is Shape.Cylinder or Shape.Polygon;
        m.WrapTextureU=sphere||cylinder;
        bool radial=s.Shape is Shape.Heart or Shape.Star or Shape.Oval or Shape.Disc or Shape.Pendant or Shape.Drop;
        double radialSpan=s.Shape is Shape.Disc or Shape.Pendant?s.Diameter:Math.Max(s.Width,s.Height);
        double w=(sphere||cylinder)?Math.PI*s.Diameter:radial?Math.PI*radialSpan:s.Width;
        double h=sphere?Math.PI*s.Diameter/2:cylinder?s.Height:radial?radialSpan/2:s.Height;
        step=Math.Max(step,Math.Sqrt(w*h/budget));
        int nx=Math.Clamp((int)Math.Ceiling(w/step),8,2400),ny=Math.Clamp((int)Math.Ceiling(h/step),4,2400);
        if(s.Shape==Shape.Polygon) nx=(int)Math.Ceiling(nx/(double)s.Sides)*s.Sides;
        if(radial)nx=(int)Math.Ceiling(nx/20.0)*20;
        if(s.Shape==Shape.PhotoHoneycomb&&ny%2!=0)ny++;
        if(s.IsArch&&nx%2!=0)nx++;
        m.ActualResolution=Math.Max(w/nx,h/ny);
        double Thickness(double u,double v)
        {
            double gray=image.Sample(u,1-v);if(s.Negative)gray=1-gray;
            return s.MinThickness+(1-gray)*(s.MaxThickness-s.MinThickness);
        }
        if(sphere) Sphere();
        else if(radial) Radial();
        else Grid();
        m.OrientOutward();
        if(s.EffectivePart==ModelPart.Assembly)m=SphereSocket.Assemble(m,SphereSocket.Generate(s,preview,token),s.SocketPlateHeight,token);
        else if(applyPlacement)PrintPlacement.Apply(m,s.PrintOrientation,s.TurnOnBed90,token,s.Angles);
        progress?.Report(1);return m;

        int V(double x,double y,double z,double u=.5,double v=.5,bool photo=true) { m.Vertices.Add(new((float)x,(float)y,(float)z));m.TextureCoordinates.Add(new((float)u,(float)(1-v)));m.PhotoVertices.Add(photo);return m.Vertices.Count-1; }
        void Check(int row) { token.ThrowIfCancellationRequested();progress?.Report(Math.Min(.99,row/(double)(ny*2+6))); }
        void Grid()
        {
            var tabs=s.SupportsHangingTabs&&s.HangingTabsEnabled?HangingTabs.CreateLayout(s,nx,preview):null;
            if(tabs!=null)nx=tabs.U.Length-1;
            int cols=cylinder?nx:nx+1;
            bool stand=s.SupportsStand && s.StandEnabled;
            bool floor=cylinder && s.LampBaseThickness>0;
            int rowCount=ny+1+(stand?2:0);
            bool arch=s.Shape==Shape.ArchedWindow,honey=s.Shape==Shape.PhotoHoneycomb;
            int holeHalf=Math.Max(1,(int)Math.Round(2.5/s.Width*nx));
            int holeTop=ny-Math.Max(1,(int)Math.Round(4/s.Height*ny));
            int holeBottom=holeTop-Math.Max(1,(int)Math.Round(5/s.Height*ny));
            if(stand){holeBottom+=2;holeTop+=2;}
            int holeLeft=nx/2-holeHalf,holeRight=(nx+1)/2+holeHalf;
            int[,]? ids=arch&&s.ArchHangingHole?new int[rowCount,cols]:null;
            bool Hole(int i,int j)=>ids!=null&&i>=holeLeft&&i<holeRight&&j>=holeBottom&&j<holeTop;
            for(int side=0;side<2;side++) for(int j=0;j<rowCount;j++)
            {
                Check(side*rowCount+j);
                for(int i=0;i<cols;i++)
                {
                    if(ids!=null)
                    {
                        if(i>holeLeft&&i<holeRight&&j>holeBottom&&j<holeTop)continue;
                        if(side==0)ids[j,i]=m.Vertices.Count;
                    }
                    double u=tabs?.U[i]??i/(double)nx,v=stand?Math.Max(0,j-2)/(double)ny:j/(double)ny,t=side==0?Thickness(u,v):0;
                    if(cylinder)
                    {
                        double angle=(u+.5)*Math.Tau;
                        double cosine=1,apothem=s.Diameter/2;
                        if(s.Shape==Shape.Polygon) { double sector=Math.Tau/s.Sides;double local=((angle+sector/2)%sector)-sector/2;cosine=Math.Cos(local);apothem*=Math.Cos(sector/2); }
                        // For polygonal lamps, thickness is measured normal to the flat face.
                        double r=(apothem-s.MaxThickness+t)/cosine;
                        double y=floor&&side==1?s.LampBaseThickness+v*(s.Height-s.LampBaseThickness):v*s.Height;
                        V(r*Math.Sin(angle),y,r*Math.Cos(angle),u,v);
                    }
                    else
                    {
                        double x=(u-.5)*s.Width,y=stand?(j==0?0:j==1?s.StandHeight:s.StandHeight+v*s.Height):v*s.Height;
                        bool border=s.FrameWidth>0&&(Math.Min(u,1-u)*s.Width<s.FrameWidth || Math.Min(v,1-v)*s.Height<s.FrameWidth);
                        if(border&&side==0)t=s.MaxThickness+1;
                        if(arch&&!(stand&&j<2))
                        {
                            double rise=Math.Min(s.Width*.5,s.Height*.65);
                            double top=s.Height-rise+rise*Math.Sqrt(Math.Max(0,1-Math.Pow(2*u-1,2)));
                            y=(stand?s.StandHeight:0)+v*top;
                            if(side==0&&s.ArchHangingHole&&i>=holeLeft-1&&i<=holeRight+1&&j>=holeBottom-1&&j<=holeTop+1)t=s.MaxThickness+1;
                        }
                        if(honey)
                        {
                            double rowWidth=s.Width*(1-Math.Abs(v-.5));
                            x=(u-.5)*rowWidth;
                            double sideScale=Math.Sqrt(1+Math.Pow(s.Width/(2*s.Height),2));
                            if(side==0&&(Math.Min(u,1-u)*rowWidth/sideScale<Math.Max(5,s.FrameWidth)||
                                         Math.Min(v,1-v)*s.Height<Math.Max(5,s.FrameWidth)))t=s.MaxThickness+1;
                            u=x/s.Width+.5;
                            if(side==0&&t!=s.MaxThickness+1)t=Thickness(u,v);
                        }
                        if(s.Shape==Shape.WavePicture){V(x,y,t+Math.Sin((u-.5)*Math.Tau)*Math.Min(s.Width*.10,s.Radius*.25),u,v);continue;}
                        if(s.Shape==Shape.Lightbox&&border&&side==1)t=-s.BoxDepth;
                        if(stand&&j<2)t=side==0?s.StandDepth/2:-s.StandDepth/2;
                        if(s.Shape is Shape.Curved or Shape.Nightlight)
                        { double a=x/s.Radius;V((s.Radius+t)*Math.Sin(a),y,(s.Radius+t)*Math.Cos(a)-s.Radius,u,v,!(stand&&j<2)); }
                        else V(x,y,t,u,v,!(stand&&j<2));
                    }
                }
            }
            int offset=m.Vertices.Count/2;
            int Id(int x,int y)=>ids==null?y*cols+(x%cols):ids[y,x];
            for(int j=0;j<rowCount-1;j++)for(int i=0;i<nx;i++)
            {
                int a=Id(i,j),b=Id(i+1,j),c=Id(i+1,j+1),d=Id(i,j+1);
                if(Hole(i,j))
                {
                    if(!Hole(i,j-1))Edge(b,a);
                    if(!Hole(i+1,j))Edge(c,b);
                    if(!Hole(i,j+1))Edge(d,c);
                    if(!Hole(i-1,j))Edge(a,d);
                    continue;
                }
                m.Quad(a,b,c,d);m.Quad(a+offset,d+offset,c+offset,b+offset);
            }
            for(int i=0;i<nx;i++)
            {
                if(!floor)Edge(Id(i,0),Id(i+1,0));
                double x=tabs==null?0:((tabs.U[i]+tabs.U[i+1])/2-.5)*s.Width;
                if(tabs==null||!tabs.Tabs.Any(t=>x>t.MinX&&x<t.MaxX))Edge(Id(i+1,rowCount-1),Id(i,rowCount-1));
            }
            if(tabs!=null)HangingTabs.Append(m,s,tabs,Enumerable.Range(0,cols).Select(i=>Id(i,rowCount-1)).ToArray(),Enumerable.Range(0,cols).Select(i=>Id(i,rowCount-1)+offset).ToArray(),token);
            if(floor)
            {
                if(s.LampBaseHoleDiameter==0)
                {
                    int bottom=V(0,0,0,photo:false),top=V(0,s.LampBaseThickness,0,photo:false);
                    for(int i=0;i<nx;i++){int a=Id(i,0),b=Id(i+1,0);m.Face(b,a,bottom);m.Face(a+offset,b+offset,top);}
                }
                else
                {
                    int[] lower=new int[nx],upper=new int[nx];double radius=s.LampBaseHoleDiameter/(2*Math.Cos(Math.PI/nx));
                    for(int i=0;i<nx;i++){double a=(i/(double)nx+.5)*Math.Tau;lower[i]=V(radius*Math.Sin(a),0,radius*Math.Cos(a),photo:false);upper[i]=V(radius*Math.Sin(a),s.LampBaseThickness,radius*Math.Cos(a),photo:false);}
                    for(int i=0;i<nx;i++){int n=(i+1)%nx,a=Id(i,0),b=Id(i+1,0);m.Quad(b,a,lower[i],lower[n]);m.Quad(a+offset,b+offset,upper[n],upper[i]);m.Quad(lower[n],lower[i],upper[i],upper[n]);}
                }
            }
            if(!cylinder)
            {
                // At a stand shoulder the end wall has collinear split vertices. A single quad
                // would create a T-junction; triangulate the six-vertex base end instead.
                if(stand){StandEnd(0,false);StandEnd(nx,true);}
                for(int j=stand?2:0;j<rowCount-1;j++) { Edge(Id(nx,j),Id(nx,j+1));Edge(Id(0,j+1),Id(0,j)); }
            }
            void Edge(int a,int b)=>m.Quad(b,a,a+offset,b+offset);
            void StandEnd(int column,bool reverse)
            {
                int[] polygon=[Id(column,0),Id(column,1),Id(column,2),Id(column,2)+offset,Id(column,1)+offset,Id(column,0)+offset];
                for(int i=1;i<polygon.Length-1;i++)if(reverse)m.Face(polygon[0],polygon[i+1],polygon[i]);else m.Face(polygon[0],polygon[i],polygon[i+1]);
            }
        }
        void Radial()
        {
            bool hole=s.Shape==Shape.Pendant;
            int rings=ny;
            var outline=new Vector2[nx];
            for(int i=0;i<nx;i++)
            {
                double a=i*Math.Tau/nx,r=1;
                if(s.Shape==Shape.Heart)
                {
                    outline[i]=new((float)(-16*Math.Pow(Math.Sin(a),3)),(float)(13*Math.Cos(a)-5*Math.Cos(2*a)-2*Math.Cos(3*a)-Math.Cos(4*a)));continue;
                }
                if(s.Shape==Shape.Star)
                {
                    double sector=Math.Tau/10;int k=(int)Math.Floor((a+Math.PI/2+Math.Tau)%Math.Tau/sector);
                    double a0=k*sector-Math.PI/2,a1=a0+sector;
                    var p=new Vector2((float)(Math.Cos(a0)*(k%2==0?1:.45)),(float)(Math.Sin(a0)*(k%2==0?1:.45)));
                    var q=new Vector2((float)(Math.Cos(a1)*(k%2==0?.45:1)),(float)(Math.Sin(a1)*(k%2==0?.45:1)));
                    r=(p.X*q.Y-p.Y*q.X)/(Math.Cos(a)*(q.Y-p.Y)-Math.Sin(a)*(q.X-p.X));
                }
                if(s.Shape==Shape.Drop)r=.72-.28*Math.Sin(a);
                outline[i]=new((float)(r*Math.Cos(a)),(float)(r*Math.Sin(a)));
            }
            float minX=outline.Min(p=>p.X),maxX=outline.Max(p=>p.X),minY=outline.Min(p=>p.Y),maxY=outline.Max(p=>p.Y);
            double targetW=s.Shape is Shape.Disc or Shape.Pendant?s.Diameter:s.Width,targetH=s.Shape is Shape.Disc or Shape.Pendant?s.Diameter:s.Height;
            double sx=targetW/(maxX-minX),sy=targetH/(maxY-minY);
            var front=new List<int[]>();var back=new List<int[]>();
            for(int side=0;side<2;side++)
            {
                var rows=side==0?front:back;
                if(!hole){double u=-minX*sx/targetW,v=-minY*sy/targetH;rows.Add([V(0,-minY*sy,side==0?Thickness(u,v):0,u,v)]);}
                for(int j=hole?0:1;j<=rings;j++)
                {
                    Check(side*rings+j);var row=new int[nx];
                    for(int i=0;i<nx;i++)
                    {
                        double a=i*Math.Tau/nx,r=j/(double)rings;
                        double rr=s.HoleDiameter/2+r*(s.Diameter/2-s.HoleDiameter/2);
                        double x=hole?rr*Math.Cos(a):outline[i].X*sx*r,y=hole?rr*Math.Sin(a):outline[i].Y*sy*r;
                        double u=(x-minX*sx)/targetW,v=(y-minY*sy)/targetH;
                        row[i]=V(x,y-minY*sy,side==0?Thickness(u,v):0,u,v);
                    }
                    rows.Add(row);
                }
                for(int j=0;j<rows.Count-1;j++)for(int i=0;i<nx;i++)
                {
                    int next=(i+1)%nx;
                    if(rows[j].Length==1) { if(side==0)m.Face(rows[j][0],rows[j+1][i],rows[j+1][next]);else m.Face(rows[j][0],rows[j+1][next],rows[j+1][i]); }
                    else if(side==0)m.Quad(rows[j][i],rows[j+1][i],rows[j+1][next],rows[j][next]);
                    else m.Quad(rows[j][next],rows[j+1][next],rows[j+1][i],rows[j][i]);
                }
            }
            for(int i=0;i<nx;i++) { int next=(i+1)%nx;m.Quad(front[^1][next],front[^1][i],back[^1][i],back[^1][next]);if(hole)m.Quad(front[0][i],front[0][next],back[0][next],back[0][i]); }
        }
        void Sphere()
        {
            double inner=s.Diameter/2-s.MaxThickness;
            bool lowerOpen=s.SphereOpeningDiameter>0,upperOpen=s.SphereVentDiameter>0;
            double chordCosine=Math.Cos(Math.PI/nx);
            double lower=lowerOpen?-Math.Acos(s.SphereOpeningDiameter/(2*inner*chordCosine)):-Math.PI/2;
            double upper=upperOpen?Math.Acos(s.SphereVentDiameter/(2*inner*chordCosine)):Math.PI/2;
            double cutY=inner*Math.Sin(lower);
            double maximumRadius=inner+s.MaxThickness+(s.Shape==Shape.Moon?.24:0);
            double outerLower=lowerOpen?Math.Asin(cutY/maximumRadius):lower;
            double reinforcementAngle=Math.Clamp(3*s.MaxThickness/maximumRadius,.035,.25);
            var surfaces=new List<int[]>[]{[],[]};
            for(int side=0;side<2;side++)
            {
                var rows=surfaces[side];
                if(!lowerOpen)rows.Add([V(0,-(side==0?inner+Thickness(.5,0):inner),0,.5,0)]);
                for(int j=lowerOpen?0:1;j<=(upperOpen?ny:ny-1);j++)
                {
                    double startAngle=side==0?outerLower:lower;
                    Check(side*ny+j);double phi=startAngle+(upper-startAngle)*j/ny,v=(phi+Math.PI/2)/Math.PI;
                    var row=new int[nx];
                    for(int i=0;i<nx;i++)
                    {
                        double u=i/(double)nx,a=(u+.5)*Math.Tau,r=inner+(side==0?Thickness(u,v):0);
                        if(s.Shape==Shape.Moon&&side==0)r+=.12*(1+Math.Sin(a*17)*Math.Sin(phi*23));
                        if(lowerOpen&&side==0)
                        {
                            // A planar cut through a radial shell otherwise creates a thin neck
                            // where the spherical inside joins a cylindrical collar. Reinforce
                            // the rim to maximum thickness and blend back into the photo above it.
                            double blend=Math.Clamp((phi-outerLower)/reinforcementAngle,0,1);
                            blend=blend*blend*(3-2*blend);r=maximumRadius*(1-blend)+r*blend;
                        }
                        row[i]=V(r*Math.Cos(phi)*Math.Sin(a),r*Math.Sin(phi),r*Math.Cos(phi)*Math.Cos(a),u,v,!(lowerOpen&&j==0));
                    }
                    rows.Add(row);
                }
                if(!upperOpen)rows.Add([V(0,side==0?inner+Thickness(.5,1):inner,0,.5,1)]);
                for(int j=0;j<rows.Count-1;j++)for(int i=0;i<nx;i++)
                {
                    int n=(i+1)%nx;var current=rows[j];var next=rows[j+1];
                    if(current.Length==1)F(current[0],next[n],next[i]);
                    else if(next.Length==1)F(next[0],current[i],current[n]);
                    else{F(current[i],current[n],next[n]);F(current[i],next[n],next[i]);}
                }
                void F(int a,int b,int c) { if(side==0)m.Face(a,b,c);else m.Face(a,c,b); }
            }
            if(lowerOpen)
            {
                var outer=surfaces[0][0];var inside=surfaces[1][0];
                if(s.SphereCollarHeight==0)
                {
                    for(int i=0;i<nx;i++){int n=(i+1)%nx;m.Quad(outer[n],outer[i],inside[i],inside[n]);}
                }
                else
                {
                    int[] outerFoot=new int[nx],innerFoot=new int[nx];double y=cutY-s.SphereCollarHeight;
                    double radius=s.SphereOpeningDiameter/(2*chordCosine),outerRadius=maximumRadius*Math.Cos(outerLower);
                    for(int i=0;i<nx;i++)
                    {
                        double a=(i/(double)nx+.5)*Math.Tau;
                        outerFoot[i]=V(outerRadius*Math.Sin(a),y,outerRadius*Math.Cos(a),photo:false);
                        innerFoot[i]=V(radius*Math.Sin(a),y,radius*Math.Cos(a),photo:false);
                    }
                    for(int i=0;i<nx;i++)
                    {
                        int n=(i+1)%nx;
                        m.Quad(outer[n],outer[i],outerFoot[i],outerFoot[n]);
                        m.Quad(inside[i],inside[n],innerFoot[n],innerFoot[i]);
                        m.Quad(outerFoot[n],outerFoot[i],innerFoot[i],innerFoot[n]);
                    }
                }
            }
            if(upperOpen)for(int i=0;i<nx;i++){int n=(i+1)%nx;m.Quad(surfaces[0][^1][i],surfaces[0][^1][n],surfaces[1][^1][n],surfaces[1][^1][i]);}
        }
    }
}
