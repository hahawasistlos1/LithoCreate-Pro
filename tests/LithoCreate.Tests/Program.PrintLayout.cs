using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using LithoCreate.Core;
using LithoCreate.App.Services;
using LithoCreate.App.ViewModels;

partial class Program
{
    static void TestPrintLayout()
    {
        foreach(var example in new[]{("Row",PrintArrangement.Row), ("Column",PrintArrangement.Column),("Auto",PrintArrangement.Automatic),("Tight",PrintArrangement.Row)})Test("Drucksatzabstände: gespeicherte Platzierung · "+example.Item1,()=>
        {
            bool tight=example.Item1=="Tight";var options=new PrintSetOptions{Margin=tight?0:12.5,Gap=tight?1:9.5,Arrangement=example.Item2};var printer=new PrinterProfile{BedX=tight?61:100,BedY=tight?30:80,BuildZ=50};var set=PrintSetReference(printer,options);set.Validate();
            Require(set.Margin==options.Margin&&set.Gap==options.Gap,"Abstände nicht übernommen.");Vector2 pa,pb;
            if(tight){pa=new(0,0);pb=new(41,10);}else if(example.Item2==PrintArrangement.Row){pa=new(15.25f,25);pb=new(64.75f,35);}else{pa=new(30,15.25f);pb=new(40,54.75f);}
            Require(Vector2.Distance(set.Parts[0].Position,pa)<.0001&&Vector2.Distance(set.Parts[1].Position,pb)<.0001,"Zentrierte Anordnung falsch.");Require(set.Arrangement==(example.Item2==PrintArrangement.Automatic?PrintArrangement.Column:example.Item2),"Feste Wahl oder automatische Flächenauswahl missachtet.");
            string path=Path.Combine(output,"Layout-"+example.Item1+".3mf");PrintSetExporter.Export(path,set);var read=PrintSetExporter.Read(path);Require(read[0].Position==pa&&read[1].Position==pb&&read.All(p=>p.Report.IsValid),"Gespeicherte Position verändert.");
        });
        Test("Drucksatzabstände: Zahlen, exakte Bettgrenzen und feste Richtung",()=>
        {
            foreach(double value in new[]{-.01,50.01,double.NaN,double.PositiveInfinity})Throws<InvalidDataException>(()=>new PrintSetOptions{Margin=value}.Validate());foreach(double value in new[]{0,.99,50.01,double.NaN,double.NegativeInfinity})Throws<InvalidDataException>(()=>new PrintSetOptions{Gap=value}.Validate());Throws<InvalidDataException>(()=>new PrintSetOptions{Arrangement=(PrintArrangement)99}.Validate());
            var bed=new PrinterProfile{BedX=50,BedY=59.5,BuildZ=3};var column=PrintSetReference(bed,new(){Margin=5,Gap=9.5,Arrangement=PrintArrangement.Column});column.Validate();Throws<InvalidDataException>(()=>PrintSetReference(bed,new(){Gap=9.5,Arrangement=PrintArrangement.Row}));
            bed.BedY=59.4998;Throws<InvalidDataException>(()=>PrintSetReference(bed,new(){Gap=9.5,Arrangement=PrintArrangement.Column}));
            var automatic=PrintSetReference();var explicitDefault=PrintSetReference(options:new());Require(automatic.Parts.Select(p=>p.Position).SequenceEqual(explicitDefault.Parts.Select(p=>p.Position)),"Standardanordnung verändert.");
        });
        Test("Format 13 behält Standardabstände; Format 14 erhält die Drucksatzplanung",()=>
        {
            var data=new ProjectData{Version=13,Model=TabFixture(),PrintLayout=new(){Margin=12,Gap=9,Arrangement=PrintArrangement.Column}};byte[] image=ImageProcessor.Png(ImageProcessor.Demo());string old=Path.Combine(output,"layout-legacy-v13.lithopro");if(File.Exists(old))File.Delete(old);
            using(var zip=ZipFile.Open(old,ZipArchiveMode.Create)){using(var meta=zip.CreateEntry("project.json").Open())JsonSerializer.Serialize(meta,data);using var src=zip.CreateEntry("source.bin").Open();src.Write(image);}
            var read=ProjectStore.Load(old);Require(read.Data.Version==15&&read.Data.PrintLayout.Margin==5&&read.Data.PrintLayout.Gap==5&&read.Data.PrintLayout.Arrangement==PrintArrangement.Automatic&&read.Data.Model.HangingTabsEnabled,"Migration verändert alte Optionen oder übernimmt neue Felder.");Require(JsonSerializer.Serialize(read.Data.Model)==JsonSerializer.Serialize(data.Model)&&read.Image.SequenceEqual(image),"Migration verändert Foto oder Geometrie.");
            string next=Path.Combine(output,"layout-project.lithopro");ProjectStore.Save(next,data,image);read=ProjectStore.Load(next);Require(read.Data.Version==15&&JsonSerializer.Serialize(read.Data)==JsonSerializer.Serialize(data)&&read.Image.SequenceEqual(image),"Projekt verliert Drucksatzplanung.");
            data.PrintLayout.Gap=0;byte[] before=File.ReadAllBytes(next);Throws<InvalidDataException>(()=>ProjectStore.Save(next,data,image));Require(before.SequenceEqual(File.ReadAllBytes(next)),"Ungültiger Abstand überschreibt Projekt.");
        });
        Test("Drucksatzabstände: unveränderte Einzelteile und Optionsschnappschuss",()=>
        {
            var settings=PrintSetFixture(false);byte[] photo=ImageProcessor.Png(ImageProcessor.Demo());string before=JsonSerializer.Serialize(settings);var options=new PrintSetOptions{Margin=12,Gap=9,Arrangement=PrintArrangement.Column};
            var custom=ModelComputation.BuildPrintSet(photo,settings,new(),new(),CancellationToken.None,new ActionProgress(_=>{options.Gap=40;options.Arrangement=PrintArrangement.Row;}),options);var normal=ModelComputation.BuildPrintSet(photo,settings,new(),new(),CancellationToken.None);
            Require(custom.Gap==9&&custom.Margin==12&&custom.Arrangement==PrintArrangement.Column&&before==JsonSerializer.Serialize(settings),"Schnappschuss oder Einzelteilparameter verändert.");
            for(int i=0;i<2;i++)Require(custom.Parts[i].Mesh.Vertices.SequenceEqual(normal.Parts[i].Mesh.Vertices)&&custom.Parts[i].Mesh.Triangles.SequenceEqual(normal.Parts[i].Mesh.Triangles),"Anordnung verändert Einzelteilgeometrie.");
            using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>PrintSetReference(options:new(){Gap=9}).Validate(cts.Token));
        });
        Test("Editor: Drucksatzabstände verwerfen alte Daten und erhalten das Foto",()=>WithPresetVm("layout",vm=>
        {
            vm.Project.Model=PrintSetFixture(false);vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);var preview=vm.UpdatePreview();PumpUntil(()=>preview.IsCompleted);var initial=vm.BuildPrintSet();PumpUntil(()=>initial.IsCompleted);Require(vm.HasPrintSet,"Ausgangsdrucksatz fehlt.");string model=JsonSerializer.Serialize(vm.Model),edits=JsonSerializer.Serialize(vm.Edits),printer=JsonSerializer.Serialize(vm.Printer);var source=vm.Original;vm.Dirty=false;
            vm.PrintSetGap=9.5;vm.PrintSetMargin=12.5;vm.SelectedPrintSetArrangement=PrintSetOptions.Choices[2];Require(vm.Dirty&&!vm.HasPrintSet&&!vm.ExportPrintSet.CanExecute(null),"Geänderte Abstände behalten alten Export.");Throws<InvalidDataException>(()=>vm.PrintSetGap=0);Require(vm.PrintSetGap==9.5,"Ungültiger Abstand wird übernommen.");
            var build=vm.BuildPrintSet();PumpUntil(()=>build.IsCompleted);Require(vm.PreparedPrintSet is {Margin:12.5,Gap:9.5,Arrangement:PrintArrangement.Column}&&!vm.Busy,"Editor verwendet alte Optionen.");Require(model==JsonSerializer.Serialize(vm.Model)&&edits==JsonSerializer.Serialize(vm.Edits)&&printer==JsonSerializer.Serialize(vm.Printer)&&ReferenceEquals(source,vm.Original),"Abstände verändern Foto, Modell oder Drucker.");
            var stale=vm.BuildPrintSet();vm.PrintSetGap=11;PumpUntil(()=>stale.IsCompleted);Require(!vm.HasPrintSet&&!vm.Busy,"Veralteter Drucksatz wird übernommen.");
            vm.Project.PrintLayout=new(){Margin=12,Gap=9,Arrangement=PrintArrangement.Column};vm.PresetName="Modell behält Planung";Require(vm.SaveCurrentModelPreset(),"Modellvorlage fehlt.");Require(vm.ApplySelectedModelPreset()&&vm.Project.PrintLayout.Margin==12&&vm.Project.PrintLayout.Gap==9&&vm.Project.PrintLayout.Arrangement==PrintArrangement.Column,"Modellvorlage überschreibt Drucksatzplanung.");
        }));
    }
    static void RenderPrintLayout(LithoCreate.App.MainWindow window,StudioViewModel vm,Action<string> capture)
    {
        vm.Project.Model=PrintSetFixture(false);vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);vm.Page="Editor";vm.Step=3;vm.PrintSetMargin=12.5;vm.PrintSetGap=9.5;
        foreach(var arrangement in new[]{PrintArrangement.Row,PrintArrangement.Column,PrintArrangement.Automatic})
        {
            vm.SelectedPrintSetArrangement=PrintSetOptions.Choices.First(p=>p.Arrangement==arrangement);var task=vm.BuildPrintSet();PumpUntil(()=>task.IsCompleted);Require(vm.HasPrintSet,"UI-Drucksatz fehlt.");capture("Anordnung-"+arrangement);
        }
        var gap=(System.Windows.Controls.TextBox)window.FindName("PrintSetGapBox");gap.BringIntoView();gap.Focus();gap.Text="abc";PumpUntil(()=>vm.InputError&&!vm.HasPrintSet);Require(!vm.ExportPrintSet.CanExecute(null)&&!vm.RebuildPrintSet.CanExecute(null),"Ungültige Abstände lassen Export zu.");vm.SetViewMode.Execute("surface");Require(vm.InputError&&vm.ShowPrintSet.CanExecute(null),"Fehlerhafte Drucksatzoption verhindert die Rückkehr zur Korrektur.");vm.ShowPrintSet.Execute(null);Require(vm.IsPrintSetMode&&vm.InputError&&!vm.HasPrintSet,"Rückkehr übernimmt ungültige Werte oder rechnet trotz Fehler.");gap.BringIntoView();capture("Anordnung-Ungueltig");var options=(System.Windows.Controls.ScrollViewer)window.FindName("OptionsScroller");var position=gap.TransformToAncestor(options).Transform(new System.Windows.Point(0,0));Require(position.Y>=0&&position.Y+gap.ActualHeight<=options.ActualHeight+.5,"Fehlerhaftes Eingabefeld wurde aus dem sichtbaren Bereich gescrollt.");gap.Text="0";PumpUntil(()=>vm.InputError);Require(vm.PrintSetGap==9.5,"Unzulässiger Teileabstand übernommen.");capture("Anordnung-Grenzwert");gap.Text="7,5";PumpUntil(()=>!vm.InputError);var corrected=vm.BuildPrintSet();PumpUntil(()=>corrected.IsCompleted);Require(vm.HasPrintSet&&vm.PrintSetGap==7.5,"Abstandskorrektur nicht wirksam.");capture("Anordnung-Korrigiert");
        vm.Printer.BedX=70;vm.Printer.BedY=70;vm.Changed();vm.SelectedPrintSetArrangement=PrintSetOptions.Choices[1];var rejected=vm.BuildPrintSet();PumpUntil(()=>rejected.IsCompleted);Require(!vm.HasPrintSet&&!vm.ExportPrintSet.CanExecute(null),"Feste unpassende Richtung wird ersetzt.");capture("Anordnung-Passt-Nicht");vm.Printer.BedX=220;vm.Printer.BedY=220;vm.IsPrintSetMode=false;vm.Project.PrintLayout=new();vm.Project.Model=new(){Width=120,Height=88,Resolution=1,LockDimensions=true};vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Flat);
    }
}
