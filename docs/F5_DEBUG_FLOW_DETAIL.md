# Godot F5调试完整操作流程与技术实现原理

> **文档类型**: 框架层调用链路深度解析  
> **核心焦点**: GFramework框架程序的调用机制（非Godot引擎内部）  
> **表示方式**: 文字箭头 + 缩进层次（适配小屏幕显示器）

---

## 第一部分：F5按下后的系统启动完整流程

### 阶段一：Godot引擎初始化阶段

```
用户按下F5键
    │
    ▼
Godot编辑器检测到运行请求
    │
    ├─→ 读取 project.godot 配置文件
    │       │
    │       └─→ 获取以下关键配置信息:
    │               ├─→ config/name = "GFramework-Godot-Template"
    │               ├─→ run/main_scene = "uid://6s2urcdmkpbt" (主场景UID)
    │               ├─→ [autoload] 段落中的全局单例列表
    │               └─→ dotnet/project_assembly_name
    │
    ├─→ 初始化.NET运行时环境
    │       │
    │       └─→ 加载目标框架: net10.0
    │               │
    │               └─→ 编译C#项目(如果源码有变更)
    │                       │
    │                       └─→ 生成程序集: GFramework-Godot-Template.dll
    │                               位置: .godot/mono/temp/bin/Debug/
    │
    ├─→ 初始化渲染后端
    │       │
    │       └─→ 配置: GL Compatibility模式
    │               │
    │               └─→ 设置视口大小: 960x540 (来自project.godot配置)
    │
    └─→ 创建主场景树(SceneTree)
            │
            └─→ 准备进入Autoload加载阶段
```

### 阶段二：Autoload全局单例按序加载

**⚠️ 关键：此阶段按project.godot中[autoload]声明的顺序依次执行**

