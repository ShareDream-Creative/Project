# Agent 输入限制条件

## 📋 开发前置要求

在对程序进行修改前，需全面遍历并分析现有程序结构，明确各模块间的依赖关系。严格遵守以下开发原则：

### 核心开发原则

1. **代码保护原则**
   不得修改原有结构中的任何核心代码、业务逻辑及接口定义，仅允许修改错误提示信息的文本内容和新增功能代码
2. **测试验证原则**
   新增功能必须通过单元测试验证，确保不会破坏原有逻辑流程和功能稳定性
3. **文件约束原则**
   所有开发工作仅限于 C# 代码文件的修改，严禁对 `.tscn` 场景文件进行任何形式的编辑、调整或重命名
4. 未经用户同意不得推送到github：推送智能推送到c:\Users\dgh\Desktop\Dreamcreative\code\gamebulid\Project"; git status; git log --oneline origin/level\_building\_and\_the\_test

### Godot 引擎配置流程规范

当实现集体性的完整功能模块时，必须提供详细的 Godot 引擎配置流程，包括但不限于：

- 节点层级结构设计与属性设置
- 资源文件的导入路径与配置参数
- 场景间的关联方式与通信机制
- 关键参数的调整范围与最佳实践值
- 配置变更的版本控制策略

***

## 🔧 开发规范标准

作为专业的 Godot 游戏引擎开发工程师与 C# 游戏开发工程师，在开发过程中需严格遵循以下规范：

### 编码规范

- 符合 C# 编码规范（命名约定、代码格式、注释标准）
- 遵循 Godot 引擎的最佳实践（节点组织、信号使用、资源管理）
- 实施模块化与面向对象设计原则
- 编写清晰的代码注释与文档字符串

***

## 💻 技术框架版本规范

开发必须基于 GFramework 0.0.205、Godot Engine 4.6.1 及 .NET 10.0 版本组合进行，不得使用不兼容的版本或组件。

### 技术栈详细信息

| 组件               | 版本      | 用途                 |
| ---------------- | ------- | ------------------ |
| **Godot Engine** | 4.6.1   | 提供游戏引擎基础运行环境       |
| **.NET SDK**     | 10.0    | 提供 C# 语言运行时环境      |
| **GFramework**   | 0.0.205 | 提供游戏架构框架支持         |
| **Mediator**     | 3.0.1   | 实现 CQRS 设计模式的中介者功能 |
| **Scriban**      | 7.0.1   | 提供模板引擎功能支持         |

***

## 🌐 全局功能模块扩展流程

涉及全局控制、系统监控、状态管理及核心逻辑的功能模块，必须实现在 `global` 文件中，并遵循以下扩展流程：

1. 开发前进行功能需求与现有框架的匹配度分析
2. 优先检查现有功能是否可通过继承、接口实现或组合模式进行扩展
3. 严格依照约束 1 进行功能扩展，确保向后兼容性
4. 新增功能模块需添加明确的版本标识和作者信息,标注格式如下：

```csharp
/// <summary>
///  <para>新增功能模块名称</para>
/// <author>作者姓名</author>
/// <version>版本号</version>
/// <date>日期</date>
/// <description>功能描述</description>
/// <remarks>备注信息</remarks>
/// <input param="input">输入参数</input>(可选)
/// <output param="output">输出参数</output>(可选)
/// 
/// </summary>
```

***

## ⚠️ 接口与方法使用约束

开发过程中严禁虚构或假设不存在的接口、类或方法。代码逻辑变更必须严格遵循约束规定，仅使用结构框架内已定义的接口、类和方法。

### 功能扩展流程

若根据约束 1 和 5 分析后确定现有功能无法满足需求，则需：

1. 提交新功能集成方案文档，说明扩展必要性
2. 根据项目结构创建新功能模块并集成到框架中
3. 新增功能模块必须放置在指定的扩展目录下
4. 严禁删除或修改原框架代码，仅允许通过新增方式扩展功能
5. 添加扩展功能的单元测试，确保不影响原有功能的正常运行

***

## ✅ 约束检测机制

实施严格的约束检测机制，修改完成后必须进行：

1. **代码静态分析**：检查是否符合编码规范
2. **单元测试覆盖**：确保新功能与原有功能均正常运行
3. **集成测试验证**：检查模块间交互是否正常
4. **性能测试评估**：确保新增功能不会导致性能下降
5. **版本兼容性测试**：验证在指定技术框架版本下的运行稳定性
6. **编译验证**：使用控制台执行 `dotnet build` 确保无语法错误、命名冲突、类型定义错误等

确保代码无错误、无冲突且符合所有约束条件后，方可提交给用户。

***

## 📌 需求跟踪管理

开发过程中需严格依照用户输入需求进行，建立需求跟踪矩阵，每执行一次开发思考或功能实现步骤，均需参考用户输入需求进行核对，确保开发方向不偏离用户要求。

***

## 📝 文档更新规范

每次完成功能更新后，必须对项目 README 文档进行全面重构，包括：

1. 更新功能说明与使用方法
2. 同步更新游戏版本号（遵循语义化版本规范）
3. 修改版本历史记录
4. 补充新功能的配置说明
5. 更新依赖项版本信息

确保文档与实际功能保持一致。

***

## 🔍 现有功能复用策略

开发新功能时，应优先考虑使用现有功能模块实现。若存在可参考的现有功能，需：

1. 分析参考功能的实现方式与设计思路
2. 遵循其编码风格和架构模式
3. 复用已存在的工具类和辅助方法

若现有功能无法满足需求，则严格履行上述所有约束条件进行开发。

***

## 🏗️ 工程化扩展方法

采用工程化的设计方法，原有框架的基础代码可以通过以下方式进行拓展，但不能破坏原有结构：

1. 使用 `partial` 类扩展原有类功能
2. 实现接口定义新的功能模块
3. 使用装饰器模式包装现有功能
4. 通过配置文件扩展系统行为
5. 利用事件机制添加新的响应逻辑

> **重要提示**：所有扩展必须通过框架提供的扩展点进行，确保系统架构的完整性和稳定性。

***

## 📁 Scripts 目录结构与文件创建规范

本项目 `scripts` 目录包含 **16 个核心文件夹**，每个文件夹都有明确的职责分工和严格的文件创建规范。以下是详细的目录结构说明和文件组织规则。

### 总体架构概览

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

***

### 1️⃣ component/ - 组件目录

#### 📂 层级结构

```
component/
├── VolumeContainer.cs      # 音量容器组件示例
└── [Name]Component.cs     # 其他组件类
```

#### 🎯 功能描述

- **定位**：存储游戏中的容器抽象类和组件类
- **职责**：封装可复用的 UI 容器和功能组件
- **特点**：轻量级、可组合、高内聚

#### 📝 文件命名规则

- **格式**：`[Name]Component.cs`
- **示例**：`VolumeContainer.cs`, `HealthComponent.cs`, `InventoryComponent.cs`
- **类名规范**：PascalCase，以 "Component" 结尾
- **访问修饰符**：public class

#### 💾 存储要求

- ✅ 仅存放 `.cs` 脚本文件
- ✅ 类名必须以 `Component` 结尾
- ✅ 只储存 public 字段和方法
- ❌ 禁止存放业务逻辑代码
- ❌ 禁止存放数据模型

#### 🏗️ 代码模板

```csharp
using Godot;

namespace GFrameworkGodotTemplate.scripts.component;

/// <summary>
///     [组件功能描述]
/// </summary>
public partial class [Name]Component : [BaseType]
{
    // Public 字段
    public [FieldType] [FieldName];

    // Public 方法
    public void [MethodName]()
    {
        // 实现
    }
}
```

***

### 2️⃣ constants/ - 常量目录

#### 📂 层级结构

```
constants/
├── GameConstants.cs        # 游戏核心常量
├── UiLayers.cs             # UI 层级常量
└── [Name]Constants.cs      # 其他常量类
```

#### 🎯 功能描述

- **定位**：集中管理游戏中所有不可变的常量值
- **职责**：定义魔法数字、配置参数、阈值等常量
- **特点**：全局唯一、编译期确定、不可修改

#### 📝 文件命名规则

- **格式**：`[Name]Constants.cs`
- **示例**：`GameConstants.cs`, `UiLayers.cs`, `PhysicsConstants.cs`
- **类名规范**：PascalCase，以 "Constants" 结尾
- **访问修饰符**：public static class

#### 💾 存储要求

- ✅ 仅存放 `.cs` 脚本文件
- ✅ 类名必须以 `Constants` 结尾
- ✅ 只储存 `public const` 或 `public static readonly` 常量值
- ❌ 禁止包含方法（除简单计算属性外）
- ❌ 禁止包含可变状态

#### 🏗️ 代码模板

```csharp
namespace GFrameworkGodotTemplate.scripts.constants;

/// <summary>
///     [常量类别描述]
/// </summary>
public static class [Name]Constants
{
    /// <summary>
    ///     [常量描述]
    /// </summary>
    public const [Type] [CONSTANT_NAME] = [Value];

    /// <summary>
    ///     [常量描述]
    /// </summary>
    public static readonly [Type] [ConstantName] = [Value];
}
```

