using System.IO;
using System.Numerics;
using LithoCreate.Core;

partial class Program
{
    static void TestLampShades()
    {
        var panorama = new GrayImage(4, 2, [0, 65, 180, 255, 240, 210, 90, 30]);
        foreach (var template in Templates.All.Where(item => LampShade.Supports(item.Shape)))
        {
            Test("Lampenschirm: Geometrie, Öffnung und Export · " + template.Name, () =>
            {
                var settings = new ModelSettings { Shape = template.Shape, Resolution = 1 };
                LampShade.ApplyTemplate(settings);
                foreach (var mount in Enum.GetValues<ShadeMount>())
                {
                    settings.ShadeMount = mount;
                    settings.ShadeMountDiameter = 37.4;
                    var mesh = MeshGenerator.Generate(panorama, settings, false);
                    var report = mesh.Validate();
                    Require(report.IsValid && Math.Abs(report.Size.Y - settings.ShadeHeight) < .001, "Schirm ungültig oder Höhe falsch.");
                    var index = new CrossSectionIndex(mesh);
                    foreach (double y in new[] { .613, settings.ShadeHeight - .613 })
                    {
                        var cut = index.Slice(y);
                        Require(cut.IsRegular && cut.ClosedContours == 2, "Hohlraum bzw. Ringöffnung fehlt.");
                        bool expectBore = y < settings.ShadeHeight / 2 ? mount == ShadeMount.Bottom : mount == ShadeMount.Top;
                        var hole = cut.Paths.MinBy(path => path.Points.Max(p => p.LengthSquared()))!;
                        double clearance = hole.Points.Select((point, i) => DistanceToEdge(point, hole.Points[(i + 1) % hole.Points.Count])).Min() * 2;
                        if (expectBore) Require(Math.Abs(clearance - settings.ShadeMountDiameter) < .01, "Freie Ringöffnung weicht vom Eingabemaß ab.");
                        else Require(clearance > settings.ShadeMountDiameter + 5, "Die offene Seite ist verengt.");
                    }
                }

                LampShade.ApplyTemplate(settings);
                if (settings.ShadeMount == ShadeMount.Top) settings.PrintOrientation = PrintOrientation.UpsideDown;
                var result = MeshGenerator.Generate(panorama, settings, false);
                foreach (string extension in new[] { ".stl", ".3mf" })
                {
                    string file = Path.Combine(output, template.Shape + "-Schirm" + extension);
                    ModelExporter.Export(file, result);
                    var saved = (extension == ".stl" ? ModelExporter.ReadStl(file) : ModelExporter.Read3Mf(file)).Validate();
                    Require(saved.IsValid && saved.Triangles == result.Triangles.Count && Math.Abs(saved.Bounds.Min.Z) < .001,
                        "Das exportierte Modell ist ungültig oder liegt nicht auf dem Druckbett.");
                }
                string project = Path.Combine(output, template.Shape + "-Schirm.lithopro");
                ProjectStore.Save(project, new() { Model = settings }, [1, 2, 3]);
                var loaded = ProjectStore.Load(project).Data.Model;
                Require(loaded.Shape == settings.Shape && loaded.ShadeMount == settings.ShadeMount &&
                    loaded.ShadeBottomDiameter == settings.ShadeBottomDiameter && loaded.ShadeMountDiameter == settings.ShadeMountDiameter,
                    "Die gespeicherten Einstellungen für den Schirm fehlen.");
            });
        }

        Test("Lampenschirm: unpassende Ringöffnung und ungültige Maße", () =>
        {
            var model = new ModelSettings { Shape = Shape.CeilingShade, ShadeMount = ShadeMount.Top, ShadeMountDiameter = 150 };
            Throws<InvalidDataException>(() => MeshGenerator.Generate(panorama, model, false));
            model.ShadeMount = ShadeMount.Open;
            model.ShadeTopDiameter = double.NaN;
            Throws<InvalidDataException>(() => MeshGenerator.Generate(panorama, model, false));
        });
    }

    static double DistanceToEdge(Vector2 a, Vector2 b)
    {
        Vector2 edge = b - a;
        double squared = edge.LengthSquared();
        if (squared <= 1e-12) return a.Length();
        float t = Math.Clamp(-Vector2.Dot(a, edge) / (float)squared, 0, 1);
        return (a + edge * t).Length();
    }
}
