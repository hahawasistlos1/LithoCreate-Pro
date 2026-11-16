using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using LithoCreate.Core;

try
{
    if(args.Length!=4)throw new InvalidDataException("PluginHost: DLL, Graubild, Parameter und STL-Ziel erforderlich.");
    var settingsFile=new FileInfo(args[2]);if(settingsFile.Length>1024*1024)throw new InvalidDataException("Pluginparameter zu groß.");
    var settings=JsonSerializer.Deserialize<ModelSettings>(File.ReadAllText(args[2]),ProjectStore.Json)??throw new InvalidDataException("Pluginparameter fehlen.");settings.Validate();
    using var input=new BinaryReader(File.OpenRead(args[1]));int width=input.ReadInt32(),height=input.ReadInt32();if(width is < 1 or > 4096||height is < 1 or > 4096||input.BaseStream.Length!=8L+(long)width*height)throw new InvalidDataException("Ungültiges Pluginbild.");var image=new GrayImage(width,height,input.ReadBytes(width*height));
    var context=new PluginContext();var assembly=context.LoadFromAssemblyPath(Path.GetFullPath(args[0]));var types=assembly.GetTypes().Where(t=>!t.IsAbstract&&typeof(IFormPlugin).IsAssignableFrom(t)).ToArray();if(types.Length!=1)throw new InvalidDataException("Das Modul muss genau eine konkrete IFormPlugin-Klasse enthalten.");
    var plugin=(IFormPlugin?)Activator.CreateInstance(types[0])??throw new InvalidDataException("Modul konnte nicht erstellt werden.");if(string.IsNullOrWhiteSpace(plugin.Name)||plugin.Name.Length>120)throw new InvalidDataException("Ungültiger Modulname.");
    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(55));var mesh=plugin.Generate(image,settings,timeout.Token);if(mesh is null||mesh.IsAssemblyPreview||mesh.Triangles.Count>300000||mesh.Vertices.Count>900000)throw new InvalidDataException("Modell fehlt oder überschreitet das Pluginbudget.");
    ModelExporter.Export(args[3],mesh,timeout.Token);Console.WriteLine(plugin.Name);return 0;
}
catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}
sealed class PluginContext:AssemblyLoadContext
{
    protected override Assembly? Load(AssemblyName name)=>name.Name==typeof(IFormPlugin).Assembly.GetName().Name?typeof(IFormPlugin).Assembly:null;
}
