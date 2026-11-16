using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LithoCreate.Core;

namespace LithoCreate.App.Services;
public record ProcessedImage(BitmapSource Bitmap,GrayImage Gray,int[] Histogram,BitmapSource BeforeBitmap);
public static class ImageProcessor
{
    public static BitmapSource Decode(byte[] bytes,int max=1800,bool autoOrient=true)
    {
        if(bytes.Length>64*1024*1024)throw new InvalidDataException("Die Bilddatei ist größer als 64 MB.");
        using var stream=new MemoryStream(bytes);
        var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnDemand);
        var frame=decoder.Frames[0];int orientation=autoOrient?ReadOrientation(frame):1;
        if(frame.PixelWidth<=0||frame.PixelHeight<=0)throw new InvalidDataException("Das Bild hat ungültige Abmessungen.");
        int targetWidth=Math.Max(1,(int)(frame.PixelWidth*Math.Min(1,max/(double)Math.Max(frame.PixelWidth,frame.PixelHeight))));
        stream.Position=0;
        var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;image.DecodePixelWidth=targetWidth;image.EndInit();image.Freeze();
        if((long)image.PixelWidth*image.PixelHeight>16000000)throw new InvalidDataException("Das Bild ist zu hoch. Bitte ein weniger extremes Seitenverhältnis verwenden.");
        return Orient(image,orientation);
    }
    static int ReadOrientation(BitmapFrame frame)
    {
        try
        {
            if(frame.Metadata is BitmapMetadata metadata)foreach(string query in new[]{"/app1/ifd/{ushort=274}","/ifd/{ushort=274}"})
                if(metadata.ContainsQuery(query)&&metadata.GetQuery(query) is object value){int orientation=Convert.ToInt32(value);return orientation is >=1 and <=8?orientation:1;}
        }
        catch(Exception ex) when(ex is NotSupportedException or ArgumentException or InvalidOperationException or OverflowException or FormatException or System.Runtime.InteropServices.COMException){ }
        return 1;
    }
    static BitmapSource Orient(BitmapSource image,int orientation)
    {
        if(orientation==1)return image;
        var transforms=new TransformGroup();
        if(orientation is 2 or 5 or 7)transforms.Children.Add(new ScaleTransform(-1,1));
        if(orientation==4)transforms.Children.Add(new ScaleTransform(1,-1));
        double angle=orientation switch{3=>180,5 or 8=>270,6 or 7=>90,_=>0};if(angle!=0)transforms.Children.Add(new RotateTransform(angle));
        var result=new TransformedBitmap(image,transforms);result.Freeze();return result;
    }
    public static ProcessedImage Process(BitmapSource source,ImageEdits e,CancellationToken token=default)
    {
        e.Validate();
        int x=Math.Min(source.PixelWidth-1,(int)(e.CropX*source.PixelWidth)),y=Math.Min(source.PixelHeight-1,(int)(e.CropY*source.PixelHeight));
        int cw=Math.Clamp((int)(e.CropWidth*source.PixelWidth),1,source.PixelWidth-x),ch=Math.Clamp((int)(e.CropHeight*source.PixelHeight),1,source.PixelHeight-y);
        BitmapSource current=new CroppedBitmap(source,new Int32Rect(x,y,cw,ch));
        var transforms=new TransformGroup();if(e.Mirror)transforms.Children.Add(new ScaleTransform(-1,1));if(e.Rotation%360!=0)transforms.Children.Add(new RotateTransform(e.Rotation));
        if(transforms.Children.Count>0)current=new TransformedBitmap(current,transforms);
        current=new FormatConvertedBitmap(current,PixelFormats.Bgra32,null,0);
        int w=current.PixelWidth,h=current.PixelHeight;byte[] input=new byte[w*h*4];current.CopyPixels(input,w*4,0);
        // Composite alpha on white before resampling, avoiding dark fringes around transparency.
        for(int j=0;j<h;j++){token.ThrowIfCancellationRequested();for(int i=0;i<w;i++){int p=(j*w+i)*4;if(input[p+3]<255){double alpha=input[p+3]/255.0;for(int c=0;c<3;c++)input[p+c]=(byte)Math.Round(input[p+c]*alpha+255*(1-alpha));input[p+3]=255;}}}
        if(e.StraightenAngle!=0)input=RotateFine(input,w,h,e.StraightenAngle,token);
        var before=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,input,w*4);before.Freeze();
        var output=new byte[input.Length];var gray=new byte[w*h];var histogram=new int[256];
        for(int j=0;j<h;j++)
        {
            token.ThrowIfCancellationRequested();
            for(int i=0;i<w;i++)
            {
                int p=(j*w+i)*4;double b=C(0),g=C(1),r=C(2);double lum=.2126*r+.7152*g+.0722*b;
                if(e.Grayscale)b=g=r=lum;
                output[p]=(byte)Math.Clamp(b*255,0,255);output[p+1]=(byte)Math.Clamp(g*255,0,255);output[p+2]=(byte)Math.Clamp(r*255,0,255);output[p+3]=255;
                byte value=(byte)Math.Clamp(lum*255,0,255);gray[j*w+i]=value;histogram[value]++;
                double C(int channel)
                {
                    double alpha=input[p+3]/255.0;double original=(input[p+channel]/255.0)*alpha+(1-alpha);
                    double sum=0;int n=0;
                    if(e.Denoise>0||e.Sharpness>0)
                    {
                        for(int yy=Math.Max(0,j-1);yy<=Math.Min(h-1,j+1);yy++)for(int xx=Math.Max(0,i-1);xx<=Math.Min(w-1,i+1);xx++){int q=(yy*w+xx)*4;double a=input[q+3]/255.0;sum+=input[q+channel]/255.0*a+(1-a);n++;}
                        double average=sum/n;original=original*(1-e.Denoise)+average*e.Denoise+e.Sharpness*(original-average);
                    }
                    double v=Math.Clamp((original-e.BlackPoint)/(e.WhitePoint-e.BlackPoint),0,1);
                    v=Math.Clamp((v-.5)*e.Contrast+.5+e.Brightness,0,1);v=Math.Pow(v,1/e.Gamma);return e.Invert?1-v:v;
                }
            }
        }
        var bitmap=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,output,w*4);bitmap.Freeze();return new(bitmap,new(w,h,gray),histogram,before);
    }
    static byte[] RotateFine(byte[] input,int width,int height,double degrees,CancellationToken token)
    {
        double angle=degrees*Math.PI/180,cos=Math.Cos(angle),sin=Math.Sin(angle);
        // Keep the selected aspect ratio and zoom just enough to avoid empty corner wedges.
        double zoom=Math.Max((Math.Abs(cos)*width+Math.Abs(sin)*height)/width,(Math.Abs(sin)*width+Math.Abs(cos)*height)/height);
        double cx=(width-1)/2.0,cy=(height-1)/2.0;var output=new byte[input.Length];
        for(int y=0;y<height;y++)
        {
            token.ThrowIfCancellationRequested();for(int x=0;x<width;x++)
            {
                double dx=(x-cx)/zoom,dy=(y-cy)/zoom;
                double sx=Math.Clamp(cx+cos*dx+sin*dy,0,width-1),sy=Math.Clamp(cy-sin*dx+cos*dy,0,height-1);
                int ix=(int)sx,iy=(int)sy,nx=Math.Min(ix+1,width-1),ny=Math.Min(iy+1,height-1);double fx=sx-ix,fy=sy-iy;int p=(y*width+x)*4;
                for(int c=0;c<3;c++){double a=input[(iy*width+ix)*4+c]*(1-fx)+input[(iy*width+nx)*4+c]*fx,b=input[(ny*width+ix)*4+c]*(1-fx)+input[(ny*width+nx)*4+c]*fx;output[p+c]=(byte)Math.Clamp(Math.Round(a*(1-fy)+b*fy),0,255);}output[p+3]=255;
            }
        }
        return output;
    }
    public static byte[] Png(BitmapSource bitmap)
    {
        using var ms=new MemoryStream();var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));encoder.Save(ms);return ms.ToArray();
    }
    public static BitmapSource Demo()
    {
        const int w=600,h=440;byte[] p=new byte[w*h*4];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            double u=x/(double)w,v=y/(double)h;double sky=210-110*v+18*Math.Sin(x*.018+y*.025);
            double mountain=.62+.12*Math.Sin(u*8)+.05*Math.Cos(u*24);
            double value=v>mountain?70+45*Math.Sin(x*.013+y*.03):sky;
            bool tower=Math.Abs(u-.57)<.045&&v>.23&&v<.77;
            if(tower)value=180+40*Math.Sin(x*.12);
            if(v>.19&&v<.24&&Math.Abs(u-.57)<.067)value=38;
            if(v>.145&&v<.19&&Math.Abs(u-.57)<(.19-v)*1.5)value=40;
            if(v>.255&&v<.31&&Math.Abs(u-.57)<.025)value=250;
            if(v>.70&&Math.Abs(u-.57)<.13)value=90;
            int n=(y*w+x)*4;p[n]=(byte)Math.Clamp(value*.91,0,255);p[n+1]=(byte)Math.Clamp(value*.97,0,255);p[n+2]=(byte)Math.Clamp(value,0,255);p[n+3]=255;
        }
        var result=BitmapSource.Create(w,h,96,96,PixelFormats.Bgra32,null,p,w*4);result.Freeze();return result;
    }
}
