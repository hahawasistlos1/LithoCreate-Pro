# Prüfprotokoll · LithoCreate Pro 1.15.0

Stand: 5. November 2026. Windows-x64-Entwicklungsumgebung mit .NET SDK 10.0.403 und WindowsDesktop 10.0.14. Keine physische Druck- oder Belastungsfreigabe.

## Build und Integration

Build ohne Warnungen oder Fehler. 210 Integrationstests bestanden, 0 fehlgeschlagen; 38,7 Sekunden, Peak Working Set 180 MB. Ausgabe: `artifacts/test-results-1.15.0.txt`. Zehn erneut geladene Beispielprojekte im Projektformat 13.

Neun neue Prüfgruppen behandeln flaches Bild, Rahmen, Kombination mit Standfuß, Laschen direkt am äußeren Bildrand, Vorschau-/Exportauflösung, freie Lochquerschnitte, Schichtschnitte, UV-/Strukturkennzeichnung, Grenzen und Abbruch. Sechzehn weitere Raster-/Maßvarianten innerhalb einer Prüfgruppe verwenden asymmetrische Abtastung und freie Druckwinkel. Migration von Format 12 aktiviert keine Laschen und erhält die bisherige Geometrie. Format 13 erhält alle neuen Parameter und Originalbildbytes. Eigene Vorlagen aus Modellformat 12 behalten Kennung, Namen und bisherige Einstellungen ohne aktivierte Laschen; neu gespeicherte Vorlagen übernehmen sie vollständig.

Eine zusätzliche Grenzprüfung fand degenerierte Flächen bei einem Randabstand von 8,0000001 mm: nahezu deckungsgleiche Anschlusslinien. Der Anschluss wird jetzt in physischen Koordinaten vereinheitlicht; exakt gleiche neue Strukturpunkte werden wiederverwendet. Die Regression prüft sechs positive/negative Mikroschritte und nahezu quadratische Laschen bei 999 mm Fotohöhe. Der gesamte Test-/Oberflächenlauf wurde nach der Korrektur wiederholt.

## Unabhängige Dateien

84 STL-/3MF-Dateien und drei zugeschnittene PNG-Dateien bestanden unabhängige Python-Prüfungen. Acht neue STL-/3MF-Dateien werden mit `python tools/check_tabs.py` geprüft: eine zusammenhängende Komponente, Eulerzahl −2 für zwei Durchgänge, geschlossene konsistent orientierte Kanten, endliche Koordinaten, keine degenerierten Flächen, korrekte Außenmaße und Auflage bei Z=0. Freie Lochdurchmesser, offene Durchgangsstrahlen, Material neben dem Loch und mindestens 1,2 mm Stegmaterial werden unabhängig gemessen. Das Volumen der beiden Laschen stimmt mit Rechteckflächen abzüglich gemessener polygonaler Lochflächen überein, getrennt von der Fotoplatte. Varianten: Flat, Frame, Stand und Edge.

Wiederholt gegen `artifacts/tests-1.15.0`: `check_presets.py` (2 Dateien und 15 Katalogformen), `check_dimensions.py` (8), `check_crops.py` (3 PNGs), `check_sinks.py` (6), `check_slots.py` (6), `check_angles.py` (12), `check_printsets.py` (4 Zwei-Objekt-3MF), `check_mounting.py` (4), `check_back.py` (6), `check_socket.py` (4) und `check_placements.py` (24). Ergebnisse: `artifacts/*-files-1.15.0.json`.

## Oberfläche und native Bedienung

182 WPF-Layouts bei 1400 × 900 und 960 × 640 bestanden. Vierzehn neue Aufnahmen zeigen Laschen an flachem Bild/Rahmen, beide Löcher im geometrischen Schichtschnitt, ungültige Zahleneingabe, geometrisch zu große Bohrung, Korrektur und inaktive Laschen bei einer anderen Form. Ungültige Zahleneingaben verwerfen die Vorschau und sperren den Export; unpassende Geometrie zeigt eine Erklärung. Bilder: `artifacts/ui-1.15.0`; Ausgabe: `artifacts/ui-results-1.15.0.txt`. Große Frontansicht, kleiner Schichtschnitt und kleiner Grenzfehler wurden visuell geprüft.

