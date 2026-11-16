using System.IO;
using System.IO.Compression;
using System.Text.Json;
using LithoCreate.Core;
using LithoCreate.App.Services;
using LithoCreate.App.ViewModels;

partial class Program
{
    static void TestDimensions()
    {
        foreach(var example in new[]{("Original",40d,30d), ("Width",80d,60d),("Height",60d,45d),("Unlinked",30d,45d)})Test("Proportionale Maße: Dateien und reale Wandstärke · "+example.Item1,()=>
        {
            var s=new ModelSettings{Width=40,Height=30,MinThickness=3,MaxThickness=3,FrameWidth=0,Resolution=1,LockDimensions=true,PrintOrientation=PrintOrientation.FaceUp};
            if(example.Item1!="Original"){var size=ModelSizing.Resize(s,true,80);s.Width=size.Width;s.Height=size.Height;}
            if(example.Item1 is "Height" or "Unlinked"){var size=ModelSizing.Resize(s,false,45);s.Width=size.Width;s.Height=size.Height;}
            if(example.Item1=="Unlinked"){s.LockDimensions=false;var size=ModelSizing.Resize(s,true,30);s.Width=size.Width;s.Height=size.Height;}
            Require(Math.Abs(s.Width-example.Item2)<1e-10&&Math.Abs(s.Height-example.Item3)<1e-10,"Gekoppelte Maße oder Entkoppeln fehlerhaft.");var mesh=MeshGenerator.Generate(new(1,1,[255]),s,false);Require(mesh.Validate().IsValid&&Math.Abs(mesh.Validate().Size.Y-3)<.0001&&Math.Abs(mesh.Validate().Volume-s.Width*s.Height*3)<.1,"Maßänderung skaliert Wandstärke oder verliert Topologie.");
            foreach(string ext in new[]{"stl","3mf"}){string path=Path.Combine(output,$"Size-{example.Item1}.{ext}");ModelExporter.Export(path,mesh);var read=ext=="stl"?ModelExporter.ReadStl(path):ModelExporter.Read3Mf(path);Require(read.Validate().IsValid&&Math.Abs(read.Validate().Volume-s.Width*s.Height*3)<.1,"Gespeicherte Größe verändert Volumen.");}
        });
        Test("Proportionale Maße: Grenzen, Formwechsel und Pixelformat",()=>
        {
            var s=new ModelSettings{Width=100,Height=50,LockDimensions=true};Require(ModelSizing.Resize(s,false,75)==new PhotoSize(150,75),"Höhe erhält Verhältnis nicht.");foreach(double value in new[]{0,4.9,1001,double.NaN,double.PositiveInfinity})Throws<InvalidDataException>(()=>ModelSizing.Resize(s,true,value));
            s.Width=1000;s.Height=5;Throws<InvalidDataException>(()=>ModelSizing.Resize(s,false,1000));s.Shape=Shape.Cylinder;Require(ModelSizing.Resize(s,false,1000)==new PhotoSize(1000,1000),"Inaktive Bindung verändert andere Dimension bei Zylinder.");
            Require(ModelSizing.FromImage(120,600,440)==new PhotoSize(120,88),"Pixelformat wird nicht maßhaltig übernommen.");Throws<InvalidDataException>(()=>ModelSizing.FromImage(120,0,440));Throws<InvalidDataException>(()=>ModelSizing.FromImage(1000,1,1000));
        });
        Test("Format 11 erhält Maße; Format 13 speichert Kopplung und Bilddaten",()=>
        {
            byte[] source=ImageProcessor.Png(ImageProcessor.Demo());var data=new ProjectData{Version=11,Model=new(){Width=40,Height=30,LockDimensions=true,Resolution=1},Edits=new(){CropAspectWidth=16,CropAspectHeight=9,CropAspectLock=true}};var before=MeshGenerator.Generate(new(1,1,[255]),data.Model,false);string old=Path.Combine(output,"legacy-v11.lithopro");if(File.Exists(old))File.Delete(old);
            using(var zip=ZipFile.Open(old,ZipArchiveMode.Create)){using(var stream=zip.CreateEntry("project.json").Open())JsonSerializer.Serialize(stream,data);using var image=zip.CreateEntry("source.bin").Open();image.Write(source);}
            var loaded=ProjectStore.Load(old);var after=MeshGenerator.Generate(new(1,1,[255]),loaded.Data.Model,false);Require(loaded.Data.Version==15&&!loaded.Data.Model.LockDimensions&&before.Vertices.SequenceEqual(after.Vertices)&&before.Triangles.SequenceEqual(after.Triangles)&&loaded.Data.Edits.CropAspectLock,"Migration aktiviert Bindung oder verändert Bild/Geometrie.");
            string next=Path.Combine(output,"dimensions-project.lithopro");ProjectStore.Save(next,data,source);var saved=ProjectStore.Load(next);Require(saved.Data.Version==15&&saved.Data.Model.LockDimensions&&saved.Image.SequenceEqual(source)&&JsonSerializer.Serialize(data)==JsonSerializer.Serialize(saved.Data),"Bindung oder Bildbytes gehen verloren.");
        });
        Test("Editor: gekoppelte Maße, aktueller Bildzuschnitt, Ablehnung und Abbruch",()=>
        {
            string? previous=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR");Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",Path.Combine(output,"size-vm"));StudioViewModel? vm=null;
            try
            {
                vm=new();vm.ModelWidth=100;vm.ModelHeight=50;vm.LockDimensions=true;vm.ModelWidth=150;Require(vm.ModelHeight==75,"Editor koppelt Breite nicht.");vm.ModelHeight=60;Require(vm.ModelWidth==120,"Editor koppelt Höhe nicht.");Throws<InvalidDataException>(()=>vm.ModelWidth=4);Require(vm.ModelWidth==120&&vm.ModelHeight==60,"Ungültige Größe wird teilweise übernommen.");
                vm.LockDimensions=false;vm.ModelWidth=80;vm.ModelHeight=100;vm.Crop.Execute("16:9");vm.Rotate.Execute(null);var fit=vm.FitPhotoDimensions();PumpUntil(()=>fit.IsCompleted);Require(vm.LockDimensions&&Math.Abs(vm.ModelHeight-80d*600/337)<1e-8&&vm.ModelWidth==80,"Motivformat ignoriert Zuschnitt oder Drehung.");
                vm.LockDimensions=false;vm.ModelWidth=10;vm.ModelHeight=100;vm.Model.FrameWidth=4.5;vm.Crop.Execute("reset");vm.Edits.Rotation=0;var rejected=vm.FitPhotoDimensions();PumpUntil(()=>rejected.IsCompleted);Require(vm.ModelWidth==10&&vm.ModelHeight==100&&!vm.LockDimensions&&vm.Status.Contains("nicht übernommen"),"Ungültiges Zielformat verändert Projekt.");
                vm.Model.FrameWidth=1;vm.ModelWidth=100;vm.ModelHeight=80;vm.Edits.Denoise=1;vm.Edits.Sharpness=2;vm.Changed();var stale=vm.FitPhotoDimensions();vm.Changed();PumpUntil(()=>stale.IsCompleted);Require(vm.ModelWidth==100&&vm.ModelHeight==80&&!vm.LockDimensions&&!vm.Busy,"Veraltete Berechnung verändert Maße.");
                var cancelled=vm.FitPhotoDimensions();vm.Cancel.Execute(null);PumpUntil(()=>cancelled.IsCompleted);Require(vm.ModelWidth==100&&vm.ModelHeight==80&&!vm.LockDimensions&&!vm.Busy,"Abbruch verändert Maße.");
                vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Sphere);Require(!vm.SupportsPhotoSizing&&!vm.FitDimensionsToImage.CanExecute(null),"Kugel bietet rechteckiges Motivformat an.");
            }
            finally{if(vm!=null){vm.Dirty=false;vm.CleanShutdown();}Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",previous);}
        });
    }
}
