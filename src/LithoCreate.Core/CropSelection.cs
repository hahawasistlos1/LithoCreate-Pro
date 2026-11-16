namespace LithoCreate.Core;

/// <summary>Normalized selection in the oriented, uncropped source image.</summary>
public readonly record struct CropRegion(double X,double Y,double Width,double Height);
public static class CropSelection
{
    static double Ratio(int pixelsX,int pixelsY,double aspectWidth,double aspectHeight)
    {
        if(pixelsX<=0||pixelsY<=0)throw new InvalidDataException("Der Bildausschnitt benötigt gültige Bildmaße.");
        ValidateAspect(aspectWidth,aspectHeight);return aspectWidth/aspectHeight*pixelsY/pixelsX;
    }
    public static void ValidateAspect(double width,double height)
    {
        if(!double.IsFinite(width)||!double.IsFinite(height)||width is < .1 or > 1000||height is < .1 or > 1000)throw new InvalidDataException("Seitenverhältnis: Breite und Höhe jeweils 0,1–1000 eingeben.");
    }
    public static CropRegion Centered(int pixelsX,int pixelsY,double aspectWidth,double aspectHeight)
    {
        double ratio=Ratio(pixelsX,pixelsY,aspectWidth,aspectHeight),width=Math.Min(1,ratio),height=Math.Min(1,1/ratio);return new((1-width)/2,(1-height)/2,width,height);
    }
    public static CropRegion Drag(int pixelsX,int pixelsY,double anchorX,double anchorY,double pointerX,double pointerY,double aspectWidth,double aspectHeight,bool locked)
    {
        double ratio=Ratio(pixelsX,pixelsY,aspectWidth,aspectHeight);
        if(!new[]{anchorX,anchorY,pointerX,pointerY}.All(double.IsFinite))throw new InvalidDataException("Der Bildausschnitt benötigt endliche Koordinaten.");
        anchorX=Math.Clamp(anchorX,0,1);anchorY=Math.Clamp(anchorY,0,1);pointerX=Math.Clamp(pointerX,0,1);pointerY=Math.Clamp(pointerY,0,1);
        double dx=pointerX-anchorX,dy=pointerY-anchorY,width=Math.Abs(dx),height=Math.Abs(dy);
        if(locked){width=Math.Min(width,height*ratio);height=width/ratio;}
        return new(anchorX+(dx<0?-width:0),anchorY+(dy<0?-height:0),width,height);
    }
}