#### 📊 已有常量类参考

| 文件名                | 用途      | 包含内容                      |
| ------------------ | ------- | ------------------------- |
| `GameConstants.cs` | 游戏核心参数  | 分辨率、帧率、物理参数               |
| `UiLayers.cs`      | UI 层级定义 | Page, Popup, Overlay 等层级值 |

***

### 3️⃣ core/ - 核心框架目录

#### 📂 层级结构

```
core/
├── GameArchitecture.cs              # 游戏架构入口
├── audio/                           # 音频子系统
│   ├── system/
│   │   ├── IAudioSystem.cs          # 音频系统接口
│   │   └── GodotAudioSystem.cs      # Godot音频实现
├── controller/                      # 控制器
│   └── GameInputController.cs       # 游戏输入控制器
├── environment/                     # 环境配置
│   ├── GameDevEnvironment.cs        # 开发环境
│   └── GameMainEnvironment.cs       # 主环境
├── resource/                        # 资源配置类
│   ├── SceneConfig.cs               # 场景配置
│   ├── TextureConfig.cs             # 纹理配置
│   └── UiPageConfig.cs              # UI页面配置
├── scene/                           # 场景管理
│   ├── ISimpleScene.cs              # 场景接口
│   ├── SceneRouter.cs               # 场景路由
│   └── SceneTransitionAnimationHandler.cs  # 过渡动画
├── state/impls/                     # 状态实现
│   ├── BootStartState.cs            # 启动状态
│   ├── MainMenuState.cs             # 主菜单状态
│   ├── PlayingState.cs              # 游玩状态
│   ├── PausedState.cs               # 暂停状态
│   └── GameOverState.cs             # 结束状态
├── ui/                              # UI系统
│   ├── ISimpleUiPage.cs             # UI页面接口
│   ├── IUiPage.cs                   # UI页面基类
│   ├── UiFactory.cs                 # UI工厂
│   └── UiRouter.cs                  # UI路由
└── utils/                           # 工具类
    └── GameUtil.cs                  # 游戏工具
```

#### 🎯 功能描述

- **定位**：项目核心基础设施和框架代码
- **职责**：提供场景路由、UI 管理、状态机、音频系统等基础服务
- **特点**：高度抽象、框架级代码、跨模块复用

#### 📝 文件命名规则

- **接口**：`I[Name].cs`（以 I 开头）
- **实现类**：`[Name].cs` 或 `[Prefix][Name].cs`
- **配置类**：`[Name]Config.cs`
- **处理器**：`[Name]Handler.cs`

#### 💾 存储要求

- ✅ 框架级代码，修改需谨慎
- ✅ 接口优先，实现分离
- ✅ 必须包含完整 XML 文档注释
- ❌ 禁止直接编写业务逻辑
- ❌ 禁止引用具体业务模块

#### 🏗️ 子目录规范

##### audio/system/

- **用途**：音频播放系统
- **命名**：`I[AudioFeature]System.cs`, `Godot[AudioFeature]System.cs`

##### controller/

- **用途**：输入控制器
- **命名**：`[Scope]InputController.cs`

##### environment/

- **用途**：运行环境配置
- **命名**：`Game[EnvironmentName]Environment.cs`

##### resource/

- **用途**：资源配置数据类
- **命名**：`[ResourceType]Config.cs`

##### scene/

- **用途**：场景管理和路由
- **命名**：`ISimpleScene.cs`, `SceneRouter.cs`, `Scene[Feature].cs`

##### state/impls/

- **用途**：游戏状态实现
- **命名**：`[StateName]State.cs`

##### ui/

- **用途**：UI 页面系统和路由
- **命名**：`IUi[Type].cs`, `Ui[Component].cs`

##### utils/

- **用途**：核心工具类
- **命名**：`[Domain]Util.cs`

***

### 4️⃣ cqrs/ - CQRS 模式目录

#### 📂 层级结构

```
cqrs/
├── audio/                           # 音频领域
│   ├── command/                     # 命令
│   │   ├── ChangeBgmVolumeCommand.cs
│   │   ├── ChangeBgmVolumeCommandHandler.cs
│   │   ├── input/                   # 命令输入DTO
│   │   │   └── ChangeBgmVolumeCommandInput.cs
│   │   ├── ChangeMasterVolumeCommand.cs
│   │   ├── ChangeMasterVolumeCommandHandler.cs
│   │   ├── ChangeSfxVolumeCommand.cs
│   │   └── ChangeSfxVolumeCommandHandler.cs
│   └── events/                      # 事件
│       ├── BgmChangedEvent.cs
│       ├── BgmChangedHandler.cs
│       ├── PlaySfxEvent.cs
│       └── PlaySfxHandler.cs
├── game/                            # 游戏领域
│   └── command/
│       ├── ExitGameCommand.cs
│       ├── ExitGameCommandHandler.cs
│       ├── PauseGameCommand.cs
│       ├── PauseGameCommandHandler.cs
│       ├── PauseGameWithOpenPauseMenuCommand.cs
│       ├── ResumeGameCommand.cs
│       ├── ResumeGameCommandHandler.cs
│       ├── ResumeGameWithClosePauseMenuCommand.cs
│       └── ...
├── global/                          # 全局事件
│   └── events/
│       ├── UiRootReadyEvent.cs
│       └── UiRootReadyHandler.cs
├── graphics/                        # 图形领域
│   └── command/
│       ├── ChangeResolutionCommand.cs
│       ├── ChangeResolutionCommandHandler.cs
│       ├── ToggleFullscreenCommand.cs
│       ├── ToggleFullscreenCommandHandler.cs
│       └── input/
│           ├── ChangeResolutionCommandInput.cs
│           └── ToggleFullscreenCommandInput.cs
├── menu/                            # 菜单领域
│   └── command/
│       ├── OpenOptionsMenuCommand.cs
│       └── OpenOptionsMenuCommandHandler.cs
├── pause_menu/                      # 暂停菜单领域
│   └── command/
│       ├── ClosePauseMenuCommand.cs
│       ├── ClosePauseMenuCommandHandler.cs
│       ├── OpenPauseMenuCommand.cs
│       ├── OpenPauseMenuCommandHandler.cs
│       └── input/
│           ├── ClosePauseMenuCommandInput.cs
│           └── OpenPauseMenuCommandInput.cs
├── poker/                           # 扑克领域
│   └── event/
│       └── StateChangedEvent.cs
├── scene/                           # 场景领域
│   └── events/
│       └── SceneRootReadyEvent.cs
└── setting/                         # 设置领域
    ├── command/
    │   ├── ChangeLanguageCommand.cs
    │   ├── ChangeLanguageCommandHandler.cs
    │   ├── ResetAllSettingsCommand.cs
    │   ├── ResetAllSettingsCommandHandler.cs
    │   ├── SaveSettingsCommand.cs
    │   ├── SaveSettingsCommandHandler.cs
    │   └── input/
    │       └── ChangeLanguageCommandInput.cs
    └── query/
        ├── GetCurrentSettingsQuery.cs
        ├── GetCurrentSettingsQueryHandler.cs
        └── view/
            └── SettingsView.cs
```

#### 🎯 功能描述

- **定位**：实现 CQRS（命令查询职责分离）设计模式
- **职责**：解耦命令执行、事件发布、查询处理
- **特点**：领域驱动、事件驱动、松耦合

#### 📝 文件命名规则

**命令（Command）**：

- 格式：`[Action][Target]Command.cs`
- 示例：`ChangeBgmVolumeCommand.cs`, `ExitGameCommand.cs`

**命令处理器（Handler）**：

- 格式：`[Action][Target]CommandHandler.cs`
- 示例：`ChangeBgmVolumeCommandHandler.cs`

**命令输入（Input DTO）**：

- 格式：`[Action][Target]CommandInput.cs`
- 存放位置：`command/input/` 子目录
- 示例：`ChangeBgmVolumeCommandInput.cs`

**事件（Event）**：

- 格式：`[Action][Target]Event.cs`
- 示例：`BgmChangedEvent.cs`, `PlaySfxEvent.cs`

**事件处理器（Handler）**：

- 格式：`[Action][Target]Handler.cs`
- 示例：`BgmChangedHandler.cs`

**查询（Query）**：

- 格式：`Get[Entity]Query.cs` 或 `[Action][Target]Query.cs`
- 示例：`GetCurrentSettingsQuery.cs`

**查询处理器（Handler）**：

- 格式：`[QueryName]Handler.cs`
- 示例：`GetCurrentSettingsQueryHandler.cs`

**视图模型（View）**：

- 格式：`[Entity]View.cs`
- 存放位置：`query/view/` 子目录
- 示例：`SettingsView.cs`

#### 💾 存储要求

- ✅ 按领域（Domain）划分子目录
- ✅ Command/Event/Query 分离
- ✅ Handler 与对应实体同目录
- ✅ Input DTO 放在 `input/` 子目录
- ❌ 禁止跨领域引用
- ❌ 禁止在 Handler 中包含复杂业务逻辑

