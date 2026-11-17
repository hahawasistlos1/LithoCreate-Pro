using System.Numerics;

namespace LithoCreate.Core;

public enum LightInsertKind { Socket, LedStrip }
public enum LightInsertOutline { Round, Rectangle }

public sealed record LightInsertPreset(string Name, LightInsertKind Kind, double Opening)
{
    public override string ToString() => Name;
}

public sealed record LightInsertOptions(LightInsertKind Kind, LightInsertOutline Outline, double Width,
    double Length, double Opening, double Thickness, double CableWidth)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Kind) || !Enum.IsDefined(Outline) ||
            !new[] { Width, Length, Opening, Thickness, CableWidth }.All(double.IsFinite) ||
            Length is < 15 or > 250 ||
            Thickness is < 1.2 or > 8 || CableWidth is < 2 or > 30)
            throw new InvalidDataException("Außenmaße bis 250 mm, Stärke 1,2–8 mm und Kabelbreite 2–30 mm eingeben.");

        if (Kind == LightInsertKind.LedStrip)
        {
            if (Opening is < 3 or > 40 || CableWidth > Opening || Length < Math.Max(Thickness, CableWidth / 2) + 2)
                throw new InvalidDataException("Die Bandaufnahme muss 3–40 mm breit sein. Der Kabelausgang darf nicht breiter sein als die Aufnahme.");
            return;
        }

        double halfLength = (Outline == LightInsertOutline.Round ? Width : Length) / 2;
        double notchRadius = halfLength - Math.Max(Thickness, CableWidth * .65);
        double boreRadius = Opening / 2;
        if (Width is < 10 or > 250 || Opening is < 4 or > 100 ||
            Math.Min(Width / 2, halfLength) < boreRadius + 2.1 ||
            notchRadius < boreRadius + 2.1 || CableWidth / 2 + 2 >= Math.Min(Width / 2, notchRadius))
            throw new InvalidDataException("Für Öffnung und Kabelausgang bleibt zu wenig Material. Außenmaß vergrößern oder Öffnungen verkleinern.");
    }
}

public static class LightInserts
{
    public static readonly IReadOnlyList<LightInsertPreset> Presets =
    [
        new("E14 · fertige LED-Fassung", LightInsertKind.Socket, WorkshopParts.E14Opening),
        new("E27 · fertige LED-Fassung", LightInsertKind.Socket, WorkshopParts.E27Opening),
        new("Fassung · eigenes Maß", LightInsertKind.Socket, 35),
        new("LED-Band · 8 mm", LightInsertKind.LedStrip, 8.5),
        new("LED-Band · 10 mm", LightInsertKind.LedStrip, 10.5),
        new("LED-Band · eigene Breite", LightInsertKind.LedStrip, 12.5)
    ];

