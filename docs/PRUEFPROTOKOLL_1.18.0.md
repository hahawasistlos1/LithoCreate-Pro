# Prüfprotokoll · LithoCreate Pro 1.18.0

Stand: 5. November 2026. Windows x64, .NET SDK 10.0.403 / WindowsDesktop 10.0.14. Keine physische Druckfreigabe.

## Build und Integration

Build ohne Warnungen oder Fehler. 235 Integrationstests bestanden, 0 fehlgeschlagen; 36,9 Sekunden, Peak Working Set 234 MB. Ausgabe: artifacts/test-results-1.18.0.txt. Acht neue Prüfgruppen behandeln unveränderte Netze und Maße, exakte Rand-/Abstandsgrenzen, NaN/Infinity, ungültige Strukturen, atomaren Export nach nachträglicher Netzmutation, Übernahme automatischer Positionen, Format-15-Roundtrip, Format-14-Migration und Editorbewegungen ohne Netzneuberechnung.

## Oberfläche

230 WPF-Layouts bei 1400 × 900 und 960 × 640 bestanden. Sechzehn zusätzliche Aufnahmen zeigen manuelle Positionen, Körper-/Zubehörverschiebung, Zahlenbindung, ungültige Eingaben, negative Werte, Überschreitung der Bettgrenze und anschließende Korrektur. Die große Zahlenansicht und kleine Grenzansicht wurden visuell kontrolliert. Bilder: artifacts/ui-1.18.0; Ausgabe: artifacts/ui-results-1.18.0.txt.

## Unabhängige Dateien

92 STL-/3MF-Dateien und drei zugeschnittene PNG-Dateien bestanden unabhängige Prüfungen gegen artifacts/tests-1.18.0. Vier neue manuelle Drucksätze enthalten die bekannten Quadermaße 40 × 30 × 3 und 20 × 10 × 2 mm, Volumina 3600 und 400 mm³, explizite XY-Positionen, geschlossene orientierte Kanten, reine Translationsmatrizen und Z=0. Rand, Teileabstand und Bettgrenzen werden unabhängig geprüft. Das tatsächlich gespeicherte Projekt enthält Format 15 und die vier unveränderten Positionswerte.

Prüfer: check_manual.py (4), check_layouts.py (4), check_tabs.py (8), check_presets.py (2 und 15 Katalogformen), check_dimensions.py (8), check_crops.py (3 PNG), check_sinks.py (6), check_slots.py (6), check_angles.py (12), check_printsets.py (4), check_mounting.py (4), check_back.py (6), check_socket.py (4), check_placements.py (24). check_profiles.py prüft weiterhin die bisherigen JSON-Profillisten und die leere Bibliothek. Ergebnisse: artifacts/*-files-1.18.0.json.

## Native Bedienung

Die eigenständig veröffentlichte App wurde mit eigener Kopie des neuen Beispiels und getrenntem Datenordner unter artifacts/native-1.18.0 bedient. Ziehen änderte die Position des Leuchtkastens von 20/25 auf 50,6/45,7 mm. Pfeil rechts änderte X auf 51,6 mm, Umschalt+Pfeil oben Y auf 55,7 mm. Die Rückwand blieb auf 120/100 mm. Ein anschließender Ziehversuch über die Rückwand wurde mit einer Abstands-/Überlappungsmeldung abgewiesen; die letzte gültige Position blieb erhalten.

Strg+S speicherte die Projektkopie; „Drucksatz als 3MF speichern“ erzeugte zwei getrennte Objekte und meldete die erfolgreiche erneute Prüfung. check_manual.py --native bestand unabhängig: Projekt und 3MF stimmen in den Positionen überein; Originalbild, Geometrie, Drucker und Bildkorrekturen blieben erhalten. Geändert wurden nur die Körperposition und der zum Dateinamen passende Projektname. Maße: Leuchtkasten 48 × 40 × 16 mm / 68.920 Dreiecke, Rückwand 40 × 48 × 5 mm / 3.344 Dreiecke. Bericht: artifacts/native-1.18.0/verification.json. Die eigene Testsitzung endete mit Code 0. Andere Benutzerfenster wurden nicht bedient.

## Pakete und Installer

Eigenständige Windows-x64-Laufzeit, zwölf Format-15-Beispiele, aktuelle Dokumentation, MSI und portables ZIP mit SHA-256. Die persönliche Datei printers.json wird nicht mitgeliefert. tools/check_package.py vergleicht das ZIP bytegenau mit dem veröffentlichten Ordner, prüft Beispielparameter und Hashes; Bericht: artifacts/package-verification-1.18.0.json.

Der Installer-Lebenszyklustest verwendet isolierte Installertest-Produkte mit eigenen Produkt-/Upgrade-/Komponentenkennungen, Verknüpfungen und Registry-Pfaden. Unveränderte Release-MSIs dienen als Referenzen. Die vorhandene Benutzerinstallation wird nicht aktualisiert.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.18.0-installertest.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.17.0-installertest.msi -TestRoot ./artifacts/installer-upgrade-1.18.0 -Desktop -IsolatedIdentity -ReferenceMsiPath ./artifacts/LithoCreate-Pro-1.18.0-win-x64.msi -PreviousReferenceMsiPath ./artifacts/LithoCreate-Pro-1.17.0-win-x64.msi
```

Prüfumfang: Upgrade 1.17 → 1.18, bytegenaue App-/Core-Dateien und zwölf Beispiele, integrierte Laufzeit, Test-Startmenü-/Desktopverknüpfungen, Rückstufungssperre, Deinstallation sowie Erhalt persönlicher Projektdateien und Benutzerverknüpfungen. Ergebnisse: artifacts/installer-results-1.18.0.txt und artifacts/installer-upgrade-1.18.0. Die installertest-MSIs sind keine Auslieferungspakete.

## Grenzen

Freie Platzierung betrifft genau die zwei Teile des aktuellen Projekts und deren Hüllrechtecke. Kein Silhouettennesting, keine Mehrprojektmontage, keine zusätzlichen Rotationen oder Skalierungen. Die App erzeugt keine Slicerprofile, Stützen, Werkzeugwege oder G-Code. Projektformat 15 benötigt Version 1.18 oder neuer. Physische Druck-/Passungstests, unabhängige Slicerfreigaben, Intel-N95-/Windows-10-/OS-DPI-Hardwaretests und Codesignatur stehen weiter aus.
