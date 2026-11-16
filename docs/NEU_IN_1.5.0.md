# Neu in LithoCreate Pro 1.5.0

## Separate Stecksockel

- Kugel- und Mondlampen mit unterer Öffnung können einen passenden, separat druckbaren Sockel erhalten.
- Einstellbar: Fußdurchmesser, Plattenhöhe, Einsteckhöhe, radiales Passspiel und zentrale Durchführung.
- Geschlossene, rotationssymmetrische Geometrie mit Auflageschulter und durchgehendem axialem Kanal. Prüfung auf Mindestmaterialstärke, tragenden Fußdurchmesser und mögliche Einstecktiefe.
- Montageansicht mit aufliegendem Kragen und eingestecktem Ring. Beide Teile bleiben getrennte Körper; die Montage ist eine Ansicht und kein gemeinsames Druckmodell.
- Teilwahl direkt im Editor: Lithophanie, Stecksockel oder Montage. Schichtprüfung und STL/3MF erfolgen pro Einzelteil; PNG funktioniert auch für die Montageansicht.
- Unabhängige gespeicherte Drucklagen und Bettrotation für Kugel und Sockel. Die Druckraumsuche arbeitet auf dem aktiven Einzelteil.
- Sockel ohne Fototextur; Strukturkanten werden mit expliziten Flächennormalen dargestellt. Reine Sockelansichten erzeugen keine unnötige Emissionstextur.

Der Sockel besitzt keine genormte Fassung, Gewinde, Rastverbindung oder elektrische Komponenten. Die geometrische Passungsprüfung ersetzt kein gedrucktes Probestück. Auch die Druckausrichtung und Stützstrategie bleiben im Slicer zu kontrollieren.

## Projekte und Beispiele

Format 5 speichert Sockelparameter, aktuelle Teilwahl und getrennte Drucklagen. Format 1 bis 4 bleibt lesbar; vorhandene Bild- und Drucklagen werden beibehalten und kein Zubehör automatisch aktiviert. Ältere Apps bis 1.4.0 lehnen Format 5 ab, statt ein Zubehörteil zu ignorieren. Für einen Rückwechsel die ursprüngliche Projektdatei behalten oder „Speichern unter“ verwenden.

Das Kugellampen-Beispiel enthält den Sockel und öffnet direkt die Montageansicht. MSI und portables ZIP enthalten weiterhin drei bearbeitbare Beispielprojekte und die eigenständige Windows-x64-Laufzeit.
