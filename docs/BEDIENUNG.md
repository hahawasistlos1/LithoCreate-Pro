# Bedienungsanleitung

## Installation und Start

Die MSI-Datei mit einem Doppelklick öffnen. „Desktopverknüpfung erstellen“ ist optional. Installiert wird für den aktuellen Benutzer unter `%LOCALAPPDATA%\Programs\LithoCreate Pro`. Der Startmenüeintrag heißt „LithoCreate Pro“. Die Deinstallation erfolgt über Windows → Installierte Apps. Persönliche Projektdateien und lokale Wiederherstellungs-/Profildaten werden vom Installer nicht entfernt.

Alternativ den vollständigen portablen Ordner entpacken und `LithoCreate Pro.exe` starten. Die anderen Dateien gehören zur mitgelieferten Laufzeit und müssen neben der EXE bleiben.

## 1. Foto bearbeiten

„Neues Projekt“ öffnet den Editor mit einem eingebauten, prozedural erzeugten Leuchtturm-Beispiel. Dieses Beispiel lässt sich ebenfalls exportieren. Über „Foto laden“ ein eigenes JPG, PNG, BMP oder TIFF öffnen; alternativ ins Fenster ziehen. JPEG und mehrseitige TIFF-Dateien werden über Windows WIC eingelesen; beim TIFF wird das erste Bild verwendet. WebP hängt vom lokal verfügbaren Windows-Bilddecoder ab.

Der Originalinhalt wird unverändert im Projekt gespeichert. Die Arbeitsvorschau ist auf maximal 1800 Pixel an der längsten Seite begrenzt. Der Export liest das Original erneut mit einer vom Speicherbudget abhängigen höheren Bildgrenze bis 4096 Pixel ein.

Neue JPEG-/TIFF-Fotos werden anhand ihrer EXIF-Ausrichtung gedreht bzw. gespiegelt. Der Ausschnitt bezieht sich auf das so ausgerichtete Bild. Dateien aus Projektformat 1 oder 2 behalten ihre bisherige Ausrichtung, auch beim späteren Export und beim Zurücksetzen der Bildkorrekturen.

Helligkeit, Kontrast, Gamma, Schärfe, Rauschminderung, Schwarzweiß und Farbumkehr anpassen. Schwarz- und Weißpunkt steuern die Tonwerte. Die Balken zeigen das Histogramm des bearbeiteten Bildes. Die automatische Optimierung setzt Schwarz- und Weißpunkt anhand der 1-%- und 99-%-Quantile.

„Ausschnitt & Seitenverhältnis“ aufklappen. 1:1, 3:2, 4:5, 16:9 oder „Modellformat“ schneiden das Original mittig zu und übernehmen das Verhältnis. Eigene Werte für Breite und Höhe von 0,1 bis 1000 eingeben (Verhältniswerte, keine Millimeter), dann „Mittig zuschneiden“ wählen. Zum Beispiel ergibt 7,5 : 10 ein Verhältnis von 3:4. Dezimaltrennzeichen ist das Komma.

Für eine freie Position „Ausschnitt mit der Maus wählen“ aktivieren. Das ausgerichtete Original wird angezeigt. Bei „Verhältnis beim Ziehen fixieren“ hält die Auswahl das eingestellte Verhältnis; ohne Häkchen wird ein freies Rechteck aufgezogen. Die Auswahl bleibt innerhalb des aufgezogenen Rechtecks und des Bildes, auch beim Ziehen nach links oder oben. Nach Loslassen wird der Ausschnitt angewandt. Escape oder Verlust der Mauserfassung bricht eine laufende Auswahl ab. Sehr kleine Auswahlrechtecke werden nicht übernommen.

Verhältniswerte, Bindung und fertiger Ausschnitt werden im Projekt gespeichert und lassen sich rückgängig machen bzw. wiederholen. Die Wahl eines neuen Ausschnitts setzt 90°-Drehen, Feinrotation und Spiegeln zurück, damit die Auswahl im Original-Koordinatensystem eindeutig bleibt. „Ausschnitt zurücksetzen“ stellt das vollständige Foto bei den aktuellen Dreh-/Spiegeleinstellungen wieder her und behält die Verhältniswahl. „Original wiederherstellen“ setzt auch Verhältniswerte auf 4:5 ohne aktive Bindung zurück. Zuschneiden verändert weder Originalbildbytes noch Modellbreite oder -höhe. Für unverzerrte rechteckige Motive das Foto passend zum Modellformat zuschneiden oder die Modellmaße anpassen.

„Drehen“ rotiert um 90°. Mit „Feinrotation“ zusätzlich −45° bis +45° einstellen; positive Winkel drehen im Uhrzeigersinn. Der Regler und das Zahlenfeld akzeptieren auch Dezimalwerte. Die Bildgröße und das Seitenverhältnis bleiben erhalten; die Ränder werden automatisch vergrößert und beschnitten, damit keine leeren Ecken entstehen. „Auf 0°“ setzt nur die Feinrotation zurück. Die Einstellungen gelten auch für das 3D-Modell und den Export und lassen sich rückgängig machen.

„Vorher / Nachher teilen“ zeigt links den ausgerichteten Bildausschnitt ohne Tonwert-/Farb-/Filterkorrekturen und rechts die bearbeitete Fassung. Die Trennlinie mit der Maus verschieben. Nach einem Klick ins Vergleichsbild verschieben Links/Rechts die Linie; Pos1 stellt sie in die Mitte. Beide Seiten verwenden denselben Ausschnitt, Drehung und Spiegelung. Beim Aktivieren der Originalansicht wird der geteilte Vergleich vorübergehend ausgeblendet, damit die Ausschnittwahl funktioniert. Die Vergleichsansicht verändert das Projekt nicht und wird nicht gespeichert.

„Zurück“ und „Wiederholen“ im Bildpanel betreffen Bildkorrekturen. „Original wiederherstellen“ setzt sämtliche Bildkorrekturen zurück.

## 2. Form wählen

### Eigene Vorlagen wiederverwenden

Unter „Vorlagen“ oder im Editor über „Eigene Vorlagen verwalten“ einen Namen eingeben und „Aktuelles Modell speichern“ wählen. Gespeichert werden die aktuelle Form und sämtliche Modelleinstellungen einschließlich Maße, Wandstärken, Zubehör, Rückwandöffnungen, Maßkopplung und Drucklagen der Einzelteile. Bis zu 100 Vorlagen können auf diesem Computer gespeichert werden. Ein gleicher Name wird unabhängig von Groß-/Kleinschreibung abgewiesen. Für geänderte Parameter eine zusätzliche Vorlage unter neuem Namen speichern; bestehende Vorlagen werden nicht automatisch aktualisiert.

