# Prüfprotokoll · LithoCreate Pro 1.13.0

Stand: 5. November 2026. Windows x64, SDK 10.0.403, WindowsDesktop 10.0.14.

## Funktion und Geometrie

194 Integrationstests bestanden, 0 fehlgeschlagen. Laufzeit 38,5 Sekunden, 221 MB Peak Working Set bei parallel gestarteter Oberflächenprüfung. Dies ist keine Speicherobergrenze großer Benutzermodelle. Ausgabe: `artifacts/test-results-1.13.0.txt`; Referenzen: `artifacts/tests-1.13.0`.

Die bisherigen 187 Tests bleiben enthalten. Sieben neue Fälle prüfen:

- Vier maßhaltige Quaderreferenzen: 40 × 30 × 3, 80 × 60 × 3, 60 × 45 × 3 und nach Entkopplung 30 × 45 × 3 mm. Bekannte Volumina, unveränderte 3-mm-Wandstärke, geschlossene Topologie und STL-/3MF-Roundtrips.
- Gekoppelte Breiten-/Höhenänderungen, Grenzen 5–1000 mm, nicht endliche Eingaben, Bereichsüberschreitung des jeweils zweiten Maßes und inaktive Bindung beim Formwechsel zum Zylinder.
- Übernahme des Pixelformats bei erhaltener Breite. Das tatsächlich zugeschnittene und um 90° gedrehte Motiv bestimmt die neue Höhe.
- Format-11-Migration erhält Maße, Bildausschnitt und Geometrie exakt und aktiviert keine Bindung. Format 12 erhält Bindung, Projektparameter und Originalbildbytes.
- ViewModel-Kopplung ohne Teilübernahme ungültiger Maße, Ablehnung eines geometrisch ungültigen Motivformats, Abweisung veralteter Berechnung und Abbruch ohne Größenänderung. Eine Kugel bietet die rechteckige Motivformat-Anpassung nicht an.

Build ohne Compilerwarnungen oder Fehler. Projektformat 12, Formate 1–11 bleiben lesbar. Apps bis 1.12.0 können Format 12 nicht öffnen.

## Unabhängige Referenzprüfung

74 STL-/3MF-Dateien und drei zugeschnittene PNG-Dateien bestanden unabhängige Prüfungen mit der Python-Standardbibliothek:

- `python tools/check_dimensions.py --inputs artifacts/tests-1.13.0 --output artifacts/dimension-files-1.13.0.json`: acht Größenreferenzen. Physische Druckachsen, bekannte Maße und Volumina, unveränderte Wandstärke, endliche Koordinaten, geschlossene konsistent orientierte Kanten, nicht degenerierte Dreiecke und Z=0.
- `check_crops.py`: drei PNG-Zuschnitte mit eigener Dekodierung und Pixelvergleich.
- `check_sinks.py`: sechs Senkungsdateien.
- `check_slots.py`: sechs Langlochdateien.
- `check_angles.py`: zwölf Druckwinkeldateien.
- `check_printsets.py`: vier Zwei-Objekt-3MF-Dateien.
- `check_mounting.py`: vier Rundbohrungsdateien.
- `check_back.py`: sechs Rückwand-/Aufnahmedateien.
- `check_socket.py`: vier Kugel-/Sockeldateien.
- `check_placements.py`: 24 rechtwinklige Drucklagen-Dateien.

Die Skripte wurden mit `--inputs artifacts/tests-1.13.0` und JSON-Ausgaben unter `artifacts/*-files-1.13.0.json` ausgeführt.

## Oberfläche und native Bedienung

156 WPF-Layouts bei 1400 × 900 und 960 × 640 bestanden. Vierzehn neue Ansichten zeigen gekoppelte Breiten-/Höhenänderung, Entkopplung, Motivformat-Anpassung, ungültige Größe, gekoppelte Bereichsüberschreitung und Korrektur. Bei den ersten drei neuen Aufnahmen wurde die vollständige Vorschauaktualisierung zusätzlich abgewartet und der Oberflächenlauf wiederholt. Die Maßfelder und gezeigte Modellgröße stimmen überein. Eine gekoppelte Bereichsüberschreitung verändert keines der beiden Modellmaße; der fehlerhafte Text wird markiert und der Export gesperrt. Bilder: `artifacts/ui-1.13.0`; Ausgabe: `artifacts/ui-results-1.13.0.txt`.

