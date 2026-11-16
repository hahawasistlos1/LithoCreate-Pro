# Neu in LithoCreate Pro 1.3.0

## Fotoeditor

- Feinrotation von −45° bis +45°, kombinierbar mit den vorhandenen 90°-Drehungen und Spiegelung.
- Bilineare Abtastung mit automatischem Zoom und Randbeschnitt. Seitenverhältnis und Pixelmaße des gewählten Ausschnitts bleiben erhalten; keine leeren Eckbereiche.
- Regler, direkte Winkeleingabe, „Auf 0°“ und Undo/Redo. Dieselbe Bildkorrektur wird in Vorschau, Prüfmodell und Modellexport verwendet.
- Verschiebbarer Vorher-/Nachher-Trenner. Links ohne Tonwert-, Farb- und Filterkorrekturen, rechts mit diesen Korrekturen. Ausschnitt, Drehung und Spiegelung stimmen auf beiden Seiten überein.
- Bedienung der Trennlinie mit Maus oder Tastatur. Die Originalansicht bleibt für die Ausschnittwahl verfügbar.
- Automatische Ausrichtung neuer JPEG-/TIFF-Fotos anhand aller acht EXIF-Orientierungen, einschließlich gespiegelter Varianten.
- Transparenz wird vor der Feinrotation auf Weiß zusammengesetzt, um dunkle Säume zu vermeiden.

Die Feinrotation beschneidet Randbereiche. Der geteilte Vergleich und seine Trennposition sind reine Ansichtsoptionen und werden nicht im Projekt gespeichert.

## Projekte und Kompatibilität

Neue Dateien verwenden Projektformat 3. Format 1 und 2 bleiben lesbar. Bei der Übernahme wird die bisherige Bildausrichtung ausdrücklich beibehalten; auch Bildreset, Speichern und Export aktivieren bei diesen Projekten keine nachträgliche EXIF-Drehung. Originalbildbytes bleiben unverändert.

Frühere Anwendungen bis einschließlich 1.2.0 weisen Format 3 als unbekannte Version ab. Für einen Rückwechsel die ursprüngliche Datei behalten oder in 1.3.0 „Speichern unter“ verwenden. Die neue Dateiversion verhindert, dass eine ältere App die Feinrotation beim Öffnen stillschweigend entfernt.

## Bereitstellung

Eigenständige Windows-x64-Anwendung mit Laufzeit, MSI und portablem ZIP. Die bisherigen Modellformen, Druckschichten und Beispieldateien bleiben enthalten. Ein Versionswechsel wird über den nativen Windows Installer durchgeführt; persönliche Projekte bleiben erhalten.
