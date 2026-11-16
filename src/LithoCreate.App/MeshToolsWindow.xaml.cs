using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using LithoCreate.App.Services;
using LithoCreate.App.ViewModels;
using LithoCreate.Core;
using Microsoft.Win32;

namespace LithoCreate.App;
public partial class MeshToolsWindow:Window
{
    readonly ModelSettings settings;readonly GrayImage photo;readonly byte[] source;readonly ImageEdits edits;
    ImportedModel? model;CancellationTokenSource? calculation;bool busy;
    public MeshToolsWindow(StudioViewModel vm)
    {
        InitializeComponent();settings=vm.Model.Clone();settings.Part=ModelPart.Lithophane;settings.MemoryMb=Math.Min(settings.MemoryMb,128);source=ImageProcessor.Png(vm.Original);edits=vm.Edits.Clone();photo=ImageProcessor.Process(vm.Original,edits).Gray;
        MountChoice.ItemsSource=WorkshopParts.Presets;MountChoice.SelectedIndex=0;
        Closing+=(_,e)=>{if(busy){calculation?.Cancel();e.Cancel=true;Status.Text="Abbruch angefordert. Anschließend das Fenster schließen.";}};
    }
    public async Task ImportFile(string path)=>await Run(async token=>{var read=await Task.Run(()=>StlImport.Read(path,300000,token),token);SetModel(read,Path.GetFileName(path));});
    void MountChanged(object sender,System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if(MeasureA==null||MountChoice.SelectedItem is not WorkshopPreset p)return;MeasureA.Text=p.A.ToString(System.Globalization.CultureInfo.GetCultureInfo("de-DE"));MeasureB.Text=p.B.ToString(System.Globalization.CultureInfo.GetCultureInfo("de-DE"));MeasureT.Text=p.Thickness.ToString(System.Globalization.CultureInfo.GetCultureInfo("de-DE"));MeasureALabel.Text=p.Kind==WorkshopKind.SocketRing?"Freier Durchlass · mm":p.Kind==WorkshopKind.LedChannel?"Freie Streifenbreite · mm":"Breite / Fußlänge · mm";MeasureBLabel.Text=p.Kind==WorkshopKind.SocketRing?"Außendurchmesser · mm":p.Kind==WorkshopKind.LedChannel?"Länge · mm":"Höhe · mm";
    }
    List<MountingHole> ReadPlateHoles()
    {
        var holes=new List<MountingHole>();var culture=System.Globalization.CultureInfo.GetCultureInfo("de-DE");foreach(string line in PlateHoles.Text.Split('\n',StringSplitOptions.RemoveEmptyEntries)){var f=line.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);if(f.Length!=3||!double.TryParse(f[0],System.Globalization.NumberStyles.Float,culture,out double x)||!double.TryParse(f[1],System.Globalization.NumberStyles.Float,culture,out double y)||!double.TryParse(f[2],System.Globalization.NumberStyles.Float,culture,out double d))throw new System.IO.InvalidDataException("Bohrungen: X Y Durchmesser je Zeile eingeben.");holes.Add(new(){X=x,Y=y,Diameter=d});}return holes;
    }
    void ParametersChanged(object sender,System.Windows.Controls.TextChangedEventArgs e){if(!IsLoaded||busy)return;model=null;Preview.Clear();ExportButton.IsEnabled=BambuButton.IsEnabled=false;Info.Text="Halterungsmaße geändert. Neu berechnen.";}
    void EnableInputs(bool value){MountChoice.IsEnabled=MeasureA.IsEnabled=MeasureB.IsEnabled=MeasureT.IsEnabled=PlateHoles.IsEnabled=value;}
    async void BuildMount(object sender,RoutedEventArgs e)
    {
        if(busy||MountChoice.SelectedItem is not WorkshopPreset p)return;
        if(!double.TryParse(MeasureA.Text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.GetCultureInfo("de-DE"),out double a)||!double.TryParse(MeasureB.Text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.GetCultureInfo("de-DE"),out double b)||!double.TryParse(MeasureT.Text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.GetCultureInfo("de-DE"),out double t)){Status.Text="Gültige Maße mit Dezimalkomma eingeben.";return;}
        var holes=new List<MountingHole>();
        if(p.Kind==WorkshopKind.MountingPlate){foreach(string line in PlateHoles.Text.Split('\n',StringSplitOptions.RemoveEmptyEntries)){var fields=line.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries);if(fields.Length!=3||!double.TryParse(fields[0],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.GetCultureInfo("de-DE"),out double x)||!double.TryParse(fields[1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.GetCultureInfo("de-DE"),out double y)||!double.TryParse(fields[2],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.GetCultureInfo("de-DE"),out double d)){Status.Text="Bohrungen: X Y Durchmesser je Zeile eingeben.";return;}holes.Add(new(){X=x,Y=y,Diameter=d});}}
        await Run(async token=>{var mesh=await Task.Run(()=>WorkshopParts.Generate(p.Kind,a,b,t,token,holes),token);SetModel(new(mesh,mesh.Validate(token),false,"Halterung nach Maß"),p.Name);});
    }
    async void ImportStl(object sender,RoutedEventArgs e){var dialog=new OpenFileDialog{Filter="STL-Dateien|*.stl"};if(dialog.ShowDialog(this)==true)await ImportFile(dialog.FileName);}
    async void UseProject(object sender,RoutedEventArgs e)=>await Run(async token=>{var result=await Task.Run(()=>ModelComputation.BuildExportMesh(source,settings,edits,token),token);SetModel(new(result.Mesh,result.Report,false,"Projektmodell"),"Aktuelles Foto und Modell");});
    async void LoadPlugin(object sender,RoutedEventArgs e)
    {
        var dialog=new OpenFileDialog{Filter="LithoCreate-Formmodul|*.dll",InitialDirectory=Path.Combine(AppContext.BaseDirectory,"Extensions")};if(dialog.ShowDialog(this)!=true)return;
        if(MessageBox.Show(this,"Ein Formmodul führt lokalen Programmcode aus und erhält das bearbeitete Foto und die Modellparameter. Es kann auf Dateien und Netzwerk zugreifen. Der separate Prozess ist keine Sicherheitssandbox. Nur selbst erstellte oder vertrauenswürdige Module verwenden. Dieses Modul ausführen?","Formmodul ausführen",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
        string work=Path.Combine(Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LithoCreate Pro"),"work");
        var parameters=settings.Clone();try{parameters.BackMountingHoles=ReadPlateHoles();}catch(Exception ex){Status.Text=ex.Message;return;}
        await Run(async token=>SetModel(await PluginRunner.Generate(Path.Combine(AppContext.BaseDirectory,"LithoCreate.PluginHost.exe"),dialog.FileName,photo,parameters,token,work),Path.GetFileNameWithoutExtension(dialog.FileName)));
    }
    async Task Run(Func<CancellationToken,Task> action)
    {
        if(busy)return;busy=true;EnableInputs(false);calculation=new();model=null;Preview.Clear();ExportButton.IsEnabled=BambuButton.IsEnabled=false;ImportButton.IsEnabled=PluginButton.IsEnabled=PhotoButton.IsEnabled=KitButton.IsEnabled=MountButton.IsEnabled=false;CancelButton.IsEnabled=true;Info.Text="Berechnung und Prüfung …";
        try{await action(calculation.Token);Status.Text="Modell geladen und geometrisch geprüft.";}catch(OperationCanceledException){Status.Text="Berechnung abgebrochen.";}catch(Exception ex){Info.Text="Kein Modell verfügbar.";Status.Text=ex.Message;}
        finally{busy=false;EnableInputs(true);calculation.Dispose();calculation=null;CancelButton.IsEnabled=false;ImportButton.IsEnabled=PluginButton.IsEnabled=PhotoButton.IsEnabled=KitButton.IsEnabled=MountButton.IsEnabled=true;}
    }
    void SetModel(ImportedModel value,string name)
    {
        model=value;var r=value.Report;Info.Text=$"{name}\n{value.Format}\n\n{r.Size.X:N2} × {r.Size.Z:N2} × {r.Size.Y:N2} mm\n{r.Triangles:N0} Dreiecke\n{r.Volume/1000:N2} cm³ geometrisches Volumen\n\n{r.OpenOrNonManifoldEdges} offene/nichtmanifolde Kanten\n{r.InconsistentEdges} inkonsistente Kanten\n{r.DegenerateFaces} degenerierte Flächen\n\n"+(r.IsValid?"Topologieprüfung bestanden.":"Fehlerhaftes Netz · Export gesperrt.")+(value.OrientationCorrected?"\nGesamtwicklung sicher umgekehrt.":"");
        Preview.SetMesh(value.Mesh,null,true);Preview.View("light");ExportButton.IsEnabled=BambuButton.IsEnabled=r.IsValid;
    }
    string? ChooseExport(){var dialog=new SaveFileDialog{Filter="STL-Modell|*.stl|3MF-Modell|*.3mf",FileName="Geprueftes-Modell"};return dialog.ShowDialog(this)==true?dialog.FileName:null;}
    async Task<bool> Export(string path)
    {
        if(busy||model is not {Report.IsValid:true} current)return false;busy=true;EnableInputs(false);calculation=new();ExportButton.IsEnabled=BambuButton.IsEnabled=ImportButton.IsEnabled=PluginButton.IsEnabled=PhotoButton.IsEnabled=KitButton.IsEnabled=MountButton.IsEnabled=false;CancelButton.IsEnabled=true;
        try{await Task.Run(()=>ModelExporter.Export(path,current.Mesh,calculation.Token));Status.Text="Gespeichert und erneut geprüft: "+path;return true;}catch(Exception ex){Status.Text=ex.Message;return false;}
        finally{busy=false;EnableInputs(true);calculation.Dispose();calculation=null;CancelButton.IsEnabled=false;ExportButton.IsEnabled=BambuButton.IsEnabled=true;ImportButton.IsEnabled=PluginButton.IsEnabled=PhotoButton.IsEnabled=KitButton.IsEnabled=MountButton.IsEnabled=true;}
    }
    async void SaveModel(object sender,RoutedEventArgs e){if(ChooseExport() is {} path)await Export(path);}
    async void OpenBambu(object sender,RoutedEventArgs e)
    {
        string exe=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Bambu Studio","bambu-studio.exe");if(!File.Exists(exe)){var find=new OpenFileDialog{Title="Installiertes Bambu Studio auswählen",Filter="Bambu Studio|bambu-studio.exe"};if(find.ShowDialog(this)!=true)return;exe=find.FileName;}
        if(ChooseExport() is not {} path||!await Export(path))return;
        try{var start=new ProcessStartInfo(exe){UseShellExecute=false};start.ArgumentList.Add(path);Process.Start(start);Status.Text="Geprüftes Modell an Bambu Studio übergeben. Dort P1S, PETG und Druckvorschau prüfen.";}catch(Exception ex){Status.Text=ex.Message;}
    }
    async void SaveKit(object sender,RoutedEventArgs e)
    {
        var dialog=new SaveFileDialog{Filter="PETG-Probedruckpaket|*.zip",FileName="P1S-PETG-Probedrucke"};if(dialog.ShowDialog(this)!=true)return;
        // This operation does not replace the loaded inspection model.
        if(busy)return;busy=true;MountButton.IsEnabled=KitButton.IsEnabled=ImportButton.IsEnabled=PluginButton.IsEnabled=PhotoButton.IsEnabled=ExportButton.IsEnabled=BambuButton.IsEnabled=false;calculation=new();CancelButton.IsEnabled=true;
        try{await Task.Run(()=>CalibrationKit.Export(dialog.FileName,calculation.Token));Status.Text="Geprüfte Probedrucke und Messprotokoll gespeichert: "+dialog.FileName;}catch(Exception ex){Status.Text=ex.Message;}
        finally{busy=false;EnableInputs(true);calculation.Dispose();calculation=null;MountButton.IsEnabled=KitButton.IsEnabled=ImportButton.IsEnabled=PluginButton.IsEnabled=PhotoButton.IsEnabled=true;ExportButton.IsEnabled=BambuButton.IsEnabled=model?.Report.IsValid==true;CancelButton.IsEnabled=false;}
    }
    void Cancel(object sender,RoutedEventArgs e)=>calculation?.Cancel();
    void Front(object sender,RoutedEventArgs e)=>Preview.View("front");void ViewTop(object sender,RoutedEventArgs e)=>Preview.View("top");void ToggleLight(object sender,RoutedEventArgs e)=>Preview.View("light");
}
