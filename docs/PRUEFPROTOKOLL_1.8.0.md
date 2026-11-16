# Prüfprotokoll · LithoCreate Pro 1.8.0

Stand: 4. November 2026. Windows x64, SDK 10.0.403, WindowsDesktop 10.0.14.

## Funktion und Geometrie

153 Integrationstests bestanden, 0 fehlgeschlagen. Letzter Lauf mit paralleler Oberflächenprüfung: 28,8 Sekunden, 181 MB Peak Working Set. Die Messung ist keine Speicherobergrenze großer Benutzermodelle. Ausgabe: `artifacts/test-results-1.8.0.txt`; Referenzdateien: `artifacts/tests-1.8.0`.

Die bisherigen 143 Tests bleiben enthalten. Zusätzlich:

- Zwei einzeln geprüfte, unverändert skalierte Meshes in lokalen Druckkoordinaten auf Z=0. Zentrierte Zeilen-/Spaltenanordnung und genau passende Bettgrenzen; Mindestabstand und Rand jeweils 5 mm. Bauhöhe und zu kleines Bett werden abgewiesen.
- Erhalt von Namen, Drucklagen, Volumen und Maßen. Bekannte Referenzquader 40 × 30 × 3 mm und 20 × 10 × 2 mm mit Volumina 3600 beziehungsweise 400 mm³ und bekannten Bettpositionen.
- Abweisung von Montagegeometrie, Überlappung, ungültigen Positionen, doppelten Namen, nicht endlichen Druckraummaßen und nachträglich veränderter vorbereiteter Geometrie.
- Zwei benannte Ressourcen und Build-Einträge in 3MF. Erneutes Einlesen beider gespeicherten Objekte. Defekte Einheiten, Objekt-IDs, fehlende Build-Einträge und nicht unterstützte Transformationsmatrizen werden abgewiesen. Der bisherige Einzelmodell-Leser lehnt Drucksatzdateien ausdrücklich ab.
- Abbruch vor und während des Schreibens sowie XML-Schreibfehler lassen vorhandene Zieldateien unverändert und hinterlassen keine temporären Teilstände.
- Vollständige Drucksätze aus realen Kugel-/Sockel- und Leuchtkasten-/Rückwandprojekten einschließlich Befestigungsbohrungen. Aktuelle Teilwahl und gespeicherte Drucklagen werden nicht verändert.
- Tiefe Geometriekopien, PNG-Rendering der Bettübersicht in großen und kleinen Flächen, Ansichtswechsel ohne Projektänderung, Cacheverwerfung nach Änderung, Berechnungsabbruch und gesperrter Export bei nicht passendem Bett.

Build ohne Compilerwarnungen oder Fehler. Das Projektformat bleibt 7.

## Unabhängige Dateiprüfung

42 Referenzdateien unabhängig mit der Python-Standardbibliothek geprüft:

- `python tools/check_printsets.py`: vier Zwei-Objekt-3MF-Dateien. OPC-Beziehungen, Millimeter, benannte Ressourcen, eindeutige Build-Zuordnung, reine Translationsmatrizen, Vertex-/Dreieckindizes, geschlossene orientierte Kanten, nicht degenerierte Flächen, positives Volumen, lokale Z=0-Lage, bekannte Referenzmaße/-volumina, Zeilen-/Spaltenpositionen, 5-mm-Abstände und Druckraumgrenzen. Ergebnis: `artifacts/printset-files-1.8.0.json`.
- `python tools/check_mounting.py --inputs artifacts/tests-1.8.0 --output artifacts/mounting-files-1.8.0.json`: vier Bohrungsdateien.
- `python tools/check_back.py --inputs artifacts/tests-1.8.0 --output artifacts/back-files-1.8.0.json`: sechs Rückwand-/Aufnahmedateien.
- `python tools/check_socket.py --inputs artifacts/tests-1.8.0 --output artifacts/socket-files-1.8.0.json`: vier Kugel-/Sockeldateien.
- `python tools/check_placements.py --inputs artifacts/tests-1.8.0 --output artifacts/placement-files-1.8.0.json`: 24 Drucklagen-Dateien.