```
Godot开始加载Autoload节点(按声明顺序)
    │
    ══════════════════════════════════════════════════════
    第1个: GameEntryPoint (最关键的入口点)
    ══════════════════════════════════════════════════════
    │
    ├─→ 实例化 global/game_entry_point.tscn 场景
    │       │
    │       └─→ 创建 GameEntryPoint 节点(Node类型)
    │               │
    │               └─→ 将其添加到场景树根部(成为全局可访问的单例)
    │
    ├─→ 触发 GameEntryPoint._Ready() 方法 ← 🎯 核心入口函数
    │       │
    │       ├─→ Step 1: 获取场景树引用并保存为静态属性
    │       │       │
    │       │       └─→ Tree = GetTree()
    │       │               │
    │       │               └─→ 后续所有代码都可通过 GameEntryPoint.Tree 访问场景树
    │       │
    │       ├─→ Step 2: 创建GFramework架构实例 ⭐⭐⭐ 【框架核心】
    │       │       │
    │       │       ├─→ new GameArchitecture(configuration, environment)
    │       │       │       │
    │       │       │       ├─→ configuration参数包含:
    │       │       │       │       ├─→ LoggerFactoryProvider = GodotLoggerFactoryProvider
    │       │       │       │       │       └─→ MinLevel = LogLevel.Debug(开发环境)
    │       │       │       │       └─→ 日志输出将写入Godot控制台
    │       │       │       │
    │       │       │       └─→ environment参数:
    │       │       │               ├─→ IsDev=true → new GameDevEnvironment()
    │       │       │               └─→ IsDev=false → new GameMainEnvironment()
    │       │       │
    │       │       └─→ Architecture = 新建的架构实例(保存为静态属性供全局访问)
    │       │
    │       ├─→ Step 3: 初始化架构(安装所有模块) ⭐⭐⭐ 【框架核心】
    │       │       │
    │       │       └─→ Architecture.Initialize()
    │       │               │
    │       │               ├─→ 内部调用 InstallModules() 方法
    │       │               │       │
    │       │               │       ├─→ InstallModule(new UtilityModule())
    │       │               │       │       │
    │       │               │       │       └─→ UtilityModule.Install(architecture)
    │       │               │       │               │
    │       │               │       │               └─→ 注册工具服务到DI容器:
    │       │               │       │                   ├─→ IGodotTextureRegistry → GodotTextureRegistry实例
    │       │               │       │                   ├─→ IGodotSceneRegistry → GodotSceneRegistry实例
    │       │               │       │                   └─→ IGodotUiRegistry → GodotUiRegistry实例
    │       │               │       │
    │       │               │       ├─→ InstallModule(new ModelModule())
    │       │               │       │       │
    │       │               │       │       └─→ ModelModule.Install(architecture)
    │       │               │       │               │
    │       │               │       │               └─→ 注册数据模型服务:
    │       │               │       │                   ├─→ ISettingsModel → SettingsModel实例
    │       │               │       │                   └─→ [其他业务模型...]
    │       │               │       │
    │       │               │       ├─→ InstallModule(new SystemModule()) ⭐【最重要】
    │       │               │       │       │
    │       │               │       │       └─→ SystemModule.Install(architecture)
    │       │               │       │               │
    │       │               │       │               └─→ 注册核心系统服务:
    │       │               │       │                   ├─→ IUiRouter → UiRouter实例(UI页面路由器)
    │       │               │       │                   ├─→ ISceneRouter → SceneRouter实例(场景路由器)
    │       │               │       │                   ├─→ ISettingsSystem → SettingsSystem实例
    │       │               │       │                   └─→ IAudioSystem → GodotAudioSystem实例
    │       │               │       │
    │       │               │       └─→ InstallModule(new StateModule())
    │       │               │               │
    │       │               │               └─→ StateModule.Install(architecture)
    │       │               │                       │
    │       │               │                       └─→ 注册状态机系统:
    │       │               │                           └─→ IStateMachineSystem → StateMachineSystem实例
    │       │               │
    │       │               ├─→ 调用 Configurator 委托(Mediator注册)
    │       │               │       │
    │       │               │       └─→ collection.AddMediator(options =>
    │       │               │               options.ServiceLifetime = ServiceLifetime.Singleton)
    │       │               │               │
    │       │               │               └─→ Mediator是CQRS模式的中介者
    │       │               │                   负责自动分发Command到对应的Handler
    │       │               │
    │       │               └─→ ✅ 此时DI容器中已注册所有核心服务
    │       │
    │       ├─→ Step 4: 获取设置模型并初始化
    │       │       │
    │       │       ├─→ _settingsModel = this.GetModel<ISettingsModel>()! ⭐【DI获取】
    │       │       │       │
    │       │       │       └─→ 框架从DI容器中查找ISettingsModel类型的实现
    │       │       │           返回Step 3中注册的SettingsModel实例
    │       │       │
    │       │       └─→ _ = _settingsModel.InitializeAsync()
    │       │               │
    │       │               └─→ 异步加载用户设置数据(分辨率、语言、音量等)
    │       │                   数据来源: user://save/settings文件
    │       │
    │       ├─→ Step 5: 监听设置初始化完成事件 ⭐【事件驱动】
    │       │       │
    │       │       └─→ this.RegisterEvent<SettingsInitializedEvent>(e => { ... })
    │       │               │
    │       │               ├─→ 向框架的事件总线注册监听器
    │       │               │
    │       │               └─→ 当SettingsModel初始化完成后会发布此事件
    │       │                   回调执行逻辑:
    │       │                   ├─→ _settingsSystem = this.GetSystem<ISettingsSystem>()!
    │       │                   │       └─→ 从DI获取设置系统服务
    │       │                   │
    │       │                   └─→ _ = _settingsSystem.ApplyAll()
    │       │                           └─→ 应用所有设置到Godot引擎(分辨率、音量等)
    │       │
    │       ├─→ Step 6: 获取三个核心注册表服务 ⭐【资源管理】
    │       │       │
    │       │       ├─→ _sceneRegistry = this.GetUtility<IGodotSceneRegistry>()!
    │       │       │       └─→ 场景注册表(用于通过字符串键查找PackedScene)
    │       │       │
    │       │       ├─→ _uiRegistry = this.GetUtility<IGodotUiRegistry>()!
    │       │       │       └─→ UI注册表(用于通过字符串键查找UI页面的PackedScene)
    │       │       │
    │       │       └─→ _textureRegistry = this.GetUtility<IGodotTextureRegistry>()!
    │       │               └─→ 纹理注册表(用于通过枚举键查找纹理资源)
    │       │
    │       ├─→ Step 7: 注册资源配置到各注册表 ⭐⭐⭐ 【关键步骤】
    │       │       │
    │       │       ├─→ 遍历 GameSceneConfigs 数组(Editor中拖拽配置的场景列表)
    │       │       │       │
    │       │       │       └─→ foreach (var gameSceneConfig in GameSceneConfigs)
    │       │       │               │
    │       │       │               └─→ _sceneRegistry.Registry(gameSceneConfig)
    │       │       │                       │
    │       │       │                       └─→ 将每个SceneConfig注册到字典中:
    │       │       │                           Key = SceneKey枚举的名称(如"Boot", "Main", "Home")
    │       │       │                           Value = PackedScene资源引用(对应.tscn文件)
    │       │       │
    │       │       │       示例注册结果:
    │       │       │       ├─→ "Boot" → boot_start.tscn (启动画面场景)
    │       │       │       ├─→ "Main" → main_menu.tscn (主菜单背景场景)
    │       │       │       ├─→ "Home" → home.tscn (游戏主页场景)
    │       │       │       ├─→ "Scene1" → scene_1.tscn (测试场景1)
    │       │       │       └─→ "Scene2" → scene_2.tscn (测试场景2)
    │       │       │
    │       │       ├─→ 遍历 UiPageConfigs 数组(Editor中拖拽配置的UI列表)
    │       │       │       │
    │       │       │       └─→ foreach (var uiPageConfig in UiPageConfigs)
    │       │       │               │
    │       │       │               └─→ _uiRegistry.Registry(uiPageConfig)
    │       │       │                       │
    │       │       │                       └─→ 将每个UiPageConfig注册到字典中:
    │       │       │                           Key = UiKey枚举的名称(如"MainMenu", "HomeUi")
    │       │       │                           Value = PackedScene资源引用(对应.tscn文件)
    │       │       │
    │       │       │       示例注册结果:
    │       │       │       ├─→ "MainMenu" → main_menu.tscn
    │       │       │       ├─→ "HomeUi" → home_ui.tscn
    │       │       │       ├─→ "Credits" → credits.tscn
    │       │       │       ├─→ "OptionsMenu" → options_menu.tscn
    │       │       │       └─→ "PauseMenu" → pause_menu.tscn
    │       │       │
    │       │       └─→ 遍历 TextureConfigs 数组(纹理资源配置)
    │       │               │
    │       │               └─→ _textureRegistry.Registry(textureConfig)
    │       │                       └─→ 注册纹理资源(用于UI图标、背景图等)
    │       │
    │       └─→ Step 8: 延迟初始化协程调度器
    │               │
    │               └─→ CallDeferred(nameof(CallDeferredInit))
    │                       │
    │                       └─→ 确保在当前帧所有_Ready()完成后再执行
    │                               │
    │                               └─→ CallDeferredInit() 执行:
    │                                       └─→ Timing.Prewarm()
    │                                               └─→ 预热协程系统(确保首次使用无延迟)
    │
    └─→ ✅ GameEntryPoint初始化完成，继续加载下一个Autoload
    
    │
    ══════════════════════════════════════════════════════
    第2个: SceneRoot (场景根容器)
    ══════════════════════════════════════════════════════
    │
    ├─→ 实例化 global/scene_root.tscn
    │       │
    │       └─→ 创建 SceneRoot 节点(Node2D类型, 实现ISceneRoot接口)
    │
    ├─→ 触发 SceneRoot._Ready() 方法
    │       │
    │       ├─→ var router = this.GetSystem<ISceneRouter>()! ⭐【DI获取】
    │       │       │
    │       │       └─→ 从DI容器获取Step 3注册的SceneRouter实例
    │       │
    │       ├─→ router.BindRoot(this) ⭐⭐⭐ 【绑定场景容器】
    │       │       │
    │       │       └─→ 将当前SceneRoot节点设置为场景路由器的根容器
    │       │               │
    │       │               └─→ 后续所有场景切换时:
    │       │                   新场景节点将作为子节点添加到此SceneRoot下
    │       │                   旧场景节点从此SceneRoot移除并释放
    │       │
    │       └─→ CallDeferred(nameof(CallDeferredCallback))
    │               │
    │               └─→ 延迟到帧末尾执行
    │                       │
    │                       └─→ CallDeferredCallback():
    │                               │
    │                               └─→ this.RunPublishCoroutine(new SceneRootReadyEvent())
    │                                       │
    │                                       ├─→ 发布事件到框架事件总线
    │                                       │
    │                                       └─→ 通知其他组件: 场景根节点已就绪
    │                                           可以开始进行场景切换操作
    │
    └─→ ✅ SceneRoot初始化完成
    
    │
    ══════════════════════════════════════════════════════
    第3个: UiRoot (UI画布根容器)
    ══════════════════════════════════════════════════════
    │
    ├─→ 实例化 global/ui_root.tscn
    │       │
    │       └─→ 创建 UiRoot 节点(CanvasLayer类型, 实现IUiRoot接口)
    │
    ├─→ 触发 UiRoot._Ready() 方法
    │       │
    │       ├─→ Layer = UiLayers.UiRoot
    │       │       │
    │       │       └─→ 设置CanvasLayer层级(确保UI显示在最上层)
    │       │
    │       ├─→ InitLayers() ⭐⭐⭐ 【初始化UI层级容器】
    │       │       │
    │       │       └─→ 遍历UiLayer枚举的所有值:
    │       │               │
    │       │               ├─→ UiLayer.Page (值=0)
    │       │               │       └─→ 创建Control容器节点(ZIndex基础值=0)
    │       │               │
    │       │               ├─→ UiLayer.Modal (值=1)
    │       │               │       └─→ 创建Control容器节点(ZIndex基础值=100)
    │       │               │
    │       │               ├─→ UiLayer.Tooltip (值=2)
    │       │               │       └─→ 创建Control容器节点(ZIndex基础值=200)
    │       │               │
    │       │               └─→ UiLayer.Popup (值=3)
    │       │               └─→ 创建Control容器节点(ZIndex基础值=300)
    │       │
    │       │       最终UiRoot的子节点结构:
    │       │       UiRoot(CanvasLayer)
    │       │       ├── Page (Control)      ← 普通页面容器
    │       │       ├── Modal (Control)     ← 模态对话框容器
    │       │       ├── Tooltip (Control)   ← 提示信息容器
    │       │       └── Popup (Control)     ← 弹出菜单容器
    │       │
    │       ├─→ var router = this.GetSystem<IUiRouter>()! ⭐【DI获取】
    │       │       │
    │       │       └─→ 从DI容器获取Step 3注册的UiRouter实例
    │       │
    │       ├─→ router.BindRoot(this) ⭐⭐⭐ 【绑定UI容器】
    │       │       │
    │       │       └─→ 将当前UiRoot节点设置为UI路由器的根容器
    │       │               │
    │       │               └─→ 后续所有UI页面切换时:
    │       │                   UI节点将添加到对应的Layer容器下(Page/Modal等)
    │       │
    │       └─→ CallDeferred(nameof(CallDeferredCallback))
    │               │
    │               └─→ CallDeferredCallback():
    │                       │
    │                       └─→ this.RunPublishCoroutine(new UiRootReadyEvent())
    │                               │
    │                               └─→ 发布UI根节点就绪事件
    │
    └─→ ✅ UiRoot初始化完成(含4个UI层级容器)
    
    │
    ══════════════════════════════════════════════════════
    第4个: GlobalInputController (全局输入控制器)
    ══════════════════════════════════════════════════════
    │
    ├─→ 实例化 GlobalInputController.cs 脚本
    │       │
    │       └─→ 创建 GlobalInputController 节点
    │               (继承GameInputController抽象基类)
    │
    ├─→ 触发 GlobalInputController._Ready() 方法
    │       │
    │       └─→ _stateMachineSystem = this.GetSystem<IStateMachineSystem>()! ⭐【DI获取】
    │               │
    │               └─→ 保存状态机系统引用
    │                   用于后续处理暂停/恢复等状态切换操作
    │
    └─→ ✅ GlobalInputController初始化完成
            (已准备好拦截全局输入事件)
    
    │
    ══════════════════════════════════════════════════════
    第5个: AudioManager (音频管理器)
    ══════════════════════════════════════════════════════
    │
    ├─→ 实例化 global/audio_manager.tscn
    │       │
    │       └─→ 创建 AudioManager 节点
    │
    ├─→ 触发 AudioManager._Ready() 方法
    │       │
    │       ├─→ BgmAudioStreamPlayer.Bus = GameConstants.Bgm
    │       │       │
    │       │       └─→ 设置BGM播放器的音频总线(独立音量控制)
    │       │
    │       └─→ this.GetSystem<IAudioSystem>()!.BindAudioManager(this) ⭐【框架绑定】
    │               │
    │               └─→ 将自身注册到音频系统服务
    │                   后续CQRS命令可通过音频系统控制此Manager
    │
    └─→ ✅ AudioManager初始化完成(准备播放音乐和音效)
    
    │
    ══════════════════════════════════════════════════════
    第6个: SceneTransitionManager (场景过渡动画管理器)
    ══════════════════════════════════════════════════════
    │
    ├─→ 实例化 global/scene_transition_manager.tscn
    │       │
    │       └─→ 创建 SceneTransitionManager 节点
    │
    ├─→ 触发 SceneTransitionManager._Ready() 方法
    │       │
    │       ├─→ Instance = this ⭐⭐⭐ 【设置静态单例】
    │       │       │
    │       │       └─→ 将自身赋值给静态属性Instance
    │       │           其他代码可通过 SceneTransitionManager.Instance 访问
    │       │
    │       ├─→ CanvasLayer.Layer = 100
    │       │       │
    │       │       └─→ 确保过渡动画显示在最上层(高于UiRoot)
    │       │
    │       ├─→ 初始化Shader材质
    │       │       │
    │       │       ├─→ 获取SceneTransitionRect节点的材质(ShaderMaterial)
    │       │       ├─→ _material = originalMaterial.Duplicate()
    │       │       │       │
    │       │       │       └─→ 创建材质副本(避免多个过渡共享同一材质)
    │       │       │
    │       │       └─→ 设置初始Shader参数:
    │       │               ├─→ progress = 0.0 (过渡进度0%)
    │       │               └─→ SceneTransitionRect.Visible = false (初始隐藏)
    │       │
    │       └─→ ✅ 过渡动画系统就绪
    │               支持多种Shader过渡效果(像素化、圆形展开等)
    │
    └─→ ✅ 所有Autoload节点加载完成
```

