# Neu in LithoCreate Pro 1.10.0

## Langlöcher in der Rückwand

- Jede der bis zu acht Rückwandbohrungen kann als Rundbohrung oder Langloch ausgeführt werden.
- „Langloch · Zusatzlänge“ von 0 bis 40 mm: 0 bleibt rund, größere Werte verlängern die Öffnung. Gesamtlänge = Durchmesser + Zusatzlänge. Der Durchmesser von 2–20 mm bestimmt die freie Breite.
- Senkrecht oder über „Langloch waagerecht“ horizontal. Die Richtung bezieht sich auf die aufrechte Rückwand; eigene Drucklagen und freie Druckwinkel drehen das gesamte Teil.
- X/Y bezeichnet den Mittelpunkt der gesamten Öffnung. X ab Rückwandmitte, Y ab Unterkante.
- Echte parallele Seiten und halbrunde Enden, durch die gesamte Platte. Die polygonale Näherung hält die eingestellte Mindestbreite frei.
- Die Freizonenprüfung umfasst die volle Länge, Einsteckrand, Nachbaröffnungen und zentrale Kabeldurchführung.
- Vorschau, geometrische Schichten, Montage, STL, Einzelmodell-3MF und Zwei-Objekt-Drucksatz verwenden dieselbe Geometrie.
- Eingabefehler oder zu große Öffnungen verwerfen veraltete Vorschauen und sperren den Export.

Das sechste Beispiel „Leuchtkasten mit Langlöchern“ öffnet die Rückwand mit vier gemischten Langlöchern und Kabelöffnung.

## Projektformat 9

Format 9 speichert Zusatzlänge und Richtung je Öffnung. Formate 1–8 bleiben lesbar. Bestehende Rundbohrungen und freie Druckwinkel bleiben erhalten; es werden keine Langlöcher automatisch aktiviert. Der bisherige Rundbohrungspfad erzeugt unveränderte Geometrie. Apps bis 1.9.0 können Format 9 nicht lesen; für einen Rückwechsel ursprüngliche Dateien behalten oder unter neuem Namen speichern.

Die Öffnungen dienen zur Ausrichtung einer Befestigung. Es gibt keine Senkungen, Schraubenmodelle, Verriegelung oder Belastbarkeitsfreigabe. Die Rückwand bleibt eine lose Steckabdeckung; sie verriegelt den Kasten nicht. Passung und Befestigung am gedruckten Teil prüfen.
