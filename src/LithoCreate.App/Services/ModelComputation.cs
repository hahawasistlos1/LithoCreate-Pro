using System.IO;
using LithoCreate.Core;

namespace LithoCreate.App.Services;
public static class ModelComputation
{
    public static PrintSet BuildPrintSet(byte[] bytes,ModelSettings settings,ImageEdits edits,PrinterProfile printer,CancellationToken token,IProgress<double>? progress=null,PrintSetOptions? options=null)
    {
        settings.Validate();edits.Validate();printer.Validate();token.ThrowIfCancellationRequested();
        options=(options??new()).Clone();options.Validate();
        bool socket=settings.SupportsSphereOpenings&&settings.SocketEnabled,back=settings.SupportsBackPanel&&settings.BackEnabled;
        if(!socket&&!back)throw new InvalidDataException("Für einen Drucksatz einen Stecksockel oder eine Rückwand aktivieren.");
        var body=settings.Clone();body.Part=ModelPart.Lithophane;
        var accessory=settings.Clone();accessory.Part=socket?ModelPart.Socket:ModelPart.BackPanel;
        var a=BuildExportMesh(bytes,body,edits,token,new PartProgress(progress,0,.5));
        var b=BuildExportMesh([],accessory,edits,token,new PartProgress(progress,.5,.25));
        progress?.Report(.8);var result=PrintSet.Create([new(back?"Leuchtkasten":"Lithophanie",a.Mesh),new(socket?"Stecksockel":"Rückwand",b.Mesh)],printer,token,options);progress?.Report(1);return result;
    }
    sealed class PartProgress(IProgress<double>? target,double start,double scale):IProgress<double>{public void Report(double value)=>target?.Report(start+value*scale);}
    public static (Mesh Mesh,MeshReport Report) BuildExportMesh(byte[] bytes,ModelSettings settings,ImageEdits edits,CancellationToken token,IProgress<double>? progress=null)
    {
        settings.Validate();edits.Validate();token.ThrowIfCancellationRequested();
        if(settings.EffectivePart==ModelPart.Assembly)throw new InvalidDataException("Für die Druckprüfung ein einzelnes Teil wählen: Körper oder Zubehör.");
        if(settings.EffectivePart is ModelPart.Socket or ModelPart.BackPanel){var socket=MeshGenerator.Generate(new(1,1,[255]),settings,false,token,progress);var socketReport=socket.Validate(token);if(!socketReport.IsValid)throw new InvalidDataException("Die Zubehörgeometrie hat die Prüfung nicht bestanden.");return(socket,socketReport);}
        var picture=ProcessForExport(bytes,settings,edits,token);
        var mesh=MeshGenerator.Generate(picture.Gray,settings,false,token,progress);var report=mesh.Validate(token);
        if(!report.IsValid)throw new InvalidDataException($"Geometrieprüfung fehlgeschlagen: {report.OpenOrNonManifoldEdges} Kanten, {report.InconsistentEdges} Orientierungen, {report.DegenerateFaces} Flächen.");
        return(mesh,report);
    }

    internal static ProcessedImage ProcessForExport(byte[] bytes, ModelSettings settings, ImageEdits edits, CancellationToken token)
    {
        int limit = Math.Clamp((int)Math.Sqrt(settings.MemoryMb * 1024d * 1024 / 32), 1000, 4096);
        return ImageProcessor.Process(ImageProcessor.Decode(bytes, limit, edits.AutoOrient), edits, token);
    }
}