### 阶段三：主场景加载与游戏循环启动

```
Godot加载project.godot中run/main_scene指定的主场景
    │
    ├─→ 实例化主场景(通常是一个空场景或引导场景)
    │       │
    │       └─→ 添加到场景树的Root节点下
    │
    ├─→ 触发主场景及所有子节点的 _Ready() 方法
    │
    ├─→ 启动主循环
    │       │
    │       └─→ 开始每帧调用 _Process() 和 _PhysicsProcess()
    │
    └─→ ✅ 游戏完全就绪，可以接收用户输入
```

---

## 第二部分：用户点击Button控件时的完整调用链路

### 示例场景：用户点击MainMenu界面的"新游戏"按钮

```
用户鼠标左键点击"NewGameButton"按钮控件
    │
    ▼
Godot引擎检测到鼠标点击事件
    │
    ├─→ 确定点击位置落在NewGameButton的碰撞区域内
    │
    ├─→ 识别该节点是Button类型控件
    │
    └─→ 触发Button的Pressed信号(Signal)
            │
            ▼
        Godot信号系统分发Pressed信号
            │
            ├─→ 查找所有连接到此信号的回调函数(Handler)
            │
            └─→ 找到在 MainMenu._Ready() 中绑定的Lambda表达式
                    │
                    │   代码位置: scripts/main_menu/MainMenu.cs 的 SetupEventHandlers()方法
                    │
                    │   绑定语句:
                    │   NewGameButton.Pressed += () => { ... };
                    │
                    ▼
            执行Lambda表达式回调函数
                │
                │   函数体内容:
                │   _stateMachineSystem.ChangeToAsync<PlayingState>()
                │       .ToCoroutineEnumerator()
                │       .RunCoroutine();
                │
                ├─→ 获取_stateMachineSystem字段
                │       │
                │       └─→ 此字段在MainMenu._Ready()中通过DI获取并缓存:
                │               _stateMachineSystem = this.GetSystem<IStateMachineSystem>()!;
                │               │
                │               └─→ 返回框架的状态机系统服务实例
                │                   (在SystemModule.Install()中注册到DI容器)
                │
                ├─→ 调用 ChangeToAsync<PlayingState>() 方法 ⭐⭐⭐ 【状态切换入口】
                │       │
                │       ├─→ 泛型参数 PlayingState 表示目标状态类
                │       │
                │       ├─→ 返回类型: ValueTask<IState>(异步任务)
                │       │
                │       └─→ 内部执行流程:
                │               │
                │               ├─→ 1. 查找目标状态实例
                │               │       │
                │               │       └─→ 通过反射或工厂创建 PlayingState 的新实例
                │               │           (或从状态池中获取已有实例)
                │               │
                │               ├─→ 2. 检查转换合法性
                │               │       │
                │               │       └─→ 调用当前状态的 CanTransitionToAsync(PlayingState)
                │               │               │
                │               │               └─→ MainMenuState.CanTransitionToAsync:
                │               │                       │
                │               │                       └─→ return Task.FromResult(true);
                │               │                           (MainMenu允许切换到任何状态)
                │               │
                │               ├─→ 3. 执行退出当前状态
                │               │       │
                │               │       └─→ 调用 MainMenuState.OnExitAsync(PlayingState)
                │               │               │
                │               │               └─→ MainMenuState.OnExitAsync 实现:
                │               │                       │
                │               │                       └─→ base.OnExitAsync(to)
                │               │                               │
                │               │                               └─→ 默认空实现(无特殊清理逻辑)
                │               │
                │               ├─→ 4. 更新当前状态指针
                │               │       │
                │               │       └─→ Current = PlayingState实例
                │               │
                │               └─→ 5. 执行进入新状态 ⭐⭐⭐ 【核心逻辑】
                │                       │
                │                       └─→ 调用 PlayingState.OnEnterAsync(MainMenuState)
                │                               │
                │                               └─→ PlayingState.OnEnterAsync 实现:
                │                                       │
                │                                       │   代码位置: scripts/core/state/impls/PlayingState.cs
                │                                       │
                │                                       ├─→ await this.GetSystem<IUiRouter>()! ⭐【DI获取】
                │                                       │       │
                │                                       │       └─→ 从DI容器获取UI路由器服务
                │                                       │
                │                                       └─→ .ReplaceAsync(HomeUi.UiKeyStr) ⭐⭐⭐ 【UI替换】
                │                                               │
                │                                               ├─→ 参数: "HomeUi" (字符串键)
                │                                               │       │
                │                                               │       └─→ HomeUi.UiKeyStr 是常量:
                │                                               │               public static string UiKeyStr 
                │                                               │                   => nameof(UiKey.HomeUi);
                │                                               │               解析结果 = "HomeUi"
                │                                               │
                │                                               ├─→ ReplaceAsync内部流程:
                │                                               │       │
                │                                               │       ├─→ Step 1: 查找UI页面配置
                │                                               │       │       │
                │                                               │       │       └─→ _uiRegistry.Get("HomeUi")
                │                                               │       │               │
                │                                               │       │               └─→ 在UiPageConfig字典中查找key="HomeUi"
                │                                               │       │                   │
                │                                               │       │                   └─→ 返回对应的 PackedScene 资源
                │                                               |       │                       (即home_ui.tscn)
                │                                               │       │
                │                                               │       ├─→ Step 2: 实例化UI页面场景
                │                                               │       │       │
                │                                               │       │       └─→ packedScene.Instantiate()
                │                                               │       │               │
                │                                               │       │               └─→ 创建 HomeUi 控制器节点
                │                                               │       │                   (Control类型)
                │                                               │       │
                │                                               │       ├─→ Step 3: 触发新页面生命周期
                │                                               │       │       │
                │                                               │       │       ├─→ HomeUi节点被添加到场景树
                │                                               │       │       │       │
                │                                               │       │       │       └─→ Godot触发 HomeUi._Ready()
                │                                               │       │       │               │
                │                                               │       │       │               ├─→ Hide() (先隐藏避免闪烁)
                │                                               │       │       │               │
                │                                               |       │       │               ├─→ _sceneRouter = this.GetSystem<ISceneRouter>()!
                │                                               │       |       |       │       │   └─→ DI获取场景路由器
                │                                               │       │       │               │
                │                                               │       │       │               ├─→ SetupEventHandlers()
                │                                               │       │       │               │       │
                │                                               │       │       │               │       └─→ 绑定按钮事件:
                │                                               │       │       │               │               ├─→ Scene1Button.Pressed 
                │                                               │       │       │               │               │   → SwitchScene("Scene1")
                │                                               │       │       │               │               ├─→ Scene2Button.Pressed 
                │                                               │       │       │               │               │   → SwitchScene("Scene2")
                │                                               │       │       │               │               └─→ HomeUiButton.Pressed 
                │                                               │       │       │               │                   → SwitchScene("Home")
                │                                               │       │       │               │
                │                                               │       │       │               ├─→ CallDeferred(CallDeferredInit)
                │                                               │       │       │               │       └─→ 延迟检查UI栈状态
                │                                               │       │       │               │
                │                                               │       │       │               └─→ Show() (显示页面)
                │                                               │       │       │
                │                                               │       │       ├─→ ISimpleUiPage.OnEnter(null)
                │                                               │       │       │       └─→ 空实现(默认行为)
                │                                               │       │       │
                │                                               │       │       └─→ ISimpleUiPage.OnShow()
                │                                               │       │               └─→ 空实现(默认行为)
                │                                               │       │
                │                                               │       ├─→ Step 4: 获取页面行为对象 ⭐【工厂模式】
                │                                               │       │       │
                │                                               │       │       └─→ HomeUi.GetPage()
                │                                               │       │               │
                │                                               │       │               └─→ 内部实现:
                │                                               │       │                   _page ??= UiPageBehaviorFactory.Create<Control>(
                │                                               |                       this, 
                │                                               |                       UiKeyStr, 
                │                                               |                       UiLayer.Page
                │                                               |                   );
                │                                               │       │                   │
                │                                               │       │                   ├─→ 首次调用: _page为null
                │                                               │       │                   │   → 工厂创建新的UiPageBehavior对象
                │                                               |       │                   │       该对象包装了HomeUi节点和元数据
                │                                               │       │                   │
                │                                               │       │                   └─→ 后续调用: _page非null
                │                                               │       │                       → 直接返回缓存的实例(单例模式)
                │                                               │       │
                │                                               │       ├─→ Step 5: 处理当前页面(如果有)
                │                                               │       │       │
                │                                               │       │       ├─→ 如果存在当前页面(current):
                │                                               │       │       │       │
                │                                               │       │       │       ├─→ current.OnHide()
                │                                               │       │       │       │       └─→ 隐藏当前页面
                │                                               │       │       │       │
                │                                               │       │       │       └─→ current.OnExit()
                │                                               │       │       │               └─→ 当前页面退出逻辑
                │                                               │       │       │
                │                                               │       │       └─→ 从页面栈中移除当前页面
                │                                               │       │               _pageStack.Clear() (Replace会清空栈)
                │                                               │       │
                │                                               │       ├─→ Step 6: 将新页面添加到UI树 ⭐⭐⭐ 【视觉呈现】
                │                                               │       │       │
                │                                               │       │       └─→ UiRoot.AddUiPage(newPageBehavior, UiLayer.Page)
                │                                               │       │               │
                │                                               │       │               └─→ 内部流程:
                │                                               │       │                   ├─→ 查找UiLayer.Page对应的容器节点
                │                                               |       │                   │   即UiRoot下的Page(Control)子节点
                │                                               │       │                   │
                │                                               │       │                   ├─→ container.AddChild(homeUiViewNode)
                │                                               |       │                   │   → 将HomeUi Control节点添加到Page容器下
                │                                               │       │                   │   → 此时Godot渲染系统会在下一帧绘制它
                │                                               │       │                   │
                │                                               │       │                   ├─→ view.ZIndex = 0 * 100 + 0 = 0
                │                                               |       |       │                   → 设置Z轴索引(决定渲染顺序)
                │                                               │       │                   │
                │                                               │       │                   └─→ _pages.Add(newPageBehavior)
                │                                               |                           → 记录到UiRoot的页面列表中
                │                                               │       │
                │                                               │       └─→ Step 7: 更新内部状态
                │                                               │               │
                │                                               │               └─→ _pageStack.Push(newPageBehavior)
                │                                               │                   → 新页面成为栈顶元素
                │                                               │
                │                                               └─→ ✅ ReplaceAsync完成
                │                                                       用户看到HomeUi界面显示在屏幕上
                │
                ├─→ .ToCoroutineEnumerator() ⭐⭐ 【Task→Coroutine桥接】
                │       │
                │       ├─→ 将ValueTask<IState>转换为IEnumerator<IYieldInstruction>
                │       │       │
                │       │       └─→ 这是GFramework提供的桥接机制
                │       │           用于在Godot协程系统中执行异步Task
                │       │
                │       └─→ 返回协程枚举器对象
                │
                └─→ .RunCoroutine() ⭐⭐⭐ 【启动协程执行】
                        │
                        └─→ 将协程提交给Godot的协程调度器
                                │
                                └─→ 调度器在每帧推进协程执行
                                        │
                                        └─→ 直到整个异步链路完成
                                            (状态切换→UI替换→页面显示)
                                            
    ✅ 完成: 用户点击"新游戏"后看到的最终效果
    ┌──────────────────────────────────────────────┐
    │ 1. MainMenu界面消失                          │
    │ 2. 主菜单场景被销毁(ClearAsync)              │
    │ 3. Home场景被加载并显示                      │
    │ 4. HomeUi页面覆盖在Home场景上              │
    │ 5. 状态机当前状态 = PlayingState            │
    └──────────────────────────────────────────────┘
```

