# Neu in LithoCreate Pro 1.14.0

## Eigene Modelleinstellungen als Vorlage

Unter „Vorlagen“ lassen sich jetzt bis zu 100 eigene Vorlagen lokal speichern. Jede Vorlage enthält die Form, alle Maße und Wandstärken, Zubehörparameter, Rückwandöffnungen einschließlich Langlöchern und Senkungen sowie die Drucklagen sämtlicher Einzelteile. Auch Maßkopplung, Negativmodus und Auflösung werden übernommen. Die 15 vorhandenen Grundformen bleiben verfügbar.

- Einen Namen eingeben und „Aktuelles Modell speichern“ wählen. Ein gleichnamiger Eintrag wird abgewiesen; bestehende Vorlagen werden nicht überschrieben.
- Vorlage auswählen und „Vorlage anwenden“ wählen. Die aktuellen Modelleinstellungen werden ersetzt und eine neue Vorschau berechnet. Foto, Bildkorrekturen, Druckerprofil, Projektname und aktuelles Speicherbudget bleiben erhalten.
- Zum Umbenennen den Namen ändern und „Umbenennen“ wählen. Löschen verlangt eine Bestätigung. Bereits gespeicherte Projekte behalten ihre Einstellungen.
- Ein Link im Formpanel führt direkt zur Vorlagenverwaltung. Die Auswahl allein verändert das Projekt nicht. Nach dem Anwenden muss das geänderte Projekt separat gespeichert werden.

Die Sammlung liegt in `model-presets.json` im lokalen Anwendungsdatenordner, getrennt von Bildern und Projekten. Tiefe Kopien erhalten gespeicherte Parameter auch nach späteren Modelländerungen. Erst nach erfolgreichem atomarem Schreiben wird die sichtbare Liste geändert. Beschädigte oder unbekannte Katalogversionen bleiben erhalten und werden nicht still durch eine leere Sammlung ersetzt.

## Kompatibilität und Grenzen

Projektformat 12 bleibt unverändert. Vorlagenparameter werden vollständig ins Projekt übernommen; es benötigt danach keine Verbindung zur Vorlagensammlung. Bild-Undo/Redo bleibt erhalten und betrifft weiterhin nur Bildkorrekturen. Eigene Vorlagen sind Parameterkombinationen der bestehenden Formen; externe Geometrie-Plugins, beliebige STL-Formen oder eine Bildbibliothek werden damit nicht eingeführt. Eine passend gewählte Vorlage garantiert keine Druckpassung oder Stützfreiheit. Modellmaße, Druckbett und Zubehör nach der Übernahme prüfen.
