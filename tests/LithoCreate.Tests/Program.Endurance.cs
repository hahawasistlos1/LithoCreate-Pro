using System.Diagnostics;
using System.IO;
using System.Text.Json;
using LithoCreate.Core;

partial class Program
{
    static int Endurance(string directory,double minutes)
    {
        if(!double.IsFinite(minutes)||minutes is < 1 or > 120)throw new ArgumentOutOfRangeException(nameof(minutes));Directory.CreateDirectory(directory);
        var watch=Stopwatch.StartNew();var process=Process.GetCurrentProcess();var samples=new List<object>();int cycles=0,models=0;
        var image=new GrayImage(64,48,Enumerable.Range(0,64*48).Select(i=>(byte)(i%256)).ToArray());
        while(watch.Elapsed.TotalMinutes<minutes)
        {
            foreach(var shape in Enum.GetValues<Shape>())
            {
                var model=new ModelSettings{Shape=shape,Width=64,Height=48,Diameter=48,Radius=50,Resolution=.6,MemoryMb=128,FrameWidth=2};var mesh=MeshGenerator.Generate(image,model,false);var report=mesh.Validate();Require(report.IsValid,"Dauertest: "+shape);
                var index=new CrossSectionIndex(mesh);var section=index.Slice(index.Bottom+index.Height*.51);Require(section.Paths.Count>0,"Dauertest: Schnitt fehlt.");ModelExporter.Export(Path.Combine(directory,shape+".3mf"),mesh);models++;
            }
            cycles++;process.Refresh();samples.Add(new{Seconds=watch.Elapsed.TotalSeconds,Cycles=cycles,Models=models,WorkingSetMiB=process.WorkingSet64/1048576d,PrivateMiB=process.PrivateMemorySize64/1048576d,ManagedMiB=GC.GetTotalMemory(false)/1048576d});
            File.WriteAllText(Path.Combine(directory,"progress.json"),JsonSerializer.Serialize(new{Status="running",Minutes=minutes,Cycles=cycles,Models=models,ElapsedSeconds=watch.Elapsed.TotalSeconds,PeakMiB=process.PeakWorkingSet64/1048576d,Samples=samples},ProjectStore.Json));Console.WriteLine($"PASS Zyklus {cycles}: {models} Modelle, {watch.Elapsed.TotalMinutes:N1} min, Peak {process.PeakWorkingSet64/1048576:N0} MiB");
        }
        process.Refresh();File.WriteAllText(Path.Combine(directory,"result.json"),JsonSerializer.Serialize(new{Status="PASS",Cycles=cycles,Models=models,ElapsedSeconds=watch.Elapsed.TotalSeconds,PeakMiB=process.PeakWorkingSet64/1048576d,CpuSeconds=process.TotalProcessorTime.TotalSeconds,Samples=samples},ProjectStore.Json));return 0;
    }
}
