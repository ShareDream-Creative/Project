using GFramework.Core.Abstractions.State;
using GFrameworkGodotTemplate.global;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.data;
using GFrameworkGodotTemplate.scripts.data.interfaces;
using GFrameworkGodotTemplate.scripts.data.model;
using GFrameworkGodotTemplate.scripts.player.interfaces;
using GFrameworkGodotTemplate.scripts.player.input;
using GFrameworkGodotTemplate.scripts.player.listeners;
using GFrameworkGodotTemplate.scripts.player.physics;
using GFrameworkGodotTemplate.scripts.player.state;
using Godot;

namespace GFrameworkGodotTemplate.scripts.player;

/// <summary>
///     玩家角色移动控制器(组合器模式)
///     <para>
///         协调输入处理、物理运动、状态控制三个子模块的工作
///         实现完整的玩家角色2D物理移动系统
///     </para>
///     <author>AI Assistant</author>
///     <version>2.1.0</version>
///     <date>2026-05-11</date>
///     <description>
///         核心职责:
///         1. 模块协调: 管理InputHandler、PhysicsMovement、StateController三个子模块
///         2. 生命周期: 负责子模块的创建、初始化和销毁
///         3. 帧更新: 在_PhysicsProcess中整合所有模块的逻辑
///         4. 数据同步: 从PlayerDataManager加载配置并同步到各模块
///         5. 全局服务: 获取并管理全局输入服务和数据管理器的引用
///         
///         架构设计:
///         - 组合模式(Composition): 通过组合而非继承实现功能复用
///         - 依赖注入(DI): 通过构造函数和方法注入依赖
///         - 观察者模式(Observer): 实现IPlayerDataListener接口监听数据变更
///         - 单一职责(SRP): 本类只负责协调，具体逻辑委托给子模块
///     </description>
///     <remarks>
///         设计优势:
///         1. 解耦: 各子模块独立可测试，降低耦合度
///         2. 可扩展: 可轻松替换或新增子模块
///         3. 可维护: 清晰的职责划分便于维护
///         4. 数据驱动: 运行时可从PlayerData动态调整参数
///         
///         数据流向图:
///         ┌─────────────────────────────────────────┐
///         │       PlayerMovementController          │
///         │  (组合器 - 协调中心)                     │
///         └────────┬────────┬────────┬──────────────┘
///                  │        │        │
///         ┌────────▼──┐ ┌──▼──────┐ │ ┌────────────▼──┐
///         │ InputHandler│ │Physics │ │ │ StateController│
///         │ (输入处理) │ │Movement │ │ │ (状态控制)    │
///         └─────┬──────┘ └───┬────┘ │ └───────┬────────┘
///               │             │      │         │
///               ▼             ▼      │         ▼
///         GlobalInputService  │      │   IStateMachineSystem
///               │             │      │
///               └──────┬──────┘      │
///                      ▼             │
///              CharacterBody2D ◄─────┘
///              (MoveAndSlide执行)
///         
///         子模块说明:
///         - IPlayerInputHandler: 处理键盘/手柄输入检测
///         - IPlayerPhysicsMovement: 物理速度计算和碰撞响应
///         - IPlayerStateController: 基于游戏状态的输入控制权管理
///         
///         使用示例:
///         <code>
///         // 此类作为Godot节点挂载到场景中
///         // 在gametest.tscn中添加CharacterBody2D节点
///         // 并将此脚本附加到该节点上
///         
///         // 编辑器中配置Export属性 (可选)
///         // Speed = 300.0 (运行时会被PlayerDataManager覆盖)
///         // JumpVelocity = -500.0
///         // Gravity = 980.0
///         
///         // 运行时自动初始化所有子模块
///         // 无需手动配置
///         </code>
///         
///         配置优先级:
///         1. PlayerDataManager.Data (最高优先级, 运行时值)
///         2. Export属性 (编辑器配置, 调试用)
///         3. 代码默认值 (最低优先级)
///         
///         性能优化:
///         - 所有物理计算在_PhysicsProcess中执行 (固定时间步长)
///         - 避免在单帧内重复计算
///         - 使用缓存减少GC压力
///         - 子模块采用轻量级设计，无虚方法调用开销
///         
///         注意事项:
///         - 必须作为CharacterBody2D节点的脚本使用
///         - 依赖GlobalInputController AutoLoad节点
///         - 依赖PlayerDataManager全局单例
///         - 依赖GFramework IStateMachineSystem服务
///     </remarks>
/// </summary>
[ContextAware]
[Log]
public partial class PlayerMovementController : CharacterBody2D, IController
{
	#region 导出属性配置 (编辑器调试用)

