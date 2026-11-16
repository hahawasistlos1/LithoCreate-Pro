# Prüfprotokoll · LithoCreate Pro 1.12.0

Stand: 4. November 2026. Windows x64, SDK 10.0.403, WindowsDesktop 10.0.14.

## Funktion und Bildausschnitte

187 Integrationstests bestanden, 0 fehlgeschlagen. Laufzeit 31,1 Sekunden, 235 MB Peak Working Set. Dies ist keine Speicherobergrenze großer Benutzermodelle. Ausgabe: `artifacts/test-results-1.12.0.txt`; Referenzen: `artifacts/tests-1.12.0`.

Die bisherigen 180 Tests bleiben enthalten. Sieben neue Fälle prüfen:

- 4:5-, 16:9- und 5:2-Zuschnitte: zentriertes normiertes Rechteck, erwartete Pixelmaße, unverändertes Original und weiterhin gültige Geometrie mit unveränderten Modellmaßen.
- Gebundene Auswahl in allen vier Ziehrichtungen, bei neun Startpunkten und 25 Zeigerpositionen je Startpunkt, einschließlich außerhalb des Bildes; freie Auswahl behält die bisherigen Koordinaten.
- Zulässige Extremwerte sowie Abweisung von null, zu kleinen/großen und nicht endlichen Verhältniswerten, ungültigen Bildmaßen und Zeigerkoordinaten.
- Format-10-Migration übernimmt den bisherigen Ausschnitt mit pixelidentischer bearbeiteter Ausgabe. Format 11 erhält Verhältniswahl, Bindung und Originalfoto.
- ViewModel-Verhältniswahl, mittige Anwendung, Undo/Redo, Originalmodus für Mausauswahl, Rücksetzen und Abweisung ungültiger Werte ohne Änderung der Modellmaße.

Build ohne Compilerwarnungen oder Fehler. Projektformat 11, Formate 1–10 bleiben lesbar. Apps bis 1.11.0 können Format 11 nicht öffnen.

## Unabhängige Referenzprüfung

66 STL-/3MF-Dateien und drei zugeschnittene PNG-Dateien bestanden unabhängige Prüfungen mit der Python-Standardbibliothek:

- `python tools/check_crops.py --inputs artifacts/tests-1.12.0 --output artifacts/crop-files-1.12.0.json`: eigene PNG-Dekodierung mit CRC, Filterrekonstruktion, Bildmaßen und Vergleich jedes RGB-Pixels mit dem mathematisch bestimmten Quellbereich. Maximal ein Tonwertschritt Rundungsabweichung ist erlaubt. Alpha bleibt vollständig deckend.
- `check_sinks.py`: sechs Senkungsdateien mit Tiefenprofil und Fasensteigung.
- `check_slots.py`: sechs Langlochdateien.
- `check_angles.py`: zwölf Druckwinkeldateien.
- `check_printsets.py`: vier Zwei-Objekt-3MF-Dateien.
- `check_mounting.py`: vier Rundbohrungsdateien.
- `check_back.py`: sechs Rückwand-/Aufnahmedateien.
- `check_socket.py`: vier Kugel-/Sockeldateien.
- `check_placements.py`: 24 rechtwinklige Drucklagen-Dateien.

Die Skripte wurden mit `--inputs artifacts/tests-1.12.0` und JSON-Ausgaben unter `artifacts/*-files-1.12.0.json` ausgeführt.

## Oberfläche und native Bedienung

142 WPF-Layouts bei 1400 × 900 und 960 × 640 bestanden. Zwölf neue Ansichten zeigen 4:5, eigene Dezimalwerte 7,5:10, 16:9, ungültigen Text, Grenzwert null und gültige Korrektur. Die Eingaben steuern den tatsächlichen Bildausschnitt. Ungültige Werte sperren Zuschneiden und Modell-Export; null wird nicht in das Projekt übernommen. Hochformat, eigenes Verhältnis, Breitformat und Fehlerzustand wurden visuell kontrolliert. Bilder: `artifacts/ui-1.12.0`; Ausgabe: `artifacts/ui-results-1.12.0.txt`.

