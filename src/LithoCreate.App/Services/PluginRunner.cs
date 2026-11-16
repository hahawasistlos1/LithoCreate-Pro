using System.Diagnostics;
using System.IO;
using System.Text.Json;
using LithoCreate.Core;

namespace LithoCreate.App.Services;
public static class PluginRunner
{
    public static async Task<ImportedModel> Generate(string host,string plugin,GrayImage photo,ModelSettings settings,CancellationToken token,string? workRoot=null)
    {
        token.ThrowIfCancellationRequested();
        if(!File.Exists(host)||!File.Exists(plugin))throw new FileNotFoundException("PluginHost oder gewähltes Modul fehlt.");
        if(new FileInfo(plugin).Length is 0 or > 16*1024*1024)throw new InvalidDataException("Formmodule dürfen höchstens 16 MiB groß sein.");
        string root=Path.Combine(workRoot??Path.GetTempPath(),"LithoCreate-Plugin-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        string image=Path.Combine(root,"photo.gray"),options=Path.Combine(root,"model.json"),result=Path.Combine(root,"result.stl");
        try
        {
            if(photo.Width is < 1 or > 4096||photo.Height is < 1 or > 4096||photo.Pixels.Length!=(long)photo.Width*photo.Height)throw new InvalidDataException("Ungültiges Pluginbild.");
            using(var writer=new BinaryWriter(File.Create(image))){writer.Write(photo.Width);writer.Write(photo.Height);writer.Write(photo.Pixels);}
            File.WriteAllText(options,JsonSerializer.Serialize(settings,ProjectStore.Json));var start=new ProcessStartInfo(Path.GetFullPath(host)){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=root,RedirectStandardOutput=true,RedirectStandardError=true};foreach(string argument in new[]{Path.GetFullPath(plugin),image,options,result})start.ArgumentList.Add(argument);
            using var process=Process.Start(start)??throw new IOException("Modulprozess konnte nicht gestartet werden.");using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromSeconds(60));
            // Drain output without retaining arbitrary plugin logs in memory.
            var stdout=Drain(process.StandardOutput);var stderr=Drain(process.StandardError);
            try{await process.WaitForExitAsync(timeout.Token);}
            catch(OperationCanceledException){if(!process.HasExited)process.Kill(true);await process.WaitForExitAsync(CancellationToken.None);throw new OperationCanceledException("Modulberechnung abgebrochen oder Zeitlimit von 60 Sekunden erreicht.",token);}
            await Task.WhenAll(stdout,stderr);if(process.ExitCode!=0)throw new InvalidDataException("Formmodul fehlgeschlagen: "+(await stderr).Trim());
            return await Task.Run(()=>StlImport.Read(result,300000,token),token);
        }
        finally{foreach(string file in new[]{image,options,result,result+".tmp"})if(File.Exists(file))File.Delete(file);if(Directory.Exists(root)&&!Directory.EnumerateFileSystemEntries(root).Any())Directory.Delete(root);}
    }
    static async Task<string> Drain(StreamReader reader){char[] buffer=new char[4096];var text=new System.Text.StringBuilder();int count;while((count=await reader.ReadAsync(buffer))>0)if(text.Length<2048)text.Append(buffer,0,Math.Min(count,2048-text.Length));return text.ToString();}
}
