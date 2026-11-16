using System.Numerics;

namespace LithoCreate.Core;
/// <summary>A removable cover and its exact rectangular receiver. Coordinates are Y-up.</summary>
public static class LightboxBack
{
    public static readonly PartChoice[] Parts=[new(ModelPart.Lithophane,"Leuchtkasten"),new(ModelPart.BackPanel,"Rückwand"),new(ModelPart.Assembly,"Montageansicht")];
    public static void ValidateSettings(ModelSettings s)
    {
        double w=s.Width-2*s.FrameWidth,h=s.Height-2*s.FrameWidth;
        if(s.FrameWidth<1.2||Math.Min(w,h)<6)throw new InvalidDataException("Rückwand: Rahmenbreite mindestens 1,2 mm und Innenöffnung mindestens 6 × 6 mm.");
        if(s.BackClearance is < .1 or > 1.5)throw new InvalidDataException("Rückwand-Passspiel: 0,1–1,5 mm pro Seite.");
        if(s.BackRimWidth<1.2||s.BackRimWidth>10||Math.Min(w,h)-2*s.BackClearance-2*s.BackRimWidth<2)throw new InvalidDataException("Einsteckrand: 1,2–10 mm stark; innen müssen mindestens 2 × 2 mm frei bleiben.");
        if(s.BackThickness is < .8 or > 10)throw new InvalidDataException("Rückwandstärke: 0,8–10 mm.");
        if(s.BackInsertionDepth<.8||s.BackInsertionDepth>s.BoxDepth-1)throw new InvalidDataException("Einstecktiefe: mindestens 0,8 mm; mindestens 1 mm Abstand zum Fotopanel lassen.");
        if(s.BackHoleDiameter<0||s.BackHoleDiameter is > 0 and < 2||s.BackHoleDiameter/Math.Cos(Math.PI/32)>Math.Min(w,h)-2*s.BackClearance-2*s.BackRimWidth-1.6)
            throw new InvalidDataException("Rückwand-Durchführung: 0 (geschlossen) oder mindestens 2 mm; zwischen Durchführung und Einsteckrand müssen mindestens 0,8 mm Material bleiben.");
    }
    public static Mesh GenerateBody(GrayImage image,ModelSettings s,bool preview,CancellationToken token=default)
    {
        s.Validate();token.ThrowIfCancellationRequested();
        if(!s.SupportsBackPanel||!s.BackEnabled)throw new InvalidDataException("Die Aufnahme ist nur für aktivierte Leuchtkästen verfügbar.");
        var m=new Mesh();double w=s.Width-2*s.FrameWidth,h=s.Height-2*s.FrameWidth;
        int budget=preview?22000:(int)Math.Min(1200000,(long)s.MemoryMb*1024*1024/600);
        double step=Math.Max(s.Resolution,Math.Sqrt(w*h/budget));if(preview)step=Math.Max(step,Math.Max(w,h)/130);
        int nx=Math.Clamp((int)Math.Ceiling(w/step),8,2400),ny=Math.Clamp((int)Math.Ceiling(h/step),4,2400);m.ActualResolution=Math.Max(s.Width/nx,s.Height/ny);
        int cols=nx+1,offset=cols*(ny+1);
        for(int side=0;side<2;side++)for(int j=0;j<=ny;j++)
        {
            token.ThrowIfCancellationRequested();
            for(int i=0;i<=nx;i++)
            {
                double x=-w/2+i*w/nx,y=s.FrameWidth+j*h/ny,u=x/s.Width+.5,v=y/s.Height,gray=image.Sample(u,1-v);if(s.Negative)gray=1-gray;
                Vertex(x,y,side==0?s.MinThickness+(1-gray)*(s.MaxThickness-s.MinThickness):0,u,v,side==0);
            }
        }
        for(int j=0;j<ny;j++){token.ThrowIfCancellationRequested();for(int i=0;i<nx;i++){int a=j*cols+i,b=a+1,c=b+cols,d=a+cols;m.Quad(a,b,c,d);m.Quad(a+offset,d+offset,c+offset,b+offset);}}
        var edge=new List<int>();for(int i=0;i<nx;i++)edge.Add(i);for(int j=0;j<ny;j++)edge.Add(j*cols+nx);for(int i=nx;i>0;i--)edge.Add(ny*cols+i);for(int j=ny;j>0;j--)edge.Add(j*cols);
        int[] previous=edge.ToArray();
        foreach(var ring in new[]{(Outer:false,Z:s.MaxThickness+1),(Outer:true,Z:s.MaxThickness+1),(Outer:true,Z:-s.BoxDepth),(Outer:false,Z:-s.BoxDepth)})
        {
            var next=new int[edge.Count];for(int i=0;i<edge.Count;i++){var p=m.Vertices[edge[i]];double x=ring.Outer?p.X*s.Width/w:p.X,y=ring.Outer?(p.Y-s.Height/2)*s.Height/h+s.Height/2:p.Y;next[i]=Vertex(x,y,ring.Z,.5,.5,false);}Join(previous,next);previous=next;
        }
        Join(previous,edge.Select(i=>i+offset).ToArray());m.OrientOutward();return m;
        int Vertex(double x,double y,double z,double u,double v,bool photo){int id=m.Vertices.Count;m.Vertices.Add(new((float)x,(float)y,(float)z));m.TextureCoordinates.Add(new((float)u,(float)(1-v)));m.PhotoVertices.Add(photo);return id;}
        void Join(int[] a,int[] b){for(int i=0;i<a.Length;i++){int n=(i+1)%a.Length;m.Quad(a[i],b[i],b[n],a[n]);}}
    }
    public static Mesh Generate(ModelSettings s,bool preview,CancellationToken token=default)
    {
        s.Validate();token.ThrowIfCancellationRequested();if(!s.SupportsBackPanel||!s.BackEnabled)throw new InvalidDataException("Die Rückwand ist nur für aktivierte Leuchtkästen verfügbar.");
        if(s.BackMountingHoles.Count>0)return BackMounting.Generate(s,preview,token);
        var m=new Mesh();double w=s.Width-2*(s.FrameWidth+s.BackClearance),h=s.Height-2*(s.FrameWidth+s.BackClearance),top=s.BackThickness+s.BackInsertionDepth;
        // Angles contain every rectangle corner, so the exact envelope never becomes a chamfer.
        int n=Math.Clamp((int)Math.Ceiling(Math.PI*Math.Max(s.Width,s.Height)/s.Resolution/4)*4,32,preview?192:2400);
        var corners=Corners(w,h).ToArray();
        var angles=Enumerable.Range(0,n).Select(i=>i*Math.Tau/n).Where(a=>!corners.Any(c=>Math.Abs(a-c)<1e-6)).Concat(corners).Order().ToArray();n=angles.Length;
        int[] outerLow=Ring(s.Width,s.Height,0),outerHigh=Ring(s.Width,s.Height,s.BackThickness),lipLow=Ring(w,h,s.BackThickness),lipHigh=Ring(w,h,top),innerHigh=Ring(w-2*s.BackRimWidth,h-2*s.BackRimWidth,top),innerLow=Ring(w-2*s.BackRimWidth,h-2*s.BackRimWidth,s.BackThickness);
        Join(outerLow,outerHigh);Join(outerHigh,lipLow);Join(lipLow,lipHigh);Join(lipHigh,innerHigh);Join(innerHigh,innerLow);
        if(s.BackHoleDiameter>0)
        {
            var lower=new int[n];var upper=new int[n];for(int i=0;i<n;i++){double gap=Math.Max((angles[i]-angles[(i+n-1)%n]+Math.Tau)%Math.Tau,(angles[(i+1)%n]-angles[i]+Math.Tau)%Math.Tau);double r=s.BackHoleDiameter/(2*Math.Cos(gap/2));lower[i]=Vertex(r*Math.Cos(angles[i]),r*Math.Sin(angles[i])+s.Height/2,0);upper[i]=Vertex(r*Math.Cos(angles[i]),r*Math.Sin(angles[i])+s.Height/2,s.BackThickness);}Join(innerLow,upper);Join(upper,lower);Join(lower,outerLow);
        }
        else
        {
            int lower=Vertex(0,s.Height/2,0),upper=Vertex(0,s.Height/2,s.BackThickness);for(int i=0;i<n;i++){int j=(i+1)%n;m.Face(outerLow[j],outerLow[i],lower);m.Face(innerLow[i],innerLow[j],upper);}
        }
        m.ActualResolution=Enumerable.Range(0,n).Max(i=>(double)Vector3.Distance(m.Vertices[outerLow[i]],m.Vertices[outerLow[(i+1)%n]]));m.OrientOutward();return m;
        static IEnumerable<double> Corners(double width,double height){double a=Math.Atan2(height,width);return [a,Math.PI-a,Math.PI+a,Math.Tau-a];}
        int Vertex(double x,double y,double z){token.ThrowIfCancellationRequested();int id=m.Vertices.Count;m.Vertices.Add(new((float)x,(float)y,(float)z));m.TextureCoordinates.Add(new(.5f,.5f));m.PhotoVertices.Add(false);return id;}
        int[] Ring(double width,double height,double z){var ids=new int[n];for(int i=0;i<n;i++){double c=Math.Cos(angles[i]),a=Math.Sin(angles[i]),r=Math.Min(w/(2*Math.Max(1e-15,Math.Abs(c))),h/(2*Math.Max(1e-15,Math.Abs(a))));ids[i]=Vertex(r*c*width/w,s.Height/2+r*a*height/h,z);}return ids;}
        void Join(int[] a,int[] b){for(int i=0;i<n;i++){int j=(i+1)%n;m.Quad(a[i],a[j],b[j],b[i]);}}
    }
    public static Mesh Assemble(Mesh body,Mesh back,ModelSettings s,CancellationToken token=default)=>MeshAssembly.Combine(body,Vector3.Zero,back,new(0,0,(float)(-s.BoxDepth-s.BackThickness)),token);
}
public static class MeshAssembly
{
    public static Mesh Combine(Mesh a,Vector3 shiftA,Mesh b,Vector3 shiftB,CancellationToken token=default)
    {
        var result=new Mesh{ActualResolution=Math.Max(a.ActualResolution,b.ActualResolution),WrapTextureU=a.WrapTextureU,IsAssemblyPreview=true};Append(a,shiftA);Append(b,shiftB);return result;
        void Append(Mesh source,Vector3 shift){int offset=result.Vertices.Count;for(int i=0;i<source.Vertices.Count;i++){if((i&4095)==0)token.ThrowIfCancellationRequested();result.Vertices.Add(source.Vertices[i]+shift);result.TextureCoordinates.Add(source.TextureCoordinates[i]);result.PhotoVertices.Add(source.PhotoVertices[i]);}foreach(var t in source.Triangles)result.Face(t.A+offset,t.B+offset,t.C+offset);}
    }
}
