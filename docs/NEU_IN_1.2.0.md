# Neu in LithoCreate Pro 1.2.0

## Geometrische Druckschichten

- Wechsel zwischen 3D-Modell und horizontalem Schichtschnitt direkt im Editor.
- Schichtregler, direkte Schichtnummer und Prüfschichthöhe von 0,01 bis 1,00 mm.
- Gefüllte Materialflächen mit offenen Innenräumen, Bohrungen und getrennten Konturen.
- Z-Höhe ab exportierter Auflage, Flächeninhalt, Konturzahl und tatsächliches Raster.
- XY-Gitternetz, Verschieben, Zoom, Zurücksetzen und PNG der aktuellen Schicht.
- Schnelles Vorschaumesh oder separat berechnetes Prüfmodell in Exportauflösung. Die volle Berechnung ist abbrechbar und verwendet dieselbe Bild- und Geometriepipeline wie STL/3MF.
- Ein begrenzter Höhenindex beschleunigt wiederholte Schnitte. Bei großer Indexgröße wird auf einen abbrechbaren Dreieckscan zurückgefallen.
- Bild-/Modelländerungen verwerfen gecachte Prüfdaten. Fehlerhafte Prüfschichthöhen entfernen die vorherige Kontur.

Die Schichtansicht zeigt die Geometrie. Extrusionsbahnen, Stützen, Materialverbrauch und Druckdauer werden weiterhin im Slicer erzeugt. Die Ansichtseinstellungen werden nicht im Projekt gespeichert; Projektformat 2 bleibt unverändert und mit 1.1.0 kompatibel.

## Flüssigerer Lichtregler

Eine Lichtänderung passt die Emissionsstärke der vorhandenen Textur an. Sie baut weder Mesh noch Texturbild neu auf und markiert das Projekt nicht mehr als geändert. Die warme Lichtdarstellung bleibt eine visuelle Annäherung.

## Installation

Eigenständige Windows-x64-Anwendung, MSI und portables ZIP. Version 1.2.0 erkennt frühere Installationen über die gemeinsame Upgrade-Kennung. Eine offene Anwendung vor dem Versionswechsel speichern und schließen. Vorhandene Projektdateien werden nicht vom Installer gelöscht.
