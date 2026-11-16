using System.Windows.Input;
using System.IO;
using LithoCreate.Core;

namespace LithoCreate.App.ViewModels;
public sealed partial class StudioViewModel
{
    public IReadOnlyList<MountingHole> BackHoles=>Model.BackMountingHoles??[];
    public string BackHoleCount=>$"{BackHoles.Count} / 8 Befestigungsbohrungen";
    public ICommand AddBackHole {get;private set;}=null!;
    public ICommand RemoveBackHole {get;private set;}=null!;
    void InitializeMounting()
    {
        AddBackHole=new RelayCommand(_=>TryAddBackHole(),()=>!Busy&&!InputError&&HasBackPanel&&BackHoles.Count<8);
        RemoveBackHole=new RelayCommand(p=>{if(p is MountingHole hole)DeleteBackHole(hole);},()=>!Busy&&HasBackPanel);
    }
    public bool TryAddBackHole()
    {
        if(Busy||InputError||!HasBackPanel||BackHoles.Count>=8)return false;
        var candidates=new List<(double X,double Y)>{(-.25,.75),(.25,.75),(-.25,.25),(.25,.25)};
        foreach(double y in new[]{.2,.35,.5,.65,.8})foreach(double x in new[]{-.3,-.15,0,.15,.3})candidates.Add((x,y));
        foreach(var p in candidates)
        {
            var next=Model.Clone();var hole=new MountingHole{X=Math.Round(p.X*Model.Width,2),Y=Math.Round(p.Y*Model.Height,2)};next.BackMountingHoles.Add(hole);
            try{next.Validate();}catch(InvalidDataException){continue;}
            Model.BackMountingHoles=[..BackHoles,hole];NotifyBackHoles();Changed();return true;
        }
        Status="Kein freier Platz für eine weitere 4-mm-Bohrung. Kastenmaße, Rand oder vorhandene Bohrungen anpassen.";return false;
    }
    public void DeleteBackHole(MountingHole hole)
    {
        if(Busy||!HasBackPanel||!BackHoles.Contains(hole))return;
        Model.BackMountingHoles=BackHoles.Where(h=>!ReferenceEquals(h,hole)).ToList();NotifyBackHoles();Changed();
    }
    void NotifyBackHoles(){Notify(nameof(BackHoles));Notify(nameof(BackHoleCount));CommandManager.InvalidateRequerySuggested();}
    public void RefreshMountingValidation(bool error)=>RefreshNumericValidation(error);
    public void RefreshNumericValidation(bool error,bool printLayout=false)
    {
        bool previous=InputError;InputError=error;
        if(error){debounce.Stop();ClearPreview();Status="Bitte die rot markierte Eingabe korrigieren (Dezimaltrennzeichen: Komma).";}
        else if(previous){if(printLayout)PrintLayoutChanged();else Changed();}
    }
}
