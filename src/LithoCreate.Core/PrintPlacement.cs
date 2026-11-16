using System.Numerics;
using System.Text.Json.Serialization;

namespace LithoCreate.Core;
public enum PrintOrientation { Upright, FaceUp, FaceDown, RightEdgeDown, LeftEdgeDown, UpsideDown }
public sealed record PlacementChoice(PrintOrientation Orientation,string Name)
{
    public override string ToString()=>Name;
}
public readonly record struct PrintAngles(double X,double Y,double Z)
{
    [JsonIgnore] public bool IsZero=>X==0&&Y==0&&Z==0;
    public void Validate(){if(!new[]{X,Y,Z}.All(v=>double.IsFinite(v)&&v>=-180&&v<=180))throw new InvalidDataException("Druckwinkel: endliche Werte von −180° bis +180° eingeben.");}
}
public readonly record struct PlacementFit(PrintOrientation Orientation,bool TurnOnBed90,Vector3 PrintSize,PrintAngles Angles=default);
public static class PrintPlacement
{
    public static readonly PlacementChoice[] Choices=[
        new(PrintOrientation.Upright,"Aufrecht"),new(PrintOrientation.FaceUp,"Flach · Motiv nach oben"),new(PrintOrientation.FaceDown,"Flach · Motiv nach unten"),
        new(PrintOrientation.RightEdgeDown,"Rechte Kante unten"),new(PrintOrientation.LeftEdgeDown,"Linke Kante unten"),new(PrintOrientation.UpsideDown,"Kopfstehend")];
    public static Vector3 Transform(Vector3 p,PrintOrientation orientation,bool turnOnBed90)
    {
        p=orientation switch
        {
            PrintOrientation.Upright=>p,PrintOrientation.FaceUp=>new(p.X,p.Z,-p.Y),PrintOrientation.FaceDown=>new(p.X,-p.Z,p.Y),
            PrintOrientation.RightEdgeDown=>new(p.Y,-p.X,p.Z),PrintOrientation.LeftEdgeDown=>new(-p.Y,p.X,p.Z),PrintOrientation.UpsideDown=>new(-p.X,-p.Y,p.Z),
            _=>throw new InvalidDataException("Unbekannte Druckausrichtung.")
        };
        return turnOnBed90?new(p.Z,p.Y,-p.X):p;
    }
    public static void Apply(Mesh mesh,PrintOrientation orientation,bool turnOnBed90,CancellationToken token=default,PrintAngles angles=default)
    {
        token.ThrowIfCancellationRequested();angles.Validate();if(!Enum.IsDefined(orientation))throw new InvalidDataException("Unbekannte Druckausrichtung.");
        if(angles.IsZero)
        {
            if(orientation==PrintOrientation.Upright&&!turnOnBed90)return;
            for(int i=0;i<mesh.Vertices.Count;i++){if((i&4095)==0)token.ThrowIfCancellationRequested();mesh.Vertices[i]=Transform(mesh.Vertices[i],orientation,turnOnBed90);}return;
        }
        var bounds=mesh.Bounds();var rotation=new FineRotation(angles,Transform((bounds.Min+bounds.Max)/2,orientation,false));
        for(int i=0;i<mesh.Vertices.Count;i++){if((i&4095)==0)token.ThrowIfCancellationRequested();var p=rotation.Apply(Transform(mesh.Vertices[i],orientation,false));mesh.Vertices[i]=turnOnBed90?new(p.Z,p.Y,-p.X):p;}
    }
    /// <summary>Rotate about a Y-up pivot in printer axes X, then Y, then Z.</summary>
    public static Vector3 RotateInPrintAxes(Vector3 point,PrintAngles angles,Vector3 pivot=default){angles.Validate();return angles.IsZero?point:new FineRotation(angles,pivot).Apply(point);}
    readonly struct FineRotation
    {
        readonly double sx,cx,sy,cy,sz,cz;readonly Vector3 center;
        public FineRotation(PrintAngles a,Vector3 pivot){(sx,cx)=Math.SinCos(a.X*Math.PI/180);(sy,cy)=Math.SinCos(a.Y*Math.PI/180);(sz,cz)=Math.SinCos(a.Z*Math.PI/180);center=pivot;}
        public Vector3 Apply(Vector3 p)
        {
            double x=(double)p.X-center.X,y=-((double)p.Z-center.Z),z=(double)p.Y-center.Y;
            (y,z)=(cx*y-sx*z,sx*y+cx*z);(x,z)=(cy*x+sy*z,-sy*x+cy*z);(x,y)=(cz*x-sz*y,sz*x+cz*y);
            return new((float)(x+center.X),(float)(z+center.Y),(float)(-y+center.Z));
        }
    }
    public static bool Fits(Vector3 printSize,PrinterProfile printer)
    {
        const double epsilon=.0001;
        return float.IsFinite(printSize.X)&&float.IsFinite(printSize.Y)&&float.IsFinite(printSize.Z)&&printSize.X>=0&&printSize.Y>=0&&printSize.Z>=0&&printSize.X<=printer.BedX+epsilon&&printSize.Y<=printer.BedY+epsilon&&printSize.Z<=printer.BuildZ+epsilon;
    }
    /// <summary>Retains a fitting current fine rotation; otherwise searches twelve poses with zero fine angles.</summary>
    public static PlacementFit? Find(Mesh canonical,PrinterProfile printer,PrintOrientation current=PrintOrientation.Upright,bool currentTurn=false,CancellationToken token=default,PrintAngles currentAngles=default)
    {
        printer.Validate();currentAngles.Validate();token.ThrowIfCancellationRequested();if(!Enum.IsDefined(current))throw new InvalidDataException("Unbekannte Druckausrichtung.");
        if(canonical.Vertices.Count==0||canonical.Triangles.Count==0)throw new InvalidDataException("Ein leeres Modell kann nicht ausgerichtet werden.");
        var bounds=canonical.Bounds();var corners=new List<Vector3>();
        foreach(float x in new[]{bounds.Min.X,bounds.Max.X})foreach(float y in new[]{bounds.Min.Y,bounds.Max.Y})foreach(float z in new[]{bounds.Min.Z,bounds.Max.Z})corners.Add(new(x,y,z));
        var first=Try(current,currentTurn,currentAngles);if(first!=null)return first;
        foreach(var choice in Choices)foreach(bool turn in new[]{false,true}){token.ThrowIfCancellationRequested();var fit=Try(choice.Orientation,turn);if(fit!=null)return fit;}
        return null;
        PlacementFit? Try(PrintOrientation orientation,bool turn,PrintAngles fine=default)
        {
            var min=new Vector3(float.MaxValue);var max=new Vector3(float.MinValue);
            var rotation=new FineRotation(fine,Transform((bounds.Min+bounds.Max)/2,orientation,false));int count=0;
            foreach(var point in fine.IsZero?corners:canonical.Vertices){if((count++&4095)==0)token.ThrowIfCancellationRequested();var p=Transform(point,orientation,fine.IsZero&&turn);if(!fine.IsZero){p=rotation.Apply(p);if(turn)p=new(p.Z,p.Y,-p.X);}min=Vector3.Min(min,p);max=Vector3.Max(max,p);}
            var size=max-min;var printSize=new Vector3(size.X,size.Z,size.Y);
            return Fits(printSize,printer)?new PlacementFit(orientation,turn,printSize,fine):null;
        }
    }
}
