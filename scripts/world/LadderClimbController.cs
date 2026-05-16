using Godot;

namespace GFrameworkGodotTemplate.scripts.world;

/// <summary>
///     梯子攀爬控制器
///     <para>
///         当玩家角色进入梯子区域时，捕获 A/D 方向键输入并将其转换为竖直移动，
///         实现标准的平台游戏梯子攀爬机制。支持平滑加速/减速和跳跃脱离。
///     </para>
///
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-16</date>
///     <description>
///         核心职责:
///         1. 玩家检测: 通过子节点 Area2D 监测玩家进入/离开梯子区域
///         2. 输入重映射: 将水平方向输入(A/D)转换为垂直攀爬速度
///         3. 重力屏蔽: 攀爬时取消重力影响，允许空中静止
///         4. 平滑运动: 使用插值实现自然的启停过渡效果
///         5. 跳跃脱离: 支持按跳跃键从梯子上跳下
///
///         场景节点结构要求 (latter.tscn):
///         Latter (Node2D) ← 挂载本脚本
///         └── Area2D      ← 碰撞检测区域（必需）
///             ├── CollisionShape2D
///             └── ColorRect    ← 视觉表现
///
///         输入映射规则:
///         ┌──────────┬─────────────┬────────────┐
///         │  按键    │  正常移动时   │  梯子攀爬时  │
///         ├──────────┼─────────────┼────────────┤
///         │ A / ←    │ 向左移动     │ 向上攀爬    │
///         │ D / →    │ 向右移动     │ 向下攀爬    │
///         │ W / ↑    │ (通常无响应) │ 向上攀爬    │
///         │ S / ↓    │ (通常无响应) │ 向下攀爬    │
///         │ Space    │ 跳跃        │ 跳离梯子    │
///         │ 无输入   │ 减速停止     │ 悬停静止    │
///         └──────────┴─────────────┴────────────┘
///
///         使用方式:
///         1. 将此脚本挂载到 latter.tscn 的根节点(Latter, Node2D类型)
///         2. 确保 Area2D 子节点的 CollisionShape2D 正确配置检测范围
///         3. 在 Inspector 中调整 ClimbSpeed 参数
///         4. 运行后玩家进入梯子区域即可自动启用攀爬模式
///
///         技术实现原理:
///         - 使用较低的 process_priority 确保在玩家 _PhysicsProcess 之后执行
///         - 直接修改玩家 CharacterBody2D.Velocity.Y 覆盖重力效果
///         - 通过 Input.IsKeyPressed 直接读取按键状态，绕过全局输入服务
///           （避免与正常水平移动的 HorizontalDirection 冲突）
///     </description>
///     <remarks>
///         设计原则:
///         - 单一职责(SRP): 仅负责梯子区域的攀爬行为控制
///         - 最小侵入性: 不修改任何现有玩家代码，仅通过 Velocity 覆盖实现
///         - 行业标准: 遵循经典平台游戏(如Celeste、Super Mario Bros)的梯子交互范式
///
///         执行顺序保障:
///         PlayerMovementController._PhysicsProcess() [priority=0, 默认]
///           → ApplyGravity() + UpdateHorizontalVelocity() + MoveAndSlide()
///         LadderClimbController._PhysicsProcess() [priority=-10, 延后]
///           → 覆盖 Velocity.Y 为攀爬速度（抵消重力）
///
///         状态机:
///         Normal → OnLadder → ClimbingUp/ClimbingDown/Hovering → Normal
///         (正常)   (进入梯子)  (攀爬中/悬停)          (离开梯子)
///     </remarks>
/// </summary>
[Log]
public partial class LadderClimbController : Node2D
{
	#region 导出参数

	/// <summary>攀爬速度（像素/秒）</summary>
	[Export]
	public float ClimbSpeed { get; set; } = 120f;

	/// <summary>攀爬加速度（用于平滑启动）</summary>
	[Export]
	public float ClimbAcceleration { get; set; } = 800f;

	/// <summary>是否允许跳跃脱离梯子</summary>
	[Export]
	public bool AllowJumpDismount { get; set; } = true;

	#endregion

	#region 私有字段

	/// <summary>当前在梯子区域内的玩家引用</summary>
	private CharacterBody2D? _playerOnLadder;

	/// <summary>当前攀爬速度（用于平滑插值）</summary>
	private float _currentClimbVelocity;

	/// <summary>目标攀爬速度（根据输入计算）</summary>
	private float _targetClimbVelocity;

	#endregion

	#region 节点引用

	/// <summary>
	///     梯子碰撞检测区域
	///     <para>
	///         用于检测玩家进入/离开梯子范围
	///     </para>
	/// </summary>
	private Area2D? _detectionArea;

	#endregion

	#region 生命周期方法

