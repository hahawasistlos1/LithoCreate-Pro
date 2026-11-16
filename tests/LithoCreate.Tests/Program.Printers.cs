using System.IO;
using System.Text;
using System.Text.Json;
using LithoCreate.Core;
using LithoCreate.App.Services;
using LithoCreate.App.ViewModels;

partial class Program
{
    static void TestPrinterProfiles()
    {
        Test("Druckerprofile: alte JSON-Liste, Unicode und tiefe Kopien",()=>
        {
            var defaults=PrinterProfileStore.Defaults();defaults[0].BedX=180;string path=Path.Combine(output,"printer-profiles.json");PrinterProfileStore.Save(path,defaults);var loaded=PrinterProfileStore.Load(path);Require(JsonSerializer.Serialize(defaults)==JsonSerializer.Serialize(loaded),"Profilparameter verändert.");defaults[0].BedX=190;Require(PrinterProfileStore.Load(path)[0].BedX==180&&PrinterProfileStore.Defaults()[0].BedX==220,"Profile teilen veränderbare Daten.");
            string legacy=Path.Combine(output,"printer-legacy.json");File.WriteAllText(legacy,JsonSerializer.Serialize(loaded),Encoding.Unicode);Require(PrinterProfileStore.Load(legacy)[0].BedX==180,"Bisherige JSON-Liste mit BOM nicht gelesen.");
            PrinterProfileStore.Save(legacy,[]);Require(PrinterProfileStore.Load(legacy).Length==0,"Leere gespeicherte Liste wird durch Defaults ersetzt.");
        });
        Test("Druckerprofile: ungültige Daten erhalten die letzte gültige Datei",()=>
        {
            string path=Path.Combine(output,"printer-invalid.json");PrinterProfileStore.Save(path,PrinterProfileStore.Defaults());byte[] before=File.ReadAllBytes(path);
            foreach(var bad in new PrinterProfile[]{new(){Name=""},new(){Name="A\nB"},new(){BedX=0},new(){BuildZ=2001},new(){Layer=double.NaN},new(){Material=null!}})Throws<InvalidDataException>(()=>PrinterProfileStore.Save(path,[bad]));
            Throws<InvalidDataException>(()=>PrinterProfileStore.Save(path,[new(){Name=" Mein Drucker "},new(){Name="mein drucker"}]));Throws<InvalidDataException>(()=>PrinterProfileStore.Save(path,[null!]));Throws<InvalidDataException>(()=>PrinterProfileStore.Save(path,Enumerable.Range(0,101).Select(i=>new PrinterProfile{Name="Drucker "+i})));
            Require(before.SequenceEqual(File.ReadAllBytes(path))&&!Directory.GetFiles(output,"printer-invalid.json.*.tmp").Any(),"Abgewiesene Werte verändern Profildatei.");
            string huge=Path.Combine(output,"printer-too-large.json");File.WriteAllBytes(huge,new byte[512*1024+1]);Throws<InvalidDataException>(()=>PrinterProfileStore.Load(huge));
        });
        Test("Editorprofile: speichern, aktualisieren und unabhängige Auswahl",()=>WithPrinterVm("save",vm=>
        {
            vm.Printer.Name="Mein Drucker";vm.Printer.BedX=180;vm.Dirty=false;Require(vm.SaveCurrentPrinter()&&vm.Profiles.Count==3&&!vm.Dirty,"Profil nicht unabhängig gespeichert.");var saved=vm.SelectedPrinter!;Require(saved.BedX==180&&!ReferenceEquals(saved,vm.Printer),"Profil teilt Projektdaten.");
            vm.Printer.BedX=185;Require(saved.BedX==180&&vm.SelectedPrinter==saved,"Bearbeiten verändert gespeichertes Profil oder verliert Auswahl.");vm.Printer.Name="MEIN DRUCKER";Require(vm.SaveCurrentPrinter()&&vm.Profiles.Count==3&&vm.SelectedPrinter!.BedX==185,"Aktualisieren erzeugt Duplikat oder verliert Werte.");
            var loaded=PrinterProfileStore.Load(Path.Combine(Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR")!,"printers.json"));Require(loaded.Length==3&&loaded[^1].BedX==185,"Datei stimmt nicht mit Auswahl überein.");
            vm.Busy=true;Require(!vm.SaveProfile.CanExecute(null)&&!vm.SaveCurrentPrinter()&&!vm.LoadPrinterProfiles(),"Profilspeichern/-laden während Berechnung.");vm.Busy=false;vm.InputError=true;Require(!vm.SaveCurrentPrinter(),"Profilspeichern trotz ungültiger Eingabe.");vm.InputError=false;
        }));
        Test("Editorprofile: Umbenennen und bestätigtes Löschen erhalten Projektwerte",()=>WithPrinterVm("manage",vm=>
        {
            vm.Printer.Name="Eigener";vm.Printer.BedX=180;Require(vm.SaveCurrentPrinter(),"Profil fehlt.");var selected=vm.SelectedPrinter!;string path=Path.Combine(Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR")!,"printers.json");byte[] before=File.ReadAllBytes(path);
            vm.Printer.Name=vm.Profiles[0].Name;Require(!vm.RenameSelectedPrinter()&&vm.SelectedPrinter==selected&&before.SequenceEqual(File.ReadAllBytes(path)),"Namenskonflikt übernimmt Teiländerung.");
            vm.Printer.Name=" Neuer Name ";vm.Printer.BedX=170;vm.Dirty=false;Require(vm.RenameSelectedPrinter()&&vm.SelectedPrinter!.Name=="Neuer Name"&&vm.SelectedPrinter.BedX==180&&vm.Printer.BedX==170&&vm.Dirty,"Umbenennen verändert andere gespeicherte oder aktuelle Werte.");
            string printer=JsonSerializer.Serialize(vm.Printer);vm.Dirty=false;Require(!vm.DeleteSelectedPrinter()&&vm.Profiles.Count==3,"Löschen ohne Bestätigung.");Require(vm.DeleteSelectedPrinter(true)&&vm.Profiles.Count==2&&vm.SelectedPrinter==null&&!vm.Dirty&&printer==JsonSerializer.Serialize(vm.Printer),"Löschen verändert eingebetteten Drucker oder Projektstatus.");
        }));
        Test("Editorprofile: beschädigte Datei bleibt geschützt und kann neu geladen werden",()=>
        {
            foreach(string invalid in new[]{"{beschädigt","null","{}","[null]","[{\"Name\":\"Ungültig\",\"BedX\":0}]","[{\"Name\":\"Zukunft\",\"FutureOption\":true}]"})WithPrinterVm("damaged",vm=>
            {
                string path=Path.Combine(Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR")!,"printers.json");Require(vm.Profiles.Count==2&&!vm.SaveProfile.CanExecute(null)&&!vm.RenamePrinterProfile.CanExecute(null)&&!vm.DeletePrinterProfile.CanExecute(null),"Defekte Datei kann überschrieben werden.");Require(!vm.SaveCurrentPrinter()&&File.ReadAllText(path)==invalid&&vm.PrinterProfileMessage.Contains("unverändert"),"Fehlerhafte Datei wurde überschrieben oder Fehler verschwiegen.");
                string project=JsonSerializer.Serialize(vm.Project);vm.Dirty=false;PrinterProfileStore.Save(path,[new(){Name="Gerettet",BedX=175}]);Require(vm.LoadPrinterProfiles()&&vm.Profiles.Count==1&&vm.SaveProfile.CanExecute(null)&&!vm.Dirty&&project==JsonSerializer.Serialize(vm.Project),"Neu laden verändert Projekt oder bleibt gesperrt.");
            },invalid);
        });
        Test("Editorprofile: Schreibfehler übernehmen weder Liste noch Auswahl",()=>WithPrinterVm("write-error",vm=>
        {
            string dir=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR")!;Directory.CreateDirectory(Path.Combine(dir,"printers.json"));var selected=vm.SelectedPrinter;vm.Printer.Name="Nicht gespeichert";vm.Dirty=false;string project=JsonSerializer.Serialize(vm.Project);
            Require(!vm.SaveCurrentPrinter()&&!vm.RenameSelectedPrinter()&&!vm.DeleteSelectedPrinter(true)&&vm.Profiles.Count==2&&vm.SelectedPrinter==selected&&!vm.Dirty&&project==JsonSerializer.Serialize(vm.Project),"Schreibfehler verändert Liste, Auswahl oder Projekt.");Require(!Directory.GetFiles(dir,"*.tmp").Any(),"Temporäre Profildatei bleibt liegen.");
        }));
        Test("Editorprofile: gespeicherte Werte übernehmen und eingebettete Drucker erhalten",()=>WithPrinterVm("apply",vm=>
        {
            var source=vm.Original;string model=JsonSerializer.Serialize(vm.Model),edits=JsonSerializer.Serialize(vm.Edits),layout=JsonSerializer.Serialize(vm.Project.PrintLayout);vm.Printer.BedX=180;vm.Printer.Name="Geändert";vm.Dirty=false;Require(vm.ApplyStoredPrinter()&&vm.Printer.BedX==220&&vm.Printer.Name==vm.Profiles[0].Name&&vm.Dirty,"Gespeicherte Werte lassen sich nicht erneut übernehmen.");
            vm.SelectedPrinter=vm.Profiles[1];Require(vm.Printer.BedX==143&&vm.Printer.Process=="Resin"&&!ReferenceEquals(vm.Printer,vm.Profiles[1]),"Profilwahl kopiert Daten nicht.");vm.Printer.BedX=100;Require(vm.Profiles[1].BedX==143,"Projektänderung verändert Bibliothek.");Require(ReferenceEquals(source,vm.Original)&&model==JsonSerializer.Serialize(vm.Model)&&edits==JsonSerializer.Serialize(vm.Edits)&&layout==JsonSerializer.Serialize(vm.Project.PrintLayout),"Profilwahl verändert Foto, Modell oder Drucksatzplanung.");
            string path=Path.Combine(output,"printer-embedded.lithopro");var data=new ProjectData{Printer=new(){BedX=177}};ProjectStore.Save(path,data,ImageProcessor.Png(ImageProcessor.Demo()));vm.OpenPath(path,false);Require(vm.SelectedPrinter==vm.Profiles[0]&&vm.Printer.BedX==177&&!vm.Dirty,"Gleichnamige Bibliothek überschreibt eingebettete Projektwerte.");
        }));
        Test("Editorprofile: Profilwechsel verwirft unpassende Drucksätze",()=>WithPrinterVm("printset",vm=>
        {
            string path=Path.Combine(Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR")!,"printers.json");PrinterProfileStore.Save(path,[..PrinterProfileStore.Defaults(),new(){Name="Kleines Bett",BedX=70,BedY=70}]);vm.LoadPrinterProfiles();vm.Project.Model=PrintSetFixture(false);vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);var build=vm.BuildPrintSet();PumpUntil(()=>build.IsCompleted);Require(vm.HasPrintSet,"Ausgangsdrucksatz fehlt.");
            vm.SelectedPrinter=vm.Profiles[^1];Require(!vm.HasPrintSet&&!vm.ExportPrintSet.CanExecute(null),"Profilwechsel behält alten Drucksatz.");var small=vm.BuildPrintSet();PumpUntil(()=>small.IsCompleted);Require(!vm.HasPrintSet&&vm.PrintSetInfo.Contains("nicht gemeinsam"),"Unpassender Drucksatz bleibt exportierbar.");
        }));
        Test("Editorprofile: leere Bibliothek bleibt nach Neustart leer",()=>WithPrinterVm("empty",vm=>
        {
            while(vm.Profiles.Count>0){vm.SelectedPrinter=vm.Profiles[0];vm.Dirty=false;Require(vm.DeleteSelectedPrinter(true)&&!vm.Dirty,"Löschen verändert Projekt.");}Require(vm.SelectedPrinter==null,"Leere Bibliothek behält alte Auswahl.");vm.CleanShutdown();StudioViewModel? restarted=null;
            try{restarted=new();Require(restarted.Profiles.Count==0&&restarted.SelectedPrinter==null&&restarted.SaveProfile.CanExecute(null),"Gelöschte Standardprofile werden neu erzeugt.");Require(restarted.SaveCurrentPrinter()&&restarted.Profiles.Count==1,"Leere Bibliothek kann nicht erneut befüllt werden.");}finally{restarted?.CleanShutdown();}
        }));
    }
    static void WithPrinterVm(string name,Action<StudioViewModel> test,string? file=null)
    {
        string? previous=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR");string dir=Path.Combine(output,"printer-vm-"+name+"-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);if(file!=null)File.WriteAllText(Path.Combine(dir,"printers.json"),file);Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",dir);StudioViewModel? vm=null;
        try{vm=new();test(vm);}finally{if(vm!=null){vm.Dirty=false;vm.CleanShutdown();}Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",previous);}
    }
    static void RenderPrinterProfiles(LithoCreate.App.MainWindow window,StudioViewModel vm,Action<string> capture)
    {
        vm.Page="Settings";var name=(System.Windows.Controls.TextBox)window.FindName("PrinterNameBox");var bed=(System.Windows.Controls.TextBox)window.FindName("PrinterBedXBox");var save=(System.Windows.Controls.Button)window.FindName("SavePrinterButton");name.BringIntoView();name.Focus();name.Text="Eigener Testdrucker";bed.Focus();bed.Text="180";save.Command.Execute(null);Require(vm.SelectedPrinter?.Name=="Eigener Testdrucker"&&vm.SelectedPrinter.BedX==180,"Oberfläche speichert Profil nicht.");capture("Drucker-Gespeichert");
        bed.Focus();bed.Text="185";save.Command.Execute(null);Require(vm.Profiles.Count==3&&vm.SelectedPrinter!.BedX==185,"Profilupdate erstellt Duplikat.");capture("Drucker-Aktualisiert");vm.Dirty=false;((System.Windows.Controls.ComboBox)window.FindName("PrinterLibraryCombo")).Focus();vm.ReloadPrinterProfiles.Execute(null);window.UpdateLayout();Require(!vm.Dirty&&vm.Printer.BedX==185&&vm.Printer.Name=="Eigener Testdrucker","Neu laden wendet ein anderes Profil an oder verändert den Projektstatus.");
        name.Focus();name.Text=vm.Profiles[0].Name;vm.RenamePrinterProfile.Execute(null);Require(vm.PrinterProfileMessage.Contains("bereits vorhanden")&&vm.SelectedPrinter!.Name=="Eigener Testdrucker","Namenskonflikt verliert Auswahl.");capture("Drucker-Namenskonflikt");name.Text="Mein Desktopdrucker";vm.RenamePrinterProfile.Execute(null);Require(vm.SelectedPrinter!.Name=="Mein Desktopdrucker","Profil nicht umbenannt.");capture("Drucker-Umbenannt");
        bed.BringIntoView();bed.Focus();bed.Text="abc";PumpUntil(()=>vm.InputError);Require(!vm.SaveProfile.CanExecute(null)&&!vm.RenamePrinterProfile.CanExecute(null),"Ungültige Eingabe lässt Profiländerungen zu.");capture("Drucker-Ungueltig");Require(vm.ApplyPrinterProfile.CanExecute(null),"Gespeicherte Werte können ungültige Druckereingabe nicht ersetzen.");vm.ApplyPrinterProfile.Execute(null);PumpUntil(()=>!vm.InputError);Require(vm.Printer.BedX==185&&bed.Text=="185","Profilübernahme lässt eine veraltete Fehlermarkierung zurück.");bed.Focus();bed.Text="0";PumpUntil(()=>!vm.InputError);Require(!vm.SaveCurrentPrinter()&&vm.SelectedPrinter!.BedX==185,"Ungültige Bettgröße wird gespeichert.");capture("Drucker-Grenzwert");bed.Text="177";PumpUntil(()=>!vm.InputError);vm.ApplyPrinterProfile.Execute(null);window.UpdateLayout();Require(vm.Printer.BedX==185&&bed.Text=="185","Gespeicherte Werte aktualisieren Eingabe nicht.");capture("Drucker-Korrigiert");
        string path=Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR"))!,"printer-ui-project.lithopro");ProjectStore.Save(path,new(){Model=new(){Width=120,Height=88,Resolution=1,LockDimensions=true},Printer=new(){Name="Mein Desktopdrucker",BedX=177}},ImageProcessor.Png(ImageProcessor.Demo()));vm.OpenPath(path,false);vm.Page="Settings";window.UpdateLayout();Require(vm.Printer.BedX==177&&bed.Text=="177"&&!vm.Dirty,"Bibliotheksauswahl überschreibt eingebettete Werte.");bed.BringIntoView();capture("Drucker-Projektwerte");string printer=JsonSerializer.Serialize(vm.Printer);bool removed=vm.DeleteSelectedPrinter(true);Require(removed&&vm.Profiles.Count==2&&!vm.Dirty&&printer==JsonSerializer.Serialize(vm.Printer),$"Profilspeicher-Löschen verändert Projekt: gelöscht={removed}, Profile={vm.Profiles.Count}, Dirty={vm.Dirty}, DruckerIdentisch={printer==JsonSerializer.Serialize(vm.Printer)}, Meldung={vm.PrinterProfileMessage}");capture("Drucker-Geloescht");vm.SelectedPrinter=vm.Profiles[0];
    }
}