Die veröffentlichte App wurde mit eigener Kopie des achten Beispiels und getrenntem Datenordner unter `artifacts/native-1.12.0` bedient. Beim ersten Start war die Beispieldatei noch nicht fertig veröffentlicht; die leere eigene Sitzung wurde geschlossen und nach Abschluss des Builds korrekt neu gestartet. Im Fotoeditor wurden 16 und 9 eingegeben, der mittige Zuschnitt angewandt und anschließend im Originalbild tatsächlich ein gebundener Ausschnitt mit der Maus aufgezogen. Nach Strg+S war der neue Ausschnitt gespeichert.

`python tools/check_crops.py --native --inputs artifacts/native-1.12.0 --output artifacts/native-1.12.0/verification.json` prüfte die gespeicherte Projektkopie unabhängig: Quelle 600 × 440 Pixel, Verhältniswerte 16:9 mit aktiver Bindung; normalisierte Auswahl X=0,1237468843, Y=0,0283827302, Breite=0,7605116169, Höhe=0,5833469789. Das Pixel-Seitenverhältnis entspricht 16:9. Alle übrigen Bild-/Modellparameter und die Originalbildbytes blieben unverändert; nur der Projektname wurde passend zum Dateinamen aktualisiert. Die eigene Testsitzung endete mit Code 0. Andere Benutzersitzungen wurden nicht bedient.

## Pakete und Installertest

Eigenständige Windows-x64-Laufzeit, acht erneut geladene Format-11-Beispiele, aktuelle Dokumentation, MSI, portables ZIP und SHA-256-Prüfsummen. `python tools/check_package.py --version 1.12.0` vergleicht das ZIP bytegenau mit dem veröffentlichten Ordner und prüft die Beispiele einschließlich Verhältnisbindung, freier Winkel und Rückwandöffnungen. Ergebnis: `artifacts/package-verification-1.12.0.json`.

Da Benutzerverknüpfungen vorhanden sind, läuft der Installer-Lebenszyklustest mit ausdrücklich getrennten Testpaketen. Diese besitzen eigene Produkt-, Upgrade- und sämtliche Komponentenkennungen, eigene Verknüpfungen und eine eigene Registrykennung. Vor dem Test werden die unveränderten Release-MSI-Dateien als Referenzen eingelesen und gemeinsame Komponenten-GUIDs ausgeschlossen. Die normale Releasekennung bleibt unverändert. Damit wird der Lebenszyklus der gleichen Programmdateien und Installeraktionen geprüft; die installierte Benutzeranwendung wird nicht aktualisiert.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.12.0-installertest.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.11.0-installertest.msi -TestRoot ./artifacts/installer-upgrade-1.12.0 -Desktop -IsolatedIdentity -ReferenceMsiPath ./artifacts/LithoCreate-Pro-1.12.0-win-x64.msi -PreviousReferenceMsiPath ./artifacts/LithoCreate-Pro-1.11.0-win-x64.msi
```

Geprüft werden Upgrade 1.11.0 → 1.12.0 der Testprodukte, Version und Laufzeit, bytegenaue App-/Core-DLLs und acht Beispiele, Test-Startmenü und Test-Desktoplink, Rückstufungssperre, Deinstallation, unveränderte persönliche Projekte innerhalb/außerhalb des Testordners und bytegenauer Erhalt bestehender Benutzerverknüpfungen. Ausgabe: `artifacts/installer-results-1.12.0.txt`; Logs: `artifacts/installer-upgrade-1.12.0`. Die mit „installertest“ bezeichneten MSI-Dateien sind keine Auslieferungspakete.

## Grenzen

Pixelgrenzen werden ganzzahlig abgerundet; extreme Verhältnisse können sehr wenige Bildpunkte enthalten. Die Mausauswahl erfolgt im ausgerichteten Original bei bestehender Arbeitsauflösung. Sie ändert keine Modellabmessungen; bei abweichendem Modellformat kann das Motiv weiterhin gedehnt werden. Kein zusätzlicher nativer Slicer- oder physischer Drucktest. N95-/Windows-10-/DPI-Hardwaretests und Codesignatur stehen weiter aus.
