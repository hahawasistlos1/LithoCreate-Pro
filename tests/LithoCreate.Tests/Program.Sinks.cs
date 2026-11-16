using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using LithoCreate.Core;
using LithoCreate.App.Services;

partial class Program
{
    static ModelSettings SinkFixture(string mode="Mixed")
    {
        var s=SlotFixture();
        for(int i=0;i<4;i++){var h=s.BackMountingHoles[i];h.SinkDepth=mode=="Mixed"?new[]{1.2,.8,.4,0}[i]:1;if(mode=="Round"||mode=="Mixed"&&i==0)h.SlotExtension=0;}
        return s;
    }
    static double SinkArea(ModelSettings s,double z)=>s.Width*s.Height-Math.PI*s.BackHoleDiameter*s.BackHoleDiameter/4-s.BackMountingHoles.Sum(h=>{double r=h.Diameter/2+Math.Max(0,h.SinkDepth-z);return Math.PI*r*r+2*r*h.SlotExtension;});
    static double SinkVolume(ModelSettings s)
    {
        double w=s.Width-2*(s.FrameWidth+s.BackClearance),h=s.Height-2*(s.FrameWidth+s.BackClearance),ring=w*h-(w-2*s.BackRimWidth)*(h-2*s.BackRimWidth);
        return SinkArea(s,s.BackThickness)*s.BackThickness+ring*s.BackInsertionDepth-s.BackMountingHoles.Sum(h=>h.SinkDepth*h.SinkDepth*(Math.PI*h.Diameter/2+h.SlotExtension)+Math.PI*Math.Pow(h.SinkDepth,3)/3);
    }
    static void TestSinks(GrayImage gray)
    {
        foreach(string mode in new[]{"Round","Slots","Mixed"})Test("Senkungen: Außenkontur, Tiefenprofil, Volumen und Dateien · "+mode,()=>
        {
            var s=SinkFixture(mode);
            foreach(bool preview in new[]{true,false})
            {
                var mesh=MeshGenerator.Generate(gray,s,preview);var report=mesh.Validate();Require(report.IsValid&&Euler(mesh)==-8,"Senkung erzeugt ungültiges Netz: "+report);
                Require(Vector3.Distance(report.Size,new(100,5,85))<.001&&mesh.PhotoVertices.All(v=>!v),"Senkung verändert Hülle oder Fotozuordnung.");
                Require(Math.Abs(report.Volume-SinkVolume(s))<3,"Senkungsvolumen weicht von analytischem Integral ab: "+report.Volume+" / "+SinkVolume(s));
                var index=new CrossSectionIndex(mesh);
                foreach(double z in new[]{.05,.35,.9,1.5})
                {
                    var slice=index.Slice(z);Require(slice.IsRegular&&slice.ClosedContours==6,"Senkung verliert Öffnung im Schichtschnitt.");Require(Math.Abs(slice.Area-SinkArea(s,z))<1.5,"Falsches Tiefenprofil oder falsche Seite: "+z+" / "+slice.Area);
                    foreach(var hole in s.BackMountingHoles.Where(h=>h.SinkDepth>0))
                    {
                        var path=slice.Paths.OrderBy(p=>Vector2.Distance(new((p.Points.Min(v=>v.X)+p.Points.Max(v=>v.X))/2,(p.Points.Min(v=>v.Y)+p.Points.Max(v=>v.Y))/2),new((float)hole.X,(float)hole.Y))).First();
                        double width=hole.Diameter+2*Math.Max(0,hole.SinkDepth-z);var size=new Vector2(path.Points.Max(v=>v.X)-path.Points.Min(v=>v.X),path.Points.Max(v=>v.Y)-path.Points.Min(v=>v.Y));
                        Require(Vector2.Distance(size,new((float)(width+(hole.Horizontal?hole.SlotExtension:0)),(float)(width+(hole.Horizontal?0:hole.SlotExtension))))<.005,"Senkungsbreite oder 90°-Profil falsch.");
                    }
                }
                if(!preview)foreach(string ext in new[]{"stl","3mf"}){string path=Path.Combine(output,$"Sinks-{mode}.{ext}");ModelExporter.Export(path,mesh);var read=ext=="stl"?ModelExporter.ReadStl(path):ModelExporter.Read3Mf(path);Require(read.Validate().IsValid&&Math.Abs(read.Validate().Volume-report.Volume)<.3,"Senkungsdatei verliert Geometrie.");}
            }
        });
        Test("Senkungen: Grenztiefen, kleine Öffnungen und zufällige Konturen",()=>
        {
            foreach(double thickness in new[]{.8,2d,10d})foreach(double depth in new[]{.1,thickness-.4})foreach(double diameter in new[]{2d,20d})foreach(bool horizontal in new[]{false,true})
            {
                var s=MountingFixture(0,false);s.BackThickness=thickness;s.BackMountingHoles=[new(){X=.123,Y=42.5,Diameter=diameter,SlotExtension=8,Horizontal=horizontal,SinkDepth=depth}];
                Require(MeshGenerator.Generate(gray,s,false).Validate().IsValid,"Grenztiefe erzeugt ungültige Senkung: "+thickness+" / "+depth);
            }
            var random=new Random(1110);for(int i=0;i<25;i++){var s=SinkFixture();foreach(var hole in s.BackMountingHoles){hole.X+=random.NextDouble()-.5;hole.Y+=random.NextDouble()-.5;hole.SinkDepth=.1+random.NextDouble()*1.4;hole.Diameter=2+random.NextDouble()*3;hole.SlotExtension=random.NextDouble()*8;}Require(MeshGenerator.Generate(gray,s,i%2==0).Validate().IsValid,"Unregelmäßiger Senkungsmix ungültig.");}
        });
        Test("Senkungen: Restdurchgang, Freizonen, ungültige Werte und Abbruch",()=>
        {
            foreach(double depth in new[]{-.1,.01,1.61,double.NaN,double.PositiveInfinity}){var s=SinkFixture();s.BackMountingHoles[0].SinkDepth=depth;Throws<InvalidDataException>(s.Validate);}
            var edge=MountingFixture(1,false);edge.BackMountingHoles[0].X=-42;edge.BackMountingHoles[0].Y=42.5;edge.Validate();edge.BackMountingHoles[0].SinkDepth=1.6;Throws<InvalidDataException>(edge.Validate);
            var collision=MountingFixture(0,false);collision.BackMountingHoles=[new(){X=-3.1,Y=42.5},new(){X=3.1,Y=42.5}];collision.Validate();collision.BackMountingHoles[0].SinkDepth=1;Throws<InvalidDataException>(collision.Validate);
            using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>BackMounting.Generate(SinkFixture(),false,cts.Token));
        });
        Test("Senkungen: Format 9 bleibt identisch, Format 13 erhält Werte und Foto",()=>
        {
            var s=SlotFixture();s.BackAngles=new(17,-9,23);var before=MeshGenerator.Generate(gray,s,false);byte[] image=ImageProcessor.Png(ImageProcessor.Demo());string old=Path.Combine(output,"legacy-v9.lithopro");if(File.Exists(old))File.Delete(old);
            using(var z=ZipFile.Open(old,ZipArchiveMode.Create)){using(var stream=z.CreateEntry("project.json").Open())JsonSerializer.Serialize(stream,new ProjectData{Version=9,Model=s});using var source=z.CreateEntry("source.bin").Open();source.Write(image);}
            var loaded=ProjectStore.Load(old);var after=MeshGenerator.Generate(gray,loaded.Data.Model,false);Require(loaded.Data.Version==15&&loaded.Data.Model.BackMountingHoles.All(h=>h.SinkDepth==0)&&s.BackMountingHoles.Zip(loaded.Data.Model.BackMountingHoles).All(p=>p.First.SlotExtension==p.Second.SlotExtension&&p.First.Horizontal==p.Second.Horizontal)&&loaded.Data.Model.BackAngles==s.BackAngles&&before.Vertices.SequenceEqual(after.Vertices)&&before.Triangles.SequenceEqual(after.Triangles),"Migration verändert Konturen oder Druckwinkel.");
            var data=new ProjectData{Model=SinkFixture()};var copy=data.Model.Clone();copy.BackMountingHoles[0].SinkDepth=.1;Require(data.Model.BackMountingHoles[0].SinkDepth==1.2,"Snapshot verändert Originalsenkung.");string path=Path.Combine(output,"sinks-project.lithopro");ProjectStore.Save(path,data,image);var roundtrip=ProjectStore.Load(path);Require(JsonSerializer.Serialize(data)==JsonSerializer.Serialize(roundtrip.Data)&&roundtrip.Image.SequenceEqual(image),"Senkungsprojekt verändert Daten.");
        });
        Test("Senkungen: Drucklagen, freie Winkel, Montage und Drucksatz",()=>
        {
            var s=SinkFixture();double volume=MeshGenerator.Generate(gray,s,false).Validate().Volume;
            foreach(var orientation in Enum.GetValues<PrintOrientation>())foreach(bool turn in new[]{false,true}){s.BackPrintOrientation=orientation;s.BackTurnOnBed90=turn;s.BackAngles=new(23,-17,8);var report=MeshGenerator.Generate(gray,s,false).Validate();Require(report.IsValid&&Math.Abs(report.Volume-volume)<.3,"Drucklage verändert Senkung.");}
            s.Part=ModelPart.Assembly;var a=MeshGenerator.Generate(gray,s,true);s.BackAngles=default;s.BackTurnOnBed90=false;s.BackPrintOrientation=PrintOrientation.FaceUp;var b=MeshGenerator.Generate(gray,s,true);Require(a.IsAssemblyPreview&&a.Vertices.SequenceEqual(b.Vertices),"Montage verändert sich durch Drucklage.");
            var set=ModelComputation.BuildPrintSet(ImageProcessor.Png(ImageProcessor.Demo()),s,new(),new(),CancellationToken.None);Require(set.Parts.Count==2&&s.Part==ModelPart.Assembly,"Drucksatz verändert Teilwahl.");
        });
    }
}