---

## 第三部分：用户按ESC键暂停游戏的完整流程

```
用户按下键盘ESC键
    │
    ▼
Godot输入系统生成InputEvent对象
    │
    ├─→ 事件类型: InputEventKey
    ├─→ 按键码: KEY_ESCAPE
    └─→ 动作映射: "ui_cancel" (Godot内置动作)
            │
            ▼
    Godot开始分发未处理的输入事件
        │
        └─→ 调用场景树中所有节点的 _Input(InputEvent) 方法
                │
                │   分发顺序: 从树底层向上(子节点优先)
                │
                ├─→ [可能被其他节点消费] 如果某节点调用了:
                │       GetViewport().SetInputAsHandled()
                │       则后续节点不再收到此事件
                │
                └─→ 最终到达 GlobalInputController._Input(@event) ⭐⭐⭐ 【全局拦截】
                        │
                        │   代码位置: global/GlobalInputController.cs
                        │   (继承自GameInputController基类)
                        │
                        ├─→ var pausedAtFrameStart = Tree.Paused
                        │       │
                        │       └─→ 检测当前场景树是否处于暂停状态
                        │           (Paused=true时游戏逻辑冻结,但UI仍可响应)
                        │
                        ├─→ Dispatch(InputPhase.Global, @event) ⭐【第一阶段分发】
                        │       │
                        │       └─→ Dispatch方法内部流程:
                        │               │
                        │               ├─→ AcceptPhase(InputPhase.Global) 检查
                        │               │       │
                        │               │       └─→ GlobalInputController重写了此方法:
                        │               │               │
                        │               │               └─→ return phase is InputPhase.Global 
                        │               |                   or InputPhase.Paused;
                        │               |                   → true (接受全局和暂停阶段的输入)
                        │               │
                        │               ├─→ IsBlocked() 检查
                        │               │       │
                        │               │       └─→ return false (默认不阻塞)
                        │               │
                        │               └─→ Handle(InputPhase.Global, @event) ⭐⭐⭐ 【实际处理】
                        │                       │
                        │                       └─→ GlobalInputController.Handle实现:
                        │                               │
                        │                               ├─→ if (!@event.IsActionPressed("ui_cancel"))
                                │       │               └─→ return; (不是ESC键,直接返回)
                                │       │
                                │                       ├─→ if (_stateMachineSystem.Current is not PlayingState)
                                │       │       │
                                │       │       └─→ return; (当前不在游戏中,忽略ESC)
                                │       │           (只有在PlayingState才响应暂停)
                                │       │
                                │                       ├─→ _log.Debug("暂停游戏") (记录日志)
                                │                       │
                                │                       └─→ 发送暂停命令 ⭐⭐⭐ 【CQRS命令】
                                │                               │
                                │                               _pauseMenuUiHandle = this.SendCommand(
                                |                                   new PauseGameWithOpenPauseMenuCommand(
                                |                                       new OpenPauseMenuCommandInput {
                                |                                           Handle = _pauseMenuUiHandle
                                |                                       }
                                |                                   )
                                |                               );
                                │                               │
                                │                               ├─→ SendCommand是GFramework提供的快捷方法
                                │                               │   内部调用Mediator发送命令并同步等待结果
                                │                               │
                                │                               └─→ 命令对象结构:
                                │                                   PauseGameWithOpenPauseMenuCommand
                                │                                   ├─→ 继承自 ICommand<OpenPauseMenuCommandInput>
                                │                                   └─→ 携带输入参数: Handle(用于后续关闭菜单)
                                │
                        │
                        ├─→ Dispatch(pausedAtFrameStart ? InputPhase.Paused : InputPhase.Gameplay, @event)
                        │       │
                        │       └─→ 第二阶段分发(根据暂停状态选择Gameplay或Paused阶段)
                        │               由于已在Global阶段处理了ESC,此处通常不会有额外操作
                        │
                        └─→ GetViewport().SetInputAsHandled() ⭐⭐⭐ 【标记输入已消费】
                                │
                                └─→ 告诉Godot此输入已被处理
                                    后续节点不再收到这个ESC按键事件
                                    
    │
    ▼
Mediator接收PauseGameWithOpenPauseMenuCommand命令 ⭐⭐⭐ 【CQRS分发核心】
    │
    ├─→ Mediator查询已注册的Handler
    │       │
    │       └─→ 找到 PauseGameWithOpenPauseMenuCommandHandler 类
    │               (实现了 ICommandHandler<PauseGameWithOpenPauseMenuCommand> 接口)
    │
    └─→ 调用 Handler.Handle(command, cancellationToken) 方法
            │
            │   代码位置: scripts/cqrs/pause_menu/command/PauseGameWithOpenPauseMenuCommandHandler.cs
            │
            ├─→ Handler内部执行流程:
            │       │
            │       ├─→ 1. 暂停场景树 ⭐⭐⭐ 【游戏冻结】
            │       │       │
            │       │       └─→ GameUtil.GetTree().Paused = true;
            │       │               │
            │       │               └─→ 效果:
            │       │                   ├─→ 所有节点的 _Process() 停止调用
            │       │                   ├─→ 物理模拟冻结
            │       │                   ├─→ 动画暂停
            │       │                   └─→ 但UI仍然可以响应用户输入
            │       │
            │       ├─→ 2. 状态机切换到 PausedState ⭐⭐⭐ 【状态切换】
            │       │       │
            │       │       └─→ (_stateMachineSystem ??= this.GetSystem<IStateMachineSystem>())!
            │       │               .ChangeToAsync<PausedState>()
            │       │               .ToCoroutineEnumerator()
            │       │               .RunCoroutine();
            │       │               │
            │       │               └─→ 与前面"新游戏"按钮相同的状态切换流程:
            │       │                   ├─→ CanTransitionToAsync(PausedState) → true
            │       │                   ├─→ PlayingState.OnExitAsync(PausedState)
            │       │                   ├─→ Current = PausedState
            │       │                   └─→ PausedState.OnEnterAsync(PlayingState)
            │       │                           │
            │       │                           └─→ PausedState.OnEnterAsync 实现:
            │       │                                   │
            │       │                                   └─→ PushAsync(PauseMenu.UiKeyStr) ⭐⭐⭐ 【UI入栈】
            │       │                                           │
            │       │                                           └─→ UiRouter.PushAsync("PauseMenu")
            │       │                                                   │
            │       │                                                   ├─→ 查找UiPageConfig["PauseMenu"]
            │       |                                                   │   → 获取pause_menu.tscn
            │       │                                                   │
            │       │                                                   ├─→ Instantiate() → PauseMenu控制器节点
            │       │                                                   │       │
            │       │                                                   │       └─→ PauseMenu._Ready() 触发:
            │       │                                                   │               │
            │       |                                                   |               ├─→ SetupEventHandlers()
            │       |                                                   |               │       │
            │       |                                                   |               │       └─→ 绑定按钮事件:
            │       |                                                   |               │           ├─→ ResumeButton 
            │       |                                                   |               │           │   → 发送ResumeGameWithClosePauseMenuCommand
            │       |                                                   |               │           ├─→ SaveButton 
            │       |                                                   |               │           │   → 保存+恢复命令
            │       |                                                   |               │           ├─→ OptionsButton 
            │       |                                                   |               │           │   → OpenOptionsMenuCommand
            │       |                                                   |               │           ├─→ MainMenuButton 
            │       |                                                   |               │           │   → 恢复+切换到MainMenuState
            │       |                                                   |               │           └─→ QuitButton 
            │       |                                                   |               │               → ExitGameCommand
            │       |                                                   |               │
            │       |                                                   |               └─→ _stateMachineSystem 
            │       |                                                           = this.GetSystem<>()
            │       |                                                               (缓存状态机引用)
            |       |                                                   │
            |       |                                                   ├─→ PauseMenu.GetPage()
            |       |                                                   │       └─→ 工厂创建UiPageBehavior
            |       |                                                   │           注意: 使用 UiLayer.Modal (模态层)
            |       |                                                   |
            |       |                                                   ├─→ ISimpleUiPage.OnEnter(null)
            |       |                                                   │
            |       |                                                   ├─→ ISimpleUiPage.OnShow()
            |       |                                                   │
            |       |                                                   ├─→ UiRoot.AddUiPage(pauseMenu, UiLayer.Modal)
            |       |                                                   │       │
            |       |                                                   │       └─→ 添加到Modal层容器(ZIndex=100+)
            |       |                                                   │           显示在所有普通页面之上
            |       |                                                   │
            |       |                                                   └─→ _pageStack.Push(pauseMenuBehavior)
            |       |                                                           入栈
            |       │
            │       └─→ 3. 返回UiHandle给调用者
            │               │
            │               └─→ _pauseMenuUiHandle = 返回的Handle值
            │                   (用于后续关闭暂停菜单时传入)
            │
            └─→ return ValueTask.FromResult(Unit.Value); (命令处理完成)
                    
    ✅ 完成: 用户按ESC后看到的最终效果
    ┌──────────────────────────────────────────────┐
    │ 1. 游戏场景冻结(动画、物理停止)             │
    │ 2. PauseMenu模态对话框弹出                  │
    │ 3. PauseMenu覆盖在HomeUi之上(Modal层)      │
    │ 4. 状态机当前状态 = PausedState             │
    │ 5. 用户可点击"继续"、"选项"、"主菜单"等按钮│
    └──────────────────────────────────────────────┘
```

