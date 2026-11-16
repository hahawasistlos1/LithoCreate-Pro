using System.Numerics;
using System.Windows.Input;
using LithoCreate.Core;

namespace LithoCreate.App.ViewModels;
public sealed partial class StudioViewModel
{
    // Only parameters and placements: history must never retain full-resolution mesh copies.
    sealed record PrintLayoutState(PrintSetOptions Options,Vector2[]? Positions,PrintArrangement ActualArrangement);
    readonly List<PrintLayoutState> printLayoutUndo=[],printLayoutRedo=[];
    bool changingPrintLayout;
    public ICommand UndoPrintLayout {get;private set;}=null!;
    public ICommand RedoPrintLayout {get;private set;}=null!;
    public string PrintLayoutHistoryInfo=>$"Drucksatz: {printLayoutUndo.Count} zurück · {printLayoutRedo.Count} wiederholen";
    void InitializePrintLayoutHistory()
    {
        UndoPrintLayout=new RelayCommand(_=>RestorePrintLayout(false),()=>CanRestorePrintLayout(printLayoutUndo));
        RedoPrintLayout=new RelayCommand(_=>RestorePrintLayout(true),()=>CanRestorePrintLayout(printLayoutRedo));
    }
    bool CanRestorePrintLayout(List<PrintLayoutState> history)=>IsEditor&&IsModel&&IsPrintSetMode&&HasParts&&!Busy&&!InputError&&history.Count>0;
    PrintLayoutState CapturePrintLayout()=>new(Project.PrintLayout.Clone(),preparedPrintSet?.Parts.Select(p=>p.Position).ToArray(),preparedPrintSet?.Arrangement??Project.PrintLayout.Arrangement);
    static void PushPrintLayout(List<PrintLayoutState> history,PrintLayoutState state){history.Add(state);if(history.Count>100)history.RemoveAt(0);}
    void RecordPrintLayout(){PushPrintLayout(printLayoutUndo,CapturePrintLayout());printLayoutRedo.Clear();NotifyPrintLayoutHistory();}
    void ClearPrintLayoutHistory(){printLayoutUndo.Clear();printLayoutRedo.Clear();NotifyPrintLayoutHistory();}
    void NotifyPrintLayoutHistory(){Notify(nameof(PrintLayoutHistoryInfo));CommandManager.InvalidateRequerySuggested();}
    void PrintLayoutChanged()
    {
        changingPrintLayout=true;try{Changed();}finally{changingPrintLayout=false;}
    }
    void RestorePrintLayout(bool repeat)
    {
        var from=repeat?printLayoutRedo:printLayoutUndo;var to=repeat?printLayoutUndo:printLayoutRedo;if(!CanRestorePrintLayout(from))return;
        var target=from[^1];var current=CapturePrintLayout();PrintSet? restored=null;
        if(preparedPrintSet is {} set&&target.Positions is {Length:2} positions)
        {
            restored=set with{Margin=target.Options.Margin,Gap=target.Options.Gap,Arrangement=target.ActualArrangement,Parts=[set.Parts[0] with{Position=positions[0]},set.Parts[1] with{Position=positions[1]}]};
            try{restored.ValidatePlacement();}catch(System.IO.InvalidDataException ex){Status="Drucksatzschritt nicht übernommen: "+ex.Message;return;}
        }
        from.RemoveAt(from.Count-1);PushPrintLayout(to,current);Project.PrintLayout=target.Options.Clone();NotifyPrintLayout();
        if(restored!=null){Dirty=true;SetPreparedPrintSet(restored);}else PrintLayoutChanged();
        NotifyPrintLayoutHistory();Status=(repeat?"Drucksatzschritt wiederholt.":"Drucksatzschritt zurückgenommen.")+(restored==null?" Drucksatz neu berechnen.":" Positionen bereit für den Export.");
    }
}