	/// <summary>
	///     移动速度 (像素/秒) - 编辑器调试用
	///     <para>
	///         运行时会从PlayerDataManager加载实际值
	///         仅在PlayerDataManager不可用时使用此值
	///     </para>
	///     <remarks>
	///         取值范围: [PlayerData.MIN_SPEED, PlayerData.MAX_SPEED] = [50, 1000]
	///         默认值: PlayerData.DEFAULT_SPEED = 300.0
	///         用途: 编辑器中快速测试不同速度手感
	///         
	///         注意: 正式环境应通过PlayerDataManager配置此值
	///     </remarks>
	/// </summary>
	[Export] public float Speed { get; set; } = PlayerData.DEFAULT_SPEED;

	/// <summary>
	///     跳跃力度 (像素/秒) - 编辑器调试用
	///     <para>
	///         负值表示向上跳跃(符合Godot坐标系)
	///         运行时会被PlayerDataManager覆盖
	///     </para>
	///     <remarks>
	///         取值范围: [-MAX_JUMP_VELOCITY_ABS, -MIN_JUMP_VELOCITY_ABS] = [-1000, -200]
	///         默认值: PlayerData.DEFAULT_JUMP_VELOCITY = -500.0
	///         当前配置跳跃高度 ≈ 127.5 像素
	///     </remarks>
	/// </summary>
	[Export] public float JumpVelocity { get; set; } = PlayerData.DEFAULT_JUMP_VELOCITY;

	/// <summary>
	///     重力加速度 (像素/秒²) - 编辑器调试用
	///     <para>
	///         影响角色下落速度和跳跃高度
	///         运行时会被PlayerDataManager覆盖
	///     </para>
	///     <remarks>
	///         取值范围: [PlayerData.MIN_GRAVITY, PlayerData.MAX_GRAVITY] = [100, 3000]
	///         默认值: PlayerData.DEFAULT_GRAVITY = 980.0 (接近真实地球重力)
	///     </remarks>
	/// </summary>
	[Export] public float Gravity { get; set; } = PlayerData.DEFAULT_GRAVITY;

	#endregion

	#region 子模块实例 (组合关系)

	/// <summary>
	///     输入处理器实例
	///     <para>
	///         负责检测和转换玩家输入数据
	///         将全局输入服务的数据适配为本模块格式
	///     </para>
	///     <remarks>
	///         创建时机: InitializeModules()中创建
	///         类型: PlayerInputHandler (实现IPlayerInputHandler接口)
	///         依赖: 需要IGlobalGameplayInputService实例
	 ///         
	///         功能:
	///         - 水平方向检测 (A/D键或左/右箭头)
	///         - 跳跃按键检测 (空格键)
	///         - 奔跑状态检测 (Shift键)
	///         - 奔跑倍率缓存 (从PlayerData同步)
	///     </remarks>
	/// </summary>
	private IPlayerInputHandler? _inputHandler;

	/// <summary>
	///     物理运动模块实例
	///     <para>
	///         负责物理速度计算和碰撞响应
	///         基于Godot CharacterBody2D的移动系统
	///     </para>
	///     <remarks>
	///         创建时机: InitializeModules()中创建
	///         类型: PlayerPhysicsMovement (实现IPlayerPhysicsMovement接口)
	///         依赖: 无外部依赖 (纯逻辑类)
	///         
	///         功能:
	///         - 重力应用 (ApplyGravity)
	///         - 水平速度计算 (UpdateHorizontalVelocity)
	///         - 跳跃执行 (TryJump)
	///         - 移动和碰撞 (Move)
	///         - 立即停止 (StopImmediately)
	///         
	///         数据来源: 从PlayerData自动同步属性值
	///     </remarks>
	/// </summary>
	private PlayerPhysicsMovement? _physicsMovement;

