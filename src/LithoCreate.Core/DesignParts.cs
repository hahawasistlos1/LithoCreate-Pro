using System.IO.Compression;
using System.Numerics;

namespace LithoCreate.Core;

public sealed record DesignPart(string Name, Mesh Mesh, PrintOrientation Orientation = PrintOrientation.Upright, int Copies = 1);

public static class DesignParts
{
    public static readonly IReadOnlyList<string> WallNames = Array.AsReadOnly(new[] {"Vorne", "Rechts", "Hinten", "Links"});
    const double Floor = 5, JoinGap = 1.8, Clearance = .45;

    public static void Validate(ModelSettings s)
    {
        if (s.IsArch && s.ArchHangingHole && (s.Width < 20 || s.Height < 20))
            throw new InvalidDataException("Ein Bogenfenster mit Aufhängeöffnung benötigt mindestens 20 × 20 mm.");
        if (s.Shape == Shape.PhotoHoneycomb && (s.Width < 35 || s.Height < 30))
            throw new InvalidDataException("Eine verbindbare Fotowabe benötigt mindestens 35 × 30 mm.");
        if (s.HasWallMotifs && (s.Width < 55 || s.Height < 30 || s.MaxThickness > 5))
            throw new InvalidDataException("Laterne und Häuschen benötigen Paneele ab 55 × 30 mm mit maximal 5 mm Wandstärke.");
        if (s.IsPanorama && (s.Diameter < 80 || s.Height < 25 || s.MaxThickness > 6))
            throw new InvalidDataException("Der Panoramaschirm benötigt mindestens 80 mm Durchmesser und 25 mm Höhe bei maximal 6 mm Wandstärke.");
    }

    public static void ApplyTemplate(ModelSettings s)
    {
        (s.Width, s.Height) = s.Shape switch
        {
            Shape.WavePicture => (180, 90),
            Shape.PhotoLantern => (85, 112),
            Shape.PhotoHoneycomb => (100, 50 * Math.Sqrt(3)),
            Shape.ArchedWindow => (95, 125),
            Shape.MiniHouse => (68, 65),
            Shape.PanoramaShade => (100, 145),
            _ => (s.Width, s.Height)
        };
        if (s.IsPanorama)
        {
            s.Diameter = 320;
            s.Sides = 6;
        }
        s.LockDimensions = false;
        s.FrameWidth = 3;
        s.StandEnabled = false;
        s.ArchHangingHole = false;
        if (s.HasWallMotifs || s.IsPanorama)
        {
            s.MinThickness = Math.Min(s.MinThickness, 5);
            s.MaxThickness = Math.Min(s.MaxThickness, 5);
        }
    }

    public static Mesh Preview(ModelSettings s, GrayImage primary, IReadOnlyList<GrayImage?> walls, CancellationToken token)
    {
        var mesh = new Mesh { IsAssemblyPreview = true };
        foreach (var entry in Build(s, primary, walls, true, token))
        {
            int offset = mesh.Vertices.Count;
            mesh.Vertices.AddRange(entry.Mesh.Vertices);
            foreach (var triangle in entry.Mesh.Triangles)
                mesh.Face(offset + triangle.A, offset + triangle.B, offset + triangle.C);
            int wall = entry.Name switch { "Vorne" => 0, "Rechts" => 1, "Hinten" => 2, "Links" => 3, _ => -1 };
            bool hasTexture = entry.Mesh.TextureCoordinates.Count == entry.Mesh.Vertices.Count;
            for (int i = 0; i < entry.Mesh.Vertices.Count; i++)
            {
                var uv = hasTexture ? entry.Mesh.TextureCoordinates[i] : Vector2.Zero;
                if (wall >= 0) uv.X = (wall + (.5f + uv.X * 255) / 256) / 4;
                mesh.TextureCoordinates.Add(uv);
                mesh.PhotoVertices.Add(hasTexture && entry.Mesh.PhotoVertices[i]);
            }
            mesh.ActualResolution = Math.Max(mesh.ActualResolution, entry.Mesh.ActualResolution);
        }
        return mesh;
    }

