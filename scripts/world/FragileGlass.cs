using Godot;

namespace GFrameworkGodotTemplate.scripts.world;

/// <summary>
///     易碎玻璃控制器
///     <para>
///         检测玩家与玻璃区域的持续接触，通过倒计时机制实现玻璃破碎效果。
///         玩家接触时开始计时，计时归零后销毁/隐藏玻璃对象。
///     </para>
///
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-16</date>
///     <description>
///         核心职责:
///         1. 接触检测: 通过子节点 Area2D 监测玩家进入/离开碰撞区域
///         2. 状态管理: 维护 inTouch 布尔标志表示当前是否与玩家接触
///         3. 倒计时逻辑: duringTime 仅在 inTouch=true 时按帧递减
///         4. 销毁触发: duringTime 归零时执行隐藏或销毁操作
///
///         场景节点结构要求 (glass.tscn):
///         Glass (Node2D) ← 挂载本脚本
///         ├── StaticBody2D      ← 物理碰撞体（用于支撑玩家站立）
///         │   ├── CollisionShape2D
///         │   ├── Sprite2D
///         │   └── ColorRect
///         └── Area2D            ← 接触检测区域（必需）
///             └── CollisionShape2D
///
///         状态流转:
///         Idle → Touching → CountingDown → Broken(Destroyed/Hidden)
///         (空闲)  (接触中)   (倒计时中)      (已破碎)
///
///         使用方式:
///         1. 将此脚本挂载到 glass.tscn 的根节点(Glass, Node2D类型)
///         2. 确保 Area2D 子节点的 CollisionShape2D 配置正确
///         3. 在 Inspector 中设置 DuringTime 参数（秒）
///         4. 运行后玩家接触玻璃即启动倒计时，归零时玻璃消失
///     </description>
///     <remarks>
///         设计原则:
///         - 单一职责(SRP): 仅负责易碎玻璃的接触检测和破碎逻辑
///         - 数据驱动: 通过 Export 参数支持运行时调整
///         - 安全性: 破碎后自动断开信号连接防止重复触发
///
///         计时精度:
///         使用 _Process 回调确保基于游戏时间(非物理时间)的精确倒计时，
///         与 Godot 的 TimeScale 设置保持一致。
///
///         与 TrapStatic 的区别:
///         - 本脚本为被动响应型(接触即计时)，无主动攻击行为
///         - 本脚本作用于自身(玻璃对象)，TrapStatic 作用于玩家
///         - 本脚本使用倒计时延迟破坏，TrapStatic 为即时效果
///     </remarks>
/// </summary>
[Log]
public partial class FragileGlass : Node2D
{
	#region 导出参数

	/// <summary>接触后的持续时间（秒），归零时触发破碎</summary>
	[Export]
	public float DuringTime { get; set; } = 0.5f;

	#endregion

	#region 私有字段

	/// <summary>当前是否与玩家处于接触状态</summary>
	private bool _inTouch;

	/// <summary>剩余倒计时时间（秒）</summary>
	private float _remainingTime;

	/// <summary>是否已触发破碎（防止重复执行）</summary>
	private bool _isBroken;

	/// <summary>当前在检测区域内的玩家引用</summary>
	private Node? _currentPlayer;

	#endregion

	#region 节点引用

	/// <summary>
	///     接触检测区域
	///     <para>
	///         用于检测玩家进入/离开玻璃的碰撞范围
	///     </para>
	/// </summary>
	private Area2D? _detectionArea;

	/// <summary>
	///     静态碰撞体（用于支撑玩家站立）
	///     <para>
	///         破碎时需要禁用此组件使玩家能够穿过原位置
	///     </para>
	/// </summary>
	private StaticBody2D? _staticBody;

	#endregion

	#region 公开属性（调试用）

	/// <summary>当前是否与玩家接触</summary>
	public bool IsInTouch => _inTouch;

	/// <summary>剩余倒计时时间</summary>
	public float RemainingTime => _remainingTime;

