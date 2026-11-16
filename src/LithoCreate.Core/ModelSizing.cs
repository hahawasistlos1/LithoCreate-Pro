namespace LithoCreate.Core;

public readonly record struct PhotoSize(double Width,double Height);
public static class ModelSizing
{
    public static PhotoSize Resize(ModelSettings settings,bool widthChanged,double value)
    {
        Validate(settings.Width,settings.Height);if(!double.IsFinite(value)||value is < 5 or > 1000)throw new InvalidDataException("Breite und Höhe: 5–1000 mm eingeben.");
        bool linked=settings.SupportsPhotoSizing&&settings.LockDimensions;
        double w=widthChanged?value:linked?value*settings.Width/settings.Height:settings.Width;
        double h=!widthChanged?value:linked?value*settings.Height/settings.Width:settings.Height;
        Validate(w,h);return new(w,h);
    }
    public static PhotoSize FromImage(double width,int pixelsX,int pixelsY)
    {
        if(pixelsX<=0||pixelsY<=0)throw new InvalidDataException("Das Motiv benötigt gültige Pixelmaße.");
        double height=width*pixelsY/pixelsX;Validate(width,height);return new(width,height);
    }
    static void Validate(double width,double height)
    {
        if(!double.IsFinite(width)||!double.IsFinite(height)||width is < 5 or > 1000||height is < 5 or > 1000)throw new InvalidDataException("Die berechnete Breite und Höhe müssen jeweils zwischen 5 und 1000 mm liegen. Bindung lösen oder andere Maße wählen.");
    }
}