Die 3MF-Struktur richtet sich nach der [offiziellen 3MF-Core-Spezifikation](https://github.com/3MFConsortium/spec_core/blob/master/3MF%20Core%20Specification.md). Dies sind Geometrie- und Dateiprüfungen; keine neue unabhängige Slicerfreigabe oder Drucksimulation wird behauptet.

## Oberfläche und native Bedienung

92 WPF-Layouts bei 1400 × 900 und 960 × 640. Acht neue Ansichten zeigen Kasten-/Rückwand-Drucksatz, Exportseite, zu kleines Bett und Kugel-/Sockeldrucksatz. Fehlermeldungen werden beim Ansichtswechsel sichtbar an den Panelanfang gescrollt; Speichern bleibt ohne gültigen Satz gesperrt. Alte Einzelteil-/Montagemaße werden im Drucksatzexport ausgeblendet. Bilder: `artifacts/ui-layout-1.8.0`.

Die veröffentlichte App wurde mit eigener Projektkopie und getrenntem Datenordner unter `artifacts/native-1.8.0` tatsächlich bedient. Aus der aufrechten Leuchtkastenmontage wurde ein Drucksatz berechnet und über den nativen Dateidialog im Projektordner gespeichert. Die App bestätigte die erneute Prüfung. Der Speicherdialog beginnt bei einem geöffneten Projekt im zugehörigen Ordner.

Die tatsächlich geschriebene Datei wurde danach unabhängig gelesen: zwei benannte Objekte „Leuchtkasten“ und „Rückwand“, 343144 beziehungsweise 3344 Dreiecke; Maße 100 × 24 × 85 mm beziehungsweise 100 × 85 × 5 mm. Lokale Koordinaten beginnen bei null. Die Build-Positionen sind (60, 53, 0) und (60, 82, 0) mm auf einem 220 × 220 mm großen Bett, mit 5 mm Abstand. Die Projektdatei und das Originalfoto blieben bytegenau unverändert. Die Testsitzung beendete sich mit Code 0, die ältere Benutzersitzung blieb unberührt. Ergebnis: `artifacts/native-1.8.0/verification.json`.

## Pakete und Installer

Eigenständige Windows-x64-Laufzeit, vier erneut geladene Format-7-Beispiele, Dokumentation, MSI, portables ZIP und SHA-256-Prüfsummen. Vorherige Releases bleiben erhalten.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.8.0-win-x64.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.7.0-win-x64.msi -TestRoot ./artifacts/installer-upgrade-1.8.0 -Desktop
```

Der isolierte Lebenszyklustest prüft Upgrade 1.7.0 → 1.8.0, Version und Laufzeit, bytegenaue App-/Core-DLLs und Beispielprojekte, Startmenü und Desktopicon, Rückstufungssperre, Deinstallation und unveränderte persönliche Projekte innerhalb und außerhalb des Installationsordners. Vorhandene Benutzerinstallationen und Verknüpfungen werden geschützt. Ausgabe: `artifacts/installer-results-1.8.0.txt`; MSI-Protokolle: `artifacts/installer-upgrade-1.8.0`.

## Grenzen

Zwei Teile des aktuellen Projekts, feste Abstände und zwei rechtwinklige Anordnungsmöglichkeiten. Keine freie Platzierung, schräge Drehwinkel, Modelle aus mehreren Projekten, Slicerprofile, Stützen, Druckbahnen oder Druckzeit-/Materialprognose. Die Bettübersicht zeigt Hüllrechtecke. Importpositionen und Stützstrategie im eigenen Slicer kontrollieren. Montage und mechanische Passung der gedruckten Teile benötigen weiterhin einen praktischen Test.
