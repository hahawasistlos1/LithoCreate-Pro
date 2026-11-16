# Prüfprotokoll · LithoCreate Pro 1.16.0

Stand: 5. November 2026. Verfügbare Windows-x64-Entwicklungsumgebung mit .NET SDK 10.0.403 und WindowsDesktop 10.0.14. Keine physische Druckfreigabe.

## Build und Integration

Build ohne Warnungen oder Fehler. 218 Integrationstests bestanden, 0 fehlgeschlagen; letzter Lauf 30,9 Sekunden, Peak Working Set 235 MB. Ausgabe: `artifacts/test-results-1.16.0.txt`. Elf erneut geladene Beispielprojekte im Projektformat 14. Eigene Modellvorlagen verwenden weiterhin Modellformat 13.

Acht neue Prüfgruppen behandeln eigene Zeile/Spalte/Automatik und eine exakte Bettgrenze mit 0 mm Rand und 1 mm Teileabstand, bekannte gespeicherte Positionen, Wertebereiche/NaN/Unendlich, feste unpassende Richtung, unveränderte Standardplatzierung, Format-13-Migration, Format-14-Roundtrip und fehlgeschlagenes Speichern ohne Dateiverlust. Zusätzlich werden tiefe Optionsschnappschüsse, unveränderte Einzelteilnetze, Abbruch, Verwerfen veralteter Daten und Erhalt von Foto, Bildkorrekturen, Modell und Drucker geprüft. Eigene Modellvorlagen überschreiben die aktuelle Drucksatzplanung nicht.

## Unabhängige Referenzdateien

88 STL-/3MF-Dateien und drei zugeschnittene PNG-Dateien bestanden unabhängige Python-Prüfungen. Vier neue Zwei-Objekt-3MF-Dateien werden von `check_layouts.py` geprüft: bekannte Quadermaße und Volumina (3600/400 mm³), OPC-Einträge, Millimeter, benannte getrennte Objekte, endliche Koordinaten, geschlossene orientierte Kanten, keine degenerierten Dreiecke, Z=0, tatsächliche Randabstände, exakte zentrierte Positionen, Teileabstände und Bauhöhen. Varianten: Row, Column, Auto und Tight.

Bisherige Prüfungen gegen `artifacts/tests-1.16.0`: `check_tabs.py` (8 Dateien), `check_presets.py` (2 Dateien und 15 Katalogformen), `check_dimensions.py` (8), `check_crops.py` (3 PNGs), `check_sinks.py` (6), `check_slots.py` (6), `check_angles.py` (12), `check_printsets.py` (4 Zwei-Objekt-3MF), `check_mounting.py` (4), `check_back.py` (6), `check_socket.py` (4) und `check_placements.py` (24). JSON-Ergebnisse: `artifacts/*-files-1.16.0.json`.

## Oberfläche und native Bedienung

196 WPF-Layouts bei 1400 × 900 und 960 × 640 bestanden. Vierzehn neue Aufnahmen zeigen Zeile, Spalte, Automatik, ungültige Zahlen, Bereichsüberschreitung, Korrektur und eine feste unpassende Richtung. Die Bildkontrolle deckte auf, dass eine Statusänderung das fehlerhafte Abstandsfeld nach oben aus dem sichtbaren Bereich scrollte. Die Rücksetzung erfolgt jetzt nur beim Eintritt ohne Eingabefehler in die Drucksatzansicht. Die Ansicht bleibt bei Fehlern zur Korrektur erreichbar, Berechnung/Export bleiben gesperrt. Ein Regressionstest wechselt bei fehlerhaftem Abstand zur Oberfläche und zurück und prüft die vollständig sichtbare rote Eingabe. Der Oberflächenlauf wurde nach den Korrekturen vollständig wiederholt. Bilder: `artifacts/ui-1.16.0`; Ausgabe: `artifacts/ui-results-1.16.0.txt`.

