# Neu in LithoCreate Pro 1.16.0

## Drucksätze mit eigenen Abständen planen

Im Bereich „Drucksatz · alle Teile“ lassen sich jetzt Randabstand und Abstand zwischen den Teilen einstellen. Die Werte beziehen sich auf die Hüllrechtecke der vollständigen Einzelteile in Exportauflösung.

- Randabstand: 0–50 mm, Vorgabe 5 mm.
- Teileabstand: 1–50 mm, Vorgabe 5 mm.
- Automatisch: passende Zeile oder Spalte mit der kleineren Hüllfläche.
- Zeile: beide Teile nebeneinander; Spalte: beide Teile hintereinander. Eine feste Wahl wird bei Platzmangel abgewiesen und nicht selbstständig ersetzt.

Nach einer Änderung „Drucksatz neu berechnen“ wählen. Veraltete Exportdaten werden sofort verworfen. Bettübersicht, Angaben und 3MF-Datei verwenden dieselbe Platzierung. Die gespeicherten Drucklagen und Maße der Einzelteile bleiben erhalten. Ungültige Zahlen werden markiert und sperren Berechnung/Export; das betroffene Feld bleibt auch auf kleinen Fenstern sichtbar.

## Projektformat 14

Randabstand, Teileabstand und Anordnungswahl werden im Projekt gespeichert. Ältere Projekte behalten 5 mm Rand und Teileabstand sowie automatische Anordnung. Neue Projektdateien benötigen Version 1.16 oder neuer. Eigene Modellvorlagen verwenden weiterhin Modellformat 13; sie verändern die Drucksatzplanung des aktuellen Projekts nicht.

Das elfte Beispiel „Leuchtkasten mit Drucksatzanordnung“ enthält einen Kasten mit Rückwand, 12 mm Rand, 9 mm Teileabstand und fest gewählter Spalte. „Drucksatz · alle Teile“ öffnet und berechnet die Bettübersicht.

## Grenzen

Zwei Einzelteile des aktuellen Projekts, zentriert in einer Zeile oder Spalte. Keine freie Verschiebung, beliebige Nesting-Optimierung, Slicerprofile, Werkzeugwege oder Stützen. Zusätzliche Freiräume für Brim, Skirt oder besondere Druckerbereiche selbst berücksichtigen. Die Anordnung allein garantiert keine Druckqualität.
