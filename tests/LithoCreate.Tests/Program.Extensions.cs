using System.IO;
using System.IO.Compression;
using System.Globalization;
using System.Text;
using System.Numerics;
using LithoCreate.Core;
using LithoCreate.App.Services;

partial class Program
{
    static void TestExtensions()
    {
        TestLightInserts();
        Test("STL-Import: binär, ASCII, Solid-Header, Maße und sichere Gesamtwicklung",()=>
        {
            string file=Path.Combine(output,"Import-Box.stl");var source=MeshGenerator.Generate(new(1,1,[255]),new(){Width=40,Height=30,FrameWidth=0,MinThickness=3,MaxThickness=3,Resolution=2,PrintOrientation=PrintOrientation.FaceUp},false);ModelExporter.Export(file,source);var binary=StlImport.Read(file);Require(binary.Report.IsValid&&Vector3.Distance(binary.Report.Size,new(40,3,30))<.001&&!binary.OrientationCorrected,"Binärer Import verändert Achsen oder Maße.");
            byte[] data=File.ReadAllBytes(file);Encoding.ASCII.GetBytes("solid binary despite header").CopyTo(data,0);File.WriteAllBytes(file,data);Require(StlImport.Read(file).Format=="Binär-STL","Solid-Header fälschlich als ASCII interpretiert.");
            var printer=ModelExporter.ReadStl(file);var text=new StringBuilder("solid example\n");foreach(var t in printer.Triangles){text.Append("facet normal 0 0 1\nouter loop\n");foreach(int i in new[]{t.A,t.B,t.C}){var p=printer.Vertices[i];text.AppendFormat(CultureInfo.InvariantCulture,"vertex {0:R} {1:R} {2:R}\n",p.X,p.Y,p.Z);}text.Append("endloop\nendfacet\n");}text.Append("endsolid example\n");string ascii=Path.Combine(output,"Import-Ascii.stl");File.WriteAllText(ascii,text.ToString());var read=StlImport.Read(ascii);Require(read.Report.IsValid&&read.Format=="ASCII-STL"&&Math.Abs(read.Report.Volume-3600)<.1,"ASCII-Import falsch.");ModelExporter.Export(Path.Combine(output,"Import-Roundtrip.3mf"),read.Mesh);
            for(int i=0;i<printer.Triangles.Count;i++){var t=printer.Triangles[i];printer.Triangles[i]=new(t.A,t.C,t.B);} // Write the reversed input directly, without the exporter's validation.
            for(int i=0;i<printer.Triangles.Count;i++){int start=84+50*i+12;var a=data.AsSpan(start,12).ToArray();data.AsSpan(start+24,12).CopyTo(data.AsSpan(start,12));a.CopyTo(data,start+24);}File.WriteAllBytes(file,data);var repaired=StlImport.Read(file);Require(repaired.OrientationCorrected&&repaired.Report.IsValid,"Sicher umgekehrte Gesamtwicklung nicht repariert.");
        });
        Test("STL-Import: offene Netze, Größenlimit, NaN und beschädigte Dateien",()=>
        {
            string path=Path.Combine(output,"import-invalid.stl");File.WriteAllText(path,"solid open\nfacet normal 0 0 1\nouter loop\nvertex 0 0 0\nvertex 1 0 0\nvertex 0 1 0\nendloop\nendfacet\nendsolid open\n");Require(!StlImport.Read(path).Report.IsValid,"Offene STL als gültig markiert.");File.WriteAllText(path,File.ReadAllText(path).Replace("vertex 1","vertex NaN"));Throws<InvalidDataException>(()=>StlImport.Read(path));File.WriteAllBytes(path,new byte[14]);Throws<InvalidDataException>(()=>StlImport.Read(path));Throws<InvalidDataException>(()=>StlImport.Read(Path.Combine(output,"Import-Ascii.stl"),1));using var cancel=new CancellationTokenSource();cancel.Cancel();Throws<OperationCanceledException>(()=>StlImport.Read(Path.Combine(output,"Import-Ascii.stl"),token:cancel.Token));
        });
        foreach(var preset in WorkshopParts.Presets)Test("Halterung: "+preset.Name,()=>
        {
            var mesh=WorkshopParts.Generate(preset.Kind,preset.A,preset.B,preset.Thickness);var r=mesh.Validate();Require(r.IsValid,"Halterung nicht geschlossen: "+r);string name="Workshop-"+Array.IndexOf(WorkshopParts.Presets,preset);ModelExporter.Export(Path.Combine(output,name+".stl"),mesh);ModelExporter.Export(Path.Combine(output,name+".3mf"),mesh);
            if(preset.Kind==WorkshopKind.SocketRing){var section=new CrossSectionIndex(mesh).Slice(preset.Thickness/2);double expected=128d/2*Math.Sin(Math.Tau/128)*(preset.B*preset.B/4-Math.Pow(preset.A/(2*Math.Cos(Math.PI/128)),2))*preset.Thickness;Require(section.IsRegular&&section.ClosedContours==2&&Math.Abs(r.Volume-expected)<.2,"Ringloch oder Volumen falsch.");}
        });
        Test("STL-Import: gemischte Teilkörperwicklung wird nicht als sichere Reparatur angenommen",()=>
        {
            byte[] source=File.ReadAllBytes(Path.Combine(output,"Import-Box.stl"));int count=BitConverter.ToInt32(source,80);byte[] data=new byte[84+count*100];Array.Copy(source,data,84);BitConverter.GetBytes(count*2).CopyTo(data,80);
            for(int i=0;i<count;i++){Array.Copy(source,84+i*50,data,84+i*50,50);Array.Copy(source,84+i*50,data,84+(i+count)*50,50);for(int j=0;j<3;j++)for(int axis=0;axis<3;axis++){int offset=84+i*50+12+j*12+axis*4;float value=BitConverter.ToSingle(source,offset)*.5f+(axis==0?60:0);BitConverter.GetBytes(value).CopyTo(data,84+(i+count)*50+12+j*12+axis*4);}int start=84+(i+count)*50+12;var a=data.AsSpan(start,12).ToArray();data.AsSpan(start+24,12).CopyTo(data.AsSpan(start,12));a.CopyTo(data,start+24);}
            string file=Path.Combine(output,"import-mixed.stl");File.WriteAllBytes(file,data);Throws<InvalidDataException>(()=>StlImport.Read(file));
        });
        Test("Halterung: fehlerhafte Maße und Wandstärken",()=>
        {
            foreach(double bad in new[]{double.NaN,double.PositiveInfinity,-1,300})Throws<InvalidDataException>(()=>WorkshopParts.Generate(WorkshopKind.LedChannel,bad,50,2));Throws<InvalidDataException>(()=>WorkshopParts.Generate(WorkshopKind.SocketRing,40,42,3));Throws<InvalidDataException>(()=>WorkshopParts.Generate(WorkshopKind.Support,3,3,2));
        });
        Test("Montage- und Fotoplatte: freie Rundbohrungen und konstante Wandstärken",()=>
        {
            MountingHole[] holes=[new(){X=-15,Y=20,Diameter=4},new(){X=15,Y=20,Diameter=4}];var plate=BackMounting.GeneratePlate(60,40,3,holes);var report=plate.Validate();Require(report.IsValid&&Math.Abs(report.Volume-(60*40-2*Math.PI*4)*3)<1,"Montageplattenvolumen oder Bohrungen falsch.");PrintPlacement.Apply(plate,PrintOrientation.FaceUp,false);ModelExporter.Export(Path.Combine(output,"Plate-Holes.stl"),plate);
            foreach(byte gray in new byte[]{0,255}){var settings=new ModelSettings{Width=60,Height=40,MinThickness=.8,MaxThickness=3,Resolution=1};var photo=BackMounting.GeneratePlate(60,40,3,holes,photo:new(1,1,[gray]),photoSettings:settings);var check=photo.Validate();double thickness=gray==0?3:.8;Require(check.IsValid&&Math.Abs(check.Size.Z-thickness)<.001&&Math.Abs(check.Volume-(60*40-2*Math.PI*4)*thickness)<1,"Fotoplatte verliert Dicke oder freie Bohrungen.");PrintPlacement.Apply(photo,PrintOrientation.FaceUp,false);ModelExporter.Export(Path.Combine(output,$"Photo-Holes-{gray}.3mf"),photo);}
            Throws<InvalidDataException>(()=>BackMounting.GeneratePlate(60,40,3,[new(){X=29,Y=20}]));Throws<InvalidDataException>(()=>BackMounting.GeneratePlate(60,40,3,[new(){X=0,Y=20},new(){X=1,Y=20}]));
        });
        Test("P1S-PETG: Profil ohne Überschreiben und geometrisch geprüfte Probedrucke",()=>
        {
            var profile=CalibrationKit.P1SPetg();profile.Validate();Require(profile.BedX==256&&profile.BedY==256&&profile.BuildZ==256&&profile.Material.StartsWith("PETG"),"P1S-Profil falsch.");string path=Path.Combine(output,"P1S-PETG-Probedrucke.zip");CalibrationKit.Export(path);using var zip=ZipFile.OpenRead(path);Require(zip.Entries.Count(e=>e.FullName.EndsWith(".stl"))==16&&zip.GetEntry("Messprotokoll.csv")!=null,"Probedruckpaket unvollständig.");
            WithPresetVm("p1s",vm=>{Require(vm.AddP1SPrinter()&&vm.Profiles.Count==3&&vm.Printer.BedX==256,"P1S nicht angelegt.");vm.Printer.BedX=250;vm.SaveCurrentPrinter();vm.Printer.BedX=245;Require(vm.AddP1SPrinter()&&vm.Profiles.Count==3&&vm.Printer.BedX==250,"P1S-Vorgabe überschreibt vorhandenes Profil.");});
        });
        Test("Formmodul: separater Host, aktuelles Foto, begrenzte Ausgabe und Abbruch",()=>
        {
            string host=Path.Combine(AppContext.BaseDirectory,"LithoCreate.PluginHost.exe"),plugin=Path.Combine(AppContext.BaseDirectory,"LithoCreate.WaveForm.dll");
            if(!File.Exists(plugin))plugin=Path.GetFullPath("extensions/WaveForm/bin/Release/net10.0/LithoCreate.WaveForm.dll");if(!File.Exists(host))host=Path.GetFullPath("tools/LithoCreate.PluginHost/bin/Release/net10.0/LithoCreate.PluginHost.exe");
            var settings=new ModelSettings{Width=48,Height=36,Resolution=1,FrameWidth=2};var task=PluginRunner.Generate(host,plugin,new(8,8,Enumerable.Range(0,64).Select(i=>(byte)(i*4)).ToArray()),settings,CancellationToken.None,Path.Combine(output,"plugin-work"));PumpUntil(()=>task.IsCompleted);var model=task.GetAwaiter().GetResult();Require(model.Report.IsValid&&settings.Shape==Shape.Flat,"Modul verändert Parameter oder liefert ungültiges Netz.");ModelExporter.Export(Path.Combine(output,"Plugin-Arc.3mf"),model.Mesh);
            string drilled=Path.GetFullPath("extensions/DrilledPhoto/bin/Release/net10.0/LithoCreate.DrilledPhoto.dll");var drillTask=PluginRunner.Generate(host,drilled,new(1,1,[255]),settings,CancellationToken.None,Path.Combine(output,"plugin-work"));PumpUntil(()=>drillTask.IsCompleted);var drilledModel=drillTask.GetAwaiter().GetResult();Require(drilledModel.Report.IsValid,"Bohrungsmodul nicht geschlossen.");ModelExporter.Export(Path.Combine(output,"Plugin-Drilled.3mf"),drilledModel.Mesh);
            using var cancel=new CancellationTokenSource();cancel.Cancel();var canceled=PluginRunner.Generate(host,plugin,new(1,1,[255]),settings,cancel.Token,Path.Combine(output,"plugin-work"));PumpUntil(()=>canceled.IsCompleted);Throws<OperationCanceledException>(()=>canceled.GetAwaiter().GetResult());
            var detailed=new ModelSettings{Width=120,Height=88,Resolution=.3};var detailedTask=PluginRunner.Generate(host,plugin,new(1,1,[255]),detailed,CancellationToken.None,Path.Combine(output,"plugin-work"));PumpUntil(()=>detailedTask.IsCompleted);var detailedModel=detailedTask.GetAwaiter().GetResult();Require(detailedModel.Report.IsValid&&detailedModel.Mesh.Triangles.Count<=300000&&detailed.Resolution==.3,"Beispielmodul überschreitet bei üblichen Projektmaßen das Hostlimit oder verändert das Projektraster.");
        });
    }
}
