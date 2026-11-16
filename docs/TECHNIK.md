# Technische Dokumentation

## Aufbau

`src/LithoCreate.Core` ist eine UI-unabhängige .NET-10-Bibliothek. Sie enthält Projekte, Bildparameter, den Formkatalog, Graubildabtastung, eigene Meshgenerierung, Topologieprüfung, ZIP-Projektspeicherung und STL-/3MF-I/O. `src/LithoCreate.App` enthält WPF/XAML, das zentrale MVVM-ViewModel, die WIC-Bildpipeline und die interaktive Viewport-Komponente. View-Code kümmert sich um Mausgesten, Darstellung und Eingabevalidierung; Berechnungen und Persistenz liegen außerhalb der Fensterklasse. `tests/LithoCreate.Tests` ist ein ausführbares Integrations-Testprogramm ohne zusätzliche Testpakete. `tools/LithoCreate.Packager` erzeugt über die native `msi.dll` ein Windows-Installer-Datenbankpaket und verwendet Windows `makecab.exe` für den eingebetteten Cabinet-Inhalt.

## Kompatibilität und Abhängigkeiten

Gebaut mit SDK 10.0.403, Zielframework `net10.0-windows`, x64-Veröffentlichung mit .NET/WindowsDesktop 10.0.14. Rendering verwendet `System.Windows.Media.Media3D`; Bilder verwenden Windows Imaging Component. Externe Mesh-, MVVM-, Rendering- oder Bildpakete werden nicht benötigt. Dadurch entfällt eine zusätzliche Kompatibilitäts-/Lizenzabhängigkeit für die Kernberechnung. Microsofts Laufzeit und deren mitgelieferte Lizenz-/Drittanbieterhinweise werden im Self-contained-Ordner veröffentlicht. Der Installer lädt selbst nichts nach.

Offizielle Referenzen für die Implementierungsentscheidung, geprüft am 03.11.2026:

- [.NET-Veröffentlichung und Self-contained-Deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
- [WPF-3D-Übersicht](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/3-d-graphics-overview)
- [Windows Installer-Datenbanktabellen](https://learn.microsoft.com/en-us/windows/win32/msi/database-tables)
- [Windows Imaging Component](https://learn.microsoft.com/en-us/windows/win32/wic/-wic-about-windows-imaging-codec)

Die im Auftrag genannte Webseite diente als funktionale Referenz. Es wurden weder deren Oberfläche noch Code oder Inhalte übernommen.

## Geometrie

Die Grundformel lautet `t = t_min + (1 - luminance) * (t_max - t_min)`. Der Negativmodus kehrt die Helligkeit für diese Formel um. Luminanz wird mit 0,2126 R + 0,7152 G + 0,0722 B berechnet. Das fertig bearbeitete Bild wird bilinear abgetastet. Die Implementierung setzt nicht bloß ein flaches STL auf eine andere Form um: Zylinder, Polygonlampe, Bogen und Kugel werden direkt in ihren Parameterräumen aufgebaut; Materialstärken folgen der jeweiligen radialen Richtung.

Rechteckige Platten erhalten zwei zusammenhängende Gitterschichten und geschlossene Randquads. Zylinder schließen die periodische U-Naht ohne doppelte Randvertices. Geschlossene Kugeln verwenden je einen einzelnen Vertex an jedem Pol, getrennte innere/äußere geschlossene Oberflächen und umgekehrte Orientierung des Innenraums. Herz und Stern verwenden formabhängige Konturen mit konzentrischen Ringen und einer zentralen Dreiecksfächerfläche. Der Anhänger verwendet einen Ringquerschnitt mit geschlossenem Lochrand. Die radialen Plattenkonturen werden auf die gewünschten äußeren Breiten/Höhen normiert.

Die Vorschau rechnet mit höchstens ungefähr 22.000 Oberflächenzellen; der Export mit bis zu 1,2 Mio. Zellen und zusätzlich speicherabhängiger Begrenzung. Für die Budgetrechnung werden konservativ 600 Byte je Oberflächenzelle vorgesehen. Das berücksichtigt wesentliche Mesh- und Kantentabellen, stellt aber kein strenges Prozesslimit dar. Das effektive Raster kann gröber als der Wunschwert sein. Wiederholte Validierungen und WPF erzeugen zusätzliche temporäre Objekte.

Die Kantentabelle verwendet einen gemischten Hash aus beiden Indexhälften. Ein einfacher `ulong`-Hash erzeugte bei regelmäßigen Gittern starke Kollisionen; ein größerer Integrationstest deckte dies auf und die Implementierung wurde korrigiert.

Intern ist Y die Höhenachse. Exportiert wird die orientierungserhaltende Rotation `(x,y,z) → (x,-z,y-minY)`. So liegt das Modell im Slicer auf Z=0 und die Druckraumprüfung entspricht der Exportorientierung. STL speichert Float32 und hat keine ausdrückliche Einheit; 3MF erhält `unit="millimeter"`.

## Bildpipeline und Hintergrundarbeit

Das Original wird als Bytefolge erhalten, nicht überschrieben. Vor der Pixelbearbeitung erfolgt ein geometrischer Zuschnitt, anschließend Spiegelung und Rotation. Die WIC-Arbeitskopie ist auf 1800 Pixel begrenzt. Die Bildkorrektur arbeitet in BGRA, berücksichtigt transparente Pixel auf weißem Grund und erzeugt gleichzeitig Graubild und Histogramm. Nachbarfilter sind bewusst einfach und lokal: 3×3-Mittelung für Rauschminderung und eine Differenz zur lokalen Mittelung für Schärfe. Dies ist keine KI-Entrauschung.

Vorschauänderungen werden um 400 ms verzögert. Die vorherige Berechnung erhält ein CancellationToken. Bild- und Meshberechnung laufen über `Task.Run`; die gefrorene Bitmap und das Ergebnis werden anschließend auf dem UI-Thread übernommen. Ein Export verwendet einen Snapshot sämtlicher relevanter Parameter und liest das Original in einer vom Speicherbudget abhängigen Auflösung bis 4096 Pixel erneut ein. Abbruch wird in Bildzeilen, Meshzeilen, Exportblöcken und während der Validierung geprüft. Eine einzelne WIC-Decodierung ist nicht mitten im Decoder abbrechbar.

## Dateiformate und Fehlerbehandlung

`.lithopro` ist ZIP mit `project.json`, `source.bin` und optional `preview.png`. Version 15 wird geschrieben; Version 1 bis 14 wird übernommen. Für Format 1/2 bleibt die EXIF-Neuausrichtung deaktiviert; für Formate vor 4 bleibt die Drucklage aufrecht ohne Bettrotation. Vor Format 5 wird kein Sockel aktiviert. Vor Format 6 bleibt die Rückwand deaktiviert und die alte Kastengeometrie erhalten. Vor Format 7 bleibt die Liste zusätzlicher Rückwandbohrungen leer. Vor Format 8 werden alle drei Winkelsätze auf null gesetzt. Vor Format 9 werden Zusatzlängen und Richtungen auf Rundbohrungen zurückgesetzt. Vor Format 10 werden alle Senktiefen auf null gesetzt. Vor Format 11 startet das Zuschneideverhältnis bei 4:5 ohne aktive Bindung; bisherige Ausschnitte bleiben erhalten. Vor Format 12 bleibt die Maßkopplung ausgeschaltet. Unbekannte Versionen und fehlerhafte Bild-, Form-, Teil-, Sockel-, Ausrichtungs-, Winkel- und Druckereinstellungen werden vor der Übernahme abgewiesen. Die Originalbytes bleiben unverändert. Größenbegrenzungen: Quelldatei/Bilddaten 64 MB, Projektbeschreibung 1 MB, Vorschaubild beim Listenlesen 4 MB. Speichern erfolgt zunächst in eine temporäre Datei neben dem Ziel und anschließend per Ersetzen. Die App verwaltet die letzten zwölf vorhandenen Pfade in `recent.json` und Druckerprofile in `printers.json`.

3MF wird als OPC-ZIP mit `[Content_Types].xml`, `_rels/.rels` und `3D/3dmodel.model` geschrieben. Es enthält einen Meshgegenstand und einen Build-Eintrag. XML-Einlesen unterbindet DTDs. Der interne STL-Reader unterstützt die selbst erzeugten binären STL-Dateien und verschweißt identische Float-Koordinaten beim erneuten Einlesen.

Nach jeder Modell-Ausgabe werden Topologie und Maße der geschriebenen Datei erneut geprüft. Ein Fehler oder Abbruch entfernt die temporäre Exportdatei; eine bereits vorhandene Zieldatei wird erst nach erfolgreicher Prüfung ersetzt. Es gibt keine allgemeine Reparatur fremder Modelle.

## Build und Installation

`build.ps1` baut seriell und ohne gemeinsamen Roslyn-Compiler, damit der Build auch in eingeschränkten Entwicklungsumgebungen reproduzierbar ist. Für die erste Self-contained-Veröffentlichung werden die offiziellen Runtime-NuGet-Pakete benötigt. Nach dem Cachen ist der Build mit `--no-restore` offline möglich; die Anwendung selbst enthält keinerlei Netzwerkanforderung.

Der MSI-Packager hat keine externen NuGet-Abhängigkeiten. Jede Programm-/Laufzeitdatei erhält eine stabile MSI-Komponente. Das Paket enthält native Datei-, Verzeichnis-, Feature-, Registry-, Shortcut-, Upgrade- und Installationssequenzen. Es arbeitet pro Benutzer und enthält keine eigenen erhöhten Custom Actions. Die optionale Desktopverknüpfung ist an `DESKTOP=1` gebunden. Gespeicherte Projekte und Einstellungen liegen außerhalb der vom Paket verwalteten Komponenten und werden nicht gelöscht. Bei automatischer Installation: `msiexec /i <Paket.msi> DESKTOP=1`; ohne DESKTOP bleibt die Desktopverknüpfung aus. Das Startmenü wird immer angelegt.

Entwicklungseingriff für isolierte Tests/portable Datenhaltung: Die Umgebungsvariable `LITHOCREATE_DATA_DIR` kann den App-Datenordner überschreiben. Sie wird von der fertigen App nicht automatisch gesetzt.

## Erweiterungen in 1.1.0

Standfüße werden als zusätzliche Gitterschichten mit exakten Schulterhöhen integriert. Ihre Stirnwände verwenden einen unterteilten Dreiecksfächer, damit an den Schultern keine T-Verbindungen oder degenerierten Quads entstehen. Lampenböden schließen den unteren Mantel über Dreiecksfächer bzw. zwei Ringflächen mit verbindender Lochwand. Kugelöffnungen entstehen durch eine Begrenzung des sphärischen Parameters, verbundene Innen-/Außenränder und einen angefügten ringförmigen Kragen. Der untere äußere Rand schneidet die Kugel in derselben Ebene wie der innere Rand. Ein weich auslaufender Verstärkungsbereich beginnt mit maximaler Wandstärke; damit entsteht bei dünnen Fotobereichen kein schmaler Hals zwischen sphärischer Wand und zylindrischem Kragen. Bei 0 mm Kragenhöhe werden die vorhandenen Ringe direkt verbunden, ohne zusammenfallende zusätzliche Wandflächen. Kreisöffnungen kompensieren den Unterschied zwischen Polygon-Umkreis und freiem Inkreis. Bei Polygonlampen wird die Materialstärke entlang der Flächennormalen statt als konstanter radialer Abstand berechnet.

Für die Vorschau kennzeichnet das Mesh Foto- und Strukturvertices. Ein separater Materialteil verhindert die Fotoprojektion auf Standfuß, Boden und Kragen. Nur im Rendernetz werden UV-Nahtvertices dupliziert; das geprüfte Exportnetz bleibt verschweißt. Die automatische Kameraeinpassung prüft alle acht Hüllenecken gegen horizontales und vertikales Sichtfeld. Parameter-Snapshots werden mit skalaren Kopien erzeugt; dadurch erreichen auch nicht endliche Eingaben die kontrollierte Validierung, statt bereits bei einer JSON-Zwischenserialisierung einen ungefangenen Fehler zu erzeugen.

`InstanceLock` hält im Datenordner eine Datei mit `FileShare.None` geöffnet. Die Sperre gilt pro Datenordner und wird vom Betriebssystem auch nach einem Prozessabbruch freigegeben. Mehrere isolierte Testordner bleiben möglich. Die 1.0.0-Version kennt diese Sperre noch nicht und muss vor einem Versionswechsel geschlossen werden.

Der Release-Build liest die Versionsnummer aus der App-Projektdatei, veröffentlicht in einen versionierten Ordner, legt zwölf geprüfte Beispielprojekte bei und erzeugt MSI, ZIP und SHA-256-Datei. Der MSI-Packager liest seine Produktversion aus der veröffentlichten DLL, verwendet eine neue stabile Produktkennung je Version und dieselbe Upgrade-Kennung. Eine Eingabedateiliste mit Größen und Zeitstempeln verhindert die Wiederverwendung eines nicht passenden Cabinet-Inhalts. Der optionale Installer-Lebenszyklustest prüft Installation, Upgrade, Rückstufungssperre, Desktopverknüpfung und unveränderte Projektdateien. Er bricht ab, wenn bereits eine Benutzerinstallation oder eigene Verknüpfung vorhanden ist.

Die freiwillige unabhängige Slicerprüfung `python tools/check_slicer.py` verwendet ein bereits installiertes Bambu Studio, exportiert drei Testmodelle erneut als 3MF und vergleicht Dreieckzahlen und Modellhüllen aus allen enthaltenen Meshdateien. Sie ist keine Voraussetzung für den Offlinebetrieb der Anwendung und versendet keinen Druckauftrag.

## Erweiterungen in 1.2.0

`CrossSectionIndex` schneidet das interne Y-aufrechte Netz in horizontalen Ebenen. Die resultierenden XY-Koordinaten entsprechen der Exportabbildung `(x, -z)`; die Bauhöhe wird vom unteren Modellrand gemessen. Ein Index mit 256 Höhenbereichen enthält maximal zwei Millionen Dreieckverweise. Bei Überschreitung wird ohne diesen zusätzlichen Index gescannt. Ein räumliches Toleranzraster verschweißt Schnittpunkte; gerichtete Kanten behalten Material links und erlauben eine vorzeichenbehaftete Flächenberechnung mit Innenräumen. Koplanare Flächen werden auf ihre Randkanten reduziert und innere Gegenkanten entfernt. Abweichende Knotengrade und offene Konturen werden angezeigt. Diese Geometrieprüfung ersetzt keine Werkzeugwegberechnung.

Die Schichtberechnung läuft verzögert und abbrechbar auf einem Hintergrundtask. Ein Revisionszähler verhindert die Übernahme veralteter Ergebnisse. Das optionale Prüfmodell verwendet über `ModelComputation.BuildExportMesh` dieselbe Bilddekodierung, Korrekturen, adaptive Detailauflösung und Validierung wie der Export. Ein Wechsel zurück zur schnellen Vorschau gibt die Referenzen auf das volle Modell frei. Änderungen verwerfen volle Geometrie und Höhenindex. `SectionViewport` zeichnet gecachte `StreamGeometry` mit EvenOdd-Füllregel, sodass Bohrungen und Hohlräume frei bleiben. Ansichtseinstellungen sind lokal und verändern Projektformat 2 nicht.

Der Lichtregler ändert nur die Deckkraft der vorhandenen Emissionstextur. Mesh, Texturbild und Projekt bleiben unverändert. Die Emission enthält einen konstanten Grundanteil und einen skalierbaren Bildanteil; dies bleibt eine visuelle Näherung.

## Erweiterungen in 1.3.0

Neue Fotos lesen die EXIF-Orientierung über WIC-Metadatenpfade für JPEG und TIFF. Alle acht Varianten werden mit orthogonalen Transformationen umgesetzt; unbekannte oder nicht unterstützte Metadaten lassen das Bild unverändert. Vorhandene Format-1-/Format-2-Projekte erhalten `AutoOrient=false`, damit sich ihr Koordinatensystem und Ausschnitt nicht ändern. Dieser Modus bleibt beim Bildreset und Export erhalten. Format 3 speichert zusätzlich `StraightenAngle` und die Ausrichtungsentscheidung. Ältere Apps lehnen die neue Version ab.

WPFs `TransformedBitmap` unterstützt orthogonale Transformationen. Die Feinrotation wird daher selbst als inverse bilineare Rasterabbildung berechnet, nach Ausschnitt, Spiegelung und 90°-Drehung, vor Tonwerten und Filtern. Der Zoom ist das Maximum der auf das Seitenverhältnis normierten Breiten-/Höhenprojektionen. Damit liegen alle Ausgabepunkte im Eingaberechteck. Alpha wird vor der Interpolation auf Weiß zusammengesetzt; der Abbruch wird je Bildzeile geprüft.

`ProcessedImage` liefert zusätzlich ein gefrorenes Vergleichsbitmap mit identischer Bildgeometrie vor den Tonwert-/Farb-/Filterkorrekturen. `PhotoComparison` zeichnet beide Bitmaps in dasselbe eingepasste Rechteck und clippt rechts an der verschiebbaren Linie. Maus- und Tastatureingaben verändern nur eine Ansichts-DependencyProperty. Die Vergleichsansicht wird bei der Original-Ausschnittwahl ausgeblendet.

Offizielle Grundlagen, geprüft am 04.11.2026:

- [WIC-Metadatenabfragen](https://learn.microsoft.com/en-us/windows/desktop/wic/-wic-codec-metadataquerylanguage)
- [Unterstützte Transformationen von TransformedBitmap](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.imaging.transformedbitmap.transform?view=windowsdesktop-10.0)

## Erweiterungen in 1.4.0

`PrintPlacement` verwendet sechs exakte Achsenrotationen mit Determinante +1 und optional eine zusätzliche Rotation um die interne Y-Achse. Es werden nur Vertexkoordinaten umgeordnet bzw. negiert; Materialstärken, Dreieckindizes, Foto-/Strukturkennzeichnung und UV-Koordinaten bleiben erhalten. `MeshGenerator` wendet die Drucklage als letzten Geometrieschritt an. So verwenden Vorschau, `CrossSectionIndex`, Druckvolumenprüfung und Export dieselbe Lage. Der Export dreht weiterhin von Y-aufwärts nach Z-aufwärts und verschiebt auf Z=0.

Die Ausrichtungssuche baut ein kanonisch aufrechtes Modell über dieselbe geprüfte Pipeline wie der Export. Aus dessen acht Hüllenecken lassen sich für rechtwinklige Rotationen exakte Hüllengrößen bestimmen. Eine passende aktuelle Auswahl hat Vorrang; sonst werden die zwölf Varianten in fester Reihenfolge geprüft. Die Prüfung erlaubt eine numerische Toleranz von 0,0001 mm. Sie trifft keine Aussage über Auflage, Stützen oder optimale Druckqualität.

Die Suche läuft im Hintergrund mit Abbruch, Parametersnapshot und Revisionsprüfung. Änderungen verhindern die Übernahme eines veralteten Ergebnisses. Bei erfolgreichem Wechsel werden Vorschau und Schichtcaches erneuert; die Kamera erhält eine zur Modellhülle passende Reset-Perspektive. Format 4 speichert Orientierung und Bettrotation ausdrücklich.

`python tools/check_placements.py` liest die 24 STL-/3MF-Ausgaben der Ausrichtungsfixtures unabhängig mit der Python-Standardbibliothek. Es prüft die bekannten Quadermaße, positives signiertes Volumen, Millimeter bei 3MF und Z=0. Dies ergänzt die C#-Roundtrips, ist aber keine Slicer- oder Drucksimulation.

## Erweiterungen in 1.5.0

`SphereSocket` rotiert ein sechspunktiges geschlossenes Radius-/Höhenprofil. Es erzeugt Fuß, Schulter, Einsteckring und axialen Durchgang als einen geschlossenen Körper. Die Kreisauflösung ist begrenzt; Innenbohrungen berücksichtigen die freien Polygonsehnen. Das radiale Spiel wird vom freien Kugelöffnungsradius abgezogen. Konservative Schranken für den Kugelrand bestimmen Mindest-Fußdurchmesser und Einstecktiefe. Der Ring behält mindestens 1,2 mm Material; die Kopf-/Plattenflächen werden nicht überlappend modelliert.

`ModelPart` unterscheidet Lithophanie, Sockel und Montage. Der Sockel ist fotounabhängig und besitzt eigene gespeicherte Drucklagen. Die Montage kopiert die beiden kanonisch aufrechten Netze und verschiebt den Kugelkragen auf die Oberseite der Sockelplatte. Sie bleibt eine Vorschau mit getrennten Körpern. `IsAssemblyPreview` verhindert ihren Export auch auf Core-Ebene; der Schutz bleibt bei der Koordinatenumrechnung erhalten. Schichtprüfung, Hochauflösungsprüfung und Raumsuche der App sind auf Einzelteile beschränkt.

Strukturgeometrie erhält im WPF-Rendernetz eigene Flächennormalen, damit Schultern und Durchführungen visuell nicht rundgeschliffen werden. Die geprüfte Exportgeometrie bleibt unverändert. Für Sockel ohne Fotodreiecke wird keine Emissionstextur angelegt; Bildbeleuchtungs-Bedienelemente sind in diesem Modus ausgeblendet.

`python tools/check_socket.py` liest Sockel und Aufnahme unabhängig als STL/3MF, kontrolliert die Referenzmaße, das analytische Polygonprofil-Volumen, freien Kabeldurchmesser und radiales Spiel. Die Prüfung nutzt ausschließlich die Python-Standardbibliothek.

## Erweiterungen in 1.6.0

`LightboxBack` erzeugt eine maßhaltige rechteckige Aufnahme und eine getrennte Rückwand. Das Fotopanel ist ein Raster innerhalb des Rahmens. Die umlaufenden, segmentgleichen Ringe verbinden Fotokante, Rahmenfront, Außenwand, hintere Auflage und senkrechte Innenwand ohne T-Verbindungen oder überlappende Teilquader. Diese Geometrie wird nur bei aktiver Rückwand verwendet; alte Kästen bleiben identisch.

Die Rückwand ist eine geschlossene Platte mit erhöhtem rechteckigem Einsteckring. Korrespondierende Ringe erhalten die exakten Ecken; ihre Innenmaße folgen Rahmen und Spiel. Der optionale axiale Kabeldurchgang berücksichtigt freie Polygonsehnen und endet oberhalb der Plattenstärke im offenen Innenraum des Rings. Einstecktiefe, Randstärke, minimale Innenfläche und Abstand zur Durchführung werden validiert. Das tatsächliche größte Kantensegment wird als Rückwand-Raster ausgegeben.

`ModelPart.BackPanel` wird hinten an das bestehende Enum angehängt, damit Format-5-Kennungen stabil bleiben. Die Drucklage der Rückwand ist getrennt vom Kasten gespeichert und startet mit `FaceUp`. Die Montage kopiert kanonische Einzelteile und verschiebt die Rückwand um `-BoxDepth-BackThickness` in Z. Ihr Einsteckrand sitzt innerhalb der hinteren Öffnung, ihre Platte liegt am Kastenrand auf. `MeshAssembly.Combine` wird auch vom Kugelsockel verwendet. Export, Prüfung und Schichtschnitte nutzen weiterhin dieselbe Teile-/Drucklagenpipeline. Die fünfte feste Kameraansicht zeigt die Rückseite.

`python tools/check_back.py` liest sechs STL-/3MF-Dateien unabhängig von C#: exakte Außenmaße und Innenöffnung, 0,25-mm-Spiel, analytische Volumina von Platte und Einsteckring, freie Kabelöffnung und Z=0. Keine Drucksimulation.

## Erweiterungen in 1.7.0

`BackMounting` erzeugt Rückwände mit bis zu acht zusätzlichen Bohrungen und der optionalen zentralen Kabelöffnung. Ohne zusätzliche Bohrungen bleibt die vorherige `LightboxBack`-Geometrie identisch. Mit Bohrungen wird die ebene Platte in disjunkte rechteckige Bohrungsbereiche und ein grobes Rechteckgitter mit den exakten Stufen des Einsteckrings zerlegt. Kreisränder besitzen 32 Segmente in der Vorschau und 32–128 im Export; zusätzliche Grenzknoten schließen die Nachbarflächen ohne T-Verbindungen an. Der tatsächliche größte Winkelschritt bestimmt die Sehnenkorrektur für den freien Mindestdurchmesser.

Hilfsgrenzen der Bohrungsbereiche werden innerhalb 0,01 mm zusammengeführt, um schmale Restzellen zu vermeiden. Außenhülle und Maße des Einsteckrings bleiben exakt. Materialzugabe von 1,02 mm hält auch nach diesem Zusammenführen mindestens 1 mm Abstand zum Einsteckrand. Freizonen benachbarter Bohrungen benötigen zusätzlich einen kleinen Abstand. Dies ist eine konservative Rechteckprüfung, keine allgemeine Mesh-Boolean-Operation. Die Bohrungskoordinaten sind unabhängig von der Drucklage; der Durchgang reicht durch die Plattenstärke. Geschlossene Netze mit positiver Orientierung und den erwarteten Lochkonturen werden geprüft.

`ModelSettings.Clone` kopiert die Bohrungsobjekte und ihre Liste tief. Damit verwenden asynchrone Vorschau, Export, Schichtprüfung und Raumsuche unveränderliche Arbeitsschnappschüsse. Format 7 speichert die Liste, Format 1–6 aktiviert keine Bohrungen. Der Editor erstellt anpassbare Zeilen mit X, Y und Durchmesser. WPF-Zahlenfehler verwerfen veraltete Vorschauen; gültige Korrektur oder Entfernen einer fehlerhaften Zeile hebt die Sperre wieder auf. Beide Mesh-Exportbefehle erfordern ein verfügbares Vorschaumodell und fehlerfreie Eingaben.

`python tools/check_mounting.py` liest vier exportierte STL-/3MF-Referenzen unabhängig von C#: Positionen und freie Mindestdurchmesser, offene axiale Wege, verbleibendes Material, signiertes Volumen, Hülle und konsistent orientierte geschlossene Kanten. Die bisherigen 34 Dateiprüfungen bleiben erhalten.

## Erweiterungen in 1.8.0

`PrintSet.Create` übernimmt genau zwei unabhängig geprüfte Y-up-Einzelteilnetze und kopiert sie in lokale Druckkoordinaten: X nach rechts, Y auf dem Bett, Z nach oben. Die minimale lokale Koordinate ist null. Bestehende Drucklagen/Bettrotationen stammen aus der normalen Geometriepipeline und bleiben unverändert. Zwei Anordnungen (Zeile/Spalte) werden mit 5 mm Rand und Zwischenraum geprüft; unter den passenden Varianten gewinnt die kleinere Hüllfläche. Der gesamte Satz wird auf dem Bett zentriert. Bauhöhe, Namen, Maße, konsistente Topologie, Volumen und Mindestabstände werden geprüft. Die Eingabemeshes werden nicht verändert.

`PrintSetExporter` schreibt einen OPC/ZIP-Container mit zwei Ressourcenobjekten, je einem Mesh und einem zugehörigen Build-Eintrag. Objektnamen bleiben UTF-8. Lokale Geometrie beginnt bei Z=0; eine reine XY-Verschiebung im Build-Eintrag platziert jedes Teil. Die Reihenfolge der zwölf Matrixwerte und die Zuordnung von Ressourcen zu Build-Einträgen folgen der [offiziellen 3MF-Core-Spezifikation, Abschnitte 3.3/3.4](https://github.com/3MFConsortium/spec_core/blob/master/3MF%20Core%20Specification.md). Kein Produktions- oder Slicerherstellerformat wird vorausgesetzt.

Der eigene Drucksatzleser unterstützt ausschließlich diesen Zwei-Mesh-/XY-Translationsumfang, keine allgemeinen 3MF-Projekte, Komponenten, Skalierungen oder Rotationen. Vor dem Ersetzen des Ziels werden beide gespeicherten Objekte erneut auf Namen, Positionen, Topologie, Volumen, Dreieckzahl und Maße geprüft. Temporäre Dateien haben eindeutige Namen und werden auch bei Abbruch beseitigt. Der bisherige Einzelmodell-Leser weist mehrere Objekte und Transformationen ausdrücklich ab, statt deren Indizes falsch zusammenzuführen.

`ModelComputation.BuildPrintSet` verwendet tiefe Kopien der Projekteinstellungen. Körper und Zubehör werden in der vorhandenen Exportpipeline mit ihren eigenen Drucklagen berechnet. Das ViewModel verwirft alte Drucksätze bei Geometrie-, Bild-, Drucklagen- und Profiländerungen und prüft den Revisionsstand nach dem Hintergrundtask. Vollaufgelöste Schichtprüfnetze und Drucksatznetze werden vor konkurrierenden Prüfberechnungen freigegeben. Fortschritt läuft über den UI-Synchronisationskontext, Berechnung und Export bleiben abbrechbar. Die Bettübersicht ist eine leichte WPF-2D-Darstellung von Hüllrechtecken; sie benötigt keine zweite vollständige 3D-Renderkopie.

Das Projektformat bleibt 7. Der vorbereitete Drucksatz und der Ansichtsmodus sind abgeleitete, nicht persistierte Daten. `python tools/check_printsets.py` kontrolliert vier Dateien unabhängig mit der Python-Standardbibliothek: OPC-Beziehungen, Millimeter, zwei benannte Ressourcen/Build-Einträge, Translationsmatrizen, Indizes, geschlossene orientierte Kanten, positives Volumen, Z=0, Abstände und Bettgrenzen. Referenzquader haben bekannte Maße, Volumina und Zeilen-/Spaltenpositionen.

## Erweiterungen in 1.9.0

`PrintAngles` ist ein unveränderlicher Werttyp für X/Y/Z in Grad mit Prüfung auf endliche Werte im Bereich −180 bis +180. `ModelSettings` speichert `Angles`, `SocketAngles` und `BackAngles`; `EffectiveAngles` wählt das aktive Einzelteil. Format 8 persistiert diese Werte. Projekte aus Formaten 1–7 erhalten explizit drei Nullsätze; bisherige Drucklagen und Fotoorientierungen bleiben erhalten. Die Nullwinkelpipeline verwendet exakt die bisherigen Koordinatenpermutationen.

Bei freien Winkeln bestimmt `PrintPlacement.Apply` die Mitte der kanonischen Modellhülle, transformiert sie mit der Grundlage und dreht jeden Punkt um diesen festen Drehpunkt. Interne Y-up-Koordinaten werden für die Drehung in Druckerachsen `(x,-z,y)` interpretiert. Die aktive Reihenfolge ist `Rz * Ry * Rx`; danach folgt die bisherige optionale Bettrotation. Sinus und Kosinus werden einmal pro Modell berechnet; die Schleife prüft alle 4096 Punkte auf Abbruch. Indizes, UVs und Foto-/Strukturflags bleiben gleich. Montagezweige verwenden weiterhin kanonisch aufrechte Geometrie. Export und Drucksatz übernehmen die bereits transformierten Einzelteile und normalisieren auf Z=0.

`PrintPlacement.Find` prüft zuerst die aktuelle freie Lage anhand aller tatsächlichen Vertices, ohne ein zweites vollständiges Mesh anzulegen. Die acht Hüllenecken würden bei schräg gedrehten runden Formen zu große Maße ergeben. Passt die aktuelle Lage nicht, folgen die zwölf bisherigen rechtwinkligen Varianten mit Nullwinkeln; für diese sind die acht Hüllenecken exakt. Ein Treffer übernimmt ausschließlich die Druckparameter des aktiven Teils. Hintergrundtask, tiefer Parametersnapshot, Revisionsprüfung, Abbruch und Cacheverwerfung bleiben erhalten.

Die drei WPF-Zahlenfelder binden auf aktive Winkelproxies und validieren auch Setter-Ausnahmen. Textfehler und Bereichsüberschreitungen stoppen die Eingabeverzögerung, entfernen veraltete Vorschauen und sperren beide Mesh-Exportbefehle. Der Rücksetzbefehl bleibt für Einzelteile auch bei fehlerhafter Eingabe verfügbar. Die Winkel ändern keine Bildbearbeitungsparameter.

`python tools/check_angles.py` prüft zwölf STL-/3MF-Dateien unabhängig über eine allgemeine 3×3-Matrix. Alle Vertices werden mit einer analytisch gedrehten Quaderreferenz verglichen: Drehpunkt, Achsreihenfolge, Bettrotation, Maße, Volumen, Z=0 und geschlossene orientierte Kanten. Die bisherigen 42 Dateiprüfungen bleiben enthalten. Die Erweiterung benötigt keine zusätzliche Bibliothek oder Netzwerkverbindung.

## Erweiterungen in 1.10.0

`MountingHole` ergänzt `SlotExtension` (0–40 mm, zusätzliche Länge zum Durchmesser) und `Horizontal`. Die X/Y-Koordinaten bleiben der Mittelpunkt. Format 9 persistiert beide Felder. Formate 1–8 erhalten explizit Zusatzlänge null und senkrechte Richtung; bestehende Drucklagen und Winkel bleiben erhalten. Der Nullpfad verwendet dieselbe Rundbohrungsgeometrie wie 1.9.0. Der tiefe Klon der Bohrungsliste übernimmt die zusätzlichen Wertfelder automatisch.

Ein Langloch ist die Minkowski-Summe aus einem Kreis mit Radius Durchmesser/2 und einer waagerechten oder senkrechten Strecke mit Länge `SlotExtension`. `BackMounting.Capsule` bildet die Schnittmenge gleichmäßig verteilter Tangenten-Halbebenen; deren Stützabstand ist Radius plus halbe Zusatzlänge mal Absolutwert der Achskomponente der Normalen. Das resultierende konvexe Polygon umschließt die mathematische Kapsel, damit die Polygonsehnen die freie Öffnung nicht verkleinern. Die vier Achsnormalen sind enthalten. Vorschau nutzt 32, Export 32–128 Normalen, abhängig von Radius und Raster.

Jede Polygonecke liefert einen Strahl zum äußeren rechteckigen Patchrand. Zusätzliche Patch-/Gitterknoten werden wie bisher übernommen. Die innere Kontur entsteht durch den ersten Schnitt jedes Strahls mit den Tangenten-Halbebenen. Da alle Polygonecken vorhanden sind, überspringen ihre Verbindungssegmente keine Ecke. Diese Kontur wird durch die gesamte Plattendicke gezogen und mit den vorhandenen Deckflächen verbunden. Einsteckring und Außenmaße bleiben unverändert. Rechteckige Freizonen werden um die halbe Zusatzlänge in der gewählten Richtung erweitert; Materialzugabe und Prüfung gegen Kabelöffnung und Nachbarn bleiben erhalten.

Die WPF-Zusatzlänge verwendet dieselbe Textfehlerbehandlung wie die bisherigen Bohrungskoordinaten. Der Richtungs-Checkboxwechsel zählt als Projektänderung, verwirft Prüf-/Drucksatzdaten und aktualisiert die Vorschau. Export, Schichtprüfung und Montage verwenden unverändert die gemeinsame Geometriepipeline.

`python tools/check_slots.py` prüft sechs STL-/3MF-Dateien unabhängig: achsparallele Strahlen durch die gesamte freie Kapsel, Material außerhalb der Kontur, Breite und Gesamtlänge, analytisches Platten-/Ringvolumen, geschlossene orientierte Kanten, Maße und Z=0. `--native` prüft zusätzlich die echte UI-Speicher-/Exportsitzung, Originalbildbytes und unveränderte übrige Projektparameter. Es werden keine neuen Bibliotheken oder Netzwerkverbindungen benötigt.

## Erweiterungen in 1.11.0

`MountingHole.SinkDepth` speichert die äußere Senktiefe. 0 deaktiviert die Senkung, aktive Werte beginnen bei 0,1 mm; zur Platteninnenseite bleiben mindestens 0,4 mm gerader Durchgang. Format 10 persistiert die Tiefe. Formate 1–9 erhalten explizit null, ohne ihre Langlochparameter oder freien Druckwinkel zu ändern. Der bisherige Nullpfad bleibt geometrisch identisch.

Die Senkung liegt bei lokalen Rückwand-Z=0 auf der äußeren Seite, gegenüber dem Einsteckrand. Ein gerader Ring reicht von `BackThickness` bis `SinkDepth`; eine Fase verbindet diesen mit der größeren Kontur bei Z=0. Die Außenkontur erhält Radius plus Senktiefe bei unveränderter Langloch-Mittellinie. Patchfreizonen enthalten den größeren Radius einschließlich Materialzugabe. Die zentrale Kabelöffnung bleibt ungesenkt.

Beide Konturen verwenden dieselben Tangenten-Normalen. Die äußeren Polygonecken liefern die Strahlen zum rechteckigen Patchrand. Für Zwischenknoten wird die Position auf einer Außenkante ermittelt und derselbe Kantenparameter auf deren innerer Gegenkante verwendet. Damit liegen die vier Punkte einer Fase in einer gemeinsamen Ebene mit 45°-Neigung; reine radiale Zuordnung würde die Langloch-Endfasen verdrehen. Die tangentiale Polygonannäherung umschließt den mathematischen Durchgang. Innenringe, Fasen und Deckflächen teilen ihre Vertexindizes. Abbruchprüfungen und gemeinsame Teile-/Drucklagenpipeline bleiben erhalten.

Für Radius r, Langloch-Zusatzlänge L und Senktiefe d beträgt das zusätzlich entfernte analytische Volumen `d² × (πr + L) + πd³/3`. Die Querschnittsöffnung bei Plattenhöhe z hat Radius `r + max(0,d-z)` und dieselbe Mittellinie. Diese unabhängigen Formeln prüfen Seite, Tiefe und Volumen; Dreiecksnormalen prüfen die Fasensteigung. `tools/check_sinks.py` liest sechs Referenzdateien unabhängig von C# und unterstützt mit `--native` den Vergleich der tatsächlichen UI-Speichersitzung. Es gibt keine neue Bibliotheks- oder Netzwerkabhängigkeit.

## Erweiterungen in 1.12.0

`CropSelection` berechnet normalisierte Rechtecke auf dem EXIF-ausgerichteten, unbeschnittenen Bild. Das Pixel-Seitenverhältnis wird in ein Verhältnis normalisierter Koordinaten umgerechnet. Mittiger Zuschnitt verwendet das größte passende Rechteck. Bei gebundenem Ziehen passt das Rechteck in die aufgezogene Fläche, bleibt am Startpunkt verankert und funktioniert mit beiden Vorzeichen jeder Achse. Zeigerkoordinaten werden auf das Bild begrenzt. Verhältniswerte und Bild-/Zeigermaße werden auf gültige endliche Werte geprüft.

`ImageEdits` speichert `CropAspectWidth`, `CropAspectHeight` und `CropAspectLock`. Format 11 persistiert diese Felder. Formate 1–10 erhalten 4:5 und ausgeschaltete Bindung, ohne bestehende Ausschnittkoordinaten oder Bildtransformationen zu ändern. Validierende ViewModel-Proxies sorgen für rote WPF-Zahlenfehler, verwerfen veraltete Modellvorschauen und verhindern ungültige Zuschneidebefehle. Verhältniswahl und Anwendung durchlaufen denselben Undo-/Redo-Pfad wie andere Bildkorrekturen.

`PhotoSurface` zeichnet die Auswahl im Koordinatensystem der Originalansicht. Beim Loslassen wird das berechnete Rechteck übernommen; Escape und verlorene Mauserfassung verwerfen es. Vor dem Freigeben der Erfassung wird das Ergebnis lokal gesichert. Neue Ausschnitte setzen wie bisher orthogonale Drehung, Feinrotation und Spiegeln zurück. Zurücksetzen des Ausschnitts behält die Verhältniswahl, vollständiger Bildreset stellt die Standardwerte wieder her. Bildbearbeitung und Export verwenden weiterhin die gespeicherten normalisierten Ausschnittwerte; Pixelgrenzen werden auf ganze Bildpunkte abgerundet. Modellabmessungen ändern sich nicht.

`tools/check_crops.py` dekodiert PNG mit der Python-Standardbibliothek einschließlich CRC und aller fünf PNG-Zeilenfilter. Es vergleicht alle ausgegebenen Pixel dreier Zuschnitte mit dem Originalbereich, unabhängig von WPF. `--native` liest die tatsächlich gespeicherte Projektkopie, prüft das Verhältnis der Mausauswahl und vergleicht Originalbytes sowie alle übrigen Projektfelder.

Der MSI-Packager unterstützt für Lebenszyklustests `--isolated-test`: eigene Produkt-, Upgrade- und sämtliche Komponenten-GUIDs, eigener Installations-/Startmenüname, eigener Desktoplink und eigene Registrykennung. Der normale Releasepfad behält seine bisherigen Kennungen. `Test-Installer.ps1 -IsolatedIdentity` verlangt die unveränderten Referenz-MSI-Dateien, prüft getrennte Kennungen und disjunkte Komponentenmengen vor der Installation und erhält bestehende Benutzerlinks bytegenau. Die Testpakete enthalten dieselben veröffentlichten Programmdateien, sind jedoch ausdrücklich keine Auslieferungspakete. Das ermöglicht Upgrade-/Deinstallationstests neben vorhandenen Benutzerinstallationen.

## Erweiterungen in 1.13.0

`ModelSettings.LockDimensions` speichert die Bindung. `SupportsPhotoSizing` aktiviert sie für Flat, Frame, Curved, Nightlight und Lightbox. Format 12 persistiert das Flag; Formate 1–11 übernehmen es als false. Das Flag allein verändert keine Geometrie, sondern steuert die Maßeingabe im ViewModel. Bestehende Bildausschnitte, Orientierungen, Wandstärken und Zubehörmaße bleiben erhalten.

`ModelSizing.Resize` berechnet beide Maße vor der Übernahme und prüft jeweils 5–1000 mm. Die Bindung erhält das aktuelle W/H-Verhältnis. Bei nicht unterstützten Formen bleibt sie inaktiv. `ModelWidth` und `ModelHeight` sind validierende WPF-Proxies; ein abgewiesener Versuch übernimmt auch das zweite Maß nicht. WPF zeigt bis zu drei Nachkommastellen, gespeichert wird die berechnete Genauigkeit.

`FitPhotoDimensions` verwendet einen Snapshot von Modell, Bildkorrekturen und eingefrorenem Arbeitsbild. Im Hintergrund wird das tatsächliche bearbeitete Pixelformat nach Zuschnitt und Rotation bestimmt. Höhe = bisherige Breite × Pixelhöhe / Pixelbreite. Der vollständige Ziel-Parametersatz wird vor der Übernahme validiert. CancellationToken und Revisionsprüfung verhindern veraltete Ergebnisse. Nach Abschluss, Abbruch oder Ablehnung wird eine gültige Vorschau wieder eingeplant, damit zuvor ausstehende Bildänderungen nicht verloren gehen. Die normale Geometriepipeline berechnet das Modell aus den neuen Maßen; es wird kein fertiges Mesh einschließlich Wandstärken skaliert.

`tools/check_dimensions.py` liest acht Referenz-STL-/3MF-Dateien unabhängig: bekannte Quadermaße und Volumina, unveränderte 3-mm-Wandstärke, endliche Koordinaten, geschlossene orientierte Kanten und Z=0. Der native Modus prüft die tatsächliche UI-Speicher-/Exportdatei, Originalbildbytes und unveränderte übrige Projektfelder.


## Erweiterungen in 1.14.0

`ModelPresetCatalog` speichert Schema 1 und Modellformat 12 mit maximal 100 Einträgen. Jeder Eintrag enthält Guid, Namen (1–80 Zeichen, keine Steuerzeichen, ohne äußere Leerzeichen, eindeutig ohne Beachtung der Groß-/Kleinschreibung) und eine tiefe Kopie von ModelSettings. Kein Foto, kein ImageEdits, kein Druckerprofil. Model.Validate prüft jeden Snapshot. Dateigröße maximal 2 MB; unbekannte Versionen werden abgewiesen. `ModelPresetStore.Save` validiert vor dem Schreiben und ersetzt die JSON-Datei atomar aus einer eindeutigen temporären Datei im selben Ordner. Bei Fehlern bleibt die bisherige Datei erhalten.

`StudioViewModel.Presets` lädt die getrennte Sammlung aus dem Instanz-Datenordner. Bei beschädigten oder zukünftigen Katalogen werden Schreibaktionen gesperrt und eine lokale Meldung angezeigt; kein stilles Ersetzen durch eine leere Sammlung. Änderungen der sichtbaren Liste erfolgen erst nach erfolgreichem Speichern. Tiefe Kopien verhindern Rückwirkungen zwischen aktuellem Projekt, angewendetem Modell und Vorlagen, auch für Bohrungslisten. Anwenden validiert vor dem Austausch, behält das aktuelle Speicherbudget und sämtliche übrigen Projektdaten, verwirft alte Vorschau-/Prüfdaten, plant Vorschau/Kamera neu und setzt Dirty. Bild-Undo/Redo bleibt erhalten. Die Vorlagenauswahl wird vom allgemeinen ComboBox-Projektänderungsereignis ausgeschlossen.

Projektformat 12 bleibt unverändert. Parameter aus eigenen Vorlagen werden wie normale Modelleinstellungen im Projekt gespeichert; die Projektdatei benötigt den Katalog nicht. Der Installer verwaltet keine Katalogdateien im Benutzer-Datenordner. `tools/check_presets.py` prüft unabhängig die 15 Katalogformen und ausgegebene STL/3MF-Geometrie; im nativen Modus vergleicht es echte gespeicherte Projektdateien, Originalbildbytes und Vorlage.


## Erweiterungen in 1.15.0

`HangingTabs` ergänzt zwei symmetrische rechteckige Laschen an Flat/Frame. Ihre Parameter sind HangingTabsEnabled, TabWidth, TabHeight, TabHoleDiameter und TabInset. Formate vor 13 aktivieren keine Laschen. Modellkataloge aus 1.14 (Schema 1 / ModelVersion 12) werden beim Laden in Modellformat 13 überführt, ebenfalls ohne Laschen; erst beim Speichern wird die Datei ersetzt. Unbekannte Katalogversionen bleiben geschützt.

Die Lochwand wird durch einen umschriebenen Kreispolygonzug gebildet, mit 32 Grundrichtungen in der Vorschau und 64 beim Export. Zusätzliche Winkel aus bestehenden Fotogitterpunkten und Rechteckecken erhalten den Anschluss. Der Radius berücksichtigt die größte Winkellücke, sodass der freie Lochdurchmesser nicht unterschritten wird. Die Durchmessergrenze von 20 mm sowie 2,5 mm Zugabe zu beiden Laschenmaßen sichern mindestens 1,2 mm Material auch am Polygonradius.

Vor der Grid-Erzeugung werden lediglich benötigte Anschluss-U-Werte ergänzt. Die Fotofläche behält ihre Maße und UV-Koordinaten; das Raster kann an den Anschlüssen feiner werden. Bestehende Front-/Rückseitenvertices an der oberen Panelkante werden wiederverwendet. Dort entfallen die bisherigen Abschlussquads; die Laschen schließen an dieselben Kanten an. Es entstehen keine überlappenden Einzelkörper und keine Mesh-Booleans. Neue Laschenvertices sind Struktur ohne Fototextur. Inaktive Laschen verwenden den bisherigen Rasterpfad unverändert. Bestehende Speicherbegrenzungen bleiben ein konservatives Arbeitsbudget; zusätzliche Anschlusslinien erhöhen den Aufwand etwas.

Laschen werden vor derselben Drucklagentransformation wie das Panel erzeugt. Mit Standfuß liegt der Anschluss bei StandHeight + Height, sonst bei Height. Laschenhöhe ist zusätzliche Geometrie. Kamerarahmen, Bettprüfung, freie Winkel, Schichtschnitt und Export sehen die vollständige Hülle. Der separate Parameter Height bleibt die Fotohöhe.

`tools/check_tabs.py` prüft unabhängig STL-/3MF-Dateien auf eine zusammenhängende Komponente, Eulerzahl −2 (zwei Durchgänge), gerichtete geschlossene Kanten, endliche Koordinaten, nicht degenerierte Dreiecke, Hüllmaße, freie Durchgangsstrahlen und mindestens 1,2 mm Material. Das Laschenvolumen wird getrennt vom Fotopanel gegen Rechteckfläche abzüglich gemessener Polygonlochflächen geprüft. Der native Modus vergleicht die gespeicherte Projektkopie und die echte Exportdatei, Originalbildbytes und unveränderte übrige Parameter.


Nahezu deckungsgleiche Anschlusslinien werden vor der Erzeugung in physischen Koordinaten zusammengeführt (0,0001 mm). Die unteren Laschenpunkte werden auf genau dieses gemeinsame Raster abgebildet. An der Anbindung wird die tatsächlich darstellbare Float-Höhe berücksichtigt, auch bei großen Bildhöhen. Identische neue Strukturvertices werden wiederverwendet; zusammengefallene Polygonkanten erzeugen keine Nullflächen. Eine Regression prüft positive und negative Verschiebungen von 0,0000001 bis 0,00001 mm sowie nahezu quadratische Laschen an 999 mm hohen Bildern. Der inaktive Rasterpfad bleibt unverändert.


## Erweiterungen in 1.16.0

`ProjectData.PrintLayout` enthält PrintSetOptions mit Margin (0–50 mm), Gap (1–50 mm) und PrintArrangement (Automatic, Row, Column). Projektformat 14 speichert die Werte. Beim Laden der Formate 1–13 werden die bisherigen Defaults 5/5/Automatic gesetzt. Die übrigen Parameter bleiben erhalten. ModelSettings bleibt unverändert; eigene Modellvorlagen verwenden weiterhin Modellformat 13 und ersetzen keine Drucksatzplanung.

`PrintSet.Create` und `ModelComputation.BuildPrintSet` akzeptieren einen optionalen Optionssatz. Bestehende Aufrufe ohne Parameter verwenden den bisherigen Pfad und dieselben Positionen. Optionen werden validiert und kopiert, bevor die Berechnung beginnt. Bei fester Richtung wird nur diese Anordnung geprüft; Automatic prüft beide und wählt wie zuvor die kleinere passende Hüllfläche. Zentrierung, Maßstab, Einzelteil-Drucklagen und lokale Z=0-Normalisierung bleiben erhalten. PrintSet.Validate prüft die tatsächlichen Netzmaße gegen Rand, Teileabstand und Bauhöhe. Die resultierende tatsächliche Richtung wird nur für Anzeige/Prüfung am Drucksatz geführt; 3MF benötigt weiterhin ausschließlich XY-Verschiebungen der beiden Objekte.

Die ViewModel-Proxies validieren vor dem Übernehmen und verwerfen alte vorbereitete Drucksätze bei Änderungen. Busy, CancellationToken und Revisionsprüfung schützen vor veralteten Ergebnissen. Ungültige Zahlen werden in WPF markiert und sperren Export/Berechnung. Die automatische Scrollposition wird nur beim Eintritt ohne Eingabefehler in die Drucksatzansicht zurückgesetzt, damit Änderungen oder Fehlermeldungen das fokussierte Eingabefeld nicht verstecken. Bettübersicht und Infotext zeigen die tatsächlich berechneten Abstände.

`tools/check_layouts.py` liest vier neue Zwei-Objekt-3MF-Dateien unabhängig. Bekannte Quadermaße und Volumina, OPC-Metadaten, getrennte Namen/Objekte, geschlossene orientierte Kanten, exakte zentrierte Positionen, Randabstände und Bauhöhen werden geprüft. Der native Modus vergleicht die tatsächlich gespeicherte Projektkopie samt Originalbild und alle beiden tatsächlich exportierten Objekte gegen die gewählte Spaltenanordnung.

Die Ansichtsaktion bleibt bei fehlerhaften Eingaben erreichbar: Sie öffnet die Drucksatzansicht zur Korrektur, ohne eine Berechnung zu starten. Neuberechnung und Export bleiben gesperrt. Ein WPF-Regressionstest wechselt mit fehlerhaftem Abstand in die Oberfläche und zurück und prüft danach die Sichtbarkeit des Eingabefelds.


## Erweiterungen in 1.17.0

`PrinterProfileStore` liest und schreibt weiterhin die bisherige JSON-Arraystruktur mit Name, Process, BedX, BedY, BuildZ, Nozzle, Layer und Material. Dateigröße maximal 512 KiB, maximal 100 vollständige validierte Einträge. Namen sind ohne Beachtung von Groß-/Kleinschreibung und äußeren Leerzeichen eindeutig; Steuerzeichen werden abgewiesen. Unbekannte JSON-Felder werden nicht still verworfen: Der Lesevorgang schlägt fehl und die Datei bleibt geschützt. Der StreamReader erhält die bisherige BOM-Erkennung. Fehlende Datei liefert zwei frische Startprofile; eine vorhandene leere Liste bleibt leer.

Speichern validiert vorab, kopiert die Werte und ersetzt die Datei aus einer eindeutigen temporären Datei im selben Ordner. Die sichtbare Bibliothek ändert sich erst nach erfolgreichem Schreiben. Fehler bewirken keine teilweise Listen-/Auswahländerung. Nach Lesefehlern sind Schreibaktionen gesperrt; erneutes Laden kann nach Dateikorrektur wieder entsperren, ohne Projektdaten zu überschreiben.

`StudioViewModel.Printers` trennt Bibliotheksauswahl und bearbeitbaren eingebetteten Projektdrucker. Die Sammlung ist nach außen schreibgeschützt; angewandte Drucker sind Kopien. UI-Auswahlmeldungen während interner Listenänderungen werden ignoriert. Die Referenz des Projektdruckers wird beobachtet: Beim Projektladen wird eine passende Bibliotheksauswahl nur angezeigt und nicht automatisch auf das Projekt kopiert. Gleiche Namen garantieren keine gleichen Maße. Eine ausdrückliche Übernahme oder echte neue Profilwahl kopiert gespeicherte Werte und verwirft abhängige Drucksatzdaten. Umbenennen ändert gespeicherte Geometriewerte nicht; Löschen verändert den Projektdrucker nicht.

Während Refresh-Bindungsaktualisierungen werden synthetische Text-/Slider-/Auswahl-/Checkboxänderungen nicht als Benutzerbearbeitung behandelt. Eine Revisionsnummer verwirft noch ausstehende Änderungsmeldungen vor dem Projektwechsel. Nach einem Druckerwechsel werden tatsächliche WPF-Eingabefehler erneut geprüft, sodass ein wiederhergestellter gültiger Drucker keine veraltete Fehlersperre behält; Fehler in anderen Feldern bleiben wirksam. Ein Regressionstest hält ein Zahlenfeld beim Projektladen fokussiert und prüft, dass der neue eingebettete Drucker unverändert und Dirty=false bleibt.

Projektformat 14 und Modellvorlagenformat 13 bleiben unverändert. `tools/check_profiles.py` prüft Profildateien mit der Python-Standardbibliothek; der native Modus vergleicht die tatsächlich gespeicherte Bibliothek und Projektkopie einschließlich Originalbild und unveränderter Modell-/Bild-/Drucksatzparameter.


## Erweiterungen in 1.18.0

PrintArrangement.Manual ergänzt BodyX/BodyY und AccessoryX/AccessoryY in PrintSetOptions. Werte müssen endlich und 0–2000 mm sein; die tatsächlichen Teilehüllen werden gegen Druckbett, Bauhöhe, Rand und Gap geprüft. Projektformat 15 speichert die vier Werte. Beim Laden älterer Formate werden sie auf 5/5 und 100/5 gesetzt; ein nicht unterstützter Manual-Wert in einem älteren Format wird Automatic. Vorhandene Format-14-Abstände und Row/Column bleiben erhalten. Modellvorlagenformat 13 und die bisherige Druckerprofil-JSON-Liste bleiben unverändert.

PrintSet.WithPosition/WithPositions erzeugen einen neuen Positionssatz mit denselben geprüften Mesh-Referenzen und Berichten. ValidatePlacement prüft Platzierung mit einer Toleranz von 0,001 mm, ohne alle Dreiecke erneut zu besuchen. Export.Validate prüft weiterhin die tatsächliche vollständige Geometrie und Berichtskonsistenz; anschließend wird die geschriebene 3MF-Datei erneut geprüft. Diese schnelle Platzierungsprüfung ersetzt keine Exportprüfung. Scale und Einzelteilorientierungen werden nicht verändert; die 3MF-Matrizen bleiben reine XY-Translationen auf Z=0.

Der ViewModel-Wechsel von Automatic/Row/Column zu Manual übernimmt beide tatsächlich vorbereiteten Positionen, damit das andere Teil nicht springt. Gültige Zahlenwerte und Mausbewegungen aktualisieren Cache, Dirty, Info und Export, ohne Changed/Geometrieneuberechnung. Andere Modell-/Bild-/Drucker-/Abstandsänderungen verwenden weiterhin die bisherige Invalidierung. WPF-Konvertierungsfehler sperren Export und verwerfen alte vorbereitete Daten.

PrintSetViewport verwendet dieselbe Betttransformation für Darstellung und Hit-Test. Ziehen benutzt MouseCapture, Vorschauposition, 0,1-mm-Rundung und Randbegrenzung; eine ungültige Vorschau bleibt rot und wird beim Loslassen verworfen. Escape oder verlorener Capture brechen ab. Pfeile verschieben das ausgewählte Teil um 1 mm, Shift um 10 mm. Busy sperrt Interaktion und Anordnungswahl. Die Darstellung bleibt eine Hüllrechteckübersicht; es gibt kein Silhouettennesting und keine Mischung mehrerer Projekte.

Program.ManualPrintLayout prüft Grenzen, Abstände, NaN, Struktur, immutable Positionsänderungen, wiederverwendete Geometrie, vollständige Exportprüfung nach Netzmutation, Projektmigration und Editor-/WPF-Bindungen. tools/check_manual.py liest vier tatsächliche 3MF-Dateien und das gespeicherte Projekt mit Python unabhängig; der native Modus vergleicht die bediente Projektkopie und den Export gegen Originalbild und übrige Einstellungen.


## Erweiterungen in 1.19.0

StudioViewModel.PrintLayoutHistory hält zwei auf 100 Schritte begrenzte Listen mit tief kopierten PrintSetOptions, zwei optionalen Vector2-Positionen und der tatsächlichen Anordnungsrichtung. Es werden weder Meshes noch Bildpuffer in der Historie gehalten. Nur akzeptierte Änderungen werden aufgezeichnet; eine neue Änderung verwirft Redo. Identische manuelle Bewegungen sind leer und setzen Dirty nicht. Direkte Positionsänderungen und der Wechsel aus einer vorbereiteten Automatik nehmen den bisherigen Zustand auf.

Undo/Redo validiert eine wiederverwendbare Platzierung vor Übernahme und verwendet vorhandene Mesh-Referenzen und Berichte. Ohne vorhandene Netze oder bekannte Zielpositionen wird ausschließlich die Planung restauriert und der Export bis zur Neuberechnung gesperrt. Geänderte Geometrie/Bild/Drucker räumt die Listen über Changed auf; PrintLayoutChanged schützt gezielte Planungsänderungen vor diesem Reset. Öffnen, neues Projekt und Fotoimport leeren ebenfalls beide Listen. Speichern und Neuberechnen erhalten sie. Die Historie ist nicht persistent; Projektformat 15, Modellvorlagenformat 13 und die Druckerprofilstruktur bleiben unverändert.

Die zwei eigenen Commands sind bei Busy/InputError oder außerhalb des Drucksatzeditors gesperrt. Strg+Alt+Z/Y adressiert die Planung; Strg+Z/Y bleibt unverändert beim Bildeditor. NumericValidation erhält bei der Korrektur eines Drucksatzfelds die Planhistorie, invalidiert aber weiterhin alte Prüfnetze. Die WPF-Regression prüft die echte Fehler-/Korrekturfolge und danach eine Rücknahme ohne veralteten Export.

Program.PrintLayoutHistory prüft zwei Schritte, Branching, No-op, abgewiesene Bewegungen, Automatikübernahme, Abstände, Neuberechnung, Busy/InputError, 100-Schritt-Grenze, Projektwechsel und getrennte Bildhistorie. check_history.py prüft die tatsächlich nach Undo und Redo geschriebenen 3MF-Dateien unabhängig einschließlich Positionsmatrizen, Maßen, Volumen und geschlossenen Kanten.


## Erweiterungen in 1.20.0

StlImport erkennt Binär-STL über die exakte Dateilänge einschließlich Facettenzahl, auch bei solid-Header. ASCII wird strukturiert und invariant eingelesen; Koordinaten müssen endlich sein. 64-MiB-/Dreiecklimits, exaktes Vertex-Welding, Topologiebericht und Rotation von Drucker-Z-up nach internem Y-up. Für geschlossene Netze werden kantenverbundene Komponenten bestimmt: gemischte/unsichere Wicklungen und reine Punktkontakte werden abgewiesen; nur eine übereinstimmend negative Wicklung wird umgedreht. Keine allgemeine Selbstüberschneidungsanalyse.

WorkshopParts erzeugt U-Profile und L-Winkel durch triangulierte Profilextrusion, Kreisringe mit konservativ freiem Innendurchmesser und Probediffusoren. BackMounting.GeneratePlate verwendet dieselbe geschlossene Patch-/Lochwandgeometrie ohne Rückwand-Einsteckrand. Optionales Graubild variiert die obere Fläche innerhalb der Min-/Max-Dicken; die freie XY-Bohrung bleibt erhalten. Montageplatten und Fotomodul verwenden bis zu acht Rundbohrungen.

IFormPlugin ist der tatsächliche .NET-10-Vertrag. Der separate PluginHost lädt die explizit gewählte DLL in eigenem AssemblyLoadContext, teilt nur Core und Framework, validiert die Ausgabe und schreibt STL. PluginRunner verwendet ArgumentList ohne Shell, begrenzte Logpuffer, eine eigene Arbeitsablage, erneutes Einlesen und Kill(entireProcessTree) bei Abbruch/60-Sekunden-Limit. Kein Sicherheitscontainer; Code darf nur vertrauenswürdig geladen werden. Zwei reale Beispielprojekte und DLLs werden gebaut und mitgeliefert. Quellen/Vertrag: FORMMODULE.md.

Das Prüffenster besitzt eigene unveränderliche Foto-/Parameter-Snapshots; es verändert beim Prüfen das Hauptprojekt nicht. Halterungsänderungen verwerfen alte Prüfdaten, Eingaben sind während Jobs gesperrt. Die lokale Bambu-Studio-Übergabe speichert zuerst das geprüfte Modell und startet ausschließlich den gewählten/installierten Slicer mit diesem Dateipfad; keine Shell, kein Upload-/Druckargument. Der unabhängige CLI-Prüfer verwendet eigene Arbeits-/Temporärordner und installierte Systemprofile, niemals Zugangsdaten oder Druckerkommunikation.

CalibrationKit schreibt 16 geprüfte STL-Proben, Sollwerte, Anleitung und CSV in ein atomar ersetztes ZIP. Zwischenablage liegt neben dem Ziel, wodurch Rechte-/Volumeprobleme beim Ersetzen vermieden werden. P1S-Preset wird ausdrücklich hinzugefügt und überschreibt keine bestehende gleichnamige Profilbibliothek. Projektformat 15 und Modellkatalogformat 13 bleiben unverändert.

Build veröffentlicht zusätzlich den PluginHost samt vorhandener Runtime und zwei eigene DLLs. Optionale Zertifikatsparameter signieren eigene Binärdateien vor dem MSI-Bau und anschließend das MSI, prüfen mit SignTool /pa /tw und Authenticode. Keine privaten Schlüssel/Passwörter als Parameter; nur Benutzerstore-Thumbprint und expliziter HTTPS-Zeitstempeldienst. Ohne Zertifikat bleibt der Release unsigniert. Mehrfach-Installerprüfungen verwenden getrennte Testidentitäten. Kleine Fensterhöhen klappen die dekorative Editorüberschrift ein; echte OS-DPI-Umschaltung ist von Layout-/Rastersimulation zu unterscheiden.
