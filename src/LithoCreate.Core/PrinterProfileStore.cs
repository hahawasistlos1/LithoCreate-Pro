using System.Text;
using System.Text.Json;

namespace LithoCreate.Core;

/// <summary>Compatible with existing printers.json arrays; projects keep their own printer copy.</summary>
public static class PrinterProfileStore
{
    static readonly JsonSerializerOptions readOptions=new(ProjectStore.Json){UnmappedMemberHandling=System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow};
    public static PrinterProfile[] Defaults()=>[new(),new(){Name="Resin · 143 × 89",Process="Resin",BedX=143,BedY=89,BuildZ=165,Layer=.05,Material="Resin, Weiß"}];
    public static bool SameName(string a,string b)=>StringComparer.OrdinalIgnoreCase.Equals(a.Trim(),b.Trim());
    public static void Validate(IReadOnlyList<PrinterProfile> profiles)
    {
        if(profiles.Count>100)throw new InvalidDataException("Es können höchstens 100 Druckerprofile gespeichert werden.");
        var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var profile in profiles)
        {
            if(profile is null)throw new InvalidDataException("Die Profildatei enthält einen fehlenden Eintrag.");
            profile.Validate();
            if(profile.Name.Any(char.IsControl))throw new InvalidDataException("Profilnamen dürfen keine Steuerzeichen enthalten.");
            if(!names.Add(profile.Name.Trim()))throw new InvalidDataException("Ein Druckerprofil mit diesem Namen ist bereits vorhanden. Bitte einen anderen Namen wählen.");
        }
    }
    public static PrinterProfile[] Load(string path)
    {
        if(!File.Exists(path))return Defaults();
        using var stream=File.OpenRead(path);
        if(stream.Length is 0 or > 512*1024)throw new InvalidDataException("Die Druckerprofildatei ist leer oder größer als 512 KB.");
        using var reader=new StreamReader(stream,Encoding.UTF8,true);
        var profiles=JsonSerializer.Deserialize<PrinterProfile[]>(reader.ReadToEnd(),readOptions)??throw new InvalidDataException("Die Druckerprofildatei enthält keine gültige Liste.");
        Validate(profiles);return profiles;
    }
    public static void Save(string path,IEnumerable<PrinterProfile> profiles)
    {
        var items=profiles.ToArray();Validate(items);items=items.Select(p=>p.Clone()).ToArray();
        byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(items,ProjectStore.Json);
        if(bytes.Length>512*1024)throw new InvalidDataException("Die Druckerprofildatei ist größer als 512 KB.");
        string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllBytes(temp,bytes);File.Move(temp,path,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
}
