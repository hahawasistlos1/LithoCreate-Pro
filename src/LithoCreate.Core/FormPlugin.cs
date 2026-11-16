namespace LithoCreate.Core;
/// <summary>Version 1 contract. Internal coordinates are Y-up, lengths in mm; return an individual closed mesh.</summary>
public interface IFormPlugin
{
    string Name {get;}
    Mesh Generate(GrayImage photo,ModelSettings parameters,CancellationToken cancellationToken);
}