#### 🏗️ 代码模板

**Command**：

```csharp
using Mediator;

namespace GFrameworkGodotTemplate.scripts.cqrs.[domain].command;

/// <summary>
///     [命令描述]
/// </summary>
public record [Action][Target]Command : IRequest
{
    // 命令参数
}
```

**CommandHandler**：

```csharp
using Mediator;

namespace GFrameworkGodotTemplate.scripts.cqrs.[domain].command;

/// <summary>
///     [处理器描述]
/// </summary>
public class [Action][Target]CommandHandler : IRequestHandler<[Action][Target]Command]
{
    public async ValueTask Handle([Action][Target]Command request, CancellationToken cancellationToken)
    {
        // 处理逻辑
    }
}
```

***

### 5️⃣ credits/ - 制作组目录

#### 📂 层级结构

```
credits/
└── Credits.cs    # 制作组页面脚本
```

#### 🎯 功能描述

- **定位**：制作组成员展示页面
- **职责**：显示开发团队信息、版权声明
- **特点**：静态展示、从主菜单进入

#### 📝 文件命名规则

- **格式**：`[PageName].cs`
- **示例**：`Credits.cs`
- **类名规范**：与场景名一致，PascalCase
- **继承关系**：继承自 `Control`，实现 `IController`, `IUiPageBehaviorProvider`, `ISimpleUiPage`

#### 💾 存储要求

- ✅ 单一职责，一个页面一个文件
- ✅ 实现 ISimpleUiPage 接口
- ✅ 使用 `%[ButtonName]` 获取按钮节点
- ❌ 禁止包含其他页面逻辑

***

### 6️⃣ data/ - 数据管理目录

#### 📂 层级结构

```
data/
├── SaveStorageUtility.cs           # 存档工具类
├── README.md                        # 目录说明文档
├── interfaces/                      # 接口定义
│   ├── IPlayerDataListener.cs       # 数据监听器接口
│   └── ISaveStorageUtility.cs       # 存档工具接口
└── model/                           # 数据模型
    ├── GameSaveData.cs              # 游戏存档数据
    ├── LocalDataLocation.cs         # 本地数据位置
    └── README.md                    # 模型说明文档
```

#### 🎯 功能描述

- **定位**：数据持久化、存档管理、数据验证
- **职责**：管理数据、游戏存档、配置文件读写
- **特点**：单例模式、观察者模式、脏标记优化

#### 📝 文件命名规则

**数据模型**：

- 格式：`[Entity]Data.cs`
- 存放位置：`model/` 子目录
- 示例：`PlayerData.cs`, `GameSaveData.cs`

**接口**：

- 格式：`I[Feature].cs`
- 存放位置：`interfaces/` 子目录
- 示例：`IPlayerDataListener.cs`, `ISaveStorageUtility.cs`

**工具类**：

- 格式：`[Function]Utility.cs`
- 示例：`SaveStorageUtility.cs`

#### 💾 存储要求

- ✅ 数据模型放在 `model/` 子目录
- ✅ 接口定义放在 `interfaces/` 子目录
- ✅ 管理器使用单例模式
- ✅ 实现数据验证和范围检查
- ✅ 使用 ConfigFile 进行持久化
- ❌ 禁止在数据类中包含业务逻辑
- ❌ 禁止直接访问 Godot 节点

#### 🏗️ 代码模板

**数据模型**：

```csharp
namespace GFrameworkGodotTemplate.scripts.data.model;

/// <summary>
///     [实体]数据模型
/// </summary>
public class [Entity]Data
{
    // 字段
    private [Type] _[field];

    // 属性（带验证）
    public [Type] [PropertyName]
    {
        get => _[field];
        set
        {
            // 验证逻辑
            _[field] = value;
            // 通知监听器
        }
    }
}
```

***

### 7️⃣ entities/ - 实体目录

#### 📂 层级结构

```
entities/
└── README.md    # 实体说明文档
```

#### 🎯 功能描述

- **定位**：游戏实体（Entity）定义
- **职责**：定义游戏中的实体类（如玩家、敌人、NPC 等）
- **当前状态**：预留目录，待后续开发填充

#### 📝 文件命名规则

- **格式**：`[EntityName].cs`
- **示例**：`Player.cs`, `Enemy.cs`, `NPC.cs`, `Item.cs`
- **类名规范**：PascalCase，使用领域术语

#### 💾 存储要求

- ✅ 实体类应包含 ID、组件引用
- ✅ 使用 ECS（实体-组件-系统）思想
- ✅ 轻量级，仅包含数据和组件容器
- ❌ 禁止包含复杂行为逻辑（行为应在 System 中）

#### 🏗️ 代码模板（预留）

```csharp
namespace GFrameworkGodotTemplate.scripts.entities;

/// <summary>
///     [实体名称]
/// </summary>
public class [EntityName]
{
    public Guid Id { get; } = Guid.NewGuid();

    // 组件引用
    public [ComponentType]? [ComponentName] { get; set; }
}
```

***

### 8️⃣ enums/ - 枚举目录

#### 📂 层级结构

```
enums/
├── InputPhase.cs                   # 输入阶段枚举
├── audio/                          # 音频枚举
│   ├── BgmType.cs                  # 背景音乐类型
│   └── SfxType.cs                  # 音效类型
├── poker/                          # 扑克枚举
│   └── StateType.cs                # 状态类型
├── resources/                      # 资源枚举
│   └── TextureKey.cs               # 纹理键
├── scene/                          # 场景枚举
│   └── SceneKey.cs                 # 场景键
├── settings/                       # 设置枚举
│   └── SettingsChangedReason.cs    # 设置变更原因
└── ui/                             # UI枚举
    └── UiKey.cs                    # UI键
```

#### 🎯 功能描述

- **定位**：集中管理所有枚举类型定义
- **职责**：提供类型安全的常量集合
- **特点**：按领域分类、避免魔法字符串

#### 📝 文件命名规则

- **格式**：`[EnumName].cs` 或 `[Category]Type.cs`
- **示例**：`SceneKey.cs`, `BgmType.cs`, `InputPhase.cs`
- **类名规范**：PascalCase，使用复数或 Type 后缀
- **访问修饰符**：public enum

#### 💾 存储要求

- ✅ 按领域划分子目录（audio, ui, scene 等）
- ✅ 枚举成员添加 Summary 注释
- ✅ 使用 `[Flags]` 标记位域枚举
- ❌ 禁止在枚举中包含方法
- ❌ 禁止枚举值重复或冲突

#### 🏗️ 代码模板

```csharp
namespace GFrameworkGodotTemplate.scripts.enums.[category];

/// <summary>
///     [枚举描述]
/// </summary>
public enum [EnumName]
{
    /// <summary>
    ///     [成员描述]
    /// </summary>
    [MemberName],

    /// <summary>
    ///     [成员描述]
    /// </summary>
    [AnotherMember]
}
```

***

### 9️⃣ intro/ - 介绍页目录

#### 📂 层级结构

```
intro/
└── Intro.cs    # 介绍页面脚本
```

#### 🎯 功能描述

- **定位**：游戏启动后的介绍/过渡页面
- **职责**：展示游戏简介、公司 Logo、加载过渡
- **特点**：自动跳转、动画效果

#### 📝 文件命名规则

- **格式**：`[PageName].cs`
- **示例**：`Intro.cs`
- **继承关系**：同 Credits 目录规范

#### 💾 存储要求

- 同 credits/ 目录规范

***

### 🔟 main\_menu/ - 主菜单目录

#### 📂 层级结构

```
main_menu/
└── MainMenu.cs    # 主菜单脚本
```

#### 🎯 功能描述

- **定位**：游戏主界面，导航中心
- **职责**：提供新游戏、继续、关卡选择、设置、制作组、退出等功能入口
- **特点**：核心导航节点、状态管理入口

#### 📝 文件命名规则

- **格式**：`MainMenu.cs`
- **特殊节点**：`%NewGameButton`, `%ContinueGameButton`, `%ChooseLevelButton`, `%OptionsMenuButton`, `%CreditsButton`, `%ExitButton`

#### 💾 存储要求

- ✅ 实现 IController, IUiPageBehaviorProvider, ISimpleUiPage
- ✅ 使用 ISceneRouter 和 IUiRouter 进行导航
- ✅ 使用 IStateMachineSystem 管理状态切换
- ❌ 禁止直接操作场景树（通过 Router 操作）

***

### 1️⃣1️⃣ module/ - 模块注册目录

#### 📂 层级结构

```
module/
├── ModelModule.cs     # 模型模块注册
├── StateModule.cs     # 状态模块注册
├── SystemModule.cs    # 系统模块注册
├── UtilityModule.cs   # 工具模块注册
└── README.md          # 模块说明文档
```

#### 🎯 功能描述

- **定位**：依赖注入（DI）模块注册
- **职责**：向 GFramework 容器注册服务和实现
- **特点**：启动时加载、全局服务定位

#### 📝 文件命名规则

