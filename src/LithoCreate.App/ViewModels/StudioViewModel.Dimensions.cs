using System.Windows.Input;
using LithoCreate.Core;
using LithoCreate.App.Services;

namespace LithoCreate.App.ViewModels;
public sealed partial class StudioViewModel
{
    public double ModelWidth {get=>Model.Width;set{if(value!=Model.Width)SetDimensions(ModelSizing.Resize(Model,true,value));}}
    public double ModelHeight {get=>Model.Height;set{if(value!=Model.Height)SetDimensions(ModelSizing.Resize(Model,false,value));}}
    public bool LockDimensions {get=>Model.LockDimensions;set{if(value==Model.LockDimensions)return;Model.LockDimensions=value;NotifyDimensions();Changed();}}
    public bool SupportsPhotoSizing=>Model.SupportsPhotoSizing;
    public ICommand FitDimensionsToImage {get;private set;}=null!;
    void InitializeDimensions()=>FitDimensionsToImage=new RelayCommand(async _=>await FitPhotoDimensions(),()=>!Busy&&!InputError&&SupportsPhotoSizing);
    void NotifyDimensions(){Notify(nameof(ModelWidth));Notify(nameof(ModelHeight));Notify(nameof(LockDimensions));Notify(nameof(SupportsPhotoSizing));}
    void SetDimensions(PhotoSize size){if(size.Width==Model.Width&&size.Height==Model.Height)return;Model.Width=size.Width;Model.Height=size.Height;NotifyDimensions();Changed();}
    public async Task FitPhotoDimensions()
    {
        if(Busy||InputError||!SupportsPhotoSizing)return;
        Busy=true;debounce.Stop();previewCts?.Cancel();var cts=new CancellationTokenSource();exportCts=cts;int revision=sectionRevision;var settings=Model.Clone();var edits=Edits.Clone();var original=source;
        try
        {
            Status="Motivformat aus dem aktuellen Bildausschnitt berechnen …";
            var size=await Task.Run(()=>{var picture=ImageProcessor.Process(original,edits,cts.Token);var result=ModelSizing.FromImage(settings.Width,picture.Bitmap.PixelWidth,picture.Bitmap.PixelHeight);settings.Width=result.Width;settings.Height=result.Height;settings.LockDimensions=true;settings.Validate();return result;},cts.Token);
            if(cts.IsCancellationRequested||revision!=sectionRevision||InputError){Status="Motivformat verworfen: Einstellungen wurden geändert.";return;}
            bool changed=size.Width!=Model.Width||size.Height!=Model.Height||!Model.LockDimensions;Model.Width=size.Width;Model.Height=size.Height;Model.LockDimensions=true;NotifyDimensions();if(changed)Changed();
            Status=$"Motivformat übernommen · {size.Width:N3} × {size.Height:N3} mm. Breite bleibt erhalten; Maße sind jetzt gekoppelt.";
        }
        catch(OperationCanceledException){Status="Motivformat-Berechnung abgebrochen; Maße bleiben erhalten.";}
        catch(Exception ex){Status="Motivformat nicht übernommen: "+ex.Message;}
        finally{Busy=false;if(ReferenceEquals(exportCts,cts))exportCts=null;cts.Dispose();if(!InputError)debounce.Start();}
    }
}
