# Neu in LithoCreate Pro 1.13.0

## Modellmaße proportional ändern

- „Breite und Höhe koppeln“ erhält das aktuelle Verhältnis der Fotofläche. Eine Änderung der Breite passt die Höhe an und umgekehrt.
- Für flaches und gebogenes Bild, Bilderrahmen, Nachtlicht und Leuchtkasten. Bei anderen Formen bleibt die gespeicherte Bindung inaktiv.
- Beide berechneten Maße müssen zwischen 5 und 1000 mm liegen. Eine unzulässige Änderung wird vollständig abgewiesen; das zweite Maß bleibt erhalten.
- Wandstärken, Rahmenbreite, Standfuß- und Zubehörparameter werden nicht mitskaliert. Positionen von Rückwandöffnungen bleiben in Millimetern und können nach einer Verkleinerung angepasst werden müssen.

## Höhe an das aktuelle Motivformat anpassen

Die gleichnamige Schaltfläche behält die Breite bei und berechnet die Höhe aus dem tatsächlich bearbeiteten Bild nach Zuschnitt und Drehung. Anschließend werden die Maße gekoppelt. Damit lässt sich eine Dehnung des Motivs auf rechteckigen Fotoflächen vermeiden. Die Berechnung läuft im Hintergrund, lässt sich abbrechen und übernimmt keine veralteten Ergebnisse. Unzulässige Geometrie oder unpassende Zielmaße werden ohne Änderung des Projekts gemeldet.

Diese Modelländerungen gehören wie bisher nicht zum Bild-Undo. Die Kopplung folgt dem aktuellen Modellverhältnis und wird nicht automatisch nach jeder späteren Bildbearbeitung neu berechnet. Nach einem neuen Zuschnitt die Anpassung erneut ausführen.

Das neunte Beispiel „Leuchtturm mit Formatbindung“ zeigt 120 × 88 mm, passend zum unbearbeiteten 600 × 440-Pixel-Motiv, mit aktivierter Kopplung und ohne Standfuß.

## Projektformat 12

Format 12 speichert `LockDimensions`. Formate 1–11 bleiben lesbar und starten ohne aktivierte Maßkopplung. Vorhandene Bildausschnitte, Maße und Drucklagen bleiben erhalten. Apps bis 1.12.0 können Format 12 nicht lesen; ursprüngliche Dateien für einen Rückwechsel behalten oder unter neuem Namen speichern.
