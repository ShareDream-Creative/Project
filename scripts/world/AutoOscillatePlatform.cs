using Godot;

namespace GFrameworkGodotTemplate.scripts.world;

/// <summary>
///     通用自动折返运动控制器
///     <para>
///         控制子节点 PlatformMove 在 A 和 B 两个位置标记节点之间自动往返运动
///         无需玩家交互，启动后即开始自动循环运动
///     </para>
///
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-16</date>
///     <description>
///         核心职责:
///         1. 自动往返运动: 在 A 和 B 节点之间持续往返移动
///         2. 可配置参数: 运动速度(speed)、边界停留时间(waitTime)
///         3. 平滑运动: 使用线性插值实现平滑的启停效果
///         4. 通用组件: 仅控制自身的直接子节点，不影响场景中其他节点
///
///         场景节点结构要求:
///         ParentNode (Node2D) ← 挂载本脚本
///         ├── PlatformMove (Node2D) ← 需要移动的平台对象（必需）
///         ├── A (Node2D) ← 位置A标记点（必需）
///         └── B (Node2D) ← 位置B标记点（必需）
///
///         使用方式:
///         1. 将此脚本挂载到包含上述子节点的 Node2D 父节点
///         2. 在 Inspector 中调整 speed 和 waitTime 参数
///         3. 运行后平台自动开始在 A 和 B 之间往返运动
///
///         与 MovePlatformController 的区别:
///         - 本脚本为全自动模式，无需玩家按键触发
///         - 本脚本通过直接子节点名称查找，无需 unique_name_in_owner
///         - 本脚本更轻量，专注于纯自动往返逻辑
///     </description>
///     <remarks>
///         设计原则:
///         - 单一职责(SRP): 专注于自动折返运动控制
///         - 开闭原则(OCP): 通过 Export 参数支持运行时配置
///         - 通用性: 仅依赖子节点名称约定，不绑定特定场景
///
///         状态机:
///         MovingToB → WaitingAtB → MovingToA → WaitingAtA → ...
///         (向B移动)    (B点等待)    (向A移动)    (A点等待)
///
///         坐标系统:
///         - 所有位置坐标相对于父节点（挂载脚本的节点）的局部坐标系
///         - A 和 B 节点的 Position 即为运动的起点和终点
///     </remarks>
/// </summary>
[Log]
public partial class AutoOscillatePlatform : Node2D
{
	#region 导出参数

	/// <summary>平台运动速度（像素/秒）</summary>
	[Export]
	public float Speed { get; set; } = 100f;

	/// <summary>到达端点后的停留时间（秒）</summary>
	[Export]
	public float WaitTime { get; set; } = 0.5f;

	#endregion

	#region 私有字段

	/// <summary>当前运动方向（1=向B，-1=向A）</summary>
	private int _moveDirection = 1;

	/// <summary>是否正在停留等待中</summary>
	private bool _isWaiting;

	/// <summary>等待计时器累计时间</summary>
	private float _waitTimer;

	/// <summary>当前运动进度 [0.0 = A点, 1.0 = B点]</summary>
	private float _progress;

	/// <summary>组件是否已成功初始化</summary>
	private bool _isInitialized;

	#endregion

	#region 节点引用

	/// <summary>
	///     需要移动的平台对象
	///     <para>
	///         必须是当前节点的直接子节点，名称为 "PlatformMove"
	///     </para>
	/// </summary>
	private Node2D? _platformMove;

	/// <summary>
	///     位置A标记点（运动起点）
	///     <para>
	///         必须是当前节点的直接子节点，名称为 "A"
	///     </para>
	/// </summary>
	private Node2D? _pointA;

	/// <summary>
	///     位置B标记点（运动终点）
	///     <para>
	///         必须是当前节点的直接子节点，名称为 "B"
	///     </para>
	/// </summary>
	private Node2D? _pointB;

	#endregion

	#region 生命周期方法

