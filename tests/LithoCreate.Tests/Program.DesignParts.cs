using System.IO;
using System.IO.Compression;
using LithoCreate.App.Services;
using LithoCreate.Core;

partial class Program
{
    static IEnumerable<Shape> SingleBodyShapes() => Enum.GetValues<Shape>()
        .Where(shape => shape is not (Shape.PhotoLantern or Shape.MiniHouse or Shape.PanoramaShade));

    static void TestNewDesigns()
    {
        var light = new GrayImage(1, 1, [255]);
        var dark = new GrayImage(1, 1, [0]);
        var printer = new PrinterProfile { Name = "Bambu Lab P1S", BedX = 256, BedY = 256, BuildZ = 256 };

        Test("Wellenbild hat einen S-förmigen Rücken", () =>
        {
            var model = Settings(Shape.WavePicture);
            var wave = MeshGenerator.Generate(light, model, false);
            var report = wave.Validate();
            Require(report.IsValid && report.Bounds.Min.Z < -10 && report.Bounds.Max.Z > 10, "Der Rücken ist nicht beidseitig gekrümmt.");
            CheckRoundtrip(wave, "Wellenbild");
        });
        Test("Bogenfenster mit Durchgang und integriertem Standfuß", () =>
        {
            var model = Settings(Shape.ArchedWindow);
            model.ArchHangingHole = model.StandEnabled = true;
            var arch = MeshGenerator.Generate(light, model, false);
            var report = arch.Validate();
            Require(report.IsValid && Euler(arch) == 0, $"Bogenfenster mit Öffnung ungültig: {report}");
            Require(Math.Abs(report.Size.Y - model.Height - model.StandHeight) < .001, "Bogenhöhe verändert.");
            Require(Math.Abs(report.Size.Z - model.StandDepth) < .001, "Standtiefe verändert.");
            var left = arch.Vertices.Where(p=>p.X < -model.Width / 2 + .001).Max(p=>p.Y);
            Require(report.Bounds.Max.Y - left > 15, "Rundbogen fehlt.");
            CheckRoundtrip(arch, "Bogenfenster");
        });
        Test("Wabenmaße bleiben beim Skalieren sechseckig", () =>
        {
            var dimensions = ModelSizing.Resize(Settings(Shape.PhotoHoneycomb), true, 140);
            Require(Math.Abs(dimensions.Height - 70 * Math.Sqrt(3)) < .001, "Das Seitenverhältnis wurde verändert.");
        });

        foreach (var (shape, expected) in new[]
        {
            (Shape.PhotoHoneycomb, 2), (Shape.PhotoLantern, 7), (Shape.MiniHouse, 6), (Shape.PanoramaShade, 7)
        })
        {
            Test($"{shape}: Druckteile als STL- und 3MF-ZIP", () =>
            {
                var model = Settings(shape);
                GrayImage?[] walls = [light, dark, null, null];
                foreach (var extension in new[] { "stl", "3mf" })
                {
                    string path = Path.Combine(output, $"{shape}-{extension}.zip");
                    int count = DesignParts.Export(path, extension, model, light, walls, printer);
                    using var zip = ZipFile.OpenRead(path);
                    Require(count == expected && zip.Entries.Count == count + 1 && zip.GetEntry("Montage.txt") != null, "Im Archiv fehlen Bauteile oder die Montageanleitung.");
                    foreach (var entry in zip.Entries.Where(e => e.Name.EndsWith("." + extension, StringComparison.Ordinal)))
                    {
                        string file = Path.Combine(output, $"{shape}-{entry.Name}");
                        entry.ExtractToFile(file, true);
                        var mesh = extension == "stl" ? ModelExporter.ReadStl(file) : ModelExporter.Read3Mf(file);
                        var report = mesh.Validate();
                        Require(report.IsValid && PrintPlacement.Fits(report.Size, printer), $"{entry.Name} ist ungültig oder passt nicht in das Druckvolumen.");
                    }
                }
                if (model.HasWallMotifs)
                {
                    var preview = DesignParts.Preview(model, light, walls, default);
                    Require(preview.Validate().IsValid && preview.IsAssemblyPreview, "Montagevorschau ungültig.");
                    var panels = DesignParts.Build(model, light, walls, false).Skip(1).Take(2).ToArray();
                    Require(panels[0].Mesh.Validate().Volume < panels[1].Mesh.Validate().Volume, "Unterschiedliche Wandmotive erzeugen identisches Relief.");
                    var frame = DesignParts.Build(model, light, walls, false).First().Mesh;
                    var bounds = frame.Bounds();
                    Require(VerticalIntersections(frame, .23, bounds.Min.Z + 3.3) == 0, "Bodenausschnitt ist blockiert.");
                    Require(VerticalIntersections(frame, 6.23, bounds.Min.Z + 3.3) == 2, "Material neben dem Bodenausschnitt fehlt.");
                }
            });
        }
        Test("Segmente folgen dem Rundummotiv und Nahtklammern sind gezählt", () =>
        {
            var s = Settings(Shape.PanoramaShade);
            s.Sides = 5;
            var mesh = DesignParts.Preview(s, new GrayImage(3, 1, [0, 255, 0]), [], default);
            var b = mesh.Bounds();
            Require(mesh.Validate().IsValid && b.Max.X - b.Min.X > 300, "Schirmsegmente wurden nicht im Kreis angeordnet.");
            var parts = DesignParts.Build(s, light, [], false).ToArray();
            Require(parts.Length == 6 && parts[^1].Copies == 10, "Anzahl der Nahtklammern falsch.");
            Require(parts[0].Mesh.TextureCoordinates.Max(uv => uv.X) < parts[1].Mesh.TextureCoordinates.Max(uv => uv.X), "Segmente verwenden denselben Bildausschnitt.");
        });
        Test("Wandfotos und Textmotiv bleiben im gespeicherten Projekt", () =>
        {
            var project = new ProjectData { Model = Settings(Shape.MiniHouse) };
            byte[] main = ImageProcessor.Png(ImageProcessor.Demo());
            project.WallMotifs[1] = main;
            project.WallMotifs[3] = WallImages.Text("Zuhause", project.Model.Width / project.Model.Height);
            string path = Path.Combine(output, "Mini-Haeuschen-Motive.lithopro");
            ProjectStore.Save(path, project, main);
            var loaded = ProjectStore.Load(path).Data;
            Require(loaded.WallMotifs[0] is null && loaded.WallMotifs[1]!.SequenceEqual(main) &&
                loaded.WallMotifs[3]!.SequenceEqual(project.WallMotifs[3]!), "Wandmotive wurden verändert.");
            var walls = WallImages.Decode(loaded.WallMotifs, default);
            Require(walls[3]!.Pixels.Min() < 15 && walls[3]!.Pixels.Max() > 240, "Textmotiv fehlt im Relief.");
        });
        Test("Zu große Teile lassen einen vorhandenen ZIP-Export intakt", () =>
        {
            var tiny = new PrinterProfile { BedX = 80, BedY = 80, BuildZ = 90 };
            string path = Path.Combine(output, "Unveraenderter-Drucksatz.zip");
            File.WriteAllText(path, "vorhanden");
            Throws<InvalidDataException>(() => DesignParts.Export(path, "stl", Settings(Shape.PanoramaShade), light, [], tiny));
            Require(File.ReadAllText(path) == "vorhanden", "Fehlerhafter Export hat die Zieldatei überschrieben.");
            File.Delete(path);
        });

        static ModelSettings Settings(Shape shape)
        {
            var settings = new ModelSettings { Shape = shape, Resolution = 1.4 };
            DesignParts.ApplyTemplate(settings);
            return settings;
        }
    }
}
