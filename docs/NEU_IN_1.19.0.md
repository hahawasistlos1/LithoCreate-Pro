# Neu in LithoCreate Pro 1.19.0

## Drucksatzschritte zurücknehmen und wiederholen

Die Drucksatzansicht bietet jetzt „↶ Zurück“ und „↷ Wiederholen“. Gültige Maus-/Pfeilbewegungen, numerische Positionen, Rand-/Teileabstände und die Anordnungswahl lassen sich in einer eigenen Historie zurücknehmen. Bis zu 100 Schritte bleiben während der Sitzung verfügbar. Der Zähler zeigt beide Richtungen.

Strg+Alt+Z nimmt einen Drucksatzschritt zurück, Strg+Alt+Y wiederholt ihn. Die Tastenkürzel gelten im Drucksatzeditor. Strg+Z/Y bleibt für Bildkorrekturen erhalten.

Eine abgelehnte Ziehbewegung erzeugt keinen Schritt. Eine neue Änderung nach einer Rücknahme verwirft den Wiederholen-Zweig. Unveränderte manuelle Positionen erzeugen keinen Eintrag und markieren das Projekt nicht als geändert. Zahlenfelder können mehrere gültige Zwischenwerte erzeugen.

## Speicher und Export

Die Historie speichert nur Parameter und Positionen, keine zusätzlichen Netzkopien. Vorhandene vorbereitete Netze werden bei bekannten Zielpositionen erneut auf Bettgrenzen, Rand und Teileabstand geprüft und weiterverwendet. Ohne gültige Prüfdaten ist eine Neuberechnung nötig; die Statuszeile erklärt dies. Export und erneutes Einlesen behalten ihre vollständige Geometrieprüfung.

Modell-, Foto-, Bildkorrektur- und Druckeränderungen sowie Projektwechsel leeren die Historie. Speichern und Neuberechnen erhalten sie. Die Historie selbst wird nicht gespeichert; die aktuell gewählte Drucksatzplanung bleibt im Projekt erhalten. Während einer Berechnung und bei fehlerhaften Eingaben sind Rücknahme/Wiederholen gesperrt. Korrigierte Drucksatzfelder erhalten ihre Historie.

Projektformat 15, Modellvorlagenformat 13, Druckerprofile und zwölf Beispiele bleiben unverändert. Projekte bleiben mit Version 1.18 kompatibel.