Eine gespeicherte Vorlage auswählen und „Vorlage anwenden“ anklicken. Der Editor übernimmt deren Modelleinstellungen und berechnet eine neue Vorschau. Das aktuelle Foto, alle Bildkorrekturen einschließlich Ausschnitt, Projektname, Druckerprofil und Speicherbudget bleiben erhalten. Das Projekt gilt anschließend als geändert und muss separat gespeichert werden. Vorlagenänderungen gehören nicht zum Bild-Undo. Die Auswahl einer Vorlage allein verändert das Projekt nicht.

Zum Umbenennen die Vorlage auswählen, den Namen ändern und „Umbenennen“ wählen. „Löschen …“ fragt vor dem Entfernen nach; gespeicherte Projekte behalten ihre Parameter. Vorlagenspeichern, Umbenennen und Löschen verändern den Projektstatus nicht.

Vorlagen aus Version 1.14 werden mit deaktivierten Aufhängelaschen übernommen; Namen, Kennungen und bisherige Parameter bleiben erhalten. Neue Sammlungen verwenden Modellformat 13 und benötigen Version 1.15 oder neuer.

Die Sammlung liegt unter `%LOCALAPPDATA%\LithoCreate Pro\model-presets.json`, getrennt von Fotos und Projekten. Sie bleibt bei der Deinstallation erhalten. Eine beschädigte oder von einer neueren Version stammende Sammlung wird angezeigt und nicht überschrieben; Grundformen und Projekte sind weiterhin nutzbar. Zur Sicherung diese Datei bei geschlossener App kopieren. Eigene Vorlagen speichern Parameter der 15 Grundformen, keine neuen Geometrie-Plugins.

### Modellmaße koppeln und an das Foto anpassen

Bei flachem Bild, gebogenem Bild, Bilderrahmen, Nachtlicht und Leuchtkasten steht unter den Abmessungen „Breite und Höhe koppeln“. Eine Breitenänderung passt dann die Höhe im aktuellen Modellverhältnis an, eine Höhenänderung die Breite. Beispiel: 120 × 88 mm werden bei Eingabe von 150 mm Breite zu 150 × 110 mm. Ohne Bindung lassen sich die Maße unabhängig ändern. Beide Maße müssen zwischen 5 und 1000 mm liegen; eine unzulässige gekoppelte Änderung wird vollständig abgewiesen.

„Höhe an Motivformat anpassen“ behält die Breite bei und berechnet die Höhe aus dem tatsächlichen aktuellen Bildausschnitt einschließlich Drehung. Danach ist die Kopplung aktiviert. Nach einer späteren Bildbearbeitung die Anpassung erneut ausführen; die Bindung selbst erhält das aktuelle Modellverhältnis. Die Berechnung lässt sich abbrechen. Ein veraltetes oder geometrisch unzulässiges Ergebnis wird nicht übernommen.

Wandstärken, Rahmenbreite, Krümmungsradius, Standfuß- und Zubehörparameter bleiben in Millimetern erhalten. Bohrungspositionen werden nicht mitverschoben; bei einer Verkleinerung Rahmen und Öffnungen prüfen. Modellmaße gehören nicht zum Bild-Undo. Bei anderen Formen ist die gespeicherte Bindung inaktiv. Das neunte Beispiel „Leuchtturm mit Formatbindung“ startet mit passenden 120 × 88 mm und aktivierter Kopplung.

15 Vorlagen stehen zur Verfügung. Die Kacheln enthalten gerenderte Beispiele der tatsächlichen Geometrie. Ein Klick übernimmt die Form ins aktuelle Projekt. In der Auswahlliste im Editor kann die Form jederzeit gewechselt werden.

Breite und Höhe gelten für Platten und gebogene Segmente. Der Durchmesser gilt für Kugeln, Zylinder, mehrseitige Lampen, runde Plaketten und Anhänger. Bei runden Reliefkörpern bezeichnet er die maximale äußere Konstruktionshülle bei maximaler Wandstärke; die tatsächlichen Außenmaße hängen vom Bild ab und stehen in der Vorschau. Bei der Kugel beschreibt die Höhe keinen zusätzlichen unabhängigen Maßstab. Der Anhänger hat eine zentrale Bohrung.

## 3. Maße und Druckeinstellungen

Alle geometrischen Werte sind in Millimetern. Deutsche Dezimalzahlen mit Komma eingeben. Rot markierte Felder müssen vor dem Speichern oder Export korrigiert werden.

- **Minimum:** Materialstärke bei weißen Bildpunkten, beispielsweise 0,8 mm.
- **Maximum:** Materialstärke bei schwarzen Bildpunkten, beispielsweise 3,0 mm.
- **Auflösung:** gewünschter Abtastabstand auf der Oberfläche, beispielsweise 0,3 mm. Ein kleinerer Wert erzeugt mehr Dreiecke.
- **Negativmodus:** kehrt die Zuordnung von Bildhelligkeit zu Wandstärke um.
- **Radius:** Krümmung für gebogenes Bild und Nachtlicht.
- **Rahmenbreite:** umlaufender Rand rechteckiger Platten; 0 deaktiviert den Rand. Der Rand ist 1 mm dicker als die maximale Fotowandstärke.
- **Leuchtkasten-Tiefe:** Tiefe des rückwärtigen Randes; der Kasten ist hinten offen.
- **Seitenzahl:** 3–16 Seiten bei mehrseitigen Lampen.
- **Bohrung:** Durchmesser der zentralen Öffnung beim Anhänger.
- **Standfuß:** Für flache und gebogene Fotoplatten, Rahmen und Nachtlicht. Seine Höhe wird unter der unveränderten Bildhöhe ergänzt; die Tiefe bestimmt die Auflage vor und hinter dem Foto. Der Fuß ist fest mit dem Bild verbunden.
- **Lampenboden:** Für Zylinder und mehrseitige Lampen. 0 mm lässt die Unterseite offen; sonst mindestens 0,4 mm Bodenstärke. Die äußere Lampenhöhe bleibt erhalten.
- **Bodenöffnung:** Zentrale Durchgangsöffnung durch den aktiven Lampenboden. 0 mm schließt den Boden.
- **Untere Kugelöffnung:** 0 mm erzeugt eine geschlossene Unterseite. Ein positiver Wert erzeugt einen Durchgang mit dem angegebenen freien Mindestdurchmesser und einen fest verbundenen Kragen mit ebener ringförmiger Auflage. Die Kugelkappe wird dabei entfernt.
- **Kragenhöhe:** Zusätzliche Höhe unter der Kugelöffnung; kein genormter Leuchtmittelsockel.
- **Obere Entlüftung:** Zweite Öffnung für Kugel und Mond; 0 mm schließt die Oberseite. Für einen durchgehenden axialen Luftweg beide Kugelöffnungen aktivieren.

