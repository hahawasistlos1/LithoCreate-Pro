# Neu in LithoCreate Pro 1.17.0

## Druckerprofile sicher verwalten

Die Einstellungen bieten jetzt eine vollständige lokale Profilverwaltung:

- „Profil speichern“ erstellt einen Eintrag oder aktualisiert einen gleichnamigen Eintrag. Bis zu 100 Profile; keine Namensduplikate durch Groß-/Kleinschreibung oder äußere Leerzeichen.
- „Gespeicherte Werte übernehmen“ holt die gespeicherten Einstellungen erneut ins Projekt, auch nach einer ungültigen Druckereingabe.
- „Umbenennen“ ändert den gespeicherten Namen; andere bearbeitete Werte werden erst über „Profil speichern“ aktualisiert.
- „Löschen …“ entfernt nach Bestätigung nur den Bibliothekseintrag. Projekte behalten ihre eigenen Druckereinstellungen.
- „Profildatei neu laden“ liest nach einer Dateikorrektur erneut ein. Die Projektwerte bleiben dabei erhalten.

Gespeicherte und aktuelle Werte werden getrennt angezeigt. Beim Öffnen eines Projekts überschreibt ein gleichnamiges Bibliotheksprofil dessen eingebettete Maße nicht. Änderungen im Projekt separat speichern.

## Schutz vor Datenverlust

`printers.json` wird atomar gespeichert. Ein Schreibfehler übernimmt keine Teiländerung der Liste oder Auswahl. Beschädigte, zu große, doppelt benannte oder unbekannte Profildaten werden nicht still durch Standardprofile ersetzt. Schreibaktionen bleiben bis zur Behebung und zum erneuten Laden gesperrt. Vorhandene leere Listen bleiben nach einem Neustart leer.

Ein zusätzlicher Oberflächenfehler ist behoben: Die programmatische Aktualisierung eines fokussierten Felds beim Projektladen zählt nicht mehr als neue Benutzeränderung. Eine gültige Profilübernahme entfernt außerdem eine veraltete Fehlersperre des ersetzten Druckerfelds.

## Kompatibilität

Projektformat 14 und elf Beispielprojekte bleiben erhalten. Projekte bleiben mit Version 1.16 kompatibel. Die bisherige JSON-Profilliste und die Modellvorlagen benötigen keinen Formatwechsel. Materialtexte bleiben Beschriftungen; es werden keine Slicerprofile, Werkzeugwege oder optischen Materialkennlinien erzeugt.
