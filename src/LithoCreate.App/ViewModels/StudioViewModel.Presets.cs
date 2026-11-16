using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using LithoCreate.Core;

namespace LithoCreate.App.ViewModels;
public sealed partial class StudioViewModel
{
    readonly ObservableCollection<ModelPreset> modelPresets=[];
    public ReadOnlyObservableCollection<ModelPreset> ModelPresets {get;private set;}=null!;
    ModelPreset? selectedModelPreset;
    string presetName="",presetMessage="Noch keine eigenen Vorlagen. Aktuelle Modelleinstellungen unter einem Namen speichern.";
    bool presetCatalogFailed;
    string PresetPath=>Path.Combine(dataDir,"model-presets.json");
    public ModelPreset? SelectedModelPreset {get=>selectedModelPreset;set{selectedModelPreset=value;Notify();Notify(nameof(PresetDescription));if(value!=null)PresetName=value.Name;CommandManager.InvalidateRequerySuggested();}}
    public string PresetName {get=>presetName;set{presetName=value;Notify();}}
    public string PresetDescription=>SelectedModelPreset?.Description??"Form, Maße, Zubehör und Drucklagen. Foto, Bildkorrekturen und Druckerprofil bleiben beim Anwenden erhalten.";
    public string PresetMessage {get=>presetMessage;private set{presetMessage=value;Notify();}}
    public ICommand SaveModelPreset {get;private set;}=null!;
    public ICommand ApplyModelPreset {get;private set;}=null!;
    public ICommand RenameModelPreset {get;private set;}=null!;
    public ICommand DeleteModelPreset {get;private set;}=null!;
    void InitializePresets()
    {
        ModelPresets=new(modelPresets);
        SaveModelPreset=new RelayCommand(_=>SaveCurrentModelPreset(),()=>!Busy&&!InputError&&!presetCatalogFailed);
        ApplyModelPreset=new RelayCommand(_=>ApplySelectedModelPreset(),()=>!Busy&&!InputError&&SelectedModelPreset!=null);
        RenameModelPreset=new RelayCommand(_=>RenameSelectedModelPreset(),()=>!Busy&&!presetCatalogFailed&&SelectedModelPreset!=null);
        DeleteModelPreset=new RelayCommand(_=>
        {
            if(SelectedModelPreset is {} item&&MessageBox.Show($"Eigene Vorlage „{item.Name}“ löschen? Bereits gespeicherte Projekte bleiben erhalten.","Vorlage löschen",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)DeleteSelectedModelPreset(true);
        },()=>!Busy&&!presetCatalogFailed&&SelectedModelPreset!=null);
        try
        {
            foreach(var item in ModelPresetStore.Load(PresetPath).Items)modelPresets.Add(item);
            if(modelPresets.Count>0){SelectedModelPreset=modelPresets[0];PresetMessage=$"{modelPresets.Count} eigene Vorlagen lokal geladen.";}
        }
        catch(Exception ex){presetCatalogFailed=true;PresetMessage="Eigene Vorlagen konnten nicht geladen werden. Die Datei model-presets.json bleibt unverändert. "+ex.Message;}
    }
    public bool SaveCurrentModelPreset()
    {
        if(Busy||InputError||presetCatalogFailed)return false;
        try
        {
            var item=new ModelPreset{Name=PresetName.Trim(),Model=Model.Clone()};
            var next=modelPresets.Append(item).ToArray();ModelPresetStore.Save(PresetPath,next);
            modelPresets.Add(item);SelectedModelPreset=item;PresetMessage=$"Vorlage „{item.Name}“ lokal gespeichert.";return true;
        }
        catch(Exception ex){PresetMessage=ex.Message;return false;}
    }
    public bool ApplySelectedModelPreset()
    {
        if(Busy||InputError||SelectedModelPreset is not {} item)return false;
        try
        {
            var candidate=item.Model.Clone();candidate.MemoryMb=Model.MemoryMb;candidate.Validate();
            ClearPreview();project.Model=candidate;resetForPlacement=true;Refresh();Changed(false);Page="Editor";Step=3;
            PresetMessage=$"Vorlage „{item.Name}“ angewendet.";Status=PresetMessage;return true;
        }
        catch(Exception ex){PresetMessage="Vorlage nicht übernommen: "+ex.Message;return false;}
    }
    public bool RenameSelectedModelPreset()
    {
        if(Busy||presetCatalogFailed||SelectedModelPreset is not {} item)return false;
        try
        {
            var replacement=item.Clone();replacement.Name=PresetName.Trim();
            var next=modelPresets.Select(p=>p.Id==item.Id?replacement:p).ToArray();ModelPresetStore.Save(PresetPath,next);
            modelPresets[modelPresets.IndexOf(item)]=replacement;SelectedModelPreset=replacement;PresetMessage=$"Vorlage in „{replacement.Name}“ umbenannt.";return true;
        }
        catch(Exception ex){PresetMessage=ex.Message;return false;}
    }
    public bool DeleteSelectedModelPreset(bool confirmed=false)
    {
        if(!confirmed||Busy||presetCatalogFailed||SelectedModelPreset is not {} item)return false;
        try
        {
            ModelPresetStore.Save(PresetPath,modelPresets.Where(p=>p.Id!=item.Id));
            int index=modelPresets.IndexOf(item);modelPresets.Remove(item);SelectedModelPreset=modelPresets.Count==0?null:modelPresets[Math.Min(index,modelPresets.Count-1)];
            if(modelPresets.Count==0)PresetName="";PresetMessage=$"Vorlage „{item.Name}“ gelöscht. Projekte bleiben erhalten.";return true;
        }
        catch(Exception ex){PresetMessage=ex.Message;return false;}
    }
}