### Druckerprofile verwalten

Unter „Einstellungen → Eigene Druckerprofile“ ein gespeichertes Profil auswählen. Seine Werte werden als eigene Kopie in das aktuelle Projekt übernommen. Profilname, Druckbett, Bauhöhe, Düsendurchmesser, Schichthöhe, Verfahren und Material lassen sich bearbeiten. Die Auswahl in der Liste bleibt dabei stabil; die Zeilen „Im Projekt“ und „Gespeichert ausgewählt“ unterscheiden die beiden Datenstände.

„Profil speichern“ legt unter einem neuen Namen ein weiteres Profil an. Ein gleicher Name aktualisiert die gespeicherten Werte; Groß-/Kleinschreibung und äußere Leerzeichen werden für den Namensvergleich ignoriert. Neue Namen werden ohne äußere Leerzeichen gespeichert. Bis zu 100 Profile, Namen bis 256 Zeichen ohne Steuerzeichen. Für eine neue Variante einen anderen Namen verwenden.

„Umbenennen“ ändert nur den gespeicherten Namen des ausgewählten Profils, mit dem Inhalt des Profilnamensfelds. Andere bearbeitete Werte werden dabei nicht automatisch in der Bibliothek aktualisiert; dafür „Profil speichern“ wählen. Namenskonflikte werden ohne Teilübernahme abgewiesen.

„Gespeicherte Werte übernehmen“ holt die gespeicherten Einstellungen erneut ins Projekt, auch zum Ersetzen einer ungültigen Druckereingabe. Andere ungültige Modell-/Bildfelder müssen weiterhin korrigiert werden. „Löschen …“ fragt nach und entfernt nur den ausgewählten Bibliothekseintrag. Die Druckerdaten im aktuellen und in bereits gespeicherten Projekten bleiben erhalten. Das reine Speichern oder Löschen eines Bibliothekseintrags macht ein unverändertes Projekt nicht geändert; Werteübernahme oder eine neue Profilwahl sind dagegen Projektänderungen. Das Projekt separat speichern.

Die Datei `printers.json` liegt unter `%LOCALAPPDATA%\LithoCreate Pro`. Bisherige gültige JSON-Listen bleiben lesbar; das Dateiformat wird nicht geändert. Speichern erfolgt über eine temporäre Datei und anschließendes Ersetzen. Bei einem Schreibfehler bleiben bisherige Datei, sichtbare Liste und Auswahl erhalten. Gespeicherte leere Listen bleiben nach einem Neustart leer. Nur bei fehlender Datei werden zwei Startprofile angeboten.

Beschädigte, zu große, doppelt benannte oder unbekannte Profildaten werden nicht überschrieben. Speichern, Umbenennen und Löschen bleiben dann gesperrt; eine Meldung erklärt den Fehler. Das aktuelle Projekt behält seine Druckereinstellungen. Nach Behebung oder Rücksicherung der Datei „Profildatei neu laden“ wählen. Unbekannte Datenfelder nicht ungeprüft entfernen; für Daten einer späteren Version die passende App verwenden. Maximal 512 KB Dateigröße. Sicherungen bei geschlossener App anlegen. Der Installer entfernt diese Benutzerdatei nicht.

Die Profilwahl beeinflusst Druckraumwarnung und Drucksatzprüfung. Sie erzeugt keinen G-Code und ersetzt keine Slicer-Einstellungen. Materialangaben sind Beschriftungen, keine optische Materialkalibrierung.

### Aufhängelaschen für Bilder und Rahmen

Bei „Flaches Bild“ oder „Bilderrahmen“ den Bereich „Aufhängelaschen“ aufklappen und „Zwei Aufhängelaschen ergänzen“ aktivieren. Es entstehen zwei symmetrische rechteckige Laschen mit runder Durchgangsbohrung oberhalb des Bildes, als ein zusammenhängender Körper mit dem Panel. Mindestens 1,2 mm Rahmenbreite einstellen. Die Laschen sind so dick wie der verstärkte Rahmen (maximale Fotowandstärke + 1 mm).

Breite und zusätzliche Höhe je 6–40 mm; Lochdurchmesser 2–20 mm und mindestens 2,5 mm kleiner als beide Laschenmaße. Damit bleiben auch bei der polygonalen Lochwand mindestens 1,2 mm Material. „Randabstand“ misst vom äußeren Bildrand bis zur äußeren Laschenkante, auf beiden Seiten gleich (0–100 mm). Zwischen den Laschen müssen mindestens 2 mm bleiben. Unpassende Kombinationen sperren Vorschau und Export und werden in der Statuszeile erklärt.

Die Bildbreite und Fotohöhe ändern sich nicht. Zur Druckhöhe kommen Laschenhöhe und ein eventuell aktiver Standfuß hinzu. Beispiel: 100 × 80 mm mit 14 mm hohen Laschen werden ohne Standfuß zu 100 × 94 mm; die Dicke kommt als dritte Achse hinzu. Alle Drucklagen, Schichten, Druckraumprüfung und STL/3MF verwenden die vollständige Geometrie. Maßkopplung ändert weiterhin nur die Fotofläche, keine Laschenparameter. Bei anderen Formen bleiben gespeicherte Laschen inaktiv.

Das zehnte Beispiel „Leuchtturm mit Aufhängelaschen“ zeigt diese Konfiguration mit 12 mm breiten Laschen, 4 mm Bohrung und 8 mm Randabstand. Belastbarkeit, Druckausrichtung, Schichthaftung und Befestigungsmittel praktisch prüfen. Die Laschen sind keine genormte oder lastgeprüfte Halterung.

### Separaten Kugelsockel erstellen

Bei Kugel oder Mond zuerst eine untere Öffnung einstellen und dann unter „Separater Stecksockel“ die Option „Stecksockel ergänzen“ aktivieren. Fußdurchmesser, Plattenhöhe, Einsteckhöhe, radiales Passspiel und zentrale Durchführung anpassen. Beispielsweise erzeugt 0,25 mm radiales Spiel einen um 0,5 mm kleineren Einsteckdurchmesser als die Kugelöffnung. Die Durchführung muss mindestens 2 mm frei bleiben; am Einsteckring werden mindestens 1,2 mm Material geprüft.