- **格式**：`[Domain]Module.cs`
- **示例**：`ModelModule.cs`, `StateModule.cs`, `UtilityModule.cs`
- **类名规范**：PascalCase，以 "Module" 结尾

#### 💾 存储要求

- ✅ 继承 GFramework 的 Module 基类
- ✅ 在 RegisterServices 方法中注册服务
- ✅ 按功能域划分不同 Module
- ❌ 禁止包含业务逻辑
- ❌ 禁止循环依赖

#### 🏗️ 代码模板

```csharp
using GFramework.Core.Module;

namespace GFrameworkGodotTemplate.scripts.module;

/// <summary>
///     [模块名称] - 负责[功能域]的服务注册
/// </summary>
public class [Domain]Module : [ModuleBase]
{
    protected override void RegisterServices(IServiceCollection services)
    {
        // 注册服务
        services.AddSingleton<I[Service], [Implementation]>();
        services.AddTransient<I[Service], [Implementation]>();
    }
}
```

***

### 1️⃣2️⃣ options\_menu/ - 选项菜单目录

#### 📂 层级结构

```
options_menu/
└── OptionsMenu.cs    # 选项菜单脚本
```

#### 🎯 功能描述

- **定位**：游戏设置界面
- **职责**：音量调节、分辨率设置、语言切换、全屏切换
- **特点**：实时预览、即时保存

#### 📝 文件命名规则

- **格式**：`OptionsMenu.cs`
- **继承关系**：同 main\_menu/ 规范

#### 💾 存储要求

- ✅ 使用 CQRS Command 发送设置变更
- ✅ 通过 Mediator 模式解耦
- ✅ 设置变更立即生效
- ❌ 禁止直接修改全局配置对象

***

### 1️⃣3️⃣ pause\_menu/ - 暂停菜单目录

#### 📂 层级结构

```
pause_menu/
└── PauseMenu.cs    # 暂停菜单脚本
```

#### 🎯 功能描述

- **定位**：游戏暂停时的覆盖层菜单
- **职责**：提供继续、重新开始、返回主菜单、设置等选项
- **特点**：暂停游戏循环、模态窗口

#### 📝 文件命名规则

- **格式**：`PauseMenu.cs`
- **继承关系**：同 main\_menu/ 规范

#### 💾 存储要求

- ✅ 使用 PauseGameCommand 暂停游戏
- ✅ 使用 ResumeGameCommand 恢复游戏
- ✅ 模态 UI 层级正确
- ❌ 禁止在暂停时处理游戏逻辑

***

### 1️⃣4️⃣ setting/ - 系统设置目录

#### 📂 层级结构

```
setting/
├── SettingDataLocationProvider.cs    # 设置数据位置提供者
└── SettingsDataRepository.uid        # 设置数据仓库
```

#### 🎯 功能描述

- **定位**：系统设置的持久化和读取
- **职责**：管理用户偏好设置、配置文件路径解析
- **特点**：平台适配、自动同步

#### 📝 文件命名规则

- **提供者**：`[SettingType]DataLocationProvider.cs`
- **仓库**：`[SettingType]DataRepository.cs`

#### 💾 存储要求

- ✅ 实现数据位置抽象
- ✅ 支持多平台路径
- ✅ 配置文件版本兼容
- ❌ 禁止硬编码路径

***

### 1️⃣5️⃣ stateMachine/ - 状态机目录

#### 📂 层级结构

```
stateMachine/
├── IState.cs         # 状态接口
└── IStateMachine.cs  # 状态机接口
```

#### 🎯 功能描述

- **定位**：有限状态机（FSM）核心接口定义
- **职责**：定义状态和状态机的契约
- **特点**：泛型接口、类型安全

#### 📝 文件命名规则

- **接口**：`I[Concept].cs`
- **示例**：`IState.cs`, `IStateMachine.cs`
- **访问修饰符**：public interface

#### 💾 存储要求

- ✅ 纯接口定义，无实现
- ✅ 泛型约束清晰
- ✅ 方法签名完整
- ❌ 禁止包含实现代码
- ❌ 禁止引用具体业务类型

#### 🏗️ 代码模板

```csharp
namespace GFrameworkGodotTemplate.scripts.stateMachine;

/// <summary>
///     [接口描述]
/// </summary>
public interface I[InterfaceName]
{
    /// <summary>
    ///     [方法描述]
    /// </summary>
    [ReturnType] [MethodName]([Parameters]);
}
```

***

### 1️⃣6️⃣ utility/ - 工具类目录

#### 📂 层级结构

```
utility/
├── IGodotTextureRegistry.cs    # 纹理注册表接口
└── GodotTextureRegistry.cs     # Godot纹理注册表实现
```

#### 🎯 功能描述

- **定位**：通用工具类和辅助功能
- **职责**：提供跨模块复用的工具方法
- **特点**：无状态、纯函数、高内聚低耦合

#### 📝 文件命名规则

- **接口**：`I[UtilityName].cs`
- **实现**：`Godot[UtilityName].cs` 或 `[UtilityName].cs`
- **示例**：`IGodotTextureRegistry.cs`, `GodotTextureRegistry.cs`

#### 💾 存储要求

- ✅ 接口与实现分离
- ✅ 无副作用（Pure Function）
- ✅ 线程安全（如需要）
- ✅ 完整的 XML 注释
- ❌ 禁止包含业务逻辑
- ❌ 禁止依赖具体场景或 UI

***

## 📋 通用文件创建约束

### 文件基本要求

1. **文件扩展名**
   - 所有脚本文件必须使用 `.cs` 扩展名
   - 文档文件使用 `.md` 扩展名
2. **命名规范**
   - **文件名**：PascalCase（首字母大写）
   - **类名**：与文件名一致
   - **命名空间**：`GFrameworkGodotTemplate.scripts.[directory].[subdirectory]`
3. **编码标准**
   - UTF-8 编码
   - LF 换行（Unix 风格）
   - 4 空格缩进（不使用 Tab）
4. **必需元素**
   - 每个 `.cs` 文件必须包含：
     - ✅ namespace 声明
     - ✅ XML 文档注释（`<summary>`）
     - ✅ using 语句（按字母排序）
     - ✅ 访问修饰符明确指定

### 文件头注释模板

```csharp
// ============================================================================
//  Project: GFramework-Godot-Template
//  File: [FileName].cs
//  Author: [AuthorName]
//  Created: [YYYY-MM-DD]
//  Description: [Brief description]
//  Version: [VersionNumber]
// ============================================================================
//
//  [Detailed description of the file's purpose and functionality]
//
//  Dependencies:
//  - [Dependency1]
//  - [Dependency2]
//
//  Changelog:
//  - [YYYY-MM-DD] - [Author] - Initial creation
//  - [YYYY-MM-DD] - [Author] - [Change description]
//
// ============================================================================
```

### 编译验证要求

每次新增或修改文件后，**必须**执行以下验证步骤：

1. **编译检查**
   ```bash
   cd c:\Users\dgh\Desktop\Dreamcreative\code\gamebulid\Project
   dotnet build
   ```
2. **验证清单**
   - ✅ 零编译错误（Error count = 0）
   - ✅ 无关键警告（Warning 可接受但应尽量消除）
   - ✅ 命名空间正确
   - ✅ 引用完整性
   - ✅ 类型安全性
3. **常见错误排查**
   - CS0104：命名空间冲突 → 检查 using 语句
   - CS0246：类型未找到 → 检查引用和命名空间
   - CS1503：参数类型不匹配 → 检查方法签名
   - CS1061：方法不存在 → 检查 API 版本

***

## 🎯 扩展功能集成约束

当需要添加新功能时，必须遵循以下约束条件：

### 1. 目录选择指南

根据新功能的性质，选择合适的目录：

| 功能类型     | 推荐目录                                  | 判断依据       |
| -------- | ------------------------------------- | ---------- |
| UI 容器/组件 | `component/`                          | 可复用的 UI 组件 |
| 全局常量     | `constants/`                          | 不可变的全局值    |
| 框架基础设施   | `core/`                               | 路由、状态、音频等  |
| 用户操作命令   | `cqrs/[domain]/command/`              | CQRS 命令模式  |
| 系统事件     | `cqrs/[domain]/events/`               | 事件驱动通知     |
| 数据查询     | `cqrs/[domain]/query/`                | 只读数据获取     |
| 页面脚本     | `[page_name]/`                        | 对应场景的脚本    |
| 数据模型     | `data/model/`                         | 持久化数据结构    |
| 数据管理     | `data/`                               | 数据 CRUD 操作 |
| 类型定义     | `enums/`                              | 枚举、常量集合    |
| 服务注册     | `module/`                             | DI 容器注册    |
| 工具函数     | `utility/`                            | 通用辅助方法     |
| 接口定义     | 对应目录的 `interfaces/` 或 `stateMachine/` | 抽象契约       |

### 2. 新增文件检查清单

创建新文件前，确认以下事项：

- [ ] **目录正确性**：文件放置在最合适的目录中
- [ ] **命名规范性**：符合该目录的命名规则
- [ ] **依赖合理性**：不引入循环依赖
- [ ] **接口一致性**：遵循现有接口约定
- [ ] **文档完整性**：包含完整的 XML 注释
- [ ] **编译通过性**：`dotnet build` 成功
- [ ] **风格统一性**：与同目录现有文件风格一致