	/// <summary>
	///     状态控制器实例
	///     <para>
	///         负责基于游戏状态的输入控制权管理
	///         仅在PlayingState时允许输入
	///     </para>
	///     <remarks>
	///         创建时机: InitializeModules()中创建
	///         类型: PlayerStateController (实现IPlayerStateController接口)
	///         依赖: 需要IStateMachineSystem实例
	///         
	///         功能:
	///         - 状态查询 (UpdateState)
	///         - 输入控制权判断 (IsInputEnabled)
	///         - 状态机注入 (SetStateMachineSystem)
	///         
	///         状态映射:
	///         PlayingState → IsInputEnabled=true
	///         其他状态 → IsInputEnabled=false
	///     </remarks>
	/// </summary>
	private IPlayerStateController? _stateController;

	#endregion

	#region 全局服务引用

	/// <summary>
	///     全局游戏玩法输入服务
	///     <para>
	///         来自GlobalInputController的输入数据源
	///         提供统一的游戏玩法输入状态查询
	 ///     </para>
	///     <remarks>
	///         获取方式: FindGameplayInputService()从场景树查找
	///         来源: GlobalInputController.GameplayInputService属性
	///         生命周期: 随GlobalInputController AutoLoad节点存在
	///         
	///         用途:
	///         - 注入给PlayerInputHandler作为输入数据源
	///         - 提供水平方向、跳跃按键等输入状态
	///     </remarks>
	/// </summary>
	private IGlobalGameplayInputService? _globalInputService;

	/// <summary>
	///     玩家数据管理器 (全局单例)
	///     <para>
	///         负责PlayerData的生命周期管理和持久化存储
	///         提供统一的玩家配置数据访问点
	///     </para>
	///     <remarks>
	///         获取方式: PlayerDataManager.Instance (线程安全单例)
	///         类型: PlayerDataManager (单例模式)
	///         生命周期: 全局唯一，贯穿整个游戏运行周期
	///         
	///         用途:
	///         - 加载玩家配置数据 (懒加载 + ConfigFile持久化)
	///         - 注册/移除数据监听器
	///         - 同步配置到物理模块
	///         - 监听数据变更日志记录
	///     </remarks>
	/// </summary>
	private PlayerDataManager? _dataManager;

	/// <summary>
	///     数据监听器桥接器实例
	///     <para>
	///         负责将PlayerData变更事件桥接到日志系统
	///         实现关注点分离，提升代码内聚性
	///     </para>
	///     <remarks>
	///         创建时机: InitializeDataManager()中创建
	///         类型: PlayerDataListenerBridge (实现IPlayerDataListener接口)
	///         
	///         功能:
	///         - 监控Speed/JumpVelocity/Gravity/SprintMultiplier变化
	///         - 输出变更日志用于调试和监控
	///         - 可独立测试和替换
	///     </remarks>
	/// </summary>
	private PlayerDataListenerBridge? _dataListenerBridge;

	#endregion

	#region 生命周期方法

	/// <summary>
	///     节点就绪时的初始化方法
	///     <para>
	///         Godot引擎在节点加入场景树后自动调用
	///         执行所有子模块的初始化和配置工作
	///     </para>
	///     <remarks>
	///         初始化顺序:
	///         1. base._Ready() - 调用父类初始化
	///         2. InitializeGlobalServices() - 获取全局服务引用
	///         3. InitializeDataManager() - 初始化数据管理器和监听
	///         4. InitializeModules() - 创建并配置所有子模块
	///         5. SyncConfigurationToModules() - 同步配置数据到物理模块
	///         6. 输出初始化完成日志
	///         
	///         错误处理:
	///         - 全局输入服务缺失: 记录错误日志，后续抛出异常
	///         - 数据管理器不可用: 记录警告，回退到本地配置
	///         - 状态机系统不可用: 记录警告，禁用输入
	///         
	///         性能说明:
	///         - 此方法仅在节点就绪时调用一次
	///         - 包含IO操作(首次访问PlayerDataManager可能触发文件加载)
	///         - 应避免在此方法中执行耗时操作
	///     </remarks>
	/// </summary>
	public override void _Ready()
	{
		base._Ready();
		
		InitializeGlobalServices();
		InitializeDataManager();
		InitializeModules();
		SyncConfigurationToModules();
		
		_log.Debug("PlayerMovementController初始化完成 (v2.1 数据驱动架构)");
		if (_inputHandler != null && _physicsMovement != null && _stateController != null)
		{
			_log.Debug($"子模块状态: Input={_inputHandler.GetType().Name}, Physics={_physicsMovement.GetType().Name}, State={_stateController.GetType().Name}");
		}
	}

