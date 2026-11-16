# Prüfprotokoll · LithoCreate Pro 1.1.0

## Build und funktionale Prüfungen

Der Release-Build der vollständigen Visual-Studio-Lösung mit .NET SDK 10.0.403 hat **0 Fehler und 0 Warnungen** ergeben. Die funktionale Testsuite besteht mit **50 bestandenen Tests, 0 Fehlern**. Der gemessene Lauf dauerte 18,7 Sekunden bei etwa 147 MB Peak Working Set im Testprozess. Dies ist keine Obergrenze des interaktiven Programms oder eines maximal aufgelösten Exportes.

Das maschinenlesbare bzw. vollständige Protokoll liegt im Build-Artefakt `artifacts/test-results-1.1.0.txt`. Die tatsächlich erzeugten STL-/3MF-/Projektdateien liegen unter `artifacts/tests-1.1.0`.

## Neue Geometrieprüfungen

- Alle 15 bisherigen Grundformen bestehen weiterhin die Topologieprüfung und den STL-/3MF-Datei-Roundtrip.
- Zylinder und Polygonlampen mit geschlossenem Boden und mit zentraler Bodenöffnung: Kanteninzidenz, Orientierung, Euler-Charakteristik, unveränderte Außenhöhe und tatsächliche Durchgängigkeit der Öffnung werden geprüft. Vertikale geometrische Strahlen prüfen, ob die Bodenfläche vorhanden und die Bohrung frei ist.
- Integrierte Standfüße für flache Bilder, Rahmen, gebogene Bilder und Nachtlicht: keine T-Verbindungen oder degenerierten Schulterflächen, korrekt addierte Standhöhe und Dateiprüfung. Bei den geraden Platten wird das tatsächliche Volumen mit dem analytischen Sollvolumen verglichen; eine Überlagerung einzelner Körper würde diese Prüfung nicht erfüllen.
- Kugel und Mond mit nur unterer, nur oberer und mit beiden Öffnungen: die topologische Verbindung von Innen- und Außenfläche, der freie axiale Kanal und die ebene Kragenauflage werden geprüft. Die Kreisöffnungen werden an ihren Dreieckssehnen statt nur an den Eckradien gemessen.
- Eine zusätzliche Prüfung umfasst weiße, also dünne Fotowände am Kugelkragen sowie Kragenhöhe 0 und 4 mm. Der verstärkte ebene Übergang muss mindestens die maximale Wandstärke aufweisen und weiterhin geschlossen exportierbar sein.
- Ungültige Boden-, Öffnungs-, Stand- und Polygondimensionen werden abgewiesen. Das Wechseln auf eine andere Form wird durch dort nicht verwendete Zusatzoptionen nicht verhindert.

## Projekt-, Vorschau- und Stabilitätsprüfungen

Der Projekt-Roundtrip erhält sämtliche neuen Optionen und die Originalbildbytes. Ein manuell aufgebautes Version-1-Projekt wird mit unveränderten bisherigen Einstellungen nach Version 2 übernommen. Ungültige Bildkorrekturen, Zuschnitte, Druckermaße, Formkennungen, unbekannte Versionen und fehlende Metadaten werden abgewiesen.

Die Instanzsperre wird aus einem zweiten Thread geprüft: derselbe Datenordner ist gesperrt, ein anderer Datenordner bleibt nutzbar, und nach Freigabe kann erneut geöffnet werden. Ein ViewModel-Test erzeugt eine gültige Vorschau, setzt absichtlich eine ungültige Zahl, prüft das Entfernen des alten Meshes und die Sperre des PNG-Exports und stellt danach eine korrekte Vorschau wieder her. Ein neues Projekt darf das alte Mesh ebenfalls nicht anzeigen.

Die Kamera wird bei drei unterschiedlichen Seitenverhältnissen in Reset-, Vorder-, Seiten-, Drauf- und Unteransicht geprüft. Alle acht Modellhüllenecken müssen im tatsächlichen perspektivischen Sichtvolumen liegen. Die 2048-MB-Speichereinstellung darf die Detailauflösung nicht durch einen Ganzzahlüberlauf reduzieren.

Weiterhin bestehen die bisherigen Bildformat-, Bildbearbeitungs-, Abbruch-, Dateierhalt-, Budget-, Maß- und Materialstärketests.

## Visuelle Kontrolle