Die Sockelplatte muss den Kragen vollständig tragen. Die App prüft dafür einen konservativen Mindestdurchmesser und begrenzt die Einsteckhöhe auf den Kragen und den sich nach innen weitenden unteren Kugelbereich. Unpassende Werte werden bei der Geometrieberechnung abgewiesen. Die Maße bezeichnen die Geometrie; tatsächliches Druckspiel und mechanische Belastbarkeit mit einem Probestück prüfen.

Oben im rechten Panel über „Teil / Montage“ zwischen Lithophanie, Stecksockel und Montageansicht wechseln. Jedes Druckteil besitzt eine eigene Drucklage und Bettrotation. Schichtansicht, Prüfung in Exportauflösung, Druckraumsuche und STL-/3MF-Export verwenden das aktuell gewählte Einzelteil. Das Foto beeinflusst den Sockel nicht.

Die Montageansicht zeigt die Kugel auf der Sockelplatte, den Einsteckring in der Öffnung und beide Körper stets aufrecht. Ihre gemeinsame Ausgabe als Druckmodell ist gesperrt, weil die Kontaktflächen keine gemeinsame druckbare Geometrie bilden. Für den Export zuerst Lithophanie oder Stecksockel wählen und beide Dateien getrennt speichern. Der vorgeschlagene Dateiname enthält jeweils „Lithophanie“ oder „Stecksockel“, damit sich die beiden Exporte unterscheiden. Die Montageansicht lässt sich als PNG ausgeben. Der Sockel enthält keine elektrische Fassung oder genormte Leuchtmittelaufnahme.

## 4. 3D-Vorschau

### Separate Leuchtkasten-Rückwand

„Leuchtkasten“ wählen und unter „Separate Rückwand“ die Option „Rückwand ergänzen“ aktivieren. Die Kastenöffnung wird dann exakt aus Breite, Höhe und Rahmenbreite gebildet. Unter „Weitere Formoptionen“ Rahmenbreite (mindestens 1,2 mm) und Kastentiefe einstellen.

Die Rückwand besteht aus einer äußeren Platte und einem umlaufenden, erhöhten Einsteckrand. Einstellbar sind Plattenstärke, Einstecktiefe, Randstärke und Passspiel pro Seite. Bei 0,25 mm Spiel ist der Einsteckrand insgesamt 0,5 mm schmaler und niedriger als die Kastenöffnung. Die Einstecktiefe lässt mindestens 1 mm Abstand zum Fotopanel. Eine zentrale Durchführung von 0 mm schließt die Platte; ein positiver Wert öffnet sie für das Kabel.

Unter „Teil / Montage“ Kasten, Rückwand oder Montage wählen. Die Rückwand startet flach mit dem Einsteckrand nach oben; ihre Drucklage und Bettrotation werden unabhängig vom Kasten gespeichert. Schichtprüfung, Druckraumsuche und STL/3MF gelten jeweils für das aktive Einzelteil. Die Montage zeigt die Platte auf dem hinteren Kastenrand und wird als PNG, nicht als gemeinsames Druckmodell ausgegeben. „Hinten“ öffnet die Rückansicht.

Dies ist eine lose Steckabdeckung ohne Rastverschluss. Spiel und Halt am eigenen Drucker prüfen. Bestehende Projekte werden erst durch Aktivieren der Rückwand auf die neue maßhaltige Kastengeometrie umgestellt. Das vierte Beispielprojekt zeigt die vollständige Montage.

### Befestigungsbohrungen in der Rückwand

Bei aktiver Leuchtkasten-Rückwand den Bereich „Befestigungsbohrungen“ öffnen und „+ Bohrung hinzufügen“ wählen. Die App sucht eine freie Position für eine 4-mm-Bohrung. Bis zu acht zusätzliche Bohrungen sind möglich; die zentrale Kabelöffnung zählt separat.

Jede Zeile besitzt eigene X-/Y-Koordinaten und einen Durchmesser von 2–20 mm. X wird von der horizontalen Mitte gemessen, links negativ; Y von der Unterkante der aufrechten Rückwand. Beispiel bei 100 × 85 mm: X = −25, Y = 63,75, Ø = 4. Die Bohrung führt durch die Rückwandplatte und bleibt innerhalb des umlaufenden Einsteckrands. Mindestens 1 mm Material zum Einsteckrand bleibt erhalten. Auch Kabelöffnung und Nachbarbohrungen brauchen getrennte rechteckige Freizonen mit umlaufender Materialzugabe. Diese Prüfung ist konservativ; bei einer Fehlermeldung Position ändern, Durchmesser verkleinern oder Bohrung entfernen.

„Rückwand“ als Einzelteil wählen und „Druckschicht“ öffnen. Bei der flachen Standardlage zeigt eine untere Schicht Platte, Kabelöffnung und Befestigungsbohrungen; oberhalb der Plattenstärke bleibt der offene Einsteckring. Fehlerhafte Zahlen werden rot markiert und verhindern Vorschau und Export, bis sie korrigiert oder die betreffende Bohrung entfernt wird. Änderungen werden im Projekt gespeichert, einschließlich der eigenen Rückwand-Drucklage.

Die Rückwand bleibt eine lose Steckabdeckung. Ihre Bohrungen verriegeln den Kasten nicht und erzeugen keine Schrauben oder Rastverbindung. Senkungen lassen sich pro Öffnung ergänzen. Die mechanische Verbindung und Belastbarkeit des gedruckten Aufbaus separat prüfen. Das Leuchtkasten-Beispiel enthält vier zusätzliche Bohrungen.

### Langlöcher in der Rückwand

Unter „Befestigungsbohrungen“ besitzt jede Öffnung eine „Langloch · Zusatzlänge“. 0 mm erzeugt die bisherige Rundbohrung. Mit 0–40 mm Zusatzlänge entsteht ein Langloch mit geraden Seiten und halbrunden Enden; Gesamtlänge = Durchmesser + Zusatzlänge. Der Durchmesser von 2–20 mm bestimmt die freie Breite. Beispiel: 4 mm Durchmesser und 8 mm Zusatzlänge ergeben eine 4 × 12 mm große Öffnung.

Die Öffnung ist zunächst senkrecht. „Langloch waagerecht“ dreht sie in der aufrechten Rückwand. X und Y bezeichnen weiterhin die Mitte der gesamten Öffnung; die Enden liegen je eine halbe Gesamtlänge davon entfernt. Eigene Drucklagen und freie Druckwinkel drehen die Rückwand mitsamt ihren Öffnungen. Rundbohrungen und Langlöcher können gemischt werden; die zentrale Kabeldurchführung bleibt rund.

