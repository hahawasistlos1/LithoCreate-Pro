# Prüfprotokoll · LithoCreate Pro 1.5.0

Stand: 4. November 2026. Windows x64, SDK 10.0.403, WindowsDesktop 10.0.14.

## Integrationstests

120 Tests bestanden, 0 fehlgeschlagen. Letzter Lauf: 27,4 Sekunden, 226 MB Peak Working Set. Dies ist eine Messung dieses Testlaufs, keine Speicherobergrenze für große Benutzermodelle. Ausgabe: `artifacts/test-results-1.5.0.txt`; Dateien: `artifacts/tests-1.5.0`.

Die bisherigen 107 Prüfungen bleiben enthalten. Zusätzlich:

- Sockel für Kugel und Mond mit radialem Spiel 0,1 / 0,25 / 0,6 mm: Topologie, positive Orientierung, Maße, freie Durchführung, tatsächlicher Abstand zur Kugelöffnung, Strukturkennzeichnung und STL-/3MF-Roundtrips.
- Volumenvergleich mit dem analytischen Volumen des rotationssymmetrischen Polygonprofils.
- Montage aus getrennten Körpern: unverändertes Summenvolumen, Kragen auf Sockelplatte, vier reguläre Konturen im Einsteckbereich und freier axialer Durchgang.
- Die Montage bleibt unabhängig von den Drucklagen der Teile aufrecht und wird auch auf Core-Ebene gegen gemeinsamen Export geschützt. Eine vorhandene Zieldatei bleibt erhalten.
- Abweisung zu kleiner Auflage, zu dünnen Rings, ungeeigneter Einsteckhöhe, fehlender Kugelöffnung, ungültiger Teilkennung und nicht endlicher Parameter; zusätzliche kleine gültige Konfiguration und Abbruch.
- Foto verändert den Sockel nicht. Seine Geometrie kann ohne unnötige Bilddekodierung exportiert werden.
- Format-4-Migration behält Bild-/Druckparameter ohne Zubehöraktivierung. Format 5 erhält Sockelmaße, Teilwahl und getrennte Drucklagen sowie Originalbytes.
- Teilwechsel, gesperrte Montageausgabe, Sockelschichten und Druckraumsuche auf dem aktiven Teil. Die Kugelausrichtung bleibt bei der Sockelsuche unverändert.

Build der Lösung: keine Compilerwarnungen und keine Fehler.

## Unabhängige Dateiprüfung

`python tools/check_socket.py` liest vier STL-/3MF-Dateien unabhängig mit der Python-Standardbibliothek. Alle bestanden: Referenzsockel 60 × 60 × 8 mm, positives Profilvolumen ca. 16.377,28 mm³, freier Kabeldurchmesser 8 mm und radiales Spiel zur Aufnahme 0,25 mm innerhalb der Float-Toleranz. Ergebnis: `artifacts/socket-files-1.5.0.json`.

Die 24 bisherigen Drucklagen-Dateien wurden ebenfalls unabhängig gelesen und auf Maße, positives Volumen und Z=0 geprüft: `python tools/check_placements.py --inputs artifacts/tests-1.5.0 --output artifacts/placement-files-1.5.0.json`.

Zusammen 28 unabhängige Dateiprüfungen. Dies sind Geometrie-/Dateiprüfungen, keine Slicer- oder Drucksimulation. Eine neue Bambu-CLI-Freigabe wird nicht behauptet; die Instabilität aus 1.3.0 ist separat dokumentiert.

## Oberfläche

62 WPF-Layouts bei 1400 × 900 und 960 × 640: zusätzlich Einzel-Sockel, Unterseite mit Durchführung, Sockelschicht, Montage und Exportseite ohne gemeinsame Druckausgabe. Strukturflächen verwenden explizite Flächennormalen; die Schulter bleibt sichtbar. Reine Sockelansichten enthalten keine Fototextur und keine Bildbeleuchtungsregler. Bilder: `artifacts/ui-layout-1.5.0`.

Der native Bedienungstest verwendet ein eigenes Beispielprojekt und einen getrennten Datenordner unter `artifacts/native-1.5.0`. Geprüft wurden Montage, Teilwechsel, Sockelschicht mit zwei Konturen und freier Durchführung sowie eine nur für den Sockel geänderte Bettrotation. Die erste Sockelschicht zeigte 2776,66 mm² bei 67 Prüfschichten mit eingestellter Höhe 0,12 mm. Nach dem Speichern wurden Format 5, Teilwahl, getrennte Bettrotation und unveränderte Originalbildbytes unabhängig aus der ZIP-Datei kontrolliert. Ergebnis: `artifacts/native-1.5.0/verification.json`. Die ursprüngliche Benutzersitzung blieb unberührt.

## Release und Installer

Eigenständige Windows-x64-Laufzeit, drei erneut geladene Format-5-Beispiele einschließlich Kugelmontage mit Sockel, Dokumentation, MSI, ZIP und SHA-256-Prüfsummen. Frühere Release-Dateien bleiben erhalten.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.5.0-win-x64.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.4.0-win-x64.msi -TestRoot ./artifacts/installer-upgrade-1.5.0 -Desktop
```

Der Lebenszyklustest schützt vorhandene Benutzerinstallationen und Verknüpfungen. Er prüft Upgrade, App-Version, Laufzeit, Startmenü, Desktopicon, bytegenaue Beispielprojekte, Rückstufungssperre, Deinstallation sowie unveränderte persönliche Projekte. Ausgabe: `artifacts/installer-results-1.5.0.txt`; MSI-Protokolle: `artifacts/installer-upgrade-1.5.0`.

## Grenzen

Die geometrische Passung ist geprüft; gedrucktes Passspiel, mechanische Belastbarkeit und Materialverhalten wurden nicht praktisch vermessen. Kein Gewinde, Rastverschluss oder genormter elektrischer Fassungsadapter. Keine Stützen, Werkzeugwege oder neue Hardwarefreigabe. Die übrigen Grenzen stehen im aktuellen Funktionsumfang.