### 3. 禁止事项

❌ **绝对禁止**：

- 修改 `.tscn` 场景文件
- 删除或重命名现有文件
- 修改框架核心代码（`core/` 中的接口和基类）
- 引用未定义的类型或方法
- 硬编码路径或配置值
- 在数据类中包含业务逻辑

⚠️ **需要审批**：

- 修改 `core/` 中的实现类
- 新增全局单例
- 修改枚举定义
- 变更模块依赖关系

✅ **鼓励做法**：

- 使用 partial 类扩展现有类
- 实现已有接口添加新功能
- 使用装饰器包装现有功能
- 通过配置文件扩展行为
- 利用事件机制添加响应逻辑

***

## 📚 附录：快速参考卡片

### 目录速查表

| 目录              | 主要用途    | 文件命名模式                           | 典型示例                             |
| --------------- | ------- | -------------------------------- | -------------------------------- |
| `component/`    | UI 组件   | `*Component.cs`                  | `VolumeContainer.cs`             |
| `constants/`    | 常量定义    | `*Constants.cs`                  | `GameConstants.cs`               |
| `core/`         | 框架核心    | `*[Type].cs`, `I*.cs`            | `SceneRouter.cs`                 |
| `cqrs/`         | CQRS 模式 | `*Command.cs`, `*Event.cs`       | `ExitGameCommand.cs`             |
| `credits/`      | 制作组     | `[Page].cs`                      | `Credits.cs`                     |
| `data/`         | 数据管理    | `*Manager.cs`, `*Data.cs`        | `PlayerDataManager.cs`           |
| `entities/`     | 实体定义    | `[Entity].cs`                    | （预留）                             |
| `enums/`        | 枚举类型    | `[Enum].cs`, `*Type.cs`          | `SceneKey.cs`                    |
| `intro/`        | 介绍页     | `[Page].cs`                      | `Intro.cs`                       |
| `main_menu/`    | 主菜单     | `MainMenu.cs`                    | `MainMenu.cs`                    |
| `module/`       | DI 模块   | `*Module.cs`                     | `ModelModule.cs`                 |
| `options_menu/` | 设置页     | `[Page].cs`                      | `OptionsMenu.cs`                 |
| `pause_menu/`   | 暂停菜单    | `[Page].cs`                      | `PauseMenu.cs`                   |
| `setting/`      | 系统设置    | `*Provider.cs`, `*Repository.cs` | `SettingDataLocationProvider.cs` |
| `stateMachine/` | 状态机接口   | `I*.cs`                          | `IStateMachine.cs`               |
| `utility/`      | 工具类     | `I*.cs`, `*Registry.cs`          | `IGodotTextureRegistry.cs`       |

### 常用命令速查

```bash
# 编译项目
dotnet build

# 清理并重建
dotnet clean && dotnet build

# 运行测试（如果有）
dotnet test

# 查看详细警告
dotnet build /warnaserror
```

***

## 🌍 Global 目录结构与文件创建规范

本项目 `global` 目录包含 **全局服务节点**，这些节点作为 Godot AutoLoad 单例运行，负责管理游戏的核心基础设施和全局状态。以下是详细的目录结构说明和文件组织规则。

### 总体定位

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
├── README.md                          # 目录说明文档
└── *.tscn                             # 对应的场景文件（AutoLoad节点）
```

#### 🎯 核心特征

- **运行方式**：作为 Godot AutoLoad 节点，游戏启动时自动加载
- **生命周期**：贯穿整个游戏运行周期，不会被卸载
- **访问权限**：全局可访问，所有模块均可引用
- **职责范围**：提供基础设施服务、管理全局状态、协调各子系统

***

### 1️⃣ GameEntryPoint.cs - 游戏入口点

#### 📂 文件信息

- **路径**：`global/GameEntryPoint.cs`
- **场景**：`global/game_entry_point.tscn`
- **基类**：`Node`
- **特性**：`[Log]`, `[ContextAware]`

#### 🎯 功能描述

- **定位**：游戏架构的初始化入口和配置中心
- **核心职责**：
  1. 初始化 GFramework 架构实例（IArchitecture）
  2. 配置日志系统、环境设置（开发/生产）
  3. 注册所有场景配置（SceneConfig）到场景注册表
  4. 注册所有 UI 页面配置（UiPageConfig）到 UI 注册表
  5. 注册所有纹理配置（TextureConfig）到纹理注册表
  6. 初始化设置系统并应用用户偏好
  7. 启动协程调度器预热

#### 📝 关键属性

```csharp
public static IArchitecture Architecture { get; private set; }  // 架构实例
public static SceneTree Tree { get; private set; }              // 场景树引用
[Export] public bool IsDev { get; set; }                        // 开发模式标志
[Export] public Array<UiPageConfig> UiPageConfigs { get; set; } // UI页面配置数组
[Export] public Array<SceneConfig> GameSceneConfigs { get; set; } // 场景配置数组
[Export] public Array<TextureConfig> TextureConfigs { get; set; } // 纹理配置数组
```

#### 💾 存储要求

- ✅ 必须作为第一个 AutoLoad 节点加载（优先级最高）
- ✅ 包含完整的架构初始化逻辑
- ✅ 使用 `[Export]` 暴露配置项供编辑器设置
- ❌ 禁止包含业务逻辑代码
- ❌ 禁止在 \_Ready 中执行耗时操作（使用 CallDeferred）

#### 🔧 配置要求

在 Godot 编辑器中：

1. 创建 `game_entry_point.tscn` 场景
2. 添加 Node 根节点，挂载 `GameEntryPoint.cs` 脚本
3. 在 Inspector 中配置：
   - `IsDev`: 勾选开发模式
   - `UiPageConfigs`: 添加所有 UI 页面配置资源
   - `GameSceneConfigs`: 添加所有场景配置资源
   - `TextureConfigs`: 添加所有纹理配置资源
4. 在 Project Settings → AutoLoad 中注册此场景

***

### 2️⃣ GlobalInputController.cs - 全局输入控制器

#### 📂 文件信息

- **路径**：`global/GlobalInputController.cs`
- **场景**：`global/global_input_controller.tscn`
- **基类**：`GameInputController`（继承自 core/controller/）
- **特性**：`[ContextAware]`, `[Log]`

#### 🎯 功能描述

- **定位**：全局输入事件的处理中心
- **核心职责**：
  1. 监听全局输入事件（键盘、手柄、鼠标）
  2. 处理游戏暂停/恢复逻辑（Esc 键 → PauseMenu）
  3. 管理全局游戏玩法输入服务（IGlobalGameplayInputService）
  4. 每帧同步更新输入状态缓存
  5. 为所有 Gameplay 组件提供统一的输入数据源

#### 📝 关键方法

```csharp
protected override bool AcceptPhase(InputPhase phase)
// 判断是否接受当前输入阶段（Global 或 Paused）

protected override void Handle(InputPhase phase, InputEvent @event)
// 处理输入事件，更新游戏玩法输入状态

public IGlobalGameplayInputService GameplayInputService { get; }
// 公开 API：供 PlayerMovementController 等组件访问输入服务
```

#### 💾 存储要求

- ✅ 继承 `GameInputController` 基类
- ✅ 实现输入阶段过滤（AcceptPhase）
- ✅ 使用 CQRS Command 发送暂停/恢复命令
- ✅ 集成 IGlobalGameplayInputService 服务
- ❌ 禁止直接处理具体业务逻辑（仅处理全局输入）
- ❌ 禁止包含角色移动等 Gameplay 逻辑

#### 🏗️ 扩展指南

当需要新增全局输入处理时：

1. 在 `Handle()` 方法中检测新的输入条件
2. 使用 CQRS Command 发送命令（而非直接调用方法）
3. 如需新增输入服务，创建对应的 Interface 和 Implementation
4. 在 `_Ready()` 中初始化新服务

***

### 3️⃣ IGlobalGameplayInputService.cs - 游戏玩法输入服务接口

#### 📂 文件信息

- **路径**：`global/IGlobalGameplayInputService.cs`
- **类型**：接口定义（Interface）

#### 🎯 功能描述

- **定位**：定义游戏中角色移动相关的全局输入状态查询契约
- **设计原则**：
  - **单例服务**：全局唯一实例，所有 Gameplay 组件共享同一输入状态
  - **状态缓存**：每帧更新一次，避免多次查询 Godot Input API
  - **接口隔离**：仅暴露必要的输入查询方法，隐藏实现细节

#### 📝 接口成员

```csharp
public interface IGlobalGameplayInputService
{
    float HorizontalDirection { get; }  // 水平方向 [-1.0, 1.0]
    bool IsJumpPressed { get; }         // 是否按下跳跃键（单次触发）
    void UpdateInputState();            // 更新输入状态缓存
}
```

#### 💾 存储要求

- ✅ 纯接口定义，无实现代码
- ✅ 方法签名清晰，注释完整
- ✅ 支持多设备输入源（键盘、手柄）
- ❌ 禁止包含具体实现逻辑
- ❌ 禁止依赖具体的 Godot 节点类型

***

### 4️⃣ GlobalGameplayInputService.cs - 游戏玩法输入服务实现

#### 📂 文件信息

- **路径**：`global/GlobalGameplayInputService.cs`
- **类型**：普通类（非 Node）
- **实现接口**：`IGlobalGameplayInputService`

#### 🎯 功能描述

- **定位**：统一处理游戏中角色移动相关的全局输入检测
- **架构特点**：
  - **输入源抽象层**：屏蔽底层 Godot Input API 差异
  - **状态缓存中心**：避免多个组件重复查询 Input 系统
  - **双策略输入映射**：
    - 策略1：Input Map 优先（支持自定义键位绑定）
    - 策略2：直接键盘后备（确保开箱即用）

#### 📝 输入映射策略

```csharp
// 策略1: Input Map（优先）
float axisValue = Input.GetAxis("ui_left", "ui_right");

