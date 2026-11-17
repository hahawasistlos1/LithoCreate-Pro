using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
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
        var app=new Application();app.Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("pack://application:,,,/LithoCreate Pro;component/Theme.xaml")});
        Rect primaryWork = SystemParameters.WorkArea;
        var window=new MainWindow
        {
            WindowStartupLocation=WindowStartupLocation.Manual,
            Left=primaryWork.Left-120, Top=primaryWork.Top-90,
            Width=primaryWork.Width+330, Height=primaryWork.Height+240
        };
        window.Show();
        var vm=(StudioViewModel)window.DataContext;var load=vm.UpdatePreview();PumpUntil(()=>load.IsCompleted);
        nint windowHandle = new WindowInteropHelper(window).Handle;
        Require(WindowWorkArea.TryReadBounds(windowHandle, out Rect work, out Rect frame) && work.Contains(frame),
            "Das übergroße Startfenster liegt nicht vollständig in der Arbeitsfläche.");
        var results=new List<object>();string fixture=Path.Combine(Path.GetFullPath(directory),"dpi-import.stl");ModelExporter.Export(fixture,MeshGenerator.Generate(new(1,1,[255]),new(){Width=40,Height=30,MinThickness=3,MaxThickness=3,FrameWidth=0,Resolution=2},false));
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
            var shadeMountStyle=(ComboBox)window.FindName("ShadeMountStyleCombo");
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
                if (shade.Shape == Shape.CeilingShade)
                {
                    Require(shadeMountStyle.IsVisible && Equals(shadeMountStyle.SelectedValue, ShadeMountStyle.Spokes),
                        "Die Schirmvorlage zeigt nicht den offenen Strebenring an.");
                    shadeMountStyle.SetCurrentValue(ComboBox.SelectedValueProperty, ShadeMountStyle.Plate);
                    Require(vm.Model.ShadeMountStyle == ShadeMountStyle.Plate, "Die Auswahl der Platte kommt im Modell nicht an.");
                    shadeMountStyle.SetCurrentValue(ComboBox.SelectedValueProperty, ShadeMountStyle.Spokes);
                    Require(vm.Model.ShadeMountStyle == ShadeMountStyle.Spokes, "Die Auswahl der Streben kommt im Modell nicht an.");
                    var opening = (TextBox)window.FindName("ShadeMountDiameterBox");
                    foreach (var (name, diameter) in new[] { ("ShadeE14Button", 28.5), ("ShadeE27Button", 40.5) })
                    {
                        var presetButton = (Button)window.FindName(name);
                        presetButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Require(vm.Model.ShadeMountDiameter == diameter &&
                            double.Parse(opening.Text, System.Globalization.CultureInfo.GetCultureInfo("de-DE")) == diameter,
                            "Fassungsvorgabe kommt nicht im Lochmaß und im Modell an.");
                        var updated = vm.UpdatePreview();
                        PumpUntil(() => updated.IsCompleted);
                        Require(vm.PreviewMesh != null, "Vorschau für das neue Lochmaß fehlt.");
                        var cut = new CrossSectionIndex(vm.PreviewMesh!).Slice(vm.Model.ShadeHeight - .613);
                        var bore = cut.Paths.MinBy(path => path.Points.Max(p => p.LengthSquared()))!;
                        double actual = bore.Points.Select((point, i) =>
                            DistanceToEdge(point, bore.Points[(i + 1) % bore.Points.Count])).Min() * 2;
                        Require(cut.ClosedContours == 5 && Math.Abs(actual - diameter) < .01,
                            "Das Vorschaumodell hat eine andere Ringöffnung.");
                    }
                    var spokes = vm.UpdatePreview();
                    PumpUntil(() => spokes.IsCompleted);
                    vm.Camera.Execute("top");
                    opening.BringIntoView();
                    Capture(window,$"DPI-{scale*100:0}-Strebenring",scale);
                    vm.Camera.Execute("reset");
                }
            }
            vm.SelectedShape=vm.OtherShapes.First();
            var tools=new MeshToolsWindow(vm){Owner=window,Width=1920/scale,Height=1080/scale};tools.Show();var import=tools.ImportFile(fixture);PumpUntil(()=>import.IsCompleted);Require(((Button)tools.FindName("ExportButton")).IsEnabled,"Importierter Körper nicht exportierbar.");Capture(tools,$"DPI-{scale*100:0}-STL",scale);
            var combo=(ComboBox)tools.FindName("MountChoice");var button=(Button)tools.FindName("MountButton");
            foreach(int selection in new[]{0,2,3,4,5,6}){combo.SelectedIndex=selection;button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));PumpUntil(()=>((Button)tools.FindName("ExportButton")).IsEnabled);Capture(tools,$"DPI-{scale*100:0}-Halter-{selection}",scale);}
            tools.Close();

            var insertTools = new MeshToolsWindow(vm, showInserts: true) { Owner = window, Width = 1920 / scale, Height = 1080 / scale };
            insertTools.Show();
            var insertPanel = (Expander)insertTools.FindName("LightInsertPanel");
            var insertChoice = (ComboBox)insertTools.FindName("InsertChoice");
            var buildInsert = (Button)insertTools.FindName("BuildInsertButton");
            var exportInsert = (Button)insertTools.FindName("ExportButton");
            Require(insertPanel.IsExpanded && insertChoice.IsVisible, "Lichteinsätze sind nicht direkt erreichbar.");
            foreach (int selection in new[] { 0, 4 })
            {
                insertChoice.SelectedIndex = selection;
                Require(!exportInsert.IsEnabled, "Veralteter Lichteinsatz kann nach einem Wechsel exportiert werden.");
                buildInsert.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                PumpUntil(() => exportInsert.IsEnabled);
                buildInsert.BringIntoView();
                Capture(insertTools, $"DPI-{scale*100:0}-Lichteinsatz-{selection}", scale);
            }
            insertTools.Close();
        }
        File.WriteAllText(Path.Combine(directory,"results.json"),JsonSerializer.Serialize(new{Result="PASS",Kind="WPF-Raster-/Layoutsimulation; keine Änderung der OS-DPI",PhysicalCanvas=new[]{1920,1080},Views=results},ProjectStore.Json));vm.Dirty=false;window.Close();Console.WriteLine($"PASS: {Directory.GetFiles(directory,"*.png").Length} Renderprüfungen für 125/150/200 % auf simuliertem 1920×1080-Arbeitsfeld.");return 0;
        void Capture(Window target,string name,double scale){target.UpdateLayout();System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);target.UpdateLayout();var bitmap=new RenderTargetBitmap((int)Math.Ceiling(target.ActualWidth*scale),(int)Math.Ceiling(target.ActualHeight*scale),96*scale,96*scale,PixelFormats.Pbgra32);bitmap.Render(target);File.WriteAllBytes(Path.Combine(directory,name+".png"),ImageProcessor.Png(bitmap));}
    }
}