	/// <summary>
	///     节点准备就绪时的回调
	///     <para>
	///         初始化所有组件引用、验证节点存在性、设置初始状态
	///     </para>
	/// </summary>
	public override void _Ready()
	{
		try
		{
			InitializeComponents();
			SetInitialState();

			if (_isInitialized)
			{
				_log.Info("[AutoOscillatePlatform] ✅ 自动折返运动控制器初始化完成");
				_log.Info($"[AutoOscillatePlatform] 运动速度: {Speed} px/s | 边界停留: {WaitTime}s");
				_log.Debug($"[AutoOscillatePlatform] A点位置: {_pointA?.Position} | B点位置: {_pointB?.Position}");
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[AutoOscillatePlatform] ❌ 初始化异常: {ex.Message}");
			_log.Error($"[AutoOscillatePlatform] 异常类型: {ex.GetType().FullName}");
		}
	}

	/// <summary>
	///     每帧更新回调
	///     <param name="delta">距上一帧的时间间隔（秒）</param>
	/// </summary>
	public override void _Process(double delta)
	{
		if (!_isInitialized)
		{
			return;
		}

		try
		{
			if (_isWaiting)
			{
				UpdateWaitTimer((float)delta);
			}
			else
			{
				UpdateMovement((float)delta);
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[AutoOscillatePlatform] ❌ 更新异常: {ex.Message}");
		}
	}

	#endregion

	#region 初始化方法

	/// <summary>
	///     初始化所有子节点引用
	///     <para>
	///         通过名称查找三个必需的直接子节点:
	///         PlatformMove（移动对象）、A（起点）、B（终点）
	///         缺少任一节点将输出警告并禁用运动功能
	///     </para>
	/// </summary>
	private void InitializeComponents()
	{
		_platformMove = GetNodeOrNull<Node2D>("PlatformMove");
		_pointA = GetNodeOrNull<Node2D>("A");
		_pointB = GetNodeOrNull<Node2D>("B");

		bool allValid = true;

		if (_platformMove == null)
		{
			_log.Error("[AutoOscillatePlatform] ❌ 未找到必需子节点 'PlatformMove'！");
			_log.Error("[AutoOscillatePlatform] 请确保当前节点包含名为 'PlatformMove' 的 Node2D 子节点");
			allValid = false;
		}

		if (_pointA == null)
		{
			_log.Error("[AutoOscillatePlatform] ❌ 未找到必需子节点 'A'！");
			_log.Error("[AutoOscillatePlatform] 请确保当前节点包含名为 'A' 的 Node2D 子节点作为位置标记");
			allValid = false;
		}

		if (_pointB == null)
		{
			_log.Error("[AutoOscillatePlatform] ❌ 未找到必需子节点 'B'！");
			_log.Error("[AutoOscillatePlatform] 请确保当前节点包含名为 'B' 的 Node2D 子节点作为位置标记");
			allValid = false;
		}

		_isInitialized = allValid;
	}

	/// <summary>
	///     设置初始状态
	///     <para>
	///         将平台定位到A点，初始化方向和计时器
	///     </para>
	/// </summary>
	private void SetInitialState()
	{
		_moveDirection = 1;
		_isWaiting = false;
		_waitTimer = 0f;
		_progress = 0f;

		if (_platformMove != null && _pointA != null)
		{
			_platformMove.Position = _pointA.Position;
		}

		_log.Debug("[AutoOscillatePlatform] 🎬 初始状态已设置 (位于A点，准备向B移动)");
	}

	#endregion

	#region 运动逻辑

	/// <summary>
	///     更新平台位置
	///     <param name="delta">时间间隔（秒）</param>
	///     <remarks>
	///         使用基于进度的插值运动:
	///         1. 根据 speed 和 delta 计算本帧位移对应的进度增量
	///         2. 更新 _progress 并检查是否到达端点
	///         3. 使用 Lerp 在 A 和 B 之间插值计算实际位置
	///         4. 到达端点时触发等待状态
	///     </remarks>
	/// </summary>
	private void UpdateMovement(float delta)
	{
		var platform = _platformMove;
		var pointA = _pointA;
		var pointB = _pointB;

		if (platform == null || pointA == null || pointB == null)
		{
			return;
		}

		float distance = pointA.Position.DistanceTo(pointB.Position);

		if (distance < 0.001f)
		{
			_log.Warn("[AutoOscillatePlatform] ⚠️ A点和B点位置重合或距离过近，跳过运动更新");
			return;
		}

		float progressDelta = (Speed * delta) / distance;
		_progress += progressDelta * _moveDirection;

		bool reachedTarget = _moveDirection > 0 ? _progress >= 1.0f : _progress <= 0.0f;

		if (reachedTarget)
		{
			_progress = Mathf.Clamp(_progress, 0f, 1f);
			platform.Position = pointA.Position.Lerp(pointB.Position, _progress);
			StartWaiting();
		}
		else
		{
			platform.Position = pointA.Position.Lerp(pointB.Position, _progress);
		}
	}

	/// <summary>
	///     开始在端点处等待
	///     <para>
	///         设置等待标志、重置计时器、记录日志
	///     </para>
	/// </summary>
	private void StartWaiting()
	{
		_isWaiting = true;
		_waitTimer = 0f;

		string targetName = _moveDirection > 0 ? "B点" : "A点";
		_log.Info($"[AutoOscillatePlatform] ⏸️ 到达{targetName}，开始等待 {WaitTime}s...");
	}

	/// <summary>
	///     更新等待计时器
	///     <param name="delta">时间间隔（秒）</param>
	///     <remarks>
	///         等待时间结束后反转方向，恢复运动状态
	///     </remarks>
	/// </summary>
	private void UpdateWaitTimer(float delta)
	{
		_waitTimer += delta;

		if (_waitTimer >= WaitTime)
		{
			ReverseDirection();
			_isWaiting = false;
			_waitTimer = 0f;

			string newTarget = _moveDirection > 0 ? "B" : "A";
			_log.Info($"[AutoOscillatePlatform] ▶️ 等待结束，开始向{newTarget}点移动");
		}
	}

	/// <summary>
	///     反转运动方向
	///     <para>
	///         将 _moveDirection 取反，使平台向另一个端点移动
	///     </para>
	/// </summary>
	private void ReverseDirection()
	{
		_moveDirection *= -1;
		_log.Debug($"[AutoOscillatePlatform] 🔄 方向已反转 (当前: {_moveDirection})");
	}

	#endregion
}