// 策略2: 直接键盘检测（后备）
if (axisValue == 0)
{
    bool leftPressed = Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left);
    bool rightPressed = Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right);
    // ...
}
```

#### 💾 存储要求

- ✅ 实现 IGlobalGameplayInputService 接口
- ✅ 使用私有字段缓存输入状态
- ✅ 支持 Input Map + 直接键盘双策略
- ✅ 在 UpdateInputState() 中统一更新
- ❌ 禁止继承 Node 类（纯逻辑类）
- ❌ 禁止直接修改游戏状态

#### 🏗️ 扩展新输入类型

当需要新增输入维度时（如冲刺、攻击）：

1. 在接口中添加新属性：`bool IsSprintPressed { get; }`
2. 在实现类中添加对应字段和检测逻辑
3. 在 GlobalInputController.UpdateGameplayInputState() 中调用更新
4. 在 PlayerMovementController 等组件中使用新属性

***

### 5️⃣ AudioManager.cs - 音频管理器

#### 📂 文件信息

- **路径**：`global/AudioManager.cs`
- **场景**：`global/audio_manager.tscn`
- **基类**：`Node`
- **实现接口**：`IController`
- **特性**：`[ContextAware]`, `[Log]`

#### 🎯 功能描述

- **定位**：全局音频播放管理中心
- **核心职责**：
  1. 管理 BGM（背景音乐）播放器
  2. 管理 SFX（音效）播放器池（最多12个并发）
  3. 绑定音频系统（IAudioSystem）以响应 CQRS 命令
  4. 提供音量调节、音频切换等功能

#### 📝 关键属性

```csharp
[Export] public AudioStream BgmAudioStream { get; set; }      // BGM音频流
[Export] public AudioStream GamingAudioStream { get; set; }   // 游戏中BGM
[Export] public AudioStream ReadyAudioStream { get; set; }    // 准备中BGM
[Export] public AudioStream ShipFireSfx { get; set; }         // 舰船开火音效
[Export] public AudioStream ExplosionSfx { get; set; }        // 爆炸音效
[Export] public AudioStream UiClickSfx { get; set; }          // UI点击音效
[Export] private int _maxSfxPlayerCount = 12;                 // 最大音效并发数
```

#### 💾 存储要求

- ✅ 使用对象池模式管理 SFX 播放器
- ✅ 通过 IAudioSystem 接口响应 CQRS 音频命令
- ✅ 使用 `[Export]` 暴露音频资源配置
- ✅ 设置正确的 Audio Bus（Bgm/Sfx）
- ❌ 禁止直接播放音频（必须通过 IAudioSystem）
- ❌ 禁止在业务逻辑中直接引用此类

#### 🔧 配置流程

1. 创建 `audio_manager.tscn` 场景
2. 添加 Node 根节点，挂载 AudioManager.cs
3. 添加子节点：
   - `%BgmAudioStreamPlayer`（AudioStreamPlayer 类型）
4. 在 Inspector 中拖拽音频资源到对应属性
5. 注册为 AutoLoad 节点

***

### 6️⃣ SceneRoot.cs - 场景根节点

#### 📂 文件信息

- **路径**：`global/SceneRoot.cs`
- **场景**：`global/scene_root.tscn`
- **基类**：`Node2D`
- **实现接口**：`ISceneRoot`
- **特性**：`[Log]`, `[ContextAware]`

#### 🎯 功能描述

- **定位**：场景树的根容器节点
- **核心职责**：
  1. 管理当前活动场景的行为列表（List<ISceneBehavior>）
  2. 维护当前视图节点引用
  3. 提供场景添加/移除接口（AddScene/RemoveScene）
  4. 初始化场景路由器（ISceneRouter）
  5. 发送场景就绪事件（SceneRootReadyEvent）

#### 📝 关键方法

```csharp
public void AddScene(ISceneBehavior scene)
// 添加场景到根节点，自动管理父子关系

public void RemoveScene(ISceneBehavior scene)
// 从根节点移除场景，自动清理资源

public Node? Current => _currentView;
// 获取当前活动的视图节点
```

#### 💾 存储要求

- ✅ 实现 ISceneRoot 接口
- ✅ 维护场景行为列表的一致性
- ✅ 正确处理节点的 Reparent 和 QueueFree
- ✅ 在 \_Ready 中初始化路由器并发送事件
- ❌ 禁止直接操作场景树（通过 Router 操作）
- ❌ 禁止包含场景切换的业务逻辑

#### 🔧 配置流程

1. 创建 `scene_root.tscn` 场景
2. 添加 Node2D 根节点，挂载 SceneRoot.cs
3. 确保场景名称为 "SceneRoot"
4. 注册为 AutoLoad 节点（通常作为主场景）

***

### 7️⃣ UiRoot.cs - UI根节点

#### 📂 文件信息

- **路径**：`global/UiRoot.cs`
- **场景**：`global/ui_root.tscn`
- **基类**：`CanvasLayer`
- **实现接口**：`IUiRoot`
- **特性**：`[Log]`, `[ContextAware]`

#### 🎯 功能描述

- **定位**：UI 层级系统的根容器节点
- **核心职责**：
  1. 管理 UI 层级容器字典（Dictionary\<UiLayer, Control>）
  2. 维护 UI 页面行为列表（List<IUiPageBehavior>）
  3. 提供 UI 页面添加/移除接口（AddUiPage/RemoveUiPage）
  4. 控制 UI 渲染顺序（ZIndex 和 ZAsRelative）
  5. 初始化 UI 路由器（IUiRouter）
  6. 发送 UI 根节点就绪事件（UiRootReadyEvent）

#### 📝 UI层级结构

```csharp
// UiLayers 常量定义（位于 constants/UiLayers.cs）
public static class UiLayers
{
    public const int UiRoot = 100;     // UI根层
    public const int Page = 10;        // 页面层
    public const int Popup = 20;       // 弹窗层
    public const int Overlay = 30;     // 覆盖层
    public const int Tooltip = 40;     // 提示层
}
```

#### 💾 存储要求

- ✅ 实现 IUiRoot 接口
- ✅ 使用 CanvasLayer 作为基类（支持 Layer 属性）
- ✅ 正确设置 ZIndex 控制渲染顺序
- ✅ 支持多层级 UI 组织（Page/Popup/Overlay）
- ✅ 在 \_Ready 中初始化路由器和 UI 层容器
- ❌ 禁止包含具体页面的业务逻辑
- ❌ 禁止手动排序 UI 子节点（由 ZIndex 自动控制）

#### 🔧 配置流程

1. 创建 `ui_root.tscn` 场景
2. 添加 CanvasLayer 根节点，挂载 UiRoot.cs
3. 添加子 Control 节点作为各层级容器：
   - `PageContainer`（用于页面）
   - `PopupContainer`（用于弹窗）
   - `OverlayContainer`（用于覆盖层）
4. 在脚本中通过 `_containers[UiLayer.Page] = PageContainer` 映射
5. 注册为 AutoLoad 节点

***

### 8️⃣ SceneTransitionManager.cs - 场景过渡管理器

#### 📂 文件信息

- **路径**：`global/SceneTransitionManager.cs`
- **场景**：`global/scene_transition_manager.tscn`
- **基类**：`Node`
- **实现接口**：`IController`
- **特性**：`[ContextAware]`, `[Log]`

#### 🎯 功能描述

- **定位**：场景切换时的视觉过渡效果管理器
- **核心职责**：
  1. 使用 Shader 材质实现过渡动画（淡入淡出、溶解等效果）
  2. 管理预览视口（SubViewport）用于渲染下一场景
  3. 控制过渡进度参数（Shader Parameter: progress）
  4. 协同协程系统实现异步过渡流程
  5. 提供单例访问（Instance 属性）

#### 📝 关键属性

```csharp
public static SceneTransitionManager? Instance { get; private set; }  // 单例实例
public bool IsTransitioning { get; private set; }                     // 过渡状态标志

