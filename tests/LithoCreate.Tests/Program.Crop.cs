using System.IO;
using System.IO.Compression;
using System.Text.Json;
using LithoCreate.Core;
using LithoCreate.App.Services;
using LithoCreate.App.ViewModels;

partial class Program
{
    static void TestCropRatios()
    {
        var source=GrayBitmap(120,100,Enumerable.Range(0,12000).Select(i=>(byte)((i%120+2*(i/120))%256)).ToArray());byte[] original=ImageProcessor.Png(source);File.WriteAllBytes(Path.Combine(output,"CropSource.png"),original);
        var references=new List<object>();
        foreach(var (name,w,h) in new[]{("4x5",4d,5d),("16x9",16d,9d),("5x2",5d,2d)})Test("Freies Seitenverhältnis: Pixel und Modell · "+name,()=>
        {
            var r=CropSelection.Centered(120,100,w,h);Require(Math.Abs(r.Width*120/(r.Height*100)-w/h)<1e-12&&Math.Abs(r.X*2+r.Width-1)<1e-12&&Math.Abs(r.Y*2+r.Height-1)<1e-12,"Ausschnitt ist nicht zentriert oder proportional.");
            var edits=new ImageEdits{CropX=r.X,CropY=r.Y,CropWidth=r.Width,CropHeight=r.Height,CropAspectWidth=w,CropAspectHeight=h,CropAspectLock=true};var picture=ImageProcessor.Process(source,edits);Require(picture.Bitmap.PixelWidth==Math.Max(1,(int)(r.Width*120))&&picture.Bitmap.PixelHeight==Math.Max(1,(int)(r.Height*100)),"Falsche Pixelmaße.");File.WriteAllBytes(Path.Combine(output,$"Crop-{name}.png"),ImageProcessor.Png(picture.Bitmap));
            var settings=new ModelSettings{Width=40,Height=30,Resolution=1,FrameWidth=0};var mesh=MeshGenerator.Generate(picture.Gray,settings,false);Require(mesh.Validate().IsValid&&Math.Abs(mesh.Validate().Size.X-40)<.001&&Math.Abs(mesh.Validate().Size.Y-30)<.001,"Zuschneiden verändert Modellmaße oder Topologie.");
            references.Add(new{Name=name,AspectWidth=w,AspectHeight=h,Region=r,PixelWidth=picture.Bitmap.PixelWidth,PixelHeight=picture.Bitmap.PixelHeight});Require(original.SequenceEqual(ImageProcessor.Png(source)),"Zuschneiden verändert Originalbitmap.");
        });
        File.WriteAllText(Path.Combine(output,"crop-references.json"),JsonSerializer.Serialize(references,ProjectStore.Json));
        Test("Verhältnisbindung: vier Ziehrichtungen, Ränder und freie Auswahl",()=>
        {
            foreach(double x in new[]{.05,.5,.95})foreach(double y in new[]{.05,.5,.95})foreach(double px in new[]{-.5,.01,.4,.9,1.5})foreach(double py in new[]{-.5,.01,.4,.9,1.5})
            {
                var r=CropSelection.Drag(600,440,x,y,px,py,16,9,true);Require(r.X>=-1e-12&&r.Y>=-1e-12&&r.X+r.Width<=1+1e-12&&r.Y+r.Height<=1+1e-12,"Gebundene Auswahl verlässt das Bild.");Require(r.Width==0&&r.Height==0||Math.Abs(r.Width*600/(r.Height*440)-16d/9)<1e-10,"Verhältnis hängt von der Ziehrichtung ab.");
                double pointerX=Math.Clamp(px,0,1),pointerY=Math.Clamp(py,0,1);Require(r.Width<=Math.Abs(pointerX-x)+1e-12&&r.Height<=Math.Abs(pointerY-y)+1e-12,"Auswahl verlässt das aufgezogene Rechteck.");
            }
            var free=CropSelection.Drag(600,440,.8,.7,.2,.1,4,5,false);Require(Math.Abs(free.X-.2)<1e-12&&Math.Abs(free.Y-.1)<1e-12&&Math.Abs(free.Width-.6)<1e-12&&Math.Abs(free.Height-.6)<1e-12,"Freies Aufziehen verändert alte Koordinaten.");
        });
        Test("Seitenverhältnisse: Grenzen und ungültige Werte",()=>
        {
            foreach(double value in new[]{0,.09,1001,double.NaN,double.PositiveInfinity}){Throws<InvalidDataException>(()=>CropSelection.Centered(120,100,value,5));Throws<InvalidDataException>(()=>new ImageEdits{CropAspectHeight=value}.Validate());}
            Throws<InvalidDataException>(()=>CropSelection.Centered(0,100,4,5));Throws<InvalidDataException>(()=>CropSelection.Drag(120,100,.5,.5,double.NaN,.8,4,5,true));
            foreach(var p in new[]{(.1,1000d),(1000d,.1)}){var r=CropSelection.Centered(120,100,p.Item1,p.Item2);Require(r.Width>0&&r.Height>0&&r.Width<=1&&r.Height<=1,"Extremes zulässiges Verhältnis ist ungültig.");}
        });
        Test("Format 10 behält Bildausschnitt; Format 13 speichert Verhältnis und Original",()=>
        {
            var r=CropSelection.Centered(120,100,3,2);var data=new ProjectData{Version=10,Edits=new(){CropX=r.X,CropY=r.Y,CropWidth=r.Width,CropHeight=r.Height,CropAspectWidth=16,CropAspectHeight=9,CropAspectLock=true}};var before=ImageProcessor.Process(source,data.Edits);string path=Path.Combine(output,"legacy-v10.lithopro");if(File.Exists(path))File.Delete(path);
            using(var zip=ZipFile.Open(path,ZipArchiveMode.Create)){using(var stream=zip.CreateEntry("project.json").Open())JsonSerializer.Serialize(stream,data);using var image=zip.CreateEntry("source.bin").Open();image.Write(original);}
            var migrated=ProjectStore.Load(path);Require(migrated.Data.Version==15&&migrated.Data.Edits.CropAspectWidth==4&&migrated.Data.Edits.CropAspectHeight==5&&!migrated.Data.Edits.CropAspectLock,"Migration aktiviert Verhältnisbindung.");var after=ImageProcessor.Process(source,migrated.Data.Edits);Require(ImageProcessor.Png(before.Bitmap).SequenceEqual(ImageProcessor.Png(after.Bitmap))&&migrated.Image.SequenceEqual(original),"Migration verändert Ausschnitt oder Original.");
            data.Edits.CropAspectWidth=2.5;data.Edits.CropAspectHeight=1;string next=Path.Combine(output,"crop-ratio.lithopro");ProjectStore.Save(next,data,original);var stored=ProjectStore.Load(next);Require(stored.Data.Version==15&&JsonSerializer.Serialize(stored.Data)==JsonSerializer.Serialize(data)&&stored.Image.SequenceEqual(original),"Verhältnisprojekt verändert Daten.");
        });
        Test("Fotoeditor: Verhältniswahl, Undo/Redo, Mausmodus und Rücksetzen",()=>
        {
            var previous=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR");Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",Path.Combine(output,"crop-vm"));StudioViewModel? vm=null;
            try
            {
                vm=new();vm.LoadImage(Path.Combine(output,"CropSource.png"));double width=vm.Model.Width,height=vm.Model.Height;vm.CropAspectWidth=16;vm.CropAspectHeight=9;vm.Crop.Execute("custom");var centered=vm.Edits.Clone();Require(vm.CropAspectLock&&!vm.ShowOriginal&&Math.Abs(vm.Edits.CropHeight-.675)<1e-12,"Eigenes Verhältnis wird nicht angewandt.");
                vm.Undo.Execute(null);Require(vm.Edits.CropWidth==1&&vm.Edits.CropHeight==1&&!vm.CropAspectLock,"Undo stellt falschen Ausschnitt wieder her.");vm.Redo.Execute(null);Require(JsonSerializer.Serialize(centered)==JsonSerializer.Serialize(vm.Edits),"Redo verliert Verhältnis oder Ausschnitt.");
                vm.Crop.Execute("free");Require(vm.ShowOriginal,"Mausmodus zeigt nicht das Original.");var r=CropSelection.Drag(120,100,.1,.2,.8,.9,16,9,true);vm.SetCrop(r.X,r.Y,r.Width,r.Height);Require(!vm.ShowOriginal&&vm.CropAspectLock&&vm.Model.Width==width&&vm.Model.Height==height,"Mausauswahl verändert Modellmaße oder Bindung.");vm.Undo.Execute(null);Require(JsonSerializer.Serialize(vm.Edits)==JsonSerializer.Serialize(centered),"Mausauswahl ist nicht rückgängig machbar.");
                vm.ResetImage.Execute(null);Require(vm.CropAspectWidth==4&&vm.CropAspectHeight==5&&!vm.CropAspectLock&&vm.Edits.CropWidth==1,"Bildreset verliert neue Standardwerte.");Throws<InvalidDataException>(()=>vm.CropAspectWidth=0);Require(vm.CropAspectWidth==4,"Ungültiges Verhältnis wird ins Projekt übernommen.");
            }
            finally{if(vm!=null){vm.Dirty=false;vm.CleanShutdown();}Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",previous);}
        });
    }
}