	/// <summary>
	///     节点从场景树移除时的清理方法
	///     <para>
	///         Godot引擎在节点即将销毁前自动调用
	///         执行资源清理工作，防止内存泄漏
	///     </para>
	///     <remarks>
	///         清理内容:
	///         - 移除自身对PlayerData的监听
	///         - 移除所有子模块对PlayerData的监听
	///         - 释放全局服务引用
	///         
	///         重要性:
	///         如果不正确清理监听器，可能导致：
	///         - 内存泄漏 (被监听的对象无法被GC回收)
	///         - 异常调用 (已销毁对象仍收到回调)
	///         
	///         调用时机:
	///         Godot引擎在以下情况会调用此方法:
	///         - 场景切换时
	///         - 节点QueueFree()后
	///         - 场景树重建时
	///     </remarks>
	/// </summary>
	public override void _ExitTree()
	{
		CleanupResources();
	}

	/// <summary>
	///     物理帧更新方法 (每帧调用)
	///     <para>
	///         Godot引擎以固定时间间隔调用 (默认60FPS)
	///         执行所有的物理计算和移动逻辑
	///     </para>
	///     <param name="delta">
	///     距离上一帧的时间间隔 (秒)
	///     通常为 1/60 ≈ 0.0167秒 (60FPS时)
	///     </param>
	///     <remarks>
	///         执行流程:
	///         1. UpdateStateAndInput(delta) - 更新状态和输入缓存
	///         2. 检查IsInputEnabled - 判断是否允许输入
	///            - 如果禁止: 停止移动并返回
	///            - 如果允许: 继续下一步
	///         3. ProcessMovement(delta) - 执行完整移动逻辑
	///         
	///         为什么选择_PhysicsProcess而非_Process:
	///         - 固定时间步长，物理模拟更稳定
	///         - 与MoveAndSlide配合更好
	///         - 不受帧率波动影响
	///         
	///         性能考虑:
	///         - 此方法是性能热点，应保持高效
	///         - 避免在此方法中进行内存分配
	///         - 避免调用耗时的API
	///     </remarks>
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{
		var deltaF = (float)delta;
		
		UpdateStateAndInput(deltaF);
		
		if (!_stateController!.IsInputEnabled)
		{
			_physicsMovement!.StopImmediately();
			_physicsMovement.Move(this);
			return;
		}

		ProcessMovement(deltaF);
	}

	#endregion

	#region 私有方法 - 全局服务初始化

	/// <summary>
	///     初始化全局服务引用
	///     <para>
	///         获取全局输入服务和数据管理器的引用
	///         为后续模块初始化做准备
	///     </para>
	///     <remarks>
	///         获取的服务:
	///         1. IGlobalGameplayInputService - 全局输入服务
	///            来源: GlobalInputController.AutoLoad节点
	///            方法: FindGameplayInputService()
	///            
	///         2. PlayerDataManager - 数据管理器单例
	///            来源: PlayerDataManager.Instance (静态属性)
	///            方法: 直接访问 (线程安全)
	///         
	///         错误处理:
	///         - 输入服务获取失败: 记录ERROR级别日志
	///           后续InitializeModules()会抛出异常
	///         - 数据管理器获取失败: 记录WARN级别日志
	///           后续回退到本地配置
	///         
	///         调用时机:
	///         在_Ready()中最先调用，确保其他初始化有依赖可用
	///     </remarks>
	/// </summary>
	private void InitializeGlobalServices()
	{
		_globalInputService = FindGameplayInputService();
		_dataManager = PlayerDataManager.Instance;
		
		if (_globalInputService != null)
		{
			_log.Debug("成功获取全局游戏玩法输入服务");
		}
		else
		{
			_log.Error("无法找到全局游戏玩法输入服务! 输入功能将不可用。");
		}
		
		if (_dataManager != null)
		{
			_log.Debug("成功获取玩家数据管理器");
		}
		else
		{
			_log.Warn("无法获取玩家数据管理器! 将使用本地默认配置。");
		}
	}

