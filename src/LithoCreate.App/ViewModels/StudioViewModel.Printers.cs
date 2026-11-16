using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using LithoCreate.Core;

namespace LithoCreate.App.ViewModels;
public sealed partial class StudioViewModel
{
    readonly ObservableCollection<PrinterProfile> printerProfiles=[];
    public ReadOnlyObservableCollection<PrinterProfile> Profiles {get;private set;}=null!;
    PrinterProfile? selectedStoredPrinter,observedProjectPrinter;
    bool printerCatalogFailed,updatingPrinterLibrary;
    string printerProfileMessage="Profile bleiben lokal. Ein neuer Name speichert ein weiteres Profil; gleicher Name aktualisiert dessen gespeicherte Werte.";
    string PrinterPath=>Path.Combine(dataDir,"printers.json");
    public PrinterProfile? SelectedPrinter
    {
        get=>selectedStoredPrinter;
        set
        {
            if(updatingPrinterLibrary)return;
            if(value==null){selectedStoredPrinter=null;NotifyPrinterState();return;}
            if(!printerProfiles.Contains(value))return;
            if(ReferenceEquals(value,selectedStoredPrinter)&&ReferenceEquals(observedProjectPrinter,Printer))return;
            value.Validate();selectedStoredPrinter=value;project.Printer=value.Clone();observedProjectPrinter=Printer;
            Notify(nameof(Printer));NotifyPrinterState();Changed();
        }
    }
    public string ActivePrinterName=>"Aktiv: "+Printer.Name;
    public string PrinterProfileHint=>$"Im Projekt: {Printer.Name} · {Printer.BedX:N1} × {Printer.BedY:N1} × {Printer.BuildZ:N1} mm\nGespeichert ausgewählt: {selectedStoredPrinter?.Name??"keines"}";
    public string PrinterProfileMessage {get=>printerProfileMessage;private set{printerProfileMessage=value;Notify();}}
    public ICommand RenamePrinterProfile {get;private set;}=null!;
    public ICommand DeletePrinterProfile {get;private set;}=null!;
    public ICommand ReloadPrinterProfiles {get;private set;}=null!;
    public ICommand ApplyPrinterProfile {get;private set;}=null!;
    public ICommand AddP1SProfile {get;private set;}=null!;
    public bool AddP1SPrinter()
    {
        if(Busy||InputError||printerCatalogFailed)return false;
        try{var profile=CalibrationKit.P1SPetg();var existing=printerProfiles.FirstOrDefault(p=>PrinterProfileStore.SameName(p.Name,profile.Name));if(existing!=null){selectedStoredPrinter=existing;ApplyStoredPrinter();PrinterProfileMessage="Vorhandenes P1S-Profil übernommen; gespeicherte Werte bleiben erhalten.";return true;}PrinterProfileStore.Save(PrinterPath,printerProfiles.Append(profile));UpdatePrinterLibrary(()=>printerProfiles.Add(profile),null);SelectedPrinter=profile;PrinterProfileMessage="P1S / PETG angelegt. 0,4-mm-Düse und 0,12-mm-Schicht sind Ausgangswerte; tatsächlich montierte Düse prüfen. Druckparameter in Bambu Studio wählen.";return true;}catch(Exception ex){PrinterProfileMessage=ex.Message;return false;}
    }
    void InitializePrinters()
    {
        Profiles=new(printerProfiles);AddP1SProfile=new RelayCommand(_=>AddP1SPrinter(),()=>!Busy&&!InputError&&!printerCatalogFailed);
        ApplyPrinterProfile=new RelayCommand(_=>ApplyStoredPrinter(),()=>!Busy&&SelectedPrinter!=null);
        RenamePrinterProfile=new RelayCommand(_=>RenameSelectedPrinter(),()=>!Busy&&!InputError&&!printerCatalogFailed&&SelectedPrinter!=null);
        DeletePrinterProfile=new RelayCommand(_=>
        {
            if(SelectedPrinter is {} profile&&MessageBox.Show($"Gespeichertes Druckerprofil „{profile.Name}“ löschen? Druckereinstellungen im aktuellen und in gespeicherten Projekten bleiben erhalten.","Druckerprofil löschen",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)DeleteSelectedPrinter(true);
        },()=>!Busy&&!InputError&&!printerCatalogFailed&&SelectedPrinter!=null);
        ReloadPrinterProfiles=new RelayCommand(_=>LoadPrinterProfiles(),()=>!Busy);
        LoadPrinterProfiles();
    }
    public bool ApplyStoredPrinter()
    {
        if(Busy||SelectedPrinter is not {} profile)return false;
        profile.Validate();project.Printer=profile.Clone();observedProjectPrinter=Printer;
        Notify(nameof(Printer));NotifyPrinterState();Changed();PrinterProfileMessage=$"Gespeicherte Werte von „{profile.Name}“ im Projekt übernommen.";return true;
    }
    public bool LoadPrinterProfiles()
    {
        if(Busy)return false;
        try
        {
            var loaded=PrinterProfileStore.Load(PrinterPath);
            UpdatePrinterLibrary(()=>{printerProfiles.Clear();foreach(var profile in loaded)printerProfiles.Add(profile);},loaded.FirstOrDefault(p=>PrinterProfileStore.SameName(p.Name,Printer.Name)));
            printerCatalogFailed=false;observedProjectPrinter=Printer;NotifyPrinterState();
            PrinterProfileMessage=$"{printerProfiles.Count} Druckerprofile lokal geladen. Neuer Name erstellt ein weiteres Profil; gleicher Name aktualisiert gespeicherte Werte.";return true;
        }
        catch(Exception ex)
        {
            printerCatalogFailed=true;
            if(observedProjectPrinter==null){foreach(var profile in PrinterProfileStore.Defaults())printerProfiles.Add(profile);}
            selectedStoredPrinter=printerProfiles.FirstOrDefault(p=>PrinterProfileStore.SameName(p.Name,Printer.Name));observedProjectPrinter=Printer;NotifyPrinterState();
            PrinterProfileMessage="Druckerprofile konnten nicht geladen werden. printers.json bleibt unverändert; Speichern, Umbenennen und Löschen sind gesperrt. Nach Dateikorrektur „Profildatei neu laden“ wählen. "+ex.Message;return false;
        }
    }
    void UpdatePrinterLibrary(Action update,PrinterProfile? selection)
    {
        updatingPrinterLibrary=true;
        try{update();selectedStoredPrinter=selection;NotifyPrinterState();}
        finally{updatingPrinterLibrary=false;}
    }
    void NotifyPrinterState()
    {
        if(!ReferenceEquals(observedProjectPrinter,Printer))
        {
            observedProjectPrinter=Printer;selectedStoredPrinter=printerProfiles.FirstOrDefault(p=>PrinterProfileStore.SameName(p.Name,Printer.Name));
        }
        Notify(nameof(SelectedPrinter));Notify(nameof(PrinterProfileHint));Notify(nameof(ActivePrinterName));CommandManager.InvalidateRequerySuggested();
    }
    public bool SaveCurrentPrinter()
    {
        if(Busy||InputError||printerCatalogFailed)return false;
        try
        {
            var item=Printer.Clone();item.Name=item.Name.Trim();item.Validate();
            int index=printerProfiles.ToList().FindIndex(p=>PrinterProfileStore.SameName(p.Name,item.Name));
            var next=printerProfiles.ToList();if(index>=0)next[index]=item;else next.Add(item);
            PrinterProfileStore.Save(PrinterPath,next);
            UpdatePrinterLibrary(()=>{if(index>=0)printerProfiles[index]=item;else printerProfiles.Add(item);},item);
            if(Printer.Name!=item.Name){Printer.Name=item.Name;Notify(nameof(Printer));Changed();}
            NotifyPrinterState();PrinterProfileMessage=$"Druckerprofil „{item.Name}“ lokal {(index>=0?"aktualisiert":"gespeichert")}.";return true;
        }
        catch(Exception ex){PrinterProfileMessage=ex.Message;return false;}
    }
    public bool RenameSelectedPrinter()
    {
        if(Busy||InputError||printerCatalogFailed||SelectedPrinter is not {} original)return false;
        try
        {
            var item=original.Clone();item.Name=Printer.Name.Trim();
            var next=printerProfiles.Select(p=>ReferenceEquals(p,original)?item:p).ToArray();PrinterProfileStore.Save(PrinterPath,next);
            UpdatePrinterLibrary(()=>printerProfiles[printerProfiles.IndexOf(original)]=item,item);
            Printer.Name=item.Name;Notify(nameof(Printer));NotifyPrinterState();Changed();
            PrinterProfileMessage=$"Gespeichertes Profil in „{item.Name}“ umbenannt. Zum Übernehmen anderer bearbeiteter Werte „Profil speichern“ wählen.";return true;
        }
        catch(Exception ex){PrinterProfileMessage=ex.Message;return false;}
    }
    public bool DeleteSelectedPrinter(bool confirmed=false)
    {
        if(!confirmed||Busy||InputError||printerCatalogFailed||SelectedPrinter is not {} original)return false;
        try
        {
            PrinterProfileStore.Save(PrinterPath,printerProfiles.Where(p=>!ReferenceEquals(p,original)));
            UpdatePrinterLibrary(()=>printerProfiles.Remove(original),null);
            PrinterProfileMessage=$"Gespeichertes Profil „{original.Name}“ gelöscht. Die Druckereinstellungen im Projekt bleiben erhalten.";return true;
        }
        catch(Exception ex){PrinterProfileMessage=ex.Message;return false;}
    }
}
