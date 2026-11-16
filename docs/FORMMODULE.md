# Lokale Formmodule · Schnittstelle 1

„STL & Module“ öffnet den separaten Prüf- und Erweiterungsbereich. „Formmodul laden“ wählt ausdrücklich eine DLL. Vor dem Ausführen wird auf die Codeausführung hingewiesen. Eine DLL kann Dateien und Netzwerk verwenden; nur eigene oder vertrauenswürdige Module laden. Der getrennte Prozess schützt den Editor vor Hängern/Abstürzen, ist aber keine Sicherheitssandbox.

Ein Modul implementiert genau eine öffentliche, nicht abstrakte Klasse mit parameterlosem Konstruktor:

```csharp
public interface IFormPlugin
{
    string Name { get; }
    Mesh Generate(GrayImage photo, ModelSettings parameters, CancellationToken token);
}
```

Gegen die mitgelieferte LithoCreate.Core.dll und .NET 10 bauen. Das Modul erhält ein bearbeitetes Graubild und einen Parameterschnappschuss. Die Einheit ist Millimeter; im internen Mesh zeigt Y nach oben. Einzelteil statt Montage zurückgeben. Vertexindizes müssen gültig, Koordinaten endlich, Kanten geschlossen/konsistent und das Volumen positiv sein. Bei langen Schleifen CancellationToken prüfen. Keine Veränderung des Hauptprojekts erwarten. Zusätzliche private Bibliotheken werden in Schnittstelle 1 nicht automatisch aufgelöst.

Grenzen: DLL bis 16 MiB; Ergebnis höchstens 300.000 Dreiecke / 900.000 Vertices; Bild bis 4096 × 4096 Pixel. Der Host prüft die Ausgabe und schreibt STL; der Editor liest sie separat erneut ein. Nach spätestens 60 Sekunden oder Abbruch wird der Prozess samt Kindprozessen beendet. Fremder Code muss dennoch als voller Benutzerprozess betrachtet werden; die Grenzprüfungen sind keine Garantie gegen bösartigen Code.

Zwei quelloffene Beispiele liegen unter extensions:

- WaveForm / LithoCreate.WaveForm.dll erzeugt ein 90°-Bogensegment aus der vorhandenen radialen Lithophaniegeometrie. Der Radius wird aus Breite / (π/2) bestimmt. Das Beispiel vergröbert bei Bedarf sein eigenes Raster auf ungefähr 50.000 Fotogitterzellen, um unter dem Hostlimit zu bleiben; die Auflösung des Hauptprojekts bleibt erhalten. Der historische Dateiname bezeichnet nicht die sichtbare Modulform.
- DrilledPhoto / LithoCreate.DrilledPhoto.dll erzeugt eine Fotoplatte mit freien Rundbohrungen. Die XY-/Durchmesserliste des Prüffensters wird über ModelSettings.BackMountingHoles übergeben. X ist relativ zur Mitte, Y ab Unterkante. Rundbohrungen ohne Langloch/Senkung; höchstens acht. Ohne Liste verwendet das Modul zwei obere Bohrungen. Frontdicke folgt den Bildwerten, Lochwände werden geschlossen erzeugt.

Der Moduleingang verwendet das Foto, die Bildkorrekturen und Modellmaße beim Öffnen des Prüffensters. Ergebnisse und Halterungen als geprüfte STL/3MF speichern. Das Prüffenster speichert keine zusätzlichen Formmodulparameter im normalen .lithopro-Projekt und führt beim Öffnen eines normalen Projekts keinen fremden Code automatisch aus. Ein fremdes STL wird hier nicht automatisch zu einer Fotoprojektionsvorlage.

## Belege und Referenzen

- Microsoft: [AssemblyLoadContext](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext).
- Eigene Prüfungen: Program.Extensions.cs und tools/check_workshop.py. Die zwei Beispiel-DLLs und der PluginHost werden tatsächlich gebaut und ausgeführt.