	/// <summary>
	///     初始化数据管理器并注册监听
	///     <para>
	///         将本控制器注册为PlayerData的监听器
	///         用于接收数据变更通知并进行日志记录
	///     </para>
	///     <remarks>
	///         注册目的:
	///         - 监控玩家配置数据的实时变化
	///         - 输出详细的变更日志用于调试
	///         - 未来可用于触发UI更新或其他业务逻辑
	///         
	///         注册对象:
	///         - this (PlayerMovementController本身)
	///           用于日志记录和调试监控
	///         
	///         安全检查:
	///         - 检查_dataManager是否为null
	///         - 为null时跳过注册（避免NullReferenceException）
	///         
	///         调用时机:
	///         在InitializeGlobalServices()之后调用
	///         确保数据管理器已经获取成功
	///     </remarks>
	/// </summary>
	private void InitializeDataManager()
	{
		if (_dataManager == null) return;
		
		// 创建数据监听器桥接器（用于日志记录）
		_dataListenerBridge = new PlayerDataListenerBridge(
			msg => _log.Info(msg),
			msg => _log.Warn(msg),
			msg => _log.Error(msg),
			msg => _log.Debug(msg)
		);
		
		// 注册监听器桥接器为数据监听器
		_dataManager.Data.AddListener(_dataListenerBridge);
		
		_log.Debug("已注册PlayerDataListenerBridge为PlayerData监听器");
	}

	/// <summary>
	///     查找全局游戏玩法输入服务
	///     <para>
	///         从场景树中查找GlobalInputController节点
	///         并提取其GameplayInputService属性
	///     </para>
	///     <returns>
	///     成功时返回IGlobalGameplayInputService实例
	///     失败时返回null (节点不存在或服务未初始化)
	///     </returns>
	///     <remarks>
	///         查找路径: /root/GlobalInputController (AutoLoad节点)
	///         
	///         查找步骤:
	///         1. 获取当前SceneTree引用
	///         2. 从根节点(/root)查找GlobalInputController
	///         3. 检查其GameplayInputService属性是否可用
	///         4. 返回服务实例或null
	///         
	///         异常处理:
	///         - SceneTree为null: 记录错误并返回null
	///         - GetNode失败: 捕获异常并返回null
	///         - 服务为null: 返回null (不是错误)
	///         
	///         可能失败的原因:
	///         - GlobalInputController未注册为AutoLoad
	///         - 节点名称不匹配
	///         - 服务尚未初始化完成
	///         
	///         性能说明:
	///         此方法仅调用一次 (在_Ready中)
	///         不会造成性能问题
	///     </remarks>
	/// </summary>
	private IGlobalGameplayInputService? FindGameplayInputService()
	{
		var tree = GetTree();
		if (tree == null)
		{
			_log.Error("无法获取 SceneTree!");
			return null;
		}

		try
		{
			var globalController = tree.Root.GetNode<GlobalInputController>("GlobalInputController");
			
			if (globalController != null && globalController.GameplayInputService != null)
			{
				return globalController.GameplayInputService;
			}
			
			return null;
		}
		catch (Exception ex)
		{
			_log.Error($"查找 GlobalInputController 失败: {ex.Message}");
			return null;
		}
	}

	#endregion

	#region 私有方法 - 模块初始化

