# Prüfprotokoll · LithoCreate Pro 1.20.0

Stand: 6. November 2026. Nutzerziel Windows 11 / Intel N95 / Bambu Lab P1S / PETG. Der Prozessor wurde lokal über die Registry bestätigt; OS-Build 26200. Physische Druck- und Wärmeversuche sind nicht erfolgt.

## Build und Integration

Der vollständige finale Lauf bestand mit 255 Tests, 0 Fehlern, 55,7 s und 182 MiB Peak Working Set. Build ohne Warnungen/Fehler. Neue Prüfgruppen behandeln ASCII-/Binär-STL, solid-Binärheader, Maße/Achsen, Gesamtwicklung, gemischte Teilkörperwicklungen, offene Netze, NaN, beschädigte Dateien, Limits/Abbruch, sieben Halterungen, Bohrungs-/Fotoplatten, Licht-/Passungspaket, P1S-Profil ohne Überschreiben und beide tatsächlich ausgeführten Module. Das Bogenmodul wurde zusätzlich mit üblichen Projektmaßen 120×88 mm bei angefordertem 0,3-mm-Raster ausgeführt; es bleibt durch sein eigenes adaptives Raster unter dem Hostlimit, ohne die Projektauflösung zu ändern. Ausgaben: artifacts/test-results-1.20.0.txt und artifacts/build-final-1.20.0.txt. Einzelmodellexporte verwenden jetzt eindeutige temporäre Dateien, damit mehrere Prüfplatzfenster keine gemeinsame .tmp-Datei benutzen.

## Intel-N95-Dauertest

artifacts/endurance-n95/result.json: PASS, 512 Zyklen, 7.680 Modelle, 1801,69 Sekunden. Alle 15 Formen wurden wiederholt erzeugt, topologisch geprüft, geschnitten und als 3MF geschrieben/erneut gelesen. Peak Working Set 154,82 MiB. Private Bytes erster/letzter Zyklus: 48,14 / 64,42 MiB. Kein mehrtägiger GUI-/Treiber- oder Leakfreiheitsnachweis. Parallel fanden zeitweise Entwicklungs-/Buildarbeiten statt.

## Unabhängige Dateien und Slicer

tools/check_workshop.py prüft 36 zusätzliche STL-/3MF-Dateien unabhängig: sieben STL-/3MF-Halterungspaare, Montage-/Fotoplatten, Import-Roundtrip, zwei Modulausgaben und 16 Probedruckteile. Geschlossene orientierte Kanten, positive Volumina, Maße, bekannte LED-/Winkel-/Diffusorvolumina und Z=0. Die bisherigen 94 Dateien und drei zugeschnittenen PNGs bestanden ebenfalls erneut. Insgesamt 130 STL-/3MF-Prüfdateien plus drei PNGs.

Bambu Studio 2.8.2.61 ist lokal installiert. tools/check_bambu.py las alle 15 Grundformen ein und exportierte native Bambu-3MFs; Dreieckzahl und Maße wurden unabhängig mit den STL-Eingängen verglichen. Mit aufgelösten installierten P1S-0,4-/Generic-PETG-/0,12-mm-Profilen wurden echte Werkzeugwege berechnet: result.json return_code=0 und plate_1.gcode. Für das kleine flache Testmotiv: prognostizierte Gesamtzeit 1 h 7 min 44 s, Filament laut Slicer 4,46 g. Das sind Slicerprognosen, keine Messwerte des Nutzerdruckers. Keine Druck-/Uploadaktion.

Die erste CLI-Slicing-Ausgabe schlug in der eingeschränkten Testumgebung fehl. Für den erfolgreichen Lauf wurde die temporäre Dateiablage in den eigenen Arbeitsordner verlegt. Der wiederholbare Test verwendet eine eigene temporäre Ablage innerhalb des Arbeitsordners. Der kombinierte Export als geslicetes Bambu-3MF scheiterte; der erfolgreiche Test wertet deshalb die tatsächlich geschriebene G-Code-Datei samt erfolgreichem Slicerbericht aus. Die App übergibt geprüfte Modelle an die reguläre Bambu-Studio-Oberfläche; sie liefert keine eigene Bambu-Druckprojekt-Implementierung. Berichte: artifacts/bambu-1.20.0/verification.json, verification-imports.json, result.json und toolpath-evidence.txt.

## Oberfläche und Pakete

Die bisherigen 244 WPF-Layouts und 39 zusätzliche Raster-/Layoutsimulationen auf einem 1920×1080-Arbeitsfeld bei 125/150/200 % bestanden. Eine zusätzliche kompakte Editordarstellung ab kleinen Fensterhöhen verhindert eine zu kleine 3D-Vorschau. Der Berechnungsabbruch im Prüfplatz ist außerhalb des scrollenden Optionsbereichs sichtbar.