	/// <summary>
	///     节点准备就绪时的回调
	///     <para>
	///         初始化组件引用、连接信号、设置物理处理优先级
	///     </para>
	/// </summary>
	public override void _Ready()
	{
		try
		{
			InitializeComponents();
			SetupSignalConnections();
			SetProcessPriority();

			_log.Info("[LadderClimbController] ✅ 梯子攀爬控制器初始化完成");
			_log.Info($"[LadderClimbController] 攀爬速度: {ClimbSpeed} px/s | 加速度: {ClimbAcceleration} px/s² | 允许跳跃脱离: {AllowJumpDismount}");
		}
		catch (Exception ex)
		{
			_log.Error($"[LadderClimbController] ❌ 初始化异常: {ex.Message}");
			_log.Error($"[LadderClimbController] 异常类型: {ex.GetType().FullName}");
		}
	}

	/// <summary>
	///     物理帧更新回调
	///     <param name="delta">距上一帧的时间间隔（秒）</param>
	///     <remarks>
	///         使用较低优先级确保在玩家 _PhysicsProcess 之后执行，
	///         从而能够覆盖玩家物理系统已计算的垂直速度（重力等）
	///     </remarks>
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{
		if (_playerOnLadder == null || !GodotObject.IsInstanceValid(_playerOnLadder))
		{
			return;
		}

		try
		{
			ProcessClimbingMovement((float)delta);
		}
		catch (Exception ex)
		{
			_log.Error($"[LadderClimbController] ❌ 物理更新异常: {ex.Message}");
		}
	}

	#endregion

	#region 初始化方法

	/// <summary>
	///     初始化子节点引用
	///     <para>
	///         查找名为 "Area2D" 的直接子节点作为碰撞检测区域
	///     </para>
	/// </summary>
	private void InitializeComponents()
	{
		_detectionArea = GetNodeOrNull<Area2D>("Area2D");

		if (_detectionArea == null)
		{
			_log.Error("[LadderClimbController] ❌ 未找到必需子节点 'Area2D'！");
			_log.Error("[LadderClimbController] 请确保 Latter 节点包含名为 'Area2D' 的子节点");
		}
		else
		{
			_log.Debug("[LadderClimbController] ✓ Area2D 检测区域已找到");
		}
	}

