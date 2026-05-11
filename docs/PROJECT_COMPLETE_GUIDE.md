# GFramework-Godot-Template 项目完整开发指南

> **版本**: 3.0.0 (整合版)  
> **最后更新**: 2026-05-11  
> **适用框架**: GFramework 0.0.205 + Godot Engine 4.6 + .NET 10.0  
> **文档类型**: 项目总览与实战指南（整合自10份技术文档）

---

## 📖 目录

1. [技术栈与架构概览](#1-技术栈与架构概览)
2. [F5调试启动完整流程](#2-f5调试启动完整流程)
3. [项目目录结构与规范](#3-项目目录结构与规范)
4. [核心系统详解](#4-核心系统详解)
5. [开发工作流指南](#5-开发工作流指南)
6. [场景切换系统](#6-场景切换系统)
7. [玩家移动系统](#7-玩家移动系统)
8. [玩家数据管理系统](#8-玩家数据管理系统)
9. [常见问题与解决方案](#9-常见问题与解决方案)
10. [最佳实践与约束条件](#10-最佳实践与约束条件)

---

## 1. 技术栈与架构概览

### 1.1 技术组件

| 组件 | 版本 | 用途 |
|------|------|------|
| **Godot Engine** | 4.6.1 | 游戏引擎基础运行环境 |
| **.NET SDK** | 10.0 | C# 语言运行时环境 |
| **GFramework** | 0.0.205 | 游戏架构框架支持 |
| **Mediator** | 3.0.1 | CQRS 设计模式的中介者功能 |
| **Scriban** | 7.0.1 | 模板引擎功能支持 |

### 1.2 架构设计模式

本项目采用 **GFramework 模块化架构**，核心设计模式包括：

```
┌─────────────────────────────────────────────────────┐
│                  GameEntryPoint                      │
│              (全局入口点 - Autoload)                   │
├─────────────────────────────────────────────────────┤
│                    GameArchitecture                   │
│         (模块安装: System/Model/State/Utility)        │
├──────────┬──────────┬──────────┬────────────────────┤
│   CQRS   │ 状态机系统 │ 场景路由 │    UI路由系统      │
│ Command  │ StateMachine│SceneRouter│ UiRouter       │
│  Query   │           │          │                 │
│  Event   │           │          │                 │
└──────────┴──────────┴──────────┴────────────────────┘
```

#### 分层架构

```
表现层 (Presentation)
├── Scenes (场景视图)
├── UI (UI页面)
└── Global Nodes (Autoload单例)

应用层 (Application)
├── CQRS Commands/Events/Queries
└── State Machine System

核心层 (Core)
├── SystemModule (路由、设置、音频)
├── ModelModule (数据模型)
├── StateModule (状态管理)
└── UtilityModule (工具服务)

基础设施层 (Infrastructure)
├── GameArchitecture (架构实例)
├── DI Container (依赖注入)
├── Mediator (CQRS中介者)
├── Logger (日志系统)
└── Config (配置管理)
```

### 1.3 核心设计原则

- ✅ **单一职责 (SRP)**：每个模块只负责一个关注点
- ✅ **开闭原则 (OCP)**：通过接口扩展，无需修改源码
- ✅ **依赖倒置 (DIP)**：依赖于抽象而非具体实现
- ✅ **CQRS模式**：命令查询职责分离
- ✅ **事件驱动 (EDA)**：解耦组件间通信
- ✅ **状态机 (FSM)**：基于有限状态机的游戏状态管理

---

## 2. F5调试启动完整流程

### 2.1 Godot引擎初始化阶段

```
用户按下F5键
    │
    ▼
Godot编辑器检测到运行请求
    │
    ├─→ 读取 project.godot 配置文件
    │       └─→ 获取: 主场景UID、AutoLoad列表、程序集名称
    │
    ├─→ 初始化.NET运行时环境 (.NET 10.0)
    │       └─→ 编译C#项目 → 生成 .dll 程序集
    │
    ├─→ 初始化渲染后端 (GL Compatibility, 960x540)
    │
    └─→ 创建主场景树 (SceneTree)
```

### 2.2 Autoload全局单例加载顺序

**按 project.godot 中 [autoload] 声明的顺序依次执行**

#### 第1个: GameEntryPoint (最关键的入口点)

```csharp
public override void _Ready()
{
    // Step 1: 获取场景树引用
    Tree = GetTree();
    
    // Step 2: 创建GFramework架构实例 ⭐⭐⭐ 【框架核心】
    Architecture = new GameArchitecture(configuration, environment);
    
    // Step 3: 初始化架构(安装所有模块) ⭐⭐⭐ 【框架核心】
    Architecture.Initialize();
    /*
      内部调用 InstallModules():
      ├── InstallModule(new UtilityModule())
      │     └─→ 注册: IGodotTextureRegistry, IGodotSceneRegistry, IGodotUiRegistry
      ├── InstallModule(new ModelModule())
      │     └─→ 注册: ISettingsModel, ...
      ├── InstallModule(new SystemModule()) ⭐【最重要】
      │     └─→ 注册: IUiRouter, ISceneRouter, ISettingsSystem, IAudioSystem
      └── InstallModule(new StateModule())
            └─→ 注册: IStateMachineSystem
      
      + Mediator注册 (CQRS中介者)
    */
    
    // Step 4: 获取设置模型并初始化
    _settingsModel = this.GetModel<ISettingsModel>()!;
    _ = _settingsModel.InitializeAsync(); // 异步加载用户设置
    
    // Step 5: 监听设置初始化完成事件
    this.RegisterEvent<SettingsInitializedEvent>(e => {
        _settingsSystem = this.GetSystem<ISettingsSystem>()!;
        _ = _settingsSystem.ApplyAll(); // 应用所有设置
    });
    
    // Step 6: 获取三个核心注册表服务
    _sceneRegistry = this.GetUtility<IGodotSceneRegistry>()!;
    _uiRegistry = this.GetUtility<IGodotUiRegistry>()!;
    _textureRegistry = this.GetUtility<IGodotTextureRegistry>()!;
    
    // Step 7: 注册资源配置到各注册表 ⭐⭐⭐ 【关键步骤】
    foreach (var config in GameSceneConfigs) 
        _sceneRegistry.Registry(config); // 场景注册
    
    foreach (var config in UiPageConfigs) 
        _uiRegistry.Registry(config); // UI页面注册
        
    foreach (var config in TextureConfigs) 
        _textureRegistry.Registry(config); // 纹理注册
    
    // Step 8: 延迟初始化协程调度器
    CallDeferred(nameof(CallDeferredInit));
}
```

#### 第2个: SceneRoot (场景根容器)

```csharp
public override void _Ready()
{
    var router = this.GetSystem<ISceneRouter>()!;
    router.BindRoot(this); // 绑定场景容器 ⭐⭐⭐
    
    CallDelayed(() => {
        this.RunPublishCoroutine(new SceneRootReadyEvent()); // 发布就绪事件
    });
}
```

#### 第3个: UiRoot (UI画布根容器)

```csharp
public override void _Ready()
{
    Layer = UiLayers.UiRoot; // 设置UI层级
    
    InitLayers(); // 初始化UI层级容器 ⭐⭐⭐
    /*
      UiLayer.Page     → Page容器 (ZIndex=0)
      UiLayer.Modal    → Modal容器 (ZIndex=100)
      UiLayer.Tooltip  → Tooltip容器 (ZIndex=200)
      UiLayer.Popup    → Popup容器 (ZIndex=300)
    */
    
    var router = this.GetSystem<IUiRouter>()!;
    router.BindRoot(this); // 绑定UI容器 ⭐⭐⭐
    
    CallDelayed(() => {
        this.RunPublishCoroutine(new UiRootReadyEvent()); // 发布就绪事件
    });
}
```

#### 第4个及以后: 其他Global节点

- **GlobalInputController**: 全局输入控制器，处理暂停等全局输入
- **AudioManager**: 音频管理器，绑定IAudioSystem
- **SceneTransitionManager**: 场景过渡动画管理器

### 2.3 启动完成标志

所有AutoLoad节点初始化完成后：
- ✅ DI容器中已注册所有核心服务
- ✅ 场景/UI/纹理注册表已填充
- ✅ 设置系统已加载并应用
- ✅ 协程调度器已预热
- ✅ 可以开始进行场景切换操作

---

## 3. 项目目录结构与规范

### 3.1 Scripts 目录 (16个核心文件夹)

```
scripts/
├── component/          # 组件层：游戏容器与抽象组件
├── constants/          # 常量层：全局常量定义
├── core/               # 核心层：框架基础设施
├── cqrs/               # CQRS层：命令查询职责分离
├── credits/            # 业务层：制作组页面
├── data/               # 数据层：数据管理与持久化
├── entities/           # 实体层：游戏实体定义
├── enums/              # 枚举层：枚举类型定义
├── intro/              # 业务层：介绍页面
├── main_menu/          # 业务层：主菜单页面
├── module/             # 模块层：依赖注入模块注册
├── options_menu/       # 业务层：选项菜单页面
├── pause_menu/         # 业务层：暂停菜单页面
├── setting/            # 配置层：系统设置管理
├── stateMachine/       # 状态机层：状态机接口定义
└── utility/            # 工具层：通用工具类
```

### 3.2 Global 目录 (全局服务节点)

```
global/
├── GameEntryPoint.cs                  # 游戏入口点（架构初始化）
├── GlobalInputController.cs           # 全局输入控制器
├── IGlobalGameplayInputService.cs     # 游戏玩法输入服务接口
├── GlobalGameplayInputService.cs      # 游戏玩法输入服务实现
├── AudioManager.cs                    # 音频管理器
├── SceneRoot.cs                       # 场景根节点
├── UiRoot.cs                          # UI根节点
├── SceneTransitionManager.cs          # 场景过渡管理器
└── *.tscn                             # 对应的场景文件（AutoLoad节点）
```

**核心特征**：
- 作为 Godot AutoLoad 单例运行
- 生命周期贯穿整个游戏运行周期
- 全局可访问，所有模块均可引用
- 提供基础设施服务和全局状态管理

### 3.3 文件命名规则速查

| 目录类型 | 命名格式 | 示例 |
|---------|---------|------|
| 组件 | `*Component.cs` | `VolumeContainer.cs` |
| 常量 | `*Constants.cs` | `GameConstants.cs` |
| 核心接口 | `I*.cs` | `ISimpleScene.cs` |
| CQRS命令 | `*Command.cs` | `ExitGameCommand.cs` |
| CQRS处理器 | `*CommandHandler.cs` | `ExitGameCommandHandler.cs` |
| 页面脚本 | `[PageName].cs` | `MainMenu.cs` |
| 数据模型 | `*Data.cs` | `PlayerData.cs` |
| 数据管理器 | `*Manager.cs` | `PlayerDataManager.cs` |
| 枚举 | `[Enum].cs`, `*Type.cs` | `SceneKey.cs` |
| DI模块 | `*Module.cs` | `SystemModule.cs` |

---

## 4. 核心系统详解

### 4.1 CQRS 系统 (命令查询职责分离)

**位置**: `scripts/cqrs/`

#### 领域划分

```
cqrs/
├── audio/           # 音频领域 (音量调节命令/事件)
├── game/            # 游戏领域 (暂停/退出/恢复命令)
├── global/          # 全局事件 (UI根/场景根就绪事件)
├── graphics/        # 图形领域 (分辨率/全屏切换)
├── menu/            # 菜单领域 (打开选项菜单)
├── pause_menu/      # 暂停菜单领域 (打开/关闭暂停菜单)
├── scene/           # 场景领域 (场景根就绪事件)
└── setting/         # 设置领域 (语言/重置/保存 + 设置查询)
```

#### 文件类型

| 类型 | 格式 | 示例 |
|------|------|------|
| Command | `[Action][Target]Command.cs` | `ChangeBgmVolumeCommand.cs` |
| Handler | `[Action][Target]CommandHandler.cs` | `ChangeBgmVolumeCommandHandler.cs` |
| Input DTO | `[Action][Target]CommandInput.cs` | `ChangeBgmVolumeCommandInput.cs` |
| Event | `[Action][Target]Event.cs` | `BgmChangedEvent.cs` |
| Query | `Get[Entity]Query.cs` | `GetCurrentSettingsQuery.cs` |
| View Model | `[Entity]View.cs` | `SettingsView.cs` |

#### 使用示例

```csharp
// 发送命令
this.SendCommand(new PauseGameWithOpenPauseMenuCommand(input));

// 监听事件
this.RegisterEvent<SettingsInitializedEvent>(e => {
    // 处理逻辑
});
```

### 4.2 状态机系统

**位置**: `scripts/stateMachine/`, `scripts/core/state/impls/`

#### 状态定义

```csharp
// scripts/core/state/impls/PlayingState.cs
public class PlayingState : AsyncContextAwareStateBase
{
    public override async Task OnEnterAsync(IState? from)
    {
        await this.GetSystem<IUiRouter>()!.ReplaceAsync(HomeUi.UiKeyStr);
    }
}
```

#### 5状态模型

| 状态 | 触发条件 | 行为 |
|------|---------|------|
| **BootStartState** | 游戏启动 | 加载资源、初始化系统 |
| **MainMenuState** | 启动完成 | 显示主菜单 |
| **PlayingState** | 进入游戏 | 允许玩家交互 |
| **PausedState** | 按ESC键 | 暂停游戏、显示菜单 |
| **GameOverState** | 游戏结束 | 显示结果界面 |

#### 状态切换方法

```csharp
await _stateMachineSystem
    .ChangeToAsync<PlayingState>()
    .ToCoroutineEnumerator()
    .RunCoroutine();
```

### 4.3 双路由系统

#### SceneRouter (场景路由)

- **接口**: `ISceneRouter`
- **功能**: 管理场景的替换和切换
- **绑定**: `router.BindRoot(SceneRoot)`
- **方法**: `ReplaceAsync(sceneKey)`, `PushAsync(sceneKey)`, `PopAsync()`

#### UiRouter (UI路由)

- **接口**: `IUiRouter`
- **功能**: 管理UI页面的栈式导航
- **绑定**: `router.BindRoot(UiRoot)`
- **方法**: `PushAsync(uiKey)`, `PopAsync()`, `ReplaceAsync(uiKey)`

#### UI层级系统

```csharp
public static class UiLayers
{
    public const int UiRoot = 100;     // UI根层
    public const int Page = 10;        // 页面层
    public const int Popup = 20;       // 弹窗层
    public const int Overlay = 30;     // 覆盖层
    public const int Tooltip = 40;     // 提示层
}
```

---

## 5. 开发工作流指南

### 5.1 新功能开发流程

#### Step 1: 创建枚举定义

```csharp
// 文件位置: scripts/enums/{模块名}/
public enum StateType { Idle, Drag, ... }
```

#### Step 2: 定义接口契约

```csharp
// 文件位置: scripts/{功能模块}/I{接口名}.cs
public interface IPoker
{
    Guid GetId();
    void SetGlobalPosition(Vector2 pos);
}
```

#### Step 3: 实现CQRS命令/查询

```csharp
// 命令: scripts/cqrs/{领域}/command/
public class MyCommand : ICommand { }

// 处理器: 同目录下
public class MyCommandHandler : AbstractCommandHandler<MyCommand>
{
    public override ValueTask<Unit> Handle(MyCommand command, CancellationToken ct)
    {
        // 业务逻辑实现
    }
}
```

#### Step 4: 实现状态管理 (如需要)

```csharp
// 文件位置: scripts/core/state/impls/
public class MyState : AsyncContextAwareStateBase
{
    public override async Task OnEnterAsync(IState? from) { }
}
```

#### Step 5: 创建场景/UI控制器

```csharp
[ContextAware]
[Log]
public partial class MyScene : Node2D, IController, ISceneBehaviorProvider, ISimpleScene
{
    public static string SceneKeyStr => nameof(SceneKey.MyScene);
    
    public ISceneBehavior GetScene()
    {
        _scene ??= SceneBehaviorFactory.Create<Node2D>(this, SceneKeyStr);
        return _scene;
    }
}
```

#### Step 6: 配置资源注册

在 `global/game_entry_point.tscn` 的 Inspector 中：
- `GameSceneConfigs`: 添加新场景配置
- `UiPageConfigs`: 添加新UI页面配置

### 5.2 编码规范

- 符合 C# 编码规范（命名约定、代码格式）
- 遵循 Godot 引擎最佳实践
- 使用 `[ContextAware]` 和 `[Log]` 特性启用DI和日志
- 所有公开API必须包含 XML 文档注释
- 使用 `this.GetSystem<T>()`, `this.GetModel<T>()`, `this.GetUtility<T>()` 获取服务

### 5.3 编译验证

每次修改后必须执行：

```bash
cd c:\Users\dgh\Desktop\Dreamcreative\code\gamebulid\Project
dotnet build
```

**验证清单**:
- ✅ 零编译错误
- ✅ 无关键警告
- ✅ 命名空间正确
- ✅ 引用完整性

---

## 6. 场景切换系统

### 6.1 导航流程图

```
MainMenu (主菜单)
    │ 点击"新游戏"
    ▼
HomeUi (主界面/场景选择)
    │ 点击"测试用场景"
    ▼
LevelPerpare (关卡准备) ← 包含 LevelPrepareUi 子UI
    │ 点击"开始构建" / "退回"
    ├───────────────────┤
    ▼                   ▼
LevelPlay             HomeUi (返回)
    │ 加载完成自动激活 LevelBuildUi
    │ 点击"完成!"
    ▼
LevelPlayUi (游戏模式)
```

### 6.2 关键实现要点

#### 两阶段导航 (重要!)

**❌ 错误做法** (仅替换场景):
```csharp
_sceneRouter.ReplaceAsync(LevelPerpare); // HomeUi仍显示!
```

**✅ 正确做法** (先关闭UI再切换场景):
```csharp
private IEnumerator SwitchSceneCoroutine(string sceneKey)
{
    // 步骤1: 关闭当前UI页面
    yield return _uiRouter.PopAsync().AsTask().ToCoroutineEnumerator();
    
    // 步骤2: 替换底层场景
    yield return _sceneRouter.ReplaceAsync(sceneKey)
        .AsTask()
        .ToCoroutineEnumerator();
}
```

#### 场景注册配置

在 `game_entry_point.tscn` 中配置 `GameSceneConfigs`:

| SceneKey | 场景路径 |
|----------|---------|
| MainMenu | `res://scenes/main_menu/main_menu.tscn` |
| Home | `res://scenes/tests/home_ui.tscn` |
| LevelPerpare | `res://scenes/level/level_perpare.tscn` |
| LevelPlay | `res://scenes/level/level_play.tscn` |
| GameTest | `res://scenes/tests/gametest.tscn` |

### 6.3 UI状态管理 (LevelPlay示例)

```csharp
// LevelPlay.cs - UI模式切换
private void SwitchToGameMode()
{
    LevelBuildUi.Hide();  // 失活构建UI
    LevelPlayUi.Show();   // 激活游戏UI
}

private void OnFinishButtonPressed()
{
    SwitchToGameMode();
}
```

### 6.4 资源管理

在 `_ExitTree()` 中清理资源：

```csharp
public override void _ExitTree()
{
    CleanupResources();
    GC.Collect(); // 强制垃圾回收
}

private void CleanupResources()
{
    // 释放子UI引用
    if (LevelBuildUi != null) { /* 清理 */ }
    if (LevelPlayUi != null) { /* 清理 */ }
}
```

---

## 7. 玩家移动系统

### 7.1 系统架构 (重构后)

采用**组合模式+策略模式**，将原162行单体类拆分为7个文件的模块化架构：

```
scripts/player/
├── interfaces/                      # 接口定义层
│   ├── IPlayerInputHandler.cs       # 输入处理接口
│   ├── IPlayerPhysicsMovement.cs    # 物理运动接口
│   └── IPlayerStateController.cs    # 状态控制接口
├── input/                           # 输入处理模块
│   └── PlayerInputHandler.cs        # Godot输入封装
├── physics/                         # 物理运动模块
│   └── PlayerPhysicsMovement.cs     # CharacterBody2D物理逻辑
├── state/                           # 状态控制模块
│   └── PlayerStateController.cs     # PlayingState检测
└── PlayerMovementController.cs      # 组合器/协调者
```

### 7.2 核心接口契约

#### IPlayerInputHandler (输入处理)

```csharp
public interface IPlayerInputHandler
{
    float HorizontalDirection { get; }  // [-1.0, 1.0]
    bool IsJumpPressed { get; }         // 单次触发
    float CachedSprintMultiplier { get; } // 奔跑倍率缓存
    void UpdateInput();                  // 每帧刷新
}
```

#### IPlayerPhysicsMovement (物理运动)

```csharp
public interface IPlayerPhysicsMovement
{
    float Speed { get; set; }
    float JumpVelocity { get; set; }
    float Gravity { get; set; }
    Vector2 CurrentVelocity { get; }
    bool IsOnFloor { get; }
    
    void ApplyGravity(float delta);
    void UpdateHorizontalVelocity(float dir);
    bool TryJump();
    void Move(CharacterBody2D body);
    void StopImmediately();
}
```

#### IPlayerStateController (状态控制)

```csharp
public interface IPlayerStateController
{
    bool IsInputEnabled { get; }
    void Initialize();
    void UpdateState();
}
```

### 7.3 组合器协调逻辑

```csharp
public override void _PhysicsProcess(double delta)
{
    var deltaF = (float)delta;
    
    // Step 1: 更新状态和输入
    _stateController.UpdateState();
    _inputHandler.UpdateInput();
    
    // Step 2: 状态检查 (非PlayingState则停止)
    if (!_stateController.IsInputEnabled)
    {
        _physicsMovement.StopImmediately();
        _physicsMovement.Move(this);
        return;
    }
    
    // Step 3: 正常移动流程
    _physicsMovement.ApplyGravity(deltaF);
    _physicsMovement.UpdateHorizontalVelocity(_inputHandler.HorizontalDirection);
    
    if (_inputHandler.IsJumpPressed && _physicsMovement.TryJump())
    {
        _log.Debug("玩家跳跃");
    }
    
    _physicsMovement.Move(this);
}
```

### 7.4 输入映射策略 (双策略模式)

```csharp
// PlayerInputHandler - 双策略输入检测
public void UpdateInput()
{
    // 策略1: Input Map优先 (支持自定义键位)
    _horizontalDirection = Input.GetAxis("ui_left", "ui_right");
    
    // 策略2: 直接键盘后备 (确保开箱即用)
    if (_horizontalDirection == 0)
    {
        bool left = Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left);
        bool right = Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right);
        
        if (left && !right) _horizontalDirection = -1.0f;
        else if (right && !left) _horizontalDirection = 1.0f;
    }
    
    _jumpPressed = Input.IsActionJustPressed("ui_accept")
                || Input.IsKeyPressed(Key.Space);
}
```

### 7.5 Godot编辑器配置步骤

#### 创建Player节点

```
1. 在 gametest.tscn 中添加 CharacterBody2D 节点，命名为 "Player"
2. 添加子节点:
   ├── CollisionShape2D (必须)
   │   └── Shape: RectangleShape2D 或 CircleShape2D
   └── Sprite2D (推荐)
       └── Texture: 角色精灵图
3. 挂载脚本: res://scripts/player/PlayerMovementController.cs
```

#### 配置导出参数

| 参数 | 默认值 | 推荐范围 | 说明 |
|------|--------|----------|------|
| Speed | 300.0 | 200-600 | 移动速度(像素/秒) |
| JumpVelocity | -500.0 | -400 to -800 | 跳跃初速度(负数向上) |
| Gravity | 980.0 | 800-1200 | 重力加速度 |

### 7.6 与全局状态集成

通过 `IGlobalGameplayInputService` 接口从 Global 层获取输入：

```csharp
// PlayerMovementController._Ready()
var inputCtrl = GetNode<GlobalInputController>("/root/GlobalInputController");
_inputHandler = (IPlayerInputHandler)inputCtrl.GameplayInputService;
```

**状态控制流程**:

```
进入GameTest场景 → GameTest.OnEnterAsync()
    │
    ▼
EnsurePlayingStateAsync()
    │
    ├── 当前是PlayingState? → 是: 跳过
    └── 否: ChangeToAsync<PlayingState>()
            │
            ▼
PlayerMovementController 开始响应输入 ✅

按下ESC → PausedState
    │
    ▼
IsInputEnabled() 返回 false
    │
    ▼
输入被禁用! 角色停止 ❌
```

---

## 8. 玩家数据管理系统

### 8.1 系统架构

```
┌─────────────────────────────────────────────────────────┐
│              Player Data Management System              │
├─────────────────────────────────────────────────────────┤
│  Presentation Layer                                     │
│  └── PlayerMovementController (组合器)                  │
│                                                          │
│  Data Layer                                             │
│  ├── PlayerData (数据模型)                              │
│  │   ├── Speed / JumpVelocity / Gravity                │
│  │   └── SprintMultiplier                              │
│  ├── IPlayerDataListener (观察者接口)                   │
│  └── PlayerDataManager (全局单例)                       │
│      ├── Instance (单例访问点)                          │
│      ├── SaveData() / LoadData()                       │
│      └── AddListener() / RemoveListener()              │
└─────────────────────────────────────────────────────────┘
```

### 8.2 核心组件

#### PlayerData (数据模型)

```csharp
// scripts/data/model/PlayerData.cs
public class PlayerData
{
    private float _speed = 300.0f;
    private float _jumpVelocity = -500.0f;
    private float _gravity = 980.0f;
    private float _sprintMultiplier = 1.5f;
    
    // 属性带验证和变更通知
    public float Speed
    {
        get => _speed;
        set
        {
            _speed = ValidateSpeed(value); // 范围检查
            NotifySpeedChanged(); // 通知监听器
        }
    }
    
    // ... 其他属性类似
}
```

#### PlayerDataManager (单例管理器)

```csharp
// scripts/data/PlayerDataManager.cs
public class PlayerDataManager
{
    private static PlayerDataManager? _instance;
    private static readonly object _lock = new();
    
    public static PlayerDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new PlayerDataManager();
                }
            }
            return _instance;
        }
    }
    
    private List<IPlayerDataListener> _listeners = new();
    
    public void AddListener(IPlayerDataListener listener)
    {
        if (!_listeners.Contains(listener))
            _listeners.Add(listener);
    }
    
    public void SaveData()
    {
        var config = new ConfigFile();
        config.SetValue("player", "speed", PlayerData.Speed);
        // ... 保存其他属性
        config.Save("user://player_data.cfg");
    }
    
    public void LoadData() { /* 从ConfigFile加载 */ }
}
```

#### IPlayerDataListener (观察者接口)

```csharp
// scripts/data/interfaces/IPlayerDataListener.cs
public interface IPlayerDataListener
{
    void OnSpeedChanged(float newSpeed);
    void OnJumpVelocityChanged(float newVelocity);
    void OnGravityChanged(float newGravity);
    void OnSprintMultiplierChanged(float newMultiplier);
}
```

### 8.3 数据同步机制

当属性值变更时自动通知所有监听器：

```
PlayerData.Speed = 350.0f
    │
    ▼
ValidateSpeed(350.0f) → 通过验证
    │
    ▼
遍历 _listeners:
    ├── Listener1.OnSpeedChanged(350.0f) → 更新物理模块
    ├── Listener2.OnSpeedChanged(350.0f) → 更新UI显示
    └── Listener3.OnSpeedChanged(350.0f) → 记录日志
```

### 8.4 使用示例

```csharp
// 在 PlayerMovementController 中使用
public class PlayerMovementController : Node2D, IPlayerDataListener
{
    private PlayerData _playerData;
    
    public override void _Ready()
    {
        _playerData = PlayerDataManager.Instance.PlayerData;
        PlayerDataManager.Instance.AddListener(this);
        
        // 初始同步
        SyncDataToPhysics();
    }
    
    public void OnSpeedChanged(float newSpeed)
    {
        _physicsMovement.Speed = newSpeed;
        _log.Info($"速度已更新为: {newSpeed}");
    }
    
    private void SyncDataToPhysics()
    {
        _physicsMovement.Speed = _playerData.Speed;
        _physicsMovement.JumpVelocity = _playerData.JumpVelocity;
        _physicsMovement.Gravity = _playerData.Gravity;
    }
}
```

---

## 9. 常见问题与解决方案

### 9.1 KeyNotFoundException (场景未注册)

**症状**: 点击按钮时抛出 `"The given key 'xxx' was not present in the dictionary"`

**原因**: 场景未在 `GameEntryPoint.GameSceneConfigs` 中注册

**解决方案**:

1. 打开 `global/game_entry_point.tscn`
2. Inspector → `GameSceneConfigs` 属性
3. 点击 "+" 添加新元素
4. 配置:
   - SceneKey: `GameTest` (或对应的枚举名称)
   - Scene: 选择对应的 `.tscn` 文件
5. 保存场景

**错误发生位置**: `SceneTransitionAnimationHandler.cs:71`

```csharp
// 第71行 - 字典访问失败点
TransitionManager.PlayTransitionCoroutine(
    SwitchCoroutine(),
    () => sceneMap[toSceneKey].Instantiate() // ❌ toSceneKey不存在!
).RunCoroutine();
```

### 9.2 HomeUi遮挡问题 (场景切换后UI不消失)

**症状**: 从HomeUi进入其他场景后，HomeUi仍然显示并遮挡新场景

**原因**: 只调用了 `_sceneRouter.ReplaceAsync()` 但没有关闭UI页面

**解决方案**: 实现**两阶段导航**

```csharp
// ✅ 正确做法: 先Pop UI再Replace Scene
private IEnumerator SwitchSceneCoroutine(string sceneKey)
{
    // 阶段1: 关闭当前UI
    yield return _uiRouter.PopAsync()
        .AsTask()
        .ToCoroutineEnumerator();
    
    // 阶段2: 切换场景
    yield return _sceneRouter.ReplaceAsync(sceneKey)
        .AsTask()
        .ToCoroutineEnumerator();
}
```

**参考实现**: Credits.cs 的正确模式 (`scripts/credits/Credits.cs#L82`)

### 9.3 玩家无法移动

**可能原因及排查**:

| 症状 | 可能原因 | 解决方案 |
|------|----------|----------|
| 不响应输入 | 未处于PlayingState | 检查控制台是否有"[GameTest] 场景初始化完成"日志 |
| A/D键无效 | Input Map未配置 | Project Settings → Input Map → ui_left/ui_right |
| 脚本未挂载 | Script属性为空 | 重新选择 `.cs` 文件 |
| 穿透地面 | 缺少CollisionShape2D | 添加碰撞体并配置Shape |
| 无法跳跃 | IsOnFloor返回false | 确认地面有StaticBody2D |

### 9.4 编译错误排查

| 错误代码 | 含义 | 常见原因 | 解决方案 |
|---------|------|----------|----------|
| CS0104 | 命名空间冲突 | using重复 | 检查using语句 |
| CS0246 | 类型未找到 | 缺少引用 | 检查命名空间和NuGet包 |
| CS1503 | 参数类型不匹配 | 方法签名错误 | 检查API版本 |
| CS1061 | 方法不存在 | API版本差异 | 更新为正确的方法名 |
| CS8618 | 非空字段未初始化 | 可空引用类型 | 添加 `!` 或 `?` |

---

## 10. 最佳实践与约束条件

### 10.1 开发约束 (来自 Agent输入限制条件.md)

✅ **允许的操作**:
- 仅修改 C# 代码文件
- 新增功能代码
- 修改错误提示文本
- 通过接口扩展功能
- 使用 partial 类扩展现有类
- 通过配置文件扩展行为
- 利用事件机制添加响应逻辑

❌ **禁止的操作**:
- 修改 `.tscn` 场景文件
- 删除或修改原有核心代码/业务逻辑/接口定义
- 修改框架核心代码 (`core/` 中的接口和基类)
- 引用未定义的类型或方法
- 硬编码路径或配置值
- 在数据类中包含业务逻辑
- 循环依赖

### 10.2 必须遵循的开发原则

1. **代码保护原则**: 不修改原有结构的核心代码
2. **测试验证原则**: 新增功能必须通过单元测试
3. **文件约束原则**: 所有工作仅限于 C# 文件修改
4. **接口优先**: 先检查现有接口是否可复用
5. **框架集成**: 所有扩展必须通过框架扩展点进行

### 10.3 扩展方式推荐

| 方式 | 适用场景 | 示例 |
|------|---------|------|
| partial 类 | 扩展现有类功能 | 为Node添加新方法 |
| 实现接口 | 添加新功能模块 | 实现IPlayerDataListener |
| 装饰器模式 | 包装现有功能 | 日志装饰器 |
| 配置文件 | 扩展系统行为 | 添加新的场景配置 |
| 事件机制 | 添加响应逻辑 | 监听SettingsChangedEvent |

### 10.4 性能优化建议

1. **对象分配**: 子模块在 `_Ready()` 中创建一次，避免每帧分配
2. **输入缓存**: InputHandler 已实现缓存，避免重复查询Godot Input API
3. **资源释放**: 在 `_ExitTree()` 中清理引用并调用 `GC.Collect()`
4. **日志级别**: 生产环境降低日志级别 (Debug → Warning)
5. **协程使用**: 异步操作使用协程避免阻塞主线程

### 10.5 调试技巧

#### 启用详细日志

```csharp
[Log] // 自动注入 _log 字段
public partial class MyClass : Node
{
    public override void _Ready()
    {
        _log.Debug("调试信息");
        _log.Info("一般信息");
        _log.Warn("警告");
        _log.Error("错误");
    }
}
```

#### 可视化调试工具

Godot Debug菜单:
- Visible Collision Shapes → 显示绿色碰撞轮廓
- Visible Navigation → 显示导航网格
- Sync Draw → 同步绘制
- FPS → 显示帧率计数er

---

## 📚 附录

### A. 关键文件路径索引

| 功能 | 文件路径 |
|------|---------|
| 游戏入口 | `global/GameEntryPoint.cs` |
| 场景路由 | `scripts/core/scene/SceneRouter.cs` |
| UI路由 | `scripts/core/ui/UiRouter.cs` |
| 全局输入 | `global/GlobalInputController.cs` |
| 音频管理 | `global/AudioManager.cs` |
| 过渡动画 | `global/SceneTransitionManager.cs` |
| 玩家移动 | `scripts/player/PlayerMovementController.cs` |
| 玩家数据 | `scripts/data/PlayerDataManager.cs` |
| 玩家数据模型 | `scripts/data/model/PlayerData.cs` |
| 开发约束 | `docs/Agent输入限制条件.md` |

### B. 常用命令速查

```bash
# 编译项目
dotnet build

# 清理并重建
dotnet clean && dotnet build

# 查看详细警告
dotnet build /warnaserror
```

### C. 版本历史

| 版本 | 日期 | 变更内容 |
|------|------|----------|
| **3.0.0** | 2026-05-11 | **重大整合**: 合并10份文档为统一指南 |
| 2.x | 2026-05-07~10 | 各专项文档迭代更新 |
| 1.0.0 | 2026-05-06 | 初始版本：基础架构和开发流程 |

---

> **文档维护说明**  
> 本文档是项目唯一权威的技术指南，整合了之前分散的10份文档。  
> 后续所有技术更新应直接在本文档中进行，无需创建新的独立文档。  
> 
> **适用项目**: GFramework-Godot-Template  
> **技术栈**: Godot 4.6 + .NET 10.0 + GFramework 0.0.205  
> **最后整合时间**: 2026-05-11
