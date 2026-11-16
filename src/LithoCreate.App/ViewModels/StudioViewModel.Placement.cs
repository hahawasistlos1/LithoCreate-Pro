using System.Windows.Input;
using LithoCreate.Core;
using LithoCreate.App.Services;

namespace LithoCreate.App.ViewModels;
public sealed partial class StudioViewModel
{
    bool buildingPlacement,resetForPlacement;
    string placementSearchInfo="",printSizeInfo="Druckmaße werden berechnet …";
    public PlacementChoice[] Placements=>PrintPlacement.Choices;
    public PlacementChoice SelectedPlacement {get=>Placements.First(p=>p.Orientation==Model.EffectiveOrientation);set{if(value==null||value.Orientation==Model.EffectiveOrientation)return;SetPartPlacement(value.Orientation,Model.EffectiveTurnOnBed90);resetForPlacement=true;Notify();Notify(nameof(PlacementSummary));Changed();}}
    public bool TurnOnBed90 {get=>Model.EffectiveTurnOnBed90;set{if(value==Model.EffectiveTurnOnBed90)return;SetPartPlacement(Model.EffectiveOrientation,value);resetForPlacement=true;Notify();Notify(nameof(PlacementSummary));Changed();}}
    void SetPartPlacement(PrintOrientation orientation,bool turn){if(Model.EffectivePart==ModelPart.Socket){Model.SocketPrintOrientation=orientation;Model.SocketTurnOnBed90=turn;}else if(Model.EffectivePart==ModelPart.BackPanel){Model.BackPrintOrientation=orientation;Model.BackTurnOnBed90=turn;}else{Model.PrintOrientation=orientation;Model.TurnOnBed90=turn;}}
    public double AngleX {get=>Model.EffectiveAngles.X;set=>SetAngles(Model.EffectiveAngles with{X=value});}
    public double AngleY {get=>Model.EffectiveAngles.Y;set=>SetAngles(Model.EffectiveAngles with{Y=value});}
    public double AngleZ {get=>Model.EffectiveAngles.Z;set=>SetAngles(Model.EffectiveAngles with{Z=value});}
    void WriteAngles(PrintAngles value){if(Model.EffectivePart==ModelPart.Socket)Model.SocketAngles=value;else if(Model.EffectivePart==ModelPart.BackPanel)Model.BackAngles=value;else Model.Angles=value;}
    void SetAngles(PrintAngles value){value.Validate();if(value==Model.EffectiveAngles)return;WriteAngles(value);resetForPlacement=true;NotifyPlacement();Changed();}
    public ICommand ResetAngles {get;private set;}=null!;
    public string PlacementSummary=>IsAssemblyPart?"Montageansicht · aufrecht":SelectedPlacement.Name+(Model.EffectiveAngles.IsZero?"":$" · X {AngleX:N1}° / Y {AngleY:N1}° / Z {AngleZ:N1}°")+(TurnOnBed90?" · Bett 90°":"");
    public string PlacementSearchInfo {get=>placementSearchInfo;private set{placementSearchInfo=value;Notify();}}
    public string PrintSizeInfo {get=>printSizeInfo;private set{printSizeInfo=value;Notify();}}
    public ICommand FitToPrinter {get;private set;}=null!;
    void InitializePlacement()
    {
        FitToPrinter=new RelayCommand(async _=>await FitPlacement(),()=>!Busy&&!InputError&&!IsAssemblyPart&&PreviewMesh!=null);
        ResetAngles=new RelayCommand(_=>{bool changed=!Model.EffectiveAngles.IsZero;WriteAngles(default);NotifyPlacement();if(changed){resetForPlacement=true;Changed();}},()=>!Busy&&!IsAssemblyPart);
    }
    void NotifyPlacement(){Notify(nameof(SelectedPlacement));Notify(nameof(TurnOnBed90));Notify(nameof(PlacementSummary));Notify(nameof(AngleX));Notify(nameof(AngleY));Notify(nameof(AngleZ));}
    void InvalidatePlacement(){PlacementSearchInfo="";if(buildingPlacement)exportCts?.Cancel();}
    public async Task FitPlacement()
    {
        if(Busy||InputError||IsAssemblyPart||PreviewMesh==null)return;
        InvalidatePrintSet();Busy=true;buildingPlacement=true;debounce.Stop();previewCts?.Cancel();var cts=new CancellationTokenSource();exportCts=cts;int revision=sectionRevision;Progress=0;
        try
        {
            var settings=Model.Clone();settings.Validate();var edits=Edits.Clone();var bytes=sourceBytes;
            var printer=new PrinterProfile{Name=Printer.Name,Process=Printer.Process,BedX=Printer.BedX,BedY=Printer.BedY,BuildZ=Printer.BuildZ,Nozzle=Printer.Nozzle,Layer=Printer.Layer,Material=Printer.Material};printer.Validate();
            var current=settings.EffectiveOrientation;var currentAngles=settings.EffectiveAngles;bool currentTurn=settings.EffectiveTurnOnBed90;if(settings.EffectivePart==ModelPart.Socket){settings.SocketPrintOrientation=PrintOrientation.Upright;settings.SocketTurnOnBed90=false;settings.SocketAngles=default;}else if(settings.EffectivePart==ModelPart.BackPanel){settings.BackPrintOrientation=PrintOrientation.Upright;settings.BackTurnOnBed90=false;settings.BackAngles=default;}else{settings.PrintOrientation=PrintOrientation.Upright;settings.TurnOnBed90=false;settings.Angles=default;}
            Status="Druckausrichtung wird mit der Geometrie in Exportauflösung geprüft …";
            var progress=new Progress<double>(p=>Progress=p*95);
            var result=await Task.Run(()=>{var mesh=ModelComputation.BuildExportMesh(bytes,settings,edits,cts.Token,progress).Mesh;return(Fit:PrintPlacement.Find(mesh,printer,current,currentTurn,cts.Token,currentAngles),Raster:mesh.ActualResolution);},cts.Token);
            if(cts.IsCancellationRequested||revision!=sectionRevision||InputError){Status="Ausrichtungssuche verworfen: Einstellungen wurden geändert.";return;}
            buildingPlacement=false;Progress=100;
            if(result.Fit is PlacementFit fit)
            {
                bool changed=Model.EffectiveOrientation!=fit.Orientation||Model.EffectiveTurnOnBed90!=fit.TurnOnBed90||Model.EffectiveAngles!=fit.Angles;
                if(changed){WriteAngles(fit.Angles);SetPartPlacement(fit.Orientation,fit.TurnOnBed90);resetForPlacement=true;NotifyPlacement();Changed();}
                PlacementSearchInfo=$"{(changed?"Gewählt":"Bereits passend")}: {PlacementSummary}\n{fit.PrintSize.X:N2} × {fit.PrintSize.Y:N2} × {fit.PrintSize.Z:N2} mm · Export-Raster {result.Raster:N3} mm";
                Status=changed?"Passende Druckausrichtung übernommen. Vorschau und Schichten werden aktualisiert.":"Die aktuelle Druckausrichtung passt bereits in das gewählte Druckvolumen.";
            }
            else{PlacementSearchInfo="Weder die aktuelle Lage noch eine der 12 rechtwinkligen Lagen passt in dieses Druckvolumen. Maße verkleinern oder ein anderes Profil wählen.";Status="Keine passende Druckausrichtung gefunden; die bisherige Lage bleibt erhalten.";}
        }
        catch(OperationCanceledException){PlacementSearchInfo="Ausrichtungssuche abgebrochen.";Status=PlacementSearchInfo;}
        catch(Exception ex){PlacementSearchInfo="Ausrichtungssuche fehlgeschlagen: "+ex.Message;Status=PlacementSearchInfo;}
        finally{buildingPlacement=false;Busy=false;if(ReferenceEquals(exportCts,cts))exportCts=null;cts.Dispose();}
    }
}
