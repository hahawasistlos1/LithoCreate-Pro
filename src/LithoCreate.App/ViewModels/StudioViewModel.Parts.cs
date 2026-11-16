using System.Windows.Input;
using LithoCreate.Core;

namespace LithoCreate.App.ViewModels;
public sealed partial class StudioViewModel
{
    public bool HasSocket=>Model.SupportsSphereOpenings&&Model.SocketEnabled;
    public bool HasBackPanel=>Model.SupportsBackPanel&&Model.BackEnabled;
    public bool HasParts=>HasSocket||HasBackPanel;
    public PartChoice[] Parts=>HasSocket?SphereSocket.Parts:HasBackPanel?LightboxBack.Parts:[SphereSocket.Parts[0]];
    public PartChoice SelectedPart {get=>Parts.First(p=>p.Part==Model.EffectivePart);set{if(value==null||value.Part==Model.Part||!Parts.Any(p=>p.Part==value.Part))return;Model.Part=value.Part;resetForPlacement=true;NotifyParts();Changed();}}
    public ModelPart? SelectedPartKind {get=>Model.EffectivePart;set{if(value is ModelPart part&&Parts.FirstOrDefault(p=>p.Part==part) is PartChoice choice)SelectedPart=choice;}}
    public bool SocketEnabled {get=>Model.SocketEnabled;set{if(value==Model.SocketEnabled)return;Model.SocketEnabled=value;if(!value)Model.Part=ModelPart.Lithophane;resetForPlacement=true;NotifyParts();Changed();}}
    public bool BackEnabled {get=>Model.BackEnabled;set{if(value==Model.BackEnabled)return;Model.BackEnabled=value;if(!value)Model.Part=ModelPart.Lithophane;resetForPlacement=true;NotifyParts();Changed();}}
    public bool IsAssemblyPart=>Model.EffectivePart==ModelPart.Assembly;
    public bool CanSlicePart=>!IsAssemblyPart;
    public bool HasPhotoSurface=>Model.EffectivePart is not (ModelPart.Socket or ModelPart.BackPanel);
    public string PartHint=>Model.EffectivePart==ModelPart.BackPanel?"Rückwand mit Einsteckrand · separat drucken. Das Foto verändert nur den Kasten.":IsAssemblyPart?"Montage aufrecht · Kontaktflächen prüfen. Einzelteile ausrichten; einen Drucksatz separat berechnen.":Model.EffectivePart==ModelPart.Socket?"Der Sockel wird separat gedruckt. Das Foto verändert nur die Lithophanie.":HasBackPanel?"Leuchtkasten und Rückwand sind getrennte Druckteile.":"Lithophanie und Stecksockel sind getrennte Druckteile.";
    public string ExportHeading=>IsPrintSetMode?"Alle Teile als 3MF-Drucksatz":IsAssemblyPart?"Einzelteil oder Drucksatz wählen":"✓  Dein Modell exportieren";
    void NotifyParts()
    {
        Notify(nameof(SelectedShadeMount));
        NotifyDimensions();NotifyBackHoles();Notify(nameof(HasSocket));Notify(nameof(HasBackPanel));Notify(nameof(HasParts));Notify(nameof(BackEnabled));Notify(nameof(Parts));Notify(nameof(SelectedPart));Notify(nameof(SelectedPartKind));Notify(nameof(SocketEnabled));Notify(nameof(IsAssemblyPart));Notify(nameof(CanSlicePart));Notify(nameof(HasPhotoSurface));Notify(nameof(PartHint));Notify(nameof(PreviewTitle));NotifyPlacement();Notify(nameof(ExportHeading));
        if(!HasParts)IsPrintSetMode=false;if(IsAssemblyPart)IsSectionMode=false;CommandManager.InvalidateRequerySuggested();
    }
}
