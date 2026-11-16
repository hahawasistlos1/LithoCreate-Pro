using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using LithoCreate.Core;
using LithoCreate.App.Services;

partial class Program
{
    static ModelSettings SlotFixture(string mode="Mixed")
    {
        var s=MountingFixture(4,true);
        for(int i=0;i<4;i++){s.BackMountingHoles[i].SlotExtension=8;s.BackMountingHoles[i].Horizontal=mode=="Horizontal"||mode=="Mixed"&&i%2==1;}
        return s;
    }
    static void TestSlots(GrayImage gray)
    {
        foreach(string mode in new[]{"Vertical","Horizontal","Mixed"})Test("Langlöcher: Geometrie, Schichten und Dateien · "+mode,()=>
        {
            var s=SlotFixture(mode);
            foreach(bool preview in new[]{true,false})
            {
                var mesh=MeshGenerator.Generate(gray,s,preview);var report=mesh.Validate();
                Require(report.IsValid&&Euler(mesh)==-8,"Langloch-Netz ungültig: "+report);
                Require(Vector3.Distance(report.Size,new(100,5,85))<.001&&mesh.PhotoVertices.All(v=>!v),"Außenmaße oder Fotozuordnung verändert.");
                var slice=new CrossSectionIndex(mesh).Slice(.7);Require(slice.IsRegular&&slice.ClosedContours==6,"Langlöcher sind keine durchgehenden Öffnungen.");
                double expected=8500-4*(Math.PI*4+4*8)-Math.PI*16;
                Require(Math.Abs(slice.Area-expected)<1.2,"Langlochfläche weicht von Kapselreferenz ab: "+slice.Area);
                double ring=93.5*78.5-90.3*75.3;Require(Math.Abs(report.Volume-(slice.Area*2+ring*3))<.3,"Platten- oder Ringvolumen verändert.");
                foreach(var hole in s.BackMountingHoles)
                {
                    var path=slice.Paths.OrderBy(p=>Vector2.Distance(new((p.Points.Min(v=>v.X)+p.Points.Max(v=>v.X))/2,(p.Points.Min(v=>v.Y)+p.Points.Max(v=>v.Y))/2),new((float)hole.X,(float)hole.Y))).First();
                    var size=new Vector2(path.Points.Max(v=>v.X)-path.Points.Min(v=>v.X),path.Points.Max(v=>v.Y)-path.Points.Min(v=>v.Y));
                    Require(Vector2.Distance(size,hole.Horizontal?new(12,4):new(4,12))<.001,"Freie Breite, Gesamtlänge oder Richtung falsch.");
                }
                if(!preview)foreach(string extension in new[]{"stl","3mf"}){string path=Path.Combine(output,$"Slots-{mode}.{extension}");ModelExporter.Export(path,mesh);var read=extension=="stl"?ModelExporter.ReadStl(path):ModelExporter.Read3Mf(path);Require(read.Validate().IsValid&&Math.Abs(read.Validate().Volume-report.Volume)<.3,"Langloch-Datei verliert Geometrie.");}
            }
        });
        Test("Langlöcher: kleine Längen, Randwerte und unregelmäßige Hilfskanten",()=>
        {
            foreach(double extension in new[]{.001,.1,1,8,40})foreach(bool horizontal in new[]{false,true})foreach(double diameter in new[]{2d,20d})
            {
                var s=MountingFixture(0,false);s.BackMountingHoles=[new(){Y=42.5,X=.123,Diameter=diameter,SlotExtension=extension,Horizontal=horizontal}];
                var mesh=MeshGenerator.Generate(gray,s,false);Require(mesh.Validate().IsValid,"Grenzfall ungültig: "+extension+" / "+diameter);
            }
            var random=new Random(1100);for(int i=0;i<30;i++){var s=SlotFixture();foreach(var hole in s.BackMountingHoles){hole.X+=random.NextDouble()-.5;hole.Y+=random.NextDouble()-.5;hole.SlotExtension=1+random.NextDouble()*7;hole.Diameter=2+random.NextDouble()*3;}Require(MeshGenerator.Generate(gray,s,i%2==0).Validate().IsValid,"Unregelmäßiger Langlochmix ungültig.");}
        });
        Test("Langlöcher: ungültige Werte, Rand, Überlappung und Abbruch",()=>
        {
            foreach(Action<ModelSettings> change in new Action<ModelSettings>[] {s=>s.BackMountingHoles[0].SlotExtension=-1,s=>s.BackMountingHoles[0].SlotExtension=41,s=>s.BackMountingHoles[0].SlotExtension=double.NaN,s=>s.BackMountingHoles[0].SlotExtension=double.PositiveInfinity,s=>s.BackMountingHoles[0].Y=78,s=>{s.BackMountingHoles[0].Horizontal=true;s.BackMountingHoles[0].X=-42;},s=>{s.BackMountingHoles[0].Y=42.5;s.BackMountingHoles[0].X=0;}}){var s=SlotFixture();change(s);Throws<InvalidDataException>(s.Validate);}
            using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>BackMounting.Generate(SlotFixture(),false,cts.Token));
        });
        Test("Langlöcher: Format 8 bleibt rund, Format 13 erhält Werte und Originalfoto",()=>
        {
            var s=MountingFixture(4,true);s.BackAngles=new(17,-9,23);var before=MeshGenerator.Generate(gray,s,false);byte[] image=ImageProcessor.Png(ImageProcessor.Demo());
            string old=Path.Combine(output,"legacy-v8.lithopro");if(File.Exists(old))File.Delete(old);
            using(var z=ZipFile.Open(old,ZipArchiveMode.Create)){using(var stream=z.CreateEntry("project.json").Open())JsonSerializer.Serialize(stream,new ProjectData{Version=8,Model=s});using var source=z.CreateEntry("source.bin").Open();source.Write(image);}
            var loaded=ProjectStore.Load(old);var after=MeshGenerator.Generate(gray,loaded.Data.Model,false);
            Require(loaded.Data.Version==15&&loaded.Data.Model.BackMountingHoles.All(h=>h.SlotExtension==0&&!h.Horizontal)&&loaded.Data.Model.BackAngles==s.BackAngles&&before.Vertices.SequenceEqual(after.Vertices)&&before.Triangles.SequenceEqual(after.Triangles),"Migration verändert Rundbohrungen oder Winkel.");
            var data=new ProjectData{Model=SlotFixture()};var copy=data.Model.Clone();copy.BackMountingHoles[0].SlotExtension=30;Require(data.Model.BackMountingHoles[0].SlotExtension==8,"Snapshot verändert Original-Langloch.");
            string path=Path.Combine(output,"slots-project.lithopro");ProjectStore.Save(path,data,image);var roundtrip=ProjectStore.Load(path);Require(JsonSerializer.Serialize(data)==JsonSerializer.Serialize(roundtrip.Data)&&roundtrip.Image.SequenceEqual(image),"Langlochprojekt verändert Parameter oder Foto.");
        });
        Test("Langlöcher: freie Druckwinkel, Montage und Drucksatz",()=>
        {
            var s=SlotFixture();var reference=MeshGenerator.Generate(gray,s,false).Validate();s.BackAngles=new(23,-17,8);s.BackTurnOnBed90=true;var mesh=MeshGenerator.Generate(gray,s,false);Require(mesh.Validate().IsValid&&Math.Abs(mesh.Validate().Volume-reference.Volume)<.3,"Winkel verändern Langlochvolumen.");
            s.Part=ModelPart.Assembly;var a=MeshGenerator.Generate(gray,s,true);s.BackAngles=default;s.BackTurnOnBed90=false;var b=MeshGenerator.Generate(gray,s,true);Require(a.IsAssemblyPreview&&a.Vertices.SequenceEqual(b.Vertices),"Montage dreht Langlöcher unabhängig.");
            s.BackAngles=new(23,-17,8);var set=LithoCreate.App.Services.ModelComputation.BuildPrintSet(ImageProcessor.Png(ImageProcessor.Demo()),s,new(),new(),CancellationToken.None);Require(set.Parts.Count==2&&s.Part==ModelPart.Assembly&&s.BackAngles==new PrintAngles(23,-17,8),"Drucksatz verändert Projekt.");
        });
    }
}
