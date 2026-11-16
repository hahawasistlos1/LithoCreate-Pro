# Prüfprotokoll · LithoCreate Pro 1.10.0

Stand: 4. November 2026. Windows x64, SDK 10.0.403, WindowsDesktop 10.0.14.

## Funktion und Geometrie

173 Integrationstests bestanden, 0 fehlgeschlagen. Laufzeit 25,6 Sekunden, 185 MB Peak Working Set. Dies ist keine Speicherobergrenze großer Benutzermodelle. Ausgabe: `artifacts/test-results-1.10.0.txt`; Referenzdateien: `artifacts/tests-1.10.0`.

Die bisherigen 166 Tests bleiben enthalten. Sieben zusätzliche Fälle prüfen:

- Senkrechte, waagerechte und gemischte Langlöcher mit zentraler Kabelöffnung, jeweils in Vorschau und Exportauflösung. Außenmaße, freie Breite 4 mm, Gesamtlänge 12 mm, fünf durchgehende Öffnungen, reguläre Schichtkonturen und analytisches Platten-/Ringvolumen; STL-/3MF-Roundtrips.
- Zusatzlängen 0,001 / 0,1 / 1 / 8 / 40 mm, Durchmesser 2 und 20 mm, beide Richtungen und 30 zufällig versetzte Öffnungssätze. Geschlossene konsistent orientierte Topologie und positives Volumen.
- Ungültige Längen, NaN/Unendlich, Randüberschreitung, Kollision mit der Kabelöffnung und Abbruch.
- Format-8-Migration bleibt geometrisch identisch und erhält freie Druckwinkel. Format 9 erhält Zusatzlänge und Richtung, tiefe Kopien sowie unveränderte Originalbildbytes.
- Freie Rückwandwinkel erhalten Volumen und Topologie. Montage bleibt aufrecht. Drucksatz übernimmt die gedrehten Einzelteile, ohne Projektparameter zu verändern.

Build ohne Compilerwarnungen oder Fehler. Format 9 wird geschrieben; Formate 1–8 bleiben lesbar. Apps bis einschließlich 1.9.0 können Format 9 nicht öffnen.

## Unabhängige Dateiprüfung

60 Referenzdateien erfolgreich mit der Python-Standardbibliothek geprüft:

- `python tools/check_slots.py --inputs artifacts/tests-1.10.0 --output artifacts/slot-files-1.10.0.json`: sechs STL-/3MF-Dateien mit senkrechten, waagerechten und gemischten Langlöchern. Pro Datei 360 Strahlen durch die freien Kapselöffnungen, Material außerhalb der Öffnungen, Breite/Gesamtlänge, analytisches Volumen, geschlossene orientierte Kanten, nicht degenerierte Dreiecke, endliche Koordinaten, Außenmaße und Z=0.
- `check_angles.py`: zwölf Dateien mit unabhängigen Drehmatrizen und Vergleich aller Vertices.
- `check_printsets.py`: vier Zwei-Objekt-3MF-Dateien.
- `check_mounting.py`: vier Rundbohrungsdateien.
- `check_back.py`: sechs Rückwand-/Aufnahmedateien.
- `check_socket.py`: vier Kugel-/Sockeldateien.
- `check_placements.py`: 24 rechtwinklige Drucklagen-Dateien.

Alle Skripte wurden mit `--inputs artifacts/tests-1.10.0` und JSON-Ausgaben unter `artifacts/*-files-1.10.0.json` ausgeführt. Zusätzlich wurden die vollständigen Dreieckskoordinaten aller vier Rundbohrungs-STL-/3MF-Dateien mit den tatsächlich von Version 1.9.0 erzeugten Dateien verglichen: exakt gleich. Ergebnis: `artifacts/round-hole-compatibility-1.10.0.json`. Dieser Vergleich ist keine zusätzliche Referenzdateizählung.

## Oberfläche und native Bedienung