Die veröffentlichte App wurde mit eigener Kopie des neunten Beispiels und getrenntem Datenordner unter `artifacts/native-1.13.0` bedient. Ausgehend von 120 × 88 mm wurde die Kopplung gelöst und die Höhe auf 100 mm gesetzt. „Höhe an Motivformat anpassen“ berechnete anhand des 600 × 440-Pixel-Motivs wieder 120 × 88 mm und aktivierte die Bindung. Danach führte die Eingabe von 150 mm Breite automatisch zu 110 mm Höhe. Das Projekt wurde per Strg+S gespeichert und die volle Geometrie über den nativen 3MF-Dialog exportiert. Die App bestätigte das erneute Einlesen: 737468 Dreiecke, effektives Raster 0,300 mm.

`python tools/check_dimensions.py --native --inputs artifacts/native-1.13.0 --output artifacts/native-1.13.0/verification.json` prüfte diese tatsächliche 3MF-Datei unabhängig: Druckmaße 150 × 4 × 110 mm, positives Volumen 33910,2027 mm³, geschlossene orientierte Kanten, nicht degenerierte Dreiecke, endliche Koordinaten und Z=0. Die 4 mm entstehen wie zuvor am verstärkten Rahmen; die gespeicherten Wandstärken blieben unverändert. Originalbild und sämtliche übrigen Projektparameter blieben identisch; nur Breite, Höhe und der zum Dateinamen passende Projektname änderten sich. Die eigene Testsitzung endete mit Code 0. Andere Benutzersitzungen wurden nicht bedient.

## Pakete und Installertest

Eigenständige Windows-x64-Laufzeit, neun erneut geladene Format-12-Beispiele, aktuelle Dokumentation, MSI, portables ZIP und SHA-256-Prüfsummen. `python tools/check_package.py --version 1.13.0` vergleicht das ZIP bytegenau mit dem veröffentlichten Ordner und prüft die Beispiele einschließlich Maßkopplung, Verhältnisbindung, freier Winkel und Rückwandöffnungen. Ergebnis: `artifacts/package-verification-1.13.0.json`.

Der Installer-Lebenszyklus wird wie in 1.12.0 mit getrennten Testprodukten, Upgrade- und Komponentenkennungen, Registrykennung sowie eigenen Verknüpfungen geprüft. Vor der Installation werden die unveränderten Release-MSI-Dateien als Referenzen eingelesen und gemeinsame Komponenten-GUIDs ausgeschlossen. Die normale Releasekennung bleibt unverändert. Die installierte Benutzeranwendung wird nicht aktualisiert.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.13.0-installertest.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.12.0-installertest.msi -TestRoot ./artifacts/installer-upgrade-1.13.0 -Desktop -IsolatedIdentity -ReferenceMsiPath ./artifacts/LithoCreate-Pro-1.13.0-win-x64.msi -PreviousReferenceMsiPath ./artifacts/LithoCreate-Pro-1.12.0-win-x64.msi
```

Geprüft werden Upgrade 1.12.0 → 1.13.0 der Testprodukte, Version und Laufzeit, bytegenaue App-/Core-DLLs und neun Beispiele, Test-Startmenü und Test-Desktoplink, Rückstufungssperre, Deinstallation, unveränderte persönliche Projekte innerhalb/außerhalb des Testordners und bytegenauer Erhalt bestehender Benutzerverknüpfungen. Ausgabe: `artifacts/installer-results-1.13.0.txt`; Logs: `artifacts/installer-upgrade-1.13.0`. Die mit „installertest“ bezeichneten MSI-Dateien sind keine Auslieferungspakete.

## Grenzen

Maßkopplung für fünf rechteckige Fotoflächen, keine allgemeine Skalierung sämtlicher Geometrieparameter. Rahmenbreite, Radius, Standfuß- und Zubehörparameter sowie Bohrungspositionen bleiben in Millimetern erhalten. Nach Verkleinerung können Anpassungen nötig sein. Die Formatübernahme verwendet das tatsächliche bearbeitete Pixelformat einschließlich ganzzahliger Ausschnittgrenzen bei der Arbeitsauflösung. Spätere Bildkorrekturen aktualisieren die Modellmaße nicht automatisch. Modelländerungen sind nicht Teil des Bild-Undo. Physische Druck-/Passungstests, unabhängige Slicerfreigaben, N95-/Windows-10-/DPI-Hardwaretests und Codesignatur stehen weiter aus.