Die Freizonenprüfung berücksichtigt die volle Langlochlänge, Nachbaröffnungen, Kabeldurchführung und Einsteckrand. Bei zu wenig Platz Position, Länge oder Breite anpassen. Fehlerhafte Zahlen sperren Vorschau und Export; eine gültige Korrektur oder Entfernen der Öffnung stellt den Ablauf wieder her. Im Schichtschnitt der flachen Rückwand sind die durchgehenden Öffnungen sichtbar. Das sechste Beispiel „Leuchtkasten mit Langlöchern“ zeigt vier gemischte Langlöcher. Senkungen sind über ihre eigene Tiefe einstellbar. Es gibt keine Verriegelung; Befestigung und Passung praktisch prüfen.

### Schraubensenkungen auf der Außenseite

„Senkung außen · Tiefe mm“ erzeugt pro Rückwandöffnung eine 90°-Senkung. 0 deaktiviert sie. Aktive Tiefe mindestens 0,1 mm; mindestens 0,4 mm gerader Durchgang müssen stehen bleiben. Bei einer 2 mm starken Platte sind somit höchstens 1,6 mm Senktiefe zulässig. Durchmesser und Langloch-Zusatzlänge bestimmen weiterhin den unveränderten inneren Durchgang. Außenbreite = Durchmesser + 2 × Senktiefe; die äußere Gesamtlänge enthält zusätzlich die Langloch-Zusatzlänge.

Die Senkung liegt auf der äußeren Rückseite, ohne Einsteckrand. Bei der flachen Standarddrucklage liegt sie unten. Mit „Unten“ ansehen oder eine andere Drucklage wählen. In der Schichtansicht zeigt eine untere Schicht die größere Öffnung, eine Schicht oberhalb der Senktiefe den geraden Durchgang. Der Winkel ist fest: 45° je Fase, 90° an der Rundbohrung.

Die größere Außenöffnung benötigt mehr Platz zum Einsteckrand und zu Nachbaröffnungen. Zu tiefe Senkungen oder verletzte Freizonen sperren Vorschau und Export. Die zentrale Kabelöffnung bleibt ungesenkt. Das siebte Beispiel „Leuchtkasten mit Senkungen“ zeigt Rund- und Langlöcher mit unterschiedlichen Tiefen. Schraubenkopfmaße, Material und Passung praktisch prüfen. Die Rückwand bleibt eine lose Steckabdeckung ohne Verriegelung oder Belastbarkeitsfreigabe.

### Druckausrichtung wählen

Im rechten Panel „Druckausrichtung“ aufklappen. Aufrecht, flach mit Motiv nach oben/unten, rechte/linke Kante unten oder kopfstehend wählen. Die Richtungen beziehen sich auf die ursprüngliche aufrechte Modelllage. Bei Kugeln und Lampen beschreiben sie dieselben Modellachsen. „90° auf dem Druckbett drehen“ vertauscht zusätzlich Breite und Tiefe. Die Auswahl verändert die tatsächliche Geometrie für Vorschau, Schichten und Export und wird im Projekt gespeichert.

Unter „Freie Druckwinkel“ X, Y und Z in Grad von −180 bis +180 eingeben; Dezimaltrennzeichen ist das Komma. Die Winkel beziehen sich auf die Druckerachsen: X und Y liegen auf dem Bett, Z zeigt nach oben. Z dreht auf dem Bett. Nach der gewählten Grundlage wird um die Modellmitte zuerst X, dann Y, dann Z gedreht; anschließend folgt gegebenenfalls die zusätzliche 90°-Bettrotation. Die Reihenfolge ist fest. Jeder Körper, Sockel und jede Rückwand besitzt eigene Winkel. Die Montage bleibt aufrecht. „Freie Winkel zurücksetzen“ setzt nur die drei Winkel des aktiven Einzelteils auf null. Foto und Materialstärken bleiben unverändert. Ungültige Zahlen oder Werte außerhalb des Bereichs sperren Vorschau und Export; Korrektur oder Zurücksetzen hebt die Sperre auf.

„Passend zum Druckraum ausrichten“ berechnet das Modell in Exportauflösung. Passt die aktuelle Lage einschließlich freier Winkel, wird sie beibehalten. Dabei werden die tatsächlichen Modellpunkte geprüft. Andernfalls sucht die App zwölf rechtwinklige Lagen ohne freie Winkel: zuerst aufrechte, dann flache und seitliche Varianten. Bei einem Treffer werden Grundlage, Bettrotation und auf null gesetzte freie Winkel nur für das aktive Teil übernommen. Ohne Treffer bleibt die bisherige Lage erhalten. Die Berechnung lässt sich abbrechen. Das Ergebnis nennt Druckmaße und effektives Export-Raster. Eine automatische Suche beliebiger schräger Winkel ist nicht enthalten.

Die Druckmaße sind Breite × Tiefe × Bauhöhe. Ein Lagewechsel verwirft vorhandene Prüfmodelle und Schichtdaten. Die Kamera wird neu eingepasst; flache Modelle werden beim Zurücksetzen von oben betrachtet. Die Suche kontrolliert die Hülle, nicht die Stabilität, Stützstrategie oder Druckqualität. Diese Punkte im Slicer prüfen. Exportiert wird das Modell auf Z=0; das Zentrieren auf dem konkreten Druckbett erfolgt im Slicer.

### Gemeinsamer Drucksatz aus zwei Einzelteilen

Bei aktiver Leuchtkasten-Rückwand oder aktivem Kugel-/Mondsockel im Bereich „Ansicht“ auf „Drucksatz · alle Teile“ klicken. Die App berechnet beide Einzelteile in Exportauflösung und prüft sie getrennt. Auch aus der Montageansicht ist dieser Schritt erreichbar. Die Montage selbst bleibt eine Ansicht.

Die gespeicherte Drucklage und Bettrotation jedes Teils bleiben erhalten. Beide Teile werden auf Z=0 gesetzt und auf dem im Projekt gewählten Druckbett angeordnet. Rechts den Mindestabstand zum Bettrand von 0–50 mm und den Teileabstand von 1–50 mm einstellen. Die Werte beziehen sich auf die Hüllrechtecke; bei Zeile/Spalte bleibt die Gruppe auf dem Bett zentriert. Die Vorgabe ist jeweils 5 mm. „Automatisch“ vergleicht Zeile und Spalte und wählt die passende Variante mit der kleineren Hüllfläche. Alternativ „Zeile · nebeneinander“ oder „Spalte · hintereinander“ fest wählen. Passt diese Richtung nicht, wird sie mit einer Erklärung abgewiesen und nicht automatisch ersetzt. Die Bauhöhe wird ebenfalls geprüft. Es wird weder skaliert noch automatisch umgedreht.

