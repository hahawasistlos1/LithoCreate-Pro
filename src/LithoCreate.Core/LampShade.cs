namespace LithoCreate.Core;

public enum ShadeMount { Open, Top, Bottom }
public enum ShadeMountStyle { Plate, Spokes }

public static class LampShade
{
    const double SpokeRingWidth = 4;
    const int SpokeCount = 3;

    public static bool Supports(Shape shape) =>
        shape is Shape.CeilingShade or Shape.FloorShade or Shape.BellShade or Shape.FacetedShade;

    public static void ApplyTemplate(ModelSettings settings)
    {
        (settings.ShadeTopDiameter, settings.ShadeBottomDiameter, settings.ShadeHeight, settings.ShadeMount) =
            settings.Shape switch
            {
                Shape.CeilingShade => (90, 200, 135, ShadeMount.Top),
                Shape.FloorShade => (185, 185, 185, ShadeMount.Bottom),
                Shape.BellShade => (86, 205, 130, ShadeMount.Top),
                Shape.FacetedShade => (115, 190, 145, ShadeMount.Open),
                _ => throw new ArgumentException("Diese Vorlage ist kein Lampenschirm.")
            };
        settings.ShadeMountDiameter = WorkshopParts.E27Opening;
        settings.ShadeMountThickness = 3;
        settings.ShadeMountStyle = ShadeMountStyle.Spokes;
        settings.ShadeSides = 6;
    }

    internal static void Validate(ModelSettings settings)
    {
        double top = settings.ShadeTopDiameter;
        double bottom = settings.ShadeBottomDiameter;
        double height = settings.ShadeHeight;
        if (!double.IsFinite(top) || !double.IsFinite(bottom) || !double.IsFinite(height) ||
            top is < 20 or > 1000 || bottom is < 20 or > 1000 || height is < 10 or > 1000)
            throw new InvalidDataException("Schirmdurchmesser: 20–1000 mm. Schirmhöhe: 10–1000 mm.");

        if (!Enum.IsDefined(settings.ShadeMount))
            throw new InvalidDataException("Unbekannte Position für den Montagering.");
        if (!Enum.IsDefined(settings.ShadeMountStyle))
            throw new InvalidDataException("Unbekannte Form für den Montagering.");
        if (settings.IsFacetedShade && settings.ShadeSides is < 3 or > 16)
            throw new InvalidDataException("Facettenschirm: 3 bis 16 Seiten eingeben.");

        double apothemFactor = ApothemFactor(settings);
        if (Math.Min(top, bottom) * apothemFactor / 2 - settings.MaxThickness < 2)
            throw new InvalidDataException("Der schmale Schirmrand benötigt mehr Durchmesser oder eine kleinere Wandstärke.");

        double ringHeight = settings.ShadeMountThickness;
        double hole = settings.ShadeMountDiameter;
        if (!double.IsFinite(ringHeight) || !double.IsFinite(hole))
            throw new InvalidDataException("Die Maße des Montagerings müssen endliche Zahlen sein.");
        if (settings.ShadeMount == ShadeMount.Open) return;

        if (ringHeight < 1 || ringHeight > Math.Min(15, height / 3))
            throw new InvalidDataException("Ringstärke: 1–15 mm und höchstens ein Drittel der Schirmhöhe.");

        bool topMount = settings.ShadeMount == ShadeMount.Top;
        double rim = topMount ? top : bottom;
        double ringInsideY = topMount ? height - ringHeight : ringHeight;
        double innerDiameter = Math.Min(rim, DiameterAt(settings, ringInsideY / height)) * apothemFactor - 2 * settings.MaxThickness;
        double inset = settings.ShadeMountStyle == ShadeMountStyle.Spokes ? 2 * (SpokeRingWidth + 2) : 3;
        double maxHole = (innerDiameter - inset) * Math.Cos(Math.PI / 32);
        if (maxHole < 4 && settings.ShadeMountStyle == ShadeMountStyle.Spokes)
            throw new InvalidDataException("Für den Strebenring den Schirmdurchmesser am Montagerand vergrößern.");
        if (hole < 4 || hole > maxHole)
            throw new InvalidDataException($"Freie Ringöffnung: mindestens 4 mm und bei diesen Schirmmaßen höchstens {Math.Floor(maxHole * 10) / 10:0.0} mm. Falls nötig, den Schirmdurchmesser am Montagerand vergrößern.");
    }

