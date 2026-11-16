# Neu in LithoCreate Pro 1.18.0

## Drucksatzteile frei platzieren

Die zwei Teile des aktuellen Drucksatzes lassen sich auf dem Druckbett verschieben: Leuchtkasten/Rückwand oder Kugel/Mond mit Stecksockel. Ein Teil im Bettbild ziehen oder „Manuell · freie Positionen“ wählen und die vier XY-Werte eingeben. Beim Wechsel werden beide vorhandenen Positionen übernommen.

X/Y beziehen sich auf die linke untere Ecke des jeweiligen Hüllrechtecks, gemessen vom Bettursprung links unten. Ziehen rundet auf 0,1 mm. Mit ausgewähltem Teil verschieben Pfeiltasten um 1 mm, Umschalt+Pfeil um 10 mm. Escape bricht eine Ziehbewegung ab.

Randabstand, Teileabstand, Bettgrenzen und Bauhöhe bleiben verbindlich. Ungültige Ziehpositionen erscheinen rot und werden beim Loslassen verworfen; die letzte gültige Platzierung bleibt erhalten. Modellmaße, Drucklagen und Foto ändern sich nicht. Gültige Bewegungen verwenden die vorbereiteten Netze wieder. Vor dem Export werden die vollständige Geometrie und die tatsächlich geschriebene 3MF-Datei erneut geprüft.

„Automatisch“, „Zeile“ und „Spalte“ stehen weiterhin zur Verfügung. „Automatisch“ wählen und neu berechnen stellt die automatische Zentrierung wieder her. Die Bettübersicht zeigt Hüllrechtecke; es gibt kein Silhouettennesting und keine Kombination mehrerer Projekte.

## Speichern und Beispiele

Projektformat 15 erhält die vier Positionen. „Projekt speichern“ sichert die Planung separat vom 3MF-Export. Formate 1–14 bleiben lesbar; vorhandene Abstände und Zeilen-/Spaltenanordnung bleiben erhalten. Neu gespeicherte Dateien benötigen Version 1.18 oder neuer. Für einen Rückwechsel die ursprüngliche Datei behalten oder „Speichern unter“ verwenden.

Das zwölfte bearbeitbare Beispiel „Leuchtkasten mit freier Platzierung“ zeigt zwei versetzte Teile mit 10 mm Rand und 8 mm Teileabstand. Modellvorlagenformat 13 und die bisherige lokale Druckerprofil-JSON-Liste bleiben unverändert.