Nach einer Änderung „Drucksatz neu berechnen“ wählen. Alte Drucksatzdaten und Exportmöglichkeiten werden sofort verworfen. Beide Abstände und die Anordnungswahl werden im Projekt gespeichert und machen es geändert. Foto, Bildkorrekturen, Modellparameter und Einzelteil-Drucklagen bleiben erhalten. Die Einstellungen sind nicht Bestandteil eigener Modellvorlagen.

„Manuell · freie Positionen“ übernimmt bei vorbereitetem Drucksatz beide aktuellen Positionen. Alternativ ein Teil direkt im Bettbild ziehen. Die Position bezieht sich auf die linke untere Ecke seines Hüllrechtecks, gemessen vom Bettursprung links unten; X nach rechts, Y nach oben. Vier Zahlenfelder erlauben genaue Werte in Millimetern. Mit angeklicktem Teil verschieben die Pfeiltasten um 1 mm, Umschalt+Pfeil um 10 mm. Ziehen rundet auf 0,1 mm und begrenzt auf den zulässigen Bettrand. Escape bricht eine laufende Ziehbewegung ab. Bei Unterschreitung des Teileabstands erscheint die bewegte Hülle rot; Loslassen weist die Bewegung ab und behält die letzte gültige Position. Numerische Werte werden ebenfalls geprüft. Ohne vorbereiteten Drucksatz erfolgt die Prüfung der tatsächlichen Hüllen erst bei „Drucksatz neu berechnen“.

Gültiges Verschieben aktualisiert den Drucksatz ohne erneute Netzberechnung und macht das Projekt geändert. „Projekt speichern“ erhält die Positionen unabhängig vom 3MF-Export. Geometrie-, Drucker- und Abstandsänderungen erfordern weiterhin eine Neuberechnung. Zur automatischen Zentrierung zurückkehren: „Automatisch“ wählen und neu berechnen. Die manuelle Anordnung umfasst die beiden Teile des aktuellen Projekts; sie fügt keine weiteren Projekte hinzu.

„↶ Zurück“ und „↷ Wiederholen“ unter den Positionseinstellungen betreffen die Drucksatzplanung: gültige Positionswerte, Maus-/Pfeilbewegungen, Rand-/Teileabstände und Anordnungswahl. Die Tastenkürzel sind Strg+Alt+Z und Strg+Alt+Y und gelten im sichtbaren Drucksatzeditor. Strg+Z/Y bleibt bei der Bildbearbeitung. Bis zu 100 Schritte werden innerhalb der Sitzung gespeichert. Neue Änderungen nach einer Rücknahme verwerfen den Wiederholen-Zweig. Ein ungültiger Ziehversuch erzeugt keinen Schritt; Zahlenfelder können mehrere gültige Zwischenwerte erzeugen.

Wenn weiterhin ein vorbereiteter Satz und bekannte Zielpositionen vorhanden sind, verwendet die Rücknahme dieselben Netze und ist unmittelbar exportierbar. Nach verworfenen Prüfdaten oder Abstandsschritten ohne bekannten Positionsstand muss der Satz neu berechnet werden; die Statuszeile weist darauf hin. Modell-, Foto-/Bildkorrektur- und Druckeränderungen sowie Öffnen/Neuanlegen leeren die Drucksatzhistorie. Die Historie selbst wird nicht im Projekt gespeichert; die aktuell gewählte Planung bleibt gespeichert. Speichern allein leert die Historie nicht. Ungültige Eingaben zunächst korrigieren; während einer Berechnung oder bei Eingabefehlern sind die Aktionen gesperrt.

Die Bettübersicht zeigt die Hüllrechtecke blau und grün, mit Teilenamen und X-/Y-Richtung. Rechts stehen die tatsächlichen B × T × H-Maße, Dreieckzahlen und effektiven Raster. Dies ist eine Anordnungshilfe, keine Silhouette oder Sliceransicht. „Bettübersicht als PNG“ speichert diese Darstellung.

Nach erfolgreicher Berechnung „Drucksatz als 3MF speichern“ beziehungsweise unter Export „Alle Teile als 3MF-Drucksatz“ wählen. Der Speicherdialog startet im Ordner des geöffneten Projekts. Die Datei enthält zwei unabhängige, benannte Objekte mit ihren Bettpositionen. Beide werden nach dem Schreiben erneut eingelesen und geprüft. Bei zu kleinem Druckbett oder zu geringer Bauhöhe bleibt der Export gesperrt. Über „3D-Modell“ zu den Einzelteilen zurückkehren und Drucklagen, Abmessungen oder Druckerprofil ändern; anschließend den Drucksatz neu berechnen. Alternativ die bisherigen Einzelteil-STL/3MF-Dateien verwenden.

Modell-, Bild-, Drucklagen-, Abstands-, automatische Anordnungs- oder Profiländerungen verwerfen vorbereitete Drucksatzdaten. Berechnung und Export lassen sich abbrechen; eine vorhandene Zieldatei wird erst nach erfolgreicher vollständiger Prüfung ersetzt. Die Ansicht und Berechnung allein verändern den Projektstatus nicht. Eine Änderung von Abstand oder Anordnungswahl wird im Projekt gespeichert. Format 15 speichert die freien XY-Positionen und diese Optionen, Geometrieparameter, Einzelteil-Drucklagen und freie Winkel; der Drucksatz wird daraus neu erzeugt. Die freien Winkel jedes Einzelteils bleiben auch im Drucksatz erhalten.

Die 3MF-Datei enthält keine Slicerprofile, G-Code-Daten, Stützen, Druckzeiten oder Materialberechnung. Positionierung, Druckbettbezug und Stützstrategie nach dem Import im eigenen Slicer kontrollieren.

### Kamera und Licht

Linke Maustaste zum Drehen, rechte Maustaste oder Umschalt+Ziehen zum Verschieben, Mausrad zum Zoomen. Die Werkzeugleiste bietet Kamera-Reset, Vorder-/Rück-/Seiten-/Drauf-/Unteransicht, Licht, Bodengitter und ein ausgedünntes Drahtgitter. Die Maße stehen links unten als Breite × Höhe × Tiefe. Die Achse nach oben im Editor wird beim Export in die Z-Achse des Slicers überführt; das Modell wird auf Z=0 gestellt.