---

## 第四部分：场景之间的关联机制详解

### 4.1 场景关联的核心枢纽 - 状态机系统

```
┌─────────────────────────────────────────────────────────────┐
│                     场景关联机制总览                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   场景A ──→ 状态机 ──→ 场景B                              │
│   (当前)     (协调器)   (目标)                             │
│                                                             │
│   状态机的职责:                                              │
│   1. 定义哪些场景之间可以互相切换                            │
│   2. 控制切换时机(何时可以从A切到B)                        │
│   3. 执行切换前的清理工作(离开A时的OnExit)                 │
│   4. 执行切换后的初始化工作(进入B时的OnEnter)              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 五个状态如何关联不同场景/UI

```
═════════════════════════════════════════════════════════
状态1: BootStartState (启动状态)
═════════════════════════════════════════════════════════
    │
    │ OnEnterAsync执行:
    │
    └─→ ISceneRouter.ReplaceAsync("Boot")
            │
            ├─→ 查找: SceneKey.Boot → boot_start.tscn
            ├─→ 实例化: 启动画面场景(Logo动画等)
            ├─→ 添加到: SceneRoot节点下
            └─→ 显示: 启动画面(通常几秒后自动切换)

    
═════════════════════════════════════════════════════════
状态2: MainMenuState (主菜单状态) ⭐⭐⭐ 【特殊:完全重置点】
═════════════════════════════════════════════════════════
    │
    │ OnEnterAsync执行:
    │
    ├─→ IUiRouter.ClearAsync()
    │       │
    │       └─→ 清空所有UI页面栈
    │           移除并释放所有当前显示的UI
    │
    ├─→ ISceneRouter.ClearAsync()
    │       │
    │       └─→ 清空所有场景
    │           移除并释放当前活动场景
    │
    └─→ IUiRouter.PushAsync("MainMenu")
            │
            ├─→ 查找: UiKey.MainMenu → main_menu.tscn
            ├─→ 实例化: MainMenu控制器(Control节点)
            │       │
            │       └─→ MainMenu._Ready() → SetupEventHandlers()
            │               绑定5个按钮:
            │               ├── NewGameButton → ChangeToAsync<PlayingState>
            │               ├── OptionsMenuButton → OpenOptionsMenuCommand
            │               ├── CreditsButton → PushAsync("Credits")
            │               └── ExitButton → ExitGameCommand
            │
            ├─→ 添加到: UiRoot.Page层
            └─→ 显示: 主菜单界面


