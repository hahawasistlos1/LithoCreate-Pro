using System.IO;
using System.Windows.Input;
using LithoCreate.Core;
using LithoCreate.App.Services;
using Microsoft.Win32;

namespace LithoCreate.App.ViewModels;
public sealed partial class StudioViewModel
{
    bool printSetMode,buildingPrintSet;
    PrintSet? preparedPrintSet;
    string printSetInfo="Beide Einzelteile in Exportauflösung berechnen. Gespeicherte Drucklagen bleiben erhalten.";
    public event Action<PrintSet?>? PrintSetReady;
    public PrintSet? PreparedPrintSet=>preparedPrintSet;
    public bool HasPrintSet=>preparedPrintSet!=null;
    public bool IsPrintSetMode {get=>printSetMode;set{if(value&&!HasParts||value==printSetMode)return;printSetMode=value;if(value)IsSectionMode=false;NotifyPrintSetView();}}
    public string PrintSetInfo {get=>printSetInfo;private set{printSetInfo=value;Notify();}}
    public ICommand ShowPrintSet {get;private set;}=null!;
    public ICommand RebuildPrintSet {get;private set;}=null!;
    public ICommand ExportPrintSet {get;private set;}=null!;
    void InitializePrintSet()
    {
        InitializePrintLayoutHistory();
        ShowPrintSet=new RelayCommand(async _=>{IsPrintSetMode=true;if(!HasPrintSet&&!InputError)await BuildPrintSet();},()=>!Busy&&HasParts);
        RebuildPrintSet=new RelayCommand(async _=>{InvalidatePrintSet();await BuildPrintSet();},()=>!Busy&&!InputError&&HasParts);
        ExportPrintSet=new RelayCommand(async _=>await SavePrintSet(),()=>!Busy&&!InputError&&HasParts&&HasPrintSet);
    }
    void NotifyPrintSetView(){Notify(nameof(IsPrintSetMode));Notify(nameof(IsSurfaceMode));Notify(nameof(ShowModelOptions));Notify(nameof(PreviewTitle));Notify(nameof(NavigationHint));Notify(nameof(ExportHeading));CommandManager.InvalidateRequerySuggested();}
    void InvalidatePrintSet()
    {
        if(buildingPrintSet)exportCts?.Cancel();preparedPrintSet=null;Notify(nameof(PreparedPrintSet));Notify(nameof(HasPrintSet));PrintSetReady?.Invoke(null);
        PrintSetInfo="Drucksatz neu berechnen: Modell, Bild, Drucklage oder Druckerprofil können sich geändert haben.";
    }
    public async Task BuildPrintSet()
    {
        if(Busy||InputError||!HasParts)return;
        IsPrintSetMode=true;InvalidateInspection();Busy=true;buildingPrintSet=true;debounce.Stop();previewCts?.Cancel();var cts=new CancellationTokenSource();exportCts=cts;int revision=sectionRevision;Progress=0;
        try
        {
            var settings=Model.Clone();var edits=Edits.Clone();var bytes=sourceBytes;var options=Project.PrintLayout.Clone();var printer=new PrinterProfile{Name=Printer.Name,Process=Printer.Process,BedX=Printer.BedX,BedY=Printer.BedY,BuildZ=Printer.BuildZ,Nozzle=Printer.Nozzle,Layer=Printer.Layer,Material=Printer.Material};
            Status="Beide Einzelteile in Exportauflösung berechnen und auf dem Druckbett anordnen …";
            var progress=new Progress<double>(p=>Progress=p*100);
            var result=await Task.Run(()=>ModelComputation.BuildPrintSet(bytes,settings,edits,printer,cts.Token,progress,options),cts.Token);
            if(cts.IsCancellationRequested||revision!=sectionRevision||InputError)return;
            SetPreparedPrintSet(result);
            Progress=100;Status="Drucksatz bereit: zwei getrennte, geprüfte Objekte auf dem Druckbett.";
        }
        catch(OperationCanceledException){PrintSetInfo="Drucksatz-Berechnung abgebrochen.";Status=PrintSetInfo;}
        catch(Exception ex){PrintSetInfo=ex.Message;Status="Drucksatz nicht verfügbar: "+ex.Message;}
        finally{buildingPrintSet=false;Busy=false;if(ReferenceEquals(exportCts,cts))exportCts=null;cts.Dispose();CommandManager.InvalidateRequerySuggested();}
    }
    void SetPreparedPrintSet(PrintSet result)
    {
        preparedPrintSet=result;Notify(nameof(PreparedPrintSet));Notify(nameof(HasPrintSet));PrintSetReady?.Invoke(result);
        string arrangement=result.Arrangement switch{PrintArrangement.Row=>"Zeile",PrintArrangement.Column=>"Spalte",_=>"Manuell"};
        PrintSetInfo=$"Druckbett {result.BedX:N1} × {result.BedY:N1} mm · Bauhöhe {result.BuildZ:N1} mm\n{result.Margin:N2} mm Rand · {result.Gap:N2} mm Teileabstand\n{arrangement}\n\n"+string.Join("\n\n",result.Parts.Select(p=>$"{p.Name}\nXY: {p.Position.X:N2} / {p.Position.Y:N2} mm\n{p.Report.Size.X:N2} × {p.Report.Size.Y:N2} × {p.Report.Size.Z:N2} mm\n{p.Report.Triangles:N0} Dreiecke · Raster {p.Report.Resolution:N3} mm"));
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }
    async Task SavePrintSet()
    {
        IsPrintSetMode=true;if(!HasPrintSet)await BuildPrintSet();if(preparedPrintSet is not PrintSet set||Busy||InputError)return;
        string folder=Path.GetDirectoryName(projectPath)??"";
        var dialog=new SaveFileDialog{Filter="3MF-Drucksatz|*.3mf",FileName=Project.Name+" – Drucksatz",InitialDirectory=Directory.Exists(folder)?folder:""};if(dialog.ShowDialog()!=true)return;
        Busy=true;debounce.Stop();previewCts?.Cancel();var cts=new CancellationTokenSource();exportCts=cts;Progress=0;
        try{Status="Drucksatz schreiben und beide gespeicherten Objekte erneut prüfen …";var progress=new Progress<double>(p=>Progress=p*100);await Task.Run(()=>PrintSetExporter.Export(dialog.FileName,set,cts.Token,progress),cts.Token);Progress=100;Status="✓ 3MF-Drucksatz mit zwei getrennten Objekten gespeichert und erneut geprüft · "+dialog.FileName;}
        catch(OperationCanceledException){Status="Drucksatz-Export abgebrochen. Eine bestehende Zieldatei blieb unverändert.";}
        catch(Exception ex){Status="Drucksatz-Export fehlgeschlagen: "+ex.Message;}
        finally{Busy=false;if(ReferenceEquals(exportCts,cts))exportCts=null;cts.Dispose();}
    }
}
