# Prüfprotokoll · LithoCreate Pro 1.4.0

Stand: 4. November 2026. Windows x64, SDK 10.0.403, WindowsDesktop 10.0.14.

## Integrationstests

107 Tests bestanden, 0 fehlgeschlagen. Laufzeit: 27,8 Sekunden, 175 MB Peak Working Set. Dies beschreibt den Testlauf und ist keine Speicherobergrenze für große Benutzermodelle. Vollständige Ausgabe: `artifacts/test-results-1.4.0.txt`; erzeugte Dateien: `artifacts/tests-1.4.0`.

Die 90 bisherigen Prüfungen für Fotoeditor, EXIF, Geometrie, Projekte, Kamera, STL/3MF, Instanzsperre und Schichtschnitte bleiben enthalten. Neue Prüfungen:

- Zwölf Drucklagen eines bekannten Quaders: exakte Druckmaße, positives gleichbleibendes Volumen, geschlossene Topologie, Schichtfläche und STL-/3MF-Roundtrips.
- Alle 15 Formen behalten bei einer seitlichen Drucklage Volumen, Topologie, Foto-UVs und Strukturkennzeichnung.
- Die Druckraumsuche findet jeweils eine nötige Bettrotation, flache oder seitliche Lage und erhält eine bereits passende bewusste Auswahl.
- Kein Treffer verändert die Auswahl nicht. Ungültige Profile, unbekannte Drucklagen, leere Netze und Abbruch werden geprüft.
- Format-3-Migration erhält Bildkorrekturen und aufrechte Drucklage; Format 4 speichert Lage und Bettrotation einschließlich unveränderter Originalbytes.
- Die Suche aktualisiert Vorschau und Schichten und verwirft ein altes Prüfmodell. Wiederholte bereits passende Suche und erfolglose Suche verändern das Projekt nicht. Abbruch gibt die Bedienung wieder frei.

Build: keine Compilerwarnungen oder Fehler.

## Unabhängige Dateiprüfung

`python tools/check_placements.py` liest die 24 erzeugten STL-/3MF-Dateien mit der Python-Standardbibliothek. Alle Prüfungen bestanden: bekannte Druckmaße, positives signiertes Volumen von 3600 mm³ und Auflage bei Z=0. 3MF enthält ausdrücklich Millimeter. Ergebnis: `artifacts/placement-files-1.4.0.json`.

Dies ist eine unabhängige Inhaltsprüfung, keine Slicer- oder Drucksimulation. Für 1.4.0 wird keine neue Bambu-CLI-Freigabe behauptet. Die Instabilität der optionalen Bambu-Nachtests aus 1.3.0 ist separat unter `artifacts/SLICER_NACHTEST_1.3.0.md` dokumentiert.

## Oberfläche

50 tatsächliche WPF-Layouts bei 1400 × 900 und 960 × 640, einschließlich flacher und seitlicher Lage, flacher Schichtansicht und automatisch ausgewählter Bettrotation. Die neue Reset-Perspektive hält das Motiv sichtbar. Kleine Fenster verwenden scrollbare Eingabepanels. Renderbilder: `artifacts/ui-layout-1.4.0`.

Der zusätzliche native Bedienungstest verwendet ein eigenes Format-4-Projekt unter `artifacts/native-1.4.0`: aufrecht außerhalb eines 110 × 80 × 10-mm-Druckraums, flach passend. App-Daten werden für diesen Test in einen eigenen Unterordner geleitet. Die native Suche hat korrekt „Flach · Motiv nach oben“ übernommen. Die Vorschau und Volumenwarnung wurden aktualisiert; die erste Schicht zeigte 6500 mm² Material bei insgesamt 24 Schichten zu 0,12 mm. Nach Speichern wurden Format 4, die gewählte Lage und unveränderte Originalbildbytes unabhängig aus der Projekt-ZIP-Datei geprüft. Ergebnis: `artifacts/native-1.4.0/verification.json`. Die ursprüngliche Benutzersitzung blieb unberührt.

## Release und Installer

Eigenständige Windows-x64-Anwendung mit Laufzeit, drei erneut eingelesenen Format-4-Beispielprojekten, Dokumentation, MSI, ZIP und SHA-256-Prüfsummen. Frühere Release-Dateien bleiben erhalten.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.4.0-win-x64.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.3.0-win-x64.msi -TestRoot ./artifacts/installer-upgrade-1.4.0 -Desktop
```

Das Skript schützt vorhandene Benutzerinstallationen und Verknüpfungen. Es prüft Upgrade, App-Version, Laufzeit, Startmenü, Desktopicon, bytegenaue Beispielprojekte, Rückstufungssperre, Deinstallation und unveränderte persönliche Projekte innerhalb und außerhalb des Programmordners. Einzelresultate: `artifacts/installer-results-1.4.0.txt`; MSI-Protokolle: `artifacts/installer-upgrade-1.4.0`.

## Grenzen

Rechtwinklige Hüllenpassung ohne Bewertung von Auflagefläche, Überhängen, Stützen oder optimaler Druckqualität. Kein Anordnen mehrerer Modelle und kein G-Code. Keine physische Druckprobe, Intel-N95-Langzeitprüfung oder neue Windows-10-/DPI-Hardwarefreigabe. MSI und EXE sind nicht digital signiert. Weitere Grenzen stehen im Funktionsumfang.
