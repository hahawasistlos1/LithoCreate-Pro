# Prüfprotokoll · LithoCreate Pro 1.11.0

Stand: 4. November 2026. Windows x64, SDK 10.0.403, WindowsDesktop 10.0.14.

## Funktion und Geometrie

180 Integrationstests bestanden, 0 fehlgeschlagen. Letzter Lauf 27,9 Sekunden, 237 MB Peak Working Set. Dies ist keine Speicherobergrenze großer Benutzermodelle. Ausgabe: `artifacts/test-results-1.11.0.txt`; Referenzdateien: `artifacts/tests-1.11.0`.

Die bisherigen 173 Tests bleiben enthalten. Sieben zusätzliche Fälle prüfen:

- Senkungen an Rundbohrungen, Langlöchern und gemischten Öffnungen, jeweils in Vorschau und Exportauflösung. Geschlossene orientierte Topologie, Außenmaße, fotounabhängige Rückwand, analytisches Volumen, sechs Schichtkonturen und Öffnungsmaße bei 0,05 / 0,35 / 0,9 / 1,5 mm Plattenhöhe. STL-/3MF-Roundtrips.
- Plattenstärken 0,8 / 2 / 10 mm, Senktiefe 0,1 mm und maximale Tiefe mit 0,4 mm Restdurchgang, freie Breiten 2 und 20 mm, beide Langlochrichtungen sowie 25 zufällig versetzte Öffnungssätze.
- Negative, zu kleine und zu große Tiefe, NaN/Unendlich, Randüberschreitung und Kollision allein durch die größere Außenöffnung; Abbruch.
- Format-9-Migration bleibt geometrisch identisch und erhält Langlochparameter und freie Druckwinkel. Format 10 erhält Senktiefen, tiefe Kopien und Originalbildbytes.
- Alle zwölf rechtwinkligen Drucklagen mit zusätzlichen freien Winkeln erhalten Volumen und Topologie. Montage bleibt aufrecht. Ein Zwei-Objekt-Drucksatz verändert die Teilwahl nicht.

Build ohne Compilerwarnungen oder Fehler. Projektformat 10, Formate 1–9 bleiben lesbar. Apps bis einschließlich 1.10.0 können Format 10 nicht öffnen.

## Unabhängige Dateiprüfung

66 Referenzdateien mit der Python-Standardbibliothek unabhängig von der C#-Geometrie geprüft:

- `python tools/check_sinks.py --inputs artifacts/tests-1.11.0 --output artifacts/sink-files-1.11.0.json`: sechs Senkungs-STL-/3MF-Dateien. Freier gerader Durchgang, verbleibendes Material, innere und äußere Öffnungsmaße, Tiefenprofil an bis zu 288 Messpunkten pro Datei, 45°-Fasen anhand der Dreiecksnormalen, analytisches Volumen, geschlossene orientierte Kanten, nicht degenerierte Flächen, endliche Koordinaten, Außenmaße und Z=0. Die analytische Öffnung wird aus Kreis-/Kapselgeometrie bestimmt; es wird kein C#-Code verwendet.
- `check_slots.py`: sechs Langlochdateien.
- `check_angles.py`: zwölf Dateien mit unabhängigen Drehmatrizen und Vergleich aller Vertices.
- `check_printsets.py`: vier Zwei-Objekt-3MF-Dateien.
- `check_mounting.py`: vier Rundbohrungsdateien.
- `check_back.py`: sechs Rückwand-/Aufnahmedateien.
- `check_socket.py`: vier Kugel-/Sockeldateien.
- `check_placements.py`: 24 rechtwinklige Drucklagen-Dateien.

Alle Skripte wurden mit `--inputs artifacts/tests-1.11.0` und JSON-Ausgaben unter `artifacts/*-files-1.11.0.json` ausgeführt. Die vollständigen Dreieckskoordinaten von zehn Rund-/Langlochdateien ohne Senkung wurden zusätzlich mit tatsächlich von Version 1.10.0 erzeugten Dateien verglichen: exakt gleich. Ergebnis: `artifacts/unsunk-compatibility-1.11.0.json`; diese Dateien sind bereits in den 66 Referenzen enthalten.

