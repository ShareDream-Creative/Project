# GFramework-Godot-Template 项目开发流程文档

> **版本**: 1.0.0  
> **最后更新**: 2026-05-06  
> **适用范围**: 所有项目开发团队成员  
> **框架版本**: GFramework 0.0.205 + Godot Engine 4.6 + .NET 10.0

---

## 📋 目录

1. [项目整体开发流程概述](#1-项目整体开发流程概述)
2. [接口设计规范与实现方法](#2-接口设计规范与实现方法)
3. [框架设计原则与文件存放规范](#3-框架设计原则与文件存放规范)
4. [编程规范与最佳实践](#4-编程规范与最佳实践)
5. [开发环境配置指南](#5-开发环境配置指南)
6. [常见问题与解决方案](#6-常见问题与解决方案)

---

## 1. 项目整体开发流程概述

### 1.1 技术栈概览

| 组件 | 版本 | 用途 |
|------|------|------|
| **Godot Engine** | 4.6.1 | 游戏引擎基础 |
| **.NET SDK** | 10.0 | C#运行时环境 |
| **GFramework** | 0.0.205 | 游戏架构框架 |
| **Mediator** | 3.0.1 | CQRS中介者模式 |
| **Scriban** | 7.0.1 | 模板引擎 |

### 1.2 架构模式总览

本项目采用 **GFramework** 架构，核心设计模式包括：

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

### 1.3 开发工作流

#### 阶段一：需求分析与设计
```
需求文档 → 功能拆解 → 接口设计 → 数据模型设计 → UI/UX原型
```

#### 阶段二：开发实施步骤

**Step 1: 创建枚举定义**
```csharp
// 文件位置: scripts/enums/{模块名}/
// 示例: scripts/enums/poker/StateType.cs
public enum StateType
{
    Idle,
    Drag,
    // ... 其他状态
}
```

**Step 2: 定义接口契约**
```csharp
// 文件位置: scripts/{功能模块}/I{接口名}.cs
// 示例: scripts/poker/IPoker.cs
public interface IPoker
{
    Guid GetId();
    void SetGlobalPosition(Vector2 pos);
    // ...
}
```

**Step 3: 实现CQRS命令/查询**
```csharp
// 命令定义: scripts/cqrs/{领域}/command/
// 处理器实现: 同目录下的 *CommandHandler.cs
public class PauseGameCommand : ICommand { }
public class PauseGameCommandHandler : AbstractCommandHandler<PauseGameCommand>
{
    public override ValueTask<Unit> Handle(PauseGameCommand command, CancellationToken ct)
    {
        // 业务逻辑实现
    }
}
```

**Step 4: 实现状态管理**
```csharp
// 文件位置: scripts/core/state/impls/
public class PlayingState : AsyncContextAwareStateBase
{
    public override async Task OnEnterAsync(IState? from)
    {
        await this.GetSystem<IUiRouter>()!.ReplaceAsync(HomeUi.UiKeyStr);
    }
}
```

**Step 5: 创建场景/UI控制器**
```csharp
// 使用模板快速生成: script_templates/Node/SimpleSceneControllerTemplate.cs
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

**Step 6: 配置资源注册**
```csharp
// 在 GameEntryPoint._Ready() 中注册:
foreach (var config in GameSceneConfigs) 
    _sceneRegistry.Registry(config);

foreach (var config in UiPageConfigs) 
    _uiRegistry.Registry(config);
```

#### 阶段三：测试与验证
- 单元测试：验证核心业务逻辑
- 集成测试：验证场景切换和UI交互
- 性能测试：确保帧率和内存使用正常

---

## 2. 接口设计规范与实现方法

### 2.1 核心接口体系

#### 2.1.1 场景接口 (IScene / ISimpleScene)

```csharp
/// <summary>
/// 完整场景生命周期接口（适用于复杂场景）
/// </summary>
public interface IScene
{
    ValueTask OnLoadAsync(ISceneEnterParam? param);   // 场景加载
    ValueTask OnEnterAsync();                           // 进入场景
    ValueTask OnPauseAsync();                           // 暂停场景
    ValueTask OnResumeAsync();                          // 恢复场景
    ValueTask OnExitAsync();                            // 退出场景
    ValueTask OnUnloadAsync();                          // 卸载场景
}

/// <summary>
/// 简化场景接口（提供默认空实现）
/// 适用场景：无需复杂生命周期管理的简单场景
/// </summary>
public interface ISimpleScene : IScene
{
    // 所有方法已提供默认空实现
}
```

**使用示例：**
```csharp
[ContextAware]
[Log]
public partial class MainMenu : Node2D, IController, ISceneBehaviorProvider, ISimpleScene
{
    public static string SceneKeyStr => nameof(SceneKey.Main);
    
    public override void _Ready()
    {
        // 初始化逻辑（可选重写）
    }
}
```

#### 2.1.2 UI页面接口 (IUiPage / ISimpleUiPage)

```csharp
/// <summary>
/// UI页面生命周期接口
/// </summary>
public interface IUiPage
{
    void OnExit();                                      // 页面退出
    void OnPause();                                     // 页面暂停
    void OnResume();                                    // 页面恢复
    void OnShow();                                      // 页面显示
    void OnHide();                                      // 页面隐藏
    void OnEnter(IUiPageEnterParam? param);             // 页面进入
}

/// <summary>
/// 简化UI页面接口（提供默认空实现）
/// </summary>
public interface ISimpleUiPage : IUiPage
{
    // 所有方法已提供默认空实现
}
```

**使用示例：**
```csharp
[ContextAware]
[Log]
public partial class HomeUi : Control, ISimpleUiPage
{
    public const string UiKeyStr = "HomeUi";
    
    // 可选择性重写生命周期方法
}
```

#### 2.1.3 状态机接口 (IState / IStateMachine)

```csharp
/// <summary>
/// 状态接口 - 定义状态的基本行为
/// </summary>
public interface IState
{
    string Name { get; }                                // 状态名称
}

/// <summary>
/// 异步上下文感知状态基类（推荐使用）
/// 提供自动的依赖注入能力
/// </summary>
public abstract class AsyncContextAwareStateBase : IState
{
    public abstract Task OnEnterAsync(IState? from);     // 进入状态
    public virtual Task OnExitAsync(IState? to);         // 退出状态（可重写）
    
    // 通过 this.GetSystem<T>() 获取系统服务
    // 通过 this.GetModel<T>() 获取模型服务
    // 通过 this.GetUtility<T>() 获取工具服务
}
```

**状态机实现示例：**
```csharp
// 定义状态
public class BootStartState : AsyncContextAwareStateBase
{
    public override async Task OnEnterAsync(IState? from)
    {
        var sceneRouter = this.GetSystem<ISceneRouter>()!;
        await sceneRouter.ChangeAsync(SceneKey.Main.ToString());
    }
}

// 状态切换调用
this.GetSystem<IStateMachineSystem>()!
    .ChangeToAsync<BootStartState>()
    .ToCoroutineEnumerator()
    .RunCoroutine();
```

#### 2.1.4 CQRS接口规范

**命令 (Command) 设计规范：**
```csharp
/// ✅ 正确示范: 命令对象应为简单数据载体
public class PauseGameCommand : ICommand
{
    // 无需属性，仅表示意图
}

/// ✅ 带输入参数的命令
public class ChangeBgmVolumeCommand : ICommand<float>
{
    // 泛型参数表示输入类型
}

/// ✅ 复杂输入使用 Input 对象
public class ChangeResolutionCommand : ICommand<ChangeResolutionCommandInput>

public record ChangeResolutionCommandInput(Vector2I Resolution);
```

**处理器 (Handler) 实现规范：**
```csharp
/// ✅ 正确示范: 继承 AbstractCommandHandler
public class PauseGameCommandHandler : AbstractCommandHandler<PauseGameCommand>
{
    private IStateMachineSystem? _stateMachineSystem;
    
    public override ValueTask<Unit> Handle(
        PauseGameCommand command, 
        CancellationToken cancellationToken)
    {
        // 业务逻辑实现
        GameUtil.GetTree().Paused = true;
        
        (_stateMachineSystem ??= this.GetSystem<IStateMachineSystem>())!
            .ChangeToAsync<PausedState>()
            .ToCoroutineEnumerator()
            .RunCoroutine();
            
        return ValueTask.FromResult(Unit.Value);
    }
}
```

**事件 (Event) 设计规范：**
```csharp
/// ✅ 领域事件定义
public record BgmChangedEvent(float Volume) : INotification;

/// ✅ 事件处理器
public class BgmChangedHandler : INotificationHandler<BgmChangedEvent>
{
    public async ValueTask Handle(BgmChangedEvent notification, CancellationToken ct)
    {
        // 事件处理逻辑
    }
}
```

**查询 (Query) 设计规范：**
```csharp
/// ✅ 查询定义
public record GetCurrentSettingsQuery : IQuery<SettingsView>;

/// ✅ 查询处理器
public class GetCurrentSettingsQueryHandler 
    : AbstractQueryHandler<GetCurrentSettingsQuery, SettingsView>
{
    public override async ValueTask<SettingsView> Handle(
        GetCurrentSettingsQuery query, 
        CancellationToken ct)
    {
        // 返回查询结果
        return new SettingsView { /* ... */ };
    }
}
```

### 2.2 接口命名约定

| 类型 | 命前缀 | 示例 |
|------|--------|------|
| 接口 | `I` + 名词 | `IPoker`, `IScene`, `IAudioSystem` |
| 命令 | 动词 + 名词 + `Command` | `PauseGameCommand`, `ChangeVolumeCommand` |
| 命令处理器 | 命令名 + `Handler` | `PauseGameCommandHandler` |
| 事件 | 名词/动词 + `Event` | `BgmChangedEvent`, `PlaySfxEvent` |
| 事件处理器 | 事件名 + `Handler` | `BgmChangedHandler` |
| 查询 | 动词/形容词 + `Query` | `GetCurrentSettingsQuery` |
| 查询处理器 | 查询名 + `Handler` | `GetCurrentSettingsQueryHandler` |
| 输入参数 | 命令名 + `Input` | `ChangeResolutionCommandInput` |

---

## 3. 框架设计原则与文件存放规范

### 3.1 目录结构详解

```
Project/
├── .github/                     # GitHub CI/CD 配置
│   └── workflows/               # 自动化工作流
├── addons/                      # Godot 插件配置
├── assets/                      # 游戏资源文件
│   ├── art/                     # 美术资源
│   │   └── textures/            # 纹理图片
│   ├── audio/                   # 音频资源
│   │   └── music/               # 背景音乐
│   ├── data/                    # 数据文件
│   ├── fonts/                   # 字体文件
│   ├── music/                   # 音乐文件（备用）
│   ├── shader/                  # 着色器程序
│   └── sound/                   # 音效文件
├── global/                      # 全局单例节点 (Autoload)
│   ├── *.cs                     # 全局脚本
│   └── *.tscn                   # 全局场景
├── resource/                    # 项目级资源配置
│   ├── shader/                  # 共享着色器
│   └── theme/                   # UI主题
├── scenes/                      # 场景文件
│   ├── component/               # 可复用组件场景
│   ├── credits/                 # 致谢界面
│   ├── intro/                   # 开场动画
│   ├── main_menu/               # 主菜单
│   ├── options_menu/            # 设置菜单
│   ├── pause_menu/              # 暂停菜单
│   ├── poker/                   # 扑克游戏
│   ├── tests/                   # 测试场景
│   └── main.tscn                # 主场景
├── script_templates/            # 自定义脚本模板
│   └── Node/                    # 节点类型模板
├── scripts/                     # C# 脚本代码
│   ├── GlobalUsings.cs          # 全局命名空间导入
│   ├── component/               # UI组件脚本
│   ├── constants/               # 常量定义
│   ├── core/                    # 核心框架代码 ⭐
│   │   ├── audio/               # 音频系统
│   │   │   └── system/          # 音频系统实现
│   │   ├── controller/          # 控制器
│   │   ├── environment/         # 环境配置
│   │   ├── resource/            # 资源配置类
│   │   ├── scene/               # 场景路由
│   │   ├── state/               # 状态管理
│   │   │   └── impls/           # 状态实现
│   │   ├── ui/                  # UI路由
│   │   └── utils/               # 工具类
│   ├── cqrs/                    # CQRS 实现 ⭐
│   │   ├── audio/               # 音频相关CQRS
│   │   ├── game/                # 游戏核心CQRS
│   │   ├── global/              # 全局事件CQRS
│   │   ├── graphics/            # 图形设置CQRS
│   │   ├── menu/                # 菜单CQRS
│   │   ├── pause_menu/          # 暂停菜单CQRS
│   │   ├── poker/               # 扑克游戏CQRS
│   │   ├── scene/               # 场景事件CQRS
│   │   └── setting/             # 设置CQRS
│   ├── credits/                 # 致谢页脚本
│   ├── data/                    # 数据层
│   │   ├── interfaces/          # 数据接口
│   │   └── model/               # 数据模型
│   ├── entities/                # 实体定义
│   ├── enums/                   # 枚举定义 ⭐
│   │   ├── audio/               # 音频枚举
│   │   ├── poker/               # 扑克枚举
│   │   ├── resources/           # 资源枚举
│   │   ├── scene/               # 场景枚举
│   │   ├── settings/            # 设置枚举
│   │   └── ui/                  # UI枚举
│   ├── intro/                   # 开场脚本
│   ├── main_menu/               # 主菜单脚本
│   ├── module/                  # 模块注册 ⭐
│   ├── options_menu/            # 设置菜单脚本
│   ├── pause_menu/              # 暂停菜单脚本
│   ├── poker/                   # 扑克游戏逻辑
│   │   └── state/               # 扑克状态机
│   ├── setting/                 # 设置系统
│   ├── stateMachine/            # 状态机接口
│   ├── tests/                   # 测试脚本
│   └── utility/                 # 工具类
├── .editorconfig                # 编辑器配置
├── project.godot                # Godot 项目配置
├── *.csproj                     # .NET 项目文件
└── *.sln                        # 解决方案文件
```

### 3.2 核心组件位置约束

#### 3.2.1 必须位于 `scripts/core/` 的组件

| 组件类型 | 目录路径 | 说明 |
|----------|----------|------|
| **架构类** | `scripts/core/GameArchitecture.cs` | 游戏架构核心 |
| **场景路由** | `scripts/core/scene/SceneRouter.cs` | 场景切换管理 |
| **UI路由** | `scripts/core/ui/UiRouter.cs` | UI页面栈管理 |
| **音频系统** | `scripts/core/audio/system/` | 音频播放控制 |
| **状态实现** | `scripts/core/state/impls/` | 游戏全局状态 |
| **环境配置** | `scripts/core/environment/` | 开发/生产环境 |
| **资源配置** | `scripts/core/resource/` | SceneConfig, UiPageConfig等 |
| **控制器** | `scripts/core/controller/` | 输入控制器 |

**约束规则：**
- ❌ 禁止在业务逻辑中直接实例化这些核心组件
- ✅ 必须通过依赖注入获取：`this.GetSystem<T>()`, `this.GetModel<T>()`, `this.GetUtility<T>()`
- ✅ 核心组件注册统一在 `scripts/module/` 的 Module 类中完成

#### 3.2.2 CQRS 文件组织规范

**标准目录结构：**
```
scripts/cqrs/{领域名}/
├── command/                     # 命令
│   ├── input/                   # 命令输入参数（可选）
│   │   └── XxxCommandInput.cs
│   ├── XxxCommand.cs            # 命令定义
│   └── XxxCommandHandler.cs     # 命令处理器
├── query/                       # 查询（可选）
│   ├── view/                    # 查询返回视图模型（可选）
│   │   └── XxxView.cs
│   ├── XxxQuery.cs              # 查询定义
│   └── XxxQueryHandler.cs       # 查询处理器
└── events/                      # 事件（可选）
    ├── XxxEvent.cs              # 事件定义
    └── XxxHandler.cs            # 事件处理器
```

**现有领域划分示例：**
- `audio/` - 音频控制（音量调节、音效播放）
- `game/` - 游戏核心（暂停、恢复、退出）
- `graphics/` - 图形设置（分辨率、全屏）
- `setting/` - 系统设置（语言、保存、重置）
- `menu/` - 菜单操作
- `pause_menu/` - 暂停菜单
- `poker/` - 扑克游戏逻辑

#### 3.2.3 枚举定义规范

**存放位置**: `scripts/enums/{分类}/{枚举名}.cs`

```csharp
namespace GFrameworkGodotTemplate.scripts.enums.scene;

/// <summary>
/// 场景键值枚举 - 用于标识和路由到不同场景
/// </summary>
public enum SceneKey
{
    Boot,           // 启动场景
    Main,           // 主场景
    Scene1,         // 测试场景1
    Scene2,         // 测试场景2
    Home            // 主页场景
}
```

**必须维护同步性：**
1. 枚举项添加后必须在对应的 Config 类中更新
2. 在 `GameEntryPoint` 中注册新的配置
3. 更新 `scenes/` 目录下对应的场景文件

#### 3.2.4 模块注册规范

**模块文件位置**: `scripts/module/{Module名}Module.cs`

```csharp
/// ✅ 正确示范: SystemModule
public class SystemModule : IArchitectureModule
{
    public void Install(IArchitecture architecture)
    {
        architecture.RegisterSystem(new UiRouter());
        architecture.RegisterSystem(new SceneRouter());
        architecture.RegisterSystem(new SettingsSystem());
        architecture.RegisterSystem(new GodotAudioSystem());
    }
}
```

**四大核心模块：**

| 模块名 | 注册内容 | 职责 |
|--------|----------|------|
| **UtilityModule** | 工具类 (TextureRegistry等) | 提供基础设施服务 |
| **ModelModule** | 数据模型 (SettingsModel等) | 管理数据和持久化 |
| **SystemModule** | 系统服务 (Router, Audio等) | 核心业务系统 |
| **StateModule** | 状态机系统 | 管理游戏状态流转 |

### 3.3 全局节点 (Autoload) 规范

**存放位置**: `global/` 目录

**当前全局节点列表：**

| 节点名 | 类型 | 职责 |
|--------|------|------|
| **GameEntryPoint** | Node | 游戏入口点，初始化架构 |
| **SceneRoot** | Node | 场景根节点容器 |
| **UiRoot** | Node | UI根节点容器 |
| **GlobalInputController** | Node | 全局输入管理 |
| **AudioManager** | Node | 音频管理器 |
| **SceneTransitionManager** | Node | 场景转场动画管理 |

**访问方式：**
```csharp
// 通过静态属性访问
var arch = GameEntryPoint.Architecture;
var tree = GameEntryPoint.Tree;

// 或通过单例模式访问（如果已实现）
var instance = SceneManager.Instance;
```

---

## 4. 编程规范与最佳实践

### 4.1 命名规范总览

#### 4.1.1 基本命名规则

| 元素 | 规则 | 示例 |
|------|------|------|
| **变量** | camelCase | `playerName`, `_sceneRegistry` |
| **常量** | UPPER_SNAKE_CASE | `MAX_PLAYERS`, `DEFAULT_VOLUME` |
| **属性** | PascalCase | `SceneKey`, `Volume` |
| **方法** | PascalCase | `GetId()`, `ResetPos()` |
| **类/结构** | PascalCase | `PokerStateMachine`, `SceneConfig` |
| **接口** | I + PascalCase | `IPoker`, `ISimpleScene` |
| **文件夹/文件** | snake_case | `game_entry_point.cs`, `volume_container.tscn` |
| **私有字段** | _camelCase | `_stateMachineSystem`, `_log` |
| **静态只读** | PascalCase | `SceneKeyStr`, `UiKeyStr` |

#### 4.1.2 特殊命名约定

```csharp
// ✅ 场景键字符串常量
public static string SceneKeyStr => nameof(SceneKey.MyScene);

// ✅ UI键字符串常量  
public const string UiKeyStr = "HomeUi";

// ✅ 延迟初始化私有字段
private IStateMachineSystem? _stateMachineSystem;

// ✅ 空接口合并模式（??= 操作符）
(_stateMachineSystem ??= this.GetSystem<IStateMachineSystem>())!;
```

### 4.2 注释与文档规范

#### 4.2.1 XML文档注释要求

**公共API必须包含完整注释：**

```csharp
/// <summary>
/// 暂停游戏命令处理器类，负责处理暂停游戏的命令逻辑
/// 继承自AbstractCommandHandler，专门处理PauseGameCommand类型的命令
/// </summary>
public class PauseGameCommandHandler : AbstractCommandHandler<PauseGameCommand>
{
    /// <summary>
    /// 状态机系统实例，用于切换状态
    /// </summary>
    private IStateMachineSystem? _stateMachineSystem;

    /// <summary>
    /// 处理暂停游戏命令的核心方法
    /// 通过设置游戏树的暂停状态并切换到暂停状态来实现游戏暂停功能
    /// </summary>
    /// <param name="command">暂停游戏命令对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>表示异步操作完成的ValueTask，返回Unit值表示无返回结果</returns>
    public override ValueTask<Unit> Handle(
        PauseGameCommand command, 
        CancellationToken cancellationToken)
    {
        // 实现...
    }
}
```

**注释语言要求：**
- ✅ 使用中文注释（与项目团队语言一致）
- ✅ `<summary>` 简明扼要描述用途
- ✅ `<param>` 说明每个参数含义
- ✅ `<returns>` 说明返回值
- ✅ `<remarks>` 补充说明注意事项（可选）

### 4.3 属性装饰器规范

#### 4.3.1 必须使用的特性

```csharp
// ✅ 所有需要日志记录的类
[Log]

// ✅ 所有需要依赖注入的类（Controller、State、Handler）
[ContextAware]

// ✅ Godot导出属性（可在编辑器中配置）
[Export] 
public Array<UiPageConfig> UiPageConfigs { get; set; } = null!;

// ✅ Godot全局类（可在编辑器中作为节点类型）
[GlobalClass]
public partial class SceneConfig : Resource, IKeyValue<string, PackedScene>
```

#### 4.3.2 特性使用组合

**场景控制器标准模板：**
```csharp
using GFramework.Core.Abstractions.Controller;
using GFramework.Game.Abstractions.Scene;
using GFramework.Godot.Scene;
using GFramework.SourceGenerators.Abstractions.Logging;
using GFramework.SourceGenerators.Abstractions.Rule;
using GFrameworkGodotTemplate.scripts.core.scene;
using GFrameworkGodotTemplate.scripts.enums.scene;
using Godot;

[ContextAware]  // 启用依赖注入
[Log]           // 启用自动日志
public partial class MyScene : Node2D, IController, ISceneBehaviorProvider, ISimpleScene
{
    // 实现...
}
```

### 4.4 异步编程规范

#### 4.4.1 异步方法选择指南

| 返回类型 | 使用场景 | 示例 |
|----------|----------|------|
| `Task<T>` | 需要返回结果的异步操作 | `Task<IScene> GetScene()` |
| `ValueTask<T>` | 可能同步完成的异步操作 | `ValueTask<Unit> Handle()` |
| `Task` | 无返回值的异步操作 | `Task OnEnterAsync()` |
| `ValueTask` | 可能同步完成的void异步操作 | `ValueTask OnLoadAsync()` |

#### 4.4.2 ConfigureAwait 使用规范

```csharp
// ✅ 在库代码中使用 ConfigureAwait(true) 或不指定
public override async Task OnEnterAsync(IState? from)
{
    await this.GetSystem<IUiRouter>()!.ReplaceAsync(HomeUi.UiKeyStr).ConfigureAwait(true);
}

// ✅ 在应用层代码中使用 ConfigureAwait(false)（如有需要）
await someService.DoWorkAsync().ConfigureAwait(false);
```

#### 4.4.3 协程使用规范

```csharp
// ✅ 将异步状态切换转换为协程执行
this.GetSystem<IStateMachineSystem>()!
    .ChangeToAsync<PausedState>()
    .ToCoroutineEnumerator()  // 转换为协程枚举器
    .RunCoroutine();          // 在Godot主线程执行协程
```

### 4.5 依赖注入使用规范

#### 4.5.1 三种服务获取方式

```csharp
// 1️⃣ 获取系统服务（业务逻辑系统）
var sceneRouter = this.GetSystem<ISceneRouter>()!;
var uiRouter = this.GetSystem<IUiRouter>()!;
var stateMachine = this.GetSystem<IStateMachineSystem>()!;

// 2️⃣ 获取模型服务（数据管理层）
var settingsModel = this.GetModel<ISettingsModel>()!;
var saveData = this.GetModel<IGameSaveData>()!;

// 3️⃣ 获取工具服务（基础设施层）
var textureRegistry = this.GetUtility<IGodotTextureRegistry>()!;
var sceneRegistry = this.GetUtility<IGodotSceneRegistry>()!;
```

#### 4.5.2 服务生命周期

| 服务类型 | 生命周期 | 注册位置 |
|----------|----------|----------|
| **System** | Singleton | `*Module.Install()` |
| **Model** | Singleton | `*Module.Install()` |
| **Utility** | Singleton | `*Module.Install()` |
| **Mediator** | Singleton | `GameArchitecture.Configurator` |

### 4.6 错误处理规范

#### 4.6.1 空引用处理

```csharp
// ✅ 使用 null-forgiving operator (!) 当确定非null时
_architecture.Initialize();

// ✅ 使用 null-coalescing assignment 进行延迟初始化
(_stateMachineSystem ??= this.GetSystem<IStateMachineSystem>())!

// ✅ 使用可空类型声明可能为空的字段
private IGodotSceneRegistry _sceneRegistry = null!;
```

#### 4.6.2 取消令牌传递

```csharp
// ✅ 所有异步方法都应接受 CancellationToken
public override ValueTask<Unit> Handle(
    PauseGameCommand command, 
    CancellationToken cancellationToken)
{
    // 在长时间操作中检查取消请求
    cancellationToken.ThrowIfCancellationRequested();
}
```

### 4.7 Git工作流规范

#### 4.7.1 分支策略

```
main (生产分支)
  ↑
develop (开发分支)
  ↑
feature/xxx (功能分支)
hotfix/xxx (紧急修复分支)
```

#### 4.7.2 Commit Message 格式

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Type 类型：**
- `feat`: 新功能
- `fix`: Bug修复
- `docs`: 文档更新
- `style`: 代码格式调整
- `refactor`: 重构
- `test`: 测试相关
- `chore`: 构建/工具链

**示例：**
```
feat(poker): add drag state for card interaction

Implement the drag state machine for poker cards,
allowing users to drag and drop cards on the table.

Closes #123
```

### 4.8 性能优化建议

#### 4.8.1 内存管理

```csharp
// ✅ 使用延迟初始化避免不必要的内存分配
private ISceneBehavior? _scene;

public ISceneBehavior GetScene()
{
    _scene ??= SceneBehaviorFactory.Create<Node2D>(this, SceneKeyStr);
    return _scene;
}

// ✅ 及时释放大型资源
public override void _ExitTree()
{
    _scene?.Dispose();
    base._ExitTree();
}
```

#### 4.8.2 Godot特定优化

```csharp
// ✅ 使用 CallDeferred 避免帧内修改场景树
CallDeferred(nameof(CallDeferredInit));

// ✅ 避免在 _Process 中进行重度计算
// 将计算移至后台线程或使用协程分帧处理

// ✅ 合理使用 Export 减少运行时查找
[Export]
public Array<SceneConfig> GameSceneConfigs { get; set; } = null!;
```

### 4.9 安全性注意事项

#### 4.9.1 敏感信息保护

```csharp
// ❌ 禁止硬编码敏感信息
string apiKey = "12345abcde";

// ✅ 使用环境变量或加密存储
string apiKey = Environment.GetEnvironmentVariable("API_KEY");

// ✅ 使用 Godot 的加密存储
var encrypted = ProjectSettings.LoadEncrypted("save.enc", "encryption_key");
```

#### 4.9.2 输入验证

```csharp
// ✅ 验证所有外部输入
public void SetGlobalPosition(Vector2 pos)
{
    if (!IsValidPosition(pos))
        throw new ArgumentException("Invalid position", nameof(pos));
        
    // 处理逻辑...
}
```

---

## 5. 开发环境配置指南

### 5.1 必要软件清单

| 软件 | 最低版本 | 推荐版本 | 用途 |
|------|----------|----------|------|
| **.NET SDK** | 10.0.x | 10.0.203+ | C#编译和运行时 |
| **Godot Editor** | 4.6.stable | 4.6.1+ | 可视化编辑器 |
| **Visual Studio Code** | 最新版 | - | 代码编辑（推荐） |
| **Git** | 2.x | 最新版 | 版本控制 |

### 5.2 环境搭建步骤

#### Step 1: 克隆仓库
```bash
git clone https://github.com/ShareDream-Creative/Project.git
cd Project
```

#### Step 2: 还原依赖
```bash
dotnet restore
# 预期输出: 还原完成(4.5)
```

#### Step 3: 构建项目
```bash
dotnet build
# 预期输出: 成功，出现 0 错误（可能有警告）
```

#### Step 4: 打开编辑器
```bash
# 方式1: 双击 project.godot 文件
# 方式2: 命令行启动（如果Godot已在PATH中）
godot --editor --path .
```

### 5.3 IDE 配置推荐

#### VS Code 扩展推荐

1. **C# Dev Kit** - IntelliSense和调试支持
2. **Godot Tools** - Godot集成开发工具
3. **EditorConfig** - 代码格式统一
4. **GitLens** - Git增强功能

#### VS Code settings.json 配置
```json
{
    "omnisharp.enableImportCompletion": true,
    "dotnet.codeAnalysis.enableAllAnalyzers": true,
    "editor.formatOnSave": true,
    "[csharp]": {
        "editor.defaultFormatter": "ms-dotnettools.csharp"
    }
}
```

### 5.4 常用开发命令

```bash
# 构建调试版本
dotnet build -c Debug

# 构建发布版本
dotnet build -c Release

# 清理构建产物
dotnet clean

# 运行单元测试（如果有）
dotnet test

# 格式化代码（需安装dotnet-format）
dotnet format

# 分析代码质量
dotnet analyze
```

---

## 6. 常见问题与解决方案

### Q1: 如何添加新的游戏场景？

**完整步骤：**
1. 在 `scripts/enums/scene/SceneKey.cs` 添加新枚举项
2. 在 `scenes/` 创建新的 `.tscn` 场景文件
3. 在 `scripts/` 下创建控制器脚本（使用模板）
4. 创建 `SceneConfig` 资源并在编辑器中配置
5. 在 `GameEntryPoint` 中注册新配置

**代码示例：**
```csharp
// 1. 添加枚举
public enum SceneKey
{
    // ... 已有项
    NewScene  // 新增
}

// 2. 创建控制器
[ContextAware][Log]
public partial class NewScene : Node2D, IController, ISceneBehaviorProvider, ISimpleScene
{
    public static string SceneKeyStr => nameof(SceneKey.NewScene);
}

// 3. 在 GameEntryPoint._Ready() 中会自动注册（如果在 GameSceneConfigs 数组中配置了）
```

### Q2: 如何添加新的UI页面？

**完整步骤：**
1. 在 `scripts/enums/ui/UiKey.cs` 添加新枚举项
2. 在 `scenes/component/` 或对应目录创建 `.tscn`
3. 创建继承 `Control` 并实现 `ISimpleUiPage` 的脚本
4. 创建 `UiPageConfig` 资源并配置
5. 在 `GameEntryPoint` 中注册

### Q3: 如何实现新的游戏状态？

**完整步骤：**
1. 在 `scripts/core/state/impls/` 创建新状态类
2. 继承 `AsyncContextAwareStateBase`
3. 重写 `OnEnterAsync` 和 `OnExitAsync` 方法
4. 在需要的地方通过 `IStateMachineSystem.ChangeToAsync<T>()` 触发转换

**示例：**
```csharp
public class VictoryState : AsyncContextAwareStateBase
{
    public override async Task OnEnterAsync(IState? from)
    {
        // 显示胜利UI
        await this.GetSystem<IUiRouter>()!.PushAsync(VictoryUi.UiKeyStr);
        
        // 播放胜利音效
        await this.SendCommandAsync(new PlaySfxEvent(SfxType.Victory));
    }
    
    public override async Task OnExitAsync(IState? to)
    {
        // 清理胜利状态
        await this.GetSystem<IUiRouter>()!.PopAsync();
    }
}
```

### Q4: Mediator警告如何解决？

**警告信息：**
```
warning MSG0005: MediatorGenerator found message without any registered handler
```

**解决方案：**
- 如果该消息确实不需要处理器：可以忽略此警告
- 如果需要处理器：创建对应的 Handler 类并实现正确的接口

### Q5: 如何调试状态转换问题？

**调试技巧：**
1. 启用 `[Log]` 特性的类会自动记录日志
2. 检查 `GameArchitecture` 的 Environment 配置
3. 使用 Godot 的远程调试功能
4. 在状态机的 `OnEnter`/OnExit` 方法中添加断点

### Q6: 协程和Task如何选择？

**选择指南：**
- 使用 **Task/async-await**：当需要等待异步操作完成时
- 使用 **协程 (Coroutine)**：当需要在多帧中执行或与Godot生命周期绑定时
- **混合使用**：通过 `.ToCoroutineEnumerator().RunCoroutine()` 转换

---

## 📚 附录

### A. 参考资源

- **GFramework官方文档**: https://github.com/GeWuYou/GFramework/tree/main/docs
- **Godot C# 官方文档**: https://docs.godotengine.org/en/stable/tutorials/scripting/csharp/index.html
- **Mediator.Net 文档**: https://github.com/martintom/Mediator
- **项目README.md**: `/README.md`（包含基本命名规范）

### B. 快速参考卡片

**常用代码片段：**
```csharp
// 发送命令
await this.SendCommandAsync(new PauseGameCommand());

// 发送查询
var result = await this.SendQueryAsync(new GetCurrentSettingsQuery());

// 发布事件
await this.PublishEventAsync(new BgmChangedEvent(0.8f));

// 获取服务
var system = this.GetSystem<ISomeSystem>()!;

// 状态切换
this.GetSystem<IStateMachineSystem>()!
    .ChangeToAsync<MyState>()
    .ToCoroutineEnumerator()
    .RunCoroutine();

// UI导航
await this.GetSystem<IUiRouter>()!.PushAsync(SomeUi.UiKeyStr);  // 入栈
await this.GetSystem<IUiRouter>()!.PopAsync();                   // 出栈
await this.GetSystem<IUiRouter>()!.ReplaceAsync(NewUi.UiKeyStr); // 替换

// 场景切换
await this.GetSystem<ISceneRouter>()!.ChangeAsync(SceneKey.Main.ToString());
```

### C. 版本历史

| 版本 | 日期 | 作者 | 变更说明 |
|------|------|------|----------|
| 1.0.0 | 2026-05-06 | 开发团队 | 初始版本创建 |

---

## 📝 文档维护说明

本文档由项目架构师和维护团队共同维护。

**更新触发条件：**
- 新增核心框架组件时
- 修改目录结构规范时
- 引入新的编程模式时
- 团队成员反馈问题时

**审核流程：**
1. 技术负责人初审
2. 团队评审确认
3. 合入main分支

---

> **最后提醒**: 在开始编码之前，请务必通读本手册的相关章节！🎯