	/// <summary>是否已破碎</summary>
	public bool IsBroken => _isBroken;

	#endregion

	#region 生命周期方法

	/// <summary>
	///     节点准备就绪时的回调
	///     <para>
	///         初始化组件引用、连接信号、设置初始状态
	///     </para>
	/// </summary>
	public override void _Ready()
	{
		try
		{
			InitializeComponents();
			SetupSignalConnections();
			SetInitialState();

			_log.Info("[FragileGlass] ✅ 易碎玻璃控制器初始化完成");
			_log.Info($"[FragileGlass] 持续时间: {DuringTime}s | 节点: {Name}");
		}
		catch (Exception ex)
		{
			_log.Error($"[FragileGlass] ❌ 初始化异常: {ex.Message}");
			_log.Error($"[FragileGlass] 异常类型: {ex.GetType().FullName}");
		}
	}

	/// <summary>
	///     每帧更新回调
	///     <param name="delta">距上一帧的时间间隔（秒）</param>
	///     <remarks>
	///         仅当 inTouch=true 且未破碎时执行倒计时递减，
	///         使用 _Process 而非 _PhysicsProcess 以基于游戏时间计时
	///     </remarks>
	/// </summary>
	public override void _Process(double delta)
	{
		if (_isBroken || !_inTouch)
		{
			return;
		}

		try
		{
			UpdateCountdown((float)delta);
		}
		catch (Exception ex)
		{
			_log.Error($"[FragileGlass] ❌ 更新异常: {ex.Message}");
		}
	}

	#endregion

	#region 初始化方法

	/// <summary>
	///     初始化所有子节点引用
	/// </summary>
	private void InitializeComponents()
	{
		_detectionArea = GetNodeOrNull<Area2D>("Area2D");
		_staticBody = GetNodeOrNull<StaticBody2D>("StaticBody2D");

		if (_detectionArea == null)
		{
			_log.Error("[FragileGlass] ❌ 未找到必需子节点 'Area2D'！");
			_log.Error("[FragileGlass] 请确保 Glass 节点包含名为 'Area2D' 的子节点作为检测区域");
		}

		if (_staticBody == null)
		{
			_log.Warn("[FragileGlass] ⚠️ 未找到 'StaticBody2D' 子节点，破碎时将仅隐藏视觉元素");
		}
	}

	/// <summary>
	///     设置信号连接
	///     <para>
	///         连接 Area2D 的 BodyEntered 和 BodyExited 信号
	///     </para>
	/// </summary>
	private void SetupSignalConnections()
	{
		if (_detectionArea == null)
		{
			return;
		}

		try
		{
			_detectionArea.BodyEntered += OnDetectionAreaBodyEntered;
			_detectionArea.BodyExited += OnDetectionAreaBodyExited;
			_log.Debug("[FragileGlass] ✓ 检测区域信号已连接");
		}
		catch (Exception ex)
		{
			_log.Error($"[FragileGlass] ❌ 信号连接失败: {ex.Message}");
		}
	}

	/// <summary>
	///     设置初始状态
	/// </summary>
	private void SetInitialState()
	{
		_inTouch = false;
		_remainingTime = DuringTime;
		_isBroken = false;
		_currentPlayer = null;
	}

	#endregion

	#region 区域检测事件

	/// <summary>
	///     当物体进入玻璃检测区域时的回调
	///     <param name="body">进入区域的物理实体</param>
	/// </summary>
	private void OnDetectionAreaBodyEntered(Node body)
	{
		if (_isBroken || !IsPlayerBody(body))
		{
			return;
		}

		_inTouch = true;
		_remainingTime = DuringTime;
		_currentPlayer = body;

		_log.Info("════════════ 玩家接触玻璃 ════════════");
		_log.Info($"[FragileGlass] 👤 玩家 {body.Name} 接触玻璃 | 开始倒计时: {DuringTime}s");
	}

