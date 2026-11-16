# Prüfprotokoll · LithoCreate Pro 1.3.0

Stand: 4. November 2026. Windows x64, SDK 10.0.403, WindowsDesktop 10.0.14.

## Automatisierte Prüfungen

90 Integrationstests bestanden, 0 fehlgeschlagen. Letzter Lauf: 32,7 Sekunden, 146 MB Peak Working Set. Dies ist eine Messung des Testlaufs, keine Speicherobergrenze für große Benutzermodelle. Ausgabe: `artifacts/test-results-1.3.0.txt`; Testdateien: `artifacts/tests-1.3.0`.

Die bisherigen 75 Prüfungen für Geometrie, STL/3MF, Projekte, Bildpipeline, Kamera, Wiederherstellung und Schichtschnitte bleiben enthalten. Neue bzw. erweiterte Prüfungen:

- Alle acht TIFF-EXIF-Ausrichtungen mit konkreten erwarteten Pixelanordnungen, einschließlich Spiegelungen und Achsentausch.
- JPEG-EXIF-Ausrichtung und ignorierte unbekannte Orientierungswerte.
- Bilineare Feinrotation gegen eine analytische lineare Helligkeitsfunktion, bei unveränderten Originalbytes und Bildmaßen.
- Keine leeren Eckkeile bei ±45° und schmalen/hohen Bildern bis hin zu einer Pixelspalte.
- Abweisung ungültiger und nicht endlicher Winkel, zeilenweiser Abbruch der Bildpipeline.
- Identische Geometrie beider Vergleichsseiten, bei unterschiedlichen Tonwerten.
- Tatsächliches WPF-Rendering des geteilten Vergleichs bei 25 und 75 Prozent. Die Pixelprüfung kontrolliert die Bildhälften nach einem Layout-/Renderupdate. Nicht endliche und außerhalb liegende Trennpositionen werden begrenzt.
- Weißes Zusammensetzen transparenter Pixel vor der Interpolation.
- Feinrotation in Undo/Redo und Reset; reine Vergleichsoptionen verändern das Projekt nicht.
- Format-1-/Format-2-Migration auf Format 3 mit deaktivierter nachträglicher EXIF-Ausrichtung; Bildreset und erneuter Export behalten dieselbe Ausrichtung.
- Speicherung und erneutes Laden von Feinrotation, Ausrichtungsmodus und Originalbytes.

Build der Lösung: keine Compilerwarnungen und keine Fehler.

## Oberfläche

42 WPF-Layouts bei 1400 × 900 und 960 × 640, einschließlich drei Trennpositionen bei aktiver Feinrotation und geänderten Tonwerten. Beide Seiten bleiben deckungsgleich. Die Synchronisierung von Winkelfeld und Regler in beide Richtungen sowie nach Undo wird anhand der tatsächlichen WPF-Bindungen geprüft. Die bisherigen Seiten und Druckschicht-Ansichten werden ebenfalls gerendert. Bilder: `artifacts/ui-layout-1.3.0`.

Der separate native Bedienungstest verwendet ausschließlich `artifacts/native-1.3.0` und einen eigenen Datenordner. Geprüft wurden das Einschalten des Vergleichs ohne Projektänderung, Verschieben der Linie per Maus, Pos1-Rücksetzen, Eingabe von 12,5°, automatische Vorschau und Speichern im eigenen Testprojekt. Die gespeicherte ZIP-Metadatei wurde anschließend unabhängig auf Format 3, Winkel 12,5 und aktivierte EXIF-Ausrichtung geprüft. Das Testfenster wurde regulär mit Exitcode 0 beendet. Die ältere Benutzersitzung wurde nicht verändert.

## Release und Installer

Eigenständiges Windows-x64-Paket mit Laufzeit, drei erneut eingelesenen Beispielprojekten in Format 3, Dokumentation, MSI, ZIP und SHA-256-Prüfsummen. Frühere Releases werden nicht überschrieben.

Reproduzierbarer Installer-Lebenszyklustest:

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.3.0-win-x64.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.2.0-win-x64.msi -TestRoot ./artifacts/installer-upgrade-1.3.0 -Desktop
```

Die einzelnen Ergebnisse und MSI-Protokolle stehen unter `artifacts/installer-results-1.3.0.txt` bzw. `artifacts/installer-upgrade-1.3.0`. Der Test schützt vorhandene Benutzerinstallationen und Verknüpfungen. Er kontrolliert Upgrade, Laufzeit, Version, Startmenü, Desktopicon, Beispielprojekte mit Umlauten, Rückstufungssperre, Deinstallation sowie unveränderte persönliche Projektdateien.

## Grenzen

Feinrotation mit automatischem Randbeschnitt, ohne Erweiterung der Leinwand. Kein gesondertes ICC-Farbmanagement. Der Vergleich zeigt Tonwert-/Farb-/Filteränderungen bei gleicher Bildgeometrie; die unveränderte Gesamtansicht bleibt separat erreichbar. Keine physische Druckprobe oder zusätzliche Hardwarefreigabe. Die übrigen Grenzen stehen im aktuellen Funktionsumfang.
