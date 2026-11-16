using System.Numerics;

namespace LithoCreate.Core;
public enum WorkshopKind { LedChannel,SocketRing,Diffuser,Support,MountingPlate }
public sealed record WorkshopPreset(string Name,WorkshopKind Kind,double A,double B,double Thickness)
{
    public override string ToString()=>Name;
}
public static class WorkshopParts
{
    public static readonly WorkshopPreset[] Presets=[new("LED-Streifen 8 mm · U-Halter",WorkshopKind.LedChannel,8.5,50,2),new("LED-Streifen 10 mm · U-Halter",WorkshopKind.LedChannel,10.5,50,2),new("E14 · Durchlass nach Maß",WorkshopKind.SocketRing,28.5,50,3),new("E27 · Durchlass nach Maß",WorkshopKind.SocketRing,40.5,65,3),new("Diffusorplatte · Probestück",WorkshopKind.Diffuser,100,80,1.2),new("Winkelstütze",WorkshopKind.Support,40,30,3),new("Montageplatte · freie Bohrungen",WorkshopKind.MountingPlate,60,40,3)];
    public static Mesh Generate(WorkshopKind kind,double a,double b,double thickness,CancellationToken token=default,IReadOnlyList<MountingHole>? holes=null)
    {
        if(!Enum.IsDefined(kind)||!new[]{a,b,thickness}.All(double.IsFinite)||a is < 2 or > 250||b is < 2 or > 250||thickness is < .6 or > 10)throw new InvalidDataException("Halterung: Maße 2–250 mm, Wandstärke 0,6–10 mm eingeben.");
        if(kind==WorkshopKind.Diffuser)return MeshGenerator.Generate(new(1,1,[255]),new(){Width=a,Height=b,FrameWidth=0,MinThickness=thickness,MaxThickness=thickness,Resolution=2,PrintOrientation=PrintOrientation.FaceUp},false,token);
        if(kind==WorkshopKind.MountingPlate){var plate=BackMounting.GeneratePlate(a,b,thickness,holes??[new(){X=-a/4,Y=b/2},new(){X=a/4,Y=b/2}],token);PrintPlacement.Apply(plate,PrintOrientation.FaceUp,false,token);return plate;}
        if(kind==WorkshopKind.SocketRing)
        {
            const int n=128;double inner=a/(2*Math.Cos(Math.PI/n)),outer=b/2;if(outer-inner<2)throw new InvalidDataException("Fassungsring: mindestens 2 mm Material um den freien Durchlass lassen.");var mesh=new Mesh();
            foreach(var r in new[]{(R:outer,Y:0d),(R:outer,Y:thickness),(R:inner,Y:thickness),(R:inner,Y:0d)})for(int i=0;i<n;i++){double angle=i*Math.Tau/n;mesh.Vertices.Add(new((float)(r.R*Math.Cos(angle)),(float)r.Y,(float)(r.R*Math.Sin(angle))));}
            for(int j=0;j<4;j++)for(int i=0;i<n;i++){token.ThrowIfCancellationRequested();int k=(i+1)%n,next=(j+1)%4;mesh.Quad(j*n+i,j*n+k,next*n+k,next*n+i);}mesh.OrientOutward();return mesh;
        }
        if(kind==WorkshopKind.Support)
        {
            if(a<2*thickness||b<2*thickness)throw new InvalidDataException("Winkelstütze: Schenkel mindestens doppelt so groß wie die Wandstärke wählen.");
            return Extrude([new(0,0),new((float)a,0),new((float)a,(float)thickness),new((float)thickness,(float)thickness),new((float)thickness,(float)b),new(0,(float)b)],20,token);
        }
        double w=a+2*thickness,h=6+thickness;
        return Extrude([new(0,0),new((float)w,0),new((float)w,(float)h),new((float)(w-thickness),(float)h),new((float)(w-thickness),(float)thickness),new((float)thickness,(float)thickness),new((float)thickness,(float)h),new(0,(float)h)],b,token);
    }
    static Mesh Extrude(Vector2[] polygon,double length,CancellationToken token)
    {
        var mesh=new Mesh();int n=polygon.Length;foreach(double z in new[]{0d,length})foreach(var p in polygon)mesh.Vertices.Add(new(p.X,p.Y,(float)z));var remaining=Enumerable.Range(0,n).ToList();
        while(remaining.Count>3)
        {
            token.ThrowIfCancellationRequested();bool found=false;
            for(int i=0;i<remaining.Count;i++)
            {
                int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];if(Cross(polygon[b]-polygon[a],polygon[c]-polygon[b])<=0)continue;
                if(remaining.Any(k=>k!=a&&k!=b&&k!=c&&Inside(polygon[k],polygon[a],polygon[b],polygon[c])))continue;
                Face(a,b,c);remaining.RemoveAt(i);found=true;break;
            }
            if(!found)throw new InvalidDataException("Halterungsprofil kann nicht trianguliert werden.");
        }
        Face(remaining[0],remaining[1],remaining[2]);for(int i=0;i<n;i++){int j=(i+1)%n;mesh.Quad(i,j,j+n,i+n);}mesh.OrientOutward();return mesh;
        void Face(int a,int b,int c){mesh.Face(a,c,b);mesh.Face(a+n,b+n,c+n);}
        static float Cross(Vector2 a,Vector2 b)=>a.X*b.Y-a.Y*b.X;
        static bool Inside(Vector2 p,Vector2 a,Vector2 b,Vector2 c)=>Cross(b-a,p-a)>=-1e-6&&Cross(c-b,p-b)>=-1e-6&&Cross(a-c,p-c)>=-1e-6;
    }
}