private CanvasLayer CanvasLayer => GetNode<CanvasLayer>("%CanvasLayer");        // 画布层
private ColorRect SceneTransitionRect => GetNode<ColorRect>("%SceneTransitionRect"); // 过渡矩形
private SubViewport PreviewViewport => GetNode<SubViewport>("%PreviewViewport"); // 预览视口
private ShaderMaterial _material;  // Shader材质实例
```

#### 💾 存储要求

- ✅ 使用单例模式（Instance 属性）
- ✅ 通过 ShaderMaterial 控制视觉效果
- ✅ 支持协程驱动的异步过渡
- ✅ 正确管理材质副本（Duplicate 避免共享状态）
- ✅ 初始状态完全隐藏（Visible=false, progress=0）
- ❌ 禁止在过渡过程中响应其他操作
- ❌ 禁止手动修改 Shader 参数（通过公开方法控制）

#### 🔧 配置流程

1. 创建 `scene_transition_manager.tscn` 场景
2. 添加 Node 根节点，挂载 SceneTransitionManager.cs
3. 添加子节点：
   - `CanvasLayer`（CanvasLayer，Layer=100）
     - `ColorRect`（全屏矩形，附加 ShaderMaterial）
       - Name: SceneTransitionRect
     - `SubViewport`（预览视口）
       - Name: PreviewViewport
4. 为 ColorRect 配置 Shader 材质（如 transition.shader）
5. 使用 `%` 路径引用节点
6. 注册为 AutoLoad 节点

***

## 📋 Global 目录通用规范

### 文件命名规则

| 类型    | 格式                             | 示例                               |
| ----- | ------------------------------ | -------------------------------- |
| 入口节点  | `[System]EntryPoint.cs`        | `GameEntryPoint.cs`              |
| 输入控制器 | `[Scope]InputController.cs`    | `GlobalInputController.cs`       |
| 服务接口  | `I[Service]Service.cs`         | `IGlobalGameplayInputService.cs` |
| 服务实现  | `[Service]Service.cs`          | `GlobalGameplayInputService.cs`  |
| 管理器   | `[Domain]Manager.cs`           | `AudioManager.cs`                |
| 根节点   | `[Domain]Root.cs`              | `SceneRoot.cs`, `UiRoot.cs`      |
| 过渡管理器 | `[Domain]TransitionManager.cs` | `SceneTransitionManager.cs`      |

### 类继承与接口实现规则

| 文件类型  | 基类                       | 实现接口            | 必需特性                      |
| ----- | ------------------------ | --------------- | ------------------------- |
| 入口节点  | `Node`                   | 无               | `[Log]`, `[ContextAware]` |
| 输入控制器 | `GameInputController`    | 无               | `[Log]`, `[ContextAware]` |
| 服务接口  | 无                        | 自身              | 无                         |
| 服务实现  | 无                        | 对应接口            | 无                         |
| 管理器   | `Node`                   | `IController`   | `[Log]`, `[ContextAware]` |
| 根节点   | `Node2D` / `CanvasLayer` | `I[Domain]Root` | `[Log]`, `[ContextAware]` |
| 过渡管理器 | `Node`                   | `IController`   | `[Log]`, `[ContextAware]` |

### 场景文件（.tscn）配置要求

每个 Global 节点都必须有对应的 `.tscn` 场景文件：

1. **命名规范**：使用 snake\_case（与脚本 PascalCase 对应）
   - `GameEntryPoint.cs` → `game_entry_point.tscn`
   - `GlobalInputController.cs` → `global_input_controller.tscn`
2. **AutoLoad 注册**：
   - 在 Project Settings → AutoLoad 中注册
   - 设置正确的加载顺序（GameEntryPoint 最先加载）
3. **节点结构**：
   ```
   [根节点: Node/Node2D/CanvasLayer]
   └── 脚本: [对应 .cs 文件]
   └── 子节点: %[UniqueName] (使用 UniqueName)
   ```
4. **唯一性保证**：
   - 所有子节点应使用 `%` 前缀标记为 UniqueName
   - 在脚本中使用 `GetNode<NodeType>("%NodeName")` 引用

### 依赖注入与框架集成

所有 Global 节点都应遵循以下 DI 模式：

```csharp
// 获取系统服务
var system = this.GetSystem<ISystemInterface>()!;

// 获取模型
var model = this.GetModel<IModelInterface>()!;

// 获取工具类
var utility = this.GetUtility<IUtilityInterface>()!;

// 发送 CQRS 命令
var handle = this.SendCommand(new SomeCommand());

// 注册事件监听
this.RegisterEvent<SomeEvent>(e => { /* 处理 */ });
```

### 日志规范

所有 Global 节点都应使用 `[Log]` 特性启用日志：

```csharp
[Log]  // 自动注入 _log 字段
public partial class MyGlobalNode : Node
{
    public override void _Ready()
    {
        _log.Debug("节点初始化完成");
        _log.Info("重要信息");
        _log.Warn("警告信息");
        _log.Error("错误信息");
    }
}
```

### 编译验证要求

每次修改 Global 目录文件后，**必须**执行：

```bash
cd c:\Users\dgh\Desktop\Dreamcreative\code\gamebulid\Project
dotnet build
```

验证清单：

- ✅ 零编译错误
- ✅ AutoLoad 节点正确注册
- ✅ 场景文件（.tscn）与脚本匹配
- ✅ 接口实现完整
- ✅ 依赖注入配置正确

***

## 📚 附录：Global 目录快速参考卡片

### 文件清单速查表

| 文件名                              | 类型   | 场景文件 | 核心职责       | 优先级 |
| -------------------------------- | ---- | ---- | ---------- | --- |
| `GameEntryPoint.cs`              | 入口点  | ✓    | 架架初始化、配置注册 | ⭐⭐⭐ |
| `GlobalInputController.cs`       | 输入控制 | ✓    | 全局输入、暂停管理  | ⭐⭐⭐ |
| `IGlobalGameplayInputService.cs` | 接口   | ✗    | 输入服务契约定义   | ⭐⭐  |
| `GlobalGameplayInputService.cs`  | 服务实现 | ✗    | 输入检测、状态缓存  | ⭐⭐  |
| `AudioManager.cs`                | 管理器  | ✓    | 音频播放、音效池   | ⭐⭐  |
| `SceneRoot.cs`                   | 根节点  | ✓    | 场景容器、路由初始化 | ⭐⭐⭐ |
| `UiRoot.cs`                      | 根节点  | ✓    | UI容器、层级管理  | ⭐⭐⭐ |
| `SceneTransitionManager.cs`      | 过渡管理 | ✓    | 场景切换动画     | ⭐⭐  |

### 加载顺序要求

```
1. GameEntryPoint        ← 最先加载（初始化架构）
2. SceneRoot             ← 第二（作为主场景容器）
3. UiRoot                ← 第三（UI层容器）
4. GlobalInputController  ← 第四（开始接收输入）
5. AudioManager           ← 第五（准备音频系统）
6. SceneTransitionManager ← 最后（过渡效果就绪）
```

### 常见使用场景

**在 Gameplay 组件中获取全局输入**：

```csharp
// 方式1：通过 GlobalInputController（推荐）
var inputCtrl = GetNode<GlobalInputController>("/root/GlobalInputController");
var inputService = inputCtrl.GameplayInputService;
float horizontal = inputService.HorizontalDirection;
bool jump = inputService.IsJumpPressed;

// 方式2：通过依赖注入（如果已注册为服务）
var inputService = this.GetService<IGlobalGameplayInputService>();
```

**发送全局命令**：

```csharp
// 发送暂停命令（从任何节点）
this.SendCommand(new PauseGameWithOpenPauseMenuCommand(input));

// 发送设置变更命令
this.SendCommand(new ChangeBgmVolumeCommand(0.8f));
```

***

## 📚 项目总文档归档规范

### 归档目标

所有项目技术文档、开发指南、系统设计文档等必须最终归档整合至 **`PROJECT_COMPLETE_GUIDE.md`**，确保项目文档体系的**唯一性、完整性和可维护性**。

### 触发条件

以下情况必须执行文档归档流程：

1. **新增技术文档**：创建任何新的 `.md` 技术文档时
2. **系统功能完成**：完成某个核心系统的开发和文档编写后
3. **问题解决记录**：记录重大 Bug 修复或技术方案后
4. **架构变更**：进行架构重构或模块调整后
5. **定期整理**：每完成一个重要开发阶段后

### 归档工作流程

#### Step 1: 内容筛选与提取

对源文档进行系统性分析：

- ✅ **提取关键信息**：核心技术方案、实现细节、配置参数
- ✅ **识别核心内容**：架构设计、系统流程、代码示例
- ❌ **过滤冗余信息**：重复描述、临时笔记、调试过程
- ❌ **排除过时内容**：已废弃的方案、历史遗留信息

**筛选标准**：

```
优先级 P0 (必须包含):
├── 系统架构设计与核心原理
├── 关键实现代码与技术方案
├── 配置参数与约束条件
└── 问题解决方案与最佳实践

优先级 P1 (建议包含):
├── 开发流程与操作步骤
├── 接口定义与使用示例
├── 目录结构与文件组织
└── 常见问题 FAQ

