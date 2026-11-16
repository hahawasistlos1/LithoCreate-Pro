# Prüfprotokoll · LithoCreate Pro 1.14.0

Stand: 5. November 2026. Geprüft auf der verfügbaren Windows-x64-Entwicklungsumgebung mit .NET SDK 10.0.403 und WindowsDesktop 10.0.14. Keine physische Druckfreigabe.

## Build und Integration

Build ohne Warnungen oder Fehler. 201 Integrationstests bestanden, 0 fehlgeschlagen; letzter Lauf 29,0 Sekunden, Peak Working Set 237 MB. Ausgabe: `artifacts/test-results-1.14.0.txt`. Projektformat 12 bleibt unverändert; neun erneut geladene Beispielprojekte.

Sieben neue Prüfgruppen decken alle 15 Formen im eigenen Katalog, tiefe Kopien von Bohrungen, lokale Speicherung und Neustart, Namens-/Kennungskonflikte, ungültige Maße, 100-Einträge-/2-MB-Limits, unbekannte Versionen, Erhalt der letzten gültigen Datei, Schreibfehler ohne Teilübernahme, gesperrte Aktionen, bestätigtes Löschen und vollständige Zubehör-/Winkelübernahme ab. Beim Anwenden bleiben Originalbild, Bildkorrekturen, Druckerprofil, Projektname, aktuelles Speicherbudget und Bild-Undo/Redo erhalten. Vorlagenverwaltung allein verändert Dirty nicht. Für wiederholte Testläufe erhält jede Vorlagen-VM einen frischen isolierten Unterordner.

## Unabhängige Dateien

76 STL-/3MF-Dateien und drei zugeschnittene PNG-Dateien bestanden die unabhängigen Python-Prüfungen. `check_presets.py` prüft zwei nach Vorlagenübernahme ausgegebene Dateien: 40 × 30 × 3 mm, bekanntes Volumen 3600 mm³, positive konsistente Orientierung, geschlossene Kanten, endliche Koordinaten, keine degenerierten Dreiecke und Z=0. Zusätzlich werden die 15 Katalogformen und reine Modellmetadaten ohne Foto/Bildkorrekturen geprüft.

Die bisherigen Prüfungen wurden gegen `artifacts/tests-1.14.0` wiederholt: `check_dimensions.py` (8 Dateien), `check_crops.py` (3 PNGs), `check_sinks.py` (6), `check_slots.py` (6), `check_angles.py` (12), `check_printsets.py` (4 Zwei-Objekt-3MF), `check_mounting.py` (4), `check_back.py` (6), `check_socket.py` (4) und `check_placements.py` (24). JSON-Ergebnisse: `artifacts/*-files-1.14.0.json`.

## Oberfläche und native Bedienung

168 WPF-Layouts bei 1400 × 900 und 960 × 640 bestanden. Zwölf neue Aufnahmen zeigen leere Sammlung, gespeicherte Vorlage, Namenskonflikt, langen umbenannten Eintrag, angewendetes Modell und gelöschte Vorlage. Auswahl allein verändert das Projekt nicht. Bei der ersten Bildkontrolle fiel der technische Typname im Auswahlfeld auf. ModelPreset zeigt jetzt seinen Namen wie die vorhandenen Auswahltypen; der gesamte Test-/Oberflächenlauf wurde danach erfolgreich wiederholt. Die korrigierte kleine Namensansicht wurde visuell geprüft. Ausgabe: `artifacts/ui-results-1.14.0.txt`; Bilder: `artifacts/ui-1.14.0`.

Die eigenständig veröffentlichte Anwendung wurde mit eigener Kopie des neunten Beispiels und getrenntem Datenordner unter `artifacts/native-1.14.0` bedient. „Leuchtturm · Vorlage“ wurde mit 120 × 88 mm gespeichert und in „Leuchtturm · Wiederverwendbar“ umbenannt. Anschließend wurde die aktuelle Breite auf 150 mm geändert (gekoppelte Höhe 110 mm). Anwenden der Vorlage stellte 120 × 88 mm und die entsprechende neue Vorschau wieder her. Strg+S speicherte die eigene Projektkopie; die Sitzung wurde sauber mit Code 0 geschlossen.

`python tools/check_presets.py --native --inputs artifacts/native-1.14.0 --output artifacts/native-1.14.0/verification.json` bestand: Katalogversion 1, Modellformat 12, genau eine umbenannte Vorlage mit vollständigen ursprünglichen Modellparametern. Originalbildbytes und sämtliche übrigen Projektparameter einschließlich Bildkorrekturen und Druckerprofil blieben gleich. Lediglich der beim Speichern zum Dateinamen passende Projektname änderte sich. Andere Benutzerfenster wurden nicht bedient.

## Auslieferung und Installer

Eigenständige Windows-x64-Laufzeit, aktuelle Dokumentation und neun Format-12-Beispiele im normalen MSI und portablen ZIP. `python tools/check_package.py --version 1.14.0` vergleicht jede ZIP-Datei bytegenau, prüft die Beispiele, Laufzeit und SHA-256-Prüfsummen. Ergebnis: `artifacts/package-verification-1.14.0.json`.

Der MSI-Lebenszyklustest verwendet getrennte Installertest-Produkte mit eigenen Produkt-/Upgrade-/Komponentenkennungen und eigenen Verknüpfungen; die unveränderten Release-MSI-Dateien dienen als Referenzen. Das Benutzerprodukt wird nicht aktualisiert.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.14.0-installertest.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.13.0-installertest.msi -TestRoot ./artifacts/installer-upgrade-1.14.0 -Desktop -IsolatedIdentity -ReferenceMsiPath ./artifacts/LithoCreate-Pro-1.14.0-win-x64.msi -PreviousReferenceMsiPath ./artifacts/LithoCreate-Pro-1.13.0-win-x64.msi
```

Prüfumfang: Upgrade 1.13 → 1.14, bytegenaue App-/Core-Dateien und Beispiele, integrierte Laufzeit, eigene Startmenü-/Desktopverknüpfungen, Rückstufungssperre, saubere Deinstallation, Erhalt persönlicher Projektdateien und bytegenauer Erhalt bestehender Benutzerverknüpfungen. Ausgabe: `artifacts/installer-results-1.14.0.txt`; Logs: `artifacts/installer-upgrade-1.14.0`. „installertest“-MSIs sind keine Auslieferungspakete.

## Grenzen

Eigene Vorlagen speichern Parameter der vorhandenen Formen. Keine externen Form-Plugins oder importierten STL-Geometrien; Bilder und Druckerprofile werden nicht in die Sammlung kopiert. Der Katalog ist lokal und getrennt von Projektdateien. Beschädigte oder zukünftige Kataloge sperren Schreibaktionen bis zur Behebung und zum Neustart; Grundformen und Projekte bleiben nutzbar. Das Anwenden ersetzt Modelleinstellungen und gehört nicht zum Bild-Undo. Physische Druck-/Passungstests, unabhängige Slicerfreigaben, Intel-N95-/Windows-10-/OS-DPI-Hardwaretests und Codesignatur stehen weiter aus.
