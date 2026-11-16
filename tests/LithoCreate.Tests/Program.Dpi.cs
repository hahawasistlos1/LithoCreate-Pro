using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LithoCreate.App;
using LithoCreate.App.Services;
using LithoCreate.App.ViewModels;
using LithoCreate.Core;

partial class Program
{
    static int RenderDpi(string directory)
    {
        Directory.CreateDirectory(directory);Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",Path.Combine(Path.GetFullPath(directory),"data"));
        var app=new Application();app.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("pack://application:,,,/LithoCreate Pro;component/Theme.xaml")});var window=new MainWindow();window.Show();var vm=(StudioViewModel)window.DataContext;var load=vm.UpdatePreview();PumpUntil(()=>load.IsCompleted);var results=new List<object>();string fixture=Path.Combine(Path.GetFullPath(directory),"dpi-import.stl");ModelExporter.Export(fixture,MeshGenerator.Generate(new(1,1,[255]),new(){Width=40,Height=30,MinThickness=3,MaxThickness=3,FrameWidth=0,Resolution=2},false));
        foreach(double scale in new[]{1.25,1.5,2})
        {
            window.Width=1920/scale;window.Height=1080/scale;
            foreach(var view in new[]{("Home",1),("Editor",1),("Editor",3),("Editor",5),("Templates",1),("Settings",1)})
            {
                vm.Page=view.Item1;vm.Step=view.Item2;window.UpdateLayout();Require(window.ActualWidth<=1920/scale+1&&window.ActualHeight<=1080/scale+1,"Mindestfenster größer als simuliertes 1080p-Arbeitsfeld.");Capture(window,$"DPI-{scale*100:0}-{view.Item1}-{view.Item2}",scale);results.Add(new{Scale=scale,Page=view.Item1,Step=view.Item2,Width=window.ActualWidth,Height=window.ActualHeight});
                if(vm.IsEditor){var scroll=(ScrollViewer)window.FindName("OptionsScroller");Require(scroll.ViewportHeight>100&&scroll.ScrollableHeight>=0,"Eingabepanel bei hoher Skalierung unbenutzbar.");if(view.Item2==3)Require(((System.Windows.FrameworkElement)window.FindName("Viewport")).ActualHeight>=120,"3D-Vorschau bei 200 % zu klein.");}
            }
            var shadePanel=(FrameworkElement)window.FindName("LampShadePanel");
            var shadeMount=(ComboBox)window.FindName("ShadeMountCombo");
            foreach (var shade in vm.ShadeShapes)
            {
                vm.SelectTemplate.Execute(shade);
                var preview=vm.UpdatePreview();
                PumpUntil(()=>preview.IsCompleted);
                window.UpdateLayout();
                Require(shadePanel.IsVisible && vm.PreviewMesh != null &&
                    Equals(shadeMount.SelectedValue,vm.Model.ShadeMount) &&
                    Math.Abs(vm.PreviewMesh.Bounds().Max.Y - vm.Model.ShadeHeight) < .001,
                    "Lampenschirm ist im Editor nicht korrekt sichtbar oder ausgewählt.");
                ((ScrollViewer)window.FindName("OptionsScroller")).ScrollToTop();
                Capture(window,$"DPI-{scale*100:0}-{shade.Shape}",scale);
            }
            vm.SelectedShape=vm.OtherShapes.First();
            var tools=new MeshToolsWindow(vm){Owner=window,Width=1920/scale,Height=1080/scale};tools.Show();var import=tools.ImportFile(fixture);PumpUntil(()=>import.IsCompleted);Require(((Button)tools.FindName("ExportButton")).IsEnabled,"Importierter Körper nicht exportierbar.");Capture(tools,$"DPI-{scale*100:0}-STL",scale);
            var combo=(ComboBox)tools.FindName("MountChoice");var button=(Button)tools.FindName("MountButton");
            foreach(int selection in new[]{0,2,3,4,5,6}){combo.SelectedIndex=selection;button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));PumpUntil(()=>((Button)tools.FindName("ExportButton")).IsEnabled);Capture(tools,$"DPI-{scale*100:0}-Halter-{selection}",scale);}
            tools.Close();
        }
        File.WriteAllText(Path.Combine(directory,"results.json"),JsonSerializer.Serialize(new{Result="PASS",Kind="WPF-Raster-/Layoutsimulation; keine Änderung der OS-DPI",PhysicalCanvas=new[]{1920,1080},Views=results},ProjectStore.Json));vm.Dirty=false;window.Close();Console.WriteLine($"PASS: {Directory.GetFiles(directory,"*.png").Length} Renderprüfungen für 125/150/200 % auf simuliertem 1920×1080-Arbeitsfeld.");return 0;
        void Capture(Window target,string name,double scale){target.UpdateLayout();System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);target.UpdateLayout();var bitmap=new RenderTargetBitmap((int)Math.Ceiling(target.ActualWidth*scale),(int)Math.Ceiling(target.ActualHeight*scale),96*scale,96*scale,PixelFormats.Pbgra32);bitmap.Render(target);File.WriteAllBytes(Path.Combine(directory,name+".png"),ImageProcessor.Png(bitmap));}
    }
}
