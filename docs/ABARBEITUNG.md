# Abnahme und offene Arbeiten · 6. November 2026

Zielgerät nach Nutzerangabe: vorhandener Windows-11-Laptop / Intel N95; Drucker Bambu Lab P1S, PETG. Bambu Studio wurde unter C:\Program Files\Bambu Studio\bambu-studio.exe gefunden. Windows 10 bleibt ein ursprüngliches Kompatibilitätsziel, hat für die konkrete Abnahme auf diesem Laptop keine Priorität. Im aktuellen Benutzerzertifikatspeicher wurde kein Codesignaturzertifikat gefunden.

| Paket | Stand / Abschlusskriterium |
|---|---|
| ASCII-/Binär-STL-Import | Implementiert: begrenztes Einlesen, Achsen/Einheiten, Topologieprüfung, Vorschau und geprüfter Export. Gesamtwicklung nur bei übereinstimmender Teilkörperorientierung korrigieren; gemischte Wicklungen/Punktkontakte ablehnen. Keine allgemeine Fremdnetzreparatur oder automatische Fotoabbildung auf beliebige Fremdnetze. |
| Externe Form-Plugins | Implementiert: IFormPlugin-Vertrag, zwei reale Beispiel-DLLs, separater Host, Abbruch/60-Sekunden-Limit, erneutes Einlesen der Ausgabe. Freigabe vor Ausführen, keine Sicherheitssandbox. Anleitung FORMMODULE.md. |
| Zusätzliche Halterungen / Löcher | Implementiert: sieben Varianten inklusive freier Montageplattenbohrungen, Diffusor-Probe, LED-U-Halter und Winkelstütze. Zusätzlich Fotoplattenmodul mit Rundbohrungen. Belastbarkeit und Passung benötigen Druckversuche. |
| Lichtoptionen | Implementiert als passive Maßhalter: LED-Streifen sowie E14/E27-Durchlassbeispiele. Konkrete Fassung und Wärmeprüfung erforderlich. Keine Freigabe für Glüh-/Halogenlampen in PETG. |
| Slicer | Implementiert: geprüfte Datei an installiertes Bambu Studio übergeben. Unabhängiger Vergleich aller 15 Formen bestanden; tatsächliche P1S-/Generic-PETG-Testwerkzeugwege erzeugt, Erfolgsbericht und G-Code geprüft. Keine Upload-/Druckaktion. |
| Physische Druckversuche | Vorbereitet: 16 geometrisch geprüfte PETG-Licht-/Passungsproben mit Messprotokoll im ZIP. Tatsächliche PETG-Drucke und Messwerte durch Nutzer erforderlich. Resin-Prüfungen benötigen passende Hardware. |
| Intel-N95-Dauertest | Bestanden für die 15-Formen-Corepipeline: 30 Minuten, 512 Zyklen, 7.680 Modelle, Peak 154,82 MiB. Kein mehrtägiger GUI-/Treiber-/Leakfreiheitsnachweis. Bericht artifacts/endurance-n95/result.json. |
| DPI 125/150/200 % | 39 automatische Render-/Layoutprüfungen bestanden. Echte Windows-Umschaltung 125 → 150 → 125 % mit laufender App geprüft und ursprüngliche 125 % wiederhergestellt. 200 % wird regulär auf diesem Bildschirm nicht angeboten; nur simuliert. Mehrmonitorprüfung bleibt hardwareabhängig. Bericht artifacts/os-dpi-1.20.0/state.json. |
| Codesignatur | Signier-/Verifikationsschritt implementiert, Vorprüfung durchgeführt. Zertifikat/Signierdienst und SignTool fehlen hier; keine tatsächliche Signatur. Kein Ersatzzertifikat erzeugt. |
| Installer / Regression | Bestehende Integration/WPF-Prüfungen fortgeführt; drei isolierte Upgrade-/Rückstufungs-/Deinstallationszyklen bestanden. Endergebnis in artifacts/installer-cycles-final-1.20.0/results.json. Benutzerinstallationen und Verknüpfungen bleiben geschützt. |

Erledigt bedeutet implementiert und mit angegebenem Nachweis geprüft. Externe Voraussetzungen oder vorbereitete Prüfpläne gelten nicht als bestandene Abnahme.


## Was ich vom Nutzer für den Abschluss der praktischen Abnahme brauche

1. Das mitgelieferte PETG-Probedruckpaket auf dem P1S drucken und das CSV-Messprotokoll ausfüllen: tatsächlich verwendetes PETG/Farbe, Düse, Schichthöhe, gemessene Dicke und Passung/Lichtwirkung. Drucklage der späteren Lithophanie berücksichtigen.
2. Tatsächliche LED-Streifenbreite oder das gekaufte E14/E27-LED-Fassungsmodell samt mechanischen Maßen auswählen. Die Maßringe sind keine universellen Norm-/Wärmefreigaben.
3. Für eine vertrauenswürdige Veröffentlichung ein Codesignaturzertifikat/Signierdienst und SignTool bereitstellen. Keine privaten Schlüssel/Kennwörter im Chat senden.
4. Falls andere Bildschirme verwendet werden: Monitorwechsel und dort verfügbare Skalierungen praktisch prüfen. 125 % und 150 % wurden auf diesem Laptop nativ geprüft. Windows 10 wird für den vorhandenen Windows-11-Laptop nicht als bereits bestandene Abnahme ausgegeben.