    public static (LightInsertOptions Options, string Placement) Suggest(ModelSettings model, LightInsertPreset preset)
    {
        const double clearance = .6;
        bool round = model.IsShade || model.IsPanorama || model.SupportsSphereOpenings || model.SupportsLampBase;
        double width = model.Width;
        double depth = model.Height;
        string placement = "Separat hinter dem Motiv platzieren.";
        if (model.IsShade)
        {
            double diameter = model.ShadeMount == ShadeMount.Top ? model.ShadeTopDiameter : model.ShadeBottomDiameter;
            if (model.IsFacetedShade) diameter *= Math.Cos(Math.PI / model.ShadeSides);
            width = depth = diameter - 2 * model.MaxThickness;
            string rim = model.ShadeMount == ShadeMount.Top ? "oberen" : "unteren";
            placement = $"Maßvorschlag für den {rim} Schirmrand. Einen vorhandenen Montagering bei der Anprobe berücksichtigen.";
        }
        else if (model.IsPanorama)
        {
            width = depth = model.Diameter - 2 * (model.MaxThickness + 1);
            placement = "Separaten Träger am offenen Panoramaschirm platzieren und unabhängig abstützen.";
        }
        else if (model.HasWallMotifs)
        {
            width = depth = model.Width - 15;
            placement = "Den Einsatz lose auf den Rahmenboden stellen und die Leitung zum rückwärtigen Schlitz führen.";
        }
        else if (model.SupportsSphereOpenings)
        {
            bool open = model.SphereOpeningDiameter > 0;
            width = depth = open ? model.SphereOpeningDiameter : model.Diameter * .65;
            placement = open
                ? "Maßvorschlag für die untere Kugelöffnung. Eingriff und Befestigung separat prüfen."
                : "Maßvorschlag für einen Außenträger. Für einen Einsatz im Inneren zuerst eine Kugelöffnung anlegen.";
        }
        else if (model.SupportsLampBase)
        {
            double face = model.Shape == Shape.Polygon ? Math.Cos(Math.PI / model.Sides) : 1;
            width = depth = model.Diameter * face - 2 * model.MaxThickness;
            placement = "Maßvorschlag für das offene Ende der Lampe.";
        }
        else if (model.SupportsBackPanel)
        {
            width -= 2 * model.FrameWidth;
            depth -= 2 * model.FrameWidth;
            placement = "Maßvorschlag für die Rückseite des Leuchtkastens.";
        }
        else if (model.Shape is Shape.Disc or Shape.Pendant)
        {
            width = depth = model.Diameter;
        }

        double availableWidth = Math.Clamp(Math.Floor((width - clearance) * 10) / 10, 10, 250);
        double availableDepth = Math.Clamp(Math.Floor((depth - clearance) * 10) / 10, 10, 250);
        if (preset.Kind == LightInsertKind.LedStrip)
        {
            double railWidth = preset.Opening + 4;
            double availableLength = round
                ? Math.Sqrt(Math.Max(0, availableWidth * availableWidth - railWidth * railWidth))
                : availableWidth;
            if (availableLength < 20 || (!round && availableDepth < railWidth))
                placement = "Die Schiene benötigt am gewählten Modell einen separaten Außenträger.";
            double length = Math.Clamp(Math.Floor(availableLength * 10) / 10, 20, 220);
            return (new(preset.Kind, LightInsertOutline.Rectangle, railWidth, length, preset.Opening, 2, 5), placement);
        }

        double minimum = Math.Ceiling(preset.Opening + 16);
        if (availableWidth < minimum || availableDepth < minimum)
            placement = "Für diesen Fassungsdurchlass ist ein größerer Außenträger erforderlich.";
        return (new(preset.Kind, round ? LightInsertOutline.Round : LightInsertOutline.Rectangle,
            Math.Max(minimum, availableWidth), Math.Max(minimum, availableDepth), preset.Opening, 3, 8), placement);
    }

    public static Mesh Generate(LightInsertOptions options, CancellationToken token = default)
    {
        options.Validate();
        token.ThrowIfCancellationRequested();
        return options.Kind == LightInsertKind.Socket ? BuildSocketPlate(options, token) : BuildStripTray(options, token);
    }

    private static Mesh BuildSocketPlate(LightInsertOptions options, CancellationToken token)
    {
        bool round = options.Outline == LightInsertOutline.Round;
        double halfWidth = options.Width / 2;
        double halfLength = round ? halfWidth : options.Length / 2;
        double notchRadius = halfLength - Math.Max(options.Thickness, options.CableWidth * .65);
        double notchAngle = Math.Asin(options.CableWidth / (2 * notchRadius));
        double transition = Math.Min(Math.Atan2(1.5, notchRadius), .03);
        var angles = new SortedSet<double>(Enumerable.Range(0, 192).Select(i => Math.Tau * i / 192));
        AddAngle(notchAngle);
        AddAngle(notchAngle + transition);
        AddAngle(Math.Tau - notchAngle);
        AddAngle(Math.Tau - notchAngle - transition);
        if (!round)
        {
            double corner = Math.Atan2(halfWidth, halfLength);
            AddAngle(corner);
            AddAngle(Math.PI - corner);
            AddAngle(Math.PI + corner);
            AddAngle(Math.Tau - corner);
        }
        double[] rays = angles.ToArray();
        int count = rays.Length;
        double largestGap = rays.Zip(rays.Skip(1).Append(Math.Tau), (a, b) => b - a).Max();
        double openingRadius = options.Opening / (2 * Math.Cos(largestGap / 2));
        var mesh = new Mesh();
        for (int loop = 0; loop < 4; loop++)
        {
            foreach (double angle in rays)
            {
                token.ThrowIfCancellationRequested();
                var (x, z) = Math.SinCos(angle);
                double radius = openingRadius;
                if (loop < 2)
                {
                    double edge = round ? halfWidth : Math.Min(halfWidth / Math.Abs(x), halfLength / Math.Abs(z));
                    double distance = Math.Min(angle, Math.Tau - angle);
                    double blend = Math.Clamp((distance - notchAngle) / transition, 0, 1);
                    radius = notchRadius + (edge - notchRadius) * blend;
                }
                float height = loop is 1 or 2 ? (float)options.Thickness : 0;
                mesh.Vertices.Add(new((float)(x * radius), height, (float)(z * radius)));
            }
        }
        for (int loop = 0; loop < 4; loop++)
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count, adjoining = (loop + 1) % 4;
            mesh.Quad(loop * count + i, loop * count + next, adjoining * count + next, adjoining * count + i);
        }
        mesh.OrientOutward();
        return mesh;