═════════════════════════════════════════════════════════
状态3: PlayingState (游戏中状态)
═════════════════════════════════════════════════════════
    │
    │ OnEnterAsync执行:
    │
    └─→ IUiRouter.ReplaceAsync("HomeUi")
            │
            ├─→ 查找: UiKey.HomeUi → home_ui.tscn
            ├─→ 实例化: HomeUi控制器(Control节点)
            │       │
            │       └─→ HomeUi._Ready() → SetupEventHandlers()
            │               绑定3个场景切换按钮:
            │               ├── Scene1Button → ReplaceAsync("Scene1")
            │               ├── Scene2Button → ReplaceAsync("Scene2")
            │               └── HomeUiButton → ReplaceAsync("Home")
            │
            ├─→ 替换: 移除旧UI,显示HomeUi
            └─→ 显示: 游戏主页(叠加在游戏场景之上)


═════════════════════════════════════════════════════════
状态4: PausedState (暂停状态)
═════════════════════════════════════════════════════════
    │
    │ OnEnterAsync执行:
    │
    └─→ IUiRouter.PushAsync("PauseMenu")  ← 注意:是Push不是Replace
            │
            ├─→ 查找: UiKey.PauseMenu → pause_menu.tscn
            ├─→ 实例化: PauseMenu控制器(Control节点)
            │       │
            │       └─→ PauseMenu._Ready() → SetupEventHandlers()
            │               绑定6个按钮:
            │               ├── ResumeButton → ResumeGameWithClosePauseMenuCommand
            │               ├── SaveButton → 保存+恢复命令
            │               ├── LoadButton → 加载存档(待实现)
            │               ├── OptionsButton → OpenOptionsMenuCommand
            │               ├── MainMenuButton → Resume+ChangeToAsync<MainMenuState>
            │               └── QuitButton → ExitGameCommand
            │
            ├─→ 入栈: PauseMenu压入UI栈(保留HomeUi在栈中)
            ├─→ 添加到: UiRoot.Modal层(ZIndex更高,覆盖HomeUi)
            └─→ 显示: 暂停菜单(模态对话框样式)


