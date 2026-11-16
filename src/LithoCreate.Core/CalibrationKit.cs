using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace LithoCreate.Core;
public static class CalibrationKit
{
    public static PrinterProfile P1SPetg()=>new(){Name="Bambu Lab P1S · PETG",BedX=256,BedY=256,BuildZ=256,Nozzle=.4,Layer=.12,Process="FDM",Material="PETG · Farbe/Hersteller im Slicer wählen"};
    public static void Export(string path,CancellationToken token=default)
    {
        string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";string root=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!,".LithoCreate-Test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        var reports=new List<object>();
        try
        {
            foreach(double thickness in new[]{.6,.8,1,1.2,1.6,2,2.4,3})
                Add($"Licht-{thickness.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)}mm",new(){Width=15,Height=35,FrameWidth=0,MinThickness=thickness,MaxThickness=thickness,Resolution=1,PrintOrientation=PrintOrientation.FaceUp});
            foreach(double clearance in new[]{.15,.25,.35,.45})
            {
                var s=new ModelSettings{Shape=Shape.Lightbox,Width=40,Height=35,FrameWidth=3,BoxDepth=8,BackEnabled=true,BackClearance=clearance,BackInsertionDepth=3,BackHoleDiameter=0,Resolution=1,PrintOrientation=PrintOrientation.FaceUp,BackPrintOrientation=PrintOrientation.FaceUp};
                string name="Passung-"+clearance.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture);s.Part=ModelPart.Lithophane;Add(name+"-Kasten",s);s.Part=ModelPart.BackPanel;Add(name+"-Deckel",s);
            }
            File.WriteAllText(Path.Combine(root,"Pruefwerte.json"),JsonSerializer.Serialize(new{Printer=P1SPetg(),Status="Geometrisch geprüft; noch keine physischen Messwerte",Parts=reports},ProjectStore.Json));
            File.WriteAllText(Path.Combine(root,"Messprotokoll.csv"),"Datei;Filament_Hersteller;Filament_Farbe;Duese_mm;Schicht_mm;Ist_Breite_mm;Ist_Hoehe_mm;Ist_Dicke_mm;Passung_Lichtwirkung;Bemerkung\n"+string.Join("\n",Directory.GetFiles(root,"*.stl").Select(f=>Path.GetFileName(f)+";;;;;;;;;")),new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(root,"LIESMICH.txt"),"PETG-Probedrucke für Bambu Lab P1S\r\n\r\nDie 8 Lichtplatten sind 15 x 35 mm groß; die Dicke steht im Dateinamen. Alle mit demselben realen PETG und denselben Slicereinstellungen drucken. Lichtwirkung mit derselben Lichtquelle und Entfernung vergleichen. Die Platten starten flach; fuer spaeter aufrecht gedruckte Lithophanien die ausgewaehlten Dicken zusaetzlich in derselben Drucklage testen. Die 4 Kasten/Deckel-Paare prüfen 0,15 / 0,25 / 0,35 / 0,45 mm Spiel pro Seite. Paarweise beschriften und trocken prüfen. Ergebnisse im Messprotokoll eintragen.\r\n\r\nDas P1S-Profil enthält nur Druckraumdaten und einen Ausgangswert 0,4-mm-Düse / 0,12-mm-Schicht. Düse tatsächlich prüfen. Temperaturen, Druckplatte, Trocknung, Fluss, Stützen und Werkzeugwege im passenden Bambu-Studio-PETG-Profil wählen. Kein Druckauftrag und keine Wärme-/Belastbarkeitsfreigabe.\r\n",new UTF8Encoding(true));
            ZipFile.CreateFromDirectory(root,temp);token.ThrowIfCancellationRequested();File.Move(temp,path,true);
        }
        finally{if(File.Exists(temp))File.Delete(temp);foreach(string file in Directory.GetFiles(root))File.Delete(file);Directory.Delete(root);}
        void Add(string name,ModelSettings s){token.ThrowIfCancellationRequested();var mesh=MeshGenerator.Generate(new(1,1,[255]),s,false,token);string file=Path.Combine(root,name+".stl");ModelExporter.Export(file,mesh,token);var r=ModelExporter.ReadStl(file,token).Validate(token);if(!r.IsValid)throw new InvalidDataException("Probedruck ungültig.");reports.Add(new{Name=name,Size=new[]{r.Size.X,r.Size.Y,r.Size.Z},r.Volume,r.Triangles});}
    }
}