Der Lichtregler wirkt auf die warme, bildbasierte Emissionsvorschau. Dies ist keine kalibrierte optische Simulation. Die Materialansicht zeigt die reale Meshoberfläche. Die Vorschau ist gröber als der Export und wird nach etwa 400 ms Eingabepause neu berechnet.

### Druckschichten prüfen

Im rechten Panel „Druckschicht“ wählen. Schichtnummer oder Schieberegler verändern die Schnitthöhe. Die Prüfschichthöhe lässt sich zwischen 0,01 und 1,00 mm einstellen; sie ist unabhängig vom Druckerprofil und gilt nur für diese Ansicht. Geschnitten wird in der Mitte der Schicht, auch bei einer kürzeren letzten Teilschicht. Die Z-Höhe beginnt auf der exportierten Auflage bei 0.

Warme Flächen zeigen Material, cyanfarbene Linien die Konturen. Innenräume und Bohrungen bleiben frei. Angezeigt werden Flächeninhalt, Konturzahl, Höhe und tatsächliches Geometrieraster. Ziehen verschiebt die XY-Ansicht, das Mausrad zoomt. Doppelklick oder „Ansicht zurücksetzen“ passt die ganze Modellhülle ein. „Schicht ←/→“ wechselt jeweils eine Schicht.

Zunächst wird das schnelle Vorschaumesh geschnitten. „Mit Exportauflösung prüfen“ berechnet und prüft die volle Geometrie mit demselben Verfahren und Speicherbudget wie der Modellexport. Das kann länger dauern und lässt sich abbrechen. Das effektive Raster wird angegeben; die budgetabhängige Anpassung gilt auch hier. „Vorschauraster verwenden“ gibt das zusätzliche Prüfmodell frei. Änderungen an Bild, Modell oder Druckerprofil verwerfen das Prüfmodell automatisch.

„Schicht als PNG speichern“ speichert die aktuelle Ansicht einschließlich Raster und Höhenangabe. Die Schichtprüfung erzeugt keine Werkzeugwege, keinen G-Code und keine Resin-Belichtungsbilder. Stützen und die tatsächliche Druckstrategie weiterhin im Slicer prüfen. Bei einer Warnung zu Eckkontakten oder offenen Konturen benachbarte Schichten kontrollieren.

## 5. Exportieren

Unter „Export“ STL oder 3MF wählen, Dateinamen und Zielordner bestimmen. Das Programm berechnet das Exportmodell, prüft geschlossene und konsistent orientierte Kanten, endliche Koordinaten, degenerierte Dreiecke und positives Volumen. Anschließend wird die Datei geschrieben, erneut eingelesen und auf Topologie, Dreieckzahl und Maße geprüft. Erst danach ersetzt sie die Zieldatei. Bei Abbruch bleibt eine vorhandene Zieldatei erhalten.

Der Fortschrittsbalken zeigt die Berechnung und das Schreiben; das erneute Einlesen am Ende kann zusätzliche Zeit benötigen. „Abbrechen“ beendet den Vorgang. Große Modelle können wegen der Speichergrenze ein gröberes effektives Raster bekommen; der tatsächliche Wert erscheint nach dem Export in der Statuszeile. Die Statuszeile besitzt einen Tooltip für den gesamten Dateipfad und die vollständige Meldung.

Der PNG-Export speichert die aktuell aktive 3D- oder Schichtansicht. „Bearbeitbares Projekt speichern“ erhält das Originalbild und alle Modellparameter. Kamera, Prüfschichthöhe und Prüfmodell werden nicht im Projekt gespeichert.

## Projekte und Wiederherstellung

Strg+S speichert `.lithopro`, Strg+Umschalt+S speichert unter einem anderen Namen. Strg+O öffnet ein Projekt; Strg+N beginnt ein neues. Vor dem Wechseln oder Schließen wird bei Änderungen Speichern angeboten. „Meine Projekte“ enthält die letzten zwölf vorhandenen Projektdateien mit Vorschaubildern sowie Öffnen, Kopie, Umbenennen und Löschen. Löschen erfordert eine Bestätigung und entfernt die Datei von der Festplatte.

Alle 40 Sekunden wird bei Änderungen und ohne laufenden Export eine Wiederherstellung unter `%LOCALAPPDATA%\LithoCreate Pro\recovery.lithopro` angelegt. Nach einem unerwarteten Ende wird sie beim nächsten Start angeboten. Ein reguläres Speichern oder bestätigtes Schließen entfernt die temporäre Wiederherstellung. Zwischen zwei Sicherungen können Änderungen verloren gehen. Ab Version 1.1.0 verhindert eine Dateisperre mehrere Instanzen im gleichen Datenordner. Windows gibt die Sperre beim Ende des Prozesses frei. Ein Fenster von Version 1.0.0 muss vor dem Wechsel auf 1.1.0 geschlossen werden, weil die alte Version noch keine Sperre kennt.

## Druckhinweise

STL enthält konventionsgemäß Millimeter; 3MF enthält die Einheit ausdrücklich. Das Modell im Slicer öffnen, Maße und Ausrichtung kontrollieren. Bei PLA mit einem kleinen Probestück beginnen. Kugeln/Mond sind wahlweise geschlossene Hohlkörper oder erhalten eine untere Lampenöffnung und eine obere Entlüftung. Für Resin den tatsächlichen Ablaufweg, Druckausrichtung und weitere nötige Drainagen im Slicer prüfen. Der separate Stecksockel wird in dieser App erzeugt. Weitere Fassungen, Stützen und die geeignete Druckstrategie werden weiterhin im Slicer bzw. CAD ergänzt. Eine elektrische Lichtquelle und Gehäuseteile werden nicht mitgeliefert. Eine Material-/Druckerempfehlung ist keine Garantie für ein bestimmtes optisches Ergebnis.

## Beispielprojekte und ältere Dateien

Im veröffentlichten Ordner `Beispiele` liegen zwölf bearbeitbare Leuchtturmprojekte: flaches Bild mit Standfuß, Zylinder mit Boden, Kugel mit Öffnung und Stecksockel, Leuchtkasten mit Rückwand und vier Bohrungen, ein flaches Bild mit freien Druckwinkeln, ein Leuchtkasten mit Langlöchern, ein Leuchtkasten mit Senkungen, ein Leuchtturm im Hochformat 4:5, ein Leuchtturm mit gekoppelten Maßen, ein Bilderrahmen mit zwei Aufhängelaschen sowie je ein Leuchtkasten mit eigener Drucksatzplanung und mit freier Platzierung. Die bisherigen Kugel- und Leuchtkastenbeispiele öffnen die Montageansicht. Die Beispiele mit Langlöchern und Senkungen öffnen direkt die Rückwand. Das Winkelbeispiel startet flach mit X=20°, Y=0°, Z=15°. Die Dateien lassen sich direkt über „Projekt öffnen“ laden.

