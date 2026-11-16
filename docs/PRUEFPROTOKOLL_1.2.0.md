# Prüfprotokoll · LithoCreate Pro 1.2.0

Stand: 4. November 2026. Windows x64, .NET SDK 10.0.403 und WindowsDesktop 10.0.14.

## Automatisierte Integrationstests

75 Tests bestanden, 0 fehlgeschlagen. Letzter Lauf: 33,2 Sekunden, 148 MB Peak Working Set. Der Wert ist eine Messung dieses Testlaufs und keine Obergrenze für große Benutzermodelle. Vollständige Ausgabe: `artifacts/test-results-1.2.0.txt`; erzeugte Dateien: `artifacts/tests-1.2.0`.

Zusätzlich zu den bisherigen Topologie-, Datei-, Bild-, Projekt-, Wiederherstellungs- und Kameraprüfungen:

- Analytische Querschnittsfläche und Umfang einer Platte, einschließlich oberer und unterer Begrenzung sowie außerhalb des Modells.
- Bodenfläche mit Bohrung, hohle Zylinderwand und koplanarer Übergang zwischen Boden und Wand.
- Innerer Kugelhohlraum, Standfuß und dessen koplanare Schulter.
- Integration von 180 Querschnittsflächen je Grundform gegen das geprüfte Meshvolumen aller 15 Formen; maximale zugelassene Abweichung 2,5 % bei dieser diskreten Abtastung. Alle 2700 Schnitte besitzen reguläre geschlossene Konturen.
- Abbruch beim Indexaufbau und Schnitt sowie Abweisung nicht endlicher Höhen.
- Letzte Teilschicht, unverändertes Projekt bei Ansichtsänderungen, Exportauflösung und automatische Verwerfung alter Prüfdaten.
- Volumenvergleich des tatsächlich geschriebenen STL mit dem aus Prüfschichten integrierten Volumen.
- Lichtänderungen erhalten Geometrieobjekt und Texturbild; die Emissionsstärke folgt dem Regler.
- Tatsächlich gerendertes Schicht-PNG: Hohlraum bleibt frei, Materialring wird gefüllt.

Die Lösung lässt sich ohne Compilerwarnungen bauen. Der optionale Prüfmesh und der Modellexport teilen dieselbe Berechnungspipeline.

## Oberfläche

36 tatsächliche WPF-Layouts bei 1400 × 900 und 960 × 640: Startseite, Vorlagen, Foto, Einstellungen, Hilfe, Export, Standfuß, Lampenboden, Kugelöffnungen, untere und mittlere Schnitte, Exportauflösung und ungültige Zahleneingaben. Die Schnitte wurden visuell geprüft. Kleine Fenster verwenden scrollbare Panels. Das UI-Prüfprogramm setzt explizit einen WPF-Dispatcher-Synchronisierungskontext für seine Hintergrundaufgaben.

Ungültiger Text entfernt die Kontur und deaktiviert ihren PNG-Export; nach Korrektur wird wieder ein Schnitt berechnet. Die vollständigen Renderbilder stehen unter `artifacts/ui-layout-1.2.0`.

Ein separater nativer Bedienungstest verwendet `artifacts/native-1.2.0` als isolierten Datenordner. Die vorhandene ältere Anwendung mit ungespeichertem Projekt bleibt davon unberührt.

## Unabhängiger Slicer

`python tools/check_slicer.py --inputs artifacts/tests-1.2.0 --output artifacts/slicer-1.2.0` prüft den Import und erneuten 3MF-Export durch das lokal installierte Bambu Studio. Alle drei Fälle bestanden mit unveränderten Dreieckzahlen und Maßen innerhalb von 0,02 mm:

- Zylinderboden mit Bohrung: 40.976 Dreiecke.
- Flaches Foto mit Standfuß: 7444 Dreiecke.
- Kugel mit unterer und oberer Öffnung: 86.100 Dreiecke.

Die Prüfung erzeugt keinen Druckauftrag und keine Werkzeugwege.

## Release und Installer

`build.ps1` veröffentlicht die eigenständige Anwendung einschließlich Laufzeit, drei bearbeitbaren Beispielprojekten, Dokumentation, MSI, ZIP und SHA-256-Prüfsummen. Versionierte Dateien früherer Releases bleiben erhalten.

Der Installer-Lebenszyklustest wird mit folgendem Aufruf reproduziert; seine Windows-Installer-Protokolle liegen separat unter dem angegebenen Testordner:

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.2.0-win-x64.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.1.0-win-x64.msi -TestRoot ./artifacts/installer-upgrade-1.2.0 -Desktop
```

Er prüft Installation der Vorgängerversion, Upgrade, Laufzeit, App-Version, Startmenü, Desktopverknüpfung, bytegenaue Beispielprojekte, Rückstufungssperre, Deinstallation und unveränderte persönliche Projektdateien außerhalb und innerhalb des Programmordners. Eine vorhandene Benutzerinstallation oder Verknüpfung führt zum Abbruch des Tests statt zu deren Veränderung.

## Grenzen

Geometrische Schichtansicht ohne Extrusionsbahnen, Stützen oder Druckzeitschätzung. Keine physische Druckprobe, keine optische Materialkalibrierung, kein Intel-N95-Langzeittest und keine Windows-10-/DPI-Hardwarefreigabe. MSI und EXE sind nicht digital signiert. Die übrigen Grenzen stehen im Funktionsumfang.