    internal static Mesh Generate(GrayImage photo, ModelSettings settings, bool preview, CancellationToken token, IProgress<double>? progress)
    {
        var mesh = new Mesh { WrapTextureU = true };
        double height = settings.ShadeHeight;
        double ringHeight = settings.ShadeMountThickness;
        double outerWidth = Math.PI * Math.Max(settings.ShadeTopDiameter, settings.ShadeBottomDiameter);
        double radiusDifference = (settings.ShadeTopDiameter - settings.ShadeBottomDiameter) / 2;
        double slopeFactor = settings.Shape == Shape.BellShade ? 2 : 1;
        double wallLengthBound = Math.Sqrt(height * height + Math.Pow(radiusDifference * slopeFactor, 2));
        int budget = preview ? 22000 : (int)Math.Min(1200000, (long)settings.MemoryMb * 1024 * 1024 / 600);
        double raster = Math.Max(settings.Resolution, Math.Sqrt(outerWidth * wallLengthBound / budget));
        bool spokes = settings.ShadeMount != ShadeMount.Open && settings.ShadeMountStyle == ShadeMountStyle.Spokes;
        int alignment = settings.IsFacetedShade ? settings.ShadeSides * 4 : 4;
        if (spokes && alignment % SpokeCount != 0) alignment *= SpokeCount;
        int columns = Math.Clamp((int)Math.Ceiling(outerWidth / raster / alignment), (32 + alignment - 1) / alignment, 2400 / alignment) * alignment;
        int rows = Math.Clamp((int)Math.Ceiling(wallLengthBound / raster), 4, Math.Min(2400, budget / columns));
        mesh.ActualResolution = Math.Max(outerWidth / columns, wallLengthBound / rows);

        double innerBottom = settings.ShadeMount == ShadeMount.Bottom ? ringHeight : 0;
        double innerTop = settings.ShadeMount == ShadeMount.Top ? height - ringHeight : height;
        var levels = new List<double> { 0, height };
        if (innerBottom > 0) levels.Add(innerBottom);
        if (innerTop < height) levels.Add(innerTop);
        for (int row = 1; row < rows; row++)
        {
            double y = height * row / rows;
            // Keep the wall linear through the rim band alongside the spoke cut faces.
            if (spokes && (y < innerBottom || y > innerTop)) continue;
            if (Math.Abs(y - innerBottom) > .0001 && Math.Abs(y - innerTop) > .0001) levels.Add(y);
        }
        levels.Sort();
        double apothemFactor = ApothemFactor(settings);
        var directions = new (double X, double Z, double FaceCosine)[columns];
        for (int i = 0; i < columns; i++)
        {
            double u = i / (double)columns;
            var (sin, cos) = Math.SinCos((u + .5) * Math.Tau);
            double sector = u * settings.ShadeSides;
            double faceCosine = settings.IsFacetedShade
                ? Math.Cos((sector - Math.Floor(sector) - .5) * Math.Tau / settings.ShadeSides)
                : 1;
            directions[i] = (sin, cos, faceCosine);
        }

        var outer = BuildWall(0, levels.Count - 1, true);
        var inner = BuildWall(levels.IndexOf(innerBottom), levels.IndexOf(innerTop), false);
        CloseRim(outer.First, inner.First, 0, innerBottom, false);
        CloseRim(outer.Last, inner.Last, height, innerTop, true);
        token.ThrowIfCancellationRequested();
        mesh.OrientOutward();
        return mesh;

        (int First, int Last) BuildWall(int start, int end, bool outside)
        {
            int first = mesh.Vertices.Count;
            for (int row = start; row <= end; row++)
            {
                token.ThrowIfCancellationRequested();
                progress?.Report((outside ? 0 : .5) + .48 * (row - start) / (end - start));
                // Identical sample heights keep the two curved walls nested even with a flange.
                double y = levels[row];
                double v = 1 - y / height;
                double apothem = DiameterAt(settings, y / height) * apothemFactor / 2;
                double margin = Math.Min(2, height / 10);
                double rimWeight = Math.Clamp(Math.Min(y, height - y) / margin, 0, 1);
                for (int col = 0; col < columns; col++)
                {
                    double u = col / (double)columns;
                    double thickness = 0;
                    if (outside)
                    {
                        double gray = photo.Sample(u, v);
                        // Blend the two photo edges on the back of the shade.
                        double edgeWeight = Math.Clamp(Math.Min(u, 1 - u) / .04, 0, 1);
                        if (edgeWeight < 1) gray = gray * edgeWeight + (photo.Sample(0, v) + photo.Sample(1, v)) / 2 * (1 - edgeWeight);
                        if (settings.Negative) gray = 1 - gray;
                        thickness = settings.MaxThickness - gray * rimWeight * (settings.MaxThickness - settings.MinThickness);
                    }
                    Vertex(col, (apothem - settings.MaxThickness + thickness) / directions[col].FaceCosine, y, v,
                        outside && row > start && row < end);
                }
                if (row > start) Join(mesh.Vertices.Count - 2 * columns, mesh.Vertices.Count - columns, outside);
            }
            return (first, mesh.Vertices.Count - columns);
        }

        void Vertex(int col, double radius, double y, double v, bool isPhoto)
        {
            var direction = directions[col];
            mesh.Vertices.Add(new((float)(radius * direction.X), (float)y, (float)(radius * direction.Z)));
            mesh.TextureCoordinates.Add(new((float)col / columns, (float)v));
            mesh.PhotoVertices.Add(isPhoto);
        }

        void Join(int a, int b, bool forward)
        {
            for (int col = 0; col < columns; col++) JoinSector(a, b, col, forward);
        }

        void JoinSector(int a, int b, int col, bool forward)
        {
            int next = (col + 1) % columns;
            if (forward) mesh.Quad(a + col, a + next, b + next, b + col);
            else mesh.Quad(a + col, b + col, b + next, a + next);
        }

        void CloseRim(int outside, int inside, double outerY, double innerY, bool top)
        {
            if (outerY == innerY)
            {
                Join(outside, inside, top);
                return;
            }
            // Circumscribe the measured opening so flat facets do not reduce its clearance.
            double radius = settings.ShadeMountDiameter / (2 * Math.Cos(Math.PI / columns));
            int rim = mesh.Vertices.Count;
            for (int col = 0; col < columns; col++) Vertex(col, radius, outerY, 0, false);
            int bore = mesh.Vertices.Count;
            for (int col = 0; col < columns; col++) Vertex(col, radius, innerY, 0, false);
            if (spokes)
            {
                CloseWithSpokes(outside, inside, outerY, innerY, top, rim, bore, radius);
                return;
            }
            Join(outside, rim, top);
            Join(rim, bore, top);
            Join(bore, inside, top);
        }

        void CloseWithSpokes(int outside, int inside, double outerY, double innerY, bool top,
            int lowerBore, int upperBore, double boreRadius)
        {
            double apothem = DiameterAt(settings, outerY / height) * apothemFactor / 2;
            int wallRim = mesh.Vertices.Count;
            for (int col = 0; col < columns; col++)
                Vertex(col, (apothem - settings.MaxThickness) / directions[col].FaceCosine, outerY, 0, false);
            double ringRadius = boreRadius + SpokeRingWidth;
            int outerRing = mesh.Vertices.Count;
            for (int col = 0; col < columns; col++) Vertex(col, ringRadius, outerY, 0, false);
            int innerRing = mesh.Vertices.Count;
            for (int col = 0; col < columns; col++) Vertex(col, ringRadius, innerY, 0, false);

            // Both faces of the center ring remain continuous. Only the three
            // narrow sectors join the ring to the lip of the shade.
            Join(outside, wallRim, top);
            Join(outerRing, lowerBore, top);
            Join(lowerBore, upperBore, top);
            Join(upperBore, innerRing, top);
            int period = columns / SpokeCount;
            int halfWidth = Math.Max(1, (int)Math.Round(period * 16.0 / 240));
            int first = period / 2 - halfWidth;
            int last = period / 2 + halfWidth;
            for (int col = 0; col < columns; col++)
            {
                int sector = col % period;
                if (sector >= first && sector < last)
                {
                    JoinSector(wallRim, outerRing, col, top);
                    JoinSector(innerRing, inside, col, top);
                }
                else
                {
                    JoinSector(wallRim, inside, col, top);
                    JoinSector(outerRing, innerRing, col, !top);
                }

                // Close the cut faces between each spoke and its adjacent gaps.
                if (sector == first || sector == last)
                {
                    int a = outerRing + col, b = wallRim + col, c = inside + col, d = innerRing + col;
                    if ((sector == first) != top) mesh.Quad(a, b, c, d);
                    else mesh.Quad(a, d, c, b);
                }
            }
        }
    }

    static double ApothemFactor(ModelSettings settings) => settings.IsFacetedShade ? Math.Cos(Math.PI / settings.ShadeSides) : 1;

    static double DiameterAt(ModelSettings settings, double up)
    {
        double fromTop = 1 - up;
        if (settings.Shape == Shape.BellShade) fromTop *= fromTop;
        return settings.ShadeTopDiameter + (settings.ShadeBottomDiameter - settings.ShadeTopDiameter) * fromTop;
    }
}
