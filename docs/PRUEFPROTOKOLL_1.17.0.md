# Prüfprotokoll · LithoCreate Pro 1.17.0

Stand: 5. November 2026. Verfügbare Windows-x64-Entwicklungsumgebung mit .NET SDK 10.0.403 und WindowsDesktop 10.0.14. Keine physische Druckfreigabe.

## Build und Integration

Build ohne Warnungen oder Fehler. 227 Integrationstests bestanden, 0 fehlgeschlagen; letzter Lauf 40,9 Sekunden, Peak Working Set 211 MB. Ausgabe: `artifacts/test-results-1.17.0.txt`. Projektformat 14, Modellvorlagenformat 13 und elf erneut geladene Beispielprojekte bleiben erhalten.

Neun neue Prüfgruppen decken die bisherige JSON-Profilliste, Unicode/BOM, unabhängige Kopien, vollständige Parameter, Namenskonflikte, Wertebereiche, NaN, fehlende Einträge, 100-Profil-/512-KiB-Grenzen und Erhalt der letzten gültigen Datei ab. Die ViewModel-Prüfungen behandeln Speichern/Aktualisieren, Umbenennen ohne Übernahme anderer bearbeiteter Werte, bestätigtes Löschen ohne Projektänderung, beschädigte und unbekannte Daten, erneutes Laden nach Dateikorrektur, Schreibfehler ohne Teilübernahme, ausdrückliche Werteübernahme, gleichnamige eingebettete Drucker mit anderen Maßen, Drucksatzinvalidierung nach Profilwechsel und eine nach Neustart weiterhin leere Bibliothek.

## Unabhängige Dateien

`python tools/check_profiles.py` prüft die tatsächlich geschriebenen JSON-Profillisten unabhängig: bekannte FDM-/Resin-Maße, alle acht gespeicherten Felder, Material-/Verfahrenstexte, eindeutige Namen und erhaltene leere Liste. Ergebnis: `artifacts/profile-files-1.17.0.json`.

88 STL-/3MF-Dateien und drei zugeschnittene PNG-Dateien bestanden die bisherigen unabhängigen Prüfungen gegen `artifacts/tests-1.17.0`: `check_layouts.py` (4 Zwei-Objekt-3MF), `check_tabs.py` (8), `check_presets.py` (2 und 15 Katalogformen), `check_dimensions.py` (8), `check_crops.py` (3 PNGs), `check_sinks.py` (6), `check_slots.py` (6), `check_angles.py` (12), `check_printsets.py` (4 Zwei-Objekt-3MF), `check_mounting.py` (4), `check_back.py` (6), `check_socket.py` (4) und `check_placements.py` (24). Ergebnisse: `artifacts/*-files-1.17.0.json`.

## Oberfläche und Zustandskorrekturen

214 WPF-Layouts bei 1400 × 900 und 960 × 640 bestanden. Achtzehn neue Aufnahmen zeigen gespeicherte/aktualisierte/umbenannte Profile, Namenskonflikt, ungültige Zahl, ungültigen Wertebereich, Wiederherstellung, eingebettete abweichende Projektwerte und gelöschten Bibliothekseintrag. Bilder: `artifacts/ui-1.17.0`; Ausgabe: `artifacts/ui-results-1.17.0.txt`.

Die Oberflächenprüfung fand eine falsche Dirty-Markierung nach dem Laden eines Projekts bei weiterhin fokussiertem Zahlenfeld. Programmatische Bindungsaktualisierungen werden jetzt von echten Bearbeitungen unterschieden; ausstehende Ereignisse vor dem Projektwechsel werden verworfen. Der Regressionstest lädt ein gleichnamiges Profil mit 177 statt 185 mm Bettbreite bei fokussiertem Eingabefeld und prüft nach vollständiger UI-Aktualisierung unveränderte Projektwerte und Dirty=false. Anschließendes Löschen des Bibliothekseintrags verändert diese Werte und Dirty nicht.

Gespeicherte Werte können außerdem eine ungültige Druckereingabe direkt ersetzen. WPF-Eingabefehler werden danach erneut gegen die tatsächlichen Felder geprüft, damit keine veraltete Fehlersperre bleibt; Fehler anderer Felder bleiben wirksam. Der gesamte Integrations-/Oberflächenlauf wurde nach den Korrekturen erfolgreich wiederholt. Große Profilansicht sowie kleine Fehler-/Löschansicht wurden visuell kontrolliert.

