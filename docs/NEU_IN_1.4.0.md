# Neu in LithoCreate Pro 1.4.0

## Drucklage und Druckraum

- Sechs Drucklagen und eine zusätzliche 90°-Bettrotation.
- Tatsächliche Geometrietransformation bei gleicher Fotozuordnung und Materialstärke.
- Gemeinsame Lage für 3D-Vorschau, geometrische Druckschichten, Maßanzeige, Volumenwarnung und STL/3MF.
- Automatische Suche nach einer passenden rechtwinkligen Lage im aktiven Druckerprofil. Grundlage ist die geprüfte Geometrie in Exportauflösung mit dem eingestellten Speicherbudget.
- Bereits passende Lagen bleiben erhalten; eine erfolglose oder abgebrochene Suche verändert die Auswahl nicht.
- Exportmaße werden als Breite × Tiefe × Bauhöhe angezeigt. Wie bisher wird der Export auf Z=0 gesetzt.
- Lagewechsel verwerfen veraltete Prüfmodelle und passen die Kamera neu ein. Die Reset-Perspektive berücksichtigt flach bzw. seitlich stehende Platten.

Die Suche optimiert die Hüllenpassung. Sie erzeugt keine Stützen oder Werkzeugwege und bewertet keine optimale Druckqualität. Nicht rechtwinklige Druckwinkel, Zentrieren auf einem bestimmten Bett und das Anordnen mehrerer Objekte erfolgen weiterhin im Slicer.

## Projektformat 4

Die Drucklage und Bettrotation werden gespeichert. Projekte aus Format 1, 2 und 3 bleiben lesbar und behalten ihre frühere aufrechte Drucklage. Die bisherige EXIF-Migration und Bildkorrekturen bleiben erhalten. Ältere Anwendungen bis 1.3.0 weisen Format 4 ab, statt eine gespeicherte Drucklage zu ignorieren. Für einen Rückwechsel die ursprüngliche Projektdatei behalten oder „Speichern unter“ verwenden.

MSI und portables ZIP werden mit derselben eigenständigen Windows-x64-Laufzeit veröffentlicht. Frühere Release-Dateien bleiben erhalten.
