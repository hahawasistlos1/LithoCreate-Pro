using System.IO;

namespace LithoCreate.App.Services;

/// <summary>One process per data directory; Windows releases the exclusive handle on a crash.</summary>
public sealed class InstanceLock : IDisposable
{
    readonly FileStream handle;
    InstanceLock(FileStream handle)=>this.handle=handle;
    public static InstanceLock? TryAcquire(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        try{return new(new FileStream(Path.Combine(dataDirectory,"studio.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None));}
        catch(IOException ex) when((ex.HResult&0xFFFF) is 32 or 33){return null;}
    }
    public void Dispose()=>handle.Dispose();
}
