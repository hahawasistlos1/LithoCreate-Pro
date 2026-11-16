using System.Text.Json;

namespace LithoCreate.Core;

public sealed class ModelPreset
{
    public override string ToString()=>Name;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public ModelSettings Model { get; set; } = new();
    [System.Text.Json.Serialization.JsonIgnore]
    public string Description
    {
        get
        {
            string name = Templates.All.First(t=>t.Shape==Model.Shape).Name;
            string dimensions = Model.Shape switch
            {
                _ when Model.IsShade => $"Ø {Model.ShadeTopDiameter:0.##} / {Model.ShadeBottomDiameter:0.##} × {Model.ShadeHeight:0.##} mm",
                Shape.Sphere or Shape.Moon or Shape.Disc or Shape.Pendant => $"Ø {Model.Diameter:0.##} mm",
                Shape.Cylinder or Shape.Polygon => $"Ø {Model.Diameter:0.##} × {Model.Height:0.##} mm",
                _ => $"{Model.Width:0.##} × {Model.Height:0.##} mm"
            };
            return name+" · "+dimensions;
        }
    }
    public ModelPreset Clone()=>new(){Id=Id,Name=Name,Model=Model.Clone()};
}

public sealed class ModelPresetCatalog
{
    public int Version { get; set; } = 1;
    public int ModelVersion { get; set; } = 13;
    public List<ModelPreset> Items { get; set; } = [];
    public void Validate()
    {
        if(Version!=1||ModelVersion!=13)throw new InvalidDataException("Diese Vorlagenversion wird nicht unterstützt. Die Datei bleibt erhalten.");
        if(Items is null||Items.Count>100)throw new InvalidDataException("Es können höchstens 100 eigene Vorlagen gespeichert werden.");
        var ids=new HashSet<Guid>();var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var item in Items)
        {
            if(item is null||item.Id==Guid.Empty||!ids.Add(item.Id)||item.Model is null)throw new InvalidDataException("Die Vorlagensammlung enthält fehlende oder doppelte Kennungen.");
            ValidateName(item.Name);
            if(!names.Add(item.Name))throw new InvalidDataException("Eine Vorlage mit diesem Namen ist bereits vorhanden. Bitte einen anderen Namen wählen.");
            item.Model.Validate();
        }
    }
    public static void ValidateName(string name)
    {
        if(string.IsNullOrWhiteSpace(name)||name.Length>80||name!=name.Trim()||name.Any(char.IsControl))throw new InvalidDataException("Vorlagenname: 1–80 Zeichen ohne Steuerzeichen eingeben.");
    }
}

public static class ModelPresetStore
{
    public static ModelPresetCatalog Load(string path)
    {
        if(!File.Exists(path))return new();
        using var stream=File.OpenRead(path);
        if(stream.Length is 0 or > 2*1024*1024)throw new InvalidDataException("Die Vorlagensammlung ist leer oder größer als 2 MB.");
        var catalog=JsonSerializer.Deserialize<ModelPresetCatalog>(stream,ProjectStore.Json)??throw new InvalidDataException("Die Vorlagensammlung ist ungültig.");
        if(catalog.Version==1&&catalog.ModelVersion==12&&catalog.Items!=null){foreach(var item in catalog.Items)if(item?.Model!=null)item.Model.HangingTabsEnabled=false;catalog.ModelVersion=13;}
        catalog.Validate();return catalog;
    }
    public static void Save(string path,IEnumerable<ModelPreset> items)
    {
        var catalog=new ModelPresetCatalog{Items=items.ToList()};catalog.Validate();
        catalog.Items=catalog.Items.Select(p=>p.Clone()).ToList();
        byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(catalog,ProjectStore.Json);
        if(bytes.Length>2*1024*1024)throw new InvalidDataException("Die Vorlagensammlung ist größer als 2 MB.");
        string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllBytes(temp,bytes);File.Move(temp,path,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
}
