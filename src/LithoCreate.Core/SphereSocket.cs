using System.Numerics;

namespace LithoCreate.Core;
public enum ModelPart { Lithophane, Socket, Assembly, BackPanel }
public sealed record PartChoice(ModelPart Part,string Name)
{
    public override string ToString()=>Name;
}
public static class SphereSocket
{
    public static readonly PartChoice[] Parts=[new(ModelPart.Lithophane,"Lithophanie"),new(ModelPart.Socket,"Stecksockel"),new(ModelPart.Assembly,"Montageansicht")];
    public static (double PegDiameter,double MinimumFootDiameter,double MaximumInsertion) Dimensions(ModelSettings s)
    {
        double inner=s.Diameter/2-s.MaxThickness,maximum=s.Diameter/2+(s.Shape==Shape.Moon?.24:0);
        double openingRadius=s.SphereOpeningDiameter/(2*Math.Cos(Math.PI/8));
        return(s.SphereOpeningDiameter-2*s.SocketClearance,2*Math.Sqrt(Math.Max(0,maximum*maximum-inner*inner+openingRadius*openingRadius))+2,s.SphereCollarHeight+Math.Sqrt(Math.Max(0,inner*inner-openingRadius*openingRadius)));
    }
    public static void ValidateSettings(ModelSettings s)
    {
        if(s.SphereOpeningDiameter<=0)throw new InvalidDataException("Für den Stecksockel zuerst eine untere Kugelöffnung einstellen.");
        var dimensions=Dimensions(s);
        if(s.SocketClearance is < .1 or > 1.5)throw new InvalidDataException("Radiales Passspiel: 0,1–1,5 mm.");
        if(s.SocketFootDiameter is < 10 or > 1000||s.SocketFootDiameter<dimensions.MinimumFootDiameter)
            throw new InvalidDataException($"Sockelfuß: mindestens {dimensions.MinimumFootDiameter:N2} mm Durchmesser für eine vollständige Kragenauflage, höchstens 1000 mm.");
        if(s.SocketPlateHeight is < 2 or > 30||s.SocketInsertionHeight is < 1 or > 20||s.SocketInsertionHeight>dimensions.MaximumInsertion)
            throw new InvalidDataException($"Sockelplatte: 2–30 mm. Einsteckhöhe: 1–{Math.Min(20,dimensions.MaximumInsertion):N2} mm.");
        double holeRadius=s.SocketHoleDiameter/(2*Math.Cos(Math.PI/32));
        if(s.SocketHoleDiameter<2||dimensions.PegDiameter/2-holeRadius<1.2)
            throw new InvalidDataException("Kabeldurchführung: mindestens 2 mm; am Einsteckring müssen mindestens 1,2 mm Material bleiben. Durchführung verkleinern oder Kugelöffnung vergrößern.");
    }
    public static Mesh Generate(ModelSettings s,bool preview,CancellationToken token=default)
    {
        s.Validate();token.ThrowIfCancellationRequested();
        if(!s.SupportsSphereOpenings||!s.SocketEnabled)throw new InvalidDataException("Der Stecksockel ist nur für aktivierte Kugel-/Mondsockel verfügbar.");
        int segments=Math.Clamp((int)Math.Ceiling(Math.PI*s.SocketFootDiameter/s.Resolution/4)*4,32,preview?192:2400);
        double foot=s.SocketFootDiameter/2,peg=Dimensions(s).PegDiameter/2,hole=s.SocketHoleDiameter/(2*Math.Cos(Math.PI/segments)),h=s.SocketPlateHeight,top=h+s.SocketInsertionHeight;
        (double Radius,double Y)[] profile=[(foot,0),(foot,h),(peg,h),(peg,top),(hole,top),(hole,0)];
        var mesh=new Mesh{ActualResolution=Math.PI*s.SocketFootDiameter/segments};
        foreach(var point in profile)for(int i=0;i<segments;i++)
        {
            token.ThrowIfCancellationRequested();double angle=i*Math.Tau/segments;mesh.Vertices.Add(new((float)(point.Radius*Math.Cos(angle)),(float)point.Y,(float)(point.Radius*Math.Sin(angle))));mesh.TextureCoordinates.Add(new(.5f,.5f));mesh.PhotoVertices.Add(false);
        }
        for(int j=0;j<profile.Length;j++)for(int i=0;i<segments;i++){int next=(i+1)%segments,row=(j+1)%profile.Length;mesh.Quad(j*segments+i,j*segments+next,row*segments+next,row*segments+i);}
        mesh.OrientOutward();return mesh;
    }
    public static Mesh Assemble(Mesh body,Mesh socket,double plateHeight,CancellationToken token=default)
    {
        return MeshAssembly.Combine(body,new(0,(float)(plateHeight-body.Bounds().Min.Y),0),socket,Vector3.Zero,token);
    }
}
