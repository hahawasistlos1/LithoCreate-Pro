using System.IO;
using System.Text.Json;
using LithoCreate.Core;
using LithoCreate.App.ViewModels;

partial class Program
{
    static void TestModelPresets()
    {
        Test("Eigene Vorlagen: alle Formen, tiefe Kopien und lokaler Roundtrip",()=>
        {
            var items=Enum.GetValues<Shape>().Select(shape=>new ModelPreset{Name="Form · "+shape,Model=new(){Shape=shape,Width=40,Height=30,Diameter=40,Radius=40,FrameWidth=2,Resolution=1,Angles=new(10,20,30),LockDimensions=true}}).ToList();
            foreach(var item in items.Where(item=>item.Model.IsShade)) LampShade.ApplyTemplate(item.Model);
            foreach(var item in items.Where(item=>item.Model.HasDesignParts)) DesignParts.ApplyTemplate(item.Model);
            var mounted=new ModelPreset{Name="Rückwand · ÄÖÜ",Model=MountingFixture(4,true)};items.Add(mounted);string path=Path.Combine(output,"model-presets.json");ModelPresetStore.Save(path,items);
            var loaded=ModelPresetStore.Load(path);Require(loaded.Items.Count==items.Count&&JsonSerializer.Serialize(items)==JsonSerializer.Serialize(loaded.Items),"Form-/Zubehörparameter oder Kennungen verändert.");Require(loaded.Items[^1].ToString()=="Rückwand · ÄÖÜ","Auswahl zeigt technischen Typ statt Vorlagenname.");
            var copy=mounted.Clone();copy.Model.BackMountingHoles[0].X+=3;Require(copy.Model.BackMountingHoles[0].X!=mounted.Model.BackMountingHoles[0].X,"Bohrungsliste ist keine tiefe Kopie.");
            mounted.Model.BackMountingHoles.Clear();Require(ModelPresetStore.Load(path).Items[^1].Model.BackMountingHoles.Count==4,"Spätere Änderung überschreibt gespeicherte Vorlage.");
        });
        Test("Eigene Vorlagen: ungültige Sammlungen erhalten die letzte Datei",()=>
        {
            string path=Path.Combine(output,"preset-invalid.json");var good=new ModelPreset{Name="Gültig"};ModelPresetStore.Save(path,[good]);byte[] before=File.ReadAllBytes(path);
            foreach(string name in new[]{"", " ",new string('x',81),"A\nB"," Rand"}){var bad=good.Clone();bad.Name=name;Throws<InvalidDataException>(()=>ModelPresetStore.Save(path,[bad]));}
            var duplicate=good.Clone();duplicate.Id=Guid.NewGuid();duplicate.Name="gültig";Throws<InvalidDataException>(()=>ModelPresetStore.Save(path,[good,duplicate]));duplicate.Name="Anders";duplicate.Id=good.Id;Throws<InvalidDataException>(()=>ModelPresetStore.Save(path,[good,duplicate]));
            var invalid=good.Clone();invalid.Model.Width=0;Throws<InvalidDataException>(()=>ModelPresetStore.Save(path,[invalid]));
            Throws<InvalidDataException>(()=>ModelPresetStore.Save(path,Enumerable.Range(0,101).Select(i=>new ModelPreset{Name="Vorlage "+i})));
            Require(File.ReadAllBytes(path).SequenceEqual(before),"Ungültige Eingabe überschreibt Sammlung.");
            foreach(int version in new[]{0,2}){var catalog=new ModelPresetCatalog{Version=version};Throws<InvalidDataException>(catalog.Validate);}Throws<InvalidDataException>(()=>new ModelPresetCatalog{ModelVersion=14}.Validate());
            string big=Path.Combine(output,"preset-too-large.json");File.WriteAllBytes(big,new byte[2*1024*1024+1]);Throws<InvalidDataException>(()=>ModelPresetStore.Load(big));
        });
        Test("Editorvorlage: Foto, Bildkorrekturen, Drucker und Bild-Undo bleiben erhalten",()=>WithPresetVm("apply",vm=>
        {
            vm.Project.Model=new(){Width=40,Height=30,FrameWidth=0,Resolution=1,MinThickness=3,MaxThickness=3,LockDimensions=true,PrintOrientation=PrintOrientation.FaceUp};vm.PresetName="Mein Flachbild";Require(vm.SaveCurrentModelPreset(),"Vorlage nicht gespeichert.");var preset=vm.SelectedModelPreset!;
            var expected=MeshGenerator.Generate(new(1,1,[255]),vm.Model,false);vm.ModelWidth=70;Require(preset.Model.Width==40,"Modelländerung verändert gespeicherte Vorlage.");
            vm.Mirror.Execute(null);vm.Rotate.Execute(null);vm.Undo.Execute(null);string edits=JsonSerializer.Serialize(vm.Edits),printer=JsonSerializer.Serialize(vm.Printer);var image=vm.Original;string projectName=vm.Project.Name;vm.Model.MemoryMb=128;vm.Dirty=false;
            Require(vm.ApplySelectedModelPreset(),"Vorlage nicht angewendet.");Require(vm.Dirty&&vm.Page=="Editor"&&vm.Step==3&&vm.Model.Width==40&&vm.Model.Height==30&&vm.Model.LockDimensions&&vm.Model.MemoryMb==128,"Modell oder Speicherbudget falsch übernommen.");
            Require(ReferenceEquals(vm.Original,image)&&JsonSerializer.Serialize(vm.Edits)==edits&&JsonSerializer.Serialize(vm.Printer)==printer&&vm.Project.Name==projectName,"Vorlage verändert Foto, Bildkorrekturen, Drucker oder Projektname.");
            var mesh=MeshGenerator.Generate(new(1,1,[255]),vm.Model,false);Require(mesh.Vertices.SequenceEqual(expected.Vertices)&&mesh.Triangles.SequenceEqual(expected.Triangles),"Vorlage verändert gespeicherte Geometrie.");CheckRoundtrip(mesh,"Preset-Applied");
            vm.Redo.Execute(null);Require(vm.Edits.Rotation==90&&vm.Edits.Mirror,"Anwenden verwirft Bild-Redo.");
            vm.Model.Width=50;Require(preset.Model.Width==40,"Angewendetes Modell teilt veränderbare Daten mit Vorlage.");
            vm.SelectedModelPreset=null;Require(!vm.ApplyModelPreset.CanExecute(null)&&!vm.ApplySelectedModelPreset(),"Leere Auswahl kann angewendet werden.");
        }));
        Test("Editorvorlage: Bohrungen und getrennte Drucklagen vollständig übernommen",()=>WithPresetVm("mounting",vm=>
        {
            vm.Project.Model=SinkFixture();vm.Model.Angles=new(1,2,3);vm.Model.BackAngles=new(4,5,6);vm.PresetName="Kasten mit Rückwand";Require(vm.SaveCurrentModelPreset(),"Zubehörvorlage nicht gespeichert.");string stored=JsonSerializer.Serialize(vm.Model);vm.Project.Model=new();Require(vm.ApplySelectedModelPreset()&&JsonSerializer.Serialize(vm.Model)==stored,"Zubehör, Senkungen oder Druckwinkel fehlen.");
            vm.BackHoles[0].Diameter+=.1;Require(vm.SelectedModelPreset!.Model.BackMountingHoles[0].Diameter!=vm.BackHoles[0].Diameter,"Angewendete Bohrungen teilen Vorlage.");
        }));
        Test("Editorvorlage: Name, Konflikte, bestätigtes Löschen und Neustart",()=>WithPresetVm("manage",vm=>
        {
            vm.Dirty=false;vm.PresetName="Erste";Require(vm.SaveCurrentModelPreset()&&!vm.Dirty,"Vorlagenspeichern verändert Projektstatus.");Guid id=vm.SelectedModelPreset!.Id;vm.PresetName="Zweite";Require(vm.SaveCurrentModelPreset(),"Zweite Vorlage fehlt.");
            vm.PresetName="erste";Require(!vm.RenameSelectedModelPreset()&&vm.ModelPresets.Count==2&&vm.SelectedModelPreset!.Name=="Zweite","Namenskonflikt überschreibt Vorlage.");vm.PresetName=" Lampe · Weiß ";Require(vm.RenameSelectedModelPreset()&&vm.SelectedModelPreset!.Name=="Lampe · Weiß"&&!vm.Dirty,"Umbenennen falsch.");
            vm.SelectedModelPreset=vm.ModelPresets.First(p=>p.Id==id);Require(!vm.DeleteSelectedModelPreset()&&vm.ModelPresets.Count==2,"Löschen ohne Bestätigung.");Require(vm.DeleteSelectedModelPreset(true)&&vm.ModelPresets.Count==1&&!vm.Dirty,"Löschen verändert Projekt oder falsche Sammlung.");
            StudioViewModel? restarted=null;try{restarted=new();Require(restarted.ModelPresets.Count==1&&restarted.SelectedModelPreset!.Name=="Lampe · Weiß","Neustart verliert Vorlagen.");}finally{restarted?.CleanShutdown();}
            Require(vm.DeleteSelectedModelPreset(true)&&vm.ModelPresets.Count==0&&vm.SelectedModelPreset==null&&vm.PresetName=="","Letzte Vorlage hinterlässt alte Auswahl.");
        }));
        Test("Editorvorlage: beschädigte und zukünftige Dateien werden geschützt",()=>
        {
            foreach(string invalid in new[]{"{beschädigt", "{\"Version\":2,\"ModelVersion\":12,\"Items\":[]}","{\"Version\":1,\"ModelVersion\":14,\"Items\":[]}"})WithPresetVm("damaged-"+Guid.NewGuid().ToString("N"),vm=>
            {
                Require(vm.ModelPresets.Count==0&&!vm.SaveModelPreset.CanExecute(null)&&!vm.SaveCurrentModelPreset(),"Fehlerhafte Datei kann überschrieben werden.");string path=Path.Combine(Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR")!,"model-presets.json");Require(File.ReadAllText(path)==invalid&&vm.PresetMessage.Contains("unverändert"),"Fehler nicht angezeigt oder Datei überschrieben.");
                vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Heart);Require(vm.Model.Shape==Shape.Heart,"Defekter Katalog blockiert Grundformen.");
            },invalid);
        });
        Test("Editorvorlage: Schreibfehler und gesperrte Aktionen übernehmen keine Teiländerung",()=>WithPresetVm("io-error",vm=>
        {
            vm.PresetName="Nicht gespeichert";vm.Dirty=false;vm.Busy=true;Require(!vm.SaveModelPreset.CanExecute(null)&&!vm.SaveCurrentModelPreset(),"Speichern während Berechnung.");vm.Busy=false;vm.InputError=true;Require(!vm.SaveCurrentModelPreset(),"Speichern trotz ungültiger Eingabe.");vm.InputError=false;
            string dir=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR")!;Directory.CreateDirectory(Path.Combine(dir,"model-presets.json"));Require(!vm.SaveCurrentModelPreset()&&vm.ModelPresets.Count==0&&vm.SelectedModelPreset==null&&!vm.Dirty,"Schreibfehler übernimmt Vorlagenliste oder verändert Projekt.");Require(!Directory.GetFiles(dir,"*.tmp").Any(),"Temporäre Datei bleibt liegen.");
        }));
    }
    static void WithPresetVm(string name,Action<StudioViewModel> test,string? catalog=null)
    {
        string? old=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR");string dir=Path.Combine(output,"preset-vm-"+name+"-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);if(catalog!=null)File.WriteAllText(Path.Combine(dir,"model-presets.json"),catalog);Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",dir);StudioViewModel? vm=null;
        try{vm=new();test(vm);}finally{if(vm!=null){vm.Dirty=false;vm.CleanShutdown();}Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",old);}
    }
    static void RenderPresetInterface(LithoCreate.App.MainWindow window,StudioViewModel vm,Action<string> capture)
    {
        vm.Page="Templates";var scroll=(System.Windows.Controls.ScrollViewer)window.FindName("TemplatesScroller");scroll.ScrollToTop();window.UpdateLayout();Require(vm.ModelPresets.Count==0,"UI-Vorlagenlauf startet nicht leer.");capture("Vorlagen-Leer");
        var name=(System.Windows.Controls.TextBox)window.FindName("PresetNameBox");name.Focus();name.Text="Meine Fotolampe · Weiß";var save=(System.Windows.Controls.Button)window.FindName("SavePresetButton");Require(save.Command.CanExecute(null),"Vorlagenspeichern nicht gebunden.");save.Command.Execute(null);Require(vm.ModelPresets.Count==1,"UI speichert Vorlage nicht.");capture("Vorlagen-Gespeichert");
        save.Command.Execute(null);Require(vm.ModelPresets.Count==1&&vm.PresetMessage.Contains("bereits vorhanden"),"UI überschreibt gleichnamige Vorlage.");capture("Vorlagen-Namenskonflikt");
        name.Focus();name.Text="Meine Lieblingslampe mit sehr langem Namen · 120 × 88 mm";vm.RenameModelPreset.Execute(null);Require(vm.SelectedModelPreset!.Name==name.Text,"UI benennt Vorlage nicht um.");capture("Vorlagen-Umbenannt");
        vm.Dirty=false;var combo=(System.Windows.Controls.ComboBox)window.FindName("ModelPresetCombo");combo.Focus();combo.SelectedIndex=-1;combo.SelectedIndex=0;window.UpdateLayout();Require(!vm.Dirty,"Vorlagenauswahl verändert Projektstatus.");
        vm.ModelWidth=160;var apply=(System.Windows.Controls.Button)window.FindName("ApplyPresetButton");apply.Command.Execute(null);var preview=vm.UpdatePreview();PumpUntil(()=>preview.IsCompleted);Require(vm.Page=="Editor"&&vm.Model.Width==120&&vm.Model.Height==88&&vm.PreviewMesh!=null,"UI übernimmt Vorlage oder Vorschau nicht.");capture("Vorlagen-Angewendet");
        vm.Page="Templates";scroll.ScrollToTop();Require(vm.DeleteSelectedModelPreset(true)&&vm.ModelPresets.Count==0,"UI-Vorlage bleibt nach Löschen vorhanden.");capture("Vorlagen-Geloescht");
    }
}