    public static IEnumerable<DesignPart> Build(ModelSettings s, GrayImage primary, IReadOnlyList<GrayImage?> walls,
        bool preview, CancellationToken token = default)
    {
        s.Validate();
        double thickness = s.MaxThickness + 1;
        if (s.Shape == Shape.PhotoHoneycomb)
        {
            yield return new("Fotowabe", MeshGenerator.GeneratePanel(primary, s, preview, token), PrintOrientation.FaceUp);
            yield return new("Wabenverbinder", Joiner(thickness, 15, Clearance));
            yield break;
        }
        if (s.IsPanorama)
        {
            for (int i = 0; i < s.Sides; i++)
            {
                double angle = i * Math.Tau / s.Sides;
                Mesh segment = Segment(s, primary, i, preview, token);
                if (preview) Transform(segment, angle, default);
                yield return new($"Schirmsegment {i + 1:00}", segment);
            }
            if (!preview) yield return new("Nahtklammer", Joiner(thickness, 7, .85), Copies: s.Sides * 2);
            yield break;
        }
        if (!s.HasWallMotifs) throw new InvalidDataException("Die gewählte Form hat keinen Bauteilsatz.");

        double half = s.Width / 2 + 4, outer = half + thickness + 1.4;
        double height = s.Height + Floor;
        double face = half - 2 + Clearance / 2;
        yield return new("Rahmen mit Kabelausgang", Frame(half, outer, height, thickness));
        var panel = s.Clone();
        panel.Shape = Shape.Flat;
        panel.StandEnabled = panel.HangingTabsEnabled = false;
        panel.FrameWidth = Math.Min(Math.Max(s.FrameWidth, 5), Math.Min(panel.Width, panel.Height) / 2 - 1);
        panel.MemoryMb = Math.Max(64, s.MemoryMb / 4);
        for (int i = 0; i < WallNames.Count; i++)
        {
            var part = MeshGenerator.GeneratePanel(i < walls.Count ? walls[i] ?? primary : primary, panel, preview, token);
            if (preview) Transform(part, i * Math.PI / 2, new(0, (float)(Floor + .15), (float)face));
            yield return new(WallNames[i], part, PrintOrientation.FaceUp);
        }
        Mesh lid = s.Shape == Shape.MiniHouse ? Roof(outer) : Lid(outer);
        if (preview) Transform(lid, 0, new(0, (float)(height + .35), 0));
        yield return s.Shape == Shape.MiniHouse ? new("Abnehmbares Dach", lid, PrintOrientation.RightEdgeDown) : new("Abnehmbarer Deckel", lid, PrintOrientation.UpsideDown);
        if (s.Shape == Shape.PhotoLantern)
        {
            double length = Math.Min(220, s.Width - 22);
            var tray = LightInserts.Generate(new(LightInsertKind.LedStrip, LightInsertOutline.Rectangle, 14.5, length, 10.5, 2, 7), token);
            if (preview) Transform(tray, 0, new(0, (float)(Floor + .01), (float)(-outer + 3)));
            yield return new("LED-Band-Einsatz 8–10 mm", tray);
        }
    }

    public static int Export(string path, string extension, ModelSettings settings, GrayImage main,
        IReadOnlyList<GrayImage?> walls, PrinterProfile printer, CancellationToken token = default, IProgress<string>? status = null)
    {
        if (extension is not ("stl" or "3mf")) throw new ArgumentException("Unbekanntes Druckformat.", nameof(extension));
        printer.Validate();
        string pending = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        int count = 0;
        try
        {
            using (var zip = ZipFile.Open(pending, ZipArchiveMode.Create))
            {
                using (var readme = new StreamWriter(zip.CreateEntry("Montage.txt").Open()))
                    readme.WriteLine(Instructions(settings.Shape));
                foreach (var part in Build(settings, main, walls, false, token))
                {
                    token.ThrowIfCancellationRequested();
                    status?.Report(part.Name + " prüfen und exportieren …");
                    PrintPlacement.Apply(part.Mesh, part.Orientation, false, token);
                    if (!Fits(part.Mesh, printer))
                    {
                        PrintPlacement.Apply(part.Mesh, PrintOrientation.Upright, true, token);
                        if (!Fits(part.Mesh, printer))
                            throw new InvalidDataException($"„{part.Name}“ passt nicht ins gewählte Druckvolumen. Maße reduzieren oder beim Panoramaschirm die Segmentzahl erhöhen.");
                    }
                    string name = $"{++count:00} - {part.Name}";
                    if (part.Copies > 1) name += $" ({part.Copies}x drucken)";
                    string intermediate = pending + "." + extension;
                    try
                    {
                        ModelExporter.Export(intermediate, part.Mesh, token);
                        zip.CreateEntryFromFile(intermediate, name + "." + extension, CompressionLevel.Fastest);
                    }
                    finally { File.Delete(intermediate); }
                }
            }
            token.ThrowIfCancellationRequested();
            File.Move(pending, path, true);
            return count;
        }
        finally { File.Delete(pending); }
    }

    static bool Fits(Mesh mesh, PrinterProfile printer)
    {
        var (min, max) = mesh.Bounds();
        Vector3 size = max - min;
        return PrintPlacement.Fits(new(size.X, size.Z, size.Y), printer);
    }

