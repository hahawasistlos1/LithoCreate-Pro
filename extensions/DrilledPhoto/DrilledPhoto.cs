using LithoCreate.Core;

namespace LithoCreate.Extensions;
public sealed class DrilledPhoto:IFormPlugin
{
    public string Name=>"Fotoplatte mit freien Rundbohrungen";
    public Mesh Generate(GrayImage photo,ModelSettings parameters,CancellationToken token)
    {
        var holes=parameters.BackMountingHoles.Count>0?parameters.BackMountingHoles.Select(h=>h.Clone()).ToArray():new[]{new MountingHole{X=-parameters.Width/4,Y=parameters.Height*.75},new MountingHole{X=parameters.Width/4,Y=parameters.Height*.75}};
        var mesh=BackMounting.GeneratePlate(parameters.Width,parameters.Height,parameters.MaxThickness,holes,token,photo,parameters);PrintPlacement.Apply(mesh,parameters.PrintOrientation,parameters.TurnOnBed90,token,parameters.Angles);return mesh;
    }
}
