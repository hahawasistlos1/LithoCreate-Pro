using System.Text.Json;
using System.IO.Compression;

namespace LithoCreate.Core;

public enum Shape { Flat, Curved, Cylinder, Polygon, Sphere, Moon, Heart, Star, Oval, Lightbox, Frame, Nightlight, Disc, Pendant, Drop, CeilingShade, FloorShade, BellShade, FacetedShade }
public record ShapeInfo(Shape Shape, string Name, string Icon, string Description, string Category)
{
    public override string ToString()=>Name;
}
public static class Templates
{
    public static readonly ShapeInfo[] All = [
        new(Shape.Flat,"Flaches Bild","▧","Klassisches Relief mit ebener Rückseite.","Bilder"),
        new(Shape.Curved,"Gebogenes Bild","◩","Zylindrisches Segment mit radialer Wandstärke.","Bilder"),
        new(Shape.Cylinder,"Zylinderlampe","▥","Rundum-Lithophanie mit optionalem Boden und Bodenöffnung.","Lampen"),
        new(Shape.Polygon,"Mehrseitige Lampe","⬡","Geschlossene Seitenfläche mit wählbarer Seitenzahl.","Lampen"),
        new(Shape.Sphere,"Kugel","◉","Hohle Kugel mit radial berechnetem Fotorelief.","Kugeln"),
        new(Shape.Moon,"Mond","☾","Kugelform mit dezentem, prozeduralem Kraterrelief.","Kugeln"),
        new(Shape.Heart,"Herz","♥","Herzförmige Fotoplatte mit geschlossener Rückseite.","Dekoration"),
        new(Shape.Star,"Stern","★","Fünfzackige Fotoplatte.","Dekoration"),
        new(Shape.Oval,"Oval","⬭","Ovales, hinterleuchtbares Relief.","Bilder"),
        new(Shape.Lightbox,"Leuchtkasten","▣","Fotoplatte mit integriertem tiefem Rand, hinten offen.","Rahmen"),
        new(Shape.Frame,"Bilderrahmen","▣","Fotorelief mit verstärktem umlaufendem Rahmen.","Rahmen"),
        new(Shape.Nightlight,"Nachtlicht","◒","Gebogenes, kompaktes Fotopanel.","Lampen"),
        new(Shape.Disc,"Fotoplakette","●","Runde Fotoplatte.","Dekoration"),
        new(Shape.Pendant,"Anhänger","♧","Runde Fotoplatte mit zentraler Durchgangsöffnung.","Dekoration"),
        new(Shape.Drop,"Tropfen","♦","Tropfenförmiges Fotorelief.","Dekoration"),
        new(Shape.CeilingShade,"Deckenschirm","◓","Konischer Schirm mit optionalem Montagering oben.","Lampenschirme"),
        new(Shape.FloorShade,"Stehleuchtenschirm","▥","Gerader Schirm mit optionalem Montagering unten.","Lampenschirme"),
        new(Shape.BellShade,"Glockenschirm","◠","Geschwungener Mantel mit breitem Schirmrand.","Lampenschirme"),
        new(Shape.FacetedShade,"Facettenschirm","⬡","Kantiger Schirm mit 3 bis 16 Seiten.","Lampenschirme")];
}
public sealed class ImageEdits
{
    public double Brightness { get; set; }
    public double Contrast { get; set; } = 1;
    public double Gamma { get; set; } = 1;
    public double Sharpness { get; set; }
    public double Denoise { get; set; }
    public double BlackPoint { get; set; }
    public double WhitePoint { get; set; } = 1;
    public bool Invert { get; set; }
    public bool Grayscale { get; set; } = true;
    public bool Mirror { get; set; }
    public int Rotation { get; set; }
    public double StraightenAngle { get; set; }
    public bool AutoOrient { get; set; } = true;
    public double CropX { get; set; }
    public double CropY { get; set; }
    public double CropWidth { get; set; } = 1;
    public double CropHeight { get; set; } = 1;
    public double CropAspectWidth { get; set; } = 4;
    public double CropAspectHeight { get; set; } = 5;
    public bool CropAspectLock { get; set; }
    public ImageEdits Clone() => (ImageEdits)MemberwiseClone();
    public void Validate()
    {
        CropSelection.ValidateAspect(CropAspectWidth,CropAspectHeight);
        double[] values=[Brightness,Contrast,Gamma,CropX,CropY,CropWidth,CropHeight,BlackPoint,WhitePoint,Sharpness,Denoise,StraightenAngle];
        if(values.Any(v=>!double.IsFinite(v)) || Gamma is < .1 or > 5 || WhitePoint<=BlackPoint || BlackPoint<0 || WhitePoint>1 || Contrast is < 0 or > 3 || Brightness is < -1 or > 1 || Sharpness is < 0 or > 2 || Denoise is < 0 or > 1 || Rotation%90!=0)
            throw new InvalidDataException("Ungültige Bildkorrektur oder Tonwerte.");
        if(StraightenAngle is < -45 or > 45)throw new InvalidDataException("Feinrotation: −45° bis +45° eingeben.");
        if(CropX<0 || CropY<0 || CropX>=1 || CropY>=1 || CropWidth<=0 || CropHeight<=0 || CropX+CropWidth>1.00001 || CropY+CropHeight>1.00001)
            throw new InvalidDataException("Der Bildausschnitt muss vollständig im Bild liegen.");
    }
}
public sealed class ModelSettings
{
    public Shape Shape { get; set; }
    public double Width { get; set; } = 150;
    public double Height { get; set; } = 100;
    public bool LockDimensions { get; set; }
    public bool HangingTabsEnabled { get; set; }
    public double TabWidth { get; set; } = 12;
    public double TabHeight { get; set; } = 12;
    public double TabHoleDiameter { get; set; } = 4;
    public double TabInset { get; set; } = 8;
    public double Diameter { get; set; } = 100;
    public double MinThickness { get; set; } = .8;
    public double MaxThickness { get; set; } = 3;
    public double Resolution { get; set; } = .3;
    public double Radius { get; set; } = 100;
    public double FrameWidth { get; set; } = 3;
    public double BoxDepth { get; set; } = 20;
    public double HoleDiameter { get; set; } = 5;
    public int Sides { get; set; } = 6;
    public bool Negative { get; set; }
    public PrintAngles Angles { get; set; }
    public PrintAngles SocketAngles { get; set; }
    public PrintAngles BackAngles { get; set; }
    public PrintOrientation PrintOrientation { get; set; }
    public bool TurnOnBed90 { get; set; }
    public int MemoryMb { get; set; } = 256;
    public double LampBaseThickness { get; set; }
    public double LampBaseHoleDiameter { get; set; }
    public double ShadeTopDiameter { get; set; } = 100;
    public double ShadeBottomDiameter { get; set; } = 180;
    public double ShadeHeight { get; set; } = 135;
    public ShadeMount ShadeMount { get; set; }
    public double ShadeMountDiameter { get; set; } = 42;
    public double ShadeMountThickness { get; set; } = 3;
    public ShadeMountStyle ShadeMountStyle { get; set; }
    public int ShadeSides { get; set; } = 6;
    public bool StandEnabled { get; set; }
    public double StandHeight { get; set; } = 5;
    public double StandDepth { get; set; } = 25;
    public double SphereOpeningDiameter { get; set; }
    public double SphereCollarHeight { get; set; } = 3;
    public double SphereVentDiameter { get; set; }
    public bool SocketEnabled { get; set; }
    public ModelPart Part { get; set; }
    public double SocketFootDiameter { get; set; } = 60;
    public double SocketPlateHeight { get; set; } = 5;
    public double SocketInsertionHeight { get; set; } = 3;
    public double SocketClearance { get; set; } = .25;
    public double SocketHoleDiameter { get; set; } = 8;
    public PrintOrientation SocketPrintOrientation { get; set; }
    public bool SocketTurnOnBed90 { get; set; }
    public List<MountingHole> BackMountingHoles { get; set; } = [];
    public bool BackEnabled { get; set; }
    public double BackThickness { get; set; } = 2;
    public double BackInsertionDepth { get; set; } = 3;
    public double BackRimWidth { get; set; } = 1.6;
    public double BackClearance { get; set; } = .25;
    public double BackHoleDiameter { get; set; } = 8;
    public PrintOrientation BackPrintOrientation { get; set; } = PrintOrientation.FaceUp;
    public bool BackTurnOnBed90 { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] public bool SupportsBackPanel => Shape==Shape.Lightbox;
    [System.Text.Json.Serialization.JsonIgnore] public bool SupportsHangingTabs => Shape is Shape.Flat or Shape.Frame;
    [System.Text.Json.Serialization.JsonIgnore] public bool SupportsPhotoSizing => Shape is Shape.Flat or Shape.Frame or Shape.Curved or Shape.Nightlight or Shape.Lightbox;
    [System.Text.Json.Serialization.JsonIgnore] public bool SupportsStand => Shape is Shape.Flat or Shape.Frame or Shape.Curved or Shape.Nightlight;
    [System.Text.Json.Serialization.JsonIgnore] public bool SupportsLampBase => Shape is Shape.Cylinder or Shape.Polygon;
    [System.Text.Json.Serialization.JsonIgnore] public bool IsShade => LampShade.Supports(Shape);
    [System.Text.Json.Serialization.JsonIgnore] public bool IsFacetedShade => Shape == Shape.FacetedShade;
    [System.Text.Json.Serialization.JsonIgnore] public bool SupportsSphereOpenings => Shape is Shape.Sphere or Shape.Moon;
    [System.Text.Json.Serialization.JsonIgnore] public ModelPart EffectivePart => SupportsSphereOpenings&&SocketEnabled&&Part is ModelPart.Socket or ModelPart.Assembly?Part:SupportsBackPanel&&BackEnabled&&Part is ModelPart.BackPanel or ModelPart.Assembly?Part:ModelPart.Lithophane;
    [System.Text.Json.Serialization.JsonIgnore] public PrintOrientation EffectiveOrientation=>EffectivePart==ModelPart.Socket?SocketPrintOrientation:EffectivePart==ModelPart.BackPanel?BackPrintOrientation:PrintOrientation;
    [System.Text.Json.Serialization.JsonIgnore] public bool EffectiveTurnOnBed90=>EffectivePart==ModelPart.Socket?SocketTurnOnBed90:EffectivePart==ModelPart.BackPanel?BackTurnOnBed90:TurnOnBed90;
    [System.Text.Json.Serialization.JsonIgnore] public PrintAngles EffectiveAngles=>EffectivePart==ModelPart.Socket?SocketAngles:EffectivePart==ModelPart.BackPanel?BackAngles:Angles;
    public ModelSettings Clone(){var copy=(ModelSettings)MemberwiseClone();copy.BackMountingHoles=BackMountingHoles?.Select(h=>h?.Clone()!).ToList()!;return copy;}
    public void Validate()
    {
        double[] values = [Width,Height,Diameter,MinThickness,MaxThickness,Resolution,Radius,FrameWidth,BoxDepth,HoleDiameter,LampBaseThickness,LampBaseHoleDiameter,StandHeight,StandDepth,SphereOpeningDiameter,SphereCollarHeight,SphereVentDiameter,SocketFootDiameter,SocketPlateHeight,SocketInsertionHeight,SocketClearance,SocketHoleDiameter,BackThickness,BackInsertionDepth,BackClearance,BackHoleDiameter,BackRimWidth];
        Angles.Validate();SocketAngles.Validate();BackAngles.Validate();
        HangingTabs.Validate(this);
        if (values.Any(v=>!double.IsFinite(v))) throw new InvalidDataException("Alle Maße müssen endliche Zahlen sein.");
        if (!Enum.IsDefined(Shape)) throw new InvalidDataException("Die gespeicherte Form wird nicht unterstützt.");
        if (!Enum.IsDefined(PrintOrientation)) throw new InvalidDataException("Die gespeicherte Druckausrichtung wird nicht unterstützt.");
        if (!Enum.IsDefined(SocketPrintOrientation))throw new InvalidDataException("Die gespeicherte Sockelausrichtung wird nicht unterstützt.");
        if (!Enum.IsDefined(BackPrintOrientation))throw new InvalidDataException("Die gespeicherte Rückwandausrichtung wird nicht unterstützt.");
        if(SupportsBackPanel&&BackEnabled)LightboxBack.ValidateSettings(this);
        BackMounting.Validate(this);
        if (!Enum.IsDefined(Part))throw new InvalidDataException("Das gewählte Modellteil wird nicht unterstützt.");
        if (Width is < 5 or > 1000 || Height is < 5 or > 1000 || Diameter is < 10 or > 1000) throw new InvalidDataException("Breite/Höhe: 5–1000 mm; Durchmesser: 10–1000 mm.");
        if (MinThickness < .2 || MaxThickness < MinThickness || MaxThickness > 20) throw new InvalidDataException("Wandstärken: mindestens 0,2 mm; Maximum muss mindestens dem Minimum entsprechen (bis 20 mm).");
        if (Resolution is < .05 or > 3 || Radius < 5 || FrameWidth < 0 || FrameWidth >= Math.Min(Width,Height)/2 || BoxDepth < MaxThickness || BoxDepth > 100 || Sides is < 3 or > 16 || MemoryMb is < 64 or > 2048) throw new InvalidDataException("Auflösung, Radius, Rahmen, Tiefe, Seitenzahl oder Speichergrenze liegt außerhalb des gültigen Bereichs.");
        if(IsShade) LampShade.Validate(this);
        if (Shape is Shape.Curved or Shape.Nightlight && Width/Radius > Math.PI*1.9) throw new InvalidDataException("Der Bogen ist zu groß. Radius erhöhen oder Breite reduzieren.");
        if (Shape is Shape.Sphere or Shape.Moon or Shape.Cylinder or Shape.Polygon && Diameter/2 <= MaxThickness+1) throw new InvalidDataException("Durchmesser muss größer als die doppelte Wandstärke sein.");
        if (Shape == Shape.Pendant && (HoleDiameter < 1 || HoleDiameter >= Diameter*.5)) throw new InvalidDataException("Bohrung: mindestens 1 mm und kleiner als der halbe Durchmesser.");
        if (SupportsLampBase)
        {
            double innerRadius = Diameter/2 * (Shape == Shape.Polygon ? Math.Cos(Math.PI/Sides) : 1) - MaxThickness;
            if (innerRadius <= 1) throw new InvalidDataException("Für diese Seitenzahl ist der Durchmesser im Verhältnis zur Wandstärke zu klein.");
            if (LampBaseThickness < 0 || LampBaseThickness > Height/2 || LampBaseThickness is > 0 and < .4)
                throw new InvalidDataException("Lampenboden: 0 (offen) oder mindestens 0,4 mm, höchstens die halbe Lampenhöhe.");
            if (LampBaseThickness > 0 && (LampBaseHoleDiameter < 0 || LampBaseHoleDiameter is > 0 and < 2 || LampBaseHoleDiameter > 2*innerRadius-2))
                throw new InvalidDataException("Bodenöffnung: 0 oder mindestens 2 mm; zur Innenwand muss mindestens 1 mm Boden bleiben.");
        }
        if (SupportsStand && StandEnabled)
        {
            if (StandHeight is < 1 or > 40 || StandDepth < 2*(MaxThickness+1)+2 || StandDepth > 100)
                throw new InvalidDataException("Standfuß: Höhe 1–40 mm; Tiefe mindestens doppelte Rahmenstärke plus 2 mm, höchstens 100 mm.");
            if (Shape is Shape.Curved or Shape.Nightlight && Radius <= StandDepth/2+1)
                throw new InvalidDataException("Für den Standfuß muss der Krümmungsradius größer als die halbe Standtiefe plus 1 mm sein.");
        }
        if (SupportsSphereOpenings)
        {
            double inner = Diameter/2-MaxThickness;
            if (SphereOpeningDiameter < 0 || SphereOpeningDiameter is > 0 and < 5 || SphereOpeningDiameter > inner*1.7)
                throw new InvalidDataException("Kugelöffnung: 0 (geschlossen) oder mindestens 5 mm, maximal 85 % des Innendurchmessers.");
            if (SphereCollarHeight is < 0 or > 30) throw new InvalidDataException("Die Kragenhöhe muss zwischen 0 und 30 mm liegen.");
            if (SphereVentDiameter < 0 || SphereVentDiameter is > 0 and < 2 || SphereVentDiameter > Math.Min(20,inner*.5))
                throw new InvalidDataException("Obere Entlüftungsöffnung: 0 oder 2–20 mm, maximal ein Viertel des Innendurchmessers.");
            if(SocketEnabled)SphereSocket.ValidateSettings(this);
        }
    }
}
public sealed class PrinterProfile
{
    public override string ToString()=>Name;
    public PrinterProfile Clone()=>(PrinterProfile)MemberwiseClone();
    public string Name { get; set; } = "Standard FDM · 220";
    public string Process { get; set; } = "FDM";
    public double BedX { get; set; } = 220;
    public double BedY { get; set; } = 220;
    public double BuildZ { get; set; } = 250;
    public double Nozzle { get; set; } = .4;
    public double Layer { get; set; } = .12;
    public string Material { get; set; } = "PLA, Weiß";
    public void Validate()
    {
        if(string.IsNullOrWhiteSpace(Name)||Name.Length>256 || string.IsNullOrWhiteSpace(Process)||Process.Length>32 || Material is null || Material.Length>256 || !new[]{BedX,BedY,BuildZ,Nozzle,Layer}.All(v=>double.IsFinite(v)&&v>0&&v<=2000))
            throw new InvalidDataException("Das Druckerprofil enthält einen ungültigen Namen, Materialtext oder ungültige positive Maße.");
    }
}
public sealed class ProjectData
{
    public int Version { get; set; } = 15;
    public string Name { get; set; } = "Mein Lithophanieprojekt";
    public string ImageName { get; set; } = "";
    public ModelSettings Model { get; set; } = new();
    public ImageEdits Edits { get; set; } = new();
    public PrinterProfile Printer { get; set; } = new();
    public PrintSetOptions PrintLayout { get; set; } = new();
    public void Validate()
    {
        if(Model is null||Edits is null||Printer is null||PrintLayout is null)throw new InvalidDataException("Projekteinstellungen fehlen.");
        if(string.IsNullOrWhiteSpace(Name)||Name.Length>256||ImageName is null||ImageName.Length>1024)throw new InvalidDataException("Ungültiger Projekt- oder Bildname.");
        Model.Validate();Edits.Validate();Printer.Validate();PrintLayout.Validate();
    }
}
public static class ProjectStore
{
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public static void Save(string path, ProjectData data, byte[] image, byte[]? thumbnail = null)
    {
        data.Validate();
        if(data.Version is not (1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 10 or 11 or 12 or 13 or 14 or 15))throw new InvalidDataException("Diese Projektversion wird nicht unterstützt.");
        data.Version=15;
        if(image.Length is 0 or > 64*1024*1024)throw new InvalidDataException("Bilddaten müssen vorhanden und höchstens 64 MB groß sein.");
        var temp = path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using (var zip = ZipFile.Open(temp,ZipArchiveMode.Create))
            {
                using(var stream = zip.CreateEntry("project.json").Open()) JsonSerializer.Serialize(stream,data,Json);
                using(var stream = zip.CreateEntry("source.bin",CompressionLevel.Fastest).Open()) stream.Write(image);
                if(thumbnail != null) { using var stream=zip.CreateEntry("preview.png").Open(); stream.Write(thumbnail); }
            }
            File.Move(temp,path,true);
        }
        finally { if(File.Exists(temp)) File.Delete(temp); }
    }
    public static (ProjectData Data,byte[] Image) Load(string path)
    {
        using var zip=ZipFile.OpenRead(path);
        var meta=zip.GetEntry("project.json") ?? throw new InvalidDataException("Projektbeschreibung fehlt.");
        if(meta.Length>1024*1024) throw new InvalidDataException("Projektbeschreibung ist zu groß.");
        using var ms=meta.Open();
        var data=JsonSerializer.Deserialize<ProjectData>(ms,Json) ?? throw new InvalidDataException("Ungültiges Projekt.");
        if(data.Version is not (1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 10 or 11 or 12 or 13 or 14 or 15)) throw new InvalidDataException("Diese Projektversion wird nicht unterstützt.");
        if(data.Version<3&&data.Edits!=null){data.Edits.AutoOrient=false;data.Edits.StraightenAngle=0;}
        if(data.Version<4&&data.Model!=null){data.Model.PrintOrientation=PrintOrientation.Upright;data.Model.TurnOnBed90=false;}
        if(data.Version<5&&data.Model!=null){data.Model.SocketEnabled=false;data.Model.Part=ModelPart.Lithophane;}
        if(data.Version<6&&data.Model!=null){data.Model.BackEnabled=false;if(data.Model.Part==ModelPart.BackPanel)data.Model.Part=ModelPart.Lithophane;}
        if(data.Version<7&&data.Model!=null)data.Model.BackMountingHoles=[];
        if(data.Version<8&&data.Model!=null){data.Model.Angles=default;data.Model.SocketAngles=default;data.Model.BackAngles=default;}
        if(data.Version<9&&data.Model?.BackMountingHoles!=null)foreach(var hole in data.Model.BackMountingHoles)if(hole!=null){hole.SlotExtension=0;hole.Horizontal=false;}
        if(data.Version<10&&data.Model?.BackMountingHoles!=null)foreach(var hole in data.Model.BackMountingHoles)if(hole!=null)hole.SinkDepth=0;
        if(data.Version<11&&data.Edits!=null){data.Edits.CropAspectWidth=4;data.Edits.CropAspectHeight=5;data.Edits.CropAspectLock=false;}
        if(data.Version<12&&data.Model!=null)data.Model.LockDimensions=false;
        if(data.Version<13&&data.Model!=null)data.Model.HangingTabsEnabled=false;
        if(data.Version<14)data.PrintLayout=new();
        if(data.Version<15&&data.PrintLayout!=null){data.PrintLayout.BodyX=5;data.PrintLayout.BodyY=5;data.PrintLayout.AccessoryX=100;data.PrintLayout.AccessoryY=5;if(data.PrintLayout.Arrangement==PrintArrangement.Manual)data.PrintLayout.Arrangement=PrintArrangement.Automatic;}
        data.Version=15;
        data.Validate();
        var entry=zip.GetEntry("source.bin") ?? throw new InvalidDataException("Das Originalbild fehlt.");
        if(entry.Length is 0 or > 64*1024*1024) throw new InvalidDataException("Bilddaten müssen vorhanden und höchstens 64 MB groß sein.");
        using var source=entry.Open(); using var buffer=new MemoryStream(); source.CopyTo(buffer);
        return(data,buffer.ToArray());
    }
}