Die eigenständig veröffentlichte App wurde mit einer eigenen Kopie des zehnten Beispiels und getrenntem Datenordner unter `artifacts/native-1.15.0` bedient. Die Bohrung wurde von 4 auf 5 mm geändert, die eigene Projektkopie per Strg+S gespeichert und über den nativen Dateidialog als 3MF exportiert. Die App bestätigte erneutes Einlesen und Prüfung: 396844 Dreiecke, effektives Raster 0,300 mm. Druckmaße 100 × 4 × 94 mm (Fotofläche 100 × 80 mm, Laschenhöhe 14 mm). Nach der Anschlusskorrektur wurde die Projektkopie mit der korrigierten veröffentlichten App erneut geladen, per nativem Dialog exportiert und erneut gespeichert. Dieser abschließende Lauf liegt unter `artifacts/native-1.15.0/final`. Beide eigenen Sitzungen wurden mit Code 0 geschlossen; andere Benutzerfenster wurden nicht bedient.

`python tools/check_tabs.py --native --inputs artifacts/native-1.15.0/final --output artifacts/native-1.15.0/final/verification.json` bestand unabhängig. Beide Bohrungsmittelpunkte liegen bei X=±36 mm, Höhe 87 mm. Freier Durchmesser innerhalb 0,0001 mm von 5 mm. Eine zusammenhängende Komponente, zwei freie Löcher, geschlossene Kanten, Mindestmaterial, passende Maße und positives Gesamtvolumen 18394,6557 mm³. Gemessenes Laschenvolumen 1186,7370 mm³ stimmt mit der unabhängigen Flächenrechnung überein. Originalbild und sämtliche übrigen Projektparameter blieben erhalten; nur Bohrungsdurchmesser und der beim Speichern zum Dateinamen passende Projektname änderten sich.

## Pakete und Installer

Eigenständige Windows-x64-Laufzeit, zehn Format-13-Beispiele, aktuelle Dokumentation, MSI und portables ZIP mit SHA-256-Prüfsummen. `python tools/check_package.py --version 1.15.0` prüft bytegenau das ZIP, Laufzeit, Beispiele einschließlich Aufhängelaschen und Prüfsummen. Ergebnis: `artifacts/package-verification-1.15.0.json`.

Der Installer-Lebenszyklustest verwendet getrennte Installertest-Produkte mit eigenen Produkt-/Upgrade-/Komponentenkennungen und Verknüpfungen; die unveränderten Release-MSI-Dateien dienen als Referenz. Das vorhandene Benutzerprodukt wird nicht aktualisiert.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.15.0-installertest.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.14.0-installertest.msi -TestRoot ./artifacts/installer-upgrade-1.15.0 -Desktop -IsolatedIdentity -ReferenceMsiPath ./artifacts/LithoCreate-Pro-1.15.0-win-x64.msi -PreviousReferenceMsiPath ./artifacts/LithoCreate-Pro-1.14.0-win-x64.msi
```

Prüfumfang: Upgrade 1.14 → 1.15, bytegenaue App-/Core-Dateien und zehn Beispiele, integrierte Laufzeit, eigene Startmenü-/Desktopverknüpfungen, Rückstufungssperre, Deinstallation und Erhalt persönlicher Projektdateien sowie vorhandener Benutzerverknüpfungen. Ausgabe: `artifacts/installer-results-1.15.0.txt`; Logs: `artifacts/installer-upgrade-1.15.0`. „installertest“-MSIs sind keine Auslieferungspakete.

## Grenzen

Symmetrische rechteckige Aufhängelaschen für zwei flache Formen. Keine frei positionierbaren Bohrungen in beliebigen Formen, keine gekrümmten Laschen und keine genormte oder lastgeprüfte Halterung. Mindestmaterial ist geometrisch geprüft; reale Tragfähigkeit, Druckausrichtung, Material und Schichthaftung erfordern praktische Versuche. Die neue Geometrie kann weitere Anschlusslinien im Fotogitter erzeugen; das Speicherbudget bleibt ein konservatives Arbeitsbudget, keine harte Gesamtprozessgrenze. Projektformat 13 und neu gespeicherte Vorlagen benötigen Version 1.15 oder neuer. Physische Drucktests, unabhängige Slicerfreigaben, Intel-N95-/Windows-10-/OS-DPI-Hardwaretests und Codesignatur stehen weiter aus.
