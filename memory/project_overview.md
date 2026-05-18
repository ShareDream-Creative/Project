---
name: project-overview
description: Complete architecture and design overview of the GFramework-Godot-Template project — a Godot 4.6 C# 2D platformer with card-building mechanics
metadata:
  type: project
---

# GFramework-Godot-Template — Complete Project Overview

## Identity
- **Name**: GFramework-Godot-Template (可重命名)
- **Type**: Godot 4.6.1 + C# (.NET 10.0) 2D platformer game
- **Author**: GeWuYou / NoobKazoku
- **License**: Apache 2.0 (source), All Rights Reserved (assets)
- **Framework**: GFramework 0.0.205 — custom IoC/DI, CQRS, state machine, scene/UI routing framework

## Tech Stack
| Component | Version |
|---|---|
| Godot Engine | 4.6.1 (GL Compatibility renderer) |
| .NET SDK | 10.0 (C# preview, nullable enabled) |
| GFramework | 0.0.205 |
| Mediator | 3.0.1 (source generator for CQRS) |
| Scriban | 7.0.1 (templating) |
| Meziantou.Analyzer | 3.0.25 |
| Meziantou.Polyfill | 1.0.104 |

Display: 960×540, canvas_items stretch mode.

## Directory Map
```
project/
├── assets/          — art, audio/music, fonts, shaders (13 transition effects)
├── docs/            — 3 markdown docs (PROJECT_COMPLETE_GUIDE.md is 1130 lines)
├── global/          — 6 Autoload singletons + their .tscn files
├── resource/        — audio bus layout, theme, shader materials
├── scenes/          — ~37 .tscn files (levels, UI, platforms, traps, tests)
├── scripts/         — ~72 C# source files in 16 subdirectories
├── script_templates/— Godot editor C# templates
├── project.godot    — Godot project config
├── *.csproj, *.sln — .NET build files
```

## Autoload Singletons (loaded in order)
1. **GameEntryPoint** — Initializes GFramework Architecture, registers all scene/UI/texture configs from [Export] arrays, loads settings, pre-warms coroutine scheduler
2. **SceneRoot** — Node2D scene container, binds ISceneRouter
3. **UiRoot** — CanvasLayer UI container with Page/Modal/Tooltip/Popup layers, binds IUiRouter
4. **GlobalInputController** — Global ESC key → pause, manages IGlobalGameplayInputService, per-frame input state sync
5. **AudioManager** — BGM + SFX (12-channel object pool for SFX)
6. **SceneTransitionManager** — Screenshot + SubViewport pre-render + shader tween transitions

## Architecture: GFramework DI Modules

### UtilityModule — infrastructure utilities
- GodotUiRegistry, GodotSceneRegistry, GodotTextureRegistry
- GodotUiFactory, GodotSceneFactory
- JsonSerializer, GodotFileStorage, UnifiedSettingsDataRepository
- SaveStorageUtility

### ModelModule — data models
- ISettingsModel with GodotAudioSettings, GodotGraphicsSettings, GodotLocalizationSettings applicators

### SystemModule — core systems
- UiRouter (UI page stack), SceneRouter (scene stack)
- SettingsSystem, GodotAudioSystem

### StateModule — game state machine
5 states: BootStartState → MainMenuState ↔ PlayingState ↔ PausedState, GameOverState

## Game State Machine Flow
```
Game Launch
  → GameEntryPoint._Ready() → Architecture.Initialize()
  → BootStartState → SceneRouter → Boot scene
  → MainMenuState → Clear all scenes/UI → Push MainMenu UI
      ├─ "New Game"    → PlayingState → TeachLevel (tutorial)
      ├─ "Continue"    → PlayingState → LevelPrepare (saved level)
      ├─ "Choose Level"→ LevelChooseState → Choose scene
      ├─ "Options"     → OpenOptionsMenuCommand
      ├─ "Credits"     → Push Credits UI
      └─ "Exit"        → ExitGameCommand
```

## Level System (Heart of Gameplay)

### LevelPhase State Machine (per-level)
`Build → Play → Success/Defeat`

### BaseLevelController (~900 lines, Node2D)
Core orchestrator implementing ISimpleScene + IController. Composes 4 component interfaces:
- **ILevelUiManager** — UI phase transitions (BuildUI → PlayUI → SuccessUI/DefeatUI)
- **ILevelPlayerManager** — Player spawning, end-area detection, game-complete callback
- **ILevelInputController** — Phase-sensitive input filtering
- **ILevelRulesIntegration** — Timer/rule system, timeout → defeat

Key features:
- SetCurrentPhase() unified entry for phase changes, auto-syncs to IGlobalGameplayInputService
- Defeat area detection (%defeat Area2D) → RespawnPlayerToStart()
- Trap handling (HandleTrapTriggered) → restore visibility + respawn
- Player-is-child-of-player detection (upward parent traversal)
- ResetPlayerPhysicsState clears internal _velocity vector (not just CharacterBody2D.Velocity)

### Concrete Level Controllers
| Controller | Extends | SceneKey | Notes |
|---|---|---|---|
| LevelPlay | BaseLevelController | LevelPlay | Standard level 1 |
| TeachLevel | BaseLevelController | TeachLevel | Auto-skips Build phase, resets GameLevel to None on complete |
| LevelPerpare | Node2D (standalone) | LevelPerpare | Pre-level preparation screen |
| LevelEnd | Node2D (standalone) | LevelEnd | Post-level shop/settlement |
| LevelChoose | Control (standalone) | LevelChoose | Level selection UI |
| Choose | Node2D (standalone) | Choose | Level selection background |

### Level Scenes
Level1–Level5 each have their own .tscn scene files. Teach_Level.tscn for tutorial.

### Level UI Pages
LevelPrepareUi, LevelBuildUi, LevelPlayUi, LevelSuccessUi, LevelEndUi, LevelDefateUi

## Player Movement System (Composition Pattern)

**PlayerMovementController** (CharacterBody2D, ~770 lines) — coordinator, implements IPlayerDataListener.

3 composed sub-modules:
| Module | Implementation | Interface | Role |
|---|---|---|---|
| Input | PlayerInputHandler | IPlayerInputHandler | Reads from IGlobalGameplayInputService, dual-strategy (Input Map + direct keyboard) |
| Physics | PlayerPhysicsMovement | IPlayerPhysicsMovement | Gravity, velocity calc, MoveAndSlide() |
| State | PlayerStateController | IPlayerStateController | 3-layer input check: GlobalInputService + state machine + static phase flags |

Data flow per frame:
```
_PhysicsProcess → UpdateStateAndInput → input enabled check
  → ProcessMovement → ApplyGravity → UpdateHorizontalVelocity → TryJump → Move
```

## Input Blocking System (Defense-in-Depth)

3-layer check for player input:
1. `IGlobalGameplayInputService.IsInputEnabled` (based on LevelPhase, updated per-frame)
2. `BaseLevelController.IsBuildPhaseActive` / `IsSuccessPhaseActive` (static flags)
3. `IStateMachineSystem.Current is PlayingState`

All 3 must pass. LevelPhase.Build / Success / Defeat all block input. Only Play permits it.

## GlobalGameplayInputService
- Caches _horizontalDirection, _jumpPressed, _interactPressed
- Updated every frame by GlobalInputController._Process()
- When !IsInputEnabled, zeros all inputs
- Dual-strategy key detection: Input Map (ui_left/ui_right/ui_accept) + direct keyboard (A/D/Space/E/Arrow keys)

## CQRS Domains (8 domains, ~45 files)
- **audio**: ChangeBgmVolume, ChangeMasterVolume, ChangeSfxVolume, BgmChangedEvent, PlaySfxEvent
- **game**: PauseGame, ResumeGame, ExitGame, PauseGameWithOpenPauseMenu, ResumeGameWithClosePauseMenu
  - PauseGameWithOpenPauseMenuHandler has transactional rollback (reverts on failure)
- **graphics**: ChangeResolution, ToggleFullscreen
- **menu**: OpenOptionsMenu
- **pause_menu**: OpenPauseMenu, ClosePauseMenu (with Input DTOs)
- **poker**: StateChangedEvent
- **scene**: SceneRootReadyEvent
- **setting**: ChangeLanguage, ResetAllSettings, SaveSettings, GetCurrentSettingsQuery

All handlers registered as singletons via AddMediator().

## Data Persistence
- **PlayerDataManager** — thread-safe singleton (double-checked locking), lazy-loads from user://player_data.cfg (Godot ConfigFile), dirty-flag pattern
- **PlayerData** — observable model: Speed (50–1000), JumpVelocity (negative), Gravity (100–3000), SprintMultiplier (1.0–3.0), with IPlayerDataListener observer pattern
- **GameSaveData** — versioned save data with SlotDescription, SaveTime
- **SaveStorageUtility** / ISaveStorageUtility — save I/O abstraction

## UI System
- **UiRouter** — page stack (Push/Replace/Clear/Show/Resume)
- UI pages registered via UiPageConfig [Export] resources keyed by UiKey enum
- UiRoot manages 4 layers: World(0), Page(UiRoot=100), Modal, Tooltip, Popup
- **PauseStateManager** — static utility saves/restores pre-pause UI key, fixing the "ESC returns to home_ui" bug

## Poker/Card System
Draggable playing cards with state machine:
- **Poker** (Button) — card entity with tween animations
- **PokerStateMachine** — manages IdleState and DragState via signals
- **IdleState** — default, mouse down → Drag
- **DragState** — follows mouse, applies rotation, mouse up → Idle, confines cursor

## Scene Transition System
13 custom .gdshader effects: pixelate, blind, block_displacement, circle_expand, hex_grid, page_flip, pixel_distortion_dissolve, simple_circle, voronoi_ripple, zoom_blur, etc.
SceneTransitionManager pipeline: screenshot → SubViewport pre-render → tween shader animation.

## Audio System
GodotAudioSystem (IAudioSystem) → AudioManager node. BGM + SFX (12-channel pool). Buses: Master, BGM, SFX. Types: BgmType (Gaming, MainMenu, Ready), SfxType (ShipFire, Explosion, UiClick).

## Key Design Patterns
1. **CQRS + Mediator** for all non-trivial operations
2. **Composition over Inheritance** — BaseLevelController and PlayerMovementController both refactored from monoliths
3. **Observer** (IPlayerDataListener) for data change propagation
4. **Strategy** — dual input detection (Input Map + direct keyboard)
5. **Singleton** — PlayerDataManager with double-checked locking
6. **Dirty Flag** — only persist PlayerData when changed
7. **State Machine** — both game-level and poker-level
8. **Defense-in-Depth** — 3-layer input blocking
9. **Dual Router** — separate Scene and UI routing

## Recent Commit History (current branch: level_building_and_the_test)
- `210f74d` fix: 修复主菜单按钮失效问题并增强玩家控制系统
- `d46363c` fix: 修复暂停菜单ESC键关闭时错误跳转到home_ui的严重Bug
- `3dd1ef7` refactor: 提炼通用工具类消除重复代码并修复输入检测
- `ebffc09` feat: 实现基于LevelPhase的全局输入阻断机制
- `82b3d6a` feat: 增强玩家控制系统和失败区域检测功能

## Enums Quick Reference
- **SceneKey**: Boot, Main, Scene1, Scene2, Home, GameTest, LevelPerpare, LevelPlay, Level1–5, LevelChoose, Choose, LevelEnd, TeachLevel
- **UiKey**: MainMenu, SaveMenu, LoadMenu, OptionsMenu, Credits, HomeUi, PauseMenu, LevelChoose, LevelPrepareUi, LevelBuildUi, LevelPlayUi, LevelSuccessUi, LevelEndUi, LevelDefateUi
- **GameLevel**: None(0), Level1–5, LevelTest, LevelTeach
- **LevelPhase**: Build, Play, Success, Defeat
- **InputPhase**: Global, Gameplay, Paused
- **BgmType**: Gaming, MainMenu, Ready
- **SfxType**: ShipFire, Explosion, UiClick
