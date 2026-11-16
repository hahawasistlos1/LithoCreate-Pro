using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using LithoCreate.Core;
using LithoCreate.App.ViewModels;
using LithoCreate.App.Services;

partial class Program
{
    static PrintSetOptions ManualLayout()=>new(){Arrangement=PrintArrangement.Manual,BodyX=10,BodyY=10,AccessoryX=65,AccessoryY=50};
    static void TestManualPrintLayout()
    {
        Test("Freie Platzierung: gespeicherte Positionen und unveränderte Drucklagen",()=>
        {
            var set=PrintSetReference(options:ManualLayout());set.Validate();Require(set.Arrangement==PrintArrangement.Manual&&set.Parts[0].Position==new Vector2(10,10)&&set.Parts[1].Position==new Vector2(65,50),"Manuelle Positionen nicht übernommen.");
            var moved=set.WithPosition(0,new(15,15)).WithPosition(1,new(70,60));moved.Validate();Require(set.Parts[0].Position==new Vector2(10,10)&&set.Parts[1].Position==new Vector2(65,50),"Verschieben verändert den Ausgangssatz.");
            for(int i=0;i<2;i++)Require(ReferenceEquals(set.Parts[i].Mesh,moved.Parts[i].Mesh)&&set.Parts[i].Report==moved.Parts[i].Report,"Verschieben baut Geometrie neu auf oder ändert Maße.");
            PrintSetExporter.Export(Path.Combine(output,"Manual-Original.3mf"),set);PrintSetExporter.Export(Path.Combine(output,"Manual-Moved.3mf"),moved);var read=PrintSetExporter.Read(Path.Combine(output,"Manual-Moved.3mf"));Require(read.Select(p=>p.Position).SequenceEqual(moved.Parts.Select(p=>p.Position)),"Export verliert Verschiebungen.");
        });
        Test("Freie Platzierung: exakter Rand und Mindestabstand",()=>
        {
            var set=PrintSetReference(options:ManualLayout()).WithPositions(new(5,5),new(75,65));set.Validate();PrintSetExporter.Export(Path.Combine(output,"Manual-Edge.3mf"),set);
            var gap=set.WithPositions(new(5,5),new(50,5));gap.Validate();Throws<InvalidDataException>(()=>gap.WithPosition(1,new(49.99f,5)));Throws<InvalidDataException>(()=>set.WithPosition(0,new(4.99f,5)));Throws<InvalidDataException>(()=>set.WithPosition(1,new(75.01f,65)));Throws<InvalidDataException>(()=>set.WithPosition(1,new(75,65.01f)));
        });
        Test("Freie Platzierung: Überlappung, ungültige Zahlen und Struktur",()=>
        {
            var set=PrintSetReference(options:ManualLayout());foreach(var p in new[]{new Vector2(10,10),new(-1,5),new(float.NaN,10),new(10,float.PositiveInfinity)})Throws<InvalidDataException>(()=>set.WithPosition(1,p));
            Throws<InvalidDataException>(()=>set.WithPosition(-1,new(10,10)));Throws<InvalidDataException>(()=>set.WithPosition(2,new(10,10)));Throws<InvalidDataException>(()=>(set with{Parts=[]}).WithPositions(new(10,10),new(60,50)));Require(set.Parts[1].Position==new Vector2(65,50),"Abgelehnte Bewegung verändert letzte gültige Position.");
            foreach(double v in new[]{-.01,2000.01,double.NaN,double.PositiveInfinity})foreach(int index in Enumerable.Range(0,4)){var o=ManualLayout();if(index==0)o.BodyX=v;else if(index==1)o.BodyY=v;else if(index==2)o.AccessoryX=v;else o.AccessoryY=v;Throws<InvalidDataException>(o.Validate);}
        });
        Test("Freie Platzierung: Geometrie bleibt vor Export vollständig geprüft",()=>
        {
            var set=PrintSetReference(options:ManualLayout()).WithPosition(0,new(15,15));string file=Path.Combine(output,"manual-invalid.3mf");File.WriteAllText(file,"erhalten");set.Parts[0].Mesh.Triangles.RemoveAt(0);set.ValidatePlacement();Throws<InvalidDataException>(()=>PrintSetExporter.Export(file,set));Require(File.ReadAllText(file)=="erhalten","Ungültiges Netz überschreibt vorhandenen Export.");
        });
        Test("Freie Platzierung: Automatikpositionen ausdrücklich übernehmen",()=>
        {
            var automatic=PrintSetReference();var manual=automatic.WithPositions(automatic.Parts[0].Position,automatic.Parts[1].Position);Require(manual.Arrangement==PrintArrangement.Manual&&manual.Parts.Select(p=>p.Position).SequenceEqual(automatic.Parts.Select(p=>p.Position)),"Automatische Positionen springen beim Wechsel.");PrintSetExporter.Export(Path.Combine(output,"Manual-FromAuto.3mf"),manual);
        });
        Test("Format 15: freie Positionen und Originalfoto bleiben erhalten",()=>
        {
            var data=new ProjectData{Name="Manuell",Model=PrintSetFixture(false),PrintLayout=ManualLayout()};byte[] source=ImageProcessor.Png(ImageProcessor.Demo());string path=Path.Combine(output,"manual-project.lithopro");ProjectStore.Save(path,data,source);var read=ProjectStore.Load(path);Require(read.Data.Version==15&&JsonSerializer.Serialize(data)==JsonSerializer.Serialize(read.Data)&&source.SequenceEqual(read.Image),"Format 15 verliert Positionen oder Originalfoto.");
            var next=data.PrintLayout.Clone();next.BodyX=2001;data.PrintLayout=next;byte[] before=File.ReadAllBytes(path);Throws<InvalidDataException>(()=>ProjectStore.Save(path,data,source));Require(before.SequenceEqual(File.ReadAllBytes(path)),"Ungültiges Projekt überschreibt letzte Datei.");
        });
        Test("Format 14: Abstände bleiben, neue XY-Felder werden ignoriert",()=>
        {
            foreach(var arrangement in new[]{PrintArrangement.Row,PrintArrangement.Column,PrintArrangement.Manual})
            {
                var data=new ProjectData{Version=14,PrintLayout=new(){Margin=12,Gap=9,Arrangement=arrangement,BodyX=999,BodyY=999,AccessoryX=999,AccessoryY=999}};string path=Path.Combine(output,$"manual-legacy-{arrangement}.lithopro");if(File.Exists(path))File.Delete(path);
                using(var zip=ZipFile.Open(path,ZipArchiveMode.Create)){using(var meta=zip.CreateEntry("project.json").Open())JsonSerializer.Serialize(meta,data);using var src=zip.CreateEntry("source.bin").Open();src.Write(ImageProcessor.Png(ImageProcessor.Demo()));}
                var read=ProjectStore.Load(path).Data;Require(read.Version==15&&read.PrintLayout.Margin==12&&read.PrintLayout.Gap==9&&read.PrintLayout.Arrangement==(arrangement==PrintArrangement.Manual?PrintArrangement.Automatic:arrangement)&&read.PrintLayout.BodyX==5&&read.PrintLayout.BodyY==5&&read.PrintLayout.AccessoryX==100&&read.PrintLayout.AccessoryY==5,"Migration verändert Abstände oder übernimmt nicht unterstützte XY-Felder.");
            }
        });
        Test("Editor: Verschieben aktualisiert nur Positionen und Export",()=>WithPresetVm("manual",vm=>
        {
            vm.Project.Model=PrintSetFixture(false);vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);var build=vm.BuildPrintSet();PumpUntil(()=>build.IsCompleted);Require(vm.HasPrintSet,"Ausgangssatz fehlt.");var before=vm.PreparedPrintSet!;string model=JsonSerializer.Serialize(vm.Model),edits=JsonSerializer.Serialize(vm.Edits),printer=JsonSerializer.Serialize(vm.Printer);var image=vm.Original;vm.Dirty=false;
            var target=before.Parts[0].Position-new Vector2(10,10);Require(vm.TryMovePrintSetPart(0,target)&&vm.IsManualPrintSet&&vm.Dirty&&vm.HasPrintSet&&vm.ExportPrintSet.CanExecute(null),"Verschieben sperrt Export oder verliert Dirty.");Require(vm.PreparedPrintSet!.Parts[1].Position==before.Parts[1].Position&&vm.AccessoryPositionX==before.Parts[1].Position.X&&vm.AccessoryPositionY==before.Parts[1].Position.Y,"Automatische zweite Position nicht übernommen.");
            vm.BodyPositionY+=1;Require(vm.PreparedPrintSet.Parts[0].Position==target+new Vector2(0,1)&&ReferenceEquals(before.Parts[0].Mesh,vm.PreparedPrintSet.Parts[0].Mesh),"Zahlenwert verwirft Netz oder Position.");var valid=vm.PreparedPrintSet;Require(!vm.TryMovePrintSetPart(1,valid.Parts[0].Position)&&ReferenceEquals(valid,vm.PreparedPrintSet),"Ungültige Bewegung übernommen.");Throws<InvalidDataException>(()=>vm.BodyPositionX=-1);Require(ReferenceEquals(valid,vm.PreparedPrintSet),"Ungültige Zahl verändert Satz.");
            Require(model==JsonSerializer.Serialize(vm.Model)&&edits==JsonSerializer.Serialize(vm.Edits)&&printer==JsonSerializer.Serialize(vm.Printer)&&ReferenceEquals(image,vm.Original),"Verschieben verändert Modell, Foto oder Drucker.");
            vm.SelectedPrintSetArrangement=PrintSetOptions.Choices[0];Require(!vm.HasPrintSet&&!vm.IsManualPrintSet,"Rückkehr zur Automatik behält manuellen Export.");build=vm.BuildPrintSet();PumpUntil(()=>build.IsCompleted);var auto=vm.PreparedPrintSet!;vm.SelectedPrintSetArrangement=PrintSetOptions.Choices[3];Require(vm.HasPrintSet&&vm.IsManualPrintSet&&vm.PreparedPrintSet!.Parts.Select(p=>p.Position).SequenceEqual(auto.Parts.Select(p=>p.Position)),"Ausdrücklicher Wechsel verschiebt Teile.");
        }));
    }
    static void RenderManualPrintLayout(LithoCreate.App.MainWindow window,StudioViewModel vm,Action<string> capture)
    {
        vm.Project.Model=PrintSetFixture(false);vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);vm.Page="Editor";vm.Step=3;vm.Project.PrintLayout=new(){Margin=10,Gap=8,Arrangement=PrintArrangement.Manual,BodyX=20,BodyY=25,AccessoryX=120,AccessoryY=100};vm.Changed();var build=vm.BuildPrintSet();PumpUntil(()=>build.IsCompleted);Require(vm.HasPrintSet,"Manueller UI-Satz fehlt.");
        var x=(System.Windows.Controls.TextBox)window.FindName("BodyPositionXBox");x.BringIntoView();capture("Position-Manuell");Require(vm.TryMovePrintSetPart(0,new(50,45)),"UI-Verschieben abgelehnt.");capture("Position-Verschoben");Require(vm.TryMovePrintSetPart(1,new(130,110)),"UI-Zubehörbewegung abgelehnt.");capture("Position-Zubehoer");
        x.BringIntoView();x.Focus();x.Text="55,5";PumpUntil(()=>vm.BodyPositionX==55.5);Require(vm.HasPrintSet&&vm.PreparedPrintSet!.Parts[0].Position.X==55.5f,"Zahlenbindung verwirft geprüften Satz.");capture("Position-Zahlenwert");
        x.Text="abc";PumpUntil(()=>vm.InputError&&!vm.HasPrintSet);Require(!vm.ExportPrintSet.CanExecute(null),"Fehlerhafte Position lässt Export zu.");capture("Position-Ungueltig");x.Text="-1";PumpUntil(()=>vm.InputError);Require(vm.BodyPositionX==55.5,"Negativer Zahlenwert übernommen.");capture("Position-Grenzwert");
        x.Text="210";PumpUntil(()=>!vm.InputError);build=vm.BuildPrintSet();PumpUntil(()=>build.IsCompleted);Require(!vm.HasPrintSet&&!vm.ExportPrintSet.CanExecute(null),"Position außerhalb des Betts lässt Export zu.");capture("Position-Ausserhalb");x.Text="55,5";PumpUntil(()=>!vm.InputError&&vm.BodyPositionX==55.5);build=vm.BuildPrintSet();PumpUntil(()=>build.IsCompleted);Require(vm.HasPrintSet&&vm.IsManualPrintSet,"Korrektur stellt Satz nicht wieder her.");x.BringIntoView();capture("Position-Korrigiert");
        vm.IsPrintSetMode=false;vm.Project.PrintLayout=new();vm.Project.Model=new(){Width=120,Height=88,Resolution=1,LockDimensions=true};vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Flat);
    }
}