20 tatsächliche WPF-Layouts wurden bei 1400 × 900 und 960 × 640 erzeugt, einschließlich Fotoeditor, Standfuß, Lampenboden sowie Kugelöffnung von unten. Die neuen Bedienelemente und Strukturmaterialien wurden visuell kontrolliert. Die Kameraeinpassung wurde dabei verbessert; das vollständige Modell bleibt auch bei einem breiten bzw. kleinen Vorschaufenster sichtbar. Artefakte: `artifacts/ui-layout-1.1.0`.

Zusätzlich wurde die tatsächlich veröffentlichte eigenständige EXE mit dem Kugellampen-Beispiel in einem isolierten Datenordner geöffnet. Versionsanzeige, neue Eingabefelder und fertige Vorschau wurden direkt im Windows-Fenster geprüft. Die Instanzsperre war während des Laufs tatsächlich gehalten; das Programm schloss regulär mit Exitcode 0. Ein im nativen Test aufgefallener Tooltip-Kontrastfehler wurde durch eine eigene dunkle Tooltipvorlage mit Zeilenumbruch behoben.

Die Betriebssystem-DPI-Einstellung wurde nicht verändert. Separate praktische Prüfungen bei 125 %, 150 % und 200 % sowie auf einem Intel N95 stehen weiterhin aus.

## Unabhängige Slicerprüfung

Das Hilfsskript `tools/check_slicer.py` verwendet das auf diesem Rechner vorhandene Bambu Studio. Es liest drei neu erzeugte Modelle ein, lässt Bambu jeweils eine neue 3MF-Datei schreiben und liest die darin enthaltenen Meshdateien mit einer unabhängigen Python-XML-Auswertung aus. Dreieckzahl und Außenmaße müssen unverändert bleiben (Maßtoleranz 0,02 mm).

Geprüft werden `Base-Cylinder-6.stl`, `Stand-Flat.stl` und `Opening-Sphere-30-4.3mf`. Ergebnisse und die von Bambu ausgegebenen Dateien stehen unter `artifacts/slicer-1.1.0`. Alle drei unabhängigen Bambu-Roundtrips bestanden: 40.976 Dreiecke für den Zylinderboden, 7444 für den Standfuß und 86.100 für die Kugel mit zwei Öffnungen, jeweils mit unveränderten Maßen innerhalb der Toleranz. Dies ergänzt die GUI-Importtests der Version 1.0.0; ein physischer Ausdruck wurde nicht angefertigt und kein Druckauftrag versendet.

## Veröffentlichung und Installation

`build.ps1` erzeugt die eigenständige Anwendung, drei erneut eingelesene Beispielprojekte, MSI, ZIP und Prüfsummen mit der Versionsnummer der App. Die zugehörigen Installerprüfungen werden mit `tools/Test-Installer.ps1` ausgeführt und unter `artifacts/installer-upgrade-1.1.0` protokolliert. Der Test prüft Installation, optionales Desktopicon, Versionswechsel von 1.0.0, Entfernen der alten Produktregistrierung, Abweisen eines Downgrades, Deinstallation und den unveränderten Hash einer persönlichen Projektdatei.

Der vollständige Upgrade-Lebenszyklus wurde erfolgreich ausgeführt: 410 Dateien installiert, alte Produktregistrierung entfernt, Rückstufung mit Windows-Installer-Code 1603 abgewiesen, Desktop- und Startmenüverknüpfung geprüft und beim Deinstallieren entfernt. Persönliche Projektdateien außerhalb und innerhalb des Programmordners blieben nachweislich mit unverändertem SHA-256-Hash erhalten.

Der Test verändert keine bereits vorhandene Benutzerinstallation: Er bricht vorab ab, wenn eine der geprüften Produktkennungen oder eine eigene Verknüpfung schon vorhanden ist. Die Anwendung und das Paket sind nicht digital signiert.

## Verbleibende praktische Grenzen

Keine physischen Druck- oder Materialtests, keine kalibrierte Lichtsimulation, keine allgemeine Selbstüberschneidungsanalyse, kein separater Windows-10- oder N95-Langzeittest und kein vollständiger Slicer-Lauf jeder möglichen Parameterkombination. Die geometrischen Öffnungen ersetzen keine praktische Prüfung von Resin-Ablaufweg, Druckausrichtung oder Leuchtmittelpassung. Weitere offene Produktfunktionen stehen in [FUNKTIONSUMFANG.md](FUNKTIONSUMFANG.md).