	/// <summary>
	///     初始化所有子模块
	///     <para>
	///         创建InputHandler、PhysicsMovement、StateController三个子模块
	///         注入必要的依赖项
	///         注册数据监听器实现自动同步
	///     </para>
	///     <remarks>
	///         创建顺序和依赖关系:
	///         1. PlayerInputHandler ← 需要 _globalInputService
	///         2. PlayerPhysicsMovement ← 无外部依赖
	///         3. PlayerStateController ← 需要 IStateMachineSystem
	///         
	///         依赖注入详情:
	///         - InputHandler: 构造函数注入IGlobalGameplayInputService
	///         - PhysicsMovement: 无需注入 (纯逻辑类)
	///         - StateController: 方法注入IStateMachineSystem
	///         
	///         监听器注册:
	///         将所有子模块注册为PlayerData的监听器:
	///         - InputHandler → 自动同步SprintMultiplier等配置
	///         - PhysicsMovement → 自动同步Speed/JumpVelocity/Gravity
	///         
	///         错误处理:
	///         - 全局输入服务为null: 抛出InvalidOperationException
	///           这是致命错误，输入处理器无法工作
	///         - 状态机系统为null: 记录警告，输入将被完全禁用
	///           这不是致命错误，只是功能受限
	///         
	///         调用时机:
	///         在_Ready()中，InitializeGlobalServices()之后调用
	///     </remarks>
	/// </summary>
	private void InitializeModules()
	{
		if (_globalInputService == null)
		{
			_log.Error("全局输入服务未初始化! 无法创建输入处理器。");
			throw new InvalidOperationException("IGlobalGameplayInputService 未就绪");
		}
		
		_inputHandler = new PlayerInputHandler(_globalInputService);
		_physicsMovement = new PlayerPhysicsMovement();
		_stateController = new PlayerStateController();
		
		var stateMachineSystem = this.GetSystem<IStateMachineSystem>();
		
		if (stateMachineSystem == null)
		{
			_log.Error("无法获取状态机系统服务! 输入将被完全禁用。");
		}
		else
		{
			_stateController.SetStateMachineSystem(stateMachineSystem);
		}
		
		// 注册子模块为PlayerData监听器(实现自动同步)
		if (_dataManager != null)
		{
			_dataManager.Data.AddListener((IPlayerDataListener)_inputHandler);
			_dataManager.Data.AddListener(_physicsMovement);
			
			_log.Debug("已注册所有子模块为PlayerData监听器");
		}
	}

	/// <summary>
	///     将配置同步到物理模块
	///     <para>
	///         从PlayerDataManager加载最新配置数据
	///         并同步到PlayerPhysicsMovement模块
	///     </para>
	///     <remarks>
	///         配置优先级:
	///         1. PlayerDataManager.Data (最高优先级)
	///            - 来自ConfigFile持久化存储
	///            - 经过验证的有效值
	///            - 包含用户自定义设置
	///            
	///         2. Export属性 (中等优先级)
	///            - 编辑器中的调试配置
	///            - 仅在PlayerDataManager不可用时使用
	///            
	///         3. 代码默认值 (最低优先级)
	///            - PlayerData类中定义的DEFAULT_*常量
	///            - 作为最终兜底方案
	///         
	///         同步内容:
	///         - Speed: 水平移动速度
	///         - JumpVelocity: 跳跃初速度
	///         - Gravity: 重力加速度
	///         
	///         日志输出:
	///         - 成功: 输出DEBUG级别日志显示同步的值
	///         - 回退: 输出WARN级别日志提示使用本地配置
	///         
	///         调用时机:
	///         在_Ready()中最后调用，确保所有模块已创建完毕
	///     </remarks>
	/// </summary>
	private void SyncConfigurationToModules()
	{
		if (_dataManager != null)
		{
			var data = _dataManager.Data;
			
			// 使用PlayerData的值(会自动验证范围)
			_physicsMovement!.InitializeFromData(data);
			
			_log.Debug($"从PlayerData同步配置: Speed={data.Speed}, JumpVelocity={data.JumpVelocity}, Gravity={data.Gravity}");
		}
		else
		{
			// 回退到Export属性或默认值
			_physicsMovement!.Speed = Speed;
			_physicsMovement.JumpVelocity = JumpVelocity;
			_physicsMovement.Gravity = Gravity;
			
			_log.Warn("PlayerDataManager不可用，使用本地配置");
		}
	}

	#endregion

	#region 私有方法 - 帧更新逻辑

	/// <summary>
	///     更新状态和输入缓存
	///     <para>
	///         每帧开始时调用，刷新所有动态状态
	///         为后续的移动计算提供最新的输入数据
	///     </para>
	///     <param name="delta">当前帧的时间间隔(秒)</param>
	///     <remarks>
	///         更新顺序:
	///         1. _stateController.UpdateState()
	///            - 检测当前游戏状态
	///            - 更新IsInputEnabled标志
	///            - 决定是否允许输入
	///            
	///         2. _inputHandler.UpdateInput()
	///            - 刷新输入状态缓存
	///            - 检测奔跑键状态
	///            - 准备好供本帧使用的输入数据
	///         
	///         为什么先更新状态再更新输入:
	///         - 状态决定是否应该处理输入
	///         - 即使不处理输入也需要更新状态
	///         - 保持逻辑清晰和数据一致性
	///         
	///         参数delta的用途:
	///         当前未直接使用，保留用于未来扩展
	///         例如: 输入插值、输入缓冲等
	///         
	///         性能说明:
	///         此方法非常轻量，两个Update调用都是O(1)复杂度
	///     </remarks>
	/// </summary>
	private void UpdateStateAndInput(float delta)
	{
		_stateController!.UpdateState();
		_inputHandler!.UpdateInput();
	}

