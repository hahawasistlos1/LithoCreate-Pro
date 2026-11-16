using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Numerics;
using LithoCreate.Core;

namespace LithoCreate.App.Controls;
public sealed class PrintSetViewport:FrameworkElement
{
    public PrintSetViewport(){ClipToBounds=true;Focusable=true;Cursor=Cursors.Hand;IsEnabledChanged+=(_,_)=>{if(!IsEnabled)CancelDrag();};}
    public event Action<int,Vector2>? MoveRequested;
    public event Action<string>? MoveRejected;
    int dragPart=-1,selectedPart=-1;
    Point dragStart;
    Vector2 originalPosition,proposedPosition;
    bool invalidDrop;
    (double Scale,Rect Bed) Layout()
    {
        if(set==null)return (1,Rect.Empty);
        double aw=Math.Max(1,ActualWidth-58),ah=Math.Max(1,ActualHeight-100),scale=Math.Min(aw/set.BedX,ah/set.BedY);
        double w=set.BedX*scale,h=set.BedY*scale;return(scale,new Rect((ActualWidth-w)/2,58+(ah-h)/2,w,h));
    }
    Rect PartRect(int index,Vector2 position)
    {
        var (scale,bed)=Layout();var size=set!.Parts[index].Report.Size;return new(bed.Left+position.X*scale,bed.Bottom-(position.Y+size.Y)*scale,size.X*scale,size.Y*scale);
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);if(set==null||!IsEnabled)return;Point point=e.GetPosition(this);
        for(int i=set.Parts.Count-1;i>=0;i--)if(PartRect(i,set.Parts[i].Position).Contains(point))
        {
            Focus();selectedPart=i;dragPart=i;dragStart=point;originalPosition=proposedPosition=set.Parts[i].Position;invalidDrop=false;
            if(!CaptureMouse())dragPart=-1;InvalidateVisual();e.Handled=true;return;
        }
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);if(dragPart<0||set==null)return;if(e.LeftButton!=MouseButtonState.Pressed){CancelDrag();return;}
        var delta=e.GetPosition(this)-dragStart;double scale=Layout().Scale;var size=set.Parts[dragPart].Report.Size;
        double x=Math.Clamp(Math.Round(originalPosition.X+delta.X/scale,1),set.Margin,Math.Max(set.Margin,set.BedX-set.Margin-size.X));
        double y=Math.Clamp(Math.Round(originalPosition.Y-delta.Y/scale,1),set.Margin,Math.Max(set.Margin,set.BedY-set.Margin-size.Y));
        proposedPosition=new((float)x,(float)y);try{set.WithPosition(dragPart,proposedPosition);invalidDrop=false;}catch(System.IO.InvalidDataException){invalidDrop=true;}
        InvalidateVisual();e.Handled=true;
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);if(dragPart<0)return;int index=dragPart;var position=proposedPosition;CancelDrag();RequestMove(index,position);e.Handled=true;
    }
    protected override void OnLostMouseCapture(MouseEventArgs e){base.OnLostMouseCapture(e);CancelDrag();}
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);if(e.Key==Key.Escape&&dragPart>=0){CancelDrag();e.Handled=true;return;}
        if(set==null||selectedPart<0||!IsEnabled)return;float step=Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)?10:1;
        Vector2 delta=e.Key switch{Key.Left=>new(-step,0),Key.Right=>new(step,0),Key.Up=>new(0,step),Key.Down=>new(0,-step),_=>default};
        if(delta==default)return;RequestMove(selectedPart,set.Parts[selectedPart].Position+delta);e.Handled=true;
    }
    void RequestMove(int index,Vector2 position)
    {
        if(set==null)return;
        try{set.WithPosition(index,position);MoveRequested?.Invoke(index,position);}
        catch(System.IO.InvalidDataException ex){MoveRejected?.Invoke(ex.Message);}
    }
    void CancelDrag(){int old=dragPart;dragPart=-1;invalidDrop=false;if(IsMouseCaptured)ReleaseMouseCapture();if(old>=0)InvalidateVisual();}

    PrintSet? set;
    public PrintSet? PrintSet {get=>set;set{CancelDrag();set=value;if(value==null)selectedPart=-1;InvalidateVisual();}}
    static readonly Color[] colors=[Color.FromRgb(0,177,239),Color.FromRgb(65,209,143)];
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(7,19,29)),null,new Rect(RenderSize));
        if(set==null){Text("Drucksatz berechnen",new(24,70),17,Colors.LightSteelBlue);Text("Die Bettübersicht verwendet beide Teile in Exportauflösung.",new(24,100),11,Colors.LightSteelBlue);return;}
        double availableW=Math.Max(1,ActualWidth-58),availableH=Math.Max(1,ActualHeight-100),scale=Math.Min(availableW/set.BedX,availableH/set.BedY),w=set.BedX*scale,h=set.BedY*scale,left=(ActualWidth-w)/2,top=58+(availableH-h)/2;
        var bed=new Rect(left,top,w,h);dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(13,35,47)),new Pen(new SolidColorBrush(Color.FromRgb(88,141,165)),1),bed);
        var safe=new Pen(new SolidColorBrush(Color.FromRgb(69,116,131)),1){DashStyle=DashStyles.Dash};dc.DrawRectangle(null,safe,new Rect(left+set.Margin*scale,top+set.Margin*scale,w-2*set.Margin*scale,h-2*set.Margin*scale));
        var grid=new Pen(new SolidColorBrush(Color.FromRgb(30,60,75)),.5);double step=Math.Max(10,Math.Ceiling(Math.Max(set.BedX,set.BedY)/40/10)*10);
        for(double x=step;x<set.BedX;x+=step)dc.DrawLine(grid,new(left+x*scale,top),new(left+x*scale,top+h));for(double y=step;y<set.BedY;y+=step)dc.DrawLine(grid,new(left,top+y*scale),new(left+w,top+y*scale));
        for(int i=0;i<set.Parts.Count;i++)
        {
            var p=set.Parts[i];var position=i==dragPart?proposedPosition:p.Position;var rect=PartRect(i,position);var c=i==dragPart&&invalidDrop?Colors.OrangeRed:colors[i%colors.Length];dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(75,c.R,c.G,c.B)),new Pen(new SolidColorBrush(i==selectedPart&&!invalidDrop?Colors.White:c),i==selectedPart?2.5:1.5),rect);
            if(rect.Width>48&&rect.Height>30){var label=Label((i+1)+" · "+p.Name,11,Colors.White);label.MaxTextWidth=Math.Max(1,rect.Width-8);label.Trimming=TextTrimming.CharacterEllipsis;dc.DrawText(label,new(rect.Left+4,rect.Top+5));}
        }
        Text($"{set.BedX:N1} × {set.BedY:N1} mm · X →  Y ↑",new(left,top+h+8),11,Colors.LightSteelBlue);
        Text($"Hüllrechtecke · {set.Margin:N1} mm Rand · {set.Gap:N1} mm Abstand",new(18,ActualHeight-22),10,Colors.LightSteelBlue);
        void Text(string s,Point point,double size,Color color)=>dc.DrawText(Label(s,size,color),point);
        FormattedText Label(string s,double size,Color color)=>new(s,CultureInfo.GetCultureInfo("de-DE"),FlowDirection.LeftToRight,new Typeface("Segoe UI"),size,new SolidColorBrush(color),VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }
}
