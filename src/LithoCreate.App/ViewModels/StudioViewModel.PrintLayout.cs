using LithoCreate.Core;
using System.Numerics;
using System.IO;

namespace LithoCreate.App.ViewModels;
public sealed partial class StudioViewModel
{
    public double PrintSetMargin
    {
        get=>Project.PrintLayout.Margin;
        set{if(value==PrintSetMargin)return;var next=Project.PrintLayout.Clone();next.Margin=value;SetPrintLayout(next);}
    }
    public double PrintSetGap
    {
        get=>Project.PrintLayout.Gap;
        set{if(value==PrintSetGap)return;var next=Project.PrintLayout.Clone();next.Gap=value;SetPrintLayout(next);}
    }
    public PrintArrangementChoice[] PrintSetArrangements=>PrintSetOptions.Choices;
    public PrintArrangementChoice SelectedPrintSetArrangement
    {
        get=>PrintSetOptions.Choices.First(p=>p.Arrangement==Project.PrintLayout.Arrangement);
        set
        {
            if(value==null||value.Arrangement==Project.PrintLayout.Arrangement)return;
            if(Busy)return;
            if(value.Arrangement==PrintArrangement.Manual&&preparedPrintSet is {} set){CommitManualPrintSet(set.WithPositions(set.Parts[0].Position,set.Parts[1].Position));return;}
            var next=Project.PrintLayout.Clone();next.Arrangement=value.Arrangement;SetPrintLayout(next);
        }
    }
    public bool IsManualPrintSet=>Project.PrintLayout.Arrangement==PrintArrangement.Manual;
    public double BodyPositionX {get=>Project.PrintLayout.BodyX;set=>SetManualCoordinate(0,false,value);}
    public double BodyPositionY {get=>Project.PrintLayout.BodyY;set=>SetManualCoordinate(0,true,value);}
    public double AccessoryPositionX {get=>Project.PrintLayout.AccessoryX;set=>SetManualCoordinate(1,false,value);}
    public double AccessoryPositionY {get=>Project.PrintLayout.AccessoryY;set=>SetManualCoordinate(1,true,value);}
    void SetManualCoordinate(int index,bool y,double value)
    {
        if(Busy)throw new InvalidDataException("Zuerst die laufende Berechnung abwarten oder abbrechen.");
        var next=Project.PrintLayout.Clone();double previous=index==0?(y?next.BodyY:next.BodyX):(y?next.AccessoryY:next.AccessoryX);if(previous==value)return;
        if(index==0){if(y)next.BodyY=value;else next.BodyX=value;}else{if(y)next.AccessoryY=value;else next.AccessoryX=value;}next.Validate();
        if(preparedPrintSet is {} set&&IsManualPrintSet)
        {
            var moved=set.WithPositions(new((float)next.BodyX,(float)next.BodyY),new((float)next.AccessoryX,(float)next.AccessoryY));
            RecordPrintLayout();Project.PrintLayout=next;Dirty=true;NotifyPrintLayout();SetPreparedPrintSet(moved);Status="Teileposition aktualisiert · Projekt zum Erhalten der Position speichern.";
        }
        else{RecordPrintLayout();Project.PrintLayout=next;NotifyPrintLayout();PrintLayoutChanged();}
    }
    public bool TryMovePrintSetPart(int index,Vector2 position)
    {
        if(Busy||InputError||preparedPrintSet is not {} set)return false;
        try{CommitManualPrintSet(set.WithPosition(index,position));return true;}
        catch(InvalidDataException ex){Status=ex.Message;return false;}
    }
    void CommitManualPrintSet(PrintSet moved)
    {
        var next=Project.PrintLayout.Clone();next.Arrangement=PrintArrangement.Manual;
        next.BodyX=moved.Parts[0].Position.X;next.BodyY=moved.Parts[0].Position.Y;next.AccessoryX=moved.Parts[1].Position.X;next.AccessoryY=moved.Parts[1].Position.Y;next.Validate();
        if(Project.PrintLayout.Arrangement==PrintArrangement.Manual&&next.BodyX==BodyPositionX&&next.BodyY==BodyPositionY&&next.AccessoryX==AccessoryPositionX&&next.AccessoryY==AccessoryPositionY)return;
        RecordPrintLayout();
        Project.PrintLayout=next;Dirty=true;NotifyPrintLayout();SetPreparedPrintSet(moved);Status="Freie Teilepositionen übernommen · Projekt separat speichern.";
    }
    void SetPrintLayout(PrintSetOptions next){next.Validate();RecordPrintLayout();Project.PrintLayout=next;NotifyPrintLayout();PrintLayoutChanged();}
    void NotifyPrintLayout(){Notify(nameof(PrintSetMargin));Notify(nameof(PrintSetGap));Notify(nameof(SelectedPrintSetArrangement));Notify(nameof(IsManualPrintSet));Notify(nameof(BodyPositionX));Notify(nameof(BodyPositionY));Notify(nameof(AccessoryPositionX));Notify(nameof(AccessoryPositionY));}
}
