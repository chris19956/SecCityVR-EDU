# SecCityVR-EDU

**Immersive VR-Lernumgebung zur Vermittlung von Software-Schwachstellen**

SecCityVR-EDU ist eine Erweiterung des Projekts [SecCityVR](https://github.com/dewue/SecCityVR). SecCityVR visualisiert Software-Schwachstellen mithilfe der CodeCity-Metapher in Virtual Reality: Klassen und Methoden erscheinen als Gebäude, Schwachstellen als farbig markierte Stockwerke und Aufrufbeziehungen als Verbindungslinien.

SecCityVR-EDU baut darauf eine interaktive Lernumgebung für Studierende und Programmierende auf:

- **Geführtes Lernen** mit Lektionen zu typischen Schwachstellen, derzeit *Broken Access Control* und *SQL Injection*
- **Freier Modus** zur eigenständigen Erkundung der Code-Stadt
- **Tutorial** zur Einführung in die VR-Steuerung und Bedienung
- **LLM-gestützter Lehrer-Avatar**, der Fragen beantwortet und Schwachstellen erklärt
- **Spracheingabe und Sprachausgabe** (Speech-to-Text und Text-to-Speech über ElevenLabs)

---

## Inhaltsverzeichnis

1. [Voraussetzungen](#voraussetzungen)
2. [Installation](#installation)
3. [Konfiguration der KI- und Sprachdienste](#konfiguration-der-ki--und-sprachdienste)
4. [Ausführen im Unity-Editor](#ausführen-im-unity-editor)
5. [Bedienung](#bedienung)
6. [Projektstruktur](#projektstruktur)
7. [Fehlerbehebung](#fehlerbehebung)
8. [Zitation](#zitation)

---

## Voraussetzungen

### Software

| Komponente | Version / Hinweis |
|---|---|
| Unity Hub | aktuelle Version |
| Unity Editor | **6000.4.8f1** (Unity 6). Andere Versionen können Paket- oder Importfehler verursachen. |
| Unity-Module | *Android Build Support* inkl. *OpenJDK* und *Android SDK & NDK Tools* (nur für den Quest-Build erforderlich) |
| Git | aktuelle Version |
| IDE | Visual Studio 2022 oder JetBrains Rider (optional) |
| Meta Quest Link / Meta Horizon Link | für die Ausführung im Editor mit einer Quest-Brille unter Windows |

### Hardware

- Windows-PC mit VR-fähiger Grafikkarte
- VR-Headset mit OpenXR-Unterstützung, empfohlen **Meta Quest 2, 3 oder Pro**, inklusive Touch-Controllern
- Verbindung per USB-C-Kabel (Link) oder WLAN (Air Link)
- Mikrofon für die Spracheingabe (das Headset-Mikrofon genügt)

### Verwendete Pakete

Die Pakete werden beim ersten Öffnen des Projekts automatisch über `Packages/manifest.json` installiert:

- Meta XR SDK (`com.meta.xr.sdk.all` 85.0.0)
- XR Interaction Toolkit 3.4.1
- OpenXR Plugin 1.16.1 und XR Plugin Management 4.6.0
- Universal Render Pipeline 17.4.0
- Input System 1.19.0
- Newtonsoft JSON 3.2.2

Externe Plugins im `Assets`-Ordner werden nicht benötigt.

---

## Installation

### 1. Repository klonen

```bash
git clone https://github.com/chris19956/SecCityVR-EDU.git
cd SecCityVR-EDU
```

Alternativ kannst du das Repository auf GitHub über **Code → Download ZIP** herunterladen und entpacken.

> **Hinweis:** Verwende einen kurzen Pfad ohne Sonderzeichen, z. B. `C:\Projekte\SecCityVR-EDU`. Sehr lange Pfade können unter Windows zu Importfehlern führen.

### 2. Unity-Version installieren

1. Öffne den **Unity Hub**.
2. Wechsle zu **Installs → Install Editor**.
3. Installiere **Unity 6000.4.8f1**. Falls die Version nicht direkt angezeigt wird, findest du sie im [Unity-Download-Archiv](https://unity.com/releases/editor/archive).
4. Wähle bei Bedarf das Modul **Android Build Support** aus, wenn du später eine eigenständige Quest-Version erstellen möchtest.

### 3. Projekt in Unity öffnen

1. Klicke im Unity Hub auf **Projects → Add → Add project from disk**.
2. Wähle den Ordner `SecCityVR-EDU` aus, also den Ordner, in dem sich `Assets`, `Packages` und `ProjectSettings` befinden.
3. Öffne das Projekt mit **Unity 6000.4.8f1**.
4. Beim ersten Öffnen lädt Unity alle Pakete und importiert sämtliche Assets. Dies kann **mehrere Minuten** dauern.

Falls Dialoge erscheinen:

- **„Enable the new Input System?"** bzw. Neustart-Hinweis → mit **Yes** bestätigen
- **Meta XR Project Setup Tool** → empfohlene Korrekturen über **Fix All** anwenden
- **TextMesh Pro Essentials** → **Import TMP Essentials** auswählen, falls TMP-Ressourcen fehlen
- **Safe Mode** → Konsole prüfen (siehe [Fehlerbehebung](#fehlerbehebung))

### 4. XR-Einstellungen prüfen

1. Öffne **Edit → Project Settings → XR Plug-in Management**.
2. Stelle im Tab **Windows, Mac, Linux** (PC-Symbol) sicher, dass **OpenXR** aktiviert ist.
3. Unter **XR Plug-in Management → OpenXR** sollten folgende Optionen aktiviert sein:
   - **Interaction Profiles:** *Oculus Touch Controller Profile*
   - **OpenXR Feature Groups:** *Meta XR Feature*
4. Unter **Project Validation** kannst du eventuell angezeigte Warnungen über **Fix All** beheben.

---

## Konfiguration der KI- und Sprachdienste

Der Lehrer-Avatar nutzt ein LLM über eine OpenAI-kompatible Chat-Completions-API. Für Spracheingabe und Sprachausgabe wird ElevenLabs verwendet. Ohne gültige Zugangsdaten startet die Anwendung trotzdem, allerdings stehen dann der Chat mit dem Lehrer und die Sprachfunktionen nicht zur Verfügung.

Im Repository sind Endpunkt, Modell und Stimme bereits eingetragen. **Es fehlen nur die beiden API-Schlüssel**, die du selbst ergänzen musst.

### Konfigurationsdateien (empfohlen für den Editor)

**LLM:** `Assets/Scripts/LLM/Config/llm-config.json`

```json
{
  "endpoint": "https://ai-gateway.uni-paderborn.de/v1/chat/completions",
  "model": "hs-de.qwen3.8-27b-no-think",
  "apiKey": "<dein-api-key>"
}
```

Voreingestellt ist das KI-Gateway der Universität Paderborn. Einen Schlüssel dafür bekommst du über die Universität. Alternativ kannst du `endpoint` und `model` auf jeden anderen OpenAI-kompatiblen Dienst umstellen, beispielsweise OpenAI oder einen lokalen Server wie Ollama oder LM Studio.

**Sprache (ElevenLabs):** `Assets/Scripts/SpeechTransformation/Config/speech-config.json`

```json
{
  "apiKey": "<dein-elevenlabs-api-key>",
  "voiceId": "3nMIMZ7RlGwsq1WLgxY3"
}
```

Den ElevenLabs-Schlüssel erstellst du in deinem [ElevenLabs-Konto](https://elevenlabs.io/app/settings/api-keys). Die `voiceId` legt die Stimme des Lehrers fest und kann durch eine beliebige andere ElevenLabs-Stimme ersetzt werden.


## Ausführen im Unity-Editor

### 1. Headset verbinden

#### Variante A SteamVR:

1. Installiere die Steam Link App aus dem Meta Quest Store auf das Headset, installiere Steam und SteamVR auf dem Host PC und setze SteamVR als standard OpenXR Runtime.
2. Verbinde dein Headset über die Steam Link App mit dem Computer.

#### Variante B Meta Horizon Link:

1. Starte die **Meta Horizon Link**-App auf dem PC.
2. Aktiviere unter **Einstellungen → Allgemein** die Optionen **Unbekannte Quellen** und **OpenXR Runtime: Meta Quest Link als aktive Runtime festlegen**.
3. Verbinde die Quest per USB-C-Kabel oder Air Link und aktiviere in der Brille **Quest Link**.


### 2. Szene öffnen

1. Navigiere im **Project**-Fenster zu `Assets/Scenes`.
2. Öffne die Szene **`CodeCityScene.unity`** per Doppelklick. Es ist die einzige Szene des Projekts und bereits in den Build Profiles eingetragen.

### 3. Mit dem Uni-Paderborn VPN verbinden

### 4. Play Mode starten

1. Prüfe, dass die Konsole (**Window → General → Console**) keine roten Kompilierfehler anzeigt.
2. Klicke oben im Editor auf **Play** (▶).
3. Setze die Brille auf. Die Anwendung startet mit dem Hauptmenü.
4. Mit einem erneuten Klick auf **Play** beendest du den Play Mode.

---

## Bedienung

### Ablauf

1. **Hauptmenü** → Start
2. **Modusauswahl**
   - **Freier Modus:** Fragen an den Avatar stellen
   - **Geführtes Lernen:** Lektion auswählen
     - Lektion 1: *Broken Access Control*
     - Lektion 2: *SQL Injection*
3. Vor einer Lektion wird das **Tutorial** zu Steuerung und Interaktion durchlaufen.
4. Anschließend führt der **Lehrer-Avatar** durch die Lektion, erklärt die Schwachstellen an der Tafel und beantwortet Fragen.

### Interaktion

| Aktion | Steuerung (Meta Touch Controller) |
|---|---|
| Bewegen | Linker Thumbstick |
| Drehen | Rechter Thumbstick |
| Gebäude / UI auswählen | Mit dem Controller zeigen und Grab Button drücken |
| Flugmodus ein/aus | A-Button |
| Frage an den Lehrer stellen | Y-Button |
| Menü öffnen | X-Button |
| Nächste Erklärung | Button **Weiter** am Lehrer-Panel |


## Projektstruktur

```text
SecCityVR-EDU/
├── Assets/
│   ├── Characters/                 # Lehrer-Avatar, Animationen und Animatoren
│   ├── Resources/
│   │   └── Scenarios/              # Lernszenarien als JSON
│   │       ├── ShopCity_BrokenAccessControl.json
│   │       └── ShopCity_SQLInjection.json
│   ├── Scenes/
│   │   └── CodeCityScene.unity     # Hauptszene
│   ├── Scripts/
│   │   ├── AnalysisReader.cs       # Erzeugt die Code-Stadt aus Szenario-JSON
│   │   ├── InfoPanelController.cs  # Info- und Code-Panel
│   │   ├── ScenarioValidator.cs    # Validierung der Szenarien
│   │   ├── LLM/                    # LLM-Client und Konfiguration
│   │   ├── Menu/                   # Hauptmenü und VR-Optionen
│   │   ├── Movement/               # Flugmodus
│   │   ├── SpeechTransformation/   # ElevenLabs STT/TTS
│   │   ├── Teacher/                # Lehrer-Logik
│   │   └── Tutorial/               # Tutorial-Schritte und Prüfungen
│   ├── XR/ und XRI/                # XR- und Interaction-Toolkit-Einstellungen
│   └── Samples/                    # Meta XR und XR Interaction Toolkit Samples
├── Packages/                       # Paketabhängigkeiten (manifest.json)
└── ProjectSettings/                # Unity-Projekteinstellungen
```

---

## Fehlerbehebung

| Problem | Lösung |
|---|---|
| Projekt öffnet im **Safe Mode** | Konsole prüfen. Häufige Ursachen sind fehlende Pakete; **Window → Package Manager** öffnen und Pakete neu laden. Falls das nicht hilft: Unity schließen, Ordner `Library/` löschen und Projekt neu öffnen. |
| Rosa / magentafarbene Materialien | Prüfen, ob unter **Project Settings → Graphics** eine URP-Pipeline gesetzt ist. Notfalls **Window → Rendering → Render Pipeline Converter** ausführen. |
| Kein Bild in der Brille | Quest Link muss aktiv sein und als **OpenXR-Runtime** festgelegt sein. Unity nach dem Verbinden der Brille neu starten. |
| Controller reagieren nicht | Unter **OpenXR → Interaction Profiles** das *Oculus Touch Controller Profile* hinzufügen. |
| Fehlende Texte / TMP-Fehler | **Window → TextMeshPro → Import TMP Essential Resources** |
| Lehrer antwortet nicht | `llm-config.json` bzw. Umgebungsvariablen prüfen. Die Konsole zeigt Fehler wie `401 Unauthorized` oder Verbindungsfehler an. |
| „ElevenLabs API-Schlüssel fehlt" | `speech-config.json` ausfüllen oder `SECCITY_ELEVENLABS_API_KEY` setzen. |
| „Kein Mikrofon gefunden" | In Windows unter **Einstellungen → Datenschutz → Mikrofon** den Zugriff erlauben und das Standardmikrofon prüfen. |
| „Lesson JSON could not be loaded" | Die Szenario-Datei muss in `Assets/Resources/Scenarios/` liegen; der Name wird ohne `.json` angegeben. |

---

## Zitation

SecCityVR-EDU basiert auf SecCityVR:

> Dennis Wüppelmann and Enes Yigitbas. 2025. *SecCityVR: Visualization and Collaborative Exploration of Software Vulnerabilities in Virtual Reality.* In Evaluation and Assessment in Software Engineering (EASE '25), June 17–20, 2025, Istanbul, Turkiye. ACM, New York, NY, USA, 11 pages. https://doi.org/10.1145/3756681.3756939
