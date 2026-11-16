# Prüfprotokoll · LithoCreate Pro 1.19.0

Stand: 5. November 2026. Windows x64, .NET SDK 10.0.403 / WindowsDesktop 10.0.14. Keine physische Druckfreigabe.

## Build und Integration

Build ohne Warnungen oder Fehler. 241 Integrationstests bestanden, 0 fehlgeschlagen; 36,1 Sekunden, Peak Working Set 206 MB. Ausgabe: artifacts/test-results-1.19.0.txt. Sechs neue Prüfgruppen behandeln mehrstufiges Undo/Redo, gleiche Mesh-Referenzen, Branching und No-op, abgewiesene Bewegungen, Automatikpositionen, Abstände ohne veralteten Export, Busy/InputError, 100-Schritt-Grenze, Modelländerung, getrennte Bildhistorie und Projektwechsel. Projektformat 15 und zwölf Beispiele bleiben erhalten.

## Oberfläche

244 WPF-Layouts bei 1400 × 900 und 960 × 640 bestanden. Vierzehn neue Aufnahmen zeigen leere Historie, zwei Schritte, Undo, Redo, Projektwechsel, fehlerhafte Eingabe und anschließende Korrektur. Die große Undo-Ansicht und kleine Korrekturansicht wurden visuell kontrolliert. Bilder: artifacts/ui-1.19.0; Ausgabe: artifacts/ui-results-1.19.0.txt.

Die WPF-Prüfung verwendet die tatsächlichen Button-Commands und Zahlenbindungen. Ein ungültiger Positionswert sperrt Rücknahme/Wiederholen und Export. Nach der Korrektur bleibt die Historie erhalten; Undo restauriert den vorherigen Wert und verlangt wegen verworfener Prüfnetze eine Neuberechnung. Der vollständige vorhandene Oberflächenlauf einschließlich Profil- und Vorlagenverwaltung bleibt erfolgreich.

## Unabhängige Dateien

94 STL-/3MF-Dateien und drei zugeschnittene PNG-Dateien bestanden unabhängige Prüfungen gegen artifacts/tests-1.19.0. Zwei zusätzliche History-Undo/Redo-Dateien enthalten den Leuchtkasten auf 50/45 mm und die Rückwand auf 120/100 beziehungsweise 130/100 mm. Die unabhängig gelesenen Maße, Dreieckzahlen, Volumina und geschlossenen orientierten Kanten bleiben gleich. Rand 10 mm, Teileabstand 8 mm, Bettgrenzen, Millimeter und reine XY-Translationen auf Z=0 werden geprüft. Bericht: artifacts/history-files-1.19.0.json.

Bisherige Prüfer: check_manual.py (4), check_layouts.py (4), check_tabs.py (8), check_presets.py (2 und 15 Katalogformen), check_dimensions.py (8), check_crops.py (3 PNG), check_sinks.py (6), check_slots.py (6), check_angles.py (12), check_printsets.py (4), check_mounting.py (4), check_back.py (6), check_socket.py (4) und check_placements.py (24). check_profiles.py prüft weiterhin gespeicherte Druckerprofile und die erhaltene leere Liste. Ergebnisse: artifacts/*-files-1.19.0.json.

## Native Bedienung

Die eigenständig veröffentlichte App wurde mit eigener Beispielkopie und getrenntem Datenordner unter artifacts/native-1.19.0 bedient. Ziehen änderte die Körperposition von 20/25 auf 50,6/45,7 mm. Strg+Alt+Z stellte 20/25 mm und den Zähler 0 zurück / 1 wiederholen wieder her. Strg+Alt+Y stellte 50,6/45,7 mm und 1 zurück / 0 wiederholen wieder her. Die Rückwand blieb auf 120/100 mm. Strg+S speicherte die Projektkopie und erhielt die Historie.

Der 3MF-Export nach Redo meldete die erfolgreiche erneute Prüfung. tools/check_manual.py --native bestand unabhängig gegen die tatsächlich gespeicherte Projektkopie und 3MF: genaue Positionen, Leuchtkasten 48 × 40 × 16 mm / 68.920 Dreiecke, Rückwand 40 × 48 × 5 mm / 3.344 Dreiecke, unveränderte Bildbytes und übrige Modell-/Drucker-/Bildkorrekturen. Geändert wurden nur die Körperposition und der zum Dateinamen passende Projektname. Bericht: artifacts/native-1.19.0/verification.json. Die eigene Testsitzung endete mit Code 0.

## Pakete und Installer

Eigenständige Windows-x64-Laufzeit, zwölf Format-15-Beispiele, Dokumentation, MSI und portables ZIP mit SHA-256. Persönliche Druckerprofile werden nicht gebündelt. tools/check_package.py vergleicht alle ZIP-Dateien bytegenau, prüft Beispielparameter und Prüfsummen. Bericht: artifacts/package-verification-1.19.0.json.

Der Installer-Lebenszyklustest verwendet isolierte Testprodukte mit eigenen Produkt-/Upgrade-/Komponentenkennungen, Registry-Pfaden und Verknüpfungen. Unveränderte Release-MSIs dienen als Referenzen. Die vorhandene Benutzerinstallation wird nicht aktualisiert.

```powershell
./tools/Test-Installer.ps1 -MsiPath ./artifacts/LithoCreate-Pro-1.19.0-installertest.msi -PreviousMsiPath ./artifacts/LithoCreate-Pro-1.18.0-installertest.msi -TestRoot ./artifacts/installer-upgrade-1.19.0 -Desktop -IsolatedIdentity -ReferenceMsiPath ./artifacts/LithoCreate-Pro-1.19.0-win-x64.msi -PreviousReferenceMsiPath ./artifacts/LithoCreate-Pro-1.18.0-win-x64.msi
```

Prüfumfang: Upgrade 1.18 → 1.19, bytegenaue Anwendungs-/Core-Dateien und zwölf Beispiele, integrierte Laufzeit, Testverknüpfungen, Rückstufungssperre, Deinstallation sowie Erhalt persönlicher Projektdateien und Benutzerverknüpfungen. Ergebnisse: artifacts/installer-results-1.19.0.txt und artifacts/installer-upgrade-1.19.0. Die installertest-MSIs sind keine Auslieferungspakete.

## Grenzen

Drucksatzhistorie gilt ausschließlich für Positionen, Abstände und Anordnungswahl, nicht für beliebige Modellaktionen. Gültige numerische Zwischenwerte können eigene Schritte erzeugen. Die Historie selbst ist nicht persistent; die aktuelle Planung wird in Format 15 gespeichert und bleibt mit Version 1.18 kompatibel. Physische Druck-/Passungstests, unabhängige Slicerfreigaben, Intel-N95-/Windows-10-/OS-DPI-Hardwaretests und Codesignatur stehen weiter aus.
