using System.IO;
using System.Numerics;
using System.Text.Json;
using LithoCreate.Core;
using LithoCreate.App.Services;
using LithoCreate.App.ViewModels;

partial class Program
{
    static void PrepareHistory(StudioViewModel vm)
    {
        vm.Project.Model=PrintSetFixture(false);vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);vm.Page="Editor";vm.Step=3;vm.Project.PrintLayout=new(){Margin=10,Gap=8,Arrangement=PrintArrangement.Manual,BodyX=20,BodyY=25,AccessoryX=120,AccessoryY=100};vm.Changed();var build=vm.BuildPrintSet();PumpUntil(()=>build.IsCompleted);Require(vm.HasPrintSet,"Ausgangsdrucksatz fehlt.");
    }
    static void TestPrintLayoutHistory()
    {
        Test("Drucksatzhistorie: zwei Schritte, Geometrie bleibt dieselbe",()=>WithPresetVm("history-moves",vm=>
        {
            PrepareHistory(vm);Require(!vm.UndoPrintLayout.CanExecute(null)&&!vm.RedoPrintLayout.CanExecute(null),"Leere Historie aktiv.");var original=vm.PreparedPrintSet!;string model=JsonSerializer.Serialize(vm.Model),printer=JsonSerializer.Serialize(vm.Printer),edits=JsonSerializer.Serialize(vm.Edits);
            Require(vm.TryMovePrintSetPart(0,new(50,45)),"Bewegung fehlt.");vm.AccessoryPositionX=130;vm.Dirty=false;vm.UndoPrintLayout.Execute(null);Require(vm.AccessoryPositionX==120&&vm.BodyPositionX==50&&vm.HasPrintSet&&vm.Dirty,"Undo verliert Körper, Cache oder Dirty.");vm.UndoPrintLayout.Execute(null);Require(vm.BodyPositionX==20&&vm.BodyPositionY==25&&vm.AccessoryPositionX==120&&!vm.UndoPrintLayout.CanExecute(null)&&vm.RedoPrintLayout.CanExecute(null),"Zweite Rücknahme falsch.");
            vm.RedoPrintLayout.Execute(null);Require(vm.BodyPositionX==50&&vm.BodyPositionY==45&&vm.AccessoryPositionX==120,"Erster Redo falsch.");vm.RedoPrintLayout.Execute(null);Require(vm.AccessoryPositionX==130&&!vm.RedoPrintLayout.CanExecute(null)&&vm.ExportPrintSet.CanExecute(null),"Zweiter Redo oder Export falsch.");Require(ReferenceEquals(original.Parts[0].Mesh,vm.PreparedPrintSet!.Parts[0].Mesh)&&ReferenceEquals(original.Parts[1].Mesh,vm.PreparedPrintSet.Parts[1].Mesh),"Historie baut Meshes neu auf.");Require(model==JsonSerializer.Serialize(vm.Model)&&printer==JsonSerializer.Serialize(vm.Printer)&&edits==JsonSerializer.Serialize(vm.Edits),"Historie ändert Modell, Foto oder Drucker.");
            PrintSetExporter.Export(Path.Combine(output,"History-Redo.3mf"),vm.PreparedPrintSet);vm.UndoPrintLayout.Execute(null);PrintSetExporter.Export(Path.Combine(output,"History-Undo.3mf"),vm.PreparedPrintSet!);
        }));
        Test("Drucksatzhistorie: verworfene Bewegung, neue Verzweigung und keine Leeraktionen",()=>WithPresetVm("history-branch",vm=>
        {
            PrepareHistory(vm);vm.Dirty=false;Require(vm.TryMovePrintSetPart(0,new(20,25))&&!vm.Dirty&&!vm.UndoPrintLayout.CanExecute(null),"Unveränderte Position erzeugt Historie.");vm.TryMovePrintSetPart(0,new(50,45));string before=vm.PrintLayoutHistoryInfo;Require(!vm.TryMovePrintSetPart(1,new(50,45))&&before==vm.PrintLayoutHistoryInfo,"Abgelehnte Bewegung erzeugt Eintrag.");vm.UndoPrintLayout.Execute(null);vm.BodyPositionY=30;Require(!vm.RedoPrintLayout.CanExecute(null)&&vm.BodyPositionX==20,"Neue Änderung behält alten Redo-Zweig.");vm.UndoPrintLayout.Execute(null);Require(vm.BodyPositionY==25,"Neuer Zweig nicht rücknehmbar.");
        }));
        Test("Drucksatzhistorie: automatische Anordnung ohne Positionssprung zurückholen",()=>WithPresetVm("history-auto",vm=>
        {
            PrepareHistory(vm);vm.SelectedPrintSetArrangement=PrintSetOptions.Choices[0];var build=vm.BuildPrintSet();PumpUntil(()=>build.IsCompleted);var automatic=vm.PreparedPrintSet!;vm.TryMovePrintSetPart(0,automatic.Parts[0].Position-new Vector2(10,10));vm.UndoPrintLayout.Execute(null);Require(!vm.IsManualPrintSet&&vm.HasPrintSet&&vm.Project.PrintLayout.Arrangement==PrintArrangement.Automatic&&vm.PreparedPrintSet!.Arrangement==automatic.Arrangement&&vm.PreparedPrintSet.Parts.Select(p=>p.Position).SequenceEqual(automatic.Parts.Select(p=>p.Position)),"Undo stellt Automatikpositionen nicht wieder her.");vm.RedoPrintLayout.Execute(null);Require(vm.IsManualPrintSet&&vm.HasPrintSet,"Redo stellt manuellen Satz nicht wieder her.");
        }));
        Test("Drucksatzhistorie: Abstände ohne alten Export zurücknehmen",()=>WithPresetVm("history-gap",vm=>
        {
            PrepareHistory(vm);vm.PrintSetGap=9;vm.PrintSetMargin=12;Require(!vm.HasPrintSet,"Geänderte Abstände behalten Cache.");vm.UndoPrintLayout.Execute(null);Require(vm.PrintSetMargin==10&&vm.PrintSetGap==9&&!vm.HasPrintSet,"Abstands-Undo nutzt veralteten Export.");vm.UndoPrintLayout.Execute(null);Require(vm.PrintSetGap==8&&!vm.HasPrintSet&&vm.RebuildPrintSet.CanExecute(null),"Zweiter Abstand nicht wiederhergestellt.");var build=vm.BuildPrintSet();PumpUntil(()=>build.IsCompleted);Require(vm.HasPrintSet&&vm.UndoPrintLayout.CanExecute(null)==false&&vm.RedoPrintLayout.CanExecute(null),"Neuberechnung löscht Historie.");vm.RedoPrintLayout.Execute(null);Require(vm.PrintSetGap==9&&!vm.HasPrintSet,"Redo nach Neuberechnung behält unbekannten Positionsstand.");
        }));
        Test("Drucksatzhistorie: Sperren und 100-Schritt-Grenze",()=>WithPresetVm("history-limit",vm=>
        {
            PrepareHistory(vm);for(int i=0;i<105;i++)vm.TryMovePrintSetPart(0,new(30+i*.1f,25));Require(vm.PrintLayoutHistoryInfo=="Drucksatz: 100 zurück · 0 wiederholen","Historie unbegrenzt oder falsche Einträge.");var set=vm.PreparedPrintSet;vm.Busy=true;vm.UndoPrintLayout.Execute(null);Require(!vm.UndoPrintLayout.CanExecute(null)&&ReferenceEquals(set,vm.PreparedPrintSet),"Undo läuft während Export.");vm.Busy=false;vm.InputError=true;vm.UndoPrintLayout.Execute(null);Require(ReferenceEquals(set,vm.PreparedPrintSet),"Undo läuft trotz Eingabefehler.");vm.InputError=false;vm.Page="Settings";Require(!vm.UndoPrintLayout.CanExecute(null),"Drucksatz-Shortcut außerhalb des Editors.");vm.Page="Editor";for(int i=0;i<100;i++)vm.UndoPrintLayout.Execute(null);Require(!vm.UndoPrintLayout.CanExecute(null)&&Math.Abs(vm.BodyPositionX-30.4)<.001&&vm.RedoPrintLayout.CanExecute(null),"Älteste Schritte nicht begrenzt.");
        }));
        Test("Drucksatzhistorie: Modelländerung, Fotohistorie und Projektwechsel",()=>WithPresetVm("history-project",vm=>
        {
            PrepareHistory(vm);vm.TryMovePrintSetPart(0,new(50,45));vm.Model.Width+=1;vm.Changed();Require(!vm.UndoPrintLayout.CanExecute(null)&&!vm.RedoPrintLayout.CanExecute(null),"Geometrieänderung behält alte Positionen.");PrepareHistory(vm);vm.TryMovePrintSetPart(0,new(50,45));vm.Rotate.Execute(null);Require(!vm.UndoPrintLayout.CanExecute(null)&&vm.Undo.CanExecute(null),"Fotoaktion vermischt Historien.");vm.Undo.Execute(null);Require(vm.Edits.Rotation==0,"Bild-Undo verändert.");
            PrepareHistory(vm);vm.TryMovePrintSetPart(0,new(50,45));string path=Path.Combine(output,"history-project.lithopro");ProjectStore.Save(path,vm.Project,ImageProcessor.Png(vm.Original));vm.OpenPath(path,false);Require(!vm.UndoPrintLayout.CanExecute(null)&&!vm.RedoPrintLayout.CanExecute(null)&&!vm.Dirty&&vm.BodyPositionX==50,"Projektladen übernimmt Historie oder verliert gespeicherte Position.");
        }));
    }
    static void RenderPrintLayoutHistory(LithoCreate.App.MainWindow window,StudioViewModel vm,Action<string> capture)
    {
        PrepareHistory(vm);var undo=(System.Windows.Controls.Button)window.FindName("UndoPrintLayoutButton");var redo=(System.Windows.Controls.Button)window.FindName("RedoPrintLayoutButton");undo.BringIntoView();capture("Historie-Leer");vm.TryMovePrintSetPart(0,new(50,45));vm.AccessoryPositionX=130;undo.BringIntoView();capture("Historie-Zwei-Schritte");undo.Command.Execute(null);Require(vm.AccessoryPositionX==120&&vm.BodyPositionX==50&&vm.HasPrintSet,"UI-Undo falsch.");capture("Historie-Zurueck");redo.Command.Execute(null);Require(vm.AccessoryPositionX==130&&vm.HasPrintSet,"UI-Redo falsch.");capture("Historie-Wiederholt");
        vm.Dirty=false;string path=Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR"))!,"history-ui-project.lithopro");ProjectStore.Save(path,vm.Project,ImageProcessor.Png(vm.Original));vm.OpenPath(path,false);var build=vm.BuildPrintSet();PumpUntil(()=>build.IsCompleted);Require(!vm.UndoPrintLayout.CanExecute(null)&&!vm.RedoPrintLayout.CanExecute(null)&&!vm.Dirty,"Projektwechsel übernimmt UI-Historie.");undo.BringIntoView();capture("Historie-Projektwechsel");
        vm.TryMovePrintSetPart(0,new(55,45));var x=(System.Windows.Controls.TextBox)window.FindName("BodyPositionXBox");x.BringIntoView();x.Focus();x.Text="abc";PumpUntil(()=>vm.InputError);Require(!vm.UndoPrintLayout.CanExecute(null),"Fehler lässt Undo zu.");capture("Historie-Eingabefehler");x.Text="55";PumpUntil(()=>!vm.InputError);Require(vm.UndoPrintLayout.CanExecute(null),"Korrektur verliert Historie.");undo.BringIntoView();undo.Command.Execute(null);Require(vm.BodyPositionX==50&&!vm.HasPrintSet,"Korrektur-Undo übernimmt ungeprüften Cache.");capture("Historie-Korrigiert");
        vm.IsPrintSetMode=false;vm.Project.PrintLayout=new();vm.Project.Model=new(){Width=120,Height=88,Resolution=1,LockDimensions=true};vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Flat);
    }
}
