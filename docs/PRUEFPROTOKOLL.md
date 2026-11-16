# Prüfprotokoll · 03.11.2026

## Build

- Visual-Studio-Lösung und Release-Build mit .NET SDK 10.0.403 erfolgreich.
- 0 Compilerfehler, 0 Compilerwarnungen beim abschließenden Lösungsbuild.
- Windows-x64-Veröffentlichung mit eigener .NET-/WindowsDesktop-10.0.14-Laufzeit erfolgreich erzeugt.
- Die veröffentlichte EXE wurde im tatsächlichen Windows-Desktop gestartet und die native Oberfläche bedient.

## Automatisierte Integrationstests

**28 bestanden, 0 fehlgeschlagen.** Messung des letzten vollständigen funktionalen Laufs: 7,4 Sekunden, Peak Working Set etwa 101 MB im Testprozess. Frühere vollständige Läufe lagen bei 100–172 MB; dies ist keine Messung des gesamten interaktiven Appbetriebs oder eines Exportes mit maximalem Speicherbudget.

Für alle 15 Grundformen werden echte binäre STL-Dateien und 3MF-Dateien geschrieben, erneut eingelesen und geprüft. Dabei werden geschlossene Topologie, Kantenorientierung, nicht degenerierte Flächen, positives Volumen, Dreieckzahl, Außenmaße und die Z=0-Aufstellung nach dem Export kontrolliert.

Weitere Prüfungen: exakte flache Außenmaße, exakte Konturmaße von Herz/Stern/Oval/Tropfen, konstante Materialstärken für schwarze und weiße Testbilder im Positiv-/Negativmodus, Zylindernaht, vorzeitig abgebrochene Mesh-/Bildberechnung und Exporte, Erhalt vorhandener Dateien bei Abbruch, adaptive Auflösung beim 64-MB-Geometriebudget, ungültige Zahlen und Wandstärken, beschädigte/fehlende Projektdateien, nicht unterstützte Projektversionen, fehlende Metadaten, vollständiger Projekt-Roundtrip samt unverändertem Originalbild, JPG/PNG/BMP/TIFF-Import, Rotation/Zuschnitt und unveränderte Quelldaten bei Bildkorrekturen. Ein absichtlich offenes Mesh wird abgewiesen.

Die reproduzierbaren Testausgaben liegen unter `artifacts/test-results.txt` und `artifacts/tests`. Sie sind Build-Artefakte und nicht Bestandteil des Quellcode-Repositories.

## Unabhängiger Slicer-Import

Im bereits vorhandenen Bambu Studio wurden über die echte Windows-Oberfläche geladen:

- `Flat.3mf`: 48 × 4 × 36 mm, 7248 Dreiecke, Volumen etwa 3902,29 mm³.
- `Cylinder.stl`: etwa 37,8139 × 38,9043 × 36 mm, 18.648 Dreiecke, Volumen etwa 7809,69 mm³.

Beide Modelle erschienen auf dem Druckbett. Im beobachteten Importablauf wurde keine Mesh-Reparatur angefordert. Beim Öffnen des 3MF als Bambu-Projekt meldet Bambu, dass keine gültige Bambu-Druckkonfiguration vorhanden ist und nur Geometrie geladen wird. Das ist bei dieser reinen Modell-3MF zu erwarten: Sie enthält keine herstellerspezifischen Slicer-/Filamentprofile. Diese Meldung wird dokumentiert, nicht als Geometriefehler verschwiegen.

Die Kommandozeilenversuche lieferten auf diesem System keine auswertbare Modellinformation und zählen daher nicht als erfolgreicher Kompatibilitätsnachweis. Der beschriebene Nachweis stammt aus dem sichtbaren GUI-Import. Es wurde kein Druckauftrag versendet und kein G-Code erzeugt.

## Oberfläche

Zehn Layoutbilder für Startseite, Editor/Export, Vorlagen, Einstellungen und Hilfe wurden mit den tatsächlichen WPF-Steuerelementen bei 1400 × 900 und 960 × 640 gerendert. Startseite und Export bei beiden Größen wurden visuell geprüft; zusätzlich erfolgten direkte Desktopprüfungen von Fotoeditor und Zylindervorschau. Befunde wurden korrigiert: weißer Standardfensterhintergrund, überlagerte Startseitenkarte, Darstellung interner Formbezeichner, Überlappungen durch die mehrzeilige Werkzeugleiste und nicht scrollbar erreichbare Seitennavigation.

Die gerenderten Layoutbilder liegen unter `artifacts/ui-layout`. Dies ersetzt keine vollständige praktische Kontrolle bei den OS-Skalierungsstufen 125 %, 150 % und 200 %. Die Betriebssystem-Skalierung wurde nicht verändert.

## Installer

Das native MSI wurde in ein gesondertes Verzeichnis innerhalb dieses Projekts installiert und anschließend wieder deinstalliert. Die integrierte Laufzeit (`coreclr.dll`), die installierte EXE und der Startmenüeintrag wurden überprüft. Installation und Deinstallation endeten erfolgreich. Die Windows-Installer-Protokolle liegen unter `artifacts/installer-lifecycle` bzw. beim abschließenden Pakettest unter `artifacts/installer-final`.

Eine Prüfung der erzeugten Datenbank umfasst den eingebetteten Cabinet-Inhalt, stabile Dateikomponenten, optionale Desktopkomponente und native Installations-/Entfernungssequenzen. Die Installationsdatei und die EXE sind nicht digital signiert. Ein Upgrade über mehrere bereits veröffentlichte Versionen wurde noch nicht getestet.

## Weiter ausstehende praktische Freigaben

Keine physischen Ausdrucke, keine optische Materialkalibrierung, kein Langzeittest auf Intel N95, kein separater Windows-10-Rechner, kein vollständiger Slicer-Test jeder einzelnen Form, kein echter Netzwerktrennungstest und keine allgemeine Selbstüberschneidungsprüfung fremder Modelle. Die Anwendung enthält im eigenen Code keine Netzwerkklienten, Onlinepflicht oder Telemetrie; die abgeschlossenen Kernprüfungen verwenden ausschließlich lokale Daten.
