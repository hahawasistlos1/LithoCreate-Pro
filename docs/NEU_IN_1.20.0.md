# Neu in LithoCreate Pro 1.20.0

## STL & Module

Ein zusätzlicher Arbeitsbereich liest ASCII- und binäre STL-Dateien, zeigt Maße und Topologie und exportiert gültige Einzelmodelle erneut als STL/3MF. Offene/fehlerhafte Netze bleiben vom Export ausgeschlossen. Eine sicher insgesamt umgekehrte Wicklung kann korrigiert werden. Einheit: Millimeter, Eingang Z nach oben. Bis 64 MiB / 300.000 Dreiecke; keine allgemeine Fremdnetzreparatur oder automatische Fotoabbildung auf beliebige STL-Oberflächen.

Externe Formmodule laufen in einem separaten, abbrechbaren Prozess mit 60-Sekunden-Limit. Mitgelieferte Beispiele: 90°-Bogensegment und Fotoplatte mit frei positionierbaren Rundbohrungen. Module sind ausführbarer Code; vor dem Laden wird ausdrücklich gewarnt. Anleitung: FORMMODULE.md.

## Licht und Halterungen nach Maß

Sieben Varianten: U-Halter für 8/10-mm-LED-Streifen, Maßringe für fertige E14/E27-Fassungen, Diffusor-Probeplatte, Winkelstütze und Montageplatte mit bis zu acht freien Bohrungen. Durchlass/Breite, Außenmaß/Länge/Höhe und Wandstärke sind einstellbar. Die Montageplatte verwendet X/Y/Durchmesser je Zeile. Änderungen verwerfen die alte Prüfdarstellung bis zur Neuberechnung.

Die E14/E27-Werte sind mechanische Durchlassbeispiele für eine gekaufte Fassung, keine universelle Normaufnahme, gedruckte elektrische Fassung oder Gewindeverbindung. Am konkreten Fassungsmodell nachmessen. Keine Wärmefreigabe für klassische Glüh-/Halogenlampen in PETG; LED-Leuchtmittel und Gesamtkonstruktion praktisch prüfen.

## P1S / PETG und Bambu Studio

Die Einstellungen bieten „P1S / PETG hinzufügen“. Vorhandene gleichnamige Profile bleiben erhalten. 0,4-mm-Düse / 0,12-mm-Schicht sind Ausgangswerte; reale Düse und PETG-Hersteller/Farbe prüfen. Die Vorlage beschreibt den nominalen Druckraum; effektive Höhe und ausgeschlossene Bettbereiche im Slicer berücksichtigen.

„In Bambu Studio öffnen“ speichert zunächst eine geprüfte Modelldatei und öffnet sie lokal im installierten Slicer. Drucker, Material, Stützen, Werkzeugwege, Zeit und Materialverbrauch dort prüfen. Es wird kein Druckauftrag gestartet. Die App erzeugt keine eigenen ungeprüften Werkzeugwege.

Das enthaltene ZIP „P1S-PETG-Probedrucke“ liefert acht Lichtplatten mit 0,6–3,0 mm Dicke und vier Kasten-/Deckel-Paare mit 0,15–0,45 mm Spiel pro Seite, ein Messprotokoll und die geometrischen Sollwerte. Physische Druckergebnisse sind damit vorbereitet, noch nicht bestanden.

## Prüfung und Kompatibilität

30-Minuten-Dauertest auf dem vorhandenen Intel N95 / Windows 11 mit 7.680 Modellen; unabhängiger Bambu-Studio-Import aller 15 Grundformen und tatsächlich berechnete P1S-/PETG-Testwerkzeugwege. Zusätzliche Layout-/Rasterprüfungen für 125/150/200 %. Bei kleinen Höhen entfällt der dekorative Editortitel, damit die Vorschau nutzbar bleibt. Der echte Windows-Wechsel zwischen 125 % und 150 % wurde mit laufender App geprüft; anschließend wurden die ursprünglichen 125 % wiederhergestellt. 200 % wird im regulären Windows-Auswahlmenü dieses Bildschirms nicht angeboten und wurde ausschließlich simuliert.

Projektformat 15, Modellvorlagenformat 13 und zwölf normale Beispielprojekte bleiben erhalten. Neue Signier- und Installerzykluswerkzeuge ergänzen die Abnahme; eine vertrauenswürdige Codesignatur benötigt weiterhin Zertifikat/Signierdienst und SignTool. Konkreter Reststand: ABARBEITUNG.md.
