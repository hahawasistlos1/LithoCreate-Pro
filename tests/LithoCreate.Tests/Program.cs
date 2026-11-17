using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LithoCreate.Core;
using LithoCreate.App.Services;

partial class Program
{
    static int passed,failed;
    static string output="";
    [STAThread] static int Main(string[] args)
    {
        SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext());
        if(args.FirstOrDefault()=="--endurance")return Endurance(args[1],double.Parse(args[2],System.Globalization.CultureInfo.InvariantCulture));
        if(args.FirstOrDefault()=="--dpi")return RenderDpi(args[1]);
        if(args.FirstOrDefault()=="--thumbnails")return RenderThumbnails(args[1]);
        if(args.FirstOrDefault()=="--ui")return RenderInterface(args[1]);
        if(args.FirstOrDefault()=="--kit"){CalibrationKit.Export(args[1]);return 0;}
        if(args.FirstOrDefault()=="--examples")return CreateExamples(args[1]);
        output=Path.GetFullPath(args.FirstOrDefault()=="--extensions"?args[1]:args.FirstOrDefault()??"artifacts/tests");Directory.CreateDirectory(output);
        if(args.FirstOrDefault()=="--extensions"){output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);TestExtensions();return failed==0?0:1;}
        if(args.FirstOrDefault()=="--shades"){output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);TestLampShades();return failed==0?0:1;}
        if(args.FirstOrDefault()=="--new-designs"){output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);TestNewDesigns();return failed==0?0:1;}
        var clock=Stopwatch.StartNew();var gray=new GrayImage(32,24,Enumerable.Range(0,768).Select(i=>(byte)((i%32)*255/31)).ToArray());
        foreach(var shape in SingleBodyShapes())
        {
            Test("Topologie und Datei-Roundtrip: "+shape,()=>
            {
                var s=new ModelSettings{Shape=shape,Width=48,Height=36,Diameter=40,Radius=40,Resolution=1,FrameWidth=2};var mesh=MeshGenerator.Generate(gray,s,false);var report=mesh.Validate();
                Require(report.IsValid,$"{report}");
                foreach(string ext in new[]{"stl","3mf"})
                {
                    string file=Path.Combine(output,shape+"."+ext);ModelExporter.Export(file,mesh);
                    var roundtrip=ext=="stl"?ModelExporter.ReadStl(file):ModelExporter.Read3Mf(file);
                    var check=roundtrip.Validate();Require(check.IsValid,"Datei ungültig: "+check);Require(check.Triangles==report.Triangles,"Dreieckzahl verändert");Require(Vector3.Distance(check.Size,new(report.Size.X,report.Size.Z,report.Size.Y))<.01,"Maße verändert");Require(Math.Abs(check.Bounds.Min.Z)<.001,"Modell steht nicht auf Z=0");
                }
            });
        }
        Test("Helligkeit entspricht physischer Wandstärke",()=>
        {
            foreach(byte pixel in new byte[]{0,255})foreach(bool negative in new[]{false,true})
            {
                var s=new ModelSettings{FrameWidth=0,Negative=negative,Width=40,Height=30,Resolution=1};var m=MeshGenerator.Generate(new(1,1,[pixel]),s,false);var b=m.Bounds();double expected=(pixel==0)^negative?s.MaxThickness:s.MinThickness;Require(Math.Abs(b.Max.Z-expected)<.00001,"Falsche Materialstärke");Require(Math.Abs(m.Validate().Volume-s.Width*s.Height*expected)<.1,"Falsches Volumen");
            }
        });
        Test("Exakte Außenmaße flacher Modelle",()=>{var m=MeshGenerator.Generate(gray,new(){Width=123,Height=87,FrameWidth=0},false);var size=m.Validate().Size;Require(Math.Abs(size.X-123)<.001&&Math.Abs(size.Y-87)<.001,"Maße falsch");});
        Test("Exakte Konturmaße von Herz, Stern und Oval",()=>{foreach(var shape in new[]{Shape.Heart,Shape.Star,Shape.Oval,Shape.Drop}){var m=MeshGenerator.Generate(gray,new(){Shape=shape,Width=83,Height=67,Resolution=1},false);var report=m.Validate();Require(report.IsValid,"Kontur ungültig");Require(Math.Abs(report.Size.X-83)<.001&&Math.Abs(report.Size.Y-67)<.001,"Konturmaße falsch");}});
        Test("Zylinder mit nahtfreiem Mantel",()=>{var m=MeshGenerator.Generate(gray,new(){Shape=Shape.Cylinder,Diameter=60,Height=70,Resolution=.8},false);Require(m.Validate().IsValid,"Zylinder nicht geschlossen");});
        foreach(var shape in new[]{Shape.Cylinder,Shape.Polygon})foreach(double hole in new[]{0.0,6.0})
        {
            var testedShape=shape;double diameter=hole;
            Test($"Lampenboden und Kabelöffnung: {shape}, {hole} mm",()=>
            {
                var settings=new ModelSettings{Shape=testedShape,Width=50,Height=40,Diameter=50,Resolution=.8,LampBaseThickness=3,LampBaseHoleDiameter=diameter};
                var mesh=MeshGenerator.Generate(gray,settings,false);CheckRoundtrip(mesh,$"Base-{testedShape}-{diameter}");
                Require(Euler(mesh)==(diameter==0?2:0),"Falsche Topologie des Lampenbodens");
                Require(VerticalIntersections(mesh,.37,.23)==(diameter==0?2:0),"Die Bodenöffnung ist verschlossen oder der Boden fehlt.");
                Require(VerticalIntersections(mesh,8.27,.37)==2,"Bodenfläche neben der Öffnung fehlt.");
                if(diameter>0)Require(Math.Abs(MinimumClearRadius(mesh,0)-diameter/2)<.001,"Die freien Sehnen der Bodenöffnung unterschreiten den gewählten Durchmesser.");
                Require(Math.Abs(mesh.Bounds().Min.Y)<.0001&&Math.Abs(mesh.Bounds().Max.Y-40)<.0001,"Lampenmaße verändert");
            });
        }
        foreach(var shape in new[]{Shape.Flat,Shape.Frame,Shape.Curved,Shape.Nightlight})
        {
            var testedShape=shape;
            Test("Integrierter Standfuß ohne T-Verbindungen: "+shape,()=>
            {
                var settings=new ModelSettings{Shape=testedShape,Width=48,Height=35,Radius=45,FrameWidth=0,Resolution=1,StandEnabled=true,StandHeight=6,StandDepth=22};
                var mesh=MeshGenerator.Generate(new(1,1,[255]),settings,false);CheckRoundtrip(mesh,"Stand-"+testedShape);Require(Euler(mesh)==2,"Standfuß ist nicht ein geschlossener Körper.");
                Require(Math.Abs(mesh.Bounds().Max.Y-41)<.0001,"Foto- und Standhöhe stimmen nicht.");
                if(testedShape is Shape.Flat or Shape.Frame){double expected=48*(6*22+35*.8);Require(Math.Abs(mesh.Validate().Volume-expected)<.1,"Standfuß hat Überlappungen oder falsches Volumen.");Require(Math.Abs(mesh.Validate().Size.Z-22)<.001,"Standtiefe stimmt nicht.");}
            });
        }
        foreach(var shape in new[]{Shape.Sphere,Shape.Moon})foreach(var holes in new[]{(Lower:30.0,Upper:0.0),(Lower:30.0,Upper:4.0),(Lower:0.0,Upper:4.0)})
        {
            var testedShape=shape;var openings=holes;
            Test($"Kugellampe mit Öffnungen: {shape}, {holes.Lower}/{holes.Upper} mm",()=>
            {
                var settings=new ModelSettings{Shape=testedShape,Diameter=65,Resolution=1,SphereOpeningDiameter=openings.Lower,SphereVentDiameter=openings.Upper,SphereCollarHeight=4};
                var mesh=MeshGenerator.Generate(gray,settings,false);CheckRoundtrip(mesh,$"Opening-{testedShape}-{openings.Lower}-{openings.Upper}");
                Require(Euler(mesh)==(openings.Lower>0&&openings.Upper>0?0:2),"Öffnungen verbinden Innenraum und Außenraum nicht korrekt.");
                if(openings.Lower>0&&openings.Upper>0)Require(VerticalIntersections(mesh,.31,.17)==0,"Durchgehender Entlüftungskanal ist verschlossen.");
                if(openings.Lower>0)
                {
                    double y=mesh.Bounds().Min.Y;
                    var rim=mesh.Vertices.Where(v=>Math.Abs(v.Y-y)<1e-5).ToArray();
                    Require(rim.Length>20,"Flache Standfläche am Kragen fehlt.");
                    Require(Math.Abs(MinimumClearRadius(mesh,y)-openings.Lower/2)<.001,"Unterer freier Öffnungsdurchmesser stimmt nicht.");
                }
            });
        }
        Test("Grenzmaße und Formwechsel mit gespeicherten Zusatzoptionen",()=>
        {
            Throws<InvalidDataException>(()=>MeshGenerator.Generate(gray,new(){Shape=Shape.Polygon,Diameter=12,MaxThickness=3,Sides=3},false));
            Throws<InvalidDataException>(()=>MeshGenerator.Generate(gray,new(){Shape=Shape.Cylinder,LampBaseThickness=.1},false));
            Throws<InvalidDataException>(()=>MeshGenerator.Generate(gray,new(){Shape=Shape.Cylinder,Diameter=30,LampBaseThickness=2,LampBaseHoleDiameter=25},false));
            Throws<InvalidDataException>(()=>MeshGenerator.Generate(gray,new(){Shape=Shape.Flat,StandEnabled=true,StandDepth=3},false));
            Throws<InvalidDataException>(()=>MeshGenerator.Generate(gray,new(){Shape=Shape.Sphere,SphereOpeningDiameter=99},false));
            Throws<InvalidDataException>(()=>MeshGenerator.Generate(gray,new(){Shape=Shape.Sphere,SphereVentDiameter=1},false));
            Require(MeshGenerator.Generate(gray,new(){Shape=Shape.Sphere,StandEnabled=true,LampBaseThickness=3,SphereOpeningDiameter=25,Resolution=2},false).Validate().IsValid,"Nicht anwendbare Optionen verhindern den Formwechsel.");
        });
        Test("Kugelkragen ohne Höhe bleibt geschlossen und der Übergang erhält ausreichende Materialstärke",()=>
        {
            foreach(var shape in new[]{Shape.Sphere,Shape.Moon})foreach(double collar in new[]{0.0,4.0})
            {
                var settings=new ModelSettings{Shape=shape,Diameter=65,MinThickness=.8,MaxThickness=3,Resolution=.8,SphereOpeningDiameter=30,SphereVentDiameter=4,SphereCollarHeight=collar};
                var mesh=MeshGenerator.Generate(new(1,1,[255]),settings,false);CheckRoundtrip(mesh,$"Reinforced-{shape}-{collar}");Require(Euler(mesh)==0,"Kragen ist nicht topologisch verbunden.");
                double cut=mesh.Bounds().Min.Y+collar;
                var atRim=mesh.Vertices.Where(v=>Math.Abs(v.Y-cut)<.0001).Select(v=>Math.Sqrt(v.X*v.X+v.Z*v.Z)).ToArray();
                Require(atRim.Length>20&&atRim.Max()-atRim.Min()>=settings.MaxThickness,"Übergang zwischen Kugel und Kragen erzeugt einen zu dünnen Hals.");
            }
        });
        Test("Abbruch von Berechnung und Export",()=>
        {
            using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>MeshGenerator.Generate(gray,new(),false,cts.Token));
            var mesh=MeshGenerator.Generate(gray,new(){Resolution=1},false);string path=Path.Combine(output,"cancel.stl");File.WriteAllText(path,"original");Throws<OperationCanceledException>(()=>ModelExporter.Export(path,mesh,cts.Token));Require(File.ReadAllText(path)=="original"&&!File.Exists(path+".tmp"),"Abbruch zerstört Datei");
        });
        Test("Adaptive Auflösung respektiert Geometriebudget",()=>{var m=MeshGenerator.Generate(gray,new(){Width=800,Height=600,Resolution=.05,MemoryMb=64,FrameWidth=0},false);Require(m.Triangles.Count<500000,"Budget überschritten");Require(m.ActualResolution>.05,"Raster nicht angepasst");Require(m.Validate().IsValid,"Budgetmodell ungültig");});
        Test("2048-MB-Einstellung verringert die Detailauflösung nicht",()=>{var baseline=MeshGenerator.Generate(gray,new(){Width=50,Height=35,Resolution=1,MemoryMb=256},false);var larger=MeshGenerator.Generate(gray,new(){Width=50,Height=35,Resolution=1,MemoryMb=2048},false);Require(larger.Triangles.Count==baseline.Triangles.Count,"Speicherbudget läuft bei 2048 MB über.");});
        Test("Ungültige Einstellungen und defekte Dateien",()=>
        {
            Throws<InvalidDataException>(()=>MeshGenerator.Generate(gray,new(){Width=double.NaN},false));Throws<InvalidDataException>(()=>MeshGenerator.Generate(gray,new(){MinThickness=4,MaxThickness=2},false));
            string path=Path.Combine(output,"broken.lithopro");File.WriteAllText(path,"kaputt");Throws<InvalidDataException>(()=>ProjectStore.Load(path));
            Throws<FileNotFoundException>(()=>ProjectStore.Load(Path.Combine(output,"missing.lithopro")));
        });
        Test("Projekt erhält Originalbild und alle Korrekturen",()=>
        {
            var data=new ProjectData{Name="Prüfung",Edits=new(){Brightness=.1,Mirror=true,Rotation=90,StraightenAngle=12.5,AutoOrient=false,CropX=.1,CropWidth=.8},Model=new(){Shape=Shape.Heart,Width=80,StandEnabled=true,StandHeight=7,StandDepth=28,LampBaseThickness=3,LampBaseHoleDiameter=6,SphereOpeningDiameter=30,SphereCollarHeight=4,SphereVentDiameter=3},Printer=new(){Name="Eigenes Profil",Layer=.08}};
            byte[] photo=ImageProcessor.Png(ImageProcessor.Demo());string file=Path.Combine(output,"example.lithopro");ProjectStore.Save(file,data,photo,photo);var loaded=ProjectStore.Load(file);Require(JsonSerializer.Serialize(loaded.Data)==JsonSerializer.Serialize(data),"Projektinhalt verändert");Require(loaded.Image.SequenceEqual(photo),"Originalbild verändert");
            using var zip=ZipFile.OpenRead(file);Require(zip.GetEntry("preview.png")!=null,"Vorschaubild fehlt");
        });
        Test("Versionen und Null-Metadaten werden abgewiesen",()=>
        {
            foreach(string json in new[]{"{\"Version\":999}","{\"Version\":1,\"Model\":null}"}){string file=Path.Combine(output,"version.lithopro");if(File.Exists(file))File.Delete(file);using(var zip=ZipFile.Open(file,ZipArchiveMode.Create)){using var w=new StreamWriter(zip.CreateEntry("project.json").Open());w.Write(json);}Throws<InvalidDataException>(()=>ProjectStore.Load(file));}
        });
        Test("Projektversion 1 wird ohne neue Bildtransformationen auf Version 12 übernommen",()=>
        {
            string file=Path.Combine(output,"legacy.lithopro");if(File.Exists(file))File.Delete(file);
            using(var zip=ZipFile.Open(file,ZipArchiveMode.Create)){using(var w=new StreamWriter(zip.CreateEntry("project.json").Open()))w.Write("{\"Version\":1,\"Name\":\"Altes Projekt\",\"Model\":{\"Width\":123},\"Edits\":{\"Brightness\":0.15}}");using var source=zip.CreateEntry("source.bin").Open();source.Write(ImageProcessor.Png(ImageProcessor.Demo()));}
            var loaded=ProjectStore.Load(file);Require(loaded.Data.Version==15&&loaded.Data.Model.Width==123&&loaded.Data.Edits.Brightness==.15,"Migration verändert alte Einstellungen.");Require(!loaded.Data.Edits.AutoOrient&&loaded.Data.Edits.StraightenAngle==0,"Alte Projekte erhalten neue Bildtransformationen.");Require(!loaded.Data.Model.StandEnabled&&loaded.Data.Model.LampBaseThickness==0&&loaded.Data.Model.SphereOpeningDiameter==0,"Alte Projekte erhalten ungewollte Zusatzgeometrie.");
        });
        Test("Beschädigte Bild- und Druckerparameter werden vor Projektübernahme abgewiesen",()=>
        {
            foreach(string json in new[]{"{\"Version\":2,\"Edits\":{\"Gamma\":0}}","{\"Version\":2,\"Edits\":{\"CropX\":1,\"CropWidth\":0.1}}","{\"Version\":2,\"Printer\":{\"BuildZ\":-1}}","{\"Version\":2,\"Model\":{\"Shape\":999}}"})
            {
                string path=Path.Combine(output,"invalid-parameters.lithopro");if(File.Exists(path))File.Delete(path);using(var zip=ZipFile.Open(path,ZipArchiveMode.Create)){using var writer=new StreamWriter(zip.CreateEntry("project.json").Open());writer.Write(json);}Throws<InvalidDataException>(()=>ProjectStore.Load(path));
            }
        });
        Test("Exklusive Wiederherstellungsdaten und Freigabe nach Instanzende",()=>
        {
            string dir=Path.Combine(output,"instance-lock");using(var first=InstanceLock.TryAcquire(dir))
            {
                Require(first!=null,"Erste Instanz wurde gesperrt.");Require(Task.Run(()=>{using var second=InstanceLock.TryAcquire(dir);return second==null;}).GetAwaiter().GetResult(),"Zweite Instanz kann die gleiche Wiederherstellung überschreiben.");
                using var separate=InstanceLock.TryAcquire(Path.Combine(output,"other-instance"));Require(separate!=null,"Isolierte Datenordner werden fälschlich gesperrt.");
            }
            using var reopened=InstanceLock.TryAcquire(dir);Require(reopened!=null,"Sperrdatei verhindert den nächsten Start.");
        });
        Test("Bildformate JPG, PNG, BMP, TIFF",()=>
        {
            foreach(BitmapEncoder encoder in new BitmapEncoder[]{new JpegBitmapEncoder(),new PngBitmapEncoder(),new BmpBitmapEncoder(),new TiffBitmapEncoder()}){encoder.Frames.Add(BitmapFrame.Create(ImageProcessor.Demo()));using var ms=new MemoryStream();encoder.Save(ms);var decoded=ImageProcessor.Decode(ms.ToArray(),600);Require(decoded.PixelWidth==600,"Import falsche Größe");}
        });
        Test("Nichtdestruktive Bildbearbeitung / Rotation / Zuschnitt",()=>
        {
            var source=ImageProcessor.Demo();var bytes=ImageProcessor.Png(source);var a=ImageProcessor.Process(source,new());var b=ImageProcessor.Process(source,new(){Brightness=.2,Contrast=1.2,Gamma=1.3,Denoise=.4,Sharpness=.5});Require(!a.Gray.Pixels.SequenceEqual(b.Gray.Pixels),"Korrekturen ohne Effekt");Require(bytes.SequenceEqual(ImageProcessor.Png(source)),"Original verändert");var crop=ImageProcessor.Process(source,new(){CropWidth=.5,Rotation=90});Require(crop.Gray.Height==source.PixelWidth/2,"Rotation oder Zuschnitt falsch");Throws<InvalidDataException>(()=>ImageProcessor.Process(source,new(){CropWidth=2}));
        });
        byte[][] orientedPixels=[[10,50,90,140,180,220],[90,50,10,220,180,140],[220,180,140,90,50,10],[140,180,220,10,50,90],[10,140,50,180,90,220],[140,10,180,50,220,90],[220,90,180,50,140,10],[90,220,50,180,10,140]];
        for(int orientation=1;orientation<=8;orientation++)
        {
            int expectedOrientation=orientation;Test("EXIF-Ausrichtung mit Spiegelung: "+orientation,()=>
            {
                var fixture=GrayBitmap(3,2,[10,50,90,140,180,220]);byte[] encoded=ExifFixture(fixture,expectedOrientation);var decoded=ImageProcessor.Decode(encoded);
                Require(decoded.PixelWidth==(expectedOrientation>=5?2:3)&&decoded.PixelHeight==(expectedOrientation>=5?3:2),"Ausrichtungsmaße falsch.");
                Require(ImageProcessor.Process(decoded,new()).Gray.Pixels.Zip(orientedPixels[expectedOrientation-1]).All(pair=>Math.Abs(pair.First-pair.Second)<=1),"EXIF-Rotation oder Spiegelrichtung ist falsch.");
                Require(ImageProcessor.Decode(encoded,autoOrient:false).PixelWidth==3,"Legacy-Bild wird ungefragt gedreht.");File.WriteAllBytes(Path.Combine(output,$"Exif-{expectedOrientation}.tiff"),encoded);
            });
        }
        Test("JPEG-EXIF und unbekannte Orientierung",()=>
        {
            var fixture=GrayBitmap(30,20,Enumerable.Range(0,600).Select(i=>(byte)(i%30<15?30:220)).ToArray());
            byte[] jpeg=ExifFixture(fixture,6,true);var decoded=ImageProcessor.Decode(jpeg);Require(decoded.PixelWidth==20&&decoded.PixelHeight==30,"JPEG-EXIF wird nicht berücksichtigt.");var grayJpeg=ImageProcessor.Process(decoded,new()).Gray;Require(grayJpeg.Pixels[5*20+10]<50&&grayJpeg.Pixels[25*20+10]>200,"JPEG wird in die falsche Richtung gedreht.");
            Require(ImageProcessor.Decode(ExifFixture(fixture,9)).PixelWidth==30,"Unbekannte Orientierung verhindert oder dreht den Import.");
        });
        Test("Feinrotation interpoliert korrekt und lässt das Original unverändert",()=>
        {
            const int w=20,h=16;var source=GrayBitmap(w,h,Enumerable.Range(0,w*h).Select(i=>(byte)(8*(i%w)+4*(i/w))).ToArray());byte[] original=ImageProcessor.Png(source);const double degrees=13;
            var result=ImageProcessor.Process(source,new(){StraightenAngle=degrees});Require(result.Gray.Width==w&&result.Gray.Height==h,"Seitenverhältnis verändert.");double angle=degrees*Math.PI/180,c=Math.Cos(angle),s=Math.Sin(angle),zoom=Math.Max((c*w+s*h)/w,(s*w+c*h)/h);
            foreach(var p in new[]{(X:3,Y:5),(X:15,Y:4),(X:9,Y:12)}){double dx=(p.X-(w-1)/2.0)/zoom,dy=(p.Y-(h-1)/2.0)/zoom,sx=(w-1)/2.0+c*dx+s*dy,sy=(h-1)/2.0-s*dx+c*dy;Require(Math.Abs(result.Gray.Pixels[p.Y*w+p.X]-(8*sx+4*sy))<1.2,"Bilineare Rückabbildung falsch.");}
            Require(original.SequenceEqual(ImageProcessor.Png(source)),"Originalbild wurde verändert.");
        });
        Test("Feinrotation ohne leere Ecken bei extremen Seitenverhältnissen",()=>
        {
            foreach(var size in new[]{(W:60,H:9),(W:9,H:60),(W:1,H:40)})foreach(double angle in new[]{-45.0,-12.3,12.3,45.0}){var result=ImageProcessor.Process(GrayBitmap(size.W,size.H,Enumerable.Repeat((byte)66,size.W*size.H).ToArray()),new(){StraightenAngle=angle});Require(result.Gray.Pixels.All(p=>p is >=65 and <=66),"Weiße oder schwarze Eckkeile entstanden.");}
            foreach(double angle in new[]{double.NaN,double.PositiveInfinity,45.01,-45.01})Throws<InvalidDataException>(()=>new ImageEdits{StraightenAngle=angle}.Validate());
        });
        Test("Vergleichsbild erhält Geometrie, aber keine Tonwertkorrekturen",()=>
        {
            var source=ImageProcessor.Demo();var geometry=new ImageEdits{CropX=.1,CropWidth=.7,Rotation=90,Mirror=true,StraightenAngle=-8.2};var corrected=geometry.Clone();corrected.Brightness=.2;corrected.Contrast=1.4;corrected.Invert=true;
            var before=ImageProcessor.Process(source,geometry);var after=ImageProcessor.Process(source,corrected);Require(ImageProcessor.Png(before.BeforeBitmap).SequenceEqual(ImageProcessor.Png(after.BeforeBitmap)),"Vergleichsseiten verwenden unterschiedliche Geometrie.");Require(!before.Gray.Pixels.SequenceEqual(after.Gray.Pixels),"Tonwerte bleiben unverändert.");Require(after.BeforeBitmap.PixelWidth==after.Bitmap.PixelWidth&&after.BeforeBitmap.PixelHeight==after.Bitmap.PixelHeight,"Vergleichsseiten verschoben.");
        });
        Test("Vorher-Nachher-Trenner rendert beide Seiten und bleibt begrenzt",()=>
        {
            var view=new LithoCreate.App.Controls.PhotoComparison{Before=GrayBitmap(400,300,new byte[400*300]),After=GrayBitmap(400,300,Enumerable.Repeat((byte)255,400*300).ToArray())};view.Measure(new(400,300));view.Arrange(new(0,0,400,300));view.UpdateLayout();
            foreach(double ratio in new[]{.25,.75}){view.SplitRatio=ratio;view.UpdateLayout();var bitmap=new RenderTargetBitmap(400,300,96,96,PixelFormats.Pbgra32);bitmap.Render(view);byte[] pixels=new byte[400*300*4];bitmap.CopyPixels(pixels,1600,0);Require(pixels[(200*400+50)*4]==0&&pixels[(200*400+350)*4]==255,"Äußere Vergleichsseiten falsch.");Require(pixels[(200*400+200)*4]==(ratio<.5?255:0),"Trennlinie verschiebt die Bildaufteilung nicht.");File.WriteAllBytes(Path.Combine(output,$"Comparison-{ratio}.png"),ImageProcessor.Png(bitmap));}view.SplitRatio=double.NaN;Require(view.SplitRatio==.5,"Nicht endliche Trennposition akzeptiert.");view.SplitRatio=2;Require(view.SplitRatio==.98,"Trennposition verlässt das Bild.");
        });
        Test("Transparente Pixel werden vor Feinrotation weiß hinterlegt",()=>
        {
            var source=BitmapSource.Create(20,20,96,96,PixelFormats.Bgra32,null,new byte[20*20*4],80);source.Freeze();var result=ImageProcessor.Process(source,new(){StraightenAngle=17.3});Require(result.Gray.Pixels.All(p=>p>=254),"Transparenz erzeugt dunkle Kanten.");
        });
        Test("Bildpipeline reagiert auf Abbruch",()=>{using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>ImageProcessor.Process(ImageProcessor.Demo(),new(){StraightenAngle=17},cts.Token));});
        Test("Ungültige Vorschau zeigt kein altes Modell und lässt sich wiederherstellen",()=>
        {
            string? previous=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR");Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",Path.Combine(output,"viewmodel"));LithoCreate.App.ViewModels.StudioViewModel? vm=null;
            try
            {
                vm=new();_ = vm.UpdatePreview();PumpUntil(()=>vm.PreviewMesh!=null);
                vm.Model.Width=double.NaN;vm.Changed();PumpUntil(()=>vm.PreviewMesh==null);Require(!vm.ExportPng.CanExecute(null)&&!vm.Camera.CanExecute(null),"Leere oder veraltete Vorschau kann exportiert werden.");
                vm.Model.Width=130;vm.Changed();PumpUntil(()=>vm.PreviewMesh!=null);Require(Math.Abs(vm.PreviewMesh!.Validate().Size.X-130)<.001,"Korrigiertes Modell wird nicht angezeigt.");
                vm.Dirty=false;vm.New.Execute(null);Require(vm.PreviewMesh==null&&!vm.ExportPng.CanExecute(null),"Neues Projekt zeigt das alte Mesh.");PumpUntil(()=>vm.PreviewMesh!=null);
            }
            finally{if(vm!=null){vm.Dirty=false;vm.CleanShutdown();}Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",previous);}
        });
        Test("Fotoeditor: Feinrotation, Vergleich, Undo und Übernahme alter EXIF-Projekte",()=>
        {
            string? previous=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR");Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",Path.Combine(output,"photo-vm"));LithoCreate.App.ViewModels.StudioViewModel? vm=null;
            try
            {
                vm=new();vm.Model.Width=30;vm.Model.Height=20;vm.Model.Resolution=1;vm.LoadImage(Path.Combine(output,"Exif-6.tiff"));Require(vm.Original.PixelWidth==2,"Neues Foto wird nicht ausgerichtet.");
                vm.Edits.StraightenAngle=7.2;vm.Changed();var task=vm.UpdatePreview();PumpUntil(()=>task.IsCompleted);vm.Dirty=false;vm.CompareEnabled=true;Require(vm.IsPhotoComparison&&!vm.Dirty,"Ansichtsänderung verändert das Projekt.");vm.ShowOriginal=true;Require(!vm.IsPhotoComparison,"Ausschnittwahl wird vom Vergleich überlagert.");vm.ShowOriginal=false;
                vm.Undo.Execute(null);Require(vm.Edits.StraightenAngle==0,"Feinrotation fehlt in Undo.");vm.Redo.Execute(null);Require(vm.Edits.StraightenAngle==7.2,"Feinrotation fehlt in Redo.");vm.ResetStraighten.Execute(null);Require(vm.Edits.StraightenAngle==0,"Feinrotation lässt sich nicht zurücksetzen.");vm.Undo.Execute(null);Require(vm.Edits.StraightenAngle==7.2,"Zurücksetzen ist nicht rückgängig zu machen.");vm.SetCrop(.1,0,.8,1);Require(vm.Edits.StraightenAngle==0,"Neue Originalauswahl behält unerwartete Drehung.");
                string legacy=Path.Combine(output,"legacy-exif-v2.lithopro");if(File.Exists(legacy))File.Delete(legacy);byte[] original=File.ReadAllBytes(Path.Combine(output,"Exif-6.tiff"));
                using(var zip=ZipFile.Open(legacy,ZipArchiveMode.Create)){using(var writer=new StreamWriter(zip.CreateEntry("project.json").Open()))writer.Write("{\"Version\":2,\"Name\":\"Legacy EXIF\",\"Model\":{\"Width\":30,\"Height\":20,\"Resolution\":1},\"Edits\":{\"CropX\":0.1,\"CropWidth\":0.8}}");using var stream=zip.CreateEntry("source.bin").Open();stream.Write(original);}
                vm.Dirty=false;vm.OpenPath(legacy);Require(vm.Project.Version==15&&!vm.Edits.AutoOrient&&vm.Original.PixelWidth==3,"Migration verändert die ursprüngliche EXIF-Ausrichtung.");vm.ResetImage.Execute(null);Require(!vm.Edits.AutoOrient,"Bildreset ändert die Legacy-Ausrichtung.");
                string upgraded=Path.Combine(output,"legacy-exif-v9.lithopro");ProjectStore.Save(upgraded,vm.Project,original);var loaded=ProjectStore.Load(upgraded);Require(loaded.Data.Version==15&&!loaded.Data.Edits.AutoOrient&&loaded.Image.SequenceEqual(original),"Migration ist nicht dauerhaft oder verändert Originalbytes.");
                var exported=ModelComputation.BuildExportMesh(original,vm.Model.Clone(),vm.Edits.Clone(),CancellationToken.None);var raw=ImageProcessor.Process(ImageProcessor.Decode(original,autoOrient:false),vm.Edits);var expected=MeshGenerator.Generate(raw.Gray,vm.Model,false);Require(exported.Mesh.Vertices.SequenceEqual(expected.Vertices),"Export verwendet eine andere Ausrichtung als die Legacy-Vorschau.");
            }
            finally{if(vm!=null){vm.Dirty=false;vm.CleanShutdown();}Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",previous);}
        });
        Test("Kamera zeigt das vollständige Modell bei breiten und schmalen Vorschaufenstern",()=>
        {
            var mesh=MeshGenerator.Generate(gray,new(){Width=145,Height=110,StandEnabled=true,StandHeight=8,StandDepth=28,Resolution=2},true);var viewport=new LithoCreate.App.Controls.ModelViewport();viewport.SetMesh(mesh,gray);
            foreach(var size in new[]{new System.Windows.Size(850,430),new System.Windows.Size(350,170),new System.Windows.Size(280,500)})
            {
                viewport.Measure(size);viewport.Arrange(new System.Windows.Rect(new System.Windows.Point(),size));viewport.UpdateLayout();
                foreach(string view in new[]{"reset","front","side","top","bottom"})
                {
                    viewport.View(view);var camera=(System.Windows.Media.Media3D.PerspectiveCamera)((System.Windows.Controls.Viewport3D)viewport.Children[0]).Camera;
                    var forward=camera.LookDirection;forward.Normalize();var right=System.Windows.Media.Media3D.Vector3D.CrossProduct(forward,camera.UpDirection);right.Normalize();var up=System.Windows.Media.Media3D.Vector3D.CrossProduct(right,forward);
                    double tan=Math.Tan(camera.FieldOfView*Math.PI/360);var bounds=mesh.Bounds();
                    foreach(float x in new[]{bounds.Min.X,bounds.Max.X})foreach(float y in new[]{bounds.Min.Y,bounds.Max.Y})foreach(float z in new[]{bounds.Min.Z,bounds.Max.Z})
                    {
                        var delta=new System.Windows.Media.Media3D.Point3D(x,y,z)-camera.Position;double depth=System.Windows.Media.Media3D.Vector3D.DotProduct(delta,forward);
                        Require(depth>0&&Math.Abs(System.Windows.Media.Media3D.Vector3D.DotProduct(delta,right))<depth*tan&&Math.Abs(System.Windows.Media.Media3D.Vector3D.DotProduct(delta,up))<depth*tan*size.Height/size.Width,"Modell wird durch den Kamerafrustum abgeschnitten.");
                    }
                }
            }
        });
        Test("Topologieprüfung erkennt offene Fläche",()=>{var m=new Mesh();m.Vertices.AddRange([new(0,0,0),new(1,0,0),new(0,1,0)]);m.Face(0,1,2);Require(!m.Validate().IsValid,"Offenes Mesh akzeptiert");});
        Test("Schichtschnitt einer Platte: geschlossene Kontur, Fläche und Umfang",()=>
        {
            var mesh=MeshGenerator.Generate(new(1,1,[0]),new(){Width=60,Height=40,FrameWidth=0,Resolution=1},false);var index=new CrossSectionIndex(mesh);
            foreach(double y in new[]{0.0,10.25,20,40}){var section=index.Slice(y);Require(section.IsRegular&&section.ClosedContours==1,"Plattenkontur ist offen oder verzweigt.");Require(Math.Abs(section.Area-180)<.001,"Querschnittsfläche der Platte falsch.");Require(Math.Abs(section.Perimeter-126)<.001,"Konturumfang falsch.");}
            Require(index.Slice(-1).Paths.Count==0&&index.Slice(41).Area==0,"Außerhalb des Modells wird Material angezeigt.");
        });
        Test("Schichten unterscheiden Lampenboden, Bodenöffnung und Hohlraum",()=>
        {
            var mesh=MeshGenerator.Generate(new(1,1,[0]),new(){Shape=Shape.Cylinder,Diameter=50,Height=40,Resolution=.5,LampBaseThickness=3,LampBaseHoleDiameter=6},false);var index=new CrossSectionIndex(mesh);
            foreach(double y in new[]{0.0,1.5,3.0}){var section=index.Slice(y);Require(section.IsRegular&&section.ClosedContours==2,"Boden muss Außenrand und Bohrung enthalten.");Require(Math.Abs(section.Area-Math.PI*(625-9))<7,"Bodenfläche enthält einen falschen Innenraum.");}
            var shell=index.Slice(15.1);Require(shell.IsRegular&&shell.ClosedContours==2,"Hohle Zylinderwand nicht erkannt.");Require(Math.Abs(shell.Area-Math.PI*(625-484))<3,"Ringfläche der Zylinderwand falsch.");
        });
        Test("Kugelschicht erhält den Innenraum statt einer gefüllten Scheibe",()=>
        {
            var mesh=MeshGenerator.Generate(new(1,1,[0]),new(){Shape=Shape.Sphere,Diameter=65,Resolution=.6},false);var section=new CrossSectionIndex(mesh).Slice(0);
            Require(section.IsRegular&&section.ClosedContours==2,"Innen- oder Außenkontur fehlt.");Require(Math.Abs(section.Area-Math.PI*(32.5*32.5-29.5*29.5))<5,"Kugelinnenraum wird gefüllt.");
        });
        Test("Schichtschnitt des integrierten Standfußes",()=>
        {
            var mesh=MeshGenerator.Generate(new(1,1,[0]),new(){Width=60,Height=40,FrameWidth=0,StandEnabled=true,StandHeight=5,StandDepth=24,Resolution=1},false);var index=new CrossSectionIndex(mesh);
            Require(Math.Abs(index.Slice(2).Area-60*24)<.01,"Standfläche falsch.");Require(Math.Abs(index.Slice(5).Area-60*24)<.01,"Koplanare Stand-Schulter enthält innere Trennkanten.");Require(Math.Abs(index.Slice(10).Area-60*3)<.01,"Fotoplatte oberhalb des Standes falsch.");
        });
        Test("Konturfläche integriert sich zum Volumen des Quaders",()=>
        {
            var mesh=MeshGenerator.Generate(new(1,1,[0]),new(){Width=32,Height=20,FrameWidth=0,Resolution=1},false);var index=new CrossSectionIndex(mesh);double volume=0;
            for(int layer=0;layer<100;layer++)volume+=index.Slice((layer+.5)*.2).Area*.2;
            Require(Math.Abs(volume-mesh.Validate().Volume)<.01,"Schichtflächen und Meshvolumen stimmen nicht überein.");
        });
        Test("Schichtschnitt ist abbrechbar und weist ungültige Höhen ab",()=>
        {
            var mesh=MeshGenerator.Generate(gray,new(){Resolution=1},false);using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>new CrossSectionIndex(mesh,cts.Token));var index=new CrossSectionIndex(mesh);Throws<OperationCanceledException>(()=>index.Slice(10,cts.Token));Throws<InvalidDataException>(()=>index.Slice(double.NaN));
        });
        foreach(var shape in SingleBodyShapes())Test("Schichtflächen und Meshvolumen: "+shape,()=>
        {
            var mesh=MeshGenerator.Generate(gray,new(){Shape=shape,Width=48,Height=36,Diameter=40,Radius=40,Resolution=1,FrameWidth=2},false);var index=new CrossSectionIndex(mesh);double volume=0,step=index.Height/180;
            for(int i=0;i<180;i++){var section=index.Slice(index.Bottom+(i+.5)*step);Require(section.IsRegular,$"Unregelmäßige Kontur bei {i}: {section.IrregularVertices} Kontakte, {section.OpenContours} offene Pfade");volume+=section.Area*step;}
            double expected=mesh.Validate().Volume;Require(Math.Abs(volume-expected)/expected<.025,$"Schichtvolumen {volume:N2} weicht von Meshvolumen {expected:N2} ab.");
        });
        Test("Schichtansicht verwaltet Höhe, Exportauflösung und veraltete Daten",()=>
        {
            string? previous=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR");Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",Path.Combine(output,"sections-vm"));LithoCreate.App.ViewModels.StudioViewModel? vm=null;
            try
            {
                vm=new();vm.Model.Width=40;vm.Model.Height=20.03;vm.Model.Resolution=.5;vm.Model.FrameWidth=0;_ =vm.UpdatePreview();PumpUntil(()=>vm.PreviewMesh!=null);vm.Dirty=false;
                vm.SliceLayerHeight=.12;vm.CurrentLayer=int.MaxValue;vm.IsSectionMode=true;var sectionTask=vm.UpdateSection();PumpUntil(()=>sectionTask.IsCompleted);sectionTask.GetAwaiter().GetResult();
                Require(vm.TotalLayers==167&&vm.CurrentLayer==167,"Letzte Teilschicht fehlt.");Require(vm.CurrentSection!=null&&Math.Abs(vm.CurrentSection.Height-19.975)<.001,"Teilschicht wird nicht zwischen 19,92 und 20,03 mm geschnitten.");Require(!vm.Dirty,"Ansichtsänderungen machen das Projekt ungespeichert.");
                vm.InspectFullQuality.Execute(null);PumpUntil(()=>!vm.Busy);Require(vm.HasFullInspection,"Prüfmodell wurde nicht übernommen.");var fullTask=vm.UpdateSection();PumpUntil(()=>fullTask.IsCompleted);fullTask.GetAwaiter().GetResult();Require(vm.SectionInfo.Contains("Exportauflösung"),"Hohe Qualität wird nicht verwendet.");
                vm.SliceLayerHeight=0;var invalidTask=vm.UpdateSection();PumpUntil(()=>invalidTask.IsCompleted);Require(vm.CurrentSection==null&&!vm.ExportPng.CanExecute(null)&&vm.SliceLayerError.Length>0,"Ungültige Schichthöhe zeigt altes Material.");
                vm.SliceLayerHeight=.2;vm.Model.Width=45;vm.Changed();Require(!vm.HasFullInspection&&vm.CurrentSection==null,"Modelländerung behält veraltete Prüfdaten.");PumpUntil(()=>vm.HasSectionMesh);var nextTask=vm.UpdateSection();PumpUntil(()=>nextTask.IsCompleted);Require(vm.CurrentSection!=null,"Schicht wird nach Modelländerung nicht neu aufgebaut.");
            }
            finally{if(vm!=null){vm.Dirty=false;vm.CleanShutdown();}Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",previous);}
        });
        Test("Prüfmodell und STL verwenden dieselbe hoch aufgelöste Geometrie",()=>
        {
            byte[] bytes=ImageProcessor.Png(ImageProcessor.Demo());var settings=new ModelSettings{Shape=Shape.Cylinder,Diameter=36,Height=30,Resolution=.5,LampBaseThickness=2,LampBaseHoleDiameter=6};
            var result=ModelComputation.BuildExportMesh(bytes,settings,new(){Contrast=1.3},CancellationToken.None);string path=Path.Combine(output,"Inspected-Export.stl");ModelExporter.Export(path,result.Mesh);var exported=ModelExporter.ReadStl(path);
            var index=new CrossSectionIndex(result.Mesh);double volume=0,step=index.Height/240;for(int i=0;i<240;i++)volume+=index.Slice(index.Bottom+(i+.5)*step).Area*step;
            Require(Math.Abs(volume-exported.Validate().Volume)/volume<.02,"Prüfschichten entsprechen nicht dem exportierten Körper.");using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>ModelComputation.BuildExportMesh(bytes,settings,new(),cts.Token));
        });
        Test("Lichtregler erhält Geometrie und Textur ohne erneute Allokation",()=>
        {
            var viewport=new LithoCreate.App.Controls.ModelViewport();viewport.SetMesh(MeshGenerator.Generate(gray,new(){Resolution=2},true),gray);var native=(System.Windows.Controls.Viewport3D)viewport.Children[0];var scene=(System.Windows.Media.Media3D.Model3DGroup)((System.Windows.Media.Media3D.ModelVisual3D)native.Children[0]).Content;
            var original=scene.Children.OfType<System.Windows.Media.Media3D.GeometryModel3D>().First();var group=(System.Windows.Media.Media3D.MaterialGroup)original.Material;var brush=(ImageBrush)((System.Windows.Media.Media3D.EmissiveMaterial)group.Children.Last()).Brush;var texture=brush.ImageSource;
            for(int i=0;i<100;i++)viewport.SetIntensity(i/100.0);Require(ReferenceEquals(original,scene.Children.OfType<System.Windows.Media.Media3D.GeometryModel3D>().First())&&ReferenceEquals(texture,brush.ImageSource),"Lichtänderung baut Geometrie oder Bild neu auf.");Require(Math.Abs(brush.Opacity-.99)<.00001,"Lichtwert wird nicht übernommen.");
        });
        Test("Schicht-PNG lässt Hohlraum frei und füllt den Materialring",()=>
        {
            var mesh=MeshGenerator.Generate(new(1,1,[0]),new(){Shape=Shape.Cylinder,Diameter=40,Height=30,Resolution=.5,MinThickness=3,MaxThickness=3},false);var view=new LithoCreate.App.Controls.SectionViewport{Section=new CrossSectionIndex(mesh).Slice(15.13)};view.Measure(new(400,400));view.Arrange(new(0,0,400,400));view.UpdateLayout();
            var bitmap=new RenderTargetBitmap(400,400,96,96,PixelFormats.Pbgra32);bitmap.Render(view);File.WriteAllBytes(Path.Combine(output,"Section-Cylinder.png"),ImageProcessor.Png(bitmap));byte[] pixels=new byte[400*400*4];bitmap.CopyPixels(pixels,1600,0);
            Require(pixels[(203*400+203)*4+2]<40,"Hohlraum ist als Material gefüllt.");Require(pixels[(203*400+357)*4+2]>75,"Wandmaterial wird nicht gefüllt.");
        });
        foreach(var orientation in Enum.GetValues<PrintOrientation>())foreach(bool turn in new[]{false,true})Test($"Drucklage, Schichtfläche und Datei-Roundtrip: {orientation}, Bett 90°={turn}",()=>
        {
            var settings=new ModelSettings{Width=40,Height=30,Resolution=2,FrameWidth=0,PrintOrientation=orientation,TurnOnBed90=turn};var mesh=MeshGenerator.Generate(new(1,1,[0]),settings,false);var report=mesh.Validate();Require(report.IsValid,"Gedrehte Geometrie ist nicht geschlossen und außenorientiert.");Require(Math.Abs(report.Volume-3600)<.01,"Volumen durch Drehung verändert.");
            Vector3 expected=orientation switch{PrintOrientation.Upright or PrintOrientation.UpsideDown=>new(40,3,30),PrintOrientation.FaceUp or PrintOrientation.FaceDown=>new(40,30,3),_=>new(30,3,40)};if(turn)expected=new(expected.Y,expected.X,expected.Z);
            var printed=ModelExporter.InPrintCoordinates(mesh).Validate();Require(Vector3.Distance(printed.Size,expected)<.001&&Math.Abs(printed.Bounds.Min.Z)<.0001,"Exportmaße oder Z=0 falsch.");
            var index=new CrossSectionIndex(mesh);var section=index.Slice(index.Bottom+index.Height*.37);Require(section.IsRegular&&section.ClosedContours==1&&Math.Abs(section.Area-expected.X*expected.Y)<.01,"Schichtfläche entspricht nicht der Drucklage.");CheckRoundtrip(mesh,$"Placement-{orientation}-{turn}");
        });
        Test("Druckausrichtung erhält alle Formen und Foto-UVs",()=>
        {
            foreach(var shape in SingleBodyShapes())
            {
                var settings=new ModelSettings{Shape=shape,Width=40,Height=30,Diameter=40,Radius=45,Resolution=2};var canonical=MeshGenerator.Generate(gray,settings,false);double volume=canonical.Validate().Volume;
                settings.PrintOrientation=PrintOrientation.LeftEdgeDown;settings.TurnOnBed90=true;var rotated=MeshGenerator.Generate(gray,settings,false);var check=rotated.Validate();Require(check.IsValid&&Math.Abs(check.Volume-volume)<Math.Max(.05,volume*.00001),"Volumen oder Topologie verändert: "+shape);Require(canonical.TextureCoordinates.SequenceEqual(rotated.TextureCoordinates)&&canonical.PhotoVertices.SequenceEqual(rotated.PhotoVertices)&&canonical.Triangles.SequenceEqual(rotated.Triangles),"Fotozuordnung oder Dreieckanordnung verändert: "+shape);
            }
        });
        Test("Druckraumsuche nutzt Bettrotation, flache und seitliche Lage",()=>
        {
            var mesh=MeshGenerator.Generate(new(1,1,[0]),new(){Width=120,Height=80,FrameWidth=0,Resolution=2},false);
            var bed=PrintPlacement.Find(mesh,new(){BedX=10,BedY=130,BuildZ=90});Require(bed is {Orientation:PrintOrientation.Upright,TurnOnBed90:true},"Passende Bettrotation wird nicht bevorzugt.");
            var flat=PrintPlacement.Find(mesh,new(){BedX=130,BedY=90,BuildZ=20});Require(flat is {Orientation:PrintOrientation.FaceUp,TurnOnBed90:false},"Flache Lage nicht gefunden.");
            var side=PrintPlacement.Find(mesh,new(){BedX=90,BedY=10,BuildZ=130});Require(side is {Orientation:PrintOrientation.RightEdgeDown,TurnOnBed90:false},"Seitliche Lage nicht gefunden.");
            var retain=PrintPlacement.Find(mesh,new(){BedX=100,BedY=130,BuildZ=20},PrintOrientation.FaceDown,true);Require(retain is {Orientation:PrintOrientation.FaceDown,TurnOnBed90:true},"Bereits passende bewusste Lage wird verändert.");Require(PrintPlacement.Find(mesh,new(){BedX=2,BedY=2,BuildZ=2})==null,"Nicht passendes Modell wird als passend ausgegeben.");
        });
        Test("Ausrichtungssuche und Transformation validieren Eingaben und Abbruch",()=>
        {
            var mesh=MeshGenerator.Generate(gray,new(){Width=30,Height=20,Resolution=2},false);var first=mesh.Vertices[0];using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>PrintPlacement.Apply(mesh,PrintOrientation.FaceUp,false,cts.Token));Require(mesh.Vertices[0]==first,"Abbruch verändert bereits das Mesh.");Throws<OperationCanceledException>(()=>PrintPlacement.Find(mesh,new(),token:cts.Token));Throws<InvalidDataException>(()=>PrintPlacement.Find(new Mesh(),new()));Throws<InvalidDataException>(()=>PrintPlacement.Find(mesh,new(){BedX=-1}));Throws<InvalidDataException>(()=>new ModelSettings{PrintOrientation=(PrintOrientation)999}.Validate());
        });
        Test("Format 3 behält Bildkorrekturen und aufrechte Drucklage beim Wechsel auf Format 13",()=>
        {
            string path=Path.Combine(output,"legacy-v3.lithopro");if(File.Exists(path))File.Delete(path);byte[] original=ImageProcessor.Png(ImageProcessor.Demo());
            using(var zip=ZipFile.Open(path,ZipArchiveMode.Create)){using(var w=new StreamWriter(zip.CreateEntry("project.json").Open()))w.Write("{\"Version\":3,\"Name\":\"Version 3\",\"Model\":{\"Width\":123},\"Edits\":{\"StraightenAngle\":7.5,\"AutoOrient\":true}}");using var s=zip.CreateEntry("source.bin").Open();s.Write(original);}
            var loaded=ProjectStore.Load(path);Require(loaded.Data.Version==15&&loaded.Data.Model.PrintOrientation==PrintOrientation.Upright&&!loaded.Data.Model.TurnOnBed90&&loaded.Data.Edits.StraightenAngle==7.5&&loaded.Data.Edits.AutoOrient,"Migration verändert Bild oder Lage.");loaded.Data.Model.PrintOrientation=PrintOrientation.FaceDown;loaded.Data.Model.TurnOnBed90=true;string next=Path.Combine(output,"placed-v9.lithopro");ProjectStore.Save(next,loaded.Data,loaded.Image);var persisted=ProjectStore.Load(next);Require(persisted.Data.Version==15&&persisted.Data.Model.PrintOrientation==PrintOrientation.FaceDown&&persisted.Data.Model.TurnOnBed90&&persisted.Image.SequenceEqual(original),"Druckausrichtung wird nicht gespeichert.");
        });
        Test("Druckraumsuche aktualisiert Vorschau und Schichten ohne veraltetes Prüfmodell",()=>
        {
            string? previous=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR");Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",Path.Combine(output,"placement-vm"));LithoCreate.App.ViewModels.StudioViewModel? vm=null;
            try
            {
                vm=new();vm.Model.Width=50;vm.Model.Height=30;vm.Model.FrameWidth=0;vm.Model.Resolution=1;vm.Printer.BedX=60;vm.Printer.BedY=40;vm.Printer.BuildZ=10;var preview=vm.UpdatePreview();PumpUntil(()=>preview.IsCompleted);vm.InspectFullQuality.Execute(null);PumpUntil(()=>!vm.Busy);Require(vm.HasFullInspection,"Vorheriges Prüfmodell fehlt.");
                var old=vm.PreviewMesh;var task=vm.FitPlacement();PumpUntil(()=>task.IsCompleted);task.GetAwaiter().GetResult();Require(vm.Model.PrintOrientation==PrintOrientation.FaceUp&&!vm.HasFullInspection&&vm.Dirty,"Neue Drucklage oder Cache-Verwerfung fehlt.");PumpUntil(()=>vm.PreviewMesh!=null&&!ReferenceEquals(old,vm.PreviewMesh));Require(vm.PreviewMesh!.Validate().Size.Y<4&&vm.PlacementSearchInfo.Contains("Export-Raster"),"Drucklage oder Qualitätsinformation falsch.");vm.CurrentLayer=1;var slice=vm.UpdateSection();PumpUntil(()=>slice.IsCompleted);Require(vm.TotalLayers<40&&vm.CurrentSection is {IsRegular:true}&&Math.Abs(vm.CurrentSection.Area-1500)<.1,"Schichtansicht behält die aufrechte Geometrie.");
                vm.Dirty=false;var keep=vm.FitPlacement();PumpUntil(()=>keep.IsCompleted);Require(!vm.Dirty&&vm.Model.PrintOrientation==PrintOrientation.FaceUp,"Erneute Suche verändert passende Lage.");
                vm.Printer.BedX=2;vm.Printer.BedY=2;vm.Printer.BuildZ=2;vm.Changed();vm.Dirty=false;var noFit=vm.FitPlacement();PumpUntil(()=>noFit.IsCompleted);Require(vm.Model.PrintOrientation==PrintOrientation.FaceUp&&!vm.Dirty&&vm.Model.Angles.IsZero&&vm.PlacementSearchInfo.Length>0,"Erfolglose Suche verändert Einstellungen.");
                var cancel=vm.FitPlacement();vm.Cancel.Execute(null);PumpUntil(()=>cancel.IsCompleted);Require(!vm.Busy&&vm.Model.PrintOrientation==PrintOrientation.FaceUp,"Abbruch verändert Lage oder blockiert Bedienung.");
            }
            finally{if(vm!=null){vm.Dirty=false;vm.CleanShutdown();}Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",previous);}
        });
        foreach(var shape in new[]{Shape.Sphere,Shape.Moon})foreach(double clearance in new[]{.1,.25,.6})Test($"Separater Sockel: {shape}, radiales Spiel {clearance}",()=>
        {
            var s=new ModelSettings{Shape=shape,Diameter=85,SphereOpeningDiameter=34,SphereCollarHeight=4,SphereVentDiameter=4,SocketEnabled=true,SocketClearance=clearance,Part=ModelPart.Socket,Resolution=.8};var socket=MeshGenerator.Generate(gray,s,false);var report=socket.Validate();Require(report.IsValid&&Euler(socket)==0,"Sockel ist nicht geschlossen oder die Durchführung fehlt.");Require(Math.Abs(report.Size.X-60)<.001&&Math.Abs(report.Size.Y-8)<.001&&Math.Abs(report.Size.Z-60)<.001,"Sockelmaße falsch.");Require(socket.PhotoVertices.All(p=>!p),"Sockel trägt eine Fototextur.");
            double peg=socket.Vertices.Where(v=>v.Y>s.SocketPlateHeight+.1).Max(v=>Math.Sqrt(v.X*v.X+v.Z*v.Z));Require(Math.Abs(peg-(s.SphereOpeningDiameter/2-clearance))<.001,"Radiales Spiel falsch.");Require(Math.Abs(MinimumClearRadius(socket,0)-s.SocketHoleDiameter/2)<.001,"Durchführung unterschreitet den freien Durchmesser.");Require(VerticalIntersections(socket,.1,.1)==0,"Zentrale Kabeldurchführung ist verschlossen.");
            s.Part=ModelPart.Lithophane;var body=MeshGenerator.Generate(gray,s,false);double free=MinimumClearRadius(body,body.Bounds().Min.Y);Require(free-peg>=clearance-.001,"Sockel überschreitet die Kugelöffnung.");
            if(shape==Shape.Sphere&&clearance==.25)CheckRoundtrip(body,"Socket-Receiver");
            CheckRoundtrip(socket,shape==Shape.Sphere&&clearance==.25?"Socket-Reference":$"Socket-{shape}-{clearance}");
        });
        Test("Sockelvolumen stimmt mit dem rotationssymmetrischen Profil überein",()=>
        {
            var s=new ModelSettings{Shape=Shape.Sphere,Diameter=85,SphereOpeningDiameter=34,SocketEnabled=true,Part=ModelPart.Socket,Resolution=.8};var socket=MeshGenerator.Generate(gray,s,false);int count=socket.Vertices.Count/6;double factor=count*Math.Sin(Math.Tau/count)/2,rHole=s.SocketHoleDiameter/(2*Math.Cos(Math.PI/count)),rPeg=(s.SphereOpeningDiameter-2*s.SocketClearance)/2;
            double volume=factor*((s.SocketFootDiameter*s.SocketFootDiameter/4-rHole*rHole)*s.SocketPlateHeight+(rPeg*rPeg-rHole*rHole)*s.SocketInsertionHeight);Require(Math.Abs(socket.Validate().Volume-volume)<.05,"Sockelvolumen enthält Überlappungen oder falsche Profile.");
        });
        Test("Montageansicht hat getrennte Körper und ausreichendes Spiel",()=>
        {
            var s=new ModelSettings{Shape=Shape.Sphere,Diameter=85,SphereOpeningDiameter=34,SphereCollarHeight=4,SphereVentDiameter=4,SocketEnabled=true,Resolution=1};var body=MeshGenerator.Generate(gray,s,false);s.Part=ModelPart.Socket;var socket=MeshGenerator.Generate(gray,s,false);s.Part=ModelPart.Assembly;var assembly=MeshGenerator.Generate(gray,s,false);var report=assembly.Validate();Require(assembly.IsAssemblyPreview&&report.IsValid,"Montageansicht ungültig.");Require(assembly.Vertices.Count==body.Vertices.Count+socket.Vertices.Count,"Teile fehlen in der Montage.");Require(Math.Abs(report.Volume-body.Validate().Volume-socket.Validate().Volume)<.5,"Montagevolumen verändert die Einzelteile.");Require(Math.Abs(assembly.Vertices.Take(body.Vertices.Count).Min(v=>v.Y)-s.SocketPlateHeight)<.001,"Kragen liegt nicht auf der Sockelplatte.");
            var section=new CrossSectionIndex(assembly).Slice(s.SocketPlateHeight+s.SocketInsertionHeight/2);Require(section.IsRegular&&section.ClosedContours==4,"Einsteckring überschneidet den Kragen oder Innenräume fehlen.");Require(VerticalIntersections(assembly,.1,.1)==0,"Durchgehender axialer Weg ist blockiert.");
            s.PrintOrientation=PrintOrientation.FaceDown;s.TurnOnBed90=true;var unturned=MeshGenerator.Generate(gray,s,false);Require(unturned.Vertices.SequenceEqual(assembly.Vertices),"Montageansicht übernimmt die Drucklage einzelner Teile.");
        });
        Test("Montage ist gegen gemeinsame Druckausgabe geschützt",()=>
        {
            var s=new ModelSettings{Shape=Shape.Sphere,Diameter=85,SphereOpeningDiameter=34,SocketEnabled=true,Part=ModelPart.Assembly,Resolution=1};var assembly=MeshGenerator.Generate(gray,s,false);string path=Path.Combine(output,"assembly-blocked.stl");File.WriteAllText(path,"original");Throws<InvalidDataException>(()=>ModelExporter.Export(path,assembly));Require(File.ReadAllText(path)=="original","Blockierter Export verändert eine Datei.");Throws<InvalidDataException>(()=>ModelComputation.BuildExportMesh([],s,new(),CancellationToken.None));Require(ModelExporter.InPrintCoordinates(assembly).IsAssemblyPreview,"Koordinatenwechsel entfernt den Schutz.");
        });
        Test("Sockeleinstellungen verhindern zu dünne und kollidierende Geometrie",()=>
        {
            var valid=new ModelSettings{Shape=Shape.Sphere,Diameter=85,SphereOpeningDiameter=34,SocketEnabled=true};
            foreach(Action<ModelSettings> change in new Action<ModelSettings>[] {s=>s.SphereOpeningDiameter=0,s=>s.SocketFootDiameter=10,s=>s.SocketPlateHeight=1,s=>s.SocketInsertionHeight=30,s=>s.SocketClearance=0,s=>s.SocketHoleDiameter=33,s=>s.SocketHoleDiameter=1,s=>s.SocketClearance=double.NaN,s=>s.Part=(ModelPart)999}){var s=valid.Clone();change(s);Throws<InvalidDataException>(s.Validate);}
            var tiny=new ModelSettings{Shape=Shape.Sphere,Diameter=12,SphereOpeningDiameter=5,SphereCollarHeight=0,SocketEnabled=true,SocketHoleDiameter=2,SocketInsertionHeight=1,Part=ModelPart.Socket,Resolution=1};Require(MeshGenerator.Generate(gray,tiny,false).Validate().IsValid,"Kleine gültige Passung wird abgewiesen.");tiny.SocketInsertionHeight=SphereSocket.Dimensions(tiny).MaximumInsertion+.01;Throws<InvalidDataException>(tiny.Validate);
        });
        Test("Sockel ist fotounabhängig und übernimmt die eigene Druckausrichtung",()=>
        {
            var s=new ModelSettings{Shape=Shape.Sphere,Diameter=85,SphereOpeningDiameter=34,SocketEnabled=true,Part=ModelPart.Socket,Resolution=1,SocketPrintOrientation=PrintOrientation.FaceUp};var a=MeshGenerator.Generate(new(1,1,[0]),s,false);var b=MeshGenerator.Generate(new(1,1,[255]),s,false);Require(a.Vertices.SequenceEqual(b.Vertices)&&a.Triangles.SequenceEqual(b.Triangles),"Foto verändert den Sockel.");Require(Math.Abs(a.Validate().Size.Y-s.SocketFootDiameter)<.001,"Drucklage fehlt beim Sockel.");var built=ModelComputation.BuildExportMesh([],s,new(),CancellationToken.None);Require(built.Report.IsValid,"Sockelexport verlangt unnötig ein decodierbares Foto.");using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>SphereSocket.Generate(s,false,cts.Token));
        });
        Test("Format 4 behält Drucklage ohne Zubehör, Format 13 erhält alle Sockelparameter",()=>
        {
            string legacy=Path.Combine(output,"legacy-v4.lithopro");if(File.Exists(legacy))File.Delete(legacy);byte[] photo=ImageProcessor.Png(ImageProcessor.Demo());using(var zip=ZipFile.Open(legacy,ZipArchiveMode.Create)){using(var w=new StreamWriter(zip.CreateEntry("project.json").Open()))w.Write("{\"Version\":4,\"Model\":{\"PrintOrientation\":2,\"TurnOnBed90\":true},\"Edits\":{\"StraightenAngle\":4.5}}");using var src=zip.CreateEntry("source.bin").Open();src.Write(photo);}var old=ProjectStore.Load(legacy);Require(old.Data.Version==15&&!old.Data.Model.SocketEnabled&&old.Data.Model.Part==ModelPart.Lithophane&&old.Data.Model.PrintOrientation==PrintOrientation.FaceDown&&old.Data.Model.TurnOnBed90&&old.Data.Edits.StraightenAngle==4.5,"Migration verändert Drucklage, Bild oder aktiviert Zubehör.");
            var data=new ProjectData{Model=new(){Shape=Shape.Sphere,Diameter=85,SphereOpeningDiameter=34,SocketEnabled=true,Part=ModelPart.Socket,SocketFootDiameter=64,SocketPlateHeight=6,SocketInsertionHeight=3.5,SocketClearance=.4,SocketHoleDiameter=9,SocketPrintOrientation=PrintOrientation.LeftEdgeDown,SocketTurnOnBed90=true}};string path=Path.Combine(output,"socket-project.lithopro");ProjectStore.Save(path,data,photo);var saved=ProjectStore.Load(path);Require(JsonSerializer.Serialize(saved.Data)==JsonSerializer.Serialize(data)&&saved.Image.SequenceEqual(photo),"Sockelparameter oder Original verändert.");
        });
        Test("Teilwechsel steuert Montage, Sockelschichten, Export und Raumsuche",()=>
        {
            string? previous=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR");Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",Path.Combine(output,"parts-vm"));LithoCreate.App.ViewModels.StudioViewModel? vm=null;
            try
            {
                vm=new();vm.Project.Model=new(){Shape=Shape.Sphere,Diameter=85,SphereOpeningDiameter=34,SphereCollarHeight=4,SocketEnabled=true,Resolution=1,PrintOrientation=PrintOrientation.FaceDown};vm.SelectedShape=Templates.All.First(p=>p.Shape==Shape.Sphere);var body=vm.UpdatePreview();PumpUntil(()=>body.IsCompleted);Require(vm.Parts.Length==3,"Teilwahl fehlt.");vm.IsSectionMode=true;vm.SelectedPart=SphereSocket.Parts[2];var montage=vm.UpdatePreview();PumpUntil(()=>montage.IsCompleted);Require(vm.IsAssemblyPart&&!vm.IsSectionMode&&vm.PreviewMesh is {IsAssemblyPreview:true}&&!vm.ExportStl.CanExecute(null)&&!vm.Export3Mf.CanExecute(null)&&!vm.FitToPrinter.CanExecute(null)&&!vm.InspectFullQuality.CanExecute(null),"Montage ermöglicht versehentliche Druckausgabe.");
                vm.SelectedPart=SphereSocket.Parts[1];var socket=vm.UpdatePreview();PumpUntil(()=>socket.IsCompleted);Require(vm.PreviewMesh!.PhotoVertices.All(p=>!p)&&vm.ExportStl.CanExecute(null),"Sockel bleibt Montage oder fototexturiert.");vm.IsSectionMode=true;var slice=vm.UpdateSection();PumpUntil(()=>slice.IsCompleted);Require(vm.CurrentSection is {IsRegular:true,ClosedContours:2},"Sockelschicht verliert Durchführung.");vm.SelectedPlacement=vm.Placements.First(p=>p.Orientation==PrintOrientation.FaceUp);vm.Printer.BedX=80;vm.Printer.BedY=80;vm.Printer.BuildZ=10;var fit=vm.FitPlacement();PumpUntil(()=>fit.IsCompleted);Require(vm.Model.SocketPrintOrientation==PrintOrientation.Upright&&vm.Model.PrintOrientation==PrintOrientation.FaceDown,"Raumsuche verändert Kugel statt Sockel.");
                vm.SocketEnabled=false;Require(vm.Parts.Length==1&&!vm.IsAssemblyPart&&vm.Model.Part==ModelPart.Lithophane,"Deaktiviertes Zubehör bleibt aktiv.");vm.SelectedShape=Templates.All.First(p=>p.Shape==Shape.Flat);var flat=vm.UpdatePreview();PumpUntil(()=>flat.IsCompleted);Require(vm.PreviewMesh!=null&&!vm.HasSocket,"Formwechsel zeigt Zubehör der alten Form.");
            }
            finally{if(vm!=null){vm.Dirty=false;vm.CleanShutdown();}Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",previous);}
        });
        foreach(double hole in new[]{0.0,8.0})foreach(double clearance in new[]{.1,.25,.6})
        Test($"Leuchtkasten-Rückwand: Durchführung {hole}, Spiel {clearance}",()=>
        {
            var settings=new ModelSettings{Shape=Shape.Lightbox,Width=80,Height=60,FrameWidth=3,BoxDepth=18,BackEnabled=true,BackHoleDiameter=hole,BackClearance=clearance,Resolution=.8,Part=ModelPart.BackPanel,BackPrintOrientation=PrintOrientation.Upright};
            var mesh=MeshGenerator.Generate(gray,settings,false);var report=mesh.Validate();Require(report.IsValid&&Euler(mesh)==(hole==0?2:0),"Rückwandtopologie oder Durchführung falsch: "+report);Require(mesh.PhotoVertices.All(v=>!v),"Rückwand trägt Foto.");
            Require(Vector3.Distance(report.Size,new(80,60,5))<.001,"Rückwandmaße falsch.");double w=74-2*clearance,h=54-2*clearance;
            var lip=mesh.Vertices.Where(v=>v.Z>settings.BackThickness+.1).ToArray();Require(Math.Abs(lip.Max(v=>v.X)-lip.Min(v=>v.X)-w)<.001&&Math.Abs(lip.Max(v=>v.Y)-lip.Min(v=>v.Y)-h)<.001,"Einsteckspiel falsch.");
            var flat=settings.Clone();flat.BackPrintOrientation=PrintOrientation.FaceUp;var oriented=MeshGenerator.Generate(gray,flat,false);var section=new CrossSectionIndex(oriented).Slice(settings.BackThickness+1);Require(section.IsRegular&&section.ClosedContours==2,"Einsteckschicht verliert Fläche oder Loch.");
            Require(Math.Abs(section.Area-(w*h-(w-3.2)*(h-3.2)))<.2,"Einsteckquerschnitt stimmt nicht.");
            if(hole==0)Require(Math.Abs(report.Volume-(80*60*2+(w*h-(w-3.2)*(h-3.2))*3))<.1,"Rückwandvolumen enthält Überlappungen.");
            if(clearance==.25)CheckRoundtrip(mesh,hole==0?"Back-Closed":"Back-Reference");
        });
        Test("Maßhaltige Leuchtkastenaufnahme in Vorschau und Export",()=>
        {
            var settings=new ModelSettings{Shape=Shape.Lightbox,Width=80,Height=60,FrameWidth=3,BoxDepth=18,BackEnabled=true,Resolution=.7};
            foreach(bool preview in new[]{true,false})
            {
                var mesh=MeshGenerator.Generate(new(1,1,[255]),settings,preview);var report=mesh.Validate();Require(report.IsValid&&Euler(mesh)==2,"Kasten nicht ein geschlossener Körper: "+report);
                double expected=(80*60-74*54)*(18+4)+74*54*.8;Require(Math.Abs(report.Volume-expected)<.2,"Kastenvolumen oder rechteckige Rahmenbreite falsch.");Require(Vector3.Distance(report.Size,new(80,60,22))<.001,"Kastenaußenmaße falsch.");
                PrintPlacement.Apply(mesh,PrintOrientation.FaceUp,false);var section=new CrossSectionIndex(mesh).Slice(-17);Require(section.IsRegular&&section.ClosedContours==2&&Math.Abs(section.Area-(80*60-74*54))<.1,"Innenöffnung ist nicht 74 × 54 mm.");if(!preview)CheckRoundtrip(mesh,"Back-Receiver");
            }
        });
        Test("Rückwandmontage: kontaktierende Schulter, vier getrennte Konturen",()=>
        {
            var s=new ModelSettings{Shape=Shape.Lightbox,Width=80,Height=60,FrameWidth=3,BoxDepth=18,BackEnabled=true,Part=ModelPart.Assembly,Resolution=1,PrintOrientation=PrintOrientation.FaceDown,BackPrintOrientation=PrintOrientation.RightEdgeDown,BackTurnOnBed90=true};
            var assembly=MeshGenerator.Generate(gray,s,false);var report=assembly.Validate();Require(report.IsValid&&assembly.IsAssemblyPreview,"Montage ungültig.");Require(Math.Abs(report.Bounds.Min.Z+20)<.001&&Math.Abs(report.Bounds.Max.Y-60)<.001,"Montage verwendet Drucklagen oder falschen Kontakt.");
            var body=s.Clone();body.Part=ModelPart.Lithophane;body.PrintOrientation=PrintOrientation.Upright;var back=s.Clone();back.Part=ModelPart.BackPanel;back.BackPrintOrientation=PrintOrientation.Upright;back.BackTurnOnBed90=false;
            Require(Math.Abs(report.Volume-MeshGenerator.Generate(gray,body,false).Validate().Volume-MeshGenerator.Generate(gray,back,false).Validate().Volume)<.2,"Montage verändert Teile.");
            PrintPlacement.Apply(assembly,PrintOrientation.FaceUp,false);var section=new CrossSectionIndex(assembly).Slice(-16.5);Require(section.IsRegular&&section.ClosedContours==4,"Einsteckrand überschneidet die Aufnahme oder Kabelöffnung fehlt.");
            string file=Path.Combine(output,"back-assembly-blocked.3mf");File.WriteAllText(file,"vorhanden");Throws<InvalidDataException>(()=>ModelExporter.Export(file,assembly));Require(File.ReadAllText(file)=="vorhanden","Montageexport beschädigt Datei.");Throws<InvalidDataException>(()=>ModelComputation.BuildExportMesh([],s,new(),CancellationToken.None));
        });
        Test("Rückwand: Foto unabhängig, Abbruch und ungültige Maße",()=>
        {
            var valid=new ModelSettings{Shape=Shape.Lightbox,BackEnabled=true,Part=ModelPart.BackPanel,Resolution=1};var a=ModelComputation.BuildExportMesh([],valid,new(),CancellationToken.None);var b=MeshGenerator.Generate(new(1,1,[0]),valid,false);Require(a.Mesh.Vertices.SequenceEqual(b.Vertices),"Rückwand benötigt Foto.");
            foreach(Action<ModelSettings> change in new Action<ModelSettings>[] {s=>s.FrameWidth=0,s=>s.BackThickness=.1,s=>s.BackRimWidth=.5,s=>s.BackRimWidth=70,s=>s.BackInsertionDepth=20,s=>s.BackInsertionDepth=0,s=>s.BackClearance=0,s=>s.BackClearance=2,s=>s.BackHoleDiameter=1,s=>s.BackHoleDiameter=100,s=>s.BackThickness=double.NaN,s=>s.BackPrintOrientation=(PrintOrientation)999}){var s=valid.Clone();change(s);Throws<InvalidDataException>(s.Validate);}
            using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>LightboxBack.Generate(valid,false,cts.Token));Throws<OperationCanceledException>(()=>LightboxBack.GenerateBody(gray,valid,false,cts.Token));
            valid.Shape=Shape.Flat;Require(valid.EffectivePart==ModelPart.Lithophane,"Rückwand bleibt in fremder Form aktiv.");Throws<InvalidDataException>(()=>LightboxBack.Generate(valid,false));
        });
        Test("Schmale und stark längliche Rückwände ohne degenerierte Ecken",()=>
        {
            foreach(var size in new[]{(W:12.0,H:12.0),(W:240.0,H:12.0),(W:12.0,H:240.0),(W:80.0,H:80.0),(W:79.99,H:80.0)})
            {
                var s=new ModelSettings{Shape=Shape.Lightbox,Width=size.W,Height=size.H,FrameWidth=1.2,BackEnabled=true,BackHoleDiameter=2,Part=ModelPart.BackPanel,Resolution=1};Require(MeshGenerator.Generate(gray,s,false).Validate().IsValid,"Schmale Rückwand ungültig: "+size);
            }
        });
        Test("Format 5 bleibt ohne Rückwand identisch und Format 13 erhält alle Teile",()=>
        {
            var legacy=new ProjectData{Version=5,Model=new(){Shape=Shape.Lightbox,Width=83,Height=67,FrameWidth=2,PrintOrientation=PrintOrientation.FaceDown,TurnOnBed90=true}};byte[] original=ImageProcessor.Png(ImageProcessor.Demo());var before=MeshGenerator.Generate(gray,legacy.Model,false);
            string path=Path.Combine(output,"legacy-v5-back.lithopro");if(File.Exists(path))File.Delete(path);using(var zip=ZipFile.Open(path,ZipArchiveMode.Create)){using(var stream=zip.CreateEntry("project.json").Open())JsonSerializer.Serialize(stream,legacy,ProjectStore.Json);using var photo=zip.CreateEntry("source.bin").Open();photo.Write(original);}
            var loaded=ProjectStore.Load(path);var after=MeshGenerator.Generate(gray,loaded.Data.Model,false);Require(loaded.Data.Version==15&&!loaded.Data.Model.BackEnabled&&before.Vertices.SequenceEqual(after.Vertices)&&before.Triangles.SequenceEqual(after.Triangles),"Migration verändert alte Geometrie.");
            loaded.Data.Model.BackEnabled=true;loaded.Data.Model.Part=ModelPart.BackPanel;loaded.Data.Model.BackThickness=2.5;loaded.Data.Model.BackInsertionDepth=4;loaded.Data.Model.BackClearance=.4;loaded.Data.Model.BackHoleDiameter=9;loaded.Data.Model.BackPrintOrientation=PrintOrientation.LeftEdgeDown;loaded.Data.Model.BackTurnOnBed90=true;string next=Path.Combine(output,"back-project.lithopro");ProjectStore.Save(next,loaded.Data,original);var saved=ProjectStore.Load(next);Require(JsonSerializer.Serialize(saved.Data)==JsonSerializer.Serialize(loaded.Data)&&saved.Image.SequenceEqual(original),"Rückwandparameter, Drucklagen oder Foto verändert.");
        });
        Test("Editor: Rückwandwechsel, Druckraumsuche und Montageschutz",()=>
        {
            string? previous=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR");Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",Path.Combine(output,"back-vm"));LithoCreate.App.ViewModels.StudioViewModel? vm=null;
            try
            {
                vm=new();vm.Project.Model=new(){Shape=Shape.Lightbox,Width=80,Height=60,BackEnabled=true,Resolution=1,PrintOrientation=PrintOrientation.FaceDown};vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);vm.SelectedPart=LightboxBack.Parts[1];var update=vm.UpdatePreview();PumpUntil(()=>update.IsCompleted);Require(vm.HasParts&&vm.HasBackPanel&&vm.Parts.Length==3&&!vm.HasPhotoSurface&&vm.ExportStl.CanExecute(null),"Rückwandteilwahl falsch.");
                vm.IsSectionMode=true;vm.CurrentLayer=1;var slice=vm.UpdateSection();PumpUntil(()=>slice.IsCompleted);Require(vm.CurrentSection is {IsRegular:true,ClosedContours:2},"Rückwandschicht fehlt.");vm.SelectedPlacement=vm.Placements.First(p=>p.Orientation==PrintOrientation.Upright);vm.Printer.BedX=90;vm.Printer.BedY=70;vm.Printer.BuildZ=10;var fit=vm.FitPlacement();PumpUntil(()=>fit.IsCompleted);Require(vm.Model.BackPrintOrientation==PrintOrientation.FaceUp&&vm.Model.PrintOrientation==PrintOrientation.FaceDown,"Druckraumsuche ändert Kasten statt Rückwand.");
                vm.SelectedPart=LightboxBack.Parts[2];var assembly=vm.UpdatePreview();PumpUntil(()=>assembly.IsCompleted);Require(vm.IsAssemblyPart&&!vm.IsSectionMode&&!vm.ExportStl.CanExecute(null)&&!vm.InspectFullQuality.CanExecute(null),"Montage gibt Druckmodell aus.");vm.BackEnabled=false;Require(!vm.HasParts&&vm.Parts.Length==1&&vm.Model.Part==ModelPart.Lithophane,"Deaktivierte Rückwand bleibt ausgewählt.");
                vm.BackEnabled=true;vm.SelectedPart=LightboxBack.Parts[1];vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Sphere);Require(!vm.HasBackPanel&&vm.SelectedPart.Part==ModelPart.Lithophane,"Formwechsel zeigt fremdes Teil.");
            }
            finally{if(vm!=null){vm.Dirty=false;vm.CleanShutdown();}Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",previous);}
        });
        foreach(bool cable in new[]{false,true})foreach(int count in new[]{1,4,8})
        Test($"Freie Rückwandbohrungen: {count}, Kabel {cable}",()=>
        {
            var settings=MountingFixture(count,cable);
            foreach(bool preview in new[]{true,false})
            {
                var mesh=MeshGenerator.Generate(gray,settings,preview);var report=mesh.Validate();int holes=count+(cable?1:0);Require(report.IsValid&&Euler(mesh)==2-2*holes,"Bohrungsmesh ungültig: "+report);Require(mesh.PhotoVertices.All(p=>!p),"Befestigungsrückwand trägt Foto.");
                Require(Vector3.Distance(report.Size,new(100,5,85))<.001,"Bohrungen verändern Außenmaße oder Drucklage.");var slice=new CrossSectionIndex(mesh).Slice(.7);Require(slice.IsRegular&&slice.ClosedContours==holes+1,"Bohrungen verschwinden im Schichtschnitt: "+slice.ClosedContours);
                double expected=100*85-settings.BackMountingHoles.Sum(h=>Math.PI*h.Diameter*h.Diameter/4)-(cable?Math.PI*16:0);Require(Math.Abs(slice.Area-expected)<1.2,"Bohrungsquerschnitt falsch.");double w=93.5,h=78.5,ring=w*h-(w-3.2)*(h-3.2);Require(Math.Abs(report.Volume-(slice.Area*2+ring*3))<.3,"Volumen oder Durchgangstiefe falsch.");
                var upper=new CrossSectionIndex(mesh).Slice(3);Require(upper.IsRegular&&upper.ClosedContours==2&&Math.Abs(upper.Area-ring)<.1,"Befestigungsbohrung beschädigt Einsteckring.");if(!preview&&count==4)CheckRoundtrip(mesh,cable?"Mounting-Reference":"Mounting-NoCable");
            }
        });
        Test("Bohrungen: unregelmäßige Koordinaten und fast fluchtende Freizonen",()=>
        {
            foreach(double shift in new[]{0,.00001,.004,.012,.03,.2})
            {
                var s=MountingFixture(4,true);s.BackMountingHoles[1].Y+=shift;s.BackMountingHoles[2].X+=shift;Require(MeshGenerator.Generate(gray,s,false).Validate().IsValid,"Fast fluchtende Hilfskanten ungültig: "+shift);
            }
            var random=new Random(170);for(int attempt=0;attempt<30;attempt++){var s=MountingFixture(4,true);foreach(var hole in s.BackMountingHoles){hole.X+=random.NextDouble()*3-1.5;hole.Y+=random.NextDouble()*3-1.5;hole.Diameter=2+random.NextDouble()*4;}Require(MeshGenerator.Generate(gray,s,attempt%2==0).Validate().IsValid,"Unregelmäßige Bohrungen ungültig.");}
        });
        Test("Bohrungen: Mindestabstände, Grenzwerte und Abbruch",()=>
        {
            var valid=MountingFixture(1,true);foreach(Action<ModelSettings> change in new Action<ModelSettings>[] {s=>s.BackMountingHoles=null!,s=>s.BackMountingHoles=[null!],s=>s.BackMountingHoles=Enumerable.Range(0,9).Select(_=>new MountingHole()).ToList(),s=>s.BackMountingHoles[0].X=double.NaN,s=>s.BackMountingHoles[0].Y=85,s=>s.BackMountingHoles[0].Diameter=1,s=>s.BackMountingHoles[0].Diameter=21,s=>s.BackMountingHoles[0]=new(){Y=42.5},s=>s.BackMountingHoles.Add(s.BackMountingHoles[0].Clone())}){var s=valid.Clone();change(s);Throws<InvalidDataException>(s.Validate);}
            using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>BackMounting.Generate(valid,false,cts.Token));valid.Shape=Shape.Flat;Require(valid.EffectivePart==ModelPart.Lithophane,"Bohrung erzwingt Rückwand in fremder Form.");Throws<InvalidDataException>(()=>BackMounting.Generate(valid,false));
        });
        Test("Bohrungen: tiefe Kopien und Format-6-Migration",()=>
        {
            var s=MountingFixture(4,true);var copy=s.Clone();copy.BackMountingHoles[0].X=1;copy.BackMountingHoles.RemoveAt(1);Require(s.BackMountingHoles.Count==4&&s.BackMountingHoles[0].X==-25,"Arbeitskopie verändert Originalprojekt während Berechnung.");
            byte[] photo=ImageProcessor.Png(ImageProcessor.Demo());string path=Path.Combine(output,"mounting-project.lithopro");var data=new ProjectData{Model=s};ProjectStore.Save(path,data,photo);var loaded=ProjectStore.Load(path);Require(loaded.Data.Version==15&&JsonSerializer.Serialize(loaded.Data)==JsonSerializer.Serialize(data)&&loaded.Image.SequenceEqual(photo),"Bohrungsparameter oder Originalfoto verändern sich.");
            string old=Path.Combine(output,"legacy-v6.lithopro");if(File.Exists(old))File.Delete(old);data.Version=6;data.Model.BackMountingHoles=[];using(var zip=ZipFile.Open(old,ZipArchiveMode.Create)){using(var stream=zip.CreateEntry("project.json").Open())JsonSerializer.Serialize(stream,data);using var image=zip.CreateEntry("source.bin").Open();image.Write(photo);}var migrated=ProjectStore.Load(old);Require(migrated.Data.Version==15&&migrated.Data.Model.BackMountingHoles.Count==0&&migrated.Data.Model.BackEnabled&&migrated.Data.Model.BackPrintOrientation==PrintOrientation.FaceUp,"Migration aktiviert Bohrungen oder verändert Rückwand.");
        });
        Test("Bohrungen behalten alle Drucklagen und fotounabhängigen Export",()=>
        {
            var s=MountingFixture(4,true);var canonical=ModelComputation.BuildExportMesh([],s,new(),CancellationToken.None).Mesh;double volume=canonical.Validate().Volume;
            foreach(var orientation in Enum.GetValues<PrintOrientation>())foreach(bool turn in new[]{false,true}){s.BackPrintOrientation=orientation;s.BackTurnOnBed90=turn;var mesh=MeshGenerator.Generate(gray,s,false);Require(mesh.Validate().IsValid&&Math.Abs(mesh.Validate().Volume-volume)<.3,"Bohrungen verlieren Topologie oder Volumen bei Drucklage.");}
        });
        Test("Editor: Bohrungen hinzufügen, speichern, entfernen und Montage prüfen",()=>
        {
            string? previous=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR");Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",Path.Combine(output,"mounting-vm"));LithoCreate.App.ViewModels.StudioViewModel? vm=null;
            try
            {
                vm=new();vm.Project.Model=MountingFixture(0,true);vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);Require(vm.TryAddBackHole()&&vm.TryAddBackHole(),"Editor ergänzt keine gültigen Bohrungen.");var update=vm.UpdatePreview();PumpUntil(()=>update.IsCompleted);Require(vm.BackHoles.Count==2&&vm.PreviewMesh!.Validate().IsValid&&vm.Dirty,"Editor zeigt Bohrungen nicht.");
                vm.IsSectionMode=true;vm.CurrentLayer=1;var slice=vm.UpdateSection();PumpUntil(()=>slice.IsCompleted);Require(vm.CurrentSection is {IsRegular:true,ClosedContours:4},"Editor verliert eine Bohrung.");vm.RefreshMountingValidation(true);Require(vm.InputError&&vm.PreviewMesh==null,"Ungültige Bohrung zeigt altes Mesh.");vm.DeleteBackHole(vm.BackHoles[0]);vm.RefreshMountingValidation(false);Require(!vm.InputError&&vm.BackHoles.Count==1,"Entfernte ungültige Bohrung bleibt aktiv.");vm.SelectedPart=LightboxBack.Parts[2];var assembly=vm.UpdatePreview();PumpUntil(()=>assembly.IsCompleted);Require(vm.IsAssemblyPart&&!vm.ExportStl.CanExecute(null),"Bohrungen umgehen Montageschutz.");
            }
            finally{if(vm!=null){vm.Dirty=false;vm.CleanShutdown();}Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",previous);}
        });
        Test("Drucksatz: zwei Einzelteile ohne Skalierung auf dem Bett",()=>
        {
            var set=PrintSetReference();set.Validate();var a=set.Parts[0];var b=set.Parts[1];Require(a.Name=="Lithophanie"&&b.Name=="Rückwand","Teilenamen fehlen.");Require(Vector3.Distance(a.Report.Size,new(40,30,3))<.001&&Vector3.Distance(b.Report.Size,new(20,10,2))<.001,"Gespeicherte Drucklage oder Maßstab verändert.");Require(Vector2.Distance(a.Position,new(30,17.5f))<.001&&Vector2.Distance(b.Position,new(40,52.5f))<.001,"Kompakte Spaltenanordnung oder Zentrierung falsch.");
            Require(a.Mesh.Bounds().Min==Vector3.Zero&&b.Mesh.Bounds().Min==Vector3.Zero,"Lokale Druckkoordinaten beginnen nicht bei null.");string file=Path.Combine(output,"PrintSet-Reference.3mf");PrintSetExporter.Export(file,set);var read=PrintSetExporter.Read(file);Require(read.Count==2&&read[1].Name=="Rückwand"&&read.All(p=>p.Report.IsValid),"Drucksatzdatei verliert Objekte oder Umlaute.");Throws<InvalidDataException>(()=>ModelExporter.Read3Mf(file));
        });
        Test("Drucksatz: Zeilenanordnung und exakte Bettgrenzen",()=>
        {
            var row=PrintSetReference(new(){BedX=80,BedY=50,BuildZ=10});Require(Vector2.Distance(row.Parts[0].Position,new(7.5f,10))<.001&&Vector2.Distance(row.Parts[1].Position,new(52.5f,20))<.001,"Zeilenanordnung falsch.");row.Validate();PrintSetExporter.Export(Path.Combine(output,"PrintSet-Row.3mf"),row);
            var exact=PrintSetReference(new(){BedX=50,BedY=55,BuildZ=3});exact.Validate();Require(exact.Parts[0].Position==new Vector2(5,5),"Exakte Bettgrenze abgewiesen.");Throws<InvalidDataException>(()=>PrintSetReference(new(){BedX=50,BedY=54.99,BuildZ=3}));Throws<InvalidDataException>(()=>PrintSetReference(new(){BedX=100,BedY=80,BuildZ=2.99}));
        });
        Test("Drucksatz: Überlappung, Montage und veränderte Daten abweisen",()=>
        {
            var set=PrintSetReference();Throws<InvalidDataException>(()=>(set with{Parts=[set.Parts[0],set.Parts[1] with{Position=set.Parts[0].Position}]}).Validate());Throws<InvalidDataException>(()=>(set with{Parts=[set.Parts[0],set.Parts[1] with{Position=new(-1,0)}]}).Validate());Throws<InvalidDataException>(()=>(set with{BedX=double.NaN}).Validate());
            var source=new Mesh{IsAssemblyPreview=true};Throws<InvalidDataException>(()=>PrintSet.Create([new("a",source),new("b",source)],new()));Throws<InvalidDataException>(()=>PrintSet.Create([new("a",new()),new("a",new())],new()));
            var part=set.Parts[1];for(int i=0;i<part.Mesh.Vertices.Count;i++)part.Mesh.Vertices[i]*=2;Throws<InvalidDataException>(()=>set.Validate());
        });
        Test("Drucksatz: Abbruch und fehlgeschlagener Export lassen die Zieldatei unverändert",()=>
        {
            var set=PrintSetReference();string file=Path.Combine(output,"printset-atomic.3mf");File.WriteAllText(file,"vorhanden");using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>PrintSetExporter.Export(file,set,cts.Token));Require(File.ReadAllText(file)=="vorhanden","Abbruch verändert Zieldatei.");
            using var during=new CancellationTokenSource();Throws<OperationCanceledException>(()=>PrintSetExporter.Export(file,set,during.Token,new ActionProgress(_=>during.Cancel())));Require(File.ReadAllText(file)=="vorhanden"&&!Directory.GetFiles(output,"printset-atomic.3mf.*.tmp").Any(),"Schreibabbruch hinterlässt Teilstand.");Throws<InvalidDataException>(()=>PrintSetExporter.Export(Path.Combine(output,"printset.stl"),set));
            var bad=set with{Parts=[set.Parts[0],set.Parts[1] with{Name="Ungültig\u0001"}]};Throws<ArgumentException>(()=>PrintSetExporter.Export(file,bad));Require(File.ReadAllText(file)=="vorhanden","XML-Fehler verändert Zieldatei.");
        });
        Test("Drucksatzleser weist defekte Einheiten, Referenzen und Transformationen ab",()=>
        {
            string source=Path.Combine(output,"PrintSet-Reference.3mf");int n=0;foreach(Func<string,string> change in new Func<string,string>[]{s=>s.Replace("millimeter","inch"),s=>s.Replace("1 0 0 0 1 0 0 0 1 ","2 0 0 0 1 0 0 0 1 "),s=>s.Replace("objectid=\"2\"","objectid=\"99\""),s=>s.Replace("id=\"2\" type=","id=\"1\" type="),s=>new System.Text.RegularExpressions.Regex("<item[^>]*/>").Replace(s,"",1)})
            {
                string target=Path.Combine(output,$"printset-invalid-{n++}.3mf");File.Copy(source,target,true);using(var zip=ZipFile.Open(target,ZipArchiveMode.Update)){var entry=zip.GetEntry("3D/3dmodel.model")!;string xml;using(var reader=new StreamReader(entry.Open()))xml=reader.ReadToEnd();entry.Delete();using var writer=new StreamWriter(zip.CreateEntry("3D/3dmodel.model").Open());writer.Write(change(xml));}Throws<InvalidDataException>(()=>PrintSetExporter.Read(target));
            }
        });
        foreach(bool socket in new[]{false,true})
        Test("Drucksatz aus echten Projektteilen: "+(socket?"Kugel/Sockel":"Kasten/Rückwand"),()=>
        {
            var settings=PrintSetFixture(socket);byte[] photo=ImageProcessor.Png(ImageProcessor.Demo());string before=JsonSerializer.Serialize(settings);var set=ModelComputation.BuildPrintSet(photo,settings,new(),new(),CancellationToken.None);set.Validate();Require(before==JsonSerializer.Serialize(settings),"Drucksatz ändert aktive Teilwahl oder Drucklagen.");
            foreach(var p in set.Parts)Require(p.Report.IsValid&&p.Mesh.Bounds().Min==Vector3.Zero,"Projektteil nicht druckbar normalisiert.");Require(set.Parts[1].Name==(socket?"Stecksockel":"Rückwand"),"Zubehör fehlt.");PrintSetExporter.Export(Path.Combine(output,socket?"PrintSet-Sphere.3mf":"PrintSet-Lightbox.3mf"),set);
            var one=settings.Clone();one.Part=ModelPart.Lithophane;var original=ModelComputation.BuildExportMesh(photo,one,new(),CancellationToken.None).Report;Require(Vector3.Distance(set.Parts[0].Report.Size,new(original.Size.X,original.Size.Z,original.Size.Y))<.001,"Körperdrucklage verändert.");using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>ModelComputation.BuildPrintSet(photo,settings,new(),new(),cts.Token));
        });
        Test("Drucksatz verwendet tiefe Geometrieschnappschüsse",()=>
        {
            var mesh=MeshGenerator.Generate(new(1,1,[255]),new(){Width=40,Height=30,FrameWidth=0,PrintOrientation=PrintOrientation.FaceUp},false);var vertices=mesh.Vertices.ToArray();var set=PrintSet.Create([new("a",mesh),new("b",mesh)],new());Require(mesh.Vertices.SequenceEqual(vertices),"Anordnung verändert Eingabemesh.");mesh.Vertices.Clear();set.Validate();Require(set.Parts.All(p=>p.Mesh.Vertices.Count>0),"Drucksatz teilt veränderliche Eingabegeometrie.");
            Throws<InvalidDataException>(()=>ModelComputation.BuildPrintSet([],new(),new(),new(),CancellationToken.None));
        });
        Test("Druckbettübersicht rendert zwei Teile und passt in kleine Fenster",()=>
        {
            var view=new LithoCreate.App.Controls.PrintSetViewport{PrintSet=PrintSetReference()};foreach(var size in new[]{new System.Windows.Size(700,420),new System.Windows.Size(340,180)}){view.Measure(size);view.Arrange(new System.Windows.Rect(size));view.UpdateLayout();var bitmap=new RenderTargetBitmap((int)size.Width,(int)size.Height,96,96,PixelFormats.Pbgra32);bitmap.Render(view);byte[] pixels=new byte[(int)size.Width*(int)size.Height*4];bitmap.CopyPixels(pixels,(int)size.Width*4,0);Require(Enumerable.Range(0,pixels.Length/4).Count(i=>pixels[4*i+1]>60)>500,"Bett und Objekte nicht sichtbar.");File.WriteAllBytes(Path.Combine(output,$"printset-bed-{size.Width}.png"),ImageProcessor.Png(bitmap));}
        });
        Test("Editor: Drucksatzmodi, Cache, Abbruch und unpassendes Bett",()=>
        {
            string? previous=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR");Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",Path.Combine(output,"printset-vm"));LithoCreate.App.ViewModels.StudioViewModel? vm=null;
            try
            {
                vm=new();vm.Project.Model=PrintSetFixture(false);vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);var preview=vm.UpdatePreview();PumpUntil(()=>preview.IsCompleted);vm.Dirty=false;var build=vm.BuildPrintSet();PumpUntil(()=>build.IsCompleted);Require(vm.HasPrintSet&&vm.IsPrintSetMode&&!vm.IsSurfaceMode&&!vm.IsSectionMode&&!vm.Dirty,"Drucksatzmodus oder Projektstatus falsch: "+vm.PrintSetInfo);vm.PreparedPrintSet!.Validate();Require(vm.ExportPrintSet.CanExecute(null)&&vm.ExportPng.CanExecute(null),"Drucksatzexport nicht erreichbar.");
                vm.SetViewMode.Execute("surface");Require(vm.IsSurfaceMode&&!vm.IsPrintSetMode&&vm.HasPrintSet,"Rückkehr zur Montage verliert unveränderten Drucksatz.");vm.Model.BackMountingHoles[0].X+=.5;vm.Changed();Require(!vm.HasPrintSet,"Geänderte Bohrung behält alten Drucksatz.");var cancelled=vm.BuildPrintSet();vm.Changed();PumpUntil(()=>cancelled.IsCompleted);Require(!vm.HasPrintSet&&!vm.Busy,"Abgebrochener Drucksatz bleibt exportierbar.");
                vm.Printer.BedX=20;vm.Printer.BedY=20;vm.Changed();var small=vm.BuildPrintSet();PumpUntil(()=>small.IsCompleted);Require(!vm.HasPrintSet&&!vm.Busy&&!vm.ExportPrintSet.CanExecute(null)&&vm.PrintSetInfo.Contains("nicht gemeinsam"),"Unpassender Drucksatz wird angeboten.");vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Flat);Require(!vm.IsPrintSetMode&&!vm.HasPrintSet,"Form ohne Zubehör behält Drucksatzmodus.");
            }
            finally{if(vm!=null){vm.Dirty=false;vm.CleanShutdown();}Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",previous);}
        });
        foreach(var test in new (string Name,PrintAngles Angles)[]{("Base",new PrintAngles()),("X",new PrintAngles(30,0,0)),("Y",new PrintAngles(0,30,0)),("Z",new PrintAngles(0,0,45)),("XYZ",new PrintAngles(23,-17,38)),("XYZ-Bed90",new PrintAngles(23,-17,38))})
        Test("Freie Druckwinkel und Datei-Roundtrip: "+test.Name,()=>
        {
            var settings=new ModelSettings{Width=40,Height=30,FrameWidth=0,MinThickness=3,MaxThickness=3,Resolution=1,Angles=test.Angles,TurnOnBed90=test.Name.EndsWith("Bed90")};var mesh=MeshGenerator.Generate(gray,settings,false);var report=mesh.Validate();Require(report.IsValid&&Math.Abs(report.Volume-3600)<.15,"Rotation verändert Topologie oder Volumen.");CheckRoundtrip(mesh,"Angles-"+test.Name);
            var center=(report.Bounds.Min+report.Bounds.Max)/2;var expectedCenter=settings.TurnOnBed90?new Vector3(1.5f,15,0):new Vector3(0,15,1.5f);Require(Vector3.Distance(center,expectedCenter)<.001,"Freie Rotation verschiebt die Modellmitte.");
            Vector3 size=new(report.Size.X,report.Size.Z,report.Size.Y);double c=Math.Cos(Math.PI/6),si=.5;
            if(test.Name=="X")Require(Vector3.Distance(size,new(40,(float)(3*c+30*si),(float)(3*si+30*c)))<.001,"X ist nicht die Druckachse.");if(test.Name=="Y")Require(Vector3.Distance(size,new((float)(40*c+30*si),3,(float)(40*si+30*c)))<.001,"Y ist nicht die Druckachse.");if(test.Name=="Z")Require(Vector3.Distance(size,new((float)(43/Math.Sqrt(2)),(float)(43/Math.Sqrt(2)),30))<.001,"Z ist nicht die senkrechte Druckachse.");
        });
        Test("Freie Druckwinkel: Achsreihenfolge, Pivot und Grenzen",()=>
        {
            Require(Vector3.Distance(PrintPlacement.RotateInPrintAxes(new(1,0,0),new(90,90,90)),new(0,-1,0))<.00001,"Drehreihenfolge ist nicht X, Y, Z.");Require(Vector3.Distance(PrintPlacement.RotateInPrintAxes(new(0,1,0),new(90,0,0)),new(0,0,1))<.00001,"X-Drehsinn falsch.");var pivot=new Vector3(17,-3,8);Require(PrintPlacement.RotateInPrintAxes(pivot,new(31,22,-17),pivot)==pivot,"Pivot bewegt sich.");
            foreach(var angles in new[]{new PrintAngles(181,0,0),new(0,-181,0),new(0,0,double.NaN),new(double.PositiveInfinity,0,0)})Throws<InvalidDataException>(()=>angles.Validate());var settings=new ModelSettings{SocketAngles=new(0,181,0)};Throws<InvalidDataException>(settings.Validate);settings.SocketAngles=default;settings.BackAngles=new(0,0,-181);Throws<InvalidDataException>(settings.Validate);
            var mesh=MeshGenerator.Generate(gray,new(){Width=40,Height=30,FrameWidth=0},false);var original=mesh.Vertices.ToArray();using var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>PrintPlacement.Apply(mesh,PrintOrientation.FaceUp,true,cts.Token,new(10,20,30)));Require(mesh.Vertices.SequenceEqual(original),"Abgebrochene Rotation verändert Mesh.");
        });
        Test("Freie Druckwinkel erhalten alle Formen und Foto-UVs",()=>
        {
            foreach(var shape in SingleBodyShapes())
            {
                var s=new ModelSettings{Shape=shape,Width=48,Height=36,Diameter=40,Radius=40,FrameWidth=2,Resolution=1};var original=MeshGenerator.Generate(gray,s,false);s.Angles=new(23,-17,38);var rotated=MeshGenerator.Generate(gray,s,false);var report=rotated.Validate();Require(report.IsValid&&Math.Abs(report.Volume-original.Validate().Volume)<Math.Max(.2,report.Volume*.00003),"Form bei Rotation ungültig: "+shape);Require(rotated.Triangles.SequenceEqual(original.Triangles)&&rotated.TextureCoordinates.SequenceEqual(original.TextureCoordinates)&&rotated.PhotoVertices.SequenceEqual(original.PhotoVertices),"Foto oder Topologie bei Rotation verändert: "+shape);
            }
        });
        Test("Freie Druckwinkel: Suche erhält passende echte Kontur und setzt nur bei Bedarf zurück",()=>
        {
            var disc=MeshGenerator.Generate(gray,new(){Shape=Shape.Disc,Diameter=40,FrameWidth=0,MinThickness=3,MaxThickness=3,Resolution=1},false);var angles=new PrintAngles(0,0,45);var retain=PrintPlacement.Find(disc,new(){BedX=42,BedY=42,BuildZ=10},PrintOrientation.FaceUp,false,currentAngles:angles);Require(retain is {Orientation:PrintOrientation.FaceUp}&&retain.Value.Angles==angles,"Suche verwendet gedrehte Hüllrechteck-Ecken statt der tatsächlichen Kreisgeometrie.");
            var box=MeshGenerator.Generate(gray,new(){Width=40,Height=30,FrameWidth=0,MinThickness=3,MaxThickness=3,Resolution=1},false);var fallback=PrintPlacement.Find(box,new(){BedX=50,BedY=40,BuildZ=5},currentAngles:new(0,30,0));Require(fallback is {Orientation:PrintOrientation.FaceUp}&&fallback.Value.Angles.IsZero,"Suche findet keine flache Lage ohne Zusatzwinkel.");
        });
        Test("Format 7 migriert geometrisch identisch; Format 13 erhält unabhängige Winkel",()=>
        {
            var data=new ProjectData{Version=7,Model=new(){Shape=Shape.Lightbox,Width=100,Height=85,BackEnabled=true,Part=ModelPart.BackPanel,PrintOrientation=PrintOrientation.FaceDown,TurnOnBed90=true,BackMountingHoles=MountingFixture(4,true).BackMountingHoles}};byte[] photo=ImageProcessor.Png(ImageProcessor.Demo());var original=MeshGenerator.Generate(gray,data.Model,false);string old=Path.Combine(output,"legacy-v7.lithopro");if(File.Exists(old))File.Delete(old);using(var zip=ZipFile.Open(old,ZipArchiveMode.Create)){using(var stream=zip.CreateEntry("project.json").Open())JsonSerializer.Serialize(stream,data);using var image=zip.CreateEntry("source.bin").Open();image.Write(photo);}var loaded=ProjectStore.Load(old);var after=MeshGenerator.Generate(gray,loaded.Data.Model,false);Require(loaded.Data.Version==15&&loaded.Data.Model.Angles.IsZero&&loaded.Data.Model.SocketAngles.IsZero&&loaded.Data.Model.BackAngles.IsZero&&original.Vertices.SequenceEqual(after.Vertices)&&original.Triangles.SequenceEqual(after.Triangles),"Migration verändert alte Druckgeometrie.");
            loaded.Data.Model.Angles=new(17.5,-12.5,31);loaded.Data.Model.SocketAngles=new(-18,2,3);loaded.Data.Model.BackAngles=new(25,9,-11);string next=Path.Combine(output,"angles-project.lithopro");ProjectStore.Save(next,loaded.Data,loaded.Image);var saved=ProjectStore.Load(next);Require(JsonSerializer.Serialize(saved.Data)==JsonSerializer.Serialize(loaded.Data)&&saved.Image.SequenceEqual(photo),"Winkel oder Originalbytes verändert.");
        });
        foreach(bool socket in new[]{false,true})
        Test("Freie Einzelteilwinkel und Drucksatz: "+(socket?"Sockel":"Rückwand"),()=>
        {
            var s=PrintSetFixture(socket);s.Part=ModelPart.Assembly;var assembly=MeshGenerator.Generate(gray,s,false);s.Angles=new(12,-9,7);if(socket)s.SocketAngles=new(15,5,-8);else s.BackAngles=new(25,0,18);var unchanged=MeshGenerator.Generate(gray,s,false);Require(assembly.Vertices.SequenceEqual(unchanged.Vertices),"Montage übernimmt Druckwinkel.");
            byte[] photo=ImageProcessor.Png(ImageProcessor.Demo());var set=ModelComputation.BuildPrintSet(photo,s,new(),new(),CancellationToken.None);set.Validate();foreach(var part in set.Parts)Require(part.Report.IsValid,"Drucksatzteil verliert Topologie.");s.Part=socket?ModelPart.Socket:ModelPart.BackPanel;var single=ModelComputation.BuildExportMesh([],s,new(),CancellationToken.None).Report;Require(Vector3.Distance(set.Parts[1].Report.Size,new(single.Size.X,single.Size.Z,single.Size.Y))<.001,"Drucksatz ignoriert Zubehörwinkel.");PrintSetExporter.Export(Path.Combine(output,socket?"Angles-Set-Sphere.3mf":"Angles-Set-Lightbox.3mf"),set);
        });
        Test("Editor: freie Winkel, Rücksetzen und Ausrichtungssuche gelten nur für das aktive Teil",()=>
        {
            string? previous=Environment.GetEnvironmentVariable("LITHOCREATE_DATA_DIR");Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",Path.Combine(output,"angles-vm"));LithoCreate.App.ViewModels.StudioViewModel? vm=null;
            try
            {
                vm=new();vm.Project.Model=MountingFixture(4,true);vm.Model.Angles=new(9,8,7);vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);vm.AngleX=45;Require(vm.Model.BackAngles==new PrintAngles(45,0,0)&&vm.Model.Angles==new PrintAngles(9,8,7),"Winkel landet am falschen Teil.");var preview=vm.UpdatePreview();PumpUntil(()=>preview.IsCompleted);Require(vm.PreviewMesh!=null&&vm.PlacementSummary.Contains("45"),"Winkelvorschau fehlt.");vm.Printer.BedX=110;vm.Printer.BedY=100;vm.Printer.BuildZ=6;var fit=vm.FitPlacement();PumpUntil(()=>fit.IsCompleted);Require(vm.Model.BackAngles.IsZero&&vm.Model.Angles==new PrintAngles(9,8,7)&&vm.Model.BackPrintOrientation==PrintOrientation.FaceUp,"Suche setzt falsche Teilwinkel zurück.");
                vm.AngleY=-17.5;vm.ResetAngles.Execute(null);Require(vm.Model.BackAngles.IsZero&&vm.AngleY==0,"Rücksetzen verändert nicht das aktive Teil.");vm.SelectedPart=LightboxBack.Parts[0];Require(vm.AngleX==9&&vm.AngleY==8&&vm.AngleZ==7,"Teilwechsel zeigt falsche Winkel.");
            }
            finally{if(vm!=null){vm.Dirty=false;vm.CleanShutdown();}Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",previous);}
        });
        TestSlots(gray);
        TestSinks(gray);
        TestCropRatios();
        TestDimensions();
        TestModelPresets();
        TestHangingTabs();
        TestPrintLayout();
        TestManualPrintLayout();
        TestPrintLayoutHistory();
        TestPrinterProfiles();
        TestExtensions();
        TestLampShades();
        TestNewDesigns();
        Console.WriteLine($"ERGEBNIS: {passed} bestanden, {failed} fehlgeschlagen; {clock.Elapsed.TotalSeconds:N1} s; Peak Working Set {Process.GetCurrentProcess().PeakWorkingSet64/1024/1024} MB");return failed==0?0:1;
    }
    sealed class ActionProgress(Action<double> action):IProgress<double>{public void Report(double value)=>action(value);}
    static PrintSet PrintSetReference(PrinterProfile? printer=null,PrintSetOptions? options=null)
    {
        var a=MeshGenerator.Generate(new(1,1,[255]),new(){Width=40,Height=30,FrameWidth=0,MinThickness=3,MaxThickness=3,Resolution=1,PrintOrientation=PrintOrientation.FaceUp},false);var b=MeshGenerator.Generate(new(1,1,[255]),new(){Width=20,Height=10,FrameWidth=0,MinThickness=2,MaxThickness=2,Resolution=1,PrintOrientation=PrintOrientation.FaceUp},false);return PrintSet.Create([new("Lithophanie",a),new("Rückwand",b)],printer??new(){BedX=100,BedY=80,BuildZ=50},options:options);
    }
    static ModelSettings PrintSetFixture(bool socket)=>socket?new(){Shape=Shape.Sphere,Diameter=60,SphereOpeningDiameter=30,SocketEnabled=true,Part=ModelPart.Assembly,SocketPrintOrientation=PrintOrientation.FaceUp,Resolution=1}:new(){Shape=Shape.Lightbox,Width=48,Height=40,BoxDepth=12,BackEnabled=true,Part=ModelPart.Assembly,PrintOrientation=PrintOrientation.FaceUp,BackTurnOnBed90=true,Resolution=1,BackMountingHoles=[new(){X=-10,Y=29},new(){X=10,Y=29},new(){X=-10,Y=11},new(){X=10,Y=11}]};
    static IEnumerable<System.Windows.DependencyObject> VisualChildren(System.Windows.DependencyObject root){for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);yield return child;foreach(var next in VisualChildren(child))yield return next;}}
    static ModelSettings MountingFixture(int count,bool cable)=>new(){Shape=Shape.Lightbox,Width=100,Height=85,FrameWidth=3,BoxDepth=20,BackEnabled=true,Part=ModelPart.BackPanel,Resolution=.5,BackHoleDiameter=cable?8:0,BackMountingHoles=new (double X,double Y)[]{(-25,63.75),(25,63.75),(-25,21.25),(25,21.25),(-25,42.5),(25,42.5),(0,21.25),(0,63.75)}.Take(count).Select(p=>new MountingHole{X=p.X,Y=p.Y}).ToList()};
    static BitmapSource GrayBitmap(int width,int height,byte[] gray)
    {
        byte[] pixels=new byte[width*height*4];for(int i=0;i<gray.Length;i++){pixels[i*4]=pixels[i*4+1]=pixels[i*4+2]=gray[i];pixels[i*4+3]=255;}var image=BitmapSource.Create(width,height,96,96,PixelFormats.Bgra32,null,pixels,width*4);image.Freeze();return image;
    }
    static byte[] ExifFixture(BitmapSource source,int orientation,bool jpeg=false)
    {
        var metadata=new BitmapMetadata(jpeg?"jpg":"tiff");metadata.SetQuery(jpeg?"/app1/ifd/{ushort=274}":"/ifd/{ushort=274}",(ushort)orientation);BitmapEncoder encoder=jpeg?new JpegBitmapEncoder{QualityLevel=100}:new TiffBitmapEncoder{Compression=TiffCompressOption.Zip};encoder.Frames.Add(BitmapFrame.Create(source,null,metadata,null));using var stream=new MemoryStream();encoder.Save(stream);return stream.ToArray();
    }
    static void Test(string name,Action action){try{action();passed++;Console.WriteLine("PASS "+name);}catch(Exception ex){failed++;Console.WriteLine("FAIL "+name+": "+ex);}}
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Throws<T>(Action action) where T:Exception{try{action();}catch(T){return;}throw new Exception("Erwartete Ausnahme: "+typeof(T).Name);}
    static int Euler(Mesh mesh)=>mesh.Vertices.Count-mesh.Triangles.Count/2;
    static void CheckRoundtrip(Mesh mesh,string name)
    {
        Require(mesh.Validate().IsValid,"Ungültiges Mesh: "+mesh.Validate());
        foreach(string ext in new[]{"stl","3mf"}){string path=Path.Combine(output,name+"."+ext);ModelExporter.Export(path,mesh);Require((ext=="stl"?ModelExporter.ReadStl(path):ModelExporter.Read3Mf(path)).Validate().IsValid,"Datei-Roundtrip fehlgeschlagen.");}
    }
    static int VerticalIntersections(Mesh mesh,double x,double z)
    {
        var intersections=new List<double>();
        foreach(var t in mesh.Triangles)
        {
            var a=mesh.Vertices[t.A];var b=mesh.Vertices[t.B];var c=mesh.Vertices[t.C];
            double denominator=(b.Z-c.Z)*(a.X-c.X)+(c.X-b.X)*(a.Z-c.Z);if(Math.Abs(denominator)<1e-10)continue;
            double u=((b.Z-c.Z)*(x-c.X)+(c.X-b.X)*(z-c.Z))/denominator;
            double v=((c.Z-a.Z)*(x-c.X)+(a.X-c.X)*(z-c.Z))/denominator;
            if(u>=0&&v>=0&&u+v<=1){double y=u*a.Y+v*b.Y+(1-u-v)*c.Y;if(!intersections.Any(h=>Math.Abs(h-y)<1e-4))intersections.Add(y);}
        }
        return intersections.Count;
    }
    static double MinimumClearRadius(Mesh mesh,double y)
    {
        double closest=double.MaxValue;
        foreach(var t in mesh.Triangles)foreach(var edge in new[]{(t.A,t.B),(t.B,t.C),(t.C,t.A)})
        {
            var a=mesh.Vertices[edge.Item1];var b=mesh.Vertices[edge.Item2];if(Math.Abs(a.Y-y)>1e-4||Math.Abs(b.Y-y)>1e-4)continue;
            double dx=b.X-a.X,dz=b.Z-a.Z,length=dx*dx+dz*dz;if(length<1e-12)continue;
            double alpha=Math.Clamp(-(a.X*dx+a.Z*dz)/length,0,1);double x=a.X+alpha*dx,z=a.Z+alpha*dz;closest=Math.Min(closest,Math.Sqrt(x*x+z*z));
        }
        return closest;
    }
    static void PumpUntil(Func<bool> condition)
    {
        var frame=new System.Windows.Threading.DispatcherFrame();var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(30)};var timeout=Stopwatch.StartNew();timer.Tick+=(_,_)=>{if(condition()||timeout.Elapsed.TotalSeconds>20)frame.Continue=false;};timer.Start();System.Windows.Threading.Dispatcher.PushFrame(frame);timer.Stop();Require(condition(),"Zeitüberschreitung bei der Hintergrundvorschau.");
    }
    static int RenderThumbnails(string directory)
    {
        Directory.CreateDirectory(directory);var source=ImageProcessor.Process(ImageProcessor.Demo(),new());
        foreach(var shape in Templates.All)
        {
            var settings=new ModelSettings{Shape=shape.Shape,Width=100,Height=90,Diameter=85,FrameWidth=4,Resolution=1};
            if(settings.IsShade) LampShade.ApplyTemplate(settings);
            if(settings.HasDesignParts||settings.IsArch||settings.Shape==Shape.WavePicture) DesignParts.ApplyTemplate(settings);
            var mesh=MeshGenerator.Generate(source.Gray,settings,true);
            var lighting=settings.HasWallMotifs?WallImages.Atlas(source.Gray,new GrayImage?[4],default):source.Gray;
            var viewport=new LithoCreate.App.Controls.ModelViewport();viewport.SetMesh(mesh,lighting,true);viewport.View("grid");viewport.Measure(new System.Windows.Size(300,210));viewport.Arrange(new System.Windows.Rect(0,0,300,210));viewport.UpdateLayout();System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);
            var bitmap=new RenderTargetBitmap(300,210,96,96,PixelFormats.Pbgra32);bitmap.Render(viewport);File.WriteAllBytes(Path.Combine(directory,shape.Shape+".png"),ImageProcessor.Png(bitmap));
        }
        Console.WriteLine($"{Templates.All.Length} Formvorschauen gerendert.");return 0;
    }
    static int RenderInterface(string directory)
    {
        Directory.CreateDirectory(directory);Environment.SetEnvironmentVariable("LITHOCREATE_DATA_DIR",Path.Combine(Path.GetFullPath(directory),"data"));
        var app=new System.Windows.Application();app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary{Source=new Uri("pack://application:,,,/LithoCreate Pro;component/Theme.xaml")});
        var window=new LithoCreate.App.MainWindow();window.Show();var vm=(LithoCreate.App.ViewModels.StudioViewModel)window.DataContext;
        var task=vm.UpdatePreview();var frame=new System.Windows.Threading.DispatcherFrame();var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(50)};var timeout=Stopwatch.StartNew();timer.Tick+=(_,_)=>{if(vm.PreviewMesh!=null||timeout.Elapsed.TotalSeconds>20)frame.Continue=false;};timer.Start();System.Windows.Threading.Dispatcher.PushFrame(frame);timer.Stop();Require(vm.PreviewMesh!=null,"Keine Meshvorschau für UI-Prüfung erzeugt.");
        foreach(var size in new[]{new System.Windows.Size(1400,900),new System.Windows.Size(960,640)})foreach(string page in new[]{"Home","Editor","Templates","Settings","Help"})
        {
            window.Width=size.Width;window.Height=size.Height;vm.Page=page;vm.Step=page=="Editor"?5:1;window.UpdateLayout();
            var bitmap=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(window);File.WriteAllBytes(Path.Combine(directory,$"{page}-{size.Width}.png"),ImageProcessor.Png(bitmap));
        }
        foreach(var size in new[]{new System.Windows.Size(1400,900),new System.Windows.Size(960,640)})
        {
            window.Width=size.Width;window.Height=size.Height;vm.Page="Editor";vm.Step=1;Capture("Foto");
            var oldPreview=vm.PreviewMesh;vm.Edits.StraightenAngle=-7.5;vm.Edits.Brightness=.18;vm.Edits.Contrast=1.35;vm.Changed();vm.Undo.Execute(null);vm.Redo.Execute(null);_ =vm.UpdatePreview();PumpUntil(()=>vm.PreviewMesh!=null&&!ReferenceEquals(vm.PreviewMesh,oldPreview));vm.CompareEnabled=true;
            var compare=(LithoCreate.App.Controls.PhotoComparison)window.FindName("PhotoCompare");foreach(double ratio in new[]{.3,.5,.75}){compare.SplitRatio=ratio;Capture("Fotovergleich-"+(int)(ratio*100));}
            var angleBox=(System.Windows.Controls.TextBox)window.FindName("StraightenAngleBox");var angleSlider=(System.Windows.Controls.Slider)window.FindName("StraightenSlider");angleBox.Text="12,5";window.UpdateLayout();Require(vm.StraightenAngle==12.5&&Math.Abs(angleSlider.Value-12.5)<.0001,"Winkeleingabe aktualisiert den Regler nicht.");angleSlider.Value=-6.5;window.UpdateLayout();Require(vm.StraightenAngle==-6.5&&double.Parse(angleBox.Text,System.Globalization.CultureInfo.GetCultureInfo("de-DE"))==-6.5,"Regler aktualisiert das Winkelfeld nicht.");vm.Undo.Execute(null);window.UpdateLayout();Require(vm.StraightenAngle==12.5&&angleSlider.Value==12.5,"Undo aktualisiert die Winkeleingaben nicht.");
            vm.ShowOriginal=true;Require(!vm.IsPhotoComparison,"Vergleich überlagert Originalauswahl.");vm.ShowOriginal=false;vm.CompareEnabled=false;vm.ResetImage.Execute(null);var resetTask=vm.UpdatePreview();PumpUntil(()=>resetTask.IsCompleted);
            foreach(var variant in new[]{("Standfuß",Shape.Flat),("Lampenboden",Shape.Cylinder),("Kugelöffnung",Shape.Sphere)})
            {
                var before=vm.PreviewMesh;
                vm.Project.Model=new ModelSettings{Shape=variant.Item2,Width=100,Height=85,Diameter=85,StandEnabled=true,StandHeight=7,StandDepth=26,LampBaseThickness=3,LampBaseHoleDiameter=8,SphereOpeningDiameter=34,SphereCollarHeight=4,SphereVentDiameter=4};
                vm.SelectedShape=Templates.All.First(t=>t.Shape==variant.Item2);vm.Step=3;_ =vm.UpdatePreview();PumpUntil(()=>vm.PreviewMesh!=null&&!ReferenceEquals(vm.PreviewMesh,before));vm.Camera.Execute("reset");Capture(variant.Item1);
                if(variant.Item2==Shape.Sphere){vm.Camera.Execute("grid");vm.Camera.Execute("bottom");Capture("Kugelöffnung-unten");vm.Camera.Execute("grid");}
                vm.IsSectionMode=true;vm.CurrentLayer=1;var firstTask=vm.UpdateSection();PumpUntil(()=>firstTask.IsCompleted);firstTask.GetAwaiter().GetResult();Capture(variant.Item1+"-Schicht-unten");
                vm.CurrentLayer=vm.TotalLayers/2;var midTask=vm.UpdateSection();PumpUntil(()=>midTask.IsCompleted);midTask.GetAwaiter().GetResult();Capture(variant.Item1+"-Schicht-mitte");
                if(variant.Item2==Shape.Cylinder){vm.InspectFullQuality.Execute(null);PumpUntil(()=>!vm.Busy);Require(vm.HasFullInspection,"Prüfmodell fehlt im UI.");var fullTask=vm.UpdateSection();PumpUntil(()=>fullTask.IsCompleted);fullTask.GetAwaiter().GetResult();Capture("Exportauflösung-Schicht");}
                if(variant.Item2==Shape.Cylinder){var input=(System.Windows.Controls.TextBox)window.FindName("SliceLayerHeightBox");input.Text="abc";PumpUntil(()=>vm.CurrentSection==null&&vm.SliceLayerError.Length>0);Capture("Ungültige-Schichthöhe");input.Text="0,12";PumpUntil(()=>vm.CurrentSection!=null);}
                vm.IsSectionMode=false;
            }
            vm.Project.Model=new ModelSettings{Width=100,Height=65,Resolution=1,FrameWidth=0};vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Flat);vm.Step=3;((System.Windows.Controls.Expander)window.FindName("PlacementPanel")).IsExpanded=true;
            foreach(var orientation in new[]{PrintOrientation.FaceUp,PrintOrientation.RightEdgeDown}){vm.SelectedPlacement=vm.Placements.First(p=>p.Orientation==orientation);var placed=vm.UpdatePreview();PumpUntil(()=>placed.IsCompleted);vm.Camera.Execute("reset");Capture("Drucklage-"+orientation);if(orientation==PrintOrientation.FaceUp){vm.IsSectionMode=true;vm.CurrentLayer=1;var section=vm.UpdateSection();PumpUntil(()=>section.IsCompleted);Capture("Drucklage-flach-Schicht");vm.IsSectionMode=false;}}
            vm.SelectedPlacement=vm.Placements[0];vm.Printer.BedX=80;vm.Printer.BedY=110;vm.Printer.BuildZ=90;var initial=vm.UpdatePreview();PumpUntil(()=>initial.IsCompleted);var fit=vm.FitPlacement();PumpUntil(()=>fit.IsCompleted);Require(vm.Model.PrintOrientation==PrintOrientation.Upright&&vm.TurnOnBed90,"UI-Suche findet Bettrotation nicht.");var fitted=vm.UpdatePreview();PumpUntil(()=>fitted.IsCompleted);Capture("Drucklage-Suche");vm.TurnOnBed90=false;((System.Windows.Controls.Expander)window.FindName("PlacementPanel")).IsExpanded=false;
            vm.Project.Model=new(){Shape=Shape.Sphere,Diameter=85,SphereOpeningDiameter=34,SphereCollarHeight=4,SphereVentDiameter=4,SocketEnabled=true,Resolution=1};vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Sphere);var bodyTask=vm.UpdatePreview();PumpUntil(()=>bodyTask.IsCompleted);Capture("Sockel-Kugel");
            vm.SelectedPart=SphereSocket.Parts[1];var socketTask=vm.UpdatePreview();PumpUntil(()=>socketTask.IsCompleted);Capture("Sockel-Einzeln");vm.Camera.Execute("bottom");Capture("Sockel-Unterseite");vm.Camera.Execute("reset");vm.IsSectionMode=true;vm.CurrentLayer=1;var socketSection=vm.UpdateSection();PumpUntil(()=>socketSection.IsCompleted);Capture("Sockel-Schicht");
            vm.SelectedPart=SphereSocket.Parts[2];var assemblyTask=vm.UpdatePreview();PumpUntil(()=>assemblyTask.IsCompleted);Capture("Sockel-Montage");vm.Step=5;Capture("Sockel-Montage-Export");Require(!vm.ExportStl.CanExecute(null)&&!vm.IsSectionMode,"Montage bietet Druckausgabe an.");vm.Step=3;vm.SocketEnabled=false;
            vm.Project.Model=new(){Shape=Shape.Lightbox,Width=100,Height=85,BackEnabled=true,Resolution=1};vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);vm.SelectedPart=LightboxBack.Parts[0];var boxTask=vm.UpdatePreview();PumpUntil(()=>boxTask.IsCompleted);vm.Camera.Execute("back");Capture("Rückwand-Aufnahme");Require(((System.Windows.Controls.ComboBox)window.FindName("PartCombo")).SelectedValue is ModelPart.Lithophane,"Kastenteil ist im Auswahlfeld leer.");
            vm.SelectedPart=LightboxBack.Parts[1];var backTask=vm.UpdatePreview();PumpUntil(()=>backTask.IsCompleted);Capture("Rückwand-Einzeln");vm.IsSectionMode=true;vm.CurrentLayer=1;var backSection=vm.UpdateSection();PumpUntil(()=>backSection.IsCompleted);Capture("Rückwand-Schicht");vm.SelectedPart=LightboxBack.Parts[2];var backAssembly=vm.UpdatePreview();PumpUntil(()=>backAssembly.IsCompleted);vm.Camera.Execute("back");Capture("Rückwand-Montage");vm.Step=5;Capture("Rückwand-Montage-Export");vm.Step=3;

            vm.Project.Model=MountingFixture(4,true);vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);var mountingTask=vm.UpdatePreview();PumpUntil(()=>mountingTask.IsCompleted);((System.Windows.Controls.Expander)window.FindName("BackPanelOptions")).IsExpanded=false;var mountingPanel=(System.Windows.Controls.Expander)window.FindName("MountingPanel");mountingPanel.IsExpanded=true;window.UpdateLayout();mountingPanel.BringIntoView();Capture("Bohrungen-Editor");
            vm.IsSectionMode=true;vm.CurrentLayer=1;var mountingSection=vm.UpdateSection();PumpUntil(()=>mountingSection.IsCompleted);Require(vm.CurrentSection is {ClosedContours:6,IsRegular:true},"UI verliert Befestigungsbohrungen.");Capture("Bohrungen-Schicht");vm.IsSectionMode=false;
            var holeItems=(System.Windows.Controls.ItemsControl)window.FindName("MountingHoleItems");window.UpdateLayout();var holeBox=VisualChildren(holeItems).OfType<System.Windows.Controls.TextBox>().First(t=>ReferenceEquals(t.DataContext,vm.BackHoles[0])&&t.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.ParentBinding.Path.Path=="X");holeBox.BringIntoView();holeBox.Focus();holeBox.Text="abc";PumpUntil(()=>vm.InputError&&vm.PreviewMesh==null);var blocked=vm.UpdatePreview();PumpUntil(()=>blocked.IsCompleted);Require(vm.PreviewMesh==null&&!vm.ExportStl.CanExecute(null)&&!vm.Export3Mf.CanExecute(null),"Fehlerhafte Bohrung ermöglicht Export oder altes Mesh.");Capture("Bohrungen-Ungueltige-Zahl");
            holeBox.Text="1000";PumpUntil(()=>!vm.InputError);var outside=vm.UpdatePreview();PumpUntil(()=>outside.IsCompleted);Require(vm.PreviewMesh==null,"Bohrung außerhalb der Rückwand bleibt sichtbar.");Capture("Bohrungen-Randfehler");holeBox.Text="-25";PumpUntil(()=>!vm.InputError&&vm.PreviewMesh!=null);Capture("Bohrungen-Korrigiert");
            holeBox.Text="abc";PumpUntil(()=>vm.InputError);vm.DeleteBackHole(vm.BackHoles[0]);PumpUntil(()=>!vm.InputError);var afterDelete=vm.UpdatePreview();PumpUntil(()=>afterDelete.IsCompleted);Require(vm.BackHoles.Count==3&&vm.PreviewMesh!=null,"Entfernen fehlerhafter Zeile lässt Validierungsfehler zurück.");
            vm.SelectedPart=LightboxBack.Parts[2];var mountingAssembly=vm.UpdatePreview();PumpUntil(()=>mountingAssembly.IsCompleted);vm.Camera.Execute("back");Capture("Bohrungen-Montage");
            vm.Project.Model=PrintSetFixture(false);vm.Printer.BedX=120;vm.Printer.BedY=120;vm.Printer.BuildZ=100;vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);var planPreview=vm.UpdatePreview();PumpUntil(()=>planPreview.IsCompleted);var plan=vm.BuildPrintSet();PumpUntil(()=>plan.IsCompleted);Require(vm.HasPrintSet&&vm.IsPrintSetMode,"UI-Drucksatz fehlt: "+vm.PrintSetInfo);Capture("Drucksatz-Kasten");vm.Step=5;window.UpdateLayout();((System.Windows.Controls.StackPanel)window.FindName("ExportPanel")).BringIntoView();Capture("Drucksatz-Export");vm.Step=3;vm.Printer.BedX=25;vm.Printer.BedY=25;vm.Changed();var noFit=vm.BuildPrintSet();PumpUntil(()=>noFit.IsCompleted);Require(!vm.HasPrintSet,"UI lässt unpassenden Drucksatz zu.");Capture("Drucksatz-Zu-kleines-Bett");
            vm.Project.Model=PrintSetFixture(true);vm.Printer.BedX=220;vm.Printer.BedY=220;vm.Printer.BuildZ=150;vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Sphere);var spherePlan=vm.BuildPrintSet();PumpUntil(()=>spherePlan.IsCompleted);Require(vm.HasPrintSet,"Sockel-Drucksatz fehlt.");Capture("Drucksatz-Kugel");vm.SetViewMode.Execute("surface");
            vm.Project.Model=new(){Width=100,Height=70,FrameWidth=3,Resolution=1,PrintOrientation=PrintOrientation.FaceUp,Angles=new(25,-12.5,18)};vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Flat);vm.Step=3;var tilted=vm.UpdatePreview();PumpUntil(()=>tilted.IsCompleted);((System.Windows.Controls.Expander)window.FindName("PlacementPanel")).IsExpanded=true;window.UpdateLayout();((System.Windows.Controls.Expander)window.FindName("PlacementPanel")).BringIntoView();Capture("Freie-Winkel-Modell");vm.IsSectionMode=true;vm.CurrentLayer=vm.TotalLayers/2;var angleSection=vm.UpdateSection();PumpUntil(()=>angleSection.IsCompleted);Require(vm.CurrentSection is {IsRegular:true},"Geneigte Schicht ist ungültig.");Capture("Freie-Winkel-Schicht");vm.IsSectionMode=false;
            var printAngleBox=(System.Windows.Controls.TextBox)window.FindName("PrintAngleYBox");printAngleBox.BringIntoView();printAngleBox.Focus();printAngleBox.Text="abc";PumpUntil(()=>vm.InputError&&vm.PreviewMesh==null);Require(!vm.ExportStl.CanExecute(null),"Ungültiger Winkel erlaubt Export.");Capture("Freie-Winkel-Ungueltig");printAngleBox.Text="181";PumpUntil(()=>vm.InputError&&vm.PreviewMesh==null);Require(vm.AngleY==-12.5,"Ungültiger Winkel wird ins Projekt übernommen.");Capture("Freie-Winkel-Grenzwert");vm.ResetAngles.Execute(null);PumpUntil(()=>!vm.InputError&&vm.PreviewMesh!=null);Require(vm.Model.Angles.IsZero,"Reset erhält ungültige Winkel.");Capture("Freie-Winkel-Reset");
            vm.Project.Model=SlotFixture();vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);vm.Step=3;((System.Windows.Controls.Expander)window.FindName("PlacementPanel")).IsExpanded=false;var slotsTask=vm.UpdatePreview();PumpUntil(()=>slotsTask.IsCompleted);((System.Windows.Controls.Expander)window.FindName("MountingPanel")).IsExpanded=true;window.UpdateLayout();
            var slotItems=(System.Windows.Controls.ItemsControl)window.FindName("MountingHoleItems");var slotBox=VisualChildren(slotItems).OfType<System.Windows.Controls.TextBox>().First(t=>ReferenceEquals(t.DataContext,vm.BackHoles[0])&&t.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.ParentBinding.Path.Path=="SlotExtension");slotBox.BringIntoView();Capture("Langloch-Editor");
            vm.IsSectionMode=true;vm.CurrentLayer=1;var slotSection=vm.UpdateSection();PumpUntil(()=>slotSection.IsCompleted);Require(vm.CurrentSection is {IsRegular:true,ClosedContours:6},"Langloch-Schicht verliert Öffnungen.");Capture("Langloch-Schicht");vm.IsSectionMode=false;
            var slotDirection=VisualChildren(slotItems).OfType<System.Windows.Controls.CheckBox>().First(t=>ReferenceEquals(t.DataContext,vm.BackHoles[0]));slotDirection.BringIntoView();slotDirection.Focus();vm.Dirty=false;slotDirection.IsChecked=true;PumpUntil(()=>vm.Dirty&&vm.PreviewMesh!=null);Require(vm.BackHoles[0].Horizontal,"Checkbox ändert Langlochrichtung nicht.");var directionPreview=vm.UpdatePreview();PumpUntil(()=>directionPreview.IsCompleted);Capture("Langloch-Richtung");
            slotBox.BringIntoView();slotBox.Focus();slotBox.Text="abc";PumpUntil(()=>vm.InputError&&vm.PreviewMesh==null);Require(!vm.ExportStl.CanExecute(null)&&!vm.Export3Mf.CanExecute(null),"Ungültige Langlochlänge erlaubt Export.");Capture("Langloch-Ungueltig");slotBox.Text="41";PumpUntil(()=>!vm.InputError);var slotRange=vm.UpdatePreview();PumpUntil(()=>slotRange.IsCompleted);Require(vm.PreviewMesh==null,"Zu großes Langloch bleibt exportierbar.");Capture("Langloch-Grenzwert");slotBox.Text="8";PumpUntil(()=>!vm.InputError&&vm.PreviewMesh!=null);Capture("Langloch-Korrigiert");
            vm.SelectedPart=LightboxBack.Parts[2];var slotAssembly=vm.UpdatePreview();PumpUntil(()=>slotAssembly.IsCompleted);vm.Camera.Execute("back");Capture("Langloch-Montage");
            vm.Project.Model=SinkFixture();vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Lightbox);vm.Step=3;var sinkPreview=vm.UpdatePreview();PumpUntil(()=>sinkPreview.IsCompleted);window.UpdateLayout();vm.Camera.Execute("bottom");
            var sinkItems=(System.Windows.Controls.ItemsControl)window.FindName("MountingHoleItems");var sinkBox=VisualChildren(sinkItems).OfType<System.Windows.Controls.TextBox>().First(t=>ReferenceEquals(t.DataContext,vm.BackHoles[0])&&t.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.ParentBinding.Path.Path=="SinkDepth");sinkBox.BringIntoView();Capture("Senkung-Aussenseite");
            vm.IsSectionMode=true;vm.CurrentLayer=1;var sinkLower=vm.UpdateSection();PumpUntil(()=>sinkLower.IsCompleted);Require(vm.CurrentSection is {IsRegular:true,ClosedContours:6},"Senkung verliert untere Konturen.");double lowerArea=vm.CurrentSection!.Area;Capture("Senkung-Untere-Schicht");vm.CurrentLayer=13;var sinkUpper=vm.UpdateSection();PumpUntil(()=>sinkUpper.IsCompleted);Require(vm.CurrentSection is {IsRegular:true,ClosedContours:6}&&vm.CurrentSection.Area>lowerArea+10,"Senkung verjüngt sich nicht nach innen.");Capture("Senkung-Obere-Schicht");vm.IsSectionMode=false;
            sinkBox.BringIntoView();sinkBox.Focus();sinkBox.Text="abc";PumpUntil(()=>vm.InputError&&vm.PreviewMesh==null);Require(!vm.ExportStl.CanExecute(null)&&!vm.Export3Mf.CanExecute(null),"Ungültige Senktiefe erlaubt Export.");Capture("Senkung-Ungueltig");sinkBox.Text="1,9";PumpUntil(()=>!vm.InputError);var sinkRange=vm.UpdatePreview();PumpUntil(()=>sinkRange.IsCompleted);Require(vm.PreviewMesh==null,"Zu tiefe Senkung bleibt exportierbar.");Capture("Senkung-Grenzwert");sinkBox.Text="1,4";PumpUntil(()=>!vm.InputError&&vm.PreviewMesh!=null);Capture("Senkung-Korrigiert");vm.SelectedPart=LightboxBack.Parts[2];var sinkAssembly=vm.UpdatePreview();PumpUntil(()=>sinkAssembly.IsCompleted);vm.Camera.Execute("back");Capture("Senkung-Montage");
            vm.Project.Model=new(){Width=80,Height=100,Resolution=1};vm.Project.Edits=new();vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Flat);vm.Step=1;vm.CompareEnabled=false;vm.Crop.Execute("4:5");var cropPhoto=vm.UpdatePreview();PumpUntil(()=>cropPhoto.IsCompleted);var cropPanel=(System.Windows.Controls.Expander)window.FindName("CropPanel");cropPanel.IsExpanded=true;window.UpdateLayout();var cropWidthBox=(System.Windows.Controls.TextBox)window.FindName("CropAspectWidthBox");var cropHeightBox=(System.Windows.Controls.TextBox)window.FindName("CropAspectHeightBox");cropWidthBox.BringIntoView();Capture("Zuschnitt-4x5");
            cropWidthBox.Focus();cropWidthBox.Text="7,5";cropHeightBox.Focus();cropHeightBox.Text="10";vm.Crop.Execute("custom");var cropCustom=vm.UpdatePreview();PumpUntil(()=>cropCustom.IsCompleted);Require(Math.Abs(vm.Edits.CropWidth*vm.Original.PixelWidth/(vm.Edits.CropHeight*vm.Original.PixelHeight)-.75)<1e-9,"Eigenes Verhältnis wird in der Oberfläche nicht angewandt.");Capture("Zuschnitt-Eigenes-Verhaeltnis");vm.Crop.Execute("16:9");var cropWide=vm.UpdatePreview();PumpUntil(()=>cropWide.IsCompleted);Capture("Zuschnitt-16x9");
            cropHeightBox.BringIntoView();cropHeightBox.Focus();cropHeightBox.Text="abc";PumpUntil(()=>vm.InputError&&vm.PreviewMesh==null);Require(!vm.Crop.CanExecute(null)&&!vm.ExportStl.CanExecute(null),"Ungültiges Verhältnis lässt Zuschneiden oder Export zu.");Capture("Zuschnitt-Ungueltig");cropHeightBox.Text="0";PumpUntil(()=>vm.InputError);Require(vm.CropAspectHeight==9,"Ungültiges Verhältnis wird gespeichert.");Capture("Zuschnitt-Grenzwert");cropHeightBox.Text="9";PumpUntil(()=>!vm.InputError&&vm.PreviewMesh!=null);Capture("Zuschnitt-Korrigiert");
            vm.Project.Model=new(){Width=100,Height=50,Resolution=1,LockDimensions=true};vm.Project.Edits=new();vm.SelectedShape=Templates.All.First(t=>t.Shape==Shape.Flat);vm.Step=3;var dimensionsPreview=vm.UpdatePreview();PumpUntil(()=>dimensionsPreview.IsCompleted);var widthBox=(System.Windows.Controls.TextBox)window.FindName("ModelWidthBox");var heightBox=(System.Windows.Controls.TextBox)window.FindName("ModelHeightBox");widthBox.BringIntoView();widthBox.Focus();widthBox.Text="150";PumpUntil(()=>vm.ModelHeight==75&&vm.PreviewMesh!=null);var linkedWidthPreview=vm.UpdatePreview();PumpUntil(()=>linkedWidthPreview.IsCompleted);Capture("Masse-Gekoppelte-Breite");heightBox.BringIntoView();heightBox.Focus();heightBox.Text="60";PumpUntil(()=>vm.ModelWidth==120&&vm.PreviewMesh!=null);var linkedHeightPreview=vm.UpdatePreview();PumpUntil(()=>linkedHeightPreview.IsCompleted);Capture("Masse-Gekoppelte-Hoehe");vm.LockDimensions=false;widthBox.BringIntoView();widthBox.Focus();widthBox.Text="100";PumpUntil(()=>vm.ModelWidth==100&&vm.PreviewMesh!=null);Require(vm.ModelHeight==60,"Entkoppelte Breite verändert Höhe.");var unlinkedPreview=vm.UpdatePreview();PumpUntil(()=>unlinkedPreview.IsCompleted);Capture("Masse-Entkoppelt");
            var motifFit=vm.FitPhotoDimensions();PumpUntil(()=>motifFit.IsCompleted);var motifPreview=vm.UpdatePreview();PumpUntil(()=>motifPreview.IsCompleted);Require(vm.LockDimensions&&Math.Abs(vm.ModelHeight-100d*440/600)<1e-9,"UI-Motivformat stimmt nicht.");widthBox.BringIntoView();Capture("Masse-Motivformat");
            widthBox.Focus();widthBox.Text="0";PumpUntil(()=>vm.InputError&&vm.PreviewMesh==null);Require(vm.ModelWidth==100&&!vm.Export3Mf.CanExecute(null),"Ungültige Breite wird übernommen oder exportiert.");Capture("Masse-Ungueltig");widthBox.Text="100";PumpUntil(()=>!vm.InputError&&vm.PreviewMesh!=null);vm.LockDimensions=false;vm.ModelWidth=1000;vm.ModelHeight=5;vm.LockDimensions=true;heightBox.BringIntoView();heightBox.Focus();heightBox.Text="1000";PumpUntil(()=>vm.InputError&&vm.PreviewMesh==null);Require(vm.ModelWidth==1000&&vm.ModelHeight==5,"Gekoppelte Bereichsüberschreitung verändert Maße teilweise.");Capture("Masse-Gekoppelter-Grenzwert");heightBox.Text="5";PumpUntil(()=>!vm.InputError);vm.LockDimensions=false;vm.ModelWidth=120;vm.ModelHeight=88;vm.LockDimensions=true;var correctedSize=vm.UpdatePreview();PumpUntil(()=>correctedSize.IsCompleted);Capture("Masse-Korrigiert");
            RenderPresetInterface(window,vm,Capture);
            RenderTabsInterface(window,vm,Capture);
            RenderPrintLayout(window,vm,Capture);
            RenderManualPrintLayout(window,vm,Capture);
            RenderPrintLayoutHistory(window,vm,Capture);
            RenderPrinterProfiles(window,vm,Capture);
            void Capture(string name){window.UpdateLayout();System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);window.UpdateLayout();var bitmap=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(window);File.WriteAllBytes(Path.Combine(directory,$"{name}-{size.Width}.png"),ImageProcessor.Png(bitmap));}
        }
        vm.Dirty=false;window.Close();Console.WriteLine($"{Directory.GetFiles(directory, "*.png").Length} UI-Layouts einschließlich Rückwand, Sockel, Durchführung und geschützter Montageansicht gerendert.");return 0;
    }
    static int CreateExamples(string directory)
    {
        Directory.CreateDirectory(directory);byte[] original=ImageProcessor.Png(ImageProcessor.Demo());
        foreach(var variant in new[]{("Leuchtturm mit Standfuß",Shape.Flat),("Zylinder mit Bodenöffnung",Shape.Cylinder),("Kugellampe mit Öffnungen",Shape.Sphere),("Leuchtkasten mit Rückwand",Shape.Lightbox),("Leuchtturm mit Druckwinkeln",Shape.Flat),("Leuchtkasten mit Langlöchern",Shape.Lightbox),("Leuchtkasten mit Senkungen",Shape.Lightbox),("Leuchtturm im Hochformat",Shape.Flat),("Leuchtturm mit Formatbindung",Shape.Flat),("Leuchtturm mit Aufhängelaschen",Shape.Frame),("Leuchtkasten mit Drucksatzanordnung",Shape.Lightbox),("Leuchtkasten mit freier Platzierung",Shape.Lightbox)})
        {
            var data=new ProjectData{Name=variant.Item1,ImageName="Eingebautes Leuchtturm-Beispiel.png",Model=new(){Shape=variant.Item2,Width=100,Height=85,Diameter=85,StandEnabled=true,StandHeight=7,StandDepth=26,LampBaseThickness=3,LampBaseHoleDiameter=8,SphereOpeningDiameter=34,SphereCollarHeight=4,SphereVentDiameter=4}};
            if(variant.Item2==Shape.Sphere){data.Model.SocketEnabled=true;data.Model.Part=ModelPart.Assembly;}
            if(variant.Item2==Shape.Lightbox){data.Model.BackEnabled=true;data.Model.Part=ModelPart.Assembly;data.Model.BackMountingHoles=MountingFixture(4,true).BackMountingHoles;}
            if(variant.Item1=="Leuchtturm mit Druckwinkeln"){data.Model.StandEnabled=false;data.Model.PrintOrientation=PrintOrientation.FaceUp;data.Model.Angles=new(20,0,15);}
            if(variant.Item1=="Leuchtkasten mit Langlöchern"){data.Model=SlotFixture();data.Model.Resolution=.3;}
            if(variant.Item1=="Leuchtkasten mit Senkungen"){data.Model=SinkFixture();data.Model.Resolution=.3;}
            if(variant.Item1=="Leuchtturm im Hochformat"){data.Model.Width=80;data.Model.Height=100;var r=CropSelection.Centered(600,440,4,5);data.Edits.CropX=r.X;data.Edits.CropY=r.Y;data.Edits.CropWidth=r.Width;data.Edits.CropHeight=r.Height;data.Edits.CropAspectWidth=4;data.Edits.CropAspectHeight=5;data.Edits.CropAspectLock=true;}
            if(variant.Item1=="Leuchtturm mit Formatbindung"){data.Model.Width=120;data.Model.Height=88;data.Model.LockDimensions=true;data.Model.StandEnabled=false;}
            if(variant.Item1=="Leuchtturm mit Aufhängelaschen"){data.Model=new(){Shape=Shape.Frame,Width=100,Height=80,FrameWidth=3,HangingTabsEnabled=true,TabWidth=12,TabHeight=14,TabHoleDiameter=4,TabInset=8,Resolution=.3};}
            if(variant.Item1=="Leuchtkasten mit Drucksatzanordnung"){data.Model=PrintSetFixture(false);data.Model.Resolution=.3;data.PrintLayout=new(){Margin=12,Gap=9,Arrangement=PrintArrangement.Column};}
            if(variant.Item1=="Leuchtkasten mit freier Platzierung"){data.Model=PrintSetFixture(false);data.Model.Resolution=.3;data.PrintLayout=new(){Margin=10,Gap=8,Arrangement=PrintArrangement.Manual,BodyX=20,BodyY=25,AccessoryX=120,AccessoryY=100};}
            string path=Path.Combine(directory,variant.Item1+".lithopro");ProjectStore.Save(path,data,original,original);Require(ProjectStore.Load(path).Data.Name==data.Name,"Beispielprojekt konnte nicht erneut gelesen werden.");
        }
        foreach(var shape in Templates.All.Where(s=>LampShade.Supports(s.Shape)))
        {
            var model=new ModelSettings{Shape=shape.Shape};
            LampShade.ApplyTemplate(model);
            string path=Path.Combine(directory,shape.Name+".lithopro");
            ProjectStore.Save(path,new(){Name=shape.Name,ImageName="Eingebautes Leuchtturm-Beispiel.png",Model=model},original);
            Require(ProjectStore.Load(path).Data.Model.Shape==shape.Shape,"Lampenschirm konnte nicht erneut gelesen werden.");
        }
        Console.WriteLine("16 bearbeitbare Beispielprojekte gespeichert und erneut gelesen.");return 0;
    }
}