═════════════════════════════════════════════════════════
状态5: GameOverState (游戏结束状态)
═════════════════════════════════════════════════════════
    │
    │ OnEnterAsync执行:(待具体实现)
    │
    └─→ 可能的操作:
            ├── IUiRouter.ReplaceAsync("GameOverUi")  → 显示结束界面
            ├── ISceneRouter.ReplaceAsync("Result")    → 切换到结算场景
            └── 播放结束动画/音效
```

### 4.3 场景切换的双路由协作机制

```
当需要同时切换场景和UI时(例如: MainMenuState → PlayingState):

    ┌─────────────────────────────────────────────┐
    │          PlayingState.OnEnterAsync()         │
    └─────────────────┬───────────────────────────┘
                      │
                      ▼
         IUiRouter.ReplaceAsync("HomeUi")
                      │
                      ├─→ UI路由器负责:
                      │   ├── 查找UI资源配置
                      │   ├── 实例化UI控制器
                      │   ├── 执行UI生命周期(_Ready/OnEnter/OnShow)
                      │   ├── 管理UI节点树(添加/移除)
                      │   └── 维护UI页面栈
                      │
                      ▼
         用户在HomeUi中点击"Scene1"按钮
                      │
                      ▼
         ISceneRouter.ReplaceAsync("Scene1")
                      │
                      ├─→ 场景路由器负责:
                      │   ├── 查找场景资源配置
                      │   ├── 实例化场景控制器
                      │   ├── 执行场景生命周期(_Ready/OnLoad/OnEnter)
                      │   ├── 管理场景节点树(SceneRoot下的子节点)
                      │   └── 触发过渡动画(如果有)
                      │
                      │   ┌──────────────────────────────────┐
                      │   │  SceneTransitionAnimationHandler   │
                      │   │  (Around包裹处理器)                │
                      │   │                                    │
                      │   │  在实际切换前后插入动画:          │
                      │   │  1. 截图当前屏幕(old scene)       │
                      │   │  2. 预渲染新场景(new scene)        │
                      │   │  3. 显示Shader过渡遮罩层           │
                      │   │  4. 执行真正的场景替换             │
                      │   │  5. Tween动画(progress:0→1)       │
                      │   │  6. 清理资源                      │
                      │   └──────────────────────────────────┘
                      │
                      ▼
         屏幕显示Scene1场景(带平滑过渡动画)