优先级 P2 (可选包含):
├── 历史变更记录
├── 调试过程日志
├── 临时性说明
└── 个人备注信息
```

#### Step 2: 内容合并与去重

将筛选后的信息合并到 `PROJECT_COMPLETE_GUIDE.md`：

- **结构化整合**：按主题分类，避免信息碎片化
- **消除重复**：同一技术点只保留最完整的描述
- **保持连贯**：确保上下文衔接自然，逻辑清晰
- **版本统一**：统一术语、格式、代码风格

**合并原则**：

```markdown
# 合并检查清单

□ 相同功能的描述是否重复出现？
□ 同一代码示例是否多次引用？
□ 不同文档中的矛盾信息是否已核实？
□ 技术术语是否保持一致？
□ 章节顺序是否符合逻辑？
```

#### Step 3: 真实性验证

对合并后的内容进行准确性核查：

- **技术验证**：
  - ✅ 代码示例是否可编译通过
  - ✅ API 调用是否与实际框架一致
  - ✅ 配置参数是否正确无误
  - ✅ 文件路径是否真实存在
- **逻辑验证**：
  - ✅ 流程描述是否完整无遗漏
  - ✅ 解决方案是否切实可行
  - ✅ 前后描述是否自相矛盾
  - ✅ 版本信息是否准确最新

**验证方法**：

1. 对照源代码验证关键代码段
2. 运行 `dotnet build` 验证编译正确性
3. 检查文件系统确认路径有效性
4. 交叉对比多个源文档确认一致性

#### Step 4: 结构化整理

将验证后的内容组织为标准化格式：

**文档结构模板**：

```markdown
# GFramework-Godot-Template 项目完整开发指南

> **版本**: X.X.X (整合版)
> **最后更新**: YYYY-MM-DD
> **适用框架**: GFramework X.X.X + Godot Engine X.X + .NET X.X

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

[正文内容...]
```

**格式规范**：

- 使用 Markdown 标准语法
- 标题层级不超过 4 级
- 代码块标注语言类型
- 表格用于展示对比信息
- 流程图使用 ASCII 或 Mermaid

#### Step 5: 清理原始文档

归档完成后必须执行清理操作：

**删除文件清单**：

- 所有已合并的源文档文件
- 处理过程中产生的临时文件
- 冗余的备份或草稿文件

**保留文件**：

- ✅ `PROJECT_COMPLETE_GUIDE.md` （唯一总文档）
- ✅ `Agent输入限制条件.md` （开发约束文档）
- ❌ 其他所有已归档的 `.md` 文件（删除）

**清理命令示例**：

```powershell
# 确认 docs 目录仅保留必要文件
Get-ChildItem -Path "Project\docs\" -Filter "*.md" | Select-Object Name
```

### 质量控制标准

#### 内容质量要求

| 维度      | 标准        | 验证方法        |
| ------- | --------- | ----------- |
| **完整性** | 覆盖所有核心技术点 | 检查清单对照      |
| **准确性** | 技术描述与代码一致 | 编译验证、代码审查   |
| **清晰度** | 表述明确无歧义   | 同行评审、用户反馈   |
| **结构性** | 层级分明逻辑清晰  | 目录导航测试      |
| **时效性** | 反映最新代码状态  | 版本比对、Git 记录 |

#### 文档维护规则

1. **单一来源原则**：
   - 每个技术知识点只在 `PROJECT_COMPLETE_GUIDE.md` 中存在一份权威描述
   - 避免多份文档描述相同内容导致不一致
2. **增量更新机制**：
   - 新增内容直接追加到对应章节
   - 更新内容原地修改并更新版本号
   - 废弃内容标记删除或移至附录
3. **版本管理规范**：
   ```markdown
   > **版本**: X.X.X (整合版)
   > **最后更新**: YYYY-MM-DD
   > **变更记录**:
   > - v3.0.0: 整合10份技术文档，重构目录结构
   > - v2.1.0: 新增玩家数据管理系统章节
   > - v2.0.0: 补充场景切换系统详细说明
   ```
4. **定期审查制度**：
   - 每月进行一次文档完整性审查
   - 每季度进行一次准确性验证
   - 重大版本发布后立即更新

### 异常处理

#### 场景 1: 源文档仍在被引用

**问题**：某份源文档中的内容被其他团队频繁查阅

**解决方案**：

1. 在 `PROJECT_COMPLETE_GUIDE.md` 中完善相关内容
2. 在原文档头部添加重定向提示：
   ```markdown
   > ⚠️ **本文档已归档**
   >
   > 最新内容请查看：[PROJECT_COMPLETE_GUIDE.md](./PROJECT_COMPLETE_GUIDE.md)
   >
   ```

> 本文档将于 YYYY-MM-DD 删除

````
3. 给予 7 天过渡期后删除

#### 场景 2: 内容冲突无法合并

**问题**：多份文档对同一技术点描述不一致

**处理流程**：
1. 标记冲突内容并记录来源
2. 对照源代码验证正确版本
3. 与相关开发者确认权威描述
4. 统一为经过验证的版本
5. 在文档中注明决策依据

#### 场景 3: 文档过大难以维护

**问题**：`PROJECT_COMPLETE_GUIDE.md` 超过合理规模（建议 < 2000 行）

**优化策略**：
1. 拆分为模块化子文档（如 `GUIDE_ARCHITECTURE.md`, `GUIDE_DEVELOPMENT.md`）
2. 在主文档中保留索引和摘要
3. 建立交叉引用体系
4. 考虑使用文档生成工具（如 DocFX）

### 执行检查清单

每次执行归档任务时，必须逐项确认：

```markdown
## 归档任务检查清单

### 准备阶段
- [ ] 明确归档范围和源文档列表
- [ ] 备份当前版本的 PROJECT_COMPLETE_GUIDE.md
- [ ] 通知相关团队成员归档计划

### 执行阶段
- [ ] 完成所有源文档的内容筛选
- [ ] 提取关键信息并分类整理
- [ ] 合并内容并消除重复
- [ ] 验证技术描述的准确性
- [ ] 按照标准格式重组文档
- [ ] 更新版本号和变更记录

### 收尾阶段
- [ ] 删除所有已归档的源文档
- [ ] 清理临时文件和冗余文件
- [ ] 验证 docs 目录文件清单
- [ ] 提交 Git 并撰写清晰的 commit message
- [ ] 通知团队成员归档完成

### 质量验证
- [ ] 文档可通过目录导航到所有章节
- [ ] 所有代码示例格式正确
- [ ] 所有超链接有效可访问
- [ ] 无拼写错误和语法错误
- [ ] 内容覆盖所有源文档的核心信息
````

### 示例：完整归档流程

**场景**：整合 10 份技术文档为总输出文档

**输入文件**：

```
ARCHITECTURE_REFACTORING_REPORT.md      # 架构重构报告
DEVELOPMENT_GUIDE.md                     # 开发指南
ENHANCED_NAVIGATION_SYSTEM_GUIDE.md     # 导航系统指南
F5_DEBUG_FLOW_DETAIL.md                  # F5调试流程
HOMEUI_SCENE_SWITCH_FIX.md               # 场景切换修复
KEYNOTFOUND_EXCEPTION_FIX.md             # 异常修复
PLAYER_DATA_MANAGEMENT_SYSTEM_GUIDE.md   # 数据管理指南
PLAYER_MOVEMENT_SYSTEM_GUIDE.md          # 移动系统指南
SCENE_SWITCHING_SYSTEM_GUIDE.md          # 场景切换指南
TECHNICAL_ARCHITECTURE.md                # 技术架构
```

**执行步骤**：

1. **读取分析**：批量读取所有源文档，建立内容索引
2. **分类映射**：
   - 架构相关 → 第1章（技术栈与架构概览）
   - 启动流程 → 第2章（F5调试启动完整流程）
   - 目录规范 → 第3章（项目目录结构与规范）
   - 核心系统 → 第4章（核心系统详解）
   - 开发流程 → 第5章（开发工作流指南）
   - 场景切换 → 第6章（场景切换系统）
   - 移动系统 → 第7章（玩家移动系统）
   - 数据管理 → 第8章（玩家数据管理系统）
   - 问题修复 → 第9章（常见问题与解决方案）
   - 约束条件 → 第10章（最佳实践与约束条件）
3. **去重合并**：识别并合并重复的技术描述
4. **验证核对**：对照源代码验证关键技术点
5. **输出生成**：生成结构化的 `PROJECT_COMPLETE_GUIDE.md`
6. **清理收尾**：删除所有源文档，保留总文档

**输出结果**：

```
docs/
├── PROJECT_COMPLETE_GUIDE.md    ✅ 保留（唯一总文档）
├── Agent输入限制条件.md         ✅ 保留（约束文档）
└── (其他 .md 文件已删除)        ❌ 已清理
```

***

> **文档版本**：v2.2\
> **最后更新**：2026-05-11\
> **适用项目**：GFramework-Godot-Template\
> **技术栈**：Godot 4.6 + .NET 10.0 + GFramework 0.0.205