116 WPF-Layouts bei 1400 × 900 und 960 × 640 bestanden. Vierzehn neue Ansichten zeigen Langloch-Editor, Schichtschnitt, Richtungsänderung, ungültigen Text, Bereichsüberschreitung 41 mm, gültige Korrektur und Montage. Der echte Checkboxwechsel markiert das Projekt als geändert; fehlerhafte Eingaben entfernen die Vorschau und sperren beide Mesh-Exportbefehle. Die Korrektur stellt die Vorschau wieder her. Editor, Richtung, Schicht und Grenzwert wurden visuell kontrolliert. Bilder: `artifacts/ui-1.10.0`; Ausgabe: `artifacts/ui-results-1.10.0.txt`.

Die veröffentlichte App wurde mit einer eigenen Kopie des sechsten Beispiels und getrenntem Datenordner unter `artifacts/native-1.10.0` bedient. Beim ersten Langloch wurde die Zusatzlänge von 8 auf 10 mm erhöht und die Richtung auf waagerecht gestellt. Nach Strg+S enthielt die Projektdatei genau diese Werte. Der native 3MF-Speicherdialog startete im Projektordner. Die App schrieb „Langloch-Test – Rückwand.3mf“ und bestätigte die erneute Prüfung: 5200 Dreiecke, effektives Export-Raster 1,267 mm.

`python tools/check_slots.py --native --inputs artifacts/native-1.10.0 --output artifacts/native-1.10.0/verification.json` prüfte die tatsächlich gespeicherte Datei unabhängig: 100 × 85 × 5 mm, Volumen 18147,1966 mm³ gegenüber der analytischen Kapselreferenz 18147,4181 mm³, freie 4 × 14 mm am geänderten Langloch und drei weitere 4 × 12 mm große Langlöcher, 360 freie Strahlen, verbleibendes Material, geschlossene orientierte Kanten und Z=0. Die kleine Volumendifferenz stammt aus der umschreibenden Polygonannäherung. Originalfoto und übrige Projektparameter blieben identisch; der Projektname wurde passend zum Dateinamen aktualisiert. Die eigene Testsitzung endete mit Code 0, die ältere Benutzersitzung blieb unberührt.

## Pakete und Installer

Eigenständige Windows-x64-Laufzeit, sechs erneut geladene Format-9-Beispiele, aktuelle Dokumentation, MSI, portables ZIP und SHA-256-Prüfsummen. Vorherige Releases bleiben erhalten. `python tools/check_package.py --version 1.10.0` vergleicht das ZIP bytegenau mit dem veröffentlichten Ordner und prüft sechs Projekte, freie Winkel, das gemischte Langlochbeispiel, Laufzeit und Prüfsummen. Ergebnis: `artifacts/package-verification-1.10.0.json`.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.10.0-win-x64.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.9.0-win-x64.msi -TestRoot ./artifacts/installer-upgrade-1.10.0 -Desktop
```

Der isolierte Lebenszyklustest prüft Upgrade 1.9.0 → 1.10.0, Version und Laufzeit, bytegenaue App-/Core-DLLs und sechs Beispielprojekte, Startmenü und Desktopicon, Rückstufungssperre, Deinstallation sowie unveränderte persönliche Projekte innerhalb und außerhalb des Installationsordners. Vorhandene Benutzerinstallationen und Verknüpfungen werden geschützt. Ausgabe: `artifacts/installer-results-1.10.0.txt`; MSI-Protokolle: `artifacts/installer-upgrade-1.10.0`.

## Grenzen

Langlöcher ausschließlich in der Leuchtkasten-Rückwand, senkrecht oder waagerecht in deren lokalen Achsen. Konservative rechteckige Freizonen können auch nicht kollidierende Konturen abweisen. Keine Senkungen, Schraubenmodelle, Verriegelung oder Belastbarkeitsfreigabe. Die Rückwand bleibt eine lose Steckabdeckung und verriegelt den Kasten nicht. Praktische Druck-, Passungs- und Befestigungstests, unabhängige Slicerfreigaben, N95-/Windows-10-/DPI-Hardwaretests und Codesignatur stehen weiter aus.
