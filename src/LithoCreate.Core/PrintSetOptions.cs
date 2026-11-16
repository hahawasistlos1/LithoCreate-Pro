namespace LithoCreate.Core;

public enum PrintArrangement { Automatic, Row, Column, Manual }
public sealed record PrintArrangementChoice(PrintArrangement Arrangement,string Name)
{
    public override string ToString()=>Name;
}
public sealed class PrintSetOptions
{
    public double Margin { get; set; } = 5;
    public double Gap { get; set; } = 5;
    public PrintArrangement Arrangement { get; set; }
    public double BodyX { get; set; } = 5;
    public double BodyY { get; set; } = 5;
    public double AccessoryX { get; set; } = 100;
    public double AccessoryY { get; set; } = 5;
    public static readonly PrintArrangementChoice[] Choices=[new(PrintArrangement.Automatic,"Automatisch"),new(PrintArrangement.Row,"Zeile · nebeneinander"),new(PrintArrangement.Column,"Spalte · hintereinander"),new(PrintArrangement.Manual,"Manuell · freie Positionen")];
    public PrintSetOptions Clone()=>(PrintSetOptions)MemberwiseClone();
    public void Validate()
    {
        if(!double.IsFinite(Margin)||Margin is < 0 or > 50)throw new InvalidDataException("Drucksatz-Randabstand: 0–50 mm eingeben.");
        if(!double.IsFinite(Gap)||Gap is < 1 or > 50)throw new InvalidDataException("Drucksatz-Teileabstand: 1–50 mm eingeben.");
        if(!Enum.IsDefined(Arrangement))throw new InvalidDataException("Die Drucksatzanordnung wird nicht unterstützt.");
        if(!new[]{BodyX,BodyY,AccessoryX,AccessoryY}.All(v=>double.IsFinite(v)&&v>=0&&v<=2000))throw new InvalidDataException("Teilepositionen: endliche X-/Y-Werte von 0–2000 mm eingeben.");
    }
}
