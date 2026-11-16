# Neu in LithoCreate Pro 1.1.0

## Lampen und Standfüße

- **Zylinder und mehrseitige Lampen:** integrierter Boden mit einstellbarer Stärke. Eine zentrale Bodenöffnung kann beispielsweise für eine von unten eingeführte Leitung oder Aufnahme verwendet werden. 0 mm Bodenstärke lässt den Mantel unten offen; 0 mm Öffnung erzeugt einen geschlossenen Boden. Die äußere Lampenhöhe bleibt erhalten.
- **Flaches Bild, Bilderrahmen, gebogenes Bild und Nachtlicht:** optionaler integrierter Standfuß mit eigener Höhe und Tiefe. Er wird unter dem Bild ergänzt; die Bildhöhe bleibt erhalten. Bei gebogenen Bildern folgt der Fuß dem Bogen. Unterseite, Schultern und Seitenwände bilden mit dem Panel ein geschlossenes Mesh.
- **Kugel und Mond:** untere Lampenöffnung mit einstellbarem freien Durchmesser, integriertem Kragen und ebener ringförmiger Auflage. Zusätzlich ist eine obere Entlüftungsöffnung möglich. Die untere Öffnung entfernt einen Teil der Kugelkappe; Durchmesser und tatsächliche Gesamtmaße stehen in der Vorschau. Der Übergang wird auf die maximale Wandstärke verstärkt und weich in das Fotorelief überführt. Das verhindert einen dünnen Hals am Übergang. Bei 0 mm Kragenhöhe schließt eine ebene Ringfläche die Unterkante. Der Kragen ist fest verbunden und stellt keine abnehmbare oder standardisierte Fassung dar.
- Die Kreisöffnungen berücksichtigen die Sehnen des Dreiecksrasters, damit der gewählte freie Mindestdurchmesser nicht durch eine nur an den Eckpunkten gemessene Kreisannäherung unterschritten wird. Fertigungsmaß und nötiges Passspiel müssen weiterhin zum Druckverfahren passen.

Die neuen Optionen erscheinen passend zur gewählten Form direkt im Einstellungsbereich. „Unten“ in der Werkzeugleiste zeigt die Bodenöffnung. Das Bodengitter kann für diese Ansicht separat abgeschaltet werden.

## Vorschau und Stabilität

- Standfüße, Böden und Kragen erhalten eine eigene Materialdarstellung, ohne das Foto auf diese Teile zu projizieren.
- Die Textur über periodischen Nähten wird für das Rendering korrekt wiederholt; das Exportmesh behält verschweißte Nähte.
- Kamera-Reset und feste Ansichten passen das vollständige Modell an das tatsächliche Seitenverhältnis des Vorschaufensters an. Solange noch nicht manuell gedreht oder gezoomt wurde, bleibt diese Einpassung bei Fenstergrößen- und Modelländerungen aktiv.
- Fehlerhafte Parameter löschen die veraltete Vorschau. PNG-Export und Kamerabefehle stehen erst bei einer gültigen Vorschau zur Verfügung.
- Projektdateien prüfen nun auch Bildkorrekturen, Zuschnitt, Druckerprofile und bekannte Formkennungen vor der Übernahme.
- Ein exklusiver Dateihandle verhindert ab Version 1.1.0 zwei gleichzeitige Instanzen im selben Datenordner. Windows gibt die Sperre bei einem Absturz frei; eine übrig gebliebene `studio.lock` muss nicht gelöscht werden.
- Die 2048-MB-Geometrieeinstellung verwendet eine überlaufsichere Budgetberechnung.

## Projekte und Veröffentlichung

Neue Projekte verwenden das Format **Version 2**, damit die zusätzlichen Geometrieparameter vollständig erhalten bleiben. Projekte aus Version 1 werden weiterhin gelesen und bei erneutem Speichern in Version 2 überführt. LithoCreate Pro 1.0.0 kann Version-2-Projekte nicht öffnen; für einen Rückwechsel die ursprüngliche Datei behalten oder vor dem Speichern „Speichern unter“ verwenden.

Vor dem Wechsel von der älteren Anwendung bitte das Fenster von Version 1.0.0 schließen: Diese Version kennt die neue Instanzsperre noch nicht. Die Installationsdatei 1.1.0 hat eine eigene Produktkennung und erkennt ältere Installationen über die gemeinsame Upgrade-Kennung. Versionsnummern werden beim Paketbau aus der veröffentlichten App gelesen. Frühere veröffentlichte Dateien werden nicht überschrieben.

Der Ordner **Beispiele** enthält drei sofort bearbeitbare Projekte für Standfuß, Zylinderboden und Kugelöffnung. Ihre Bilddaten stammen ausschließlich vom eingebauten Leuchtturm-Beispiel. Die Dateien lassen sich mit „Projekt öffnen“ laden oder in das Programmfenster ziehen.

Noch offen bleiben unter anderem abnehmbare Sockel und Rückwände, weitere Halterungen, frei positionierte Bohrungen, externe Form-Plugins, eine Schichtansicht sowie physische Druck- und Materialtests. Der vollständige Stand steht in [FUNKTIONSUMFANG.md](FUNKTIONSUMFANG.md).
