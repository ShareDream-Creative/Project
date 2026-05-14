using System;
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
///     <author></author>
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
/// </summary>
[ContextAware]
[Log]
public partial class PlayerMovementController : CharacterBody2D, IController, IPlayerDataListener
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
	///         ✨ 增强功能:
	///         在Godot Inspector中修改此属性时，会自动实时同步到物理模块
	///         无需重启游戏或手动刷新，修改立即生效
	///         
	///         注意: 正式环境应通过PlayerDataManager配置此值
	///     </remarks>
	/// </summary>
	private float _speedExport = PlayerData.DEFAULT_SPEED;
	
	[Export]
	public float Speed
	{
		get => _speedExport;
		set
		{
			if (Math.Abs(_speedExport - value) < 0.001f) return;
			
			var oldValue = _speedExport;
			_speedExport = value;
			
			if (_physicsMovement != null)
			{
				_physicsMovement.Speed = value;
				GD.Print($"[PlayerMovementController] 🎮 Inspector修改速度: {oldValue:F1} → {value:F1} (已实时同步到物理模块)");
			}
		}
	}

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
	///         
	///         ✨ 增强功能:
	///         在Godot Inspector中修改此属性时，会自动实时同步到物理模块
	///         无需重启游戏或手动刷新，修改立即生效
	///     </remarks>
	/// </summary>
	private float _jumpVelocityExport = PlayerData.DEFAULT_JUMP_VELOCITY;
	
	[Export]
	public float JumpVelocity
	{
		get => _jumpVelocityExport;
		set
		{
			if (Math.Abs(_jumpVelocityExport - value) < 0.001f) return;
			
			var oldValue = _jumpVelocityExport;
			_jumpVelocityExport = value;
			
			if (_physicsMovement != null)
			{
				_physicsMovement.JumpVelocity = value;
				GD.Print($"[PlayerMovementController] 🎮 Inspector修改跳跃速度: {oldValue:F1} → {value:F1} (已实时同步到物理模块)");
			}
		}
	}

	/// <summary>
	///     重力加速度 (像素/秒²) - 编辑器调试用
	///     <para>
	///         影响角色下落速度和跳跃高度
	///         运行时会被PlayerDataManager覆盖
	///     </para>
	///     <remarks>
	///         取值范围: [PlayerData.MIN_GRAVITY, PlayerData.MAX_GRAVITY] = [100, 3000]
	///         默认值: PlayerData.DEFAULT_GRAVITY = 980.0 (接近真实地球重力)
	///         
	///         ✨ 增强功能:
	///         在Godot Inspector中修改此属性时，会自动实时同步到物理模块
	///         无需重启游戏或手动刷新，修改立即生效
	///     </remarks>
	/// </summary>
	private float _gravityExport = PlayerData.DEFAULT_GRAVITY;
	
	[Export]
	public float Gravity
	{
		get => _gravityExport;
		set
		{
			if (Math.Abs(_gravityExport - value) < 0.001f) return;
			
			var oldValue = _gravityExport;
			_gravityExport = value;
			
			if (_physicsMovement != null)
			{
				_physicsMovement.Gravity = value;
				GD.Print($"[PlayerMovementController] 🎮 Inspector修改重力: {oldValue:F1} → {value:F1} (已实时同步到物理模块)");
			}
		}
	}

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
	/// </summary>
	private PlayerPhysicsMovement? _physicsMovement;

	/// <summary>
	///     状态控制器实例
	///     <para>
	///         负责基于游戏状态的输入控制权管理
	///         仅在PlayingState时允许输入
	///     </para>
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
	/// </summary>
	private IGlobalGameplayInputService? _globalInputService;

	/// <summary>
	///     玩家数据管理器 (全局单例)
	///     <para>
	///         负责PlayerData的生命周期管理和持久化存储
	///         提供统一的玩家配置数据访问点
	///     </para>
	/// </summary>
	private PlayerDataManager? _dataManager;

	/// <summary>
	///     数据监听器桥接器实例
	///     <para>
	///         负责将PlayerData变更事件桥接到日志系统
	///         实现关注点分离，提升代码内聚性
	///     </para>
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
		
		// 注册自身为数据监听器（实现Export属性→PhysicsModule的双向同步）
		_dataManager.Data.AddListener(this);
		
		_log.Debug("已注册PlayerDataListenerBridge和PlayerMovementController为PlayerData监听器");
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
	/// </summary>
	private void CleanupResources()
	{
		if (_dataManager != null)
		{
			// 移除自身监听器
			_dataManager.Data.RemoveListener(this);
			
			// 移除监听器桥接器
			_dataManager.Data.RemoveListener(_dataListenerBridge);
			
			// 移除子模块的监听器
			_dataManager.Data.RemoveListener((IPlayerDataListener)_inputHandler);
			_dataManager.Data.RemoveListener(_physicsMovement);
			
			_log.Debug("已移除所有PlayerData监听器（包括自身）");
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
	///         </code>
	///     </remarks>
	/// </summary>
	public PlayerData? CurrentPlayerData => _dataManager?.Data;

	#endregion

	#region IPlayerDataListener接口实现（Export属性双向同步）

	/// <summary>
	///     当玩家移动速度发生变化时，同步到物理模块和Export属性
	///     <para>
	///         实现PlayerData → PhysicsModule → Export属性的完整数据流
	///         确保Godot调试界面修改能够实时反映到游戏运行效果
	///     </para>
	///     <param name="oldValue">旧的速度值(像素/秒)</param>
	/// <param name="newValue">新的速度值(像素/秒)</param>
	/// </summary>
	public void OnSpeedChanged(float oldValue, float newValue)
	{
		if (_physicsMovement != null)
		{
			_physicsMovement.Speed = newValue;
		}
		
		this.Speed = newValue;
		
		GD.Print($"[PlayerMovementController] ✅ 速度已同步: {oldValue:F1} → {newValue:F1} (PhysicsModule + Export)");
	}

	/// <summary>
	///     当跳跃速度发生变化时，同步到物理模块和Export属性
	///     <param name="oldValue">旧的跳跃速度值(像素/秒)</param>
	/// <param name="newValue">新的跳跃速度值(像素/秒)</param>
	/// <remarks>
	///         同OnSpeedChanged()的数据流逻辑
	///         跳跃速度为负数表示向上（符合Godot坐标系）
	///     </remarks>
	/// </summary>
	public void OnJumpVelocityChanged(float oldValue, float newValue)
	{
		if (_physicsMovement != null)
		{
			_physicsMovement.JumpVelocity = newValue;
		}
		
		this.JumpVelocity = newValue;
		
		GD.Print($"[PlayerMovementController] ✅ 跳跃速度已同步: {oldValue:F1} → {newValue:F1} (PhysicsModule + Export)");
	}

	/// <summary>
	///     当重力加速度发生变化时，同步到物理模块和Export属性
	///     <param name="oldValue">旧的重力值(像素/秒²)</param>
	/// <param name="newValue">新的重力值(像素/秒²)</param>
	/// <remarks>
	///         同OnSpeedChanged()的数据流逻辑
	///         重力影响角色下落速度和跳跃高度
	///     </remarks>
	/// </summary>
	public void OnGravityChanged(float oldValue, float newValue)
	{
		if (_physicsMovement != null)
		{
			_physicsMovement.Gravity = newValue;
		}
		
		this.Gravity = newValue;
		
		GD.Print($"[PlayerMovementController] ✅ 重力已同步: {oldValue:F1} → {newValue:F1} (PhysicsModule + Export)");
	}

	/// <summary>
	///     当奔跑倍率发生变化时的处理
	///     <param name="oldValue">旧的奔跑倍率</param>
	/// <param name="newValue">新的奔跑倍率</param>
	/// <remarks>
	///         当前行为:
	///         奔跑倍率主要由InputHandler缓存和使用
	///         PlayerMovementController不直接使用此属性
	///         此方法保留用于未来扩展或日志记录
	///         
	///         未来可能的扩展:
	///         - 同步到UI显示组件
	///         - 记录性能指标变化
	///         - 触发相关成就系统
	///     </remarks>
	/// </summary>
	public void OnSprintMultiplierChanged(float oldValue, float newValue)
	{
		GD.Print($"[PlayerMovementController] ℹ️ 奔跑倍率变化: {oldValue:F2} → {newValue:F2} (当前未使用)");
	}

	#endregion
}
