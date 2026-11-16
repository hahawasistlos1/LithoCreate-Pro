# Prüfprotokoll · LithoCreate Pro 1.9.0

Stand: 4. November 2026. Windows x64, SDK 10.0.403, WindowsDesktop 10.0.14.

## Funktion und Geometrie

166 Integrationstests bestanden, 0 fehlgeschlagen. Letzter Lauf parallel zur Oberflächenprüfung: 33,5 Sekunden, 204 MB Peak Working Set. Dies ist keine Speicherobergrenze großer Benutzermodelle. Ausgabe: `artifacts/test-results-1.9.0.txt`; Referenzdateien: `artifacts/tests-1.9.0`.

Die bisherigen 153 Tests bleiben enthalten. Dreizehn zusätzliche Prüfungen decken ab:

- Sechs analytische Quaderfälle: Nullwinkel, X, Y, Z, XYZ sowie XYZ mit zusätzlicher Bettrotation. Bekannte Maße, feste Modellmitte, Volumen 3600 mm³ und erneutes Einlesen von STL/3MF.
- Achsreihenfolge X → Y → Z, Drehsinn, frei versetzter Drehpunkt, Bereichsgrenzen, nicht endliche Werte und Abbruch.
- Alle 15 Formen behalten bei freien Winkeln Topologie, Foto-/Strukturflags, UVs und Volumen.
- Eine schräg gedrehte Scheibe passt anhand ihrer wirklichen Kontur, obwohl die gedrehten Hüllenecken zu große Maße ergäben. Nicht passende freie Lage wechselt zu einer passenden rechtwinkligen Variante mit Nullwinkeln.
- Format-7-Migration erzeugt exakt dieselben Vertices und Dreiecke wie zuvor. Format 8 erhält die drei unabhängigen Winkelsätze und Originalbildbytes.
- Reale Kugel-/Sockel- und Leuchtkasten-/Rückwandmodelle mit unabhängigen Winkeln. Montagegeometrie bleibt aufrecht; der Zwei-Objekt-Drucksatz übernimmt die gedrehten Einzelteile.
- ViewModel-Proxies, Teilewechsel, Rücksetzen und Druckraumsuche ändern ausschließlich das aktive Teil.

Build ohne Compilerwarnungen oder Fehler. Projektformat 8; Formate 1–7 bleiben lesbar.

## Unabhängige Dateiprüfung

54 Referenzdateien mit der Python-Standardbibliothek unabhängig von der C#-Geometrie geprüft:

- `python tools/check_angles.py --inputs artifacts/tests-1.9.0 --output artifacts/angle-files-1.9.0.json`: zwölf STL-/3MF-Dateien. Allgemeine 3×3-Matrix statt der C#-Rotationsformeln; Vergleich aller Vertices mit der analytisch gedrehten Referenz. Drehpunkt, Reihenfolge, Bettrotation, Volumen, Millimeter, geschlossene orientierte Kanten und Z=0.
- `check_printsets.py`: vier Zwei-Objekt-3MF-Dateien, einschließlich Namen, OPC, Build-Zuordnung, Translationen, Geometrie, Volumen, Abständen und Bettgrenzen.
- `check_mounting.py`: vier Bohrungsdateien.
- `check_back.py`: sechs Rückwand-/Aufnahmedateien.
- `check_socket.py`: vier Kugel-/Sockeldateien.
- `check_placements.py`: 24 rechtwinklige Drucklagen-Dateien.

Alle Skripte wurden mit `--inputs artifacts/tests-1.9.0` und eigener JSON-Ausgabe unter `artifacts/*-files-1.9.0.json` ausgeführt. Dies sind Geometrie- und Dateiprüfungen, keine Drucksimulation oder neue unabhängige Slicerfreigabe.

## Oberfläche und native Bedienung

102 WPF-Layouts bei 1400 × 900 und 960 × 640 bestanden. Zehn neue Ansichten zeigen freie Modellwinkel, einen regulären geometrischen Schichtschnitt, ungültigen Text, Bereichsüberschreitung 181° und Rücksetzen. Ungültige Eingaben entfernen das Vorschaumodell und sperren die Mesh-Exporte; 181° wird nicht in das Modell übernommen. Rücksetzen stellt eine gültige Vorschau wieder her. Neue Modell-, Fehler-, Schicht- und Resetansichten wurden visuell kontrolliert. Bilder: `artifacts/ui-1.9.0`; Ausgabe: `artifacts/ui-results-1.9.0.txt`.

Die veröffentlichte App wurde mit einer eigenen Kopie des fünften Beispiels und getrenntem Datenordner unter `artifacts/native-1.9.0` tatsächlich bedient. Der Y-Winkel wurde von 0° auf −12,5° geändert und per Strg+S gespeichert. X=20°, Z=15° und die Grundlage „Flach · Motiv nach oben“ blieben erhalten. Der native Einzelmodell-Speicherdialog begann im Projektordner. Die App schrieb eine 3MF-Datei und bestätigte deren erneute Prüfung: 381896 Dreiecke, Export-Raster 0,299 mm.

`python tools/check_native_angles.py` prüft diese tatsächlich gespeicherte Datei unabhängig: 121,485436 × 102,324101 × 53,696230 mm, analytische Matrixmaße innerhalb 0,001 mm, Volumen 18197,0126 mm³, geschlossene konsistent orientierte Kanten, nicht degenerierte Flächen, endliche Koordinaten und Z=0. Das Originalfoto blieb bytegenau gleich. Außer dem beabsichtigten Y-Winkel und dem zum Dateinamen aktualisierten Projektnamen blieb die Projektbeschreibung identisch. Ergebnis: `artifacts/native-1.9.0/verification.json`. Die Testsitzung beendete sich mit Code 0; die ältere Benutzersitzung blieb unberührt.

## Pakete und Installer

Eigenständige Windows-x64-Laufzeit, fünf erneut geladene Format-8-Beispiele, aktuelle Dokumentation, MSI, portables ZIP und SHA-256-Prüfsummen. Vorherige Releases bleiben erhalten. Das ZIP wird bytegenau gegen den veröffentlichten Ordner und seine Beispieldateien geprüft; Ergebnis: `artifacts/package-verification-1.9.0.json`.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.9.0-win-x64.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.8.0-win-x64.msi -TestRoot ./artifacts/installer-upgrade-1.9.0 -Desktop
```

Der isolierte Lebenszyklustest prüft Upgrade 1.8.0 → 1.9.0, Version und Laufzeit, bytegenaue App-/Core-DLLs und fünf Beispiele, Startmenü und Desktopicon, Rückstufungssperre, Deinstallation sowie unveränderte persönliche Projekte innerhalb und außerhalb des Installationsordners. Vorhandene Benutzerinstallationen und Verknüpfungen werden geschützt. Ausgabe: `artifacts/installer-results-1.9.0.txt`; MSI-Protokolle: `artifacts/installer-upgrade-1.9.0`.

## Grenzen

Keine automatische Optimierung beliebiger Drehwinkel, Auflagestabilität, Überhänge, Stützen, Druckbahnen oder Druckzeit-/Materialprognose. Der Drucksatz umfasst zwei Teile des aktuellen Projekts mit festen Abständen und Hüllrechtecken. Format 8 ist in Apps bis 1.8.0 nicht lesbar; ursprüngliche Dateien für einen Rückwechsel behalten. Druckausrichtung, Stützstrategie, mechanische Passung und optisches Ergebnis benötigen weiterhin Kontrolle im Slicer und praktische Versuche. Intel-N95-, Windows-10- und vollständige DPI-Hardwaretests sowie Codesignatur stehen aus.
