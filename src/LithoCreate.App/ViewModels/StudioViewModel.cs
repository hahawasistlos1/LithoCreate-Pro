using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using LithoCreate.Core;
using LithoCreate.App.Services;

namespace LithoCreate.App.ViewModels;
public sealed class RelayCommand(Action<object?> execute,Func<bool>? can=null) : ICommand
{
    public bool CanExecute(object? p)=>can?.Invoke()??true;
    public void Execute(object? p)=>execute(p);
    public event EventHandler? CanExecuteChanged { add=>CommandManager.RequerySuggested+=value;remove=>CommandManager.RequerySuggested-=value; }
}
public sealed record RecentProject(string Path,string Name,string Modified,BitmapSource? Thumbnail);
public sealed partial class StudioViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    void Notify([CallerMemberName]string? name=null)=>PropertyChanged?.Invoke(this,new(name));
    readonly string dataDir=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LithoCreate Pro");
    readonly DispatcherTimer debounce=new(){Interval=TimeSpan.FromMilliseconds(400)};
    readonly DispatcherTimer autosave=new(){Interval=TimeSpan.FromSeconds(40)};
    CancellationTokenSource? previewCts,exportCts;
    readonly Stack<ImageEdits> undo=[],redo=[];
    ImageEdits? lastEdits;
    byte[] sourceBytes=[];
    BitmapSource source=ImageProcessor.Demo();
    ProjectData project=new();
    string page="Home",status="Bereit · Alle Daten bleiben auf diesem Computer.",projectPath="";
    int step=1; bool busy,dirty,showOriginal,initialized,compareEnabled;
    int bindingRefreshRevision;
    public int BindingRefreshRevision=>bindingRefreshRevision;
    public bool RefreshingBindings {get;private set;}
    double progress;
    ProcessedImage? processed;
    public Mesh? PreviewMesh {get;private set;}
    public event Action<Mesh,GrayImage>? MeshReady;
    public event Action? PreviewCleared;
    public event Action<string>? CameraRequested;
    public event Action? PngRequested;
    public ProjectData Project=>project;
    public ModelSettings Model=>project.Model;
    public ImageEdits Edits=>project.Edits;
    public double StraightenAngle {get=>Edits.StraightenAngle;set{if(Edits.StraightenAngle.Equals(value))return;Edits.StraightenAngle=value;Notify();Changed();}}
    public PrinterProfile Printer=>project.Printer;
    public ShapeInfo[] Shapes=>Templates.All;
    public IEnumerable<ShapeInfo> ShadeShapes=>Shapes.Where(s=>LampShade.Supports(s.Shape));
    public IEnumerable<ShapeInfo> OtherShapes=>Shapes.Where(s=>!LampShade.Supports(s.Shape));
    public ObservableCollection<RecentProject> Recent {get;}=[];
    public BitmapSource Image=>ShowOriginal?source:processed?.Bitmap??source;
    public BitmapSource Original=>source;
    public BitmapSource ComparisonBefore=>processed?.BeforeBitmap??source;
    public bool CompareEnabled {get=>compareEnabled;set{compareEnabled=value;Notify();Notify(nameof(IsPhotoComparison));}}
    public bool IsPhotoComparison=>CompareEnabled&&!ShowOriginal;
    public string ImageInfo {get;private set;}="Beispielmotiv · Leuchtturm · Eigenes Foto über „Foto laden“";
    public string MeshInfo {get;private set;}="Vorschau wird vorbereitet …";
    public string PrinterWarning {get;private set;}="";
    public string Histogram {get;private set;}="";
    public string Page {get=>page;set{page=value;Notify();Notify(nameof(IsHome));Notify(nameof(IsEditor));Notify(nameof(IsTemplates));Notify(nameof(IsProjects));Notify(nameof(IsSettings));Notify(nameof(IsHelp));}}
    public bool IsHome=>Page=="Home";public bool IsEditor=>Page=="Editor";public bool IsTemplates=>Page=="Templates";public bool IsProjects=>Page=="Projects";public bool IsSettings=>Page=="Settings";public bool IsHelp=>Page=="Help";
    public int Step {get=>step;set{step=Math.Clamp(value,1,5);Notify();Notify(nameof(IsPhoto));Notify(nameof(IsModel));Notify(nameof(IsExport));Notify(nameof(IsSettingsPanel));Notify(nameof(ShowModelOptions));Notify(nameof(StepTitle));Notify(nameof(StepDescription));}}
    public bool IsSettingsPanel=>Step is 2 or 3 or 4;
    public bool IsPhoto=>Step==1;public bool IsModel=>Step!=1;public bool IsExport=>Step==5;
    public string StepTitle=>Step switch{1=>"Dein Foto. Der Anfang von etwas Besonderem.",2=>"Die passende Form für deine Idee.",3=>"Jedes Detail in deiner Hand.",4=>"Licht an. Perspektive wechseln.",_=>"Bereit für deinen 3D-Druck."};
    public string StepDescription=>Step switch{1=>"Foto laden, zuschneiden und die Tonwerte für dein Lithophan optimieren.",2=>"Wähle eine Vorlage. Dein Motiv wird direkt auf der Geometrie berechnet.",3=>"Maße in Millimetern · Dunkle Bildbereiche ergeben mehr Material.",4=>"Drehen, zoomen und die Wirkung mit einer warmen Hinterleuchtung prüfen.",_=>"Der Export berechnet die volle Auflösung und prüft die geschlossene Geometrie."};
    public string Status {get=>status;set{status=value;Notify();}}
    public bool Busy {get=>busy;set{busy=value;Notify();CommandManager.InvalidateRequerySuggested();}}
    public double Progress {get=>progress;set{progress=value;Notify();}}
    public bool Dirty {get=>dirty;set{dirty=value;Notify();Notify(nameof(WindowTitle));}}
    public bool InputError {get;set;}
    public string WindowTitle=>$"{project.Name}{(Dirty?" *":"")} — LithoCreate Pro";
    public bool ShowOriginal {get=>showOriginal;set{showOriginal=value;Notify();Notify(nameof(Image));Notify(nameof(IsPhotoComparison));}}
    public ShapeInfo SelectedShape
    {
        get=>Templates.All.First(s=>s.Shape==Model.Shape);
        set
        {
            if(value==null || value.Shape==Model.Shape) return;
            Model.Shape=value.Shape;
            if(Model.IsShade) LampShade.ApplyTemplate(Model);
            Notify();
            Notify(nameof(Model));
            NotifyParts();
            Changed();
        }
    }
    public ShadeMount SelectedShadeMount
    {
        get=>Model.ShadeMount;
        set
        {
            if(value==Model.ShadeMount) return;
            Model.ShadeMount=value;
            Notify();
            Changed();
        }
    }
    public ShadeMountStyle SelectedShadeMountStyle
    {
        get=>Model.ShadeMountStyle;
        set
        {
            if(value==Model.ShadeMountStyle) return;
            Model.ShadeMountStyle=value;
            Notify();
            Changed();
        }
    }

    public ICommand Navigate {get;} public ICommand GoStep {get;} public ICommand Next {get;} public ICommand Back {get;}
    public ICommand LoadPhoto {get;} public ICommand OpenProject {get;} public ICommand Save {get;} public ICommand SaveAs {get;} public ICommand New {get;} public ICommand SelectTemplate {get;}
    public ICommand Rotate {get;} public ICommand ResetStraighten {get;} public ICommand Mirror {get;} public ICommand ResetImage {get;} public ICommand Undo {get;} public ICommand Redo {get;} public ICommand AutoContrast {get;} public ICommand Crop {get;}
    public ICommand ExportStl {get;} public ICommand Export3Mf {get;} public ICommand ExportPng {get;} public ICommand Cancel {get;} public ICommand Camera {get;}
    public ICommand OpenRecent {get;} public ICommand Duplicate {get;} public ICommand Rename {get;} public ICommand Delete {get;} public ICommand SaveProfile {get;}
    public StudioViewModel()
    {
        Directory.CreateDirectory(dataDir);
        Navigate=new RelayCommand(p=>Page=p?.ToString()??"Home");GoStep=new RelayCommand(p=>{Page="Editor";Step=int.Parse(p!.ToString()!);});Next=new RelayCommand(_=>Step++);Back=new RelayCommand(_=>Step--);
        LoadPhoto=new RelayCommand(_=>ChoosePhoto(),()=>!Busy);OpenProject=new RelayCommand(_=>ChooseProject(),()=>!Busy);Save=new RelayCommand(_=>SaveProject(false),()=>!Busy);SaveAs=new RelayCommand(_=>SaveProject(true),()=>!Busy);
        New=new RelayCommand(_=>NewProject(),()=>!Busy);SelectTemplate=new RelayCommand(p=>{if(p is ShapeInfo shape){SelectedShape=shape;Page="Editor";Step=2;}});
        Rotate=new RelayCommand(_=>Edit(e=>e.Rotation=(e.Rotation+90)%360));ResetStraighten=new RelayCommand(_=>Edit(e=>e.StraightenAngle=0));Mirror=new RelayCommand(_=>Edit(e=>e.Mirror=!e.Mirror));ResetImage=new RelayCommand(_=>{Record();project.Edits=new(){AutoOrient=Edits.AutoOrient};lastEdits=Edits.Clone();Refresh();Changed(false);});
        Undo=new RelayCommand(_=>{if(undo.TryPop(out var e)){redo.Push(Edits.Clone());project.Edits=e;lastEdits=e.Clone();Refresh();Changed(false);}},()=>undo.Count>0);
        Redo=new RelayCommand(_=>{if(redo.TryPop(out var e)){undo.Push(Edits.Clone());project.Edits=e;lastEdits=e.Clone();Refresh();Changed(false);}},()=>redo.Count>0);
        AutoContrast=new RelayCommand(_=>Optimize());Crop=new RelayCommand(p=>ApplyCrop(p?.ToString()??"free"),()=>!Busy&&!InputError);
        ExportStl=new RelayCommand(async _=>await Export("stl"),()=>!Busy&&!InputError&&!IsAssemblyPart&&PreviewMesh!=null);Export3Mf=new RelayCommand(async _=>await Export("3mf"),()=>!Busy&&!InputError&&!IsAssemblyPart&&PreviewMesh!=null);ExportPng=new RelayCommand(_=>PngRequested?.Invoke(),()=>!Busy&&(IsPrintSetMode?HasPrintSet:PreviewMesh!=null&&(!IsSectionMode||CurrentSection!=null)));Cancel=new RelayCommand(_=>{previewCts?.Cancel();exportCts?.Cancel();sectionCts?.Cancel();});Camera=new RelayCommand(p=>CameraRequested?.Invoke(p?.ToString()??"reset"),()=>PreviewMesh!=null);
        OpenRecent=new RelayCommand(p=>{if(p is RecentProject r)OpenPath(r.Path);},()=>!Busy);
        Duplicate=new RelayCommand(p=>{if(p is RecentProject r)DuplicateProject(r);},()=>!Busy);
        Rename=new RelayCommand(p=>{if(p is RecentProject r)RenameProject(r);},()=>!Busy);
        Delete=new RelayCommand(p=>{if(p is RecentProject r&&MessageBox.Show($"Projekt „{r.Name}“ wirklich von der Festplatte löschen?","Projekt löschen",MessageBoxButton.YesNo,MessageBoxImage.Warning)==MessageBoxResult.Yes){try{File.Delete(r.Path);RemoveRecent(r.Path);}catch(Exception ex){Error(ex);}}},()=>!Busy);
        SaveProfile=new RelayCommand(_=>SaveCurrentPrinter(),()=>!Busy&&!InputError&&!printerCatalogFailed);
        InitializeSections();InitializeDimensions();InitializePlacement();InitializeMounting();InitializePrintSet();sourceBytes=ImageProcessor.Png(source);lastEdits=Edits.Clone();
        LoadRecent();InitializePrinters();InitializePresets();
        debounce.Tick+=async(_,_)=>{debounce.Stop();await UpdatePreview();};
        autosave.Tick+=(_,_)=>{if(Dirty&&!Busy)try{ProjectStore.Save(RecoveryPath,project,sourceBytes,processed==null?null:ImageProcessor.Png(processed.Bitmap));}catch(Exception ex){Status="Wiederherstellung konnte nicht gespeichert werden: "+ex.Message;}};
        autosave.Start();initialized=true;debounce.Start();
    }
    string RecoveryPath=>Path.Combine(dataDir,"recovery.lithopro");
    public void Recover()
    {
        if(File.Exists(RecoveryPath)&&MessageBox.Show("Ein automatisch gesichertes Projekt wurde gefunden. Wiederherstellen?","Wiederherstellung",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes){OpenPath(RecoveryPath,false);projectPath="";Dirty=true;}
    }
    public void Changed(bool history=true)
    {
        if(!initialized)return;
        if(!changingPrintLayout)ClearPrintLayoutHistory();
        InvalidateInspection();
        if(history&&lastEdits!=null&&JsonSerializer.Serialize(lastEdits)!=JsonSerializer.Serialize(Edits)){undo.Push(lastEdits);redo.Clear();lastEdits=Edits.Clone();}
        Notify(nameof(PrinterProfileHint));Notify(nameof(ActivePrinterName));Dirty=true;previewCts?.Cancel();debounce.Stop();debounce.Start();
    }
    void Record(){undo.Push(Edits.Clone());redo.Clear();}
    void Edit(Action<ImageEdits> change){Record();change(Edits);lastEdits=Edits.Clone();Refresh();Changed(false);}
    void Refresh()
    {
        RefreshingBindings=true;int revision=++bindingRefreshRevision;
        Dispatcher.CurrentDispatcher.BeginInvoke(()=>{if(revision==bindingRefreshRevision)RefreshingBindings=false;},DispatcherPriority.Loaded);
        InputError=false;NotifyPrinterState();NotifyPrintLayout();Notify(nameof(Project));Notify(nameof(Model));Notify(nameof(Edits));Notify(nameof(StraightenAngle));NotifyParts();NotifyPlacement();NotifyCropOptions();Notify(nameof(Printer));Notify(nameof(SelectedShape));Notify(nameof(SelectedPrinter));Notify(nameof(WindowTitle));Notify(nameof(Image));Notify(nameof(ComparisonBefore));
    }
    public async Task UpdatePreview()
    {
        debounce.Stop();
        if(InputError){ClearPreview();Status="Bitte zuerst die ungültige Eingabe korrigieren.";return;}
        previewCts?.Cancel();var cts=new CancellationTokenSource();previewCts=cts;
        var settings=Model.Clone();var edits=Edits.Clone();var original=source;
        try
        {
            Status="Vorschaumodell wird berechnet …";
            var result=await Task.Run(()=>{var pic=ImageProcessor.Process(original,edits,cts.Token);var mesh=MeshGenerator.Generate(pic.Gray,settings,true,cts.Token);return(pic,mesh);},cts.Token);
            if(cts.IsCancellationRequested)return;
            processed=result.pic;PreviewMesh=result.mesh;Notify(nameof(Image));Notify(nameof(ComparisonBefore));
            int max=processed.Histogram.Max();const string bars="▁▂▃▄▅▆▇█";Histogram=string.Concat(Enumerable.Range(0,48).Select(i=>bars[Math.Clamp((int)(7.0*processed.Histogram.Skip(i*256/48).Take(6).Max()/Math.Max(1,max)),0,7)]));Notify(nameof(Histogram));
            var b=result.mesh.Bounds();var size=b.Max-b.Min;
            MeshInfo=$"{size.X:N1} × {size.Y:N1} × {size.Z:N1} mm\n{result.mesh.Triangles.Count:N0} Dreiecke · Vorschau\nEffektives Raster: {result.mesh.ActualResolution:N2} mm";Notify(nameof(MeshInfo));
            var printSize=new System.Numerics.Vector3(size.X,size.Z,size.Y);PrintSizeInfo=$"{(IsAssemblyPart?"Montagemaße":"Druckmaße")} · B × T × H\n{printSize.X:N2} × {printSize.Y:N2} × {printSize.Z:N2} mm";
            PrinterWarning=IsAssemblyPart?"Montageansicht: Körper und Zubehör einzeln prüfen und exportieren.":PrintPlacement.Fits(printSize,Printer)?"✓ Modell passt in das gewählte Druckvolumen.":"⚠ Modell überschreitet das Druckvolumen in der aktuellen Ausrichtung.";Notify(nameof(PrinterWarning));
            MeshReady?.Invoke(result.mesh,processed.Gray);if(resetForPlacement){resetForPlacement=false;CameraRequested?.Invoke("reset");}SetSectionBounds(result.mesh);QueueSection();CommandManager.InvalidateRequerySuggested();Status="Vorschau aktualisiert · Export wird mit höherer Detailauflösung berechnet.";
        }
        catch(OperationCanceledException) { }
        catch(Exception ex){ClearPreview();processed=null;Notify(nameof(Image));Notify(nameof(ComparisonBefore));Status=ex.Message;}
        finally {if(ReferenceEquals(previewCts,cts))previewCts=null;cts.Dispose();}
    }
    void ClearPreview()
    {
        InvalidateInspection();previewCts?.Cancel();PreviewMesh=null;MeshInfo="Vorschau nicht verfügbar.\nEinstellungen prüfen oder Berechnung abwarten.";PrinterWarning="";PrintSizeInfo="Druckmaße nicht verfügbar.";
        Notify(nameof(MeshInfo));Notify(nameof(PrinterWarning));PreviewCleared?.Invoke();CommandManager.InvalidateRequerySuggested();
    }
    void ChoosePhoto()
    {
        var dialog=new OpenFileDialog{Filter="Bilddateien|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.webp|Alle Dateien|*.*"};if(dialog.ShowDialog()==true)LoadImage(dialog.FileName);
    }
    public void LoadImage(string path)
    {
        try
        {
            if(new FileInfo(path).Length>64*1024*1024)throw new InvalidDataException("Bilddateien dürfen höchstens 64 MB groß sein.");
            byte[] bytes=File.ReadAllBytes(path);var bitmap=ImageProcessor.Decode(bytes);
            ClearPreview();source=bitmap;sourceBytes=bytes;project.ImageName=Path.GetFileName(path);project.Edits=new();undo.Clear();redo.Clear();ClearPrintLayoutHistory();lastEdits=Edits.Clone();processed=null;
            ImageInfo=$"{project.ImageName} · {source.PixelWidth} × {source.PixelHeight} px (Arbeitskopie)";Notify(nameof(ImageInfo));Notify(nameof(Original));Refresh();Page="Editor";Step=1;Changed(false);
        }
        catch(Exception ex){Error(new InvalidDataException("Das Bild konnte nicht geladen werden. WebP benötigt einen lokal installierten WIC-Decoder. "+ex.Message));}
    }
    void ApplyCrop(string aspect)
    {
        if(Busy||InputError)return;
        if(aspect=="free") {ShowOriginal=true;Status="Ausschnitt im Original aufziehen. Bei aktiver Verhältnisbindung bleibt das gewählte Format erhalten. Escape bricht die Auswahl ab.";return;}
        if(aspect=="reset"){Edit(e=>{e.CropX=e.CropY=0;e.CropWidth=e.CropHeight=1;});ShowOriginal=false;return;}
        (double w,double h)=aspect switch{"square"=>(1,1),"model"=>(Model.Width,Model.Height),"3:2"=>(3,2),"4:5"=>(4,5),"16:9"=>(16,9),_=>(Edits.CropAspectWidth,Edits.CropAspectHeight)};
        CropRegion region;try{region=CropSelection.Centered(source.PixelWidth,source.PixelHeight,w,h);}catch(InvalidDataException ex){Status=ex.Message;return;}
        Edit(e=>{e.CropAspectWidth=w;e.CropAspectHeight=h;e.CropAspectLock=true;e.CropX=region.X;e.CropY=region.Y;e.CropWidth=region.Width;e.CropHeight=region.Height;e.Rotation=0;e.StraightenAngle=0;e.Mirror=false;});
        ShowOriginal=false;
    }
    public void SetCrop(double x,double y,double width,double height)
    {
        // Mouse selection uses the original, uncropped view to keep coordinates non-destructive.
        Edit(e=>{e.CropX=x;e.CropY=y;e.CropWidth=width;e.CropHeight=height;e.Rotation=0;e.StraightenAngle=0;e.Mirror=false;});ShowOriginal=false;
    }
    void Optimize()
    {
        try
        {
            var raw=ImageProcessor.Process(source,new ImageEdits());var hist=raw.Histogram;int total=raw.Gray.Pixels.Length,acc=0,lo=0,hi=255;
            for(int i=0;i<256;i++){acc+=hist[i];if(acc>=total*.01){lo=i;break;}}acc=0;
            for(int i=255;i>=0;i--){acc+=hist[i];if(acc>=total*.01){hi=i;break;}}
            if(hi<=lo)return;Edit(e=>{e.BlackPoint=lo/255.0;e.WhitePoint=hi/255.0;e.Contrast=1;e.Brightness=0;});
        }
        catch(Exception ex){Error(ex);}
    }
    public bool ConfirmDiscard()
    {
        if(!Dirty)return true;var result=MessageBox.Show("Änderungen am aktuellen Projekt speichern?","Ungespeicherte Änderungen",MessageBoxButton.YesNoCancel,MessageBoxImage.Question);
        return result==MessageBoxResult.No || result==MessageBoxResult.Yes&&SaveProject(false);
    }
    void NewProject()
    {
        if(!ConfirmDiscard())return;ClearPreview();project=new();projectPath="";source=ImageProcessor.Demo();sourceBytes=ImageProcessor.Png(source);processed=null;undo.Clear();redo.Clear();ClearPrintLayoutHistory();lastEdits=Edits.Clone();ImageInfo="Beispielmotiv · Eigenes Foto über „Foto laden“";Notify(nameof(ImageInfo));Refresh();Page="Editor";Step=1;Dirty=false;debounce.Start();
    }
    public bool SaveProject(bool saveAs)
    {
        if(InputError){Status="Bitte zuerst die ungültige Eingabe korrigieren.";return false;}
        try
        {
            string path=projectPath;if(saveAs||string.IsNullOrEmpty(path)){var dialog=new SaveFileDialog{Filter="LithoCreate-Projekt|*.lithopro",FileName=project.Name};if(dialog.ShowDialog()!=true)return false;path=dialog.FileName;}
            project.Name=Path.GetFileNameWithoutExtension(path);ProjectStore.Save(path,project,sourceBytes,processed==null?null:ImageProcessor.Png(processed.Bitmap));projectPath=path;Dirty=false;Refresh();AddRecent(path);if(File.Exists(RecoveryPath))File.Delete(RecoveryPath);Status="Projekt gespeichert: "+path;return true;
        }
        catch(Exception ex){Error(ex);return false;}
    }
    void ChooseProject(){if(!ConfirmDiscard())return;var dialog=new OpenFileDialog{Filter="LithoCreate-Projekt|*.lithopro"};if(dialog.ShowDialog()==true)OpenPath(dialog.FileName,false);}
    public void OpenPath(string path,bool confirm=true)
    {
        if(confirm&&!ConfirmDiscard())return;
        try{var loaded=ProjectStore.Load(path);var decoded=ImageProcessor.Decode(loaded.Image,autoOrient:loaded.Data.Edits.AutoOrient);ClearPreview();project=loaded.Data;sourceBytes=loaded.Image;source=decoded;processed=null;projectPath=path;undo.Clear();redo.Clear();ClearPrintLayoutHistory();lastEdits=Edits.Clone();Refresh();Dirty=false;ImageInfo=$"{project.ImageName} · {source.PixelWidth} × {source.PixelHeight} px";Notify(nameof(ImageInfo));Page="Editor";Step=3;if(path!=RecoveryPath)AddRecent(path);debounce.Start();}
        catch(Exception ex){Error(ex);}
    }
    async Task Export(string format)
    {
        if(IsAssemblyPart){Status="Zuerst ein einzelnes Druckteil für den Export auswählen.";return;}
        if(InputError){Status="Bitte zuerst die ungültige Eingabe korrigieren.";return;}
        try{Model.Validate();}catch(Exception ex){Error(ex);return;}
        string partSuffix=HasParts?(Model.EffectivePart==ModelPart.Socket?" – Stecksockel":Model.EffectivePart==ModelPart.BackPanel?" – Rückwand":HasBackPanel?" – Leuchtkasten":" – Lithophanie"):"";
        string exportFolder=Path.GetDirectoryName(projectPath)??"";
        var dialog=new SaveFileDialog{InitialDirectory=Directory.Exists(exportFolder)?exportFolder:"",Filter=format=="stl"?"STL-Modell|*.stl":"3MF-Modell|*.3mf",FileName=project.Name+partSuffix};if(dialog.ShowDialog()!=true)return;
        Busy=true;debounce.Stop();previewCts?.Cancel();exportCts=new();var token=exportCts.Token;Progress=0;
        try
        {
            // Snapshot on the UI thread; changes during export affect only the next operation.
            var s=Model.Clone();var e=Edits.Clone();var bytes=sourceBytes;
            var p=new Progress<double>(v=>Progress=v*100);
            Status="Export: Geometrie berechnen, prüfen und Datei erneut einlesen …";
            var report=await Task.Run(()=>{var built=ModelComputation.BuildExportMesh(bytes,s,e,token,new ScaledProgress(p,0,.55));ModelExporter.Export(dialog.FileName,built.Mesh,token,new ScaledProgress(p,.6,.35));return built.Report;},token);
            Progress=100;Status=$"✓ {format.ToUpperInvariant()} gespeichert und erneut geprüft · {report.Triangles:N0} Dreiecke · Raster {report.Resolution:N3} mm · {dialog.FileName}";
        }
        catch(OperationCanceledException){Status="Export abgebrochen. Eine bestehende Zieldatei wurde nicht verändert.";}
        catch(Exception ex){Error(ex);}
        finally{Busy=false;exportCts.Dispose();exportCts=null;}
    }
    void LoadRecent()
    {
        Recent.Clear();try{string file=Path.Combine(dataDir,"recent.json");if(File.Exists(file))foreach(string path in JsonSerializer.Deserialize<string[]>(File.ReadAllText(file))??[])if(File.Exists(path))Recent.Add(ReadRecent(path));}catch{Status="Die Liste letzter Projekte konnte nicht vollständig gelesen werden.";}
    }
    RecentProject ReadRecent(string path)
    {
        BitmapSource? thumb=null;
        try{using var zip=ZipFile.OpenRead(path);var entry=zip.GetEntry("preview.png");if(entry is {Length:<4000000}){using var stream=entry.Open();using var ms=new MemoryStream();stream.CopyTo(ms);thumb=ImageProcessor.Decode(ms.ToArray(),240);}}catch{ }
        return new(path,Path.GetFileNameWithoutExtension(path),File.GetLastWriteTime(path).ToString("dd.MM.yyyy · HH:mm"),thumb);
    }
    void AddRecent(string path){var old=Recent.FirstOrDefault(r=>r.Path==path);if(old!=null)Recent.Remove(old);Recent.Insert(0,ReadRecent(path));while(Recent.Count>12)Recent.RemoveAt(Recent.Count-1);PersistRecent();}
    void RemoveRecent(string path){var old=Recent.FirstOrDefault(r=>r.Path==path);if(old!=null)Recent.Remove(old);PersistRecent();}
    void PersistRecent()=>File.WriteAllText(Path.Combine(dataDir,"recent.json"),JsonSerializer.Serialize(Recent.Select(r=>r.Path)));
    void DuplicateProject(RecentProject r)
    {
        var dialog=new SaveFileDialog{Filter="LithoCreate-Projekt|*.lithopro",FileName=r.Name+" – Kopie"};if(dialog.ShowDialog()!=true)return;
        try{var data=ProjectStore.Load(r.Path);data.Data.Name=Path.GetFileNameWithoutExtension(dialog.FileName);ProjectStore.Save(dialog.FileName,data.Data,data.Image);AddRecent(dialog.FileName);}catch(Exception ex){Error(ex);}
    }
    void RenameProject(RecentProject r)
    {
        var dialog=new SaveFileDialog{Title="Projekt umbenennen",Filter="LithoCreate-Projekt|*.lithopro",FileName=r.Name,InitialDirectory=Path.GetDirectoryName(r.Path)};if(dialog.ShowDialog()!=true||dialog.FileName==r.Path)return;
        try{var loaded=ProjectStore.Load(r.Path);loaded.Data.Name=Path.GetFileNameWithoutExtension(dialog.FileName);ProjectStore.Save(dialog.FileName,loaded.Data,loaded.Image);File.Delete(r.Path);RemoveRecent(r.Path);AddRecent(dialog.FileName);if(projectPath==r.Path){projectPath=dialog.FileName;project.Name=loaded.Data.Name;Refresh();}}catch(Exception ex){Error(ex);}
    }
    public void CleanShutdown(){StopSections();debounce.Stop();autosave.Stop();previewCts?.Cancel();exportCts?.Cancel();if(File.Exists(RecoveryPath))File.Delete(RecoveryPath);}
    void Error(Exception ex){Status=ex.Message;MessageBox.Show(ex.Message,"LithoCreate Pro",MessageBoxButton.OK,MessageBoxImage.Warning);}
    sealed class ScaledProgress(IProgress<double> target,double start,double scale) : IProgress<double>{public void Report(double value)=>target.Report(start+value*scale);}
}