Die eigenständig veröffentlichte App wurde mit eigener Kopie des elften Beispiels und getrenntem Datenordner unter `artifacts/native-1.16.0` bedient. Die gespeicherte Planung startete mit 12 mm Rand, 9 mm Teileabstand und Spalte. Der Teileabstand wurde auf 12,5 mm geändert, der Drucksatz neu berechnet, das Projekt per Strg+S gespeichert und über den nativen Dialog als „Anordnungs-Test – Drucksatz.3mf“ exportiert. Die App bestätigte erneutes Einlesen beider Objekte. Die eigene Sitzung wurde mit Code 0 geschlossen; andere Benutzerfenster wurden nicht bedient.

`python tools/check_layouts.py --native --inputs artifacts/native-1.16.0 --output artifacts/native-1.16.0/verification.json` bestand unabhängig:

- Leuchtkasten: 48 × 40 × 16 mm, 68920 Dreiecke, XY-Position 86 / 59,75 mm, positives Volumen 10533,4460 mm³.
- Rückwand: 40 × 48 × 5 mm, 3344 Dreiecke, XY-Position 90 / 112,25 mm, positives Volumen 4327,9730 mm³.
- Beide Objekte stehen lokal auf Z=0; die gesamte Anordnung ist zentriert auf 220 × 220 mm, Bauhöhe maximal 250 mm, mindestens 12 mm Rand und exakt 12,5 mm Teileabstand.
- Originalbildbytes und sämtliche übrigen Projektparameter bleiben identisch. Nur Teileabstand und der beim Speichern zum Dateinamen passende Projektname änderten sich.

## Pakete und Installer

Eigenständige Windows-x64-Laufzeit, elf Format-14-Beispiele, Dokumentation, MSI und portables ZIP mit SHA-256-Prüfsummen. `python tools/check_package.py --version 1.16.0` vergleicht das ZIP bytegenau, prüft Laufzeit, Beispielparameter einschließlich eigener Drucksatzplanung und Prüfsummen. Ergebnis: `artifacts/package-verification-1.16.0.json`.

Der Lebenszyklustest verwendet getrennte Installertest-Produkte mit eigenen Produkt-/Upgrade-/Komponentenkennungen sowie eigenen Verknüpfungen; unveränderte Release-MSI-Dateien dienen als Referenzen. Das vorhandene Benutzerprodukt wird nicht aktualisiert.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.16.0-installertest.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.15.0-installertest.msi -TestRoot ./artifacts/installer-upgrade-1.16.0 -Desktop -IsolatedIdentity -ReferenceMsiPath ./artifacts/LithoCreate-Pro-1.16.0-win-x64.msi -PreviousReferenceMsiPath ./artifacts/LithoCreate-Pro-1.15.0-win-x64.msi
```

Prüfumfang: Upgrade 1.15 → 1.16, bytegenaue App-/Core-Dateien und elf Beispiele, integrierte Laufzeit, Test-Startmenü-/Desktopverknüpfungen, Rückstufungssperre, Deinstallation, Erhalt persönlicher Projektdateien und vorhandener Benutzerverknüpfungen. Ausgabe: `artifacts/installer-results-1.16.0.txt`; Logs: `artifacts/installer-upgrade-1.16.0`. „installertest“-MSIs sind keine Auslieferungspakete.

## Grenzen

Zwei Einzelteile des aktuellen Projekts in einer zentrierten Zeile oder Spalte. Keine freie Platzierung, beliebige Winkeloptimierung, Silhouetten-Nesting, Werkzeugwege oder Stützen. Zusätzliche Freiräume für Brim/Skirt und besondere Druckerbereiche sind selbst zu berücksichtigen. Neue Projektdateien benötigen Version 1.16 oder neuer; ältere Projekte bleiben lesbar mit den bisherigen 5-mm-Defaults. Physische Druck-/Passungstests, unabhängige Slicerfreigaben, Intel-N95-/Windows-10-/OS-DPI-Hardwaretests und Codesignatur stehen weiter aus.
