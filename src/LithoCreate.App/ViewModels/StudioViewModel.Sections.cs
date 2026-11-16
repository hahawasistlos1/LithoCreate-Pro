using System.Windows.Input;
using System.Windows.Threading;
using LithoCreate.Core;
using LithoCreate.App.Services;

namespace LithoCreate.App.ViewModels;
public sealed partial class StudioViewModel
{
    readonly DispatcherTimer sectionDebounce=new(){Interval=TimeSpan.FromMilliseconds(140)};
    CancellationTokenSource? sectionCts;
    Mesh? inspectionMesh,indexMesh;
    CrossSectionIndex? sectionIndex;
    CrossSection? currentSection;
    bool sectionMode,sectionWorking,buildingInspection,sectionInputError;
    int currentLayer=1,sectionRevision;
    double sliceLayerHeight=.12,sectionBottom,sectionHeight;
    string sectionInfo="Eine Schicht auswählen.",inspectionSummary="Schnelles Vorschauraster";
    public event Action<CrossSection?>? SectionReady;
    public event Action? SectionResetRequested;
    public CrossSection? CurrentSection=>currentSection;
    public bool IsSectionMode {get=>sectionMode;set{if(value&&IsAssemblyPart||sectionMode==value)return;if(value)IsPrintSetMode=false;sectionMode=value;Notify();Notify(nameof(IsSurfaceMode));Notify(nameof(ShowModelOptions));Notify(nameof(PreviewTitle));Notify(nameof(NavigationHint));CommandManager.InvalidateRequerySuggested();if(value)QueueSection();else{sectionDebounce.Stop();sectionCts?.Cancel();}}}
    public bool IsSurfaceMode=>!sectionMode&&!IsPrintSetMode;
    public bool ShowModelOptions=>IsSettingsPanel&&!IsSectionMode&&!IsPrintSetMode;
    public string PreviewTitle=>IsPrintSetMode?"DRUCKSATZ · GETRENNTE OBJEKTE":IsAssemblyPart?"MONTAGE · TEILE EINZELN DRUCKEN":IsSectionMode?"DRUCKSCHICHT · GEOMETRIE":"LIVE · 3D-VORSCHAU";
    public string NavigationHint=>IsPrintSetMode?"Bettübersicht: Hüllrechtecke beider Teile in Exportauflösung":IsSectionMode?"Ziehen: verschieben · Mausrad: zoom · Doppelklick: einpassen":"Linke Maus: drehen · Rechte Maus: verschieben · Mausrad: zoom";
    public string SectionInfo {get=>sectionInfo;private set{sectionInfo=value;Notify();}}
    public string InspectionSummary {get=>inspectionSummary;private set{inspectionSummary=value;Notify();}}
    public bool HasFullInspection=>inspectionMesh!=null;
    public bool HasSectionMesh=>sectionHeight>0&&PreviewMesh!=null;
    public bool SectionWorking {get=>sectionWorking;private set{sectionWorking=value;Notify();}}
    public double SliceLayerHeight {get=>sliceLayerHeight;set{if(sliceLayerHeight.Equals(value))return;sliceLayerHeight=value;Notify();Notify(nameof(SliceLayerError));UpdateLayerRange();QueueSection();}}
    public string SliceLayerError=>sectionInputError?"Bitte Schichtnummer und Schichthöhe als Zahlen eingeben.":ValidLayerHeight?"":"Schichthöhe: 0,01–1,00 mm eingeben.";
    bool ValidLayerHeight=>double.IsFinite(sliceLayerHeight)&&sliceLayerHeight is >= .01 and <= 1;
    public int TotalLayers=>ValidLayerHeight?Math.Max(1,(int)Math.Ceiling(sectionHeight/sliceLayerHeight)):1;
    public int CurrentLayer {get=>currentLayer;set{int layer=Math.Clamp(value,1,TotalLayers);if(layer==currentLayer)return;currentLayer=layer;Notify();Notify(nameof(LayerLabel));QueueSection();}}
    public string LayerLabel=>$"Schicht {CurrentLayer:N0} / {TotalLayers:N0}";
    public ICommand SetViewMode {get;private set;}=null!;
    public ICommand InspectFullQuality {get;private set;}=null!;
    public ICommand UsePreviewQuality {get;private set;}=null!;
    public ICommand PreviousLayer {get;private set;}=null!;
    public ICommand NextLayer {get;private set;}=null!;
    public ICommand ResetSectionView {get;private set;}=null!;
    void InitializeSections()
    {
        SetViewMode=new RelayCommand(p=>{if(p?.ToString()=="section"&&IsAssemblyPart)return;IsPrintSetMode=false;IsSectionMode=p?.ToString()=="section";},()=>!Busy);
        InspectFullQuality=new RelayCommand(async _=>await BuildInspection(),()=>!Busy&&!InputError&&!IsAssemblyPart&&PreviewMesh!=null);
        UsePreviewQuality=new RelayCommand(_=>{inspectionMesh=null;sectionIndex=null;indexMesh=null;Notify(nameof(HasFullInspection));InspectionSummary="Schnelles Vorschauraster";SetSectionBounds(PreviewMesh);QueueSection();},()=>!Busy&&HasFullInspection);
        PreviousLayer=new RelayCommand(_=>CurrentLayer--,()=>CurrentLayer>1);
        NextLayer=new RelayCommand(_=>CurrentLayer++,()=>CurrentLayer<TotalLayers);
        ResetSectionView=new RelayCommand(_=>SectionResetRequested?.Invoke());
        sectionDebounce.Tick+=async(_,_)=>{sectionDebounce.Stop();await UpdateSection();};
    }
    void UpdateLayerRange()
    {
        currentLayer=Math.Clamp(currentLayer,1,TotalLayers);Notify(nameof(TotalLayers));Notify(nameof(CurrentLayer));Notify(nameof(LayerLabel));Notify(nameof(HasSectionMesh));CommandManager.InvalidateRequerySuggested();
    }
    void SetSectionBounds(Mesh? mesh)
    {
        if(mesh==null){sectionBottom=sectionHeight=0;}else{var b=mesh.Bounds();sectionBottom=b.Min.Y;sectionHeight=b.Max.Y-b.Min.Y;}
        UpdateLayerRange();
    }
    void InvalidateInspection()
    {
        InvalidatePlacement();InvalidatePrintSet();
        sectionRevision++;sectionCts?.Cancel();sectionDebounce.Stop();inspectionMesh=null;sectionIndex=null;indexMesh=null;
        if(buildingInspection)exportCts?.Cancel();
        currentSection=null;SectionReady?.Invoke(null);Notify(nameof(CurrentSection));Notify(nameof(HasFullInspection));InspectionSummary="Schnelles Vorschauraster";
        SectionInfo="Schicht wird nach der Modellberechnung aktualisiert.";SetSectionBounds(null);
    }
    void QueueSection()
    {
        sectionCts?.Cancel();sectionDebounce.Stop();if(IsSectionMode)sectionDebounce.Start();
    }
    public void SetSectionInputError(bool value)
    {
        if(sectionInputError==value)return;sectionInputError=value;Notify(nameof(SliceLayerError));QueueSection();
        if(value){currentSection=null;SectionReady?.Invoke(null);Notify(nameof(CurrentSection));SectionInfo=SliceLayerError;CommandManager.InvalidateRequerySuggested();}
    }
    public async Task UpdateSection()
    {
        sectionDebounce.Stop();
        sectionCts?.Cancel();var mesh=inspectionMesh??PreviewMesh;
        if(IsAssemblyPart||mesh==null||!ValidLayerHeight||sectionInputError||sectionHeight<=0){currentSection=null;SectionReady?.Invoke(null);Notify(nameof(CurrentSection));SectionInfo=IsAssemblyPart?"Für Druckschichten ein einzelnes Teil wählen.":sectionInputError||!ValidLayerHeight?SliceLayerError:"Vorschaumodell wird vorbereitet …";return;}
        var cts=new CancellationTokenSource();sectionCts=cts;SectionWorking=true;int revision=sectionRevision;
        double height=((currentLayer-1)*sliceLayerHeight+Math.Min(sectionHeight,currentLayer*sliceLayerHeight))/2;double y=sectionBottom+height;
        var existing=ReferenceEquals(indexMesh,mesh)?sectionIndex:null;
        try
        {
            var result=await Task.Run(()=>{var index=existing??new CrossSectionIndex(mesh,cts.Token);return(Index:index,Section:index.Slice(y,cts.Token));},cts.Token);
            if(cts.IsCancellationRequested||revision!=sectionRevision)return;
            sectionIndex=result.Index;indexMesh=mesh;currentSection=result.Section;
            string warning=currentSection.IsRegular?"":"\nEckkontakt/offene Kontur: benachbarte Schicht prüfen.";
            SectionInfo=$"Z = {height:N3} mm · {LayerLabel}\n{currentSection.Area:N2} mm² Material · {currentSection.ClosedContours} Konturen\nRaster {mesh.ActualResolution:N3} mm · {(HasFullInspection?"Exportauflösung":"Vorschaumesh")}{warning}";
            Notify(nameof(CurrentSection));SectionReady?.Invoke(currentSection);CommandManager.InvalidateRequerySuggested();
        }
        catch(OperationCanceledException){ }
        catch(Exception ex){currentSection=null;SectionReady?.Invoke(null);Notify(nameof(CurrentSection));SectionInfo="Schicht konnte nicht berechnet werden: "+ex.Message;}
        finally{if(ReferenceEquals(sectionCts,cts)){sectionCts=null;SectionWorking=false;}cts.Dispose();}
    }
    async Task BuildInspection()
    {
        if(Busy||InputError||IsAssemblyPart||PreviewMesh==null)return;
        InvalidatePrintSet();Busy=true;buildingInspection=true;debounce.Stop();previewCts?.Cancel();exportCts=new();var token=exportCts.Token;int revision=sectionRevision;
        var settings=Model.Clone();var edits=Edits.Clone();var bytes=sourceBytes;Progress=0;
        try
        {
            Status="Prüfmodell in Exportauflösung wird berechnet und geometrisch geprüft …";
            var progress=new Progress<double>(p=>Progress=p*90);
            var result=await Task.Run(()=>ModelComputation.BuildExportMesh(bytes,settings,edits,token,progress),token);
            if(token.IsCancellationRequested||revision!=sectionRevision||InputError)return;
            inspectionMesh=result.Mesh;sectionIndex=null;indexMesh=null;SetSectionBounds(inspectionMesh);Notify(nameof(HasFullInspection));
            InspectionSummary=$"Exportauflösung · {result.Mesh.ActualResolution:N3} mm · {result.Report.Triangles:N0} Dreiecke";
            Progress=100;IsSectionMode=true;QueueSection();Status="Prüfmodell bereit. Die Schichtansicht verwendet jetzt die Geometrie in Exportauflösung.";
        }
        catch(OperationCanceledException){Status="Prüfmodell-Berechnung abgebrochen.";}
        catch(Exception ex){Status="Prüfmodell konnte nicht berechnet werden: "+ex.Message;}
        finally{Busy=false;buildingInspection=false;exportCts.Dispose();exportCts=null;}
    }
    void StopSections(){sectionDebounce.Stop();sectionCts?.Cancel();}
}