## Oberfläche und native Bedienung

130 WPF-Layouts bei 1400 × 900 und 960 × 640 bestanden. Vierzehn neue Ansichten zeigen Außenseite, untere und obere Schicht, ungültigen Text, zu tiefe Senkung, gültige Korrektur und Montage. Der obere Schichtschnitt hat wie erwartet eine kleinere Öffnungsfläche. Fehler entfernen die Vorschau und sperren beide Mesh-Exportbefehle; gültige Korrektur stellt die Vorschau wieder her. Außenseite, beide Schichten und Grenzwert wurden visuell kontrolliert. Bilder: `artifacts/ui-1.11.0`; Ausgabe: `artifacts/ui-results-1.11.0.txt`.

Die veröffentlichte App wurde mit eigener Kopie des siebten Beispiels und getrenntem Datenordner unter `artifacts/native-1.11.0` bedient. Die erste Senkung wurde von 1,2 auf 1,4 mm vertieft und per Strg+S gespeichert. Der native 3MF-Dialog begann im Projektordner. Die App schrieb „Senkung-Test – Rückwand.3mf“ und bestätigte deren erneute Prüfung: 7264 Dreiecke, effektives Raster 1,031 mm.

`python tools/check_sinks.py --native --inputs artifacts/native-1.11.0 --output artifacts/native-1.11.0/verification.json` prüfte diese tatsächliche Datei unabhängig: 100 × 85 × 5 mm, Volumen 18200,0220 mm³ gegenüber der analytischen Referenz 18200,1998 mm³, 216 Tiefenprofil-Messpunkte mit maximal 0,00420 mm Abweichung, maximaler relativer Fasensteigungsfehler 0,0000234, geschlossene orientierte Kanten und Z=0. Am geänderten 4-mm-Durchgang beträgt die äußere Öffnung 6,8 mm; 0,6 mm gerader Durchgang bleiben stehen. Die kleine Volumendifferenz stammt aus der umschreibenden Polygonannäherung. Originalfoto und übrige Projektparameter blieben identisch; der Projektname wurde passend zum Dateinamen aktualisiert. Die eigene Testsitzung endete mit Code 0. Andere Benutzersitzungen wurden nicht bedient.

## Pakete und Installer

Eigenständige Windows-x64-Laufzeit, sieben erneut geladene Format-10-Beispiele, aktuelle Dokumentation, MSI, portables ZIP und SHA-256-Prüfsummen. Vorherige Releases bleiben erhalten. `python tools/check_package.py --version 1.11.0` vergleicht das ZIP bytegenau mit dem veröffentlichten Ordner und prüft Beispiele einschließlich freier Winkel, Langloch- und Senkungseinstellungen, Laufzeit und Prüfsummen. Ergebnis: `artifacts/package-verification-1.11.0.json`.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.11.0-win-x64.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.10.0-win-x64.msi -TestRoot ./artifacts/installer-upgrade-1.11.0 -Desktop
```

Der isolierte Lebenszyklustest prüft Upgrade 1.10.0 → 1.11.0, Version und Laufzeit, bytegenaue App-/Core-DLLs und sieben Beispiele, Startmenü und Desktopicon, Rückstufungssperre, Deinstallation sowie unveränderte persönliche Projekte innerhalb und außerhalb des Installationsordners. Vorhandene Benutzerinstallationen und Verknüpfungen werden geschützt. Ausgabe: `artifacts/installer-results-1.11.0.txt`; MSI-Protokolle: `artifacts/installer-upgrade-1.11.0`.

## Grenzen

Senkungen ausschließlich an zusätzlichen Rückwandöffnungen, auf der äußeren Seite, mit festem 90°-Winkel und polygonalen Konturen. Kein veränderlicher Schraubenkopfwinkel, Gewinde, Schraubenmodell, Rastverschluss oder Belastbarkeitsnachweis. Konservative Freizonen können nicht kollidierende Konturen abweisen. Die Rückwand bleibt eine lose Steckabdeckung und verriegelt den Kasten nicht. Praktische Druck-, Passungs- und Befestigungstests, unabhängige Slicerfreigaben, N95-/Windows-10-/DPI-Hardwaretests und Codesignatur stehen weiter aus.
