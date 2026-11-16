using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LithoCreate.Core;

namespace LithoCreate.App.Controls;
public sealed class SectionViewport : FrameworkElement
{
    public static readonly DependencyProperty SectionProperty=DependencyProperty.Register(nameof(Section),typeof(CrossSection),typeof(SectionViewport),new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.AffectsRender,Changed));
    public CrossSection? Section {get=>(CrossSection?)GetValue(SectionProperty);set=>SetValue(SectionProperty,value);}
    StreamGeometry? geometry;
    double zoom=1,panX,panY;
    Point? drag;
    public SectionViewport()
    {
        ClipToBounds=true;Focusable=true;Cursor=Cursors.Cross;
        ToolTip="Mausrad: zoomen · Ziehen: verschieben · Doppelklick: Ansicht zurücksetzen";
        MouseDown+=(_,e)=>{Focus();if(e.ClickCount==2){ResetView();return;}drag=e.GetPosition(this);CaptureMouse();e.Handled=true;};
        MouseMove+=(_,e)=>{if(drag is not Point previous)return;var p=e.GetPosition(this);panX+=p.X-previous.X;panY+=p.Y-previous.Y;drag=p;InvalidateVisual();};
        MouseUp+=(_,_)=>{drag=null;ReleaseMouseCapture();};
        MouseWheel+=(_,e)=>{zoom=Math.Clamp(zoom*Math.Pow(1.15,e.Delta/120.0),.2,40);InvalidateVisual();e.Handled=true;};
    }
    public void ResetView(){zoom=1;panX=panY=0;InvalidateVisual();}
    static void Changed(DependencyObject sender,DependencyPropertyChangedEventArgs e)
    {
        var view=(SectionViewport)sender;view.geometry=null;
        if(e.NewValue is not CrossSection section)return;
        var path=new StreamGeometry{FillRule=FillRule.EvenOdd};
        using(var context=path.Open())foreach(var contour in section.Paths)
        {
            if(contour.Points.Count<2)continue;var first=contour.Points[0];context.BeginFigure(new(first.X,first.Y),contour.Closed,contour.Closed);
            for(int i=1;i<contour.Points.Count;i++){var point=contour.Points[i];context.LineTo(new(point.X,point.Y),true,false);}
        }
        path.Freeze();view.geometry=path;
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(5,16,26)),null,new Rect(RenderSize));
        if(Section is not CrossSection section||geometry==null){Label(dc,"Schicht wird vorbereitet …",new(20,24),Colors.LightSlateGray);return;}
        double width=Math.Max(10,section.Max.X-section.Min.X),height=Math.Max(10,section.Max.Y-section.Min.Y);
        double scale=Math.Max(.01,Math.Min(Math.Max(1,ActualWidth-60)/width,Math.Max(1,ActualHeight-60)/height)*zoom);
        double cx=(section.Min.X+section.Max.X)/2,cy=(section.Min.Y+section.Max.Y)/2;
        double ox=ActualWidth/2+panX-cx*scale,oy=ActualHeight/2+panY+cy*scale;
        dc.PushTransform(new MatrixTransform(new Matrix(scale,0,0,-scale,ox,oy)));
        var gridPen=new Pen(new SolidColorBrush(Color.FromRgb(24,48,64)),1/scale);
        double step=Math.Pow(10,Math.Floor(Math.Log10(50/scale)));if(step*scale<22)step*=5;
        double left=(-ox)/scale,right=(ActualWidth-ox)/scale,bottom=(oy-ActualHeight)/scale,top=oy/scale;
        for(double x=Math.Floor(left/step)*step;x<=right;x+=step)dc.DrawLine(gridPen,new(x,bottom),new(x,top));
        for(double y=Math.Floor(bottom/step)*step;y<=top;y+=step)dc.DrawLine(gridPen,new(left,y),new(right,y));
        var fill=new SolidColorBrush(Color.FromArgb(95,255,187,85));var outline=new Pen(new SolidColorBrush(Color.FromRgb(49,199,255)),1.6/scale);
        dc.DrawGeometry(fill,outline,geometry);dc.Pop();
        Label(dc,$"Z = {section.Height:N3} mm",new(16,45),Color.FromRgb(153,194,215));
        Label(dc,$"Raster {step:G} mm · X → · Y ↑",new(15,Math.Max(0,ActualHeight-29)),Color.FromRgb(100,153,181));
        if(section.Paths.Count==0)Label(dc,"Kein Material in dieser Höhe.",new(20,ActualHeight/2),Colors.LightSteelBlue);
    }
    void Label(DrawingContext dc,string text,Point point,Color color)
    {
        var formatted=new FormattedText(text,CultureInfo.GetCultureInfo("de-DE"),FlowDirection.LeftToRight,new Typeface("Segoe UI"),11,new SolidColorBrush(color),VisualTreeHelper.GetDpi(this).PixelsPerDip);dc.DrawText(formatted,point);
    }
}
