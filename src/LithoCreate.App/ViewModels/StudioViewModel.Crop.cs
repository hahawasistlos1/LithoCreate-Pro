using LithoCreate.Core;

namespace LithoCreate.App.ViewModels;
public sealed partial class StudioViewModel
{
    public double CropAspectWidth {get=>Edits.CropAspectWidth;set{CropSelection.ValidateAspect(value,Edits.CropAspectHeight);if(value!=Edits.CropAspectWidth)Edit(e=>e.CropAspectWidth=value);}}
    public double CropAspectHeight {get=>Edits.CropAspectHeight;set{CropSelection.ValidateAspect(Edits.CropAspectWidth,value);if(value!=Edits.CropAspectHeight)Edit(e=>e.CropAspectHeight=value);}}
    public bool CropAspectLock {get=>Edits.CropAspectLock;set{if(value!=Edits.CropAspectLock)Edit(e=>e.CropAspectLock=value);}}
    void NotifyCropOptions(){Notify(nameof(CropAspectWidth));Notify(nameof(CropAspectHeight));Notify(nameof(CropAspectLock));}
}
