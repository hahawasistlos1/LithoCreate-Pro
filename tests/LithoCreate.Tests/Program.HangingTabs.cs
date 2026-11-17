using System.IO;
using System.IO.Compression;
using System.Text.Json;
using LithoCreate.Core;
using LithoCreate.App.Services;
using LithoCreate.App.ViewModels;

partial class Program
{
    static ModelSettings TabFixture(string name="Flat")=>new()
    {
        Shape=name=="Frame"?Shape.Frame:Shape.Flat,Width=name=="Edge"?40:80,Height=name=="Edge"?30:60,
        MinThickness=3,MaxThickness=3,FrameWidth=name=="Edge"?1.2:3,Resolution=1,HangingTabsEnabled=true,
        TabWidth=name=="Edge"?6:12,TabHeight=name=="Edge"?20:14,TabHoleDiameter=name=="Edge"?3:4,TabInset=name=="Edge"?0:8,
        StandEnabled=name=="Stand",StandHeight=5,StandDepth=25,PrintOrientation=PrintOrientation.FaceUp
    };
    static void TestHangingTabs()
    {
        foreach(string name in new[]{"Flat","Frame","Stand","Edge"})Test("Aufhängelaschen: verbundene Geometrie und Dateien · "+name,()=>
        {
            var s=TabFixture(name);foreach(bool preview in new[]{true,false})
            {
                var mesh=MeshGenerator.Generate(new(1,1,[255]),s,preview);var report=mesh.Validate();Require(report.IsValid&&Euler(mesh)==-2,"Laschen nicht als zwei Durchgänge mit Körper verbunden: "+report);
                double y=s.Height+(s.StandEnabled?s.StandHeight:0),cx=-s.Width/2+s.TabInset+s.TabWidth/2;
                Require(VerticalIntersections(mesh,cx+.031,-y-s.TabHeight/2+.027)==0&&VerticalIntersections(mesh,cx+s.TabHoleDiameter/2+1,-y-s.TabHeight/2+.031)==2,"Bohrung verschlossen oder Material fehlt.");
                Require(Math.Abs(report.Size.X-s.Width)<.001&&Math.Abs(report.Size.Z-(y+s.TabHeight))<.001&&Math.Abs(report.Size.Y-(s.StandEnabled?s.StandDepth:4))<.001,"Außenmaße oder Fotohöhe verändert.");
                Require(mesh.PhotoVertices.Any(p=>!p)&&mesh.PhotoVertices.Any(p=>p),"Laschen-/Fotoflächen fehlen.");if(!preview)CheckRoundtrip(mesh,"Tabs-"+name);
            }
        });
        Test("Aufhängelaschen: freier Lochquerschnitt und Mindestmaterial",()=>
        {
            foreach(bool preview in new[]{true,false})
            {
                var s=TabFixture();s.PrintOrientation=PrintOrientation.Upright;var m=MeshGenerator.Generate(new(1,1,[255]),s,preview);double cx=-s.Width/2+s.TabInset+s.TabWidth/2,cy=s.Height+s.TabHeight/2;double free=double.MaxValue;
                foreach(var t in m.Triangles)foreach(var edge in new[]{(t.A,t.B),(t.B,t.C),(t.C,t.A)})
                {
                    var a=m.Vertices[edge.Item1];var b=m.Vertices[edge.Item2];if(Math.Abs(a.Z)>1e-6||Math.Abs(b.Z)>1e-6||Math.Abs(a.X-cx)>3||Math.Abs(b.X-cx)>3||Math.Abs(a.Y-cy)>3||Math.Abs(b.Y-cy)>3)continue;
                    double x=a.X-cx,y=a.Y-cy,dx=b.X-a.X,dy=b.Y-a.Y,t0=Math.Clamp(-(x*dx+y*dy)/(dx*dx+dy*dy),0,1);free=Math.Min(free,Math.Sqrt(Math.Pow(x+t0*dx,2)+Math.Pow(y+t0*dy,2)));
                }
                Require(2*free>=s.TabHoleDiameter-.0001&&2*free<s.TabHoleDiameter+.01,"Freier Durchmesser unterschritten oder Form zu grob.");
                var layer=new CrossSectionIndex(m).Slice(cy);Require(layer.IsRegular&&layer.ClosedContours==4,"Querschnitt durch beide Bohrungen hat falsche Materialinseln.");
            }
        });
        Test("Aufhängelaschen: Grenzen, inaktive Formen und Abbruch",()=>
        {
            foreach(Action<ModelSettings> change in new Action<ModelSettings>[]{s=>s.TabWidth=5,s=>s.TabHeight=41,s=>s.TabHoleDiameter=1,s=>s.TabHoleDiameter=11,s=>s.TabInset=-1,s=>s.TabInset=30,s=>s.FrameWidth=1,s=>s.TabHeight=double.NaN}){var s=TabFixture();change(s);Throws<InvalidDataException>(s.Validate);}
            foreach(var shape in SingleBodyShapes().Where(t=>t is not (Shape.Flat or Shape.Frame)))
            {
                var s=new ModelSettings{Shape=shape,Width=48,Height=36,Diameter=40,Radius=40,Resolution=2};var before=MeshGenerator.Generate(new(1,1,[255]),s,false);s.HangingTabsEnabled=true;s.TabWidth=100;var after=MeshGenerator.Generate(new(1,1,[255]),s,false);Require(before.Vertices.SequenceEqual(after.Vertices)&&before.Triangles.SequenceEqual(after.Triangles),"Fremde Form verändert: "+shape);
            }
            using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>MeshGenerator.Generate(new(1,1,[255]),TabFixture(),false,cts.Token));
        });
        Test("Aufhängelaschen: Varianten, Raster und Druckwinkel",()=>
        {
            for(int i=0;i<16;i++)
            {
                var s=TabFixture();s.Width=70+i*3.7;s.Height=30+i*2.1;s.TabWidth=6+i*.7;s.TabHeight=7+i*.4;s.TabHoleDiameter=2;s.TabInset=i%3;s.Resolution=i%2==0?.7:2.3;s.Angles=new(13,-17,28);var mesh=MeshGenerator.Generate(new(2,1,[0,255]),s,i%2==0);Require(mesh.Validate().IsValid&&Euler(mesh)==-2,"Rastervariante ungültig: "+i);
            }
        });
        Test("Aufhängelaschen: nahezu deckungsgleiche Anschlusslinien",()=>
        {
            foreach(double shift in new[]{.0000001,-.0000001,.000001,-.000001,.00001,-.00001})
            {
                var s=TabFixture();s.TabInset+=shift;var mesh=MeshGenerator.Generate(new(1,1,[255]),s,false);Require(mesh.Validate().IsValid&&Euler(mesh)==-2,"Fast deckungsgleiche Rasterlinien erzeugen degenerierte Flächen: "+shift);
                s.TabInset=8;s.TabHeight=12+shift;s.Height=999;s.Resolution=2;mesh=MeshGenerator.Generate(new(1,1,[255]),s,false);Require(mesh.Validate().IsValid&&Euler(mesh)==-2,"Fast deckungsgleiche Rechteckecken verlieren den Anschluss bei großer Fotohöhe: "+shift);
            }
        });
        Test("Format 12 ohne Laschen; Format 13 und Vorlagen mit Laschen",()=>
        {
            var data=new ProjectData{Version=12,Model=TabFixture()};byte[] image=ImageProcessor.Png(ImageProcessor.Demo());string old=Path.Combine(output,"tabs-legacy-v12.lithopro");if(File.Exists(old))File.Delete(old);
            using(var zip=ZipFile.Open(old,ZipArchiveMode.Create)){using(var meta=zip.CreateEntry("project.json").Open())JsonSerializer.Serialize(meta,data);using var src=zip.CreateEntry("source.bin").Open();src.Write(image);}
            var read=ProjectStore.Load(old);Require(read.Data.Version==15&&!read.Data.Model.HangingTabsEnabled&&read.Image.SequenceEqual(image),"Migration aktiviert Laschen oder verliert Foto.");var noTabs=data.Model.Clone();noTabs.HangingTabsEnabled=false;Require(MeshGenerator.Generate(new(1,1,[255]),noTabs,false).Vertices.SequenceEqual(MeshGenerator.Generate(new(1,1,[255]),read.Data.Model,false).Vertices),"Alte Geometrie verändert.");
            string next=Path.Combine(output,"tabs-project.lithopro");ProjectStore.Save(next,data,image);read=ProjectStore.Load(next);Require(read.Data.Model.HangingTabsEnabled&&JsonSerializer.Serialize(read.Data)==JsonSerializer.Serialize(data)&&read.Image.SequenceEqual(image),"Laschenprojekt verliert Parameter.");
            string presets=Path.Combine(output,"tabs-presets.json");var item=new ModelPreset{Name="Aufhängung",Model=TabFixture()};File.WriteAllText(presets,JsonSerializer.Serialize(new{Version=1,ModelVersion=12,Items=new[]{item}}));var migrated=ModelPresetStore.Load(presets);Require(migrated.ModelVersion==13&&!migrated.Items[0].Model.HangingTabsEnabled&&migrated.Items[0].Id==item.Id,"Alte Vorlage erhält ungefragt Laschen.");ModelPresetStore.Save(presets,[item]);Require(ModelPresetStore.Load(presets).Items[0].Model.HangingTabsEnabled,"Neue Vorlage verliert Laschen.");
        });
    }
    static void RenderTabsInterface(LithoCreate.App.MainWindow window,StudioViewModel vm,Action<string> capture)
    {
        vm.Project.Model=TabFixture();vm.Model.PrintOrientation=PrintOrientation.Upright;vm.Page="Editor";vm.Step=3;vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Flat);
        foreach(var e in VisualChildren(window).OfType<System.Windows.Controls.Expander>().Where(e=>Equals(e.Header,"Standfuß")))e.IsExpanded=false;
        var panel=(System.Windows.Controls.Expander)window.FindName("HangingTabsPanel");panel.IsExpanded=true;var hole=(System.Windows.Controls.TextBox)window.FindName("TabHoleBox");hole.BringIntoView();var ready=vm.UpdatePreview();PumpUntil(()=>ready.IsCompleted);vm.Camera.Execute("front");capture("Laschen-Flach");
        vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Frame);var frame=vm.UpdatePreview();PumpUntil(()=>frame.IsCompleted);capture("Laschen-Rahmen");
        vm.SelectedPlacement=vm.Placements.First(t=>t.Orientation==PrintOrientation.FaceUp);var flat=vm.UpdatePreview();PumpUntil(()=>flat.IsCompleted);vm.IsSectionMode=true;vm.CurrentLayer=12;var slice=vm.UpdateSection();PumpUntil(()=>slice.IsCompleted);Require(vm.CurrentSection is {IsRegular:true,ClosedContours:3},"Schichtansicht verliert Laschenbohrungen.");capture("Laschen-Schicht");vm.IsSectionMode=false;
        hole.BringIntoView();hole.Focus();hole.Text="abc";PumpUntil(()=>vm.InputError&&vm.PreviewMesh==null);Require(!vm.ExportStl.CanExecute(null),"Ungültige Laschenbohrung kann exportiert werden.");capture("Laschen-Ungueltig");hole.Text="20";PumpUntil(()=>!vm.InputError);var range=vm.UpdatePreview();PumpUntil(()=>range.IsCompleted);Require(vm.PreviewMesh==null&&!vm.Export3Mf.CanExecute(null),"Zu große Bohrung kann exportiert werden.");capture("Laschen-Grenzwert");hole.Text="5";PumpUntil(()=>!vm.InputError&&vm.PreviewMesh!=null);capture("Laschen-Korrigiert");
        vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Cylinder);var cylinder=vm.UpdatePreview();PumpUntil(()=>cylinder.IsCompleted);Require(panel.Visibility==System.Windows.Visibility.Collapsed&&vm.PreviewMesh!=null,"Inaktive Laschen blockieren andere Form.");capture("Laschen-Andere-Form");
        vm.Project.Model=new(){Width=120,Height=88,Resolution=1,LockDimensions=true};vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Flat);panel.IsExpanded=false;
    }
}