## Native Bedienung

Die eigenständig veröffentlichte App wurde mit eigener Kopie des Formatbindungsbeispiels und getrenntem Datenordner unter `artifacts/native-1.17.0` bedient. „Mein N95 · PLA“ wurde mit 180 mm Bettbreite gespeichert, in „Mein N95 · Fein“ umbenannt und per Strg+S im Projekt gesichert. „Profildatei neu laden“ meldete drei Profile und ließ den Projektstatus unverändert. Der Profilname ist ein Testname, kein Hardwaretest auf einem Intel N95.

Nach der letzten Bedienkorrektur wurde die eigene Projektkopie mit der aktuellen veröffentlichten App unter `artifacts/native-1.17.0/final` erneut geöffnet. „abc“ als Bettbreite markierte das Feld rot und sperrte Speichern. „Gespeicherte Werte übernehmen“ stellte 180 mm wieder her, entfernte die Fehlersperre und aktivierte Speichern. Das Projekt wurde erneut per Strg+S gespeichert und blieb anschließend unverändert. Beide eigenen Sitzungen endeten mit Code 0; andere Benutzerfenster wurden nicht bedient.

`python tools/check_profiles.py --native --inputs artifacts/native-1.17.0/final --output artifacts/native-1.17.0/final/verification.json` bestand: drei vollständige Profile in der bisherigen JSON-Arraystruktur, genau ein umbenannter eigener Eintrag, FDM, 180 × 220 × 250 mm, Düse 0,4 mm, Schicht 0,12 mm und Materialtext „PLA, Weiß“. Projekt und Bibliothek stimmen überein. Gegenüber dem ursprünglichen Projekt änderten sich nur Profilname, Bettbreite und der zum Dateinamen passende Projektname; Originalbildbytes, Modell, Bildkorrekturen und Drucksatzplanung blieben gleich.

## Pakete und Installer

Eigenständige Windows-x64-Laufzeit, elf Format-14-Beispiele, aktuelle Dokumentation, MSI und portables ZIP mit SHA-256-Prüfsummen. Die persönliche Datei printers.json ist nicht Bestandteil der Pakete. `python tools/check_package.py --version 1.17.0` vergleicht jede ZIP-Datei bytegenau, prüft Laufzeit, Beispielparameter und Prüfsummen. Ergebnis: `artifacts/package-verification-1.17.0.json`.

Der Installer-Lebenszyklustest verwendet getrennte Installertest-Produkte mit eigenen Produkt-/Upgrade-/Komponentenkennungen sowie eigenen Verknüpfungen; unveränderte Release-MSI-Dateien dienen als Referenzen. Das vorhandene Benutzerprodukt wird nicht aktualisiert.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.17.0-installertest.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.16.0-installertest.msi -TestRoot ./artifacts/installer-upgrade-1.17.0 -Desktop -IsolatedIdentity -ReferenceMsiPath ./artifacts/LithoCreate-Pro-1.17.0-win-x64.msi -PreviousReferenceMsiPath ./artifacts/LithoCreate-Pro-1.16.0-win-x64.msi
```

Prüfumfang: Upgrade 1.16 → 1.17, bytegenaue App-/Core-Dateien und elf Beispiele, integrierte Laufzeit, Test-Startmenü-/Desktopverknüpfungen, Rückstufungssperre, Deinstallation, Erhalt persönlicher Projektdateien und vorhandener Benutzerverknüpfungen. Ausgabe: `artifacts/installer-results-1.17.0.txt`; Logs: `artifacts/installer-upgrade-1.17.0`. „installertest“-MSIs sind keine Auslieferungspakete.

## Grenzen

Profile speichern beschreibende Druckerdaten; keine Slicerparameter, G-Code, Werkzeugwege oder optische Materialkalibrierung. Ungültige oder unbekannte Bibliotheken müssen korrigiert, zurückgesichert oder mit einer passenden App gelesen werden; sie werden nicht automatisch zusammengeführt oder repariert. Projekte bleiben mit Version 1.16 kompatibel. Physische Druck-/Passungstests, unabhängige Slicerfreigaben, Intel-N95-/Windows-10-/OS-DPI-Hardwaretests und Codesignatur stehen weiter aus.
