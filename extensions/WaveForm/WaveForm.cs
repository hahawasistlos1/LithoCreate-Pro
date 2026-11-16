using System.Numerics;
using LithoCreate.Core;

namespace LithoCreate.Extensions;
public sealed class WaveForm:IFormPlugin
{
    public string Name=>"90°-Bogen · Beispielmodul";
    public Mesh Generate(GrayImage photo,ModelSettings parameters,CancellationToken token)
    {
        var s=parameters.Clone();s.Shape=Shape.Flat;s.StandEnabled=false;s.HangingTabsEnabled=false;s.BackEnabled=false;s.SocketEnabled=false;s.Part=ModelPart.Lithophane;s.PrintOrientation=PrintOrientation.Upright;s.TurnOnBed90=false;s.Angles=default;
        s.Shape=Shape.Curved;s.Radius=s.Width/(Math.PI/2);
        // Keep this example below the worker's 300,000-triangle output limit,
        // including when the current project requests a very fine raster.
        s.Resolution=Math.Max(s.Resolution,Math.Sqrt(s.Width*s.Height/50000));
        var mesh=MeshGenerator.Generate(photo,s,false,token);
        PrintPlacement.Apply(mesh,parameters.PrintOrientation,parameters.TurnOnBed90,token,parameters.Angles);return mesh;
    }
}