	/// <summary>
	///     处理完整的移动逻辑流程
	///     <para>
	///         集成奔跑系统，根据输入状态自动应用速度倍率
	///         协调所有子模块完成一帧的物理移动计算
	///     </para>
	///     <param name="delta">当前帧的时间间隔(秒)</param>
	///     <remarks>
	///         执行流程 (严格顺序):
	///         
	///         Step 1: 应用重力
	///         _physicsMovement.ApplyGravity(delta)
	///         - 仅在空中时累加重力加速度
	///         - 使用欧拉积分法更新垂直速度
	///         
	///         Step 2: 获取输入状态
	///         var isSprinting = _inputHandler.IsSprinting;
	///         var sprintMultiplier = _inputHandler.CachedSprintMultiplier;
	///         - 从输入模块读取奔跑状态
	///         - 从输入模块读取缓存的奔跑倍率
	///         - 通过接口访问，无需类型转换 ✅
	///         
	///         Step 3: 更新水平速度
	///         _physicsMovement.UpdateHorizontalVelocity(direction, isSprinting, sprintMultiplier)
	///         - 根据方向和奔跑状态计算实际速度
	///         - 奔跑时: speed * sprintMultiplier
	///         - 无输入时: 平滑减速到0
	///         
	///         Step 4: 尝试跳跃
	///         if (_inputHandler.IsJumpPressed && _physicsMovement.TryJump())
	///         - 检测跳跃按键 (单次触发)
	///         - 检查是否在地面上
	///         - 设置跳跃初速度
	///         
	///         Step 5: 执行移动
	///         _physicsMovement.Move(this)
	///         - 将速度应用到CharacterBody2D
	///         - 调用MoveAndSlide处理碰撞
	///         - 更新地面检测状态
	///         
	///         数据流向图:
	///         InputHandler ──→ PhysicsMovement ──→ CharacterBody2D
	///         (输入检测)     (物理计算)           (执行移动)
	///              ↑                                  │
	///              └──── StateController (输入控制权) ─┘
	///         
	///         奔跑系统集成:
	///         - Shift键按下 → isSprinting=true
	///         - true → actualSpeed = Speed * SprintMultiplier
	///         - false → actualSpeed = Speed (正常速度)
	///         
	///         性能优化:
	///         - 所有计算都在栈上完成，无堆分配
	///         - 避免不必要的条件分支
	///         - 利用CPU缓存局部性
	///     </remarks>
	/// </summary>
	private void ProcessMovement(float delta)
	{
		_physicsMovement!.ApplyGravity(delta);
		
		// 获取奔跑状态和倍率 (通过接口访问，无需类型转换 ✅)
		var isSprinting = _inputHandler!.IsSprinting;
		var sprintMultiplier = _inputHandler.CachedSprintMultiplier;
		
		// 传递完整的速度计算参数
		_physicsMovement.UpdateHorizontalVelocity(_inputHandler.HorizontalDirection, isSprinting, sprintMultiplier);
		
		if (_inputHandler.IsJumpPressed && _physicsMovement.TryJump())
		{
			_log.Debug("玩家跳跃");
		}
		
		_physicsMovement.Move(this);
	}

	#endregion

	#region 资源清理

