# Prüfprotokoll · LithoCreate Pro 1.6.0

Stand: 4. November 2026. Windows x64, SDK 10.0.403, WindowsDesktop 10.0.14.

## Funktion und Geometrie

132 Integrationstests bestanden, 0 fehlgeschlagen. Lauf mit paralleler UI-Prüfung: 30,8 Sekunden, 211 MB Peak Working Set. Diese Messung ist keine Speicherobergrenze großer Benutzermodelle. Ausgabe: `artifacts/test-results-1.6.0.txt`; Geometriedateien: `artifacts/tests-1.6.0`.

Die bisherigen 120 Tests bleiben enthalten. Zusätzlich:

- Rückwände geschlossen und mit 8-mm-Kabelöffnung, jeweils bei 0,1 / 0,25 / 0,6 mm Spiel: geschlossene Topologie, Orientierung, exakte Außenmaße, Einsteckrandmaße, fehlende Fototextur, reguläre Schichtkonturen und Flächen. STL-/3MF-Roundtrips der Referenzteile.
- Exakte Kastenaufnahme in Vorschau und Export: 80 × 60 mm außen, 74 × 54 mm innen, 18 mm Tiefe, positive Orientierung und analytisch berechnetes Volumen bei konstanter Fotohelligkeit.
- Rückwandmontage mit unveränderten Einzelteilvolumina, aufliegender Platte und vier getrennten Konturen im Einsteckbereich. Die Drucklagen der Einzelteile drehen die Montage nicht. Gemeinsamer Export wird vor einer Dateiänderung abgewiesen.
- Mindestmaterial, Randstärke, verbleibende Innenfläche, Spiel, Kabelöffnung, Einstecktiefe, endliche Zahlen und Ausrichtungskennungen. Abbruch sowie schmale, quadratische und stark längliche Geometrie ohne degenerierte Ecken.
- Foto-unabhängiger Zubehör-Export, Format-5-Migration mit identischer bisheriger Kastengeometrie und Format-6-Roundtrip aller Rückwandparameter sowie unveränderter Originalbytes.
- Editorwechsel zwischen Teilen, gesperrte Montageausgabe, Rückwandschichten und Druckraumsuche auf dem aktiven Teil. Die Kastenausrichtung bleibt erhalten.

Build ohne Compilerwarnungen oder Fehler. Die Teilanzeige ist an die stabile Teilkennung gebunden; die sichtbare App-Version wird aus der Assembly gelesen.

## Unabhängige Dateiprüfung

Python liest die tatsächlichen STL-/3MF-Dateien ohne Verwendung der C#-Geometrie- oder Importimplementierung. Zusammen 34 Dateiprüfungen bestanden:

- `python tools/check_back.py`: sechs Dateien, exakte Kastenöffnung, Platten-/Ringmaße, 0,25 mm Spiel pro Seite, analytische Volumina, freie 8-mm-Kabelöffnung und Export auf Z=0. Ergebnis: `artifacts/back-files-1.6.0.json`.
- `python tools/check_socket.py --inputs artifacts/tests-1.6.0 --output artifacts/socket-files-1.6.0.json`: vier Dateien der Kugel-/Sockelpassung.
- `python tools/check_placements.py --inputs artifacts/tests-1.6.0 --output artifacts/placement-files-1.6.0.json`: 24 Drucklagen-Dateien.

Dies sind Geometrie-/Dateiprüfungen, keine Slicer- oder Drucksimulation. Keine neue Bambu-CLI-Freigabe; die frühere Instabilität ist separat dokumentiert.

## Oberfläche und native Bedienung

72 WPF-Layouts bei 1400 × 900 und 960 × 640. Zehn neue Ansichten zeigen Kastenaufnahme, einzelne Rückwand, Schichtschnitt, Montage und geschützte Montage-Ausgabe. Zusätzlich wird geprüft, dass das Kastenteil im Auswahlfeld nach dem Formwechsel angezeigt wird. Bilder: `artifacts/ui-layout-1.6.0`.

Die veröffentlichte App wurde mit einem eigenen Beispielprojekt und getrenntem Datenordner unter `artifacts/native-1.6.0` gestartet. Montage, Teilwechsel, silberne Rückwand ohne Fototextur, Kabelöffnung und Schichtwechsel wurden tatsächlich bedient. Die erste Schicht zeigte 8449,73 mm² und zwei Konturen. Schicht 25 bei 2,94 mm zeigte den offenen Einsteckring mit 540,16 mm² und zwei Konturen. Die Bettrotation wurde ausschließlich für die Rückwand aktiviert und gespeichert. Danach wurden Format 6, aktive Rückwand, getrennte Drucklagen und unveränderte Originalbildbytes unabhängig aus dem Projekt gelesen. Die App beendete sich mit Code 0. Ergebnis: `artifacts/native-1.6.0/verification.json`. Die ältere Benutzersitzung blieb unverändert geöffnet.

## Pakete und Installer

Eigenständige Windows-x64-App, vier erneut geladene Format-6-Beispiele, Dokumentation, MSI, portables ZIP und SHA-256-Prüfsummen. Die vorherigen Releases bleiben erhalten.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.6.0-win-x64.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.5.0-win-x64.msi -TestRoot ./artifacts/installer-upgrade-1.6.0 -Desktop
```

Der isolierte Lebenszyklustest prüft Upgrade 1.5.0 → 1.6.0, App-Version und Laufzeit, bytegenaue App-/Core-DLLs und Beispielprojekte, Startmenü und Desktopicon, Rückstufungssperre, Deinstallation sowie unveränderte persönliche Projekte innerhalb und außerhalb des Installationsordners. Vorhandene Benutzerinstallationen und Verknüpfungen werden geschützt. Ausgabe: `artifacts/installer-results-1.6.0.txt`; MSI-Protokolle: `artifacts/installer-upgrade-1.6.0`.

## Grenzen

Lose Steckabdeckung ohne Rastverschluss, Schrauben oder genormte LED-Aufnahme. Geometrisches Spiel ist geprüft; mechanischer Halt, gedruckte Passung, thermisches Materialverhalten und Lichtwirkung wurden nicht praktisch vermessen. Kein kalibrierter Diffusor, keine elektrische Baugruppe, keine neue Hardwarefreigabe. Weitere offene Arbeiten stehen im aktuellen Funktionsumfang.