Zusätzlich wurde die echte Windows-Skalierung mit Nutzerfreigabe von ursprünglich 125 % auf 150 % und zurück auf 125 % umgestellt. Die laufende veröffentlichte App zeigte den Editor einschließlich Vorschau und Navigation ohne Abschneiden der Hauptbedienung; Wechsel zur Vorschauseite funktionierte. Die Rückstellung auf 125 % (empfohlen), 1920×1080 (empfohlen) wurde im Windows-Dialog visuell bestätigt. Das reguläre Auswahlmenü bot nur 100/125/150/175 % an; 200 % wurde deshalb ausschließlich simuliert. Keine benutzerdefinierte Skalierung, Abmeldung oder Mehrmonitorprüfung. Bericht artifacts/os-dpi-1.20.0/state.json. Die eigene App-Sitzung endete mit Code 0.

Die veröffentlichte App wurde mit eigener Projektkopie und eigenem Datenordner unter artifacts/native-1.20.0 nativ bedient: neuer Prüfplatz geöffnet, Dateiöffnungsdialog geöffnet/abgebrochen und „Aktuelles Projektmodell prüfen“ ausgeführt. Ergebnis 120 × 4 × 88 mm, 473.176 Dreiecke, geschlossene konsistente Kanten und aktivierter Export. Die Befüllung des Dateidialogs konnte wegen inkonsistenter Computer-Use-Cachedaten nicht nativ abgeschlossen werden („element 134 is not available in cached app state“); tatsächliches STL-Einlesen samt Darstellung wurde stattdessen in den WPF-/DPI-Prüfungen ausgeführt. Diese Einschränkung wird nicht als bestandener nativer Dateidialog-Endtest ausgegeben.

„P1S / PETG hinzufügen“ wurde nativ ausgeführt und die eigene Projektkopie per Strg+S gespeichert. Die unabhängige Nachprüfung bestätigt drei Profile, P1S 256³ / 0,4 / 0,12 und gleiche Projekt-/Bibliotheksdaten. Originalbild, Modell, Bildkorrekturen und Drucksatzplanung blieben unverändert. Bericht artifacts/native-1.20.0/verification.json. Die eigene Sitzung endete mit Code 0.

Finale Paketprüfung: tools/check_package.py --version 1.20.0; bytegenauer ZIP-Vergleich, zwölf Format-15-Beispiele, PluginHost, zwei eigene Module und PETG-Probedruckpaket. Persönliche Druckerdateien werden nicht gebündelt. Bericht artifacts/package-verification-1.20.0.json.

Wiederholte Installerprüfung über tools/Test-InstallerCycles.ps1: drei isolierte Zyklen 1.19 → 1.20 mit Rückstufungssperre und Deinstallation. Eigene Produkt-/Upgrade-/Komponentenkennungen, Registrypfade und Verknüpfungen; normale Release-MSIs dienen als Referenzen. Die Hashprüfung umfasst zusätzlich Host, beide Modul-DLLs und das Probedruckpaket. Ergebnisse artifacts/installer-cycles-final-1.20.0/results.json und Cycle-*.txt. Benutzerinstallationen, ihre Verknüpfungen und persönliche Projektdateien bleiben geschützt; installertest-MSIs werden nicht ausgeliefert.

## Externe Voraussetzungen

Sign-Release.ps1 -PreflightOnly fand kein Codesignaturzertifikat im aktuellen Benutzerstore und kein SignTool. Es wurden keine Schlüssel exportiert, Ersatzidentitäten erstellt oder Signaturen behauptet. Physische PETG-/Resin-/Passungs-/Belastungs-/Wärmeprüfungen und konkrete Fassungsmaße stehen aus. Die native DPI-Prüfung deckt 125/150 % auf dem vorhandenen Bildschirm ab; andere Monitore und echte 200 % bleiben ungeprüft. Windows 10 wurde nicht getestet; der Nutzer arbeitet auf Windows 11.

## Primärquellen

- [P1S-Datenblatt von Bambu Lab](https://store.bblcdn.com/0de21072f69f4ac9ad9639adb36d4b20.pdf).
- [Offizielle Bambu-Studio-CLI](https://github.com/bambulab/BambuStudio/wiki/Command-Line-Usage).
- [Microsoft SignTool](https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool) und [Zeitstempel](https://learn.microsoft.com/en-us/windows/win32/seccrypto/time-stamping-authenticode-signatures).
