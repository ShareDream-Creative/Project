using System;
using GFramework.Core.Abstractions.State;
using GFrameworkGodotTemplate.global;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.data;
using GFrameworkGodotTemplate.scripts.data.interfaces;
using GFrameworkGodotTemplate.scripts.data.model;
using GFrameworkGodotTemplate.scripts.level;  // ⭐ v2.5 新增：引用 BaseLevelController
using GFrameworkGodotTemplate.scripts.player.interfaces;
using GFrameworkGodotTemplate.scripts.player.input;
using GFrameworkGodotTemplate.scripts.player.listeners;
using GFrameworkGodotTemplate.scripts.player.physics;
using GFrameworkGodotTemplate.scripts.player.state;
using GFrameworkGodotTemplate.scripts.utility;
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
	///         默认值: 200.0 (已优化，原值为300.0，降低移动速度提升操控性)
	///         用途: 编辑器中快速测试不同速度手感
	///
	///         ✨ 增强功能:
	///         在Godot Inspector中修改此属性时，会自动实时同步到物理模块
	///         无需重启游戏或手动刷新，修改立即生效
	///
	///         🔒 参数验证机制:
	///         - 自动检测 NaN、Infinity 等特殊浮点值
	///         - 自动限制在 [MIN_SPEED, MAX_SPEED] 范围内
	///         - 无效输入自动回退到默认值 (200.0)
	///         - 验证失败时记录警告日志
	///
	///         注意: 正式环境应通过PlayerDataManager配置此值
	///     </remarks>
	/// </summary>
	private float _speedExport = 200.0f;

	[Export]
	public float Speed
	{
		get => _speedExport;
		set
		{
			var validatedValue = ValidateSpeedParameter(value);

			if (Math.Abs(_speedExport - validatedValue) < 0.001f) return;

			var oldValue = _speedExport;
			_speedExport = validatedValue;

			if (_physicsMovement != null)
			{
				_physicsMovement.Speed = validatedValue;
				GD.Print($"[PlayerMovementController] 🎮 Inspector修改速度: {oldValue:F1} → {validatedValue:F1} (已实时同步到物理模块)");
			}
		}
	}

	/// <summary>
	///     验证速度参数的有效性
	///     <param name="value">待验证的速度值</param>
	///     <returns>验证后的有效速度值（无效时返回默认值）</returns>
		private static float ValidateSpeedParameter(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			GD.PrintErr($"[PlayerMovementController] ⚠️ 速度值无效 (NaN/Infinity): {value}, 使用默认值: {200.0f}");
			return 200.0f;
		}

		if (value < PlayerData.MIN_SPEED || value > PlayerData.MAX_SPEED)
		{
			GD.PrintErr($"[PlayerMovementController] ⚠️ 速度值超出范围 [{PlayerData.MIN_SPEED}, {PlayerData.MAX_SPEED}]: {value}, 使用默认值: {200.0f}");
			return 200.0f;
		}

		return value;
	}

	/// <summary>
	///     跳跃力度 (像素/秒) - 编辑器调试用
	///     <para>
	///         负值表示向上跳跃(符合Godot坐标系)
	///         运行时会被PlayerDataManager覆盖
	///     </para>
	///     <remarks>
	///         取值范围: [-MAX_JUMP_VELOCITY_ABS, -MIN_JUMP_VELOCITY_ABS] = [-1000, -200]
	///         默认值: -380.0 (已优化，原值为-500.0，降低跳跃高度提升操控性)
	///         当前配置跳跃高度 ≈ 73.5 像素
	///
	///         ✨ 增强功能:
	///         在Godot Inspector中修改此属性时，会自动实时同步到物理模块
	///         无需重启游戏或手动刷新，修改立即生效
	///
	///         🔒 参数验证机制:
	///         - 自动检测 NaN、Infinity 等特殊浮点值
	///         - 自动检测正值（跳跃速度必须为负数）
	///         - 自动限制在 [-MAX_JUMP_VELOCITY_ABS, -MIN_JUMP_VELOCITY_ABS] 范围内
	///         - 无效输入自动回退到默认值 (-380.0)
	///         - 验证失败时记录警告日志
	///     </remarks>
	/// </summary>
	private float _jumpVelocityExport = -380.0f;

	[Export]
	public float JumpVelocity
	{
		get => _jumpVelocityExport;
		set
		{
			var validatedValue = ValidateJumpVelocityParameter(value);

			if (Math.Abs(_jumpVelocityExport - validatedValue) < 0.001f) return;

			var oldValue = _jumpVelocityExport;
			_jumpVelocityExport = validatedValue;

			if (_physicsMovement != null)
			{
				_physicsMovement.JumpVelocity = validatedValue;
				GD.Print($"[PlayerMovementController] 🎮 Inspector修改跳跃速度: {oldValue:F1} → {validatedValue:F1} (已实时同步到物理模块)");
			}
		}
	}

	/// <summary>
	///     验证跳跃速度参数的有效性
	///     <param name="value">待验证的跳跃速度值</param>
	///     <returns>验证后的有效跳跃速度值（无效时返回默认值）</returns>
	private static float ValidateJumpVelocityParameter(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			GD.PrintErr($"[PlayerMovementController] ⚠️ 跳跃速度值无效 (NaN/Infinity): {value}, 使用默认值: {-380.0f}");
			return -380.0f;
		}

		if (value >= 0)
		{
			GD.PrintErr($"[PlayerMovementController] ⚠️ 跳跃速度必须为负数(向上): {value}, 使用默认值: {-380.0f}");
			return -380.0f;
		}

		var absValue = Math.Abs(value);
		if (absValue < PlayerData.MIN_JUMP_VELOCITY_ABS || absValue > PlayerData.MAX_JUMP_VELOCITY_ABS)
		{
			GD.PrintErr($"[PlayerMovementController] ⚠️ 跳跃速度绝对值超出范围 [{PlayerData.MIN_JUMP_VELOCITY_ABS}, {PlayerData.MAX_JUMP_VELOCITY_ABS}]: {value}, 使用默认值: {-380.0f}");
			return -380.0f;
		}

		return value;
	}

	/// <summary>
	///     重力加速度 (像素/秒²) - 编辑器调试用
	///     <para>
	///         影响角色下落速度和跳跃高度
	///         运行时会被PlayerDataManager覆盖
	///     </para>
	///     <remarks>
	///         取值范围: [PlayerData.MIN_GRAVITY, PlayerData.MAX_GRAVITY] = [100, 3000]
	///         默认值: 1100.0 (已优化，原值为980.0，加快下落速度提升操控性)
	///
	///         ✨ 增强功能:
	///         在Godot Inspector中修改此属性时，会自动实时同步到物理模块
	///         无需重启游戏或手动刷新，修改立即生效
	///
	///         🔒 参数验证机制:
	///         - 自动检测 NaN、Infinity 等特殊浮点值
	///         - 自动检测零值或负数（重力必须为正数）
	///         - 自动限制在 [MIN_GRAVITY, MAX_GRAVITY] 范围内
	///         - 无效输入自动回退到默认值 (1100.0)
	///         - 验证失败时记录警告日志
	///     </remarks>
	/// </summary>
	private float _gravityExport = 1100.0f;

	[Export]
	public float Gravity
	{
		get => _gravityExport;
		set
		{
			var validatedValue = ValidateGravityParameter(value);

			if (Math.Abs(_gravityExport - validatedValue) < 0.001f) return;

			var oldValue = _gravityExport;
			_gravityExport = validatedValue;

			if (_physicsMovement != null)
			{
				_physicsMovement.Gravity = validatedValue;
				GD.Print($"[PlayerMovementController] 🎮 Inspector修改重力: {oldValue:F1} → {validatedValue:F1} (已实时同步到物理模块)");
			}
		}
	}

	/// <summary>
	///     验证重力参数的有效性
	///     <param name="value">待验证的重力值</param>
	///     <returns>验证后的有效重力值（无效时返回默认值）</returns>
	private static float ValidateGravityParameter(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			GD.PrintErr($"[PlayerMovementController] ⚠️ 重力值无效 (NaN/Infinity): {value}, 使用默认值: {1100.0f}");
			return 1100.0f;
		}

		if (value <= 0)
		{
			GD.PrintErr($"[PlayerMovementController] ⚠️ 重力值必须为正数: {value}, 使用默认值: {1100.0f}");
			return 1100.0f;
		}

		if (value < PlayerData.MIN_GRAVITY || value > PlayerData.MAX_GRAVITY)
		{
			GD.PrintErr($"[PlayerMovementController] ⚠️ 重力值超出范围 [{PlayerData.MIN_GRAVITY}, {PlayerData.MAX_GRAVITY}]: {value}, 使用默认值: {1100.0f}");
			return 1100.0f;
		}

		return value;
	}

	#endregion

	#region 梯子攀爬相关变量与配置（增量添加，不修改原有代码）

	/// <summary>
	///     标记玩家是否处于攀爬状态（互斥状态）
	///     <para>
	///         true: 处于攀爬状态，使用攀爬逻辑
	///         false: 正常状态，使用原有移动逻辑
	///     </para>
	/// </summary>
	private bool _isClimbing;

	/// <summary>
	///     当前玩家所在的梯子节点引用
	///     <para>
	///         记录梯子信息，用于获取攀爬速度和边界检测
	///         null: 玩家不在梯子区域
	///     </para>
	/// </summary>
	private Node2D? _currentLadder;

	/// <summary>
	///     攀爬水平跳跃脱离力（像素/秒）
	/// </summary>
	[Export]
	private float _climbHorizontalJumpForce = 300.0f;

	/// <summary>
	///     攀爬垂直跳跃脱离力（像素/秒）
	/// </summary>
	[Export]
	private float _climbVerticalJumpForce = -400.0f;

	/// <summary>
	///     记录上一帧空格键状态，用于检测单次按下
	/// </summary>
	private bool _spacePressedLastFrame = false;

	/// <summary>
	///     跳开梯子后的冷却时间（秒），防止立即重新吸附
	/// </summary>
	private float _climbCooldownTime = 0.0f;

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

		// ═══════════════════════════════════════
		// ⭐ v2.5 关键改进：监听关卡控制器的玩家重置信号
		//    从源头（BaseLevelController）主动通知，比被动检测更可靠
		//    彻底解决玩家在梯子上死亡后状态残留的问题
		// ═══════════════════════════════════════
		SubscribeToLevelControllerResetSignal();
		
		_log.Debug("PlayerMovementController初始化完成 (v2.5 信号驱动重置架构)");
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
	///         ✨ 增量扩展: 添加攀爬状态分支判断
	///     </para>
	///     <param name="delta">
	///     距离上一帧的时间间隔 (秒)
	///     通常为 1/60 ≈ 0.0167秒 (60FPS时)
	///     </param>
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{
		var deltaF = (float)delta;
		
		// 递减攀爬冷却时间
		if (_climbCooldownTime > 0)
		{
			_climbCooldownTime -= deltaF;
		}

		// ═══════════════════════════════════════
		// ⭐ v2.4 关键修复：攀爬状态安全网（每帧检查）
		// 解决：玩家在梯子上突然死亡时，Area2D无法触发 BodyExited，
		// 导致 _isClimbing 状态残留到重生后的问题
		// ═══════════════════════════════════════
		if (_isClimbing)
		{
			PerformClimbingSafetyCheck();
		}
		
		UpdateStateAndInput(deltaF);
		
		if (!_stateController!.IsInputEnabled)
		{
			_physicsMovement!.StopImmediately();
			_physicsMovement.Move(this);
			return;
		}

		// ✨ 增量扩展: 状态分支判断
		// 攀爬状态: 执行新增的攀爬逻辑
		// 正常状态: 完全执行原有移动逻辑（不做任何修改）
		if (_isClimbing)
		{
			HandleClimbingMovement(deltaF);
		}
		else
		{
			// 非攀爬状态: 先检查是否需要进入攀爬
			CheckAndEnterClimbing();
			ProcessMovement(deltaF);
		}
	}

	/// <summary>
	///     ⭐ v2.4 新增：攀爬状态安全网检测（每帧调用）
	///     <para>
	///         解决的核心问题：
	///         当玩家在梯子上突然死亡（触发陷阱、被敌人攻击等）时，
	///         Area2D 的 BodyExited 信号可能无法正常触发，导致：
	///         - _isClimbing 状态残留为 true
	///         - _currentLadder 引用指向已失效的节点
	///         - 重生后玩家仍保持攀爬状态（无法移动/跳跃）
	///         
	///         检测机制：
	///         1. 检查梯子节点是否仍然有效（IsInstanceValid）
	///         2. 检查玩家是否仍在梯子检测范围内
	///         3. 检查玩家自身状态是否正常（未被销毁/禁用）
	///         
	///         触发条件（任一满足即强制退出）：
	///         - _currentLadder 为 null 或已被释放
	///         - 玩家位置超出梯子边界（超过阈值）
	///         - 玩家节点自身失效
	///     </para>
	/// </summary>
	private void PerformClimbingSafetyCheck()
	{
		try
		{
			// ═══════════════════════════════════════
			// 检查1：梯子引用有效性
			// ═══════════════════════════════════════
			if (_currentLadder == null)
			{
				// 梯子引用为空（可能已被其他代码清除但 _isClimbing 未更新）
				GD.Print("[PlayerMovementController] 🛡️ 安全网: _currentLadder 为 null，强制退出攀爬");
				EmergencyExitClimbing("梯子引用丢失");
				return;
			}

			if (!GodotObject.IsInstanceValid(_currentLadder))
			{
				// 梯子节点已被释放（可能场景切换或梯子被销毁）
				GD.Print("[PlayerMovementController] 🛡️ 安全网: 梯子节点已失效(IsInstanceValid=false)，强制退出攀爬");
				_currentLadder = null; // 清除无效引用
				EmergencyExitClimbing("梯子节点失效");
				return;
			}

			// ═══════════════════════════════════════
			// 检查2：玩家自身状态
			// ═══════════════════════════════════════
			if (!GodotObject.IsInstanceValid(this))
			{
				// 玩家自身即将被销毁，无需处理（避免操作无效对象）
				return;
			}

			if (!Visible)
			{
				// 玩家被隐藏（可能是死亡/重生过程中的临时状态）
				// 每60帧输出一次日志（避免刷屏）
				if (Engine.GetProcessFrames() % 60 == 0)
				{
					GD.Print("[PlayerMovementController] 🛡️ 安全网: 玩家处于异常状态（隐藏），准备强制退出攀爬");
				}
				
				// 延迟一帧再检查（给重生逻辑时间完成）
				if (Engine.GetProcessFrames() % 2 == 0)
				{
					EmergencyExitClimbing("玩家状态异常(隐藏)");
				}
				return;
			}

			// ═══════════════════════════════════════
			// 检查3：玩家是否仍在梯子范围内（距离验证）
			// ═══════════════════════════════════════
			try
			{
				Vector2 ladderPosition = _currentLadder.GlobalPosition;
				float distanceToLadder = GlobalPosition.DistanceTo(ladderPosition);

				// 阈值：如果玩家距离梯子超过 200 像素，认为已经脱离
				// （考虑梯子高度通常在 200-300 像素左右）
				const float MAX_VALID_DISTANCE = 200f;

				if (distanceToLadder > MAX_VALID_DISTANCE)
				{
					GD.Print($"[PlayerMovementController] 🛡️ 安全网: 玩家距离梯子过远 ({distanceToLadder:F1}px > {MAX_VALID_DISTANCE}px)，强制退出攀爬");
					EmergencyExitClimbing($"距离过远({distanceToLadder:F0}px)");
					return;
				}
			}
			catch (Exception ex)
			{
				// 获取梯子位置失败（可能梯子正在被销毁）
				GD.Print($"[PlayerMovementController] 🛡️ 安全网: 获取梯子位置异常 ({ex.Message})，强制退出攀爬");
				EmergencyExitClimbing("梯子位置获取异常");
				return;
			}

			// ═══════════════════════════════════════
			// 所有检查通过，攀爬状态有效
			// ═══════════════════════════════════════
			// 每120帧输出一次"安全"日志（约2秒一次，确认系统运行正常）
			if (Engine.GetProcessFrames() % 120 == 0)
			{
				// 静默通过，不输出日志（避免刷屏）
				// 如需调试可取消下面这行的注释：
				// GD.Print("[PlayerMovementController] 🛡️ 安全网: 攀爬状态正常 ✓");
			}
		}
		catch (Exception ex)
		{
			// 安全网本身出现异常（极端情况）
			GD.PrintErr($"[PlayerMovementController] 🛡️ 安全网检测异常: {ex.Message}，强制退出攀爬以防死循环");
			
			// 极端情况下直接重置所有状态
			_isClimbing = false;
			_currentLadder = null;
			_climbCooldownTime = 1.0f; // 设置较长冷却时间
			
			if (_physicsMovement != null)
			{
				_physicsMovement.StopImmediately();
			}
		}
	}

	/// <summary>
	///     ⭐ v2.4 新增：紧急退出攀爬状态（安全网触发时调用）
	///     <para>
	///         与普通 ExitClimbingState() 的区别：
	///         - 不检查当前状态（无条件执行）
	///         - 清除所有引用和状态
	///         - 设置较长的冷却时间
	///         - 输出详细的诊断日志
	///         - 同步物理模块状态
	///     </para>
	/// </summary>
	/// <param name="reason">退出原因（用于日志）</param>
	private void EmergencyExitClimbing(string reason)
	{
		GD.Print($"[PlayerMovementController] 🚨🚨🚨 [安全网触发] 紧急退出攀爬状态！");
		GD.Print($"   触发原因: {reason}");
		GD.Print($"   当前状态: _isClimbing={_isClimbing}, _currentLadder={(_currentLadder != null ? "非null" : "null")}");

		// 1. 无条件退出攀爬状态
		if (_isClimbing)
		{
			try
			{
				ExitClimbingState();
			}
			catch (Exception ex)
			{
				GD.Print($"[PlayerMovementController]   ⚠️ ExitClimbingState() 异常: {ex.Message}（继续执行清理）");
			}
		}

		// 2. 强制清除所有引用（双重保险 + 三重保险）
		_isClimbing = false;
		
		if (_currentLadder != null && GodotObject.IsInstanceValid(_currentLadder))
			{
				// 尝试通知梯子控制器（如果梯子还有效）
				try
				{
					// 使用接口方法而非具体类型（避免依赖特定类）
					if (_currentLadder.HasMethod("OnEndClimbing"))
					{
						_currentLadder.Call("OnEndClimbing");
					}
				}
				catch (Exception ex)
				{
					// 忽略通知失败
				}
			}
		
		_currentLadder = null; // 最终清除

		// 3. 设置较长的冷却时间（防止立即重新进入）
		_climbCooldownTime = 0.8f; // 比 normal 的 0.3s-0.5s 更长

		// 4. 完全同步物理模块状态
		if (_physicsMovement != null)
		{
			_physicsMovement.StopImmediately();
		}

		// 5. 重置速度为安全值（防止高速飞出）
		Velocity = Vector2.Zero;

		GD.Print($"[PlayerMovementController] ✅✅✅ 紧急退出完成 | 冷却时间: {_climbCooldownTime:F1}s | 所有状态已重置");
	}

	/// <summary>
	///     检测并进入攀爬状态（在正常移动状态下调用）
	///     <para>
	///         当玩家在梯子区域内且按下A/D键时，进入攀爬状态
	///         不影响原有移动逻辑
	///     </para>
	/// </summary>
	/// <summary>
	///     检查是否应该进入攀爬状态
	///     <para>
	///         v2.2 增强：
	///         - 增加更严格的条件检查
	///         - 添加详细日志便于调试
	///         - 确保冷却期内绝对不会进入攀爬状态
	///     </para>
	/// </summary>
	private void CheckAndEnterClimbing()
	{
		// ═══════════════════════════════════════
		// 条件检查（任一不满足即跳过）
		// ═══════════════════════════════════════
		
		if (_isClimbing)
		{
			// 已在攀爬中，无需重复进入（正常情况）
			return;
		}

		if (_currentLadder == null)
		{
			// 无梯子引用，无法进入（可能已离开检测区域）
			return;
		}

		if (_climbCooldownTime > 0)
		{
			// ⭐ 冷却期间：禁止进入攀爬状态（防吸附关键！）
			// 每60帧输出一次日志（避免刷屏）
			if (Engine.GetProcessFrames() % 60 == 0)
			{
				GD.Print($"[PlayerMovementController] ⏳ 攀爬冷却中: {_climbCooldownTime:F2}s");
			}
			return;
		}

		// ═══════════════════════════════════════
		// 检测输入：A/D键或方向键
		// ═══════════════════════════════════════
		bool shouldEnterClimbing = 
			Input.IsKeyPressed(Key.A) || 
			Input.IsKeyPressed(Key.Left) ||
			Input.IsKeyPressed(Key.D) || 
			Input.IsKeyPressed(Key.Right);

		if (shouldEnterClimbing)
		{
			GD.Print("[PlayerMovementController] 🪜 检测到攀爬输入，正在进入攀爬状态...");
			EnterClimbingState();
		}
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
		_globalInputService = NodeTreeHelper.GetGlobalInputService(this);
		_dataManager = PlayerDataManager.Instance;
		
		if (_globalInputService != null)
		{
			_log.Debug("成功获取全局游戏玩法输入服务 (通过NodeTreeHelper)");
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

	#endregion

	#region 私有方法 - 信号订阅（v2.5 新增）

	/// <summary>
	///     ⭐ v2.5 新增：订阅关卡控制器的玩家重置信号
	///     <para>
	///         通过连接 BaseLevelController.OnPlayerResetRequested 信号，
	///         实现从源头主动通知的完整状态重置机制。
	///         
	///         设计原理：
	///         - 当玩家触发陷阱或死亡时，BaseLevelController 会发射此信号
	///         - PlayerMovementController 监听此信号并立即执行完整的状态清理
	///         - 包括：攀爬状态、物理速度、所有临时状态
	///         
	///         优势：
	///         - 比被动检测（安全网）更可靠，从源头通知
	///         - 不依赖 Area2D.BodyExited 的时序
	///         - 确保在重置流程的最早期执行状态清理
	 ///     </para>
	/// </summary>
	private void SubscribeToLevelControllerResetSignal()
	{
		try
		{
			// 方法1：通过场景树查找 BaseLevelController
			var sceneTree = GetTree();
			if (sceneTree == null)
			{
				GD.Print("[PlayerMovementController] ⚠️ 无法获取场景树，延迟尝试订阅重置信号");
				// 延迟到下一帧再尝试
				GetTree().CreateTimer(0.1).Timeout += () => SubscribeToLevelControllerResetSignal();
				return;
			}

			var currentScene = sceneTree.CurrentScene;
			if (currentScene == null)
			{
				GD.Print("[PlayerMovementController] ⚠️ 当前场景为空，延迟尝试订阅重置信号");
				GetTree().CreateTimer(0.1).Timeout += () => SubscribeToLevelControllerResetSignal();
				return;
			}

			// 查找 BaseLevelController
			var levelControllers = currentScene.FindChildren("*", "BaseLevelController", true, false);
			
			if (levelControllers.Count > 0 && levelControllers[0] is BaseLevelController controller)
			{
				// 连接信号
				controller.PlayerResetRequested += OnLevelControllerResetRequested;
				
				GD.Print("[PlayerMovementController] ✅✅✅ 已成功订阅关卡控制器重置信号！");
				GD.Print($"   信号: OnPlayerResetRequested");
				GD.Print($"   来源: {controller.Name} ({controller.GetType().Name})");
			}
			else
			{
				// 方法2：如果当前场景找不到，监听全局信号（备用方案）
				// 通过延迟等待场景完全加载后再尝试
				GD.Print("[PlayerMovementController] ℹ️ 当前未找到 BaseLevelController，将在场景加载完成后自动重试...");
				
				// 使用 Timer 延迟重试（给场景初始化时间）
				GetTree().CreateTimer(0.5).Timeout += () => 
				{
					// 重试一次
					var retryControllers = GetTree()?.CurrentScene?.FindChildren("*", "BaseLevelController", true, false);
					if (retryControllers?.Count > 0 && retryControllers[0] is BaseLevelController retryController)
					{
						retryController.PlayerResetRequested += OnLevelControllerResetRequested;
						GD.Print("[PlayerMovementController] ✅ [延迟订阅成功] 已连接到 BaseLevelController");
					}
					else
					{
						GD.Print("[PlayerMovementController] ⚠️ [延迟订阅失败] 仍未找到 BaseLevelController（将依赖被动检测作为后备）");
					}
				};
			}
		}
		catch (Exception ex)
		{
			GD.Print($"[PlayerMovementController] ⚠️ 订阅重置信号异常: {ex.Message}");
			// 订阅失败不应阻止初始化
		}
	}

	/// <summary>
	///     ⭐ v2.5 新增：处理来自关卡控制器的重置请求信号
	///     <para>
	///         这是**最可靠**的状态重置入口点！
	///         由 BaseLevelController 在 ExecuteFullPlayerReset() 中主动发射，
	///         确保在玩家被重置到初始位置的最早时机执行状态清理。
	 ///     </para>
	///     
	///     <param name="playerNode">被重置的玩家节点</param>
	private void OnLevelControllerResetRequested(Node playerNode)
	{
		try
		{
			GD.Print($"[PlayerMovementController] 📨📨📨 [信号驱动] 收到关卡控制器重置请求！");
			GD.Print($"   发送者: BaseLevelController");
			GD.Print($"   时间戳: {Time.GetTicksMsec()}ms");

			// ═══════════════════════════════════════
			// 执行完整的紧急状态重置
			// （与 EmergencyExitClimbing 类似但更彻底）
			// ═══════════════════════════════════════
			
			// 1. 强制退出攀爬状态（无条件）
			if (_isClimbing)
			{
				GD.Print("[PlayerMovementController]   检测到攀爬状态，正在强制退出...");
				try { ExitClimbingState(); } catch { /* 忽略 */ }
			}

			// 2. 清除所有攀爬相关引用（三重保险）
			bool hadClimbingState = _isClimbing || _currentLadder != null;
			
			_isClimbing = false;
			
			if (_currentLadder != null)
			{
				// 尝试通知梯子控制器
				if (_currentLadder.HasMethod("OnEndClimbing"))
				{
					try { _currentLadder.Call("OnEndClimbing"); } catch { /* 忽略 */ }
				}
				_currentLadder = null;
			}

			// 3. 设置较长的冷却时间防止立即重新进入
			_climbCooldownTime = 1.0f; // 1秒冷却（比正常情况更长）

			// 4. 完全同步物理模块状态
			if (_physicsMovement != null)
			{
				_physicsMovement.StopImmediately();
			}

			// 5. 重置速度为安全值
			Velocity = Vector2.Zero;

			// 6. 重置输入状态
			_spacePressedLastFrame = false;

			if (hadClimbingState)
			{
				GD.Print("[PlayerMovementController] ✅✅✅ [信号响应] 攀爬状态已完全清除！");
				GD.Print($"   _isClimbing: {_isClimbing}");
				GD.Print($"   _currentLadder: {_currentLadder}");
				GD.Print($"   冷却时间: {_climbCooldownTime:F1}s");
			}
			else
			{
				GD.Print("[PlayerMovementController] ✓ [信号响应] 状态确认正常（无攀爬残留）");
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[PlayerMovementController] ❌ 处理重置信号异常: {ex.Message}");
			
			// 极端情况下强制重置
			_isClimbing = false;
			_currentLadder = null;
			_climbCooldownTime = 1.0f;
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
	 ///         
	 ///         v2.2增强:
	 ///         - 将全局输入服务注入到 PlayerStateController
	 ///         - 实现基于 LevelPhase 的双重输入控制机制
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
		
		// v2.2新增: 注入全局输入服务到状态控制器
		// 实现 Success/Defeat/Build 阶段的输入阻断机制
		_stateController.SetGlobalInputService(_globalInputService);
		_log.Debug("已注入全局输入服务到PlayerStateController (支持LevelPhase阻断)");
		
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

	#region 梯子攀爬相关方法（增量添加，不修改原有代码）

	/// <summary>
	///     当玩家进入梯子碰撞区域时调用（自动检测版本）
	///     <para>
	///         仅标记当前梯子引用，不会自动进入攀爬状态
	///         不影响原有移动功能
	///     </para>
	///     <param name="ladder">梯子节点</param>
	/// </summary>
	public void OnPlayerEnteredLadder(Node2D ladder)
	{
		_currentLadder = ladder;
		GD.Print($"[PlayerMovementController] 🪜 玩家进入梯子区域");
	}

	/// <summary>
	///     当玩家离开梯子碰撞区域时调用（自动检测版本）
	///     <para>
	///         强制退出攀爬状态（无论当前是否在攀爬）
	///         恢复正常移动状态
	///         清除所有梯子相关引用
	///         
	///         v2.2 修复：
	///         确保玩家脱离检测区域后立即恢复正常状态，
	///         解决即使离开检测区域仍保持攀爬状态的问题
	///     </para>
	/// </summary>
	public void OnPlayerExitedLadder()
	{
		GD.Print($"[PlayerMovementController] 👤 玩家离开梯子区域 - 强制恢复正常状态");

		// 强制退出攀爬状态（无论 _isClimbing 当前值如何）
		if (_isClimbing)
		{
			ExitClimbingState();
		}

		// ⭐ 关键修复：无条件清除梯子引用，防止状态残留
		_currentLadder = null;
		_isClimbing = false; // 双重保险

		// 重置冷却时间（防止立即重新进入）
		_climbCooldownTime = 0.3f;

		// 同步物理模块状态
		if (_physicsMovement != null)
		{
			_physicsMovement.StopImmediately();
		}

		GD.Print($"[PlayerMovementController] ✓ 梯子状态已完全重置");
	}

	/// <summary>
	///     进入攀爬状态的处理
	///     <para>
	///         重置速度，关闭重力，标记攀爬状态
	///     </para>
	/// </summary>
	private void EnterClimbingState()
	{
		if (_isClimbing || _currentLadder == null) return;

		_isClimbing = true;
		Velocity = Vector2.Zero;

		// 通知梯子玩家开始攀爬（防止进入/离开循环）
		if (_currentLadder.HasMethod("OnStartClimbing"))
		{
			_currentLadder.Call("OnStartClimbing");
		}

		GD.Print($"[PlayerMovementController] 🪜 进入攀爬状态");
	}

	/// <summary>
	///     退出攀爬状态的处理
	///     <para>
	///         恢复正常状态，同步物理模块（关键！）
	///         确保下一帧 ProcessMovement() 使用正确的速度
	///     </para>
	/// </summary>
	private void ExitClimbingState()
	{
		if (!_isClimbing) return;

		_isClimbing = false;

		// 关键：同步物理模块的速度状态！
		// 攀爬时我们直接操作了 CharacterBody2D.Velocity，
		// 现在需要确保 _physicsMovement._velocity 也被重置，
		// 否则下一帧 ProcessMovement() 会用旧值覆盖正确的速度
		if (_physicsMovement != null)
		{
			_physicsMovement.StopImmediately();
			GD.Print("[PlayerMovementController] ✓ 退出攀爬时已同步物理模块");
		}

		// 通知梯子玩家结束攀爬
		if (_currentLadder != null && _currentLadder.HasMethod("OnEndClimbing"))
		{
			_currentLadder.Call("OnEndClimbing");
		}

		GD.Print($"[PlayerMovementController] 🪜 退出攀爬状态");
	}

	/// <summary>
	///     从梯子上跳跃脱离
	///     <para>
	///         v2.2 修复：
	///         - A键+空格：向左上方跳跃（水平速度向左，垂直速度向上）
	///         - D键+空格：向右上方跳跃（水平速度向右，垂直速度向上）
	///         - 仅空格：垂直向上跳跃
	///         
	///         增强防吸附机制：
	///         - 跳跃后设置较长的冷却时间（0.5秒）
	///         - 强制清除梯子引用，防止下一帧重新进入攀爬状态
	///         - 确保即使仍在检测区域内也不会被吸附回梯子
	///     </para>
	/// </summary>
	private void JumpOffLadder()
	{
		if (!_isClimbing || _currentLadder == null) return;

		GD.Print("[PlayerMovementController] 🚀 玩家从梯子跳跃脱离！");

		// 获取当前输入方向（用于斜向跳跃）
		float horizontalInput = 0;

		// 优先检测跳离时的方向键（不是攀爬方向）
		if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) 
		{
			horizontalInput = -1; // 向左
		}
		if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) 
		{
			horizontalInput = 1; // 向右
		}

		// 计算跳跃速度向量
		Vector2 jumpVelocity = Vector2.Zero;
		
		// ⭐ v2.2 修复：根据按键组合决定跳跃方向
		if (horizontalInput < -0.1f)
		{
			// A键按下 → 向左上方跳跃
			jumpVelocity = new Vector2(
				-_climbHorizontalJumpForce, // 水平向左（负值）
				_climbVerticalJumpForce      // 垂直向上（负值=向上）
			);
			GD.Print($"[PlayerMovementController]   ↖️ 向左上跳跃 (A+空格)");
		}
		else if (horizontalInput > 0.1f)
		{
			// D键按下 → 向右上方跳跃
			jumpVelocity = new Vector2(
				_climbHorizontalJumpForce,  // 水平向右（正值）
				_climbVerticalJumpForce      // 垂直向上（负值=向上）
			);
			GD.Print($"[PlayerMovementController]   ↗️ 向右上跳跃 (D+空格)");
		}
		else
		{
			// 仅空格键 → 垂直向上跳跃
			jumpVelocity = new Vector2(
				0,                       // 无水平移动
				_climbVerticalJumpForce   // 垂直向上
			);
			GD.Print($"[PlayerMovementController]   ⬆️ 垂直向上跳跃（仅空格）");
		}

		// ═══════════════════════════════════════
		// 应用跳跃速度
		// ═══════════════════════════════════════
		Velocity = jumpVelocity;

		// ═══════════════════════════════════════
		// ⭐ 关键修复：增强防吸附机制
		// ═══════════════════════════════════════
		
		// 1. 通知梯子控制器玩家已离开
		if (_currentLadder != null && _currentLadder.HasMethod("OnEndClimbing"))
		{
			_currentLadder.Call("OnEndClimbing");
		}

		// 2. 完全退出攀爬状态
		_isClimbing = false;
		_currentLadder = null; // ⭐ 关键：立即清除引用！

		// 3. 设置较长的冷却时间（防止立即重新进入）
		//    从原来的 0.25s 增加到 0.5s
		_climbCooldownTime = 0.5f;

		// 4. 同步物理模块
		if (_physicsMovement != null)
		{
			_physicsMovement.StopImmediately();
		}

		GD.Print($"[PlayerMovementController] ✓ 跳跃完成 | 速度: ({jumpVelocity.X:F1}, {jumpVelocity.Y:F1})");
		GD.Print($"[PlayerMovementController] ✓ 防吸附冷却: 0.5s | 梯子引用已清除");
	}

	/// <summary>
	///     从陷阱中完全恢复玩家（重置所有相关状态）
	///     <para>
	///         当玩家触发陷阱后需要重置时调用
	///         确保：
	///         1. 退出攀爬状态（如果在攀爬）- 强制退出，无条件
	///         2. 重置所有攀爬相关变量（_isClimbing, _currentLadder, _climbCooldownTime）
	///         3. 重置梯子状态管理器（LadderClimbState）- 如果存在
	///         4. 启用碰撞体（关键：解决穿模和陷阱检测问题）
	///         5. 重置物理状态（关键：同步 _physicsMovement._velocity）
	///         6. 确保玩家可见
	///         
	///         v2.2 修复：
	///         解决玩家在爬梯子时被攻击后重生仍保持爬梯状态的错误
	///     </para>
	/// </summary>
	public void ResetFromTrap()
	{
		GD.Print("[PlayerMovementController] 🔄 正在从陷阱中完全恢复...");

		// ═══════════════════════════════════════
		// ⭐ v2.4 增强：优先使用紧急退出机制
		//    确保即使 Area2D 未触发 BodyExited，
		//    也能彻底清理攀爬状态（解决死亡残留问题）
		// ═══════════════════════════════════════
		if (_isClimbing || _currentLadder != null)
		{
			GD.Print("[PlayerMovementController]   🛡️ 检测到攀爬状态残留，调用紧急退出...");
			EmergencyExitClimbing("ResetFromTrap(陷阱重生)");
		}
		
		// ⭐ 关键：无条件重置所有攀爬相关变量（三重保险）
		_isClimbing = false;
		_currentLadder = null;
		_climbCooldownTime = 0f;

		GD.Print("[PlayerMovementController]   ✓ 所有攀爬变量已重置（三重保险）");

		// ═══════════════════════════════════════
		// 2. 同步物理模块状态（防止速度冲突）
		// ═══════════════════════════════════════
		if (_physicsMovement != null)
		{
			_physicsMovement.StopImmediately();
			GD.Print("[PlayerMovementController]   ✓ 物理模块已同步");
		}

		// ═══════════════════════════════════════
		// 3. 确保重力恢复正常
		// ═══════════════════════════════════════
		// 攀爬时可能修改了重力相关属性，这里确保恢复默认值
		// 注意：CharacterBody2D 的重力由引擎自动处理，
		// 但如果之前有自定义设置需要在这里重置

		// ═══════════════════════════════════════
		// 4. 确保自己可见
		// ═══════════════════════════════════════
		Visible = true;

		// ═══════════════════════════════════════
		// 5. 关键：完全重置物理模块的速度和状态！
		//    这会同步 _physicsMovement._velocity 和 CharacterBody2D.Velocity
		// ═══════════════════════════════════════
		if (_physicsMovement != null)
		{
			_physicsMovement.StopImmediately();
			GD.Print("[PlayerMovementController]   ✓ 物理模块已完全重置（速度+地面状态）");
		}

		// ═══════════════════════════════════════
		// 6. 关键：确保碰撞体启用（使用 Timer 延迟避免 "flushing queries" 错误）
		// ═══════════════════════════════════════
		var collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collisionShape != null)
		{
			var tree = GetTree();
			if (tree != null)
			{
				tree.CreateTimer(0).Timeout += () => 
				{
					if (GodotObject.IsInstanceValid(collisionShape))
					{
						collisionShape.Disabled = false;
					}
				};
			}
			else
			{
				collisionShape.Disabled = false;
			}
		}

		// ═══════════════════════════════════════
		// 7. 重置输入状态
		// ═══════════════════════════════════════
		_spacePressedLastFrame = false;

		GD.Print("[PlayerMovementController] ✓✓✓ 从陷阱中恢复完成！（攀爬状态已完全重置 + 双重速度系统已同步 + 碰撞体将延迟启用）");
	}

	/// <summary>
	///     处理攀爬移动（v2.3 - 增强边界检测版）
	///     <para>
	///         核心功能：
	///         1. 响应 A/D 键输入进行垂直移动（D=上, A=下）
	///         2. 空格键跳离梯子
	///         3. ⭐ v2.3 新增：严格的梯子边界检测
	///         
	///         v2.3 边界检测机制：
	///         - 实时获取梯子的全局边界矩形 (Rect2)
	///         - 每帧检查玩家位置是否超出梯子顶部/底部
	///         - 超出边界时自动脱离攀爬状态并恢复重力
	///         - 防止玩家爬到梯子外部（如截图中的问题）
	///     </para>
	/// </summary>
	private void HandleClimbingMovement(float delta)
	{
		if (!_isClimbing || _currentLadder == null) return;

		// ═══════════════════════════════════════
		// ⭐ v2.3 关键修复：边界检测（每帧执行）
		// ═══════════════════════════════════════
		if (CheckLadderBoundary())
		{
			// 已超出边界，方法会自动处理脱离
			return;
		}

		// 重置水平速度，不应用重力
		var velocity = Velocity;
		velocity.X = 0;

		// 攀爬输入映射: D=上, A=下（使用与现有系统一致的方式）
		float climbInput = 0;
		if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) climbInput = -1;  // D/右 = 向上（负Y）
		if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) climbInput = 1; // A/左 = 向下（正Y）

		// 从梯子获取攀爬速度
		var climbSpeed = 150.0f; // 默认值

		// 尝试从梯子节点获取攀爬速度（通过接口）
		if (_currentLadder is GFrameworkGodotTemplate.scripts.world.interfaces.ILadderClimbable climbable)
		{
			climbSpeed = climbable.ClimbSpeed;
		}

		// ⭐ v2.3 增强：预计算下一帧位置，防止单帧穿透边界
		float nextYPosition = GlobalPosition.Y + (climbInput * climbSpeed * (float)delta);

		// 获取梯子边界（用于限制移动）
		Rect2 ladderBounds = GetLadderBounds();

		// ═══════════════════════════════════════
		// 边界限制：防止移动超出梯子范围
		// ═══════════════════════════════════════
		bool shouldStopClimbing = false;

		// 检查顶部边界（试图向上移动但已到达顶部）
		if (climbInput < 0 && nextYPosition < ladderBounds.Position.Y)
		{
			// 到达顶部，停止向上移动
			nextYPosition = ladderBounds.Position.Y;
			velocity.Y = 0;
			
			GD.Print("[PlayerMovementController] ⬆️ 到达梯子顶部边界");
			shouldStopClimbing = true;
		}
		
		// 检查底部边界（试图向下移动但已到达底部）
		if (climbInput > 0 && nextYPosition > ladderBounds.End.Y)
		{
			// 到达底部，停止向下移动
			nextYPosition = ladderBounds.End.Y;
			velocity.Y = 0;
			
			GD.Print("[PlayerMovementController] ⬇️ 到达梯子底部边界");
			shouldStopClimbing = true;
		}

		// 应用速度
		velocity.Y = climbInput * climbSpeed;

		// 空格键跳离（检测单次按下）
		bool spacePressedThisFrame = Input.IsKeyPressed(Key.Space);
		if (spacePressedThisFrame && !_spacePressedLastFrame)
		{
			JumpOffLadder();
			_spacePressedLastFrame = spacePressedThisFrame;
			return;
		}
		_spacePressedLastFrame = spacePressedThisFrame;

		Velocity = velocity;
		MoveAndSlide();

		// 移动后再次检查边界（双重保险）
		if (shouldStopClimbing || CheckLadderBoundary())
		{
			// 已经处理过或超出边界
		}
	}

	/// <summary>
	///     ⭐ v2.3 新增：检查玩家是否超出梯子边界
	///     <para>
	///         检测条件：
	///         1. 玩家位置超出梯子顶部（Y < ladderTop）
	///         2. 玩家位置超出梯子底部（Y > ladderBottom）
	///         
	///         触发动作：
	///         - 自动调用 ExitClimbingState()
	///         - 清除 _currentLadder 引用
	///         - 设置冷却时间防止立即重入
	///         - 恢复正常重力
	///     </para>
	/// </summary>
	/// <returns>true 如果已超出边界并处理完毕</returns>
	private bool CheckLadderBoundary()
	{
		try
		{
			if (_currentLadder == null) return false;

			// 获取梯子边界
			Rect2 bounds = GetLadderBounds();

			// 获取玩家当前位置
			Vector2 playerPos = GlobalPosition;

			// 检查是否超出顶部边界
			if (playerPos.Y < bounds.Position.Y)
			{
				GD.Print($"[PlayerMovementController] 🚨 超出梯子顶部边界！");
				GD.Print($"   玩家Y: {playerPos.Y:F1} | 梯子顶部Y: {bounds.Position.Y:F1}");
				GD.Print($"   差值: {(playerPos.Y - bounds.Position.Y):F1}px");

				ForceExitClimbing("顶部");
				return true;
			}

			// 检查是否超出底部边界
			if (playerPos.Y > bounds.End.Y)
			{
				GD.Print($"[PlayerMovementController] 🚨 超出梯子底部边界！");
				GD.Print($"   玩家Y: {playerPos.Y:F1} | 梯子底部Y: {bounds.End.Y:F1}");
				GD.Print($"   差值: {(playerPos.Y - bounds.End.Y):F1}px");

				ForceExitClimbing("底部");
				return true;
			}

			// 在边界内，正常
			return false;
		}
		catch (Exception ex)
		{
			GD.Print($"[PlayerMovementController] ⚠️ 边界检测异常: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	///     ⭐ v2.3 新增：强制脱离攀爬状态（边界溢出时调用）
	/// </summary>
	/// <param name="reason">脱离原因（"顶部"/"底部"）</param>
	private void ForceExitClimbing(string reason)
	{
		GD.Print($"[PlayerMovementController] 🔓 强制脱离攀爬（{reason}超出）");

		// 退出攀爬状态
		if (_isClimbing)
		{
			ExitClimbingState();
		}

		// 清除引用
		_currentLadder = null;
		_isClimbing = false;

		// 设置冷却时间防止立即重入
		_climbCooldownTime = 0.5f;

		// 同步物理模块
		if (_physicsMovement != null)
		{
			_physicsMovement.StopImmediately();
		}

		GD.Print($"[PlayerMovementController] ✓ 已强制脱离 | 冷却: 0.5s");
	}

	/// <summary>
	///     ⭐ v2.3 新增：获取当前梯子的全局边界
	/// </summary>
	/// <returns>梯子的 Rect2 边界矩形</returns>
	private Rect2 GetLadderBounds()
	{
		// 默认边界（如果无法获取）
		Rect2 defaultBounds = new Rect2(
			GlobalPosition - new Vector2(10, 147),  // 中心点偏移
			new Vector2(20, 294)                     // 默认尺寸
		);

		try
		{
			if (_currentLadder is GFrameworkGodotTemplate.scripts.world.interfaces.ILadderClimbable climbable)
			{
				return climbable.GetGlobalBounds();
			}
		}
		catch (Exception ex)
		{
			GD.Print($"[PlayerMovementController] ⚠️ 获取梯子边界失败: {ex.Message}");
		}

		return defaultBounds;
	}

	#endregion
}
