# Idle Space Miner

An idle / incremental space mining tycoon for Android and iOS, built with Unity.

Players buy planets, upgrade their mining rate, ship speed and cargo, and ship ore back to a mothership.
They smelt the ore into bars, craft high-value items, research new technology and hire managers.
When progress slows they sell the galaxy for Dark Matter and start again with permanent mothership upgrades.

| | |
|---|---|
| Engine | Unity 6 (6000.4.0f1), built-in render pipeline, 2D |
| Platforms | Android (package `com.xenium.idlespaceminer`), iOS (iPhone and iPad) |
| Language | C# |
| Orientation | Portrait |

## Gameplay content

- 16 planets mining 7 ores, revealed by 4 telescopes
- 9 smelted bars and 11 crafted items in production chains
- A research tree of 12 projects, including the Asteroid Miner, Management, Smelter, Crafter, furnace upgrades and telescopes
- Managers from 3 schools, promotable up to 7 stars
- Asteroids to catch, offline earnings (22% speed, up to 2 hours)
- Prestige: sell the galaxy for Dark Matter and upgrade 6 mothership rooms
- Tasks (money rewards), goals/achievements (Dark Matter rewards) and a guided first-time tutorial
- Optional rewarded ads: Mine Boost and the Ark reward

## Architecture

### Dependency injection with Zenject

All game logic is wired through **Zenject (Extenject 9.2)**. Every service and controller is bound to an interface in one place,
[`BindingsInstaller`](Assets/Scripts/Installers/BindingsInstaller.cs), and receives what it needs through its constructor.

**Why:**
- Controllers have no hard references to each other or to scene objects, so they can be replaced or tested in isolation.
  For example, `IRewardedAdService` is either AdMob or a simulated ad in the editor, and `IAnalyticsService` is Firebase.
- One installer shows every system in the game and how it is created.
- Lifecycle hooks (`IInitializable`, `ITickable`, `IDisposable`) give plain C# classes `Start`, `Update` and cleanup
  without being MonoBehaviours.

### Model, controller, view, connected by signals

```
Settings (ScriptableObjects)  ->  Controllers (rules, plain C#)  ->  Models (state)
                                          |  fire / subscribe
                                       SignalBus
                                          |
                                    Views (MonoBehaviours, UI and world)
```

| Layer | Folder | Role |
|---|---|---|
| Settings | `Assets/Scripts/SO`, `Assets/Resources/Settings` | Every number in the game lives in ScriptableObjects: `GameSettings`, `PlanetSettings`, `ResourceSettings` |
| Models | `Assets/Scripts/Models` | Game state only: `PlayerModel` (money, resources, research), `SpaceModel` (planets) |
| Controllers | `Assets/Scripts/Controllers` | Game rules as plain C# classes: mining, cargo, production, managers, mothership/prestige, boosts, tasks, achievements, tutorial, offline progress, saving |
| Views | `Assets/Scripts/Views`, `Assets/Scripts/UI` | MonoBehaviours that draw state and forward taps; they hold no game rules |
| Signals | `Assets/Scripts/Signals` | Small event classes sent through Zenject's `SignalBus` (planet unlocked, research completed, purchase, galaxy sold, ...) |
| Services | `Assets/Scripts/Services`, `Assets/Scripts/Analytics` | Platform-facing code: save file, ads, consent, analytics, input |

**Why:**
- **Signals decouple systems.** A purchase fires one `PurchaseSignal`; audio, toasts, the galaxy value and analytics
  each react without the buyer knowing about them. New features usually add a listener and change no existing code.
- **Data-driven balance.** Prices, rates, recipes, research costs, rewards and prestige values are all in assets,
  so balancing never needs a code change. The economy was tuned this way with an offline simulation of these formulas.
- **Views stay thin.** The whole UI was reskinned without touching game logic.

### Scenes and boot flow

1. **InitScene** creates the project-wide services (`PrefabInstaller`) and loads the menu.
2. **MenuScene** shows the start screen.
3. **GameScene** holds the galaxy, the HUD and every window. Selling the galaxy reloads this scene from a fresh save
   that keeps Dark Matter, rooms, managers and achievements.

### Saving

`SaveService` writes one JSON file (`JsonUtility`) to `Application.persistentDataPath`. `SaveController` builds the
snapshot from every controller and saves when the app is paused or closed, and every 60 seconds. Production in progress, mined ore waiting on
planets and ore in transit are all saved. Planet stats are recomputed from their levels on load, so balance changes
reach existing saves.

### Editor tooling

UI and content are generated and kept consistent by editor tools under `Assets/Editor`, mostly in **Tools > UI** and **Tools**:

| Tool | Purpose |
|---|---|
| `OrbitalThemeApplier` | Applies the Orbital UI kit theme to every prefab and window, and fixes text fit |
| `GameFontApplier` | Creates the Rajdhani TextMeshPro font assets and applies them everywhere |
| `PlanetsBuilder`, `PlanetLayoutTool`, `PlanetPanelBuilder` | Build planets and their info window |
| `ProductionUiBuilder`, `ManagersWindowBuilder`, `MothershipWindowBuilder`, `BoostsBuilder`, `TasksWindowBuilder`, `SettingsBuilder`, `TutorialBuilder` | Build each game window |
| `ResearchTreeSync` | Keeps the research tree UI in sync with `ResourceSettings` |
| `SaveFileEditorTool` | Inspect and edit the save file in the editor |

## Technology and third-party SDKs

### Unity packages

| Package | Use |
|---|---|
| TextMesh Pro, uGUI | All UI and text |
| 2D feature set | Sprites and 2D rendering |
| Adaptive Performance (Google Android, Samsung) | Thermal and performance scaling on Android devices |
| Mobile feature set, Android Logcat | Mobile build support and device logs |
| External Dependency Manager (`com.unity.external-dependency-manager` 2.2.0) | Resolves the Android and iOS native libraries the Google SDKs need into the Gradle templates and CocoaPods |
| Test Framework | Unit and play-mode tests |

### Third-party SDKs and libraries

| SDK | Version | Use |
|---|---|---|
| [Zenject / Extenject](https://github.com/Mathijs-Bakker/Extenject) | 9.2.0 | Dependency injection and the signal bus (`Assets/Plugins/Zenject`) |
| [Google Mobile Ads (AdMob)](https://developers.google.com/admob/unity) | 11.5.0 | Rewarded video ads for the Mine Boost and the Ark (`AdMobRewardedAdService`) |
| Google User Messaging Platform | bundled with Mobile Ads | GDPR/UK consent form and privacy options (`ConsentService`) |
| [Firebase Analytics](https://firebase.google.com/docs/analytics/unity/start) | 14.1.0 (`Packages/Firebase`) | Retention, sessions, play time, progress and economy events (`FirebaseAnalyticsService`, `AnalyticsController`) |
| [LeanTween](https://github.com/dentedpixel/LeanTween) | Asset Store | Tweens for floating texts and UI motion |

### Art and fonts

| Asset | Licence / source | Notes |
|---|---|---|
| Orbital Mobile Idle Kit | Purchased UI kit | UI panels, buttons, icons, FX, asteroids and backgrounds. Kept out of git (`Assets/OrbitalMobile`, `Assets/OrbitalUI`), so copy it in after cloning |
| Rajdhani | SIL Open Font License | Game font, `Assets/Fonts/Rajdhani` |
| Planets, ships and resource icons | Project art | `Assets/GFX` |

## Analytics, ads and privacy

- **Consent:** at launch, `ConsentService` runs Google's consent check. In the EEA, the UK and Switzerland it shows the
  consent form before ads or analytics start.
- **Ads and analytics follow the answers:** AdMob starts only when ads are allowed. Firebase starts with consent denied
  and applies the player's answers (Google consent mode).
- **Analytics through one interface:** every event goes through `IAnalyticsService`. The event list and dashboards are
  documented in [Docs/Analytics.md](Docs/Analytics.md).
- **Privacy policy:** [Docs/privacy-policy.html](Docs/privacy-policy.html) is the page to host.

## Getting started

1. Install Unity **6000.4.0f1** with the Android (and iOS) build modules.
2. Clone the repository and copy the Orbital Mobile Idle Kit into `Assets/OrbitalMobile` and `Assets/OrbitalUI`.
3. Open the project and wait for External Dependency Manager to resolve. On Android, run
   **Assets > External Dependency Manager > Android Resolver > Force Resolve**.
4. Firebase: `Assets/google-services.json` is included for Android; add `GoogleService-Info.plist` for iOS.
5. Signing: the Android upload keystore (`user.keystore`) is not in git. Place it in the project root and enter its
   passwords in **Player Settings > Publishing Settings**.
6. Open `Assets/Scenes/InitScene.unity` and press Play.

## Repository layout

```
Assets/
  Scripts/        Game code (Controllers, Models, Views, UI, Services, Signals, SO, Analytics, Installers)
  Editor/         Editor tools that build and theme the UI
  Resources/      Settings assets and runtime prefabs
  Prefabs/        UI and game prefabs
  Scenes/         InitScene, MenuScene, GameScene
  Fonts/          Rajdhani font and TextMeshPro assets
  Plugins/        Zenject, Android libraries and Gradle templates
Packages/         Package manifest and the Firebase packages
Docs/             Analytics reference, privacy policy, store listing texts
StoreAssets/      Store screenshots, feature graphic, icons and their source renders
```