    public static string Instructions(Shape shape) => shape switch
    {
        Shape.PhotoHoneycomb => "Eine Fotowabe pro Motiv exportieren. Waben mit gleichen Maßen und gleicher Maximalstärke verwenden. Je zwei benachbarte Kanten in die beiden Nuten des Verbinders schieben. Je Naht mindestens einen Verbinder drucken. Das Druckspiel vor dem Aufbau prüfen.",
        Shape.PanoramaShade => "Die Segmente in der Dateireihenfolge im Kreis anordnen. Pro Naht eine Klammer von oben und eine umgedreht von unten aufschieben. Klammern für den Druck vervielfältigen. Passung und Druckauflage im Slicer prüfen. Den Schirm separat und tragfähig abstützen. Nur geeignete, kühl betriebene LED-Leuchtmittel verwenden.",
        Shape.MiniHouse => "Die vier Paneele mit dem Motiv nach außen von oben in die Nuten schieben. Ein LED-Teelicht in den Innenraum stellen und das Dach lose aufsetzen. Offene Giebel lassen Licht austreten. Dach bei Bedarf im Slicer mit Brim drucken. Nur LED-Teelichter verwenden.",
        _ => "Die vier Paneele mit dem Motiv nach außen von oben in die Nuten schieben. Das LED-Band auf den losen Einsatz kleben und das Kabel durch den rückwärtigen Bodenschlitz führen. Den Deckel aufsetzen. Für Fassungen kann im Editor ein separater Lichteinsatz erzeugt werden. Nur geeignete, kühl betriebene LED-Leuchtmittel verwenden."
    };

    static Mesh Frame(double innerCorner, double outer, double height, double thickness)
    {
        double q = innerCorner, lower = q - 2, upper = lower + thickness + Clearance;
        double[] positive = [5, outer - 13, q - 10, q - 3, lower, upper, outer];
        double[] plane = positive.SelectMany(p => new[] { -p, p }).Append(0)
            .DistinctBy(p => (float)p).Order().ToArray();
        return Blocks(plane, [0, Floor, height], plane, (x, y, z) =>
        {
            if (y < Floor) return !(Math.Abs(x) < 5 && z < -outer + 13);
            x = Math.Abs(x); z = Math.Abs(z);
            if (x < q - 10 || z < q - 10) return false;
            bool grooveX = x < q - 3 && z > lower && z < upper;
            bool grooveZ = z < q - 3 && x > lower && x < upper;
            return !grooveX && !grooveZ;
        });
    }

    static Mesh Lid(double width)
    {
        double inside = width + .35, outer = inside + 2, opening = width - 9;
        double[] plane = [-outer, -inside, -opening, opening, inside, outer];
        return Blocks(plane, [-3, 0, 2.5], plane, (x, y, z) =>
        {
            double edge = Math.Max(Math.Abs(x), Math.Abs(z));
            return y < 0 ? edge > inside : edge > opening;
        });
    }

    static Mesh Roof(double width)
    {
        double inside = width + .4, eave = inside + 3, rise = Math.Min(width * .55, 45);
        double[] steps = [-eave, -inside, 0, inside, eave];
        return Shell(1, steps.Length - 1, (i, j, front) =>
        {
            double z = steps[j], y = rise * (1 - Math.Abs(z) / inside) + (front ? 2.5 : 0);
            return (new Vector3((float)(i == 0 ? -eave : eave), (float)y, (float)z), Vector2.Zero, false);
        });
    }

    static Mesh Joiner(double thickness, double length, double radialPlay)
    {
        double gap = thickness + radialPlay;
        const double halfStem = .7;
        return Blocks([-5, -halfStem, halfStem, 5], [0, length], [-1.6, 0, gap, gap + 1.6],
            (x, _, z) => Math.Abs(x) < halfStem || z < 0 || z > gap);
    }

    static Mesh Segment(ModelSettings s, GrayImage image, int segment, bool preview, CancellationToken token)
    {
        double thickness = s.MaxThickness + 1, radius = s.Diameter / 2 - thickness;
        double sector = Math.Tau / s.Sides, seam = Math.Asin(JoinGap / (2 * radius));
        double span = (sector - 2 * seam) * radius;
        double budget = preview ? 18000d / s.Sides : Math.Min(1150000d, s.MemoryMb * 1024d * 1024 / 600);
        double step = Math.Max(s.Resolution, Math.Sqrt(span * s.Height / budget));
        int nx = Math.Clamp((int)Math.Ceiling(span / step), 8, 2400);
        int ny = Math.Clamp((int)Math.Ceiling(s.Height / step), 8, 2400);
        var mesh = Shell(nx, ny, (i, j, front) =>
        {
            if (i == 0) token.ThrowIfCancellationRequested();
            double u = i / (double)nx, v = j / (double)ny;
            double a = (u - .5) * (sector - 2 * seam), y = v * s.Height;
            double photoU = ((segment + .5) * sector + a) / Math.Tau;
            double luminance = image.Sample(photoU, 1 - v);
            if (s.Negative) luminance = 1 - luminance;
            bool border = Math.Min(u, 1 - u) * span <= 5 || Math.Min(y, s.Height - y) <= 8;
            double wall = border ? thickness : s.MinThickness + (1 - luminance) * (s.MaxThickness - s.MinThickness);
            double r = front ? radius + wall : radius;
            return (new Vector3((float)(r * Math.Sin(a)), (float)y, (float)(r * Math.Cos(a))),
                new Vector2((float)photoU, (float)(1 - v)), !border);
        });
        mesh.ActualResolution = Math.Max(span / nx, s.Height / ny);
        return mesh;
    }

