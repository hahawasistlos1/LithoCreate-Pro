# Prüfprotokoll · LithoCreate Pro 1.7.0

Stand: 4. November 2026. Windows x64, SDK 10.0.403, WindowsDesktop 10.0.14.

## Funktion und Geometrie

143 Integrationstests bestanden, 0 fehlgeschlagen. Letzter Lauf mit paralleler Oberflächenprüfung: 26,7 Sekunden, 237 MB Peak Working Set. Dies ist keine Speicherobergrenze großer Benutzermodelle. Ausgabe: `artifacts/test-results-1.7.0.txt`; Referenzdateien: `artifacts/tests-1.7.0`.

Die bisherigen 132 Tests bleiben enthalten. Zusätzlich:

- Eine, vier und acht frei positionierte Befestigungsbohrungen, jeweils mit und ohne zentrale Kabelöffnung, in Vorschau und Exportauflösung. Geschlossene konsistent orientierte Topologie, erwartete Euler-Charakteristik, unveränderte Außenmaße und fehlende Fototextur.
- Schichtkonturen und Flächen durch die Platte sowie den oberen Einsteckring. Vergleich von Schichtflächen mit dem gesamten Meshvolumen, echte Durchgänge und STL-/3MF-Roundtrips.
- 30 reproduzierbare unregelmäßige Konfigurationen sowie fast fluchtende Hilfsgrenzen. Diese Fälle erzeugen keine degenerierten oder offenen Flächen.
- Abweisung ungültiger Koordinaten, nicht endlicher Zahlen, fehlender Listenelemente, mehr als acht Bohrungen, ungeeigneter Durchmesser, Randverletzungen und Überlappungen mit Nachbarbohrungen oder Kabelöffnung. Berechnungsabbruch.
- Tiefe Kopien der Liste und ihrer Bohrungsobjekte: Änderungen an Arbeitsschnappschüssen verändern das Projekt nicht.
- Format-7-Roundtrip aller Koordinaten/Durchmesser und unveränderter Originalbytes; Format 6 bleibt ohne zusätzliche Bohrungen und mit ursprünglicher Rückwand/Drucklage erhalten.
- Alle zwölf rechtwinkligen Drucklagen einschließlich Bettrotation, foto-unabhängiger Export, Hinzufügen/Entfernen im Editor und weiterhin gesperrte Montageausgabe.

Build ohne Compilerwarnungen oder Fehler.

## Unabhängige Dateiprüfung

38 tatsächliche STL-/3MF-Dateien unabhängig mit Python-Standardbibliothek geprüft. Die Python-Leser verwenden keine C#-Import- oder Geometrieimplementierung.

- `python tools/check_mounting.py`: vier neue Dateien. Bekannte Bohrungspositionen, freie Durchmesser 4 mm und 8 mm innerhalb der Float-Toleranz, offene axiale Wege, Material neben den Bohrungen, geschlossene orientierte Kanten, analytisches Platten-/Ringvolumen, exakte Außenmaße und Z=0. Ergebnis: `artifacts/mounting-files-1.7.0.json`.
- `python tools/check_back.py --inputs artifacts/tests-1.7.0 --output artifacts/back-files-1.7.0.json`: sechs Dateien der Rückwand-/Kastenpassung.
- `python tools/check_socket.py --inputs artifacts/tests-1.7.0 --output artifacts/socket-files-1.7.0.json`: vier Dateien der Kugel-/Sockelpassung.
- `python tools/check_placements.py --inputs artifacts/tests-1.7.0 --output artifacts/placement-files-1.7.0.json`: 24 Drucklagen-Dateien.

Dies sind Geometrie- und Dateiprüfungen, keine Slicer- oder Drucksimulation. Keine neue Freigabe durch die externe Bambu-CLI.

## Oberfläche

84 WPF-Layouts bei 1400 × 900 und 960 × 640. Zwölf neue Ansichten zeigen Bohrungseditor, Schichten, ungültige Zahlen, Randverletzung, Korrektur und Montage. Die schmalen Koordinatenfelder und die scrollbare Liste wurden visuell geprüft. Bilder: `artifacts/ui-layout-1.7.0`.

Zusätzlich wird die tatsächliche WPF-Bindung geprüft: „abc“ in einer X-Koordinate verwirft das Mesh und sperrt STL/3MF. Eine Randverletzung zeigt kein altes Modell. Korrektur stellt die Vorschau automatisch wieder her. Entfernen der Zeile mit ungültiger Zahl beseitigt den Validierungsfehler, ohne andere Bohrungen zu löschen.

Die veröffentlichte App wurde mit eigener Projektkopie und getrenntem Datenordner unter `artifacts/native-1.7.0` tatsächlich bedient. Eine fünfte Bohrung wurde hinzugefügt und ihre X-Koordinate auf −14,5 mm geändert, bei Y = 17 mm und Ø = 4 mm. Die untere Schicht zeigte 8386,54 mm² Material und sieben Konturen: Außenkontur, Kabelöffnung und fünf Befestigungsbohrungen. Nach dem Speichern wurden Format 7, Teilwahl, die vier unveränderten ursprünglichen Bohrungen, die fünfte geänderte Position und unveränderte Originalbildbytes unabhängig aus der Projektdatei geprüft. Die Testsitzung beendete sich mit Code 0; die ältere Benutzersitzung blieb unverändert geöffnet. Ergebnis: `artifacts/native-1.7.0/verification.json`.

## Release und Installer

Eigenständige Windows-x64-Laufzeit, vier erneut geladene Format-7-Beispiele einschließlich Leuchtkasten mit vier Bohrungen, Dokumentation, MSI, portables ZIP und SHA-256-Prüfsummen. Frühere Release-Dateien bleiben erhalten.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.7.0-win-x64.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.6.0-win-x64.msi -TestRoot ./artifacts/installer-upgrade-1.7.0 -Desktop
```

Der isolierte Lebenszyklustest prüft Upgrade 1.6.0 → 1.7.0, Version und Laufzeit, bytegenaue App-/Core-DLLs und Beispielprojekte, Startmenü und Desktopicon, Rückstufungssperre, Deinstallation und unveränderte persönliche Projekte innerhalb und außerhalb des Installationsordners. Er schützt vorhandene Benutzerinstallationen und Verknüpfungen. Ausgabe: `artifacts/installer-results-1.7.0.txt`; Protokolle: `artifacts/installer-upgrade-1.7.0`.

## Grenzen

Bohrungen gelten für die Leuchtkasten-Rückwand. Sie verriegeln den Kasten nicht mit der weiterhin losen Abdeckung. Keine Schraubenmodelle, Senkungen, Langlöcher oder genormten Halterungen. Die rechteckigen Freizonen sind konservativ; auch getrennte Kreise können abgewiesen werden. Gedrucktes Passspiel, Befestigung und Belastbarkeit wurden nicht praktisch vermessen. Weitere offene Arbeiten stehen im aktuellen Funktionsumfang.