Neue Dateien verwenden Projektformat 15. Dateien aus Format 1 bis 14 bleiben lesbar; beim nächsten Speichern werden sie in Format 15 überführt. Apps bis einschließlich 1.17.0 können Format 15 nicht öffnen. Für einen Rückwechsel die ursprüngliche Datei behalten oder „Speichern unter“ verwenden. Vor Format 8 starten freie Winkel bei null. Dateien vor Format 9 behalten Rundbohrungen ohne Langloch-Zusatzlänge. Vor Format 10 bleiben alle Senktiefen auf null. Vor Format 11 startet die Verhältniswahl bei 4:5 ohne aktive Bindung; der gespeicherte Ausschnitt wird unverändert übernommen. Vor Format 12 bleibt die Maßkopplung ausgeschaltet. Vor Format 13 werden keine Aufhängelaschen aktiviert. Vor Format 14 beginnt die Drucksatzplanung mit 5 mm Rand-/Teileabstand und automatischer Anordnung. Vor Format 15 bleiben vorhandene Abstände und Zeilen-/Spaltenanordnung erhalten; freie Positionen sind nicht aktiv. Die bisherige Geometrie und Ausrichtung bleiben erhalten. Dateien vor Format 4 behalten die aufrechte Drucklage ohne Bettrotation. Vor Format 5 wird kein Stecksockel, vor Format 6 keine Rückwand und vor Format 7 keine zusätzlichen Bohrungen aktiviert. Die Originalbildbytes bleiben erhalten.


## STL, Formmodule und Halterungen (ab 1.20)

Links „STL & Module“ wählen. Das Fenster arbeitet mit einem Schnappschuss des aktuellen Fotos, der Bildkorrekturen und Modellmaße. „STL öffnen“ liest ASCII oder Binär-STL als Millimeter/Z-up ein. Maße, Volumen und Kantenprüfung stehen rechts. Bei Fehlern bleibt Export gesperrt. Gültige Modelle lassen sich geprüft als STL/3MF speichern. Es werden keine beliebigen CAD-Netze automatisch zu Fotovorlagen umgewandelt.

„Formmodul laden“ wählt eine selbst erstellte oder vertrauenswürdige DLL. Die Bestätigung betrifft echte Codeausführung; Details in FORMMODULE.md. Die Beispiele liegen im veröffentlichten Ordner Extensions. Das Fotoplattenmodul verwendet die eingegebenen Bohrungen. X ab Mitte, Y ab Unterkante, Durchmesser in mm; je Zeile drei Werte mit Leerzeichen, z. B. `-15 20 4`. Dezimalkomma verwenden. Höchstens acht Bohrungen; Rand-/Lochabstände werden geometrisch geprüft. Ergebnisse als Modelldatei speichern. Zusätzliche Prüffenster-/Modulparameter werden nicht in das normale .lithopro-Projekt übernommen.

Unter „Licht & Halterungen“ die Variante wählen, Maße eingeben und berechnen. Bei LED-U-Haltern sind die freie Innenbreite und Länge gemeint; die Innenhöhe beträgt 6 mm. E14/E27-Maßringe benötigen den tatsächlichen freien Durchlass und Außendurchmesser. Die Winkelstütze ist 20 mm breit. Eine Diffusorplatte ist ein geometrisches Probestück, keine optisch kalibrierte Folie. Passive Bauteile ersetzen keine geprüfte elektrische Fassung. Für PETG keine klassische Glüh-/Halogenlampe als freigegeben betrachten; LED-Leuchtmittel, Abstände und Temperaturen an der kompletten Konstruktion prüfen.

„In Bambu Studio öffnen“ verlangt zunächst eine geprüfte lokale STL/3MF-Datei. Anschließend öffnet das installierte Bambu Studio diese Datei. Dort P1S, tatsächliche Düse und das passende PETG-Filamentprofil wählen, Druckplatte und ausgeschlossene Bettbereiche prüfen, schneiden und die Werkzeugwege/Stützen kontrollieren. Die App startet keinen Druck und lädt nichts zum Drucker hoch. Die Slicer-Zeit und der Materialverbrauch sind Prognosen.

Unter Einstellungen „P1S / PETG hinzufügen“ legt das nominale Geräteprofil an. Ein schon vorhandener gleichnamiger Eintrag wird übernommen und nicht durch die Vorgabewerte überschrieben. Die Schicht-/Düsenwerte sind Ausgangswerte, keine geprüften Einstellungen des konkreten Nutzerdruckers.

Das ZIP Probedrucke/P1S-PETG-Probedrucke.zip enthält acht Lichtplatten und vier Kasten-/Deckel-Paare mit verschiedenen Spielen. Alle Teile sind geometrisch geprüft. Ergebnisse im CSV eintragen; erst reale Ausdrucke erlauben Aussagen über PETG-Passung und Lichtwirkung. Für aufrecht gedruckte Lithophanien die gewählten Dicken zusätzlich in der späteren Drucklage testen.
## Mehrteilige Fotovorlagen

Unter „Vorlagen & Formen“ findest du Fotolaterne, Fotowabe, Mini-Häuschen und den teilbaren Panoramaschirm. „Bauteile und Wandmotive …“ öffnet für Laterne und Häuschen die Auswahl der vier Seiten. Pro Seite kannst du ein separates Foto oder Text übernehmen. Freie Seiten verwenden das bearbeitete Projektfoto. Die abweichenden Motive werden mit der Projektdatei gespeichert.

Über „Druckteile als ZIP exportieren …“ erzeugst du einzelne STL- oder 3MF-Dateien. Entpacke das Archiv und importiere die Druckteile in deinen Slicer. Dort kannst du Verbinder oder Klammern vervielfältigen. Die Montageanleitung liegt im ZIP.

Die Fotowabe skaliert Breite und Höhe gemeinsam. Verwende für eine zusammenhängende Fläche dieselben Maße und dieselbe maximale Wandstärke. Beim Panoramaschirm kannst du den Durchmesser, die Höhe und 3–16 Segmente einstellen. Wenn ein Segment für das gewählte Druckprofil zu breit ist, erhöhe die Segmentzahl.