    static Mesh Shell(int nx, int ny, Func<int, int, bool, (Vector3 Position, Vector2 Uv, bool Photo)> vertex)
    {
        var mesh = new Mesh();
        for (int layer = 0; layer < 2; layer++)
        for (int j = 0; j <= ny; j++)
        for (int i = 0; i <= nx; i++)
        {
            var point = vertex(i, j, layer == 0);
            mesh.Vertices.Add(point.Position);
            mesh.TextureCoordinates.Add(point.Uv);
            mesh.PhotoVertices.Add(point.Photo);
        }
        int offset = (nx + 1) * (ny + 1);
        int Id(int i, int j) => j * (nx + 1) + i;
        for (int j = 0; j < ny; j++)
        for (int i = 0; i < nx; i++)
        {
            int a = Id(i, j), b = Id(i + 1, j), c = Id(i + 1, j + 1), d = Id(i, j + 1);
            mesh.Quad(a, b, c, d);
            mesh.Quad(a + offset, d + offset, c + offset, b + offset);
            if (j == 0) Edge(a, b);
            if (j == ny - 1) Edge(c, d);
            if (i == 0) Edge(d, a);
            if (i == nx - 1) Edge(b, c);
        }
        mesh.OrientOutward();
        return mesh;
        void Edge(int a, int b) => mesh.Quad(b, a, a + offset, b + offset);
    }

    static Mesh Blocks(double[] xs, double[] ys, double[] zs, Func<double, double, double, bool> fill)
    {
        var mesh = new Mesh();
        var indices = new Dictionary<(int, int, int), int>();
        var occupied = new bool[xs.Length - 1, ys.Length - 1, zs.Length - 1];
        for (int x = 0; x < xs.Length - 1; x++)
        for (int y = 0; y < ys.Length - 1; y++)
        for (int z = 0; z < zs.Length - 1; z++)
            occupied[x, y, z] = fill((xs[x] + xs[x + 1]) / 2, (ys[y] + ys[y + 1]) / 2, (zs[z] + zs[z + 1]) / 2);
        for (int x = 0; x < xs.Length - 1; x++)
        for (int y = 0; y < ys.Length - 1; y++)
        for (int z = 0; z < zs.Length - 1; z++)
        {
            if (!Cell(x, y, z)) continue;
            if (!Cell(x - 1, y, z)) mesh.Quad(V(x,y,z), V(x,y,z+1), V(x,y+1,z+1), V(x,y+1,z));
            if (!Cell(x + 1, y, z)) mesh.Quad(V(x+1,y,z), V(x+1,y+1,z), V(x+1,y+1,z+1), V(x+1,y,z+1));
            if (!Cell(x, y - 1, z)) mesh.Quad(V(x,y,z), V(x+1,y,z), V(x+1,y,z+1), V(x,y,z+1));
            if (!Cell(x, y + 1, z)) mesh.Quad(V(x,y+1,z), V(x,y+1,z+1), V(x+1,y+1,z+1), V(x+1,y+1,z));
            if (!Cell(x, y, z - 1)) mesh.Quad(V(x,y,z), V(x,y+1,z), V(x+1,y+1,z), V(x+1,y,z));
            if (!Cell(x, y, z + 1)) mesh.Quad(V(x,y,z+1), V(x+1,y,z+1), V(x+1,y+1,z+1), V(x,y+1,z+1));
        }
        mesh.OrientOutward();
        return mesh;
        bool Cell(int x, int y, int z) => x >= 0 && y >= 0 && z >= 0 && x < xs.Length - 1 && y < ys.Length - 1 && z < zs.Length - 1 && occupied[x, y, z];
        int V(int x, int y, int z)
        {
            if (indices.TryGetValue((x, y, z), out int id)) return id;
            id = mesh.Vertices.Count;
            mesh.Vertices.Add(new((float)xs[x], (float)ys[y], (float)zs[z]));
            indices.Add((x, y, z), id);
            return id;
        }
    }

    static void Transform(Mesh mesh, double angle, Vector3 translation)
    {
        var rotation = Matrix4x4.CreateRotationY((float)angle);
        for (int i = 0; i < mesh.Vertices.Count; i++)
            mesh.Vertices[i] = Vector3.Transform(mesh.Vertices[i] + translation, rotation);
    }
}
