using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LithoCreate.App.Controls;
public sealed class PhotoComparison : FrameworkElement
{
    public static readonly DependencyProperty BeforeProperty=DependencyProperty.Register(nameof(Before),typeof(ImageSource),typeof(PhotoComparison),new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty AfterProperty=DependencyProperty.Register(nameof(After),typeof(ImageSource),typeof(PhotoComparison),new FrameworkPropertyMetadata(null,FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SplitRatioProperty=DependencyProperty.Register(nameof(SplitRatio),typeof(double),typeof(PhotoComparison),new FrameworkPropertyMetadata(.5,FrameworkPropertyMetadataOptions.AffectsRender,null,(_,value)=>double.IsFinite((double)value)?Math.Clamp((double)value,.02,.98):.5));
    public ImageSource? Before {get=>(ImageSource?)GetValue(BeforeProperty);set=>SetValue(BeforeProperty,value);}
    public ImageSource? After {get=>(ImageSource?)GetValue(AfterProperty);set=>SetValue(AfterProperty,value);}
    public double SplitRatio {get=>(double)GetValue(SplitRatioProperty);set=>SetValue(SplitRatioProperty,value);}
    public PhotoComparison()
    {
        ClipToBounds=true;Focusable=true;Cursor=Cursors.SizeWE;ToolTip="Trennlinie ziehen · Links/Rechts: verschieben · Pos1: Mitte";
        MouseLeftButtonDown+=(_,e)=>{if(!ImageRect().Contains(e.GetPosition(this)))return;Focus();CaptureMouse();MoveSplit(e.GetPosition(this));e.Handled=true;};
        MouseMove+=(_,e)=>{if(IsMouseCaptured){MoveSplit(e.GetPosition(this));e.Handled=true;}};
        MouseLeftButtonUp+=(_,e)=>{if(IsMouseCaptured){ReleaseMouseCapture();e.Handled=true;}};
        KeyDown+=(_,e)=>{if(e.Key is Key.Left or Key.Right or Key.Home){SplitRatio=e.Key==Key.Home?.5:SplitRatio+(e.Key==Key.Right?.02:-.02);e.Handled=true;}};
    }
    public Rect ImageRect()
    {
        var image=After??Before;if(image==null||image.Width<=0||image.Height<=0)return Rect.Empty;
        double scale=Math.Min(ActualWidth/image.Width,ActualHeight/image.Height),w=image.Width*scale,h=image.Height*scale;
        return new((ActualWidth-w)/2,(ActualHeight-h)/2,w,h);
    }
    void MoveSplit(Point point){var r=ImageRect();if(!r.IsEmpty&&r.Width>0)SplitRatio=(point.X-r.Left)/r.Width;}
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);var r=ImageRect();if(r.IsEmpty)return;
        dc.DrawImage(Before??After,r);double x=r.Left+r.Width*SplitRatio;
        dc.PushClip(new RectangleGeometry(new Rect(x,r.Top,Math.Max(0,r.Right-x),r.Height)));dc.DrawImage(After??Before,r);dc.Pop();
        dc.DrawLine(new Pen(Brushes.Black,4),new(x,r.Top),new(x,r.Bottom));dc.DrawLine(new Pen(Brushes.DeepSkyBlue,2),new(x,r.Top),new(x,r.Bottom));
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(9,38,56)),new Pen(Brushes.DeepSkyBlue,2),new(x,r.Top+r.Height/2),13,19);
        Label("‹ ›",new(x-10,r.Top+r.Height/2-10));Label("Vorher",new(r.Left+10,r.Top+10));Label("Nachher",new(Math.Max(r.Left+80,r.Right-77),r.Top+10));
        void Label(string text,Point p){var label=new FormattedText(text,CultureInfo.GetCultureInfo("de-DE"),FlowDirection.LeftToRight,new Typeface("Segoe UI"),12,Brushes.White,VisualTreeHelper.GetDpi(this).PixelsPerDip);dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(215,5,20,31)),null,new Rect(p.X-5,p.Y-3,label.Width+10,label.Height+6),4,4);dc.DrawText(label,p);}
    }
}
