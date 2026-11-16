# Neu in LithoCreate Pro 1.8.0

## Beide Teile als gemeinsamer 3MF-Drucksatz

- Leuchtkasten/Rückwand sowie Kugel oder Mond mit Stecksockel gemeinsam exportieren.
- Zwei getrennte benannte Objekte; keine Vereinigung der Montagekörper.
- Eigene gespeicherte Drucklagen und Bettrotationen bleiben erhalten. Beide Teile beginnen auf Z=0.
- Anordnung in Zeile oder Spalte mit 5 mm Bettrand und Teileabstand. Tatsächliche Maße in Exportauflösung und Bauhöhe werden geprüft. Kein Skalieren oder automatisches Umdrehen.
- Bettübersicht mit farbigen Hüllrechtecken, Teilenamen und Druckmaßen; PNG-Export der Übersicht.
- Export erst nach erfolgreicher Berechnung; bei unpassendem Druckbett oder ungültigen Daten bleibt er gesperrt.
- Änderungen verwerfen veraltete Drucksatzdaten. Hintergrundberechnung und Schreiben bleiben abbrechbar.
- Erneutes Einlesen prüft beide gespeicherten Objekte einschließlich Namen, Positionen, Volumen und Maßen, bevor eine vorhandene Zieldatei ersetzt wird.

Die Datei ist Standard-3MF ohne Slicerprofile, Druckbahnen, Stützen oder Druckzeit-/Materialprognose. Die Übersicht zeigt Hüllrechtecke; freie Anordnungen und Teile aus mehreren Projekten sind nicht enthalten. Positionen und Stützstrategie im eigenen Slicer kontrollieren.

## Projekte bleiben in Format 7

Die Erweiterung benötigt keine neuen persistenten Geometrieparameter. Alle bisherigen Projekte bleiben lesbar; Format 7 kann weiterhin in 1.7.0 geöffnet werden. Die Bettanordnung wird aus dem aktuellen Projekt und Druckerprofil neu berechnet. Die vier Beispielprojekte bleiben enthalten.