        void AddAngle(double value)
        {
            if (angles.GetViewBetween(value - 1e-5, value + 1e-5).Count == 0) angles.Add(value);
        }
    }

    private static Mesh BuildStripTray(LightInsertOptions options, CancellationToken token)
    {
        double halfChannel = options.Opening / 2;
        double halfCable = options.CableWidth / 2;
        double wall = options.Thickness;
        if (halfChannel - halfCable < .0001) halfCable = halfChannel;
        float[] x = new[] { -halfChannel - wall, -halfChannel, -halfCable, halfCable, halfChannel, halfChannel + wall }
            .Select(value => (float)value).Distinct().ToArray();
        float[] y = [0, (float)wall, (float)(wall + 3)];
        float[] z = [0, (float)Math.Max(wall, halfCable), (float)options.Length];
        int nx = x.Length - 1;
        bool[,,] filled = new bool[nx, 2, 2];
        for (int i = 0; i < nx; i++)
        {
            double center = (x[i] + x[i + 1]) / 2;
            bool rail = i == 0 || i == nx - 1;
            filled[i,0,0] = Math.Abs(center) > halfCable;
            filled[i,0,1] = true;
            filled[i,1,0] = filled[i,1,1] = rail;
        }
        var mesh = new Mesh();
        var vertices = new Dictionary<(int X, int Y, int Z),int>();
        for (int i = 0; i < nx; i++)
        for (int j = 0; j < 2; j++)
        for (int k = 0; k < 2; k++)
        {
            token.ThrowIfCancellationRequested();
            if (!Filled(i,j,k)) continue;
            if (!Filled(i-1,j,k)) Face((i,j,k),(i,j,k+1),(i,j+1,k+1),(i,j+1,k));
            if (!Filled(i+1,j,k)) Face((i+1,j,k),(i+1,j+1,k),(i+1,j+1,k+1),(i+1,j,k+1));
            if (!Filled(i,j-1,k)) Face((i,j,k),(i+1,j,k),(i+1,j,k+1),(i,j,k+1));
            if (!Filled(i,j+1,k)) Face((i,j+1,k),(i,j+1,k+1),(i+1,j+1,k+1),(i+1,j+1,k));
            if (!Filled(i,j,k-1)) Face((i,j,k),(i,j+1,k),(i+1,j+1,k),(i+1,j,k));
            if (!Filled(i,j,k+1)) Face((i,j,k+1),(i+1,j,k+1),(i+1,j+1,k+1),(i,j+1,k+1));
        }
        return mesh;

        bool Filled(int i, int j, int k) => i >= 0 && i < nx && j is >= 0 and < 2 && k is >= 0 and < 2 && filled[i,j,k];
        void Face((int,int,int) a, (int,int,int) b, (int,int,int) c, (int,int,int) d) =>
            mesh.Quad(Vertex(a), Vertex(b), Vertex(c), Vertex(d));
        int Vertex((int X, int Y, int Z) key)
        {
            if (vertices.TryGetValue(key,out int index)) return index;
            index = mesh.Vertices.Count;
            mesh.Vertices.Add(new Vector3(x[key.X],y[key.Y],z[key.Z]));
            vertices.Add(key,index);
            return index;
        }
    }
}