```

### 4.4 数据如何在场景间传递

```
方案1: 通过状态类携带上下文
────────────────────────────
    PlayingState
    │
    ├─→ 可持有字段: _currentLevelId, _playerData等
    │
    └─→ OnEnterAsync时读取这些数据进行初始化


方案2: 通过共享Model(推荐)
────────────────────────────
    ISettingsModel (全局单例)
    │
    ├─→ 任何场景/UI都可通过this.GetModel<ISettingsModel>()获取
    │
    ├─→ 数据修改后自动持久化
    │
    └─→ 通过事件通知其他组件数据变更


方案3: 通过CQRS命令传递参数
────────────────────────────
    ChangeResolutionCommandInput
    │
    ├─→ Command对象携带输入数据
    │
    ├─→ Handler接收并处理
    │
    └─→ 适合一次性操作的数据传递


方案4: 通过ISceneEnterParam/IUiPageEnterParam
────────────────────────────────────────────────
    LevelSceneEnterParam : ISceneEnterParam
    │
    ├── int LevelId
    ├── string PlayerName
    └── Difficulty Difficulty
    
    使用:
    await sceneRouter.ChangeAsync("Level", new LevelSceneEnterParam {
        LevelId = 5,
        PlayerName = "Player1",
        Difficulty = Difficulty.Hard
    });
    
    接收端(ISimpleScene.OnLoadAsync):
    if (param is LevelSceneEnterParam p) {
        // 使用p.LevelId等数据初始化场景
    }
```

---

## 第五部分：框架调用链路总结图

### 完整的用户交互→框架→视觉反馈链路

```
用户操作(点击/按键)
    │
    ▼
[Godot引擎层] 信号/事件生成
    │
    ▼
[全局拦截层] GlobalInputController._Input()
    │   (可选: Button直接触发信号跳过此步)
    │
    ▼
[CQRS命令层] this.SendCommand(Command对象)
    │
    ▼
[Mediator分发层] 自动路由到对应的Handler
    │
    ▼
[Handler处理层] CommandHandler.Handle()
    │   ├── 业务逻辑处理
    │   ├── 数据验证/转换
    │   └── 调用下层服务
    │
    ▼
[状态机协调层] IStateMachineSystem.ChangeToAsync<TState>()
    │   ├── 状态合法性检查
    │   ├── 旧状态.OnExit()
    │   ├── 更新Current状态指针
    │   └── 新状态.OnEnter()
    │
    ▼
[路由导航层] IUiRouter / ISceneRouter
    │   ├── PushAsync() / PopAsync() / ReplaceAsync() / ClearAsync()
    │   ├── 查找资源配置(Registry)
    │   ├── 实例化场景/UI控制器
    │   └── 执行生命周期钩子
    │
    ▼
[容器管理层] UiRoot / SceneRoot
    │   ├── AddChild() / RemoveChild()
    │   ├── 管理UI层级(ZIndex/Layer)
    │   └── 维护页面栈/场景列表
    │
    ▼
[控制器初始化层] 具体Controller._Ready()
    │   ├── DI获取所需服务
    │   └── SetupEventHandlers() 绑定交互事件
    │
    ▼
[Godot渲染层] GPU渲染输出到屏幕
    │
    ▼
✅ 用户看到视觉反馈
```

---

## 附录：关键技术点的代码定位索引

| 技术点 | 文件路径 | 关键行号 | 方法名 |
|--------|----------|----------|--------|
| 架构入口 | global/GameEntryPoint.cs | 54-103 | _Ready() |
| 模块安装 | scripts/core/GameArchitecture.cs | 30-40 | InstallModules() |
| 场景容器绑定 | global/SceneRoot.cs | 78-82 | _Ready() |
| UI容器绑定 | global/UiRoot.cs | 88-98 | _Ready() |
| 输入拦截 | global/GlobalInputController.cs | 37-53 | Handle() |
| 状态-主菜单 | scripts/core/state/impls/MainMenuState.cs | 18-28 | OnEnterAsync() |
| 状态-游戏中 | scripts/core/state/impls/PlayingState.cs | 14-19 | OnEnterAsync() |
| 主菜单按钮绑定 | scripts/main_menu/MainMenu.cs | 60-75 | SetupEventHandlers() |
| HomeUi按钮绑定 | scripts/tests/HomeUi.cs | 50-68 | SetupEventHandlers() |
| 暂停菜单按钮绑定 | scripts/pause_menu/PauseMenu.cs | 78-113 | SetupEventHandlers() |
| 过渡动画处理器 | scripts/core/scene/SceneTransitionAnimationHandler.cs | 40-72 | HandleAsync() |

---

> **文档说明**: 本文档聚焦于GFramework框架层的调用机制，省略了Godot引擎内部的C++实现细节。所有流程均基于项目实际代码分析得出。