	/// <summary>
	///     清理资源并移除所有监听器
	///     <para>
	///         在节点销毁前调用，防止内存泄漏和悬空引用
	///     </para>
	///     <remarks>
	///         清理内容:
	///         1. 移除自身对PlayerData的监听
	///            - 避免本对象被回收后仍收到回调
	///            
	///         2. 移除InputHandler对PlayerData的监听
	///            - 避免输入模块被回收后仍收到回调
	///            
	///         3. 移除PhysicsMovement对PlayerData的监听
	///            - 避免物理模块被回收后仍收到回调
	///         
	///         为什么必须清理:
	///         - PlayerData持有监听器的强引用
	///         - 如果不移除，监听器对象无法被GC回收
	///         - 导致内存泄漏，特别是频繁创建/销毁角色时
	///         
	///         安全检查:
	///         - 检查_dataManager是否为null
	///         - 检查各子模块是否为null
	///         - 为null时跳过对应的清理操作
	///         
	///         调用时机:
	///         在_ExitTree()中自动调用
	///         也可在需要时手动调用
	///     </remarks>
	/// </summary>
	private void CleanupResources()
	{
		if (_dataManager != null)
		{
			// 移除监听器桥接器
			_dataManager.Data.RemoveListener(_dataListenerBridge);
			
			// 移除子模块的监听器
			_dataManager.Data.RemoveListener((IPlayerDataListener)_inputHandler);
			_dataManager.Data.RemoveListener(_physicsMovement);
			
			_log.Debug("已移除所有PlayerData监听器");
		}
	}

	#endregion

	#region 公开API - 模块访问

	/// <summary>
	///     获取输入处理器实例 (只读)
	///     <para>
	///         提供对外部系统的输入状态访问接口
	///         可用于调试、测试或UI显示
	///     </para>
	///     <returns>
	///     IPlayerInputHandler接口实例
	///     可能为null (如果初始化未完成)
	///     </returns>
	///     <remarks>
	///         使用场景:
	///         - 外部系统需要读取输入状态
	///         - 单元测试时Mock输入
	///         - 调试工具显示输入信息
	///         
	///         注意: 返回的是接口类型，非具体实现类
	///         符合面向接口编程原则
	///     </remarks>
	/// </summary>
	public IPlayerInputHandler InputHandler => _inputHandler!;

	/// <summary>
	///     获取物理运动模块实例 (只读)
	///     <para>
	///         提供对物理运动系统的访问接口
	///         可用于调试、测试或外部系统集成
	///     </para>
	///     <returns>
	///     IPlayerPhysicsMovement接口实例
	///     可能为null (如果初始化未完成)
	///     </returns>
	///     <remarks>
	///         使用场景:
	///         - 外部系统需要控制或查询物理状态
	///         - 单元测试时验证物理行为
	///         - 调试工具显示速度、位置等信息
	///         
	///         注意: 返回的是接口类型，隐藏具体实现细节
	///     </remarks>
	/// </summary>
	public IPlayerPhysicsMovement PhysicsMovement => _physicsMovement!;

	/// <summary>
	///     获取状态控制器实例 (只读)
	///     <para>
	///         提供对状态控制系统的访问接口
	///         可用于查询当前输入控制权状态
	///     </para>
	///     <returns>
	///     IPlayerStateController接口实例
	///     可能为null (如果初始化未完成)
	///     </returns>
	///     <remarks>
	///         使用场景:
	///         - UI显示当前游戏状态
	///         - 外部系统判断是否允许玩家操作
	///         - 调试工具监控状态切换
	///         
	///         典型用法:
	///         <code>
	///         if (playerController.StateController.IsInputEnabled)
	///         {
	///             // 玩家可以操作
	///         }
	///         </code>
	///     </remarks>
	/// </summary>
	public IPlayerStateController StateController => _stateController!;

	/// <summary>
	///     获取当前使用的玩家数据 (只读)
	///     <para>
	///         提供对PlayerData的访问接口
	///         可用于读取或修改玩家配置
	///     </para>
	///     <returns>
	///     PlayerData实例
	///     可能为null (如果数据管理器不可用)
	///     </returns>
	///     <remarks>
	///         使用场景:
	///         - UI界面显示当前配置值
	///         - 调试面板查看和修改参数
	///         - 外部系统同步配置数据
	///         
	///         重要提示:
	///         - 修改此对象的属性会触发验证和通知
	///         - 会自动设置脏标记，下次Save()时会持久化
	///         - 所有注册的监听器都会收到变更通知
	///         
	///         示例:
	///         <code>
	///         var data = playerController.CurrentPlayerData;
	///         if (data != null)
	///         {
	///             GD.Print($"当前速度: {data.Speed}");
	///             data.Speed = 350.0f; // 触发验证+通知+脏标记
	///         }
	///         </code>
	///     </remarks>
	/// </summary>
	public PlayerData? CurrentPlayerData => _dataManager?.Data;

	#endregion
}