	/// <summary>
	///     当物体离开玻璃检测区域时的回调
	///     <param name="body">离开区域的物理实体</param>
	/// </summary>
	private void OnDetectionAreaBodyExited(Node body)
	{
		if (!IsPlayerBody(body) || _currentPlayer != body)
		{
			return;
		}

		_inTouch = false;
		_remainingTime = DuringTime;
		_currentPlayer = null;

		_log.Info("[FragileGlass] 👤 玩家离开玻璃区域 | 倒计时重置");
	}

	/// <summary>
	///     判断传入节点是否为玩家角色
	///     <param name="body">待检测的节点</param>
	/// </summary>
	private static bool IsPlayerBody(Node body)
	{
		var current = body;
		while (current != null)
		{
			if (current.Name.ToString().Contains("player", System.StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			current = current.GetParent();
		}

		return false;
	}

	#endregion

	#region 核心逻辑

	/// <summary>
	///     更新倒计时
	///     <param name="delta">时间间隔（秒）</param>
	///     <remarks>
	///         当 remainingTime 降至 0 或以下时触发破碎操作
	///     </remarks>
	/// </summary>
	private void UpdateCountdown(float delta)
	{
		_remainingTime -= delta;

		if (_remainingTime <= 0f)
		{
			_remainingTime = 0f;
			TriggerBreak();
		}
	}

	/// <summary>
	///     触发玻璃破碎
	///     <para>
	///         执行隐藏操作：隐藏 StaticBody2D（移除碰撞）、隐藏视觉子节点、标记已破碎
	///     </para>
	/// </summary>
	private void TriggerBreak()
	{
		_isBroken = true;
		_inTouch = false;

		try
		{
			DisableCollision();
			HideVisualElements();
			DisconnectSignals();
		}
		catch (Exception ex)
		{
			_log.Warn($"[FragileGlass] ⚠️ 破碎处理部分失败: {ex.Message}");
		}

		_log.Info("════════════ 玻璃已破碎 ════════════");
		_log.Info("[FragileGlass] 💥 倒计时归零，玻璃已被销毁/隐藏");
	}

	/// <summary>
	///     禁用静态碰撞体
	///     <para>
	///         使玩家能够穿过玻璃原来的位置
	///     </para>
	/// </summary>
	private void DisableCollision()
	{
		if (_staticBody != null && GodotObject.IsInstanceValid(_staticBody))
		{
			var collisionShape = _staticBody.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (collisionShape != null)
			{
				collisionShape.Disabled = true;
				_log.Debug("[FragileGlass] ✓ StaticBody2D 碰撞已禁用");
			}

			_staticBody.ProcessMode = ProcessModeEnum.Disabled;
		}
	}

	/// <summary>
	///     隐藏所有视觉子节点
	///     <para>
	///         隐藏 StaticBody2D 和根节点下的 Sprite2D、ColorRect 等视觉元素
	///     </para>
	/// </summary>
	private void HideVisualElements()
	{
		foreach (var child in GetChildren())
		{
			if (child is Sprite2D sprite && GodotObject.IsInstanceValid(sprite))
			{
				sprite.Visible = false;
			}

			if (child is ColorRect colorRect && GodotObject.IsInstanceValid(colorRect))
			{
				colorRect.Visible = false;
			}

			if (child is StaticBody2D staticBody && GodotObject.IsInstanceValid(staticBody))
			{
				staticBody.Visible = false;
			}
		}

		Visible = false;
		_log.Debug("[FragileGlass] ✓ 视觉元素已隐藏");
	}

	/// <summary>
	///     断开信号连接防止重复触发
	/// </summary>
	private void DisconnectSignals()
	{
		if (_detectionArea != null && GodotObject.IsInstanceValid(_detectionArea))
		{
			_detectionArea.BodyEntered -= OnDetectionAreaBodyEntered;
			_detectionArea.BodyExited -= OnDetectionAreaBodyExited;
			_log.Debug("[FragileGlass] ✓ 信号连接已断开");
		}
	}

	#endregion
}
