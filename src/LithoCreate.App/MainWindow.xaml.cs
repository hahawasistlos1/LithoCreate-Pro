using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LithoCreate.App.ViewModels;
using Microsoft.Win32;
using LithoCreate.Core;

namespace LithoCreate.App;
public sealed class ShapeThumbnailConverter : IValueConverter
{
    static readonly Dictionary<string,BitmapImage> cache=[];
    public object Convert(object value,Type target,object parameter,CultureInfo culture)
    {
        string name=value.ToString()!;if(cache.TryGetValue(name,out var bitmap))return bitmap;
        bitmap=new BitmapImage(new Uri("pack://application:,,,/LithoCreate Pro;component/Assets/"+name+".png"));bitmap.Freeze();cache[name]=bitmap;return bitmap;
    }
    public object ConvertBack(object value,Type target,object parameter,CultureInfo culture)=>throw new NotSupportedException();
}
public sealed class InverseVisibilityConverter : IValueConverter
{
    public object Convert(object value,Type target,object parameter,CultureInfo culture)=>value is true?Visibility.Collapsed:Visibility.Visible;
    public object ConvertBack(object value,Type target,object parameter,CultureInfo culture)=>throw new NotSupportedException();
}
public sealed class InverseBooleanConverter : IValueConverter
{
    public static InverseBooleanConverter Instance {get;}=new();
    public object Convert(object value,Type target,object parameter,CultureInfo culture)=>value is not true;
    public object ConvertBack(object value,Type target,object parameter,CultureInfo culture)=>throw new NotSupportedException();
}
public sealed class StepBrushConverter : IValueConverter
{
    public object Convert(object value,Type target,object parameter,CultureInfo culture)=>new SolidColorBrush((int)value==int.Parse(parameter.ToString()!)?Color.FromRgb(0,111,178):Color.FromRgb(13,32,47));
    public object ConvertBack(object value,Type target,object parameter,CultureInfo culture)=>throw new NotSupportedException();
}
public partial class MainWindow : Window
{
    StudioViewModel? vm;
    Point? cropStart;
    CropRegion cropSelection;
    public MainWindow()
    {
        InitializeComponent();Language=System.Windows.Markup.XmlLanguage.GetLanguage("de-DE");
        vm=new();DataContext=vm;QuickShapes.ItemsSource=vm.OtherShapes.Take(3).Concat(vm.ShadeShapes.Take(2));
        vm.MeshReady+=(mesh,image)=>Viewport.SetMesh(mesh,image);
        vm.PreviewCleared+=Viewport.Clear;
        vm.SectionReady+=section=>SectionView.Section=section;vm.SectionResetRequested+=SectionView.ResetView;
        vm.CameraRequested+=Viewport.View;vm.PngRequested+=SavePng;vm.PrintSetReady+=set=>PrintSetView.PrintSet=set;
        PrintSetView.MoveRequested+=(index,position)=>vm.TryMovePrintSetPart(index,position);PrintSetView.MoveRejected+=message=>vm.Status=message;
        vm.PropertyChanged+=(_,e)=>
        {
            if(vm.IsPrintSetMode&&!vm.InputError&&e.PropertyName==nameof(StudioViewModel.IsPrintSetMode))Dispatcher.BeginInvoke(()=>OptionsScroller.ScrollToTop(),System.Windows.Threading.DispatcherPriority.Loaded);
            if(IsLoaded&&e.PropertyName==nameof(StudioViewModel.Printer))Dispatcher.BeginInvoke(()=>vm.RefreshNumericValidation(HasInvalidInput(this)),System.Windows.Threading.DispatcherPriority.Background);
        };
        Loaded+=(_,_)=>{vm.Recover();var arg=Environment.GetCommandLineArgs().Skip(1).FirstOrDefault();if(arg!=null&&File.Exists(arg)){if(arg.EndsWith(".lithopro",StringComparison.OrdinalIgnoreCase))vm.OpenPath(arg);else vm.LoadImage(arg);}};
        SizeChanged+=(_,_)=>UpdateCompactLayout();UpdateCompactLayout();
        Closing+=OnClosing;
    }
    void UpdateCompactLayout(){bool compact=ActualHeight>0&&ActualHeight<700;EditorTitleRow.Height=new GridLength(compact?0:78);EditorTitlePanel.Visibility=compact?Visibility.Collapsed:Visibility.Visible;}
    void OpenMeshTools(object sender,RoutedEventArgs e){if(vm==null)return;if(vm.Busy||vm.InputError){vm.Status="Zuerst die laufende Berechnung beenden oder die ungültige Eingabe korrigieren.";return;}try{new MeshToolsWindow(vm){Owner=this}.Show();}catch(Exception ex){vm.Status=ex.Message;}}
    void OnEditText(object sender,TextChangedEventArgs e)
    {
        if(!IsLoaded||vm==null||vm.RefreshingBindings||e.OriginalSource is not TextBox box||!box.IsKeyboardFocusWithin)return;
        string? path=box.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path?.Path;
        bool hole=box.DataContext is MountingHole;
        if(!IsProjectPath(path)&&!hole)return;
        int revision=vm.BindingRefreshRevision;
        Dispatcher.BeginInvoke(()=>
        {
            if(vm==null||vm.RefreshingBindings||revision!=vm.BindingRefreshRevision)return;if(hole){vm.RefreshMountingValidation(HasInvalidInput(this));if(!vm.InputError)vm.Changed();return;}vm.RefreshNumericValidation(HasInvalidInput(this),IsPrintLayoutPath(path));
            if(vm.InputError)return;if(path is not ("StraightenAngle" or "AngleX" or "AngleY" or "AngleZ" or "CropAspectWidth" or "CropAspectHeight" or "ModelWidth" or "ModelHeight" or "PrintSetMargin" or "PrintSetGap" or "BodyPositionX" or "BodyPositionY" or "AccessoryPositionX" or "AccessoryPositionY"))vm.Changed();
        },System.Windows.Threading.DispatcherPriority.Background);
    }
    void OnAngleValidation(object sender,ValidationErrorEventArgs e)
    {
        bool layout=IsPrintLayoutPath((e.OriginalSource as TextBox??sender as TextBox)?.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path?.Path);
        if(IsLoaded)Dispatcher.BeginInvoke(()=>vm?.RefreshNumericValidation(HasInvalidInput(this),layout),System.Windows.Threading.DispatcherPriority.Background);
    }
    static bool IsPrintLayoutPath(string? path)=>path is "PrintSetMargin" or "PrintSetGap" or "BodyPositionX" or "BodyPositionY" or "AccessoryPositionX" or "AccessoryPositionY";
    void OnMountingValidation(object sender,ValidationErrorEventArgs e)=>QueueMountingValidation();
    void OnMountingListUpdated(object sender,DataTransferEventArgs e)=>QueueMountingValidation();
    void QueueMountingValidation(){if(IsLoaded)Dispatcher.BeginInvoke(()=>vm?.RefreshMountingValidation(HasInvalidInput(this)),System.Windows.Threading.DispatcherPriority.Background);}
    static bool IsProjectPath(string? path)=>path!=null&&(path is "StraightenAngle" or "AngleX" or "AngleY" or "AngleZ" or "CropAspectWidth" or "CropAspectHeight" or "ModelWidth" or "ModelHeight" or "PrintSetMargin" or "PrintSetGap" or "BodyPositionX" or "BodyPositionY" or "AccessoryPositionX" or "AccessoryPositionY"||path.StartsWith("Model.",StringComparison.Ordinal)||path.StartsWith("Edits.",StringComparison.Ordinal)||path.StartsWith("Printer.",StringComparison.Ordinal));
    void OnSectionValidationError(object sender,ValidationErrorEventArgs e)
    {
        if(e.OriginalSource is TextBox box&&(ReferenceEquals(box,SliceLayerHeightBox)||ReferenceEquals(box,CurrentLayerBox)))
            Dispatcher.BeginInvoke(()=>vm?.SetSectionInputError(Validation.GetHasError(SliceLayerHeightBox)||Validation.GetHasError(CurrentLayerBox)),System.Windows.Threading.DispatcherPriority.Background);
    }
    void OnSliderChanged(object sender,RoutedPropertyChangedEventArgs<double> e){if(IsLoaded&&vm?.RefreshingBindings!=true&&e.OriginalSource is Slider slider&&slider.GetBindingExpression(System.Windows.Controls.Primitives.RangeBase.ValueProperty)?.ParentBinding.Path?.Path is string path&&path!="StraightenAngle"&&IsProjectPath(path)&&(slider.IsMouseCaptureWithin||slider.IsKeyboardFocusWithin)){int revision=vm?.BindingRefreshRevision??0;Dispatcher.BeginInvoke(()=>{if(vm is {RefreshingBindings:false}&&revision==vm.BindingRefreshRevision)vm.Changed();},System.Windows.Threading.DispatcherPriority.Background);}}
    void OnSelectionChanged(object sender,SelectionChangedEventArgs e){if(IsLoaded&&vm?.RefreshingBindings!=true&&e.OriginalSource is ComboBox combo&&combo.IsKeyboardFocusWithin&&!ReferenceEquals(combo,ModelPresetCombo)&&!ReferenceEquals(combo,PrintSetArrangementCombo)&&!ReferenceEquals(combo,PrinterLibraryCombo)&&!ReferenceEquals(combo,EditorPrinterCombo))vm?.Changed();}
    void OnCheckChanged(object sender,RoutedEventArgs e){if(IsLoaded&&vm?.RefreshingBindings!=true&&e.OriginalSource is CheckBox check&&check.IsKeyboardFocusWithin&&(check.DataContext is MountingHole||IsProjectPath(check.GetBindingExpression(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty)?.ParentBinding.Path?.Path)))vm?.Changed();}
    void OnLightChanged(object sender,RoutedPropertyChangedEventArgs<double> e){if(Viewport!=null)Viewport.SetIntensity(e.NewValue);}
    static bool HasInvalidInput(DependencyObject element)
    {
        if(element is TextBox text&&(text.DataContext is MountingHole||IsProjectPath(text.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path?.Path))&&Validation.GetHasError(text))return true;
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(element);i++)if(HasInvalidInput(VisualTreeHelper.GetChild(element,i)))return true;
        return false;
    }
    void OnDrop(object sender,DragEventArgs e)
    {
        if(vm==null||vm.Busy)return;if(e.Data.GetData(DataFormats.FileDrop) is string[] paths&&paths.Length>0){if(paths[0].EndsWith(".lithopro",StringComparison.OrdinalIgnoreCase))vm.OpenPath(paths[0]);else vm.LoadImage(paths[0]);}
    }
    Rect ImageRect()
    {
        if(vm==null)return Rect.Empty;double scale=Math.Min(PhotoSurface.ActualWidth/vm.Original.PixelWidth,PhotoSurface.ActualHeight/vm.Original.PixelHeight);double w=vm.Original.PixelWidth*scale,h=vm.Original.PixelHeight*scale;return new((PhotoSurface.ActualWidth-w)/2,(PhotoSurface.ActualHeight-h)/2,w,h);
    }
    void OnCropDown(object sender,MouseButtonEventArgs e)
    {
        if(vm?.ShowOriginal!=true||vm.InputError||vm.Busy)return;var p=e.GetPosition(PhotoSurface);if(!ImageRect().Contains(p))return;cropStart=p;cropSelection=default;PhotoSurface.Focus();PhotoSurface.CaptureMouse();CropRectangle.Visibility=Visibility.Visible;CropRectangle.Width=CropRectangle.Height=0;e.Handled=true;
    }
    void OnCropMove(object sender,MouseEventArgs e)
    {
        UpdateCropSelection(e.GetPosition(PhotoSurface));
    }
    void OnCropUp(object sender,MouseButtonEventArgs e)
    {
        if(cropStart==null)return;UpdateCropSelection(e.GetPosition(PhotoSurface));var selected=cropSelection;var r=ImageRect();CancelCrop();if(selected.Width*r.Width>8&&selected.Height*r.Height>8)vm?.SetCrop(selected.X,selected.Y,selected.Width,selected.Height);
    }
    void UpdateCropSelection(Point p)
    {
        if(cropStart is not Point start||vm==null||vm.InputError)return;var r=ImageRect();if(r.Width<=0||r.Height<=0)return;
        cropSelection=CropSelection.Drag(vm.Original.PixelWidth,vm.Original.PixelHeight,(start.X-r.X)/r.Width,(start.Y-r.Y)/r.Height,(p.X-r.X)/r.Width,(p.Y-r.Y)/r.Height,vm.CropAspectWidth,vm.CropAspectHeight,vm.CropAspectLock);
        Canvas.SetLeft(CropRectangle,r.X+cropSelection.X*r.Width);Canvas.SetTop(CropRectangle,r.Y+cropSelection.Y*r.Height);CropRectangle.Width=cropSelection.Width*r.Width;CropRectangle.Height=cropSelection.Height*r.Height;
    }
    void CancelCrop(){cropStart=null;cropSelection=default;CropRectangle.Visibility=Visibility.Collapsed;if(PhotoSurface.IsMouseCaptured)PhotoSurface.ReleaseMouseCapture();}
    void OnCropCaptureLost(object sender,MouseEventArgs e)=>CancelCrop();
    void OnCropKey(object sender,KeyEventArgs e){if(e.Key==Key.Escape&&cropStart!=null){CancelCrop();e.Handled=true;}}
    void SavePng()
    {
        bool section=vm?.IsSectionMode==true,printSet=vm?.IsPrintSetMode==true;FrameworkElement target=printSet?PrintSetView:section?SectionView:Viewport;
        var dialog=new SaveFileDialog{Filter="PNG-Bild|*.png",FileName=printSet?"LithoCreate-Drucksatz.png":section?$"LithoCreate-Schicht-{vm!.CurrentLayer}.png":"LithoCreate-Vorschau.png"};if(dialog.ShowDialog()!=true)return;
        try{var bitmap=new RenderTargetBitmap((int)target.ActualWidth*2,(int)target.ActualHeight*2,192,192,PixelFormats.Pbgra32);bitmap.Render(target);File.WriteAllBytes(dialog.FileName,Services.ImageProcessor.Png(bitmap));if(vm!=null)vm.Status=(printSet?"Bettübersicht":section?"Druckschicht":"3D-Vorschau")+" als PNG gespeichert: "+dialog.FileName;}catch(Exception ex){MessageBox.Show(ex.Message,"PNG-Export");}
    }
    void OnClosing(object? sender,CancelEventArgs e)
    {
        if(vm==null)return;if(vm.Busy){MessageBox.Show("Bitte die laufende Berechnung zuerst abbrechen oder abwarten.","Berechnung läuft");e.Cancel=true;return;}if(!vm.ConfirmDiscard()){e.Cancel=true;return;}vm.CleanShutdown();
    }
}