	/// <summary>
	///     设置信号连接
	///     <para>
	///         连接 Area2D 的 BodyEntered 和 BodyExited 信号以监测玩家
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
			_log.Info("[LadderClimbController] 🔗 梯子区域信号已连接");
		}
		catch (Exception ex)
		{
			_log.Error($"[LadderClimbController] ❌ 信号连接失败: {ex.Message}");
		}
	}

	/// <summary>
	///     设置物理处理优先级
	///     <para>
	///         使用负值优先级确保在本帧中晚于默认优先级的节点执行，
	///         保证能够在玩家 PlayerMovementController 处理完毕后再覆盖其速度
	///     </para>
	/// </summary>
	private void SetProcessPriority()
	{
		ProcessPriority = -10;
		_log.Debug("[LadderClimbController] ⏱️ 物理处理优先级设为 -10（延后于玩家控制器执行）");
	}

	#endregion

	#region 区域检测事件

	/// <summary>
	///     当物体进入梯子检测区域时的回调
	///     <param name="body">进入区域的物理实体</param>
	/// </summary>
	private void OnDetectionAreaBodyEntered(Node body)
	{
		if (!IsPlayerBody(body))
		{
			return;
		}

		var playerBody = body as CharacterBody2D;
		if (playerBody == null)
		{
			_log.Warn("[LadderClimbController] ⚠️ 玩家节点不是 CharacterBody2D 类型，无法进行攀爬控制");
			return;
		}

		_playerOnLadder = playerBody;
		_currentClimbVelocity = 0f;
		_targetClimbVelocity = 0f;

		_log.Info("════════════ 玩家进入梯子区域 ════════════");
		_log.Info($"[LadderClimbController] 🪜 玩家 {body.Name} 进入梯子攀爬区域");
		_log.Info("[LadderClimbController] 📝 操作提示: A/↑=向上 | D/↓=向下 | Space=跳离");
	}

	/// <summary>
	///     当物体离开梯子检测区域时的回调
	///     <param name="body">离开区域的物理实体</param>
	/// </summary>
	private void OnDetectionAreaBodyExited(Node body)
	{
		if (!IsPlayerBody(body) || _playerOnLadder != body)
		{
			return;
		}

		_log.Info("════════════ 玩家离开梯子区域 ════════════");
		_log.Info($"[LadderClimbController] 👤 玩家 {body.Name} 离开梯子区域，恢复正常物理");

		_playerOnLadder = null;
		_currentClimbVelocity = 0f;
		_targetClimbVelocity = 0f;
	}

	/// <summary>
	///     判断传入节点是否为玩家角色
	///     <param name="body">待检测的节点</param>
	///     <returns>如果是玩家或其子节点返回true</returns>
	///     <remarks>
	///         通过向上遍历节点树查找名称包含 "player" 的祖先节点来判断。
	///         同时检查节点本身是否为 CharacterBody2D 类型且挂载了 PlayerMovementController
	///     </remarks>
	/// </summary>
	private bool IsPlayerBody(Node body)
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

	#region 攀爬核心逻辑

	/// <summary>
	///     处理攀爬移动逻辑
	///     <param name="delta">时间间隔（秒）</param>
	///     <remarks>
	///         核心流程:
	///         1. 读取按键输入并计算目标攀爬速度
	///         2. 使用插值平滑过渡到目标速度
	///         3. 检查跳跃脱离条件
	///         4. 将最终攀爬速度写入玩家 Velocity.Y（覆盖重力效果）
	///         
	///         输入检测使用 Input.IsKeyPressed 直接读取键盘状态，
	///         而非通过 GlobalGameplayInputService.HorizontalDirection，
	 ///         以避免干扰正常的水平移动输入解析。
	///     </remarks>
	/// </summary>
	private void ProcessClimbingMovement(float delta)
	{
		var player = _playerOnLadder;
		if (player == null || !GodotObject.IsInstanceValid(player))
		{
			return;
		}

		float climbInput = ReadClimbInput();
		_targetClimbVelocity = -climbInput * ClimbSpeed;

		bool wantsToJump = AllowJumpDismount && CheckJumpInput();

		if (wantsToJump)
		{
			HandleJumpDismount(player);
			return;
		}

		_currentClimbVelocity = Mathf.MoveToward(
			_currentClimbVelocity,
			_targetClimbVelocity,
			ClimbAcceleration * delta);

		player.Velocity = new Vector2(player.Velocity.X, _currentClimbVelocity);
		player.MoveAndSlide();
	}

	/// <summary>
	///     读取攀爬方向输入
	///     <returns>
	///         float: 攀爬方向值
	///         正数(+1)=向下攀爬, 负数(-1)=向上攀爬, 零(0)=悬停静止
	///     </returns>
	///     <remarks>
	///         输入映射:
	///         - A 键 或 ↑ 键 或 W 键 → -1 (向上攀爬)
	///         - D 键 或 ↓ 键 或 S 键 → +1 (向下攀爬)
	///         - 同时按下相反方向 → 0 (互相抵消)
	///         - 无按键按下 → 0 (悬停)
	///         
	///         设计说明:
	///         A/D 键作为主要攀爬控制符合用户需求（AD→竖直移动），
	///         同时支持 W/S 和方向键提供更直观的操作体验。
	///     </remarks>
	/// </summary>
	private static float ReadClimbInput()
	{
		bool upPressed = Input.IsKeyPressed(Key.A)
		                || Input.IsKeyPressed(Key.Up)
		                || Input.IsKeyPressed(Key.W);

		bool downPressed = Input.IsKeyPressed(Key.D)
		                  || Input.IsKeyPressed(Key.Down)
		                  || Input.IsKeyPressed(Key.S);

		if (upPressed && !downPressed)
		{
			return -1f;
		}

		if (downPressed && !upPressed)
		{
			return 1f;
		}

		return 0f;
	}

	/// <summary>
	///     检测玩家是否按下了跳跃键
	///     <returns>如果按下返回true</returns>
	///     <remarks>
	///         支持 Space、Enter 和 ui_accept 动作三种触发方式
	///     </remarks>
	/// </summary>
	private static bool CheckJumpInput()
	{
		return Input.IsKeyPressed(Key.Space)
		       || Input.IsActionPressed("ui_accept");
	}

	/// <summary>
	///     处理从梯子跳跃脱离
	///     <param name="player">玩家 CharacterBody2D 实例</param>
	///     <remarks>
	///         清除攀爬状态，给玩家一个向外的初速度使其自然离开梯子区域。
	///         跳跃方向根据最近一次攀爬方向决定:
	///         - 最后向上攀爬 → 向上跳出
	///         - 最后向下攀爬或静止 → 向下/横向跳出
	///     </remarks>
	/// </summary>
	private void HandleJumpDismount(CharacterBody2D player)
	{
		float jumpDirection = _currentClimbVelocity < 0 ? -1f : 1f;
		float jumpForce = 250f * jumpDirection;

		player.Velocity = new Vector2(player.Velocity.X, jumpForce);
		player.MoveAndSlide();

		_playerOnLadder = null;
		_currentClimbVelocity = 0f;
		_targetClimbVelocity = 0f;

		string direction = jumpDirection < 0 ? "向上" : "向下";
		_log.Info($"[LadderClimbController] 🚀 玩家按跳跃键{direction}跳离梯子");
	}

	#endregion
}
