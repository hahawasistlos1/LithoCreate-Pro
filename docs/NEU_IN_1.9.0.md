# Neu in LithoCreate Pro 1.9.0

## Freie Druckwinkel pro Einzelteil

- Drei Zahlenfelder für X, Y und Z, jeweils −180° bis +180° mit Dezimalwerten.
- Eigene Winkel für Lithophanie, Stecksockel und Rückwand. Zurücksetzen betrifft nur das aktive Teil.
- Druckerachsen: X/Y auf dem Bett, Z nach oben. Feste Reihenfolge: Grundlage → X → Y → Z → optionale 90°-Bettrotation. Drehpunkt ist die Mitte der Modellhülle nach der Grundausrichtung.
- Dieselbe Geometrie in Vorschau, Druckschichten, STL, Einzelmodell-3MF und Zwei-Objekt-Drucksatz. Foto, Wandstärken, UVs und Maßstab bleiben erhalten; die Montageansicht bleibt aufrecht.
- Die Druckraumsuche behält eine passende freie Lage anhand der tatsächlichen Modellpunkte bei. Sonst sucht sie zwölf rechtwinklige Lagen ohne freie Winkel und setzt bei einem Treffer nur die Winkel des aktiven Teils zurück. Ohne Treffer bleibt die Auswahl erhalten.
- Ungültige Zahlen und Bereichsüberschreitungen verwerfen veraltete Vorschauen und sperren den Export. Korrigieren oder „Freie Winkel zurücksetzen“ ermöglicht die Wiederaufnahme.
- Der Einzelmodell-Speicherdialog beginnt wie der Drucksatzdialog im Ordner des geöffneten Projekts.

## Projekte und Beispiele

Projektformat 8 speichert die drei unabhängigen Winkelsätze. Formate 1–7 bleiben lesbar und erhalten freie Winkel von null. Der Nullwinkelpfad verwendet weiterhin die bisherigen exakten Achsentransformationen. Die ursprünglichen Fotos werden unverändert gespeichert. Apps bis 1.8.0 weisen Format 8 ab; für einen Rückwechsel die alte Datei behalten oder unter neuem Namen speichern.

Fünf Beispiele sind enthalten. „Leuchtturm mit Druckwinkeln“ zeigt ein flaches Bild ohne Standfuß mit X=20°, Y=0°, Z=15°.

Die App sucht keine optimalen beliebigen Drehwinkel und berechnet keine Stützen, Druckbahnen oder Auflagestabilität. Drucklage und Stützstrategie im Slicer prüfen. Praktische Druck- und Passungsversuche bleiben erforderlich.
