using System.IO;
using System.Numerics;
using LithoCreate.Core;

partial class Program
{
    private static void TestLightInserts()
    {
        Test("Lichteinsätze: sechs Vorgaben für alle 19 Formen und schlanke Varianten", () =>
        {
            foreach (var template in Templates.All)
            foreach (var preset in LightInserts.Presets)
            {
                var settings = new ModelSettings { Shape = template.Shape };
                if (settings.IsShade) LampShade.ApplyTemplate(settings);
                var (options, placement) = LightInserts.Suggest(settings, preset);
                Require(placement.Length > 0 && LightInserts.Generate(options).Validate().IsValid,
                    $"Einsatz für {template.Name} / {preset.Name} ist ungültig.");
                settings.Width = settings.Height = 15;
                settings.Diameter = 20;
                Require(LightInserts.Generate(LightInserts.Suggest(settings, preset).Options).Validate().IsValid,
                    $"Außenträger für kleine Form {template.Name} ist ungültig.");
            }
        });

        Test("Lichteinsätze: freies Fassungsmaß und freie Kabelausgänge in STL und 3MF", () =>
        {
            LightInsertOptions[] cases =
            [
                new(LightInsertKind.Socket, LightInsertOutline.Round, 80, 80, 28.5, 3, 8),
                new(LightInsertKind.Socket, LightInsertOutline.Rectangle, 90, 70, 40.5, 3, 8),
                new(LightInsertKind.LedStrip, LightInsertOutline.Rectangle, 14.5, 115, 10.5, 2, 6)
            ];
            string[] names = ["Lichteinsatz-E14-rund", "Lichteinsatz-E27-rechteckig", "Lichteinsatz-LED10-Kabel"];
            for (int i = 0; i < cases.Length; i++)
            {
                var options = cases[i];
                var mesh = LightInserts.Generate(options);
                Require(mesh.Validate().IsValid, "Einsatzmesh ist nicht geschlossen.");
                var index = new CrossSectionIndex(mesh);
                var cut = index.Slice(options.Thickness * .42);
                Require(cut.IsRegular, "Schnitt am Kabelausgang ist ungültig.");
                if (options.Kind == LightInsertKind.Socket)
                {
                    Require(cut.ClosedContours == 2, "Fassungsöffnung fehlt.");
                    var hole = cut.Paths.MinBy(path => path.Points.Max(point => point.LengthSquared()))!;
                    double clearance = hole.Points.Select((point, j) =>
                        DistanceToEdge(point, hole.Points[(j + 1) % hole.Points.Count])).Min() * 2;
                    Require(Math.Abs(clearance - options.Opening) < .01, "Freier Durchlass zu klein oder zu groß.");
                    var outer = cut.Paths.Single(path => path != hole);
                    float cableY = -(float)(options.Length / 2 - options.CableWidth * .32);
                    Require(!InsideLoop(new(0, cableY), outer) &&
                        !InsideLoop(new((float)options.CableWidth * .44f, cableY), outer) &&
                        InsideLoop(new((float)options.CableWidth, cableY), outer), "Seitlicher Kabelausschnitt fehlt.");
                }
                else
                {
                    Require(cut.ClosedContours == 1, "LED-Träger mit Ausschnitt ist nicht zusammenhängend.");
                    var outline = cut.Paths.Single();
                    Require(!InsideLoop(new(0, -.5f), outline) &&
                        !InsideLoop(new((float)options.CableWidth * .49f, -.5f), outline) &&
                        InsideLoop(new((float)options.CableWidth * .51f, -.5f), outline) &&
                        InsideLoop(new(0, -10), outline), "LED-Kabelausgang sitzt nicht am Ende der Schiene.");
                    var rails = index.Slice(options.Thickness + 1);
                    Require(rails.IsRegular && rails.ClosedContours == 2, "Führungskanten für LED-Band fehlen.");
                }
                CheckRoundtrip(mesh, names[i]);
            }
            Throws<InvalidDataException>(() => LightInserts.Generate(cases[1] with { Length = 44 }));
            Throws<InvalidDataException>(() => LightInserts.Generate(cases[2] with { CableWidth = 20 }));
            Require(LightInserts.Generate(cases[2] with { CableWidth = 10.5 }).Validate().IsValid,
                "Kabelausgang mit voller Schienenbreite ist ungültig.");
        });
    }

    private static bool InsideLoop(Vector2 point, SectionPath loop)
    {
        bool inside = false;
        for (int i = 0; i < loop.Points.Count; i++)
        {
            Vector2 a = loop.Points[i], b = loop.Points[(i + 1) % loop.Points.Count];
            if ((a.Y > point.Y) != (b.Y > point.Y) &&
                point.X < a.X + (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y))
                inside = !inside;
        }
        return inside;
    }
}
