# Neu in LithoCreate Pro 1.11.0

## Schraubensenkungen an Rückwandöffnungen

- „Senkung außen · Tiefe mm“ pro Rund- oder Langloch: 0 deaktiviert die Senkung, aktive Tiefe mindestens 0,1 mm.
- 90°-Senkung mit ebenen 45°-Fasen. Bei Rundbohrungen entsteht eine konische Vertiefung, bei Langlöchern eine umlaufende Fase.
- Außenbreite = Durchmesser + 2 × Senktiefe. Die äußere Gesamtlänge eines Langlochs ist Außenbreite + Zusatzlänge.
- Mindestens 0,4 mm gerader Durchgang bleiben stehen. Bei 2 mm Rückwandstärke sind höchstens 1,6 mm Senktiefe zulässig.
- Senkung auf der äußeren Rückseite, auf der Seite ohne Einsteckrand. In der flachen Standarddrucklage liegt sie unten; bei Bedarf Drucklage im Editor bzw. Slicer anpassen.
- Freizonen berücksichtigen die gesamte größere Außenöffnung, Einsteckrand, Nachbaröffnungen und Kabeldurchführung. Die zentrale Kabelöffnung bleibt ungesenkt.
- Vorschau, geometrische Schichten, Montage, STL, Einzelmodell-3MF und Zwei-Objekt-Drucksatz verwenden dieselbe Geometrie.
- Ungültige oder zu tiefe Werte verwerfen veraltete Vorschauen und sperren den Export. Gültige Korrektur oder Entfernen der Öffnung stellt den Ablauf wieder her.

Das siebte Beispiel „Leuchtkasten mit Senkungen“ enthält eine Rundbohrung und drei Langlöcher mit unterschiedlichen Senktiefen. Es öffnet direkt die Rückwand.

## Projektformat 10

Format 10 speichert die Senktiefe je Öffnung. Formate 1–9 bleiben lesbar und erhalten Senktiefe null. Rund-/Langlochgeometrie und freie Druckwinkel bleiben erhalten. Ohne Senkung verwendet die App die bisherige Geometriepipeline. Apps bis 1.10.0 können Format 10 nicht lesen; für einen Rückwechsel ursprüngliche Dateien behalten oder unter neuem Namen speichern.

Die Senkung ist kein Gewinde, Schraubenmodell oder Verriegelung. Schraubenkopfmaße und tatsächliche Passung am gedruckten Teil prüfen. Die Rückwand bleibt eine lose Steckabdeckung; die Öffnungen verriegeln den Kasten nicht und enthalten keine Belastbarkeitsfreigabe.
