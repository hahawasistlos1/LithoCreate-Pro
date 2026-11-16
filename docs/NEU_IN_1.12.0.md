# Neu in LithoCreate Pro 1.12.0

## Freie Seitenverhältnisse und gebundene Mausauswahl

- Im Fotoeditor „Ausschnitt & Seitenverhältnis“ aufklappen.
- Vorgaben 1:1, 3:2, 4:5, 16:9 und Modellformat; jeweils mittiger Zuschnitt des Originals.
- Eigene Verhältniswerte für Breite und Höhe, jeweils 0,1–1000 mit Dezimalstellen. „Mittig zuschneiden“ wendet sie an; die Werte sind keine Millimeter.
- „Ausschnitt mit der Maus wählen“ öffnet die Originalansicht. „Verhältnis beim Ziehen fixieren“ hält das Format in allen vier Ziehrichtungen. Ohne Bindung bleibt das freie Aufziehen erhalten.
- Die feste Auswahl passt in das aufgezogene Rechteck und bleibt im Bild. Escape oder Verlust der Mauserfassung bricht die laufende Auswahl ab. Sehr kleine Auswahlrechtecke werden nicht übernommen.
- Verhältniswahl, Bindung und Ausschnitt sind im Projekt gespeichert und Teil von Rückgängig/Wiederholen.
- Ungültige Verhältniswerte markieren die Eingabe, entfernen veraltete Vorschauen und sperren Zuschneiden und Modell-Export.
- Eine neue Auswahl setzt Bilddrehung, Feinrotation und Spiegeln wie bisher zurück. Die Originalbildbytes und Modellmaße bleiben unverändert.

Das achte Beispiel „Leuchtturm im Hochformat“ zeigt 4:5 mit passender 80 × 100 mm großer Fotofläche.

## Projektformat 11

Format 11 speichert `CropAspectWidth`, `CropAspectHeight` und `CropAspectLock` zusammen mit dem bisherigen Bildausschnitt. Formate 1–10 bleiben lesbar und erhalten Verhältniswerte 4:5 ohne aktive Bindung. Ihre Ausschnittkoordinaten und Bildbearbeitung bleiben unverändert. Apps bis 1.11.0 können Format 11 nicht lesen; ursprüngliche Dateien für einen Rückwechsel behalten oder unter neuem Namen speichern.

Die Auswahl erfolgt auf dem EXIF-ausgerichteten Original bei der bestehenden Arbeitsauflösung. Sehr extreme Verhältnisse können nur wenige Pixel enthalten. Modellbreite und -höhe ändern sich beim Zuschneiden nicht; bei abweichendem Modellformat kann das Motiv weiterhin gedehnt werden.
