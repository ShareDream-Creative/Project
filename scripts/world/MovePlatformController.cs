using Godot;
using GFramework.Core.Abstractions.Controller;
using GFramework.SourceGenerators.Abstractions.Logging;
using GFramework.SourceGenerators.Abstractions.Rule;
using GFrameworkGodotTemplate.global;
using GFrameworkGodotTemplate.scripts.utility;

namespace GFrameworkGodotTemplate.scripts.world;

/// <summary>
///     移动平台控制器
///     <para>
///         负责管理交互式移动平台的完整行为逻辑
///         包括按钮区域检测、提示显示、按键控制移动等功能
///     </para>
///     
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-14</date>
///     <description>
///         核心职责:
///         1. 按钮区域检测: 监控玩家进入/离开 %button 区域
///         2. 提示元素管理: 控制线索 %clue 的显示/隐藏
///         3. 平台移动控制: 响应E键启动/停止平台移动
///         4. 往返运动: 在 %left 和 %right 之间匀速往返
///         5. 停顿机制: 到达边界时停留0.5秒后折返
///         
///         场景节点结构要求 (Level_2.tscn):
///         moveplatform (Node2D) ← 挂载本脚本
///         ├── PlatformMove (Node2D) ← 需要移动的平台对象
///         ├── left (Node2D) ← 左边界位置标记 (%left)
///         ├── right (Node2D) ← 右边界位置标记 (%right)
///         └── button (Area2D) ← 交互按钮区域 (%button)
///             ├── CollisionShape2D
///             ├── clue (ColorRect) ← 提示元素 (%clue, 初始隐藏)
///             └── ColorRect ← 按钮视觉
///         
///         使用方式:
///         1. 将此脚本挂载到 moveplatform 节点
///         2. 确保场景包含所有必需的子节点（%left, %right, %button, %clue, PlatformMove）
///         3. 设置 unique_name_in_owner = true 用于唯一名称访问
///         4. 玩家进入按钮区域按 E 键控制平台移动
///     </description>
///     <remarks>
///         设计原则:
///         - 单一职责(SRP): 专注于移动平台的交互和运动逻辑
///         - 开闭原则(OCP): 通过虚方法支持子类扩展
///         - 依赖倒置(DIP): 通过节点引用解耦具体场景结构
///         
///         状态机:
///         Idle → Moving → Waiting → Moving → ...
///         (空闲)  (移动中)   (等待)    (反向移动)
///         
///         输入处理:
///         - 仅在玩家位于 %button 区域内时响应 E 键
///         - 切换式控制: 按E键启动/停止移动
///     </remarks>
/// </summary>
[ContextAware]
[Log]
public partial class MovePlatformController : Node2D, IController
{
	#region 常量定义

	/// <summary>平台移动速度（像素/秒）</summary>
	private const float PLATFORM_SPEED = 100f;

	/// <summary>到达边界后的等待时间（秒）</summary>
	private const float WAIT_TIME_AT_BOUNDARY = 0.5f;

	#endregion

	#region 私有字段

	/// <summary>是否正在移动</summary>
	private bool _isMoving;

	/// <summary>当前移动方向（1=向右，-1=向左）</summary>
	private int _moveDirection = 1;

	/// <summary>是否在边界处等待</summary>
	private bool _isWaitingAtBoundary;

	/// <summary>边界等待计时器</summary>
	private float _waitTimer;

	/// <summary>玩家是否在按钮区域内</summary>
	private bool _isPlayerInButtonArea;

	/// <summary>
	///     全局游戏玩法输入服务
	///     <para>
	///         用于检测E键等交互输入
	 ///     </para>
	/// </summary>
	private IGlobalGameplayInputService? _globalInputService;

	#endregion

	#region 节点引用

	/// <summary>
	///     需要移动的平台对象
	///     <para>
	///         通常是一个实例化的场景或静态物体
	///         会在 left 和 right 之间水平移动
	 ///     </para>
	/// </summary>
	private Node2D PlatformMove => GetNode<Node2D>("PlatformMove");

	/// <summary>
	///     左边界位置标记
	///     <para>
	///         平台移动的左极限位置
	///         必须设置 unique_name_in_owner = true
	 ///     </para>
	/// </summary>
	private Node2D LeftPosition => GetNode<Node2D>("%left");

	/// <summary>
	///     右边界位置标记
	///     <para>
	///         平台移动的右极限位置
	///         必须设置 unique_name_in_owner = true
	 ///     </para>
	/// </summary>
	private Node2D RightPosition => GetNode<Node2D>("%right");

	/// <summary>
	///     交互按钮区域
	///     <para>
	///         Area2D类型的碰撞检测区域
	///         当玩家进入此区域时显示clue并响应E键
	///         必须设置 unique_name_in_owner = true
	 ///     </para>
	/// </summary>
	private Area2D ButtonArea => GetNode<Area2D>("%button");

	/// <summary>
	///     线索/提示元素
	///     <para>
	///         ColorRect类型的视觉提示
	///         当玩家在按钮区域内时显示，离开时隐藏
	///         默认状态为隐藏 (visible = false)
	 ///     </para>
	/// </summary>
	private ColorRect ClueElement => GetNode<ColorRect>("%clue");

	#endregion

	#region 生命周期方法

	/// <summary>
	///     节点准备就绪时的回调方法
	///     <para>
	///         初始化所有组件、连接信号、设置初始状态
	 ///     </para>
	/// </summary>
	public override void _Ready()
	{
		try
		{
			InitializeComponents();
			SetupSignalConnections();
			SetInitialState();
			
			_log.Info("[MovePlatformController] ✅ 移动平台控制器初始化完成");
			_log.Debug($"[MovePlatformController] 平台速度: {PLATFORM_SPEED} px/s, 边界等待: {WAIT_TIME_AT_BOUNDARY}s");
		}
		catch (Exception ex)
		{
			_log.Error($"[MovePlatformController] ❌ 初始化异常: {ex.Message}");
			_log.Error($"[MovePlatformController] 异常类型: {ex.GetType().FullName}");
			_log.Error($"[MovePlatformController] 堆栈跟踪:\n{ex.StackTrace}");
		}
	}

	/// <summary>
	///     每帧更新回调
	///     <param name="delta">距上一帧的时间间隔（秒）</param>
	/// </summary>
	public override void _Process(double delta)
	{
		try
		{
			if (_isMoving && !_isWaitingAtBoundary)
			{
				UpdatePlatformMovement((float)delta);
			}
			
			if (_isWaitingAtBoundary)
			{
				UpdateBoundaryWaitTimer((float)delta);
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[MovePlatformController] ❌ 更新循环异常: {ex.Message}");
		}
	}

	/// <summary>
	///     未处理的输入事件回调
	///     <param name="@event">输入事件对象</param>
	/// </summary>
	public override void _UnhandledInput(InputEvent @event)
	{
		try
		{
			if (@event is InputEventKey keyEvent && keyEvent.Pressed)
			{
				HandleKeyPress(keyEvent);
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[MovePlatformController] ❌ 输入处理异常: {ex.Message}");
		}
	}

	#endregion

	#region 初始化方法

	/// <summary>
	///     初始化所有组件引用
	///     <para>
	///         验证所有必需节点的存在性
	 ///     </para>
	/// </summary>
	private void InitializeComponents()
	{
		var platformMove = PlatformMove;
		var leftPos = LeftPosition;
		var rightPos = RightPosition;
		var buttonArea = ButtonArea;
		var clueElement = ClueElement;
		
		if (platformMove == null)
		{
			_log.Warn("[MovePlatformController] ⚠️ 未找到 PlatformMove 节点！");
		}
		
		if (leftPos == null)
		{
			_log.Warn("[MovePlatformController] ⚠️ 未找到 %left 节点！");
		}
		
		if (rightPos == null)
		{
			_log.Warn("[MovePlatformController] ⚠️ 未找到 %right 节点！");
		}
		
		if (buttonArea == null)
		{
			_log.Error("[MovePlatformController] ❌ 未找到 %button 节点！按钮检测功能将无法使用。");
		}
		
		if (clueElement == null)
		{
			_log.Warn("[MovePlatformController] ⚠️ 未找到 %clue 节点！");
		}
		
		try
		{
			_globalInputService = NodeTreeHelper.GetGlobalInputService(this);
			if (_globalInputService != null)
			{
				_log.Info("[MovePlatformController] ✅ 全局输入服务已获取 (通过NodeTreeHelper)");
			}
			else
			{
				_log.Warn("[MovePlatformController] ⚠️ 无法获取全局输入服务，将使用直接键盘检测方案");
			}
		}
		catch (Exception ex)
		{
			_log.Warn($"[MovePlatformController] ⚠️ 获取全局输入服务异常: {ex.Message}，将使用后备方案");
		}
		
		_log.Info("[MovePlatformController] 📦 组件初始化完成");
		_log.Debug($"[MovePlatformController] PlatformMove: {platformMove?.Name ?? "null"}");
		_log.Debug($"[MovePlatformController] Left: {leftPos?.Position}, Right: {rightPos?.Position}");
	}

	/// <summary>
	///     设置信号连接
	///     <para>
	///         连接按钮区域的 body_entered 和 body_exited 信号
	 ///     </para>
	/// </summary>
	private void SetupSignalConnections()
	{
		try
		{
			var buttonArea = ButtonArea;
			
			if (buttonArea != null)
			{
				buttonArea.BodyEntered += OnButtonAreaBodyEntered;
				buttonArea.BodyExited += OnButtonAreaBodyExited;
				
				_log.Info("[MovePlatformController] 🔗 按钮区域信号已连接");
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[MovePlatformController] ❌ 信号连接失败: {ex.Message}");
		}
	}

	/// <summary>
	///     设置初始状态
	/// </summary>
	private void SetInitialState()
	{
		_isMoving = false;
		_moveDirection = 1;
		_isWaitingAtBoundary = false;
		_waitTimer = 0f;
		_isPlayerInButtonArea = false;
		
		HideClue();
		
		_log.Info("[MovePlatformController] 🎬 初始状态已设置 (Idle)");
	}

	#endregion

	#region 按钮区域检测

	/// <summary>
	///     当物体进入按钮区域时的回调
	///     <param name="body">进入区域的物理实体</param>
	/// </summary>
	private void OnButtonAreaBodyEntered(Node body)
	{
		try
		{
			if (!IsPlayerBody(body))
			{
				_log.Debug($"[MovePlatformController] 非玩家物体 ({body.Name}) 进入按钮区域，忽略");
				return;
			}
			
			_isPlayerInButtonArea = true;
			ShowClue();
			
			_log.Info("════════════ 玩家进入按钮区域 ════════════");
			_log.Info($"[MovePlatformController] 👤 玩家 {body.Name} 进入 %button 区域");
			_log.Info($"[MovePlatformController] 💡 提示元素已显示 | 平台状态: {(_isMoving ? "移动中" : "静止")}");
			_log.Info("[MovePlatformController] 📝 按 E 键切换平台移动状态");
		}
		catch (Exception ex)
		{
			_log.Error($"[MovePlatformController] ❌ 处理进入事件异常: {ex.Message}");
		}
	}

	/// <summary>
	///     当物体离开按钮区域时的回调
	///     <param name="body">离开区域的物理实体</param>
	/// </summary>
	private void OnButtonAreaBodyExited(Node body)
	{
		try
		{
			if (!IsPlayerBody(body))
			{
				return;
			}
			
			_isPlayerInButtonArea = false;
			HideClue();
			
			_log.Info("════════════ 玩家离开按钮区域 ════════════");
			_log.Info($"[MovePlatformController] 👤 玩家 {body.Name} 离开 %button 区域");
			_log.Info($"[MovePlatformController] 💡 提示元素已隐藏");
		}
		catch (Exception ex)
		{
			_log.Error($"[MovePlatformController] ❌ 处理离开事件异常: {ex.Message}");
		}
	}

	/// <summary>
	///     判断是否为玩家物体
	///     <param name="body">待检测的节点</param>
	///     <returns>如果是玩家或其子节点返回true</returns>
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

	#region 提示元素管理

	/// <summary>
	///     显示提示元素
	/// </summary>
	private void ShowClue()
	{
		try
		{
			var clue = ClueElement;
			if (clue != null)
			{
				clue.Visible = true;
				_log.Debug("[MovePlatformController] ✨ Clue 元素已显示");
			}
		}
		catch (Exception ex)
		{
			_log.Warn($"[MovePlatformController] ⚠️ 显示Clue失败: {ex.Message}");
		}
	}

	/// <summary>
	///     隐藏提示元素
	/// </summary>
	private void HideClue()
	{
		try
		{
			var clue = ClueElement;
			if (clue != null)
			{
				clue.Visible = false;
				_log.Debug("[MovePlatformController] 🙈 Clue 元素已隐藏");
			}
		}
		catch (Exception ex)
		{
			_log.Warn($"[MovePlatformController] ⚠️ 隐藏Clue失败: {ex.Message}");
		}
	}

	#endregion

	#region 输入处理

	/// <summary>
	///     处理键盘按键事件
	///     <param name="keyEvent">键盘输入事件</param>
	/// </summary>
	private void HandleKeyPress(InputEventKey keyEvent)
	{
		if (!_isPlayerInButtonArea)
		{
			return;
		}
		
		bool isInteractPressed = false;
		
		if (_globalInputService != null && _globalInputService.IsInputEnabled)
		{
			isInteractPressed = keyEvent.Keycode == Key.E && keyEvent.Pressed;
		}
		else
		{
			isInteractPressed = keyEvent.Keycode == Key.E && keyEvent.Pressed;
		}
		
		if (isInteractPressed)
		{
			TogglePlatformMovement();
		}
	}

	/// <summary>
	///     切换平台移动状态
	///     <para>
	///         启动或停止平台移动
	 ///     </para>
	/// </summary>
	private void TogglePlatformMovement()
	{
		try
		{
			_isMoving = !_isMoving;
			
			if (_isMoving)
			{
				OnPlatformStarted();
			}
			else
			{
				OnPlatformStopped();
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[MovePlatformController] ❌ 切换移动状态异常: {ex.Message}");
		}
	}

	#endregion

	#region 平台移动逻辑

	/// <summary>
	///     更新平台位置
	///     <param name="delta">时间间隔（秒）</param>
	/// </summary>
	private void UpdatePlatformMovement(float delta)
	{
		try
		{
			var platform = PlatformMove;
			var leftPos = LeftPosition;
			var rightPos = RightPosition;
			
			if (platform == null || leftPos == null || rightPos == null)
			{
				_log.Warn("[MovePlatformController] ⚠️ 缺少必要的节点引用，跳过移动更新");
				return;
			}
			
			float movement = PLATFORM_SPEED * _moveDirection * delta;
			float newX = platform.Position.X + movement;
			
			float leftBound = leftPos.Position.X;
			float rightBound = rightPos.Position.X;
			
			bool reachedLeft = _moveDirection < 0 && newX <= leftBound;
			bool reachedRight = _moveDirection > 0 && newX >= rightBound;
			
			if (reachedLeft || reachedRight)
			{
				newX = reachedLeft ? leftBound : rightBound;
				platform.Position = new Vector2(newX, platform.Position.Y);
				
				StartBoundaryWait();
			}
			else
			{
				platform.Position = new Vector2(newX, platform.Position.Y);
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[MovePlatformController] ❌ 更新平台位置异常: {ex.Message}");
		}
	}

	/// <summary>
	///     开始边界等待
	/// </summary>
	private void StartBoundaryWait()
	{
		_isWaitingAtBoundary = true;
		_waitTimer = 0f;
		
		string boundaryName = _moveDirection > 0 ? "右边界" : "左边界";
		_log.Info($"[MovePlatformController] ⏸️ 到达{boundaryName}，开始等待 {WAIT_TIME_AT_BOUNDARY}s...");
	}

	/// <summary>
	///     更新边界等待计时器
	///     <param name="delta">时间间隔（秒）</param>
	/// </summary>
	private void UpdateBoundaryWaitTimer(float delta)
	{
		_waitTimer += delta;
		
		if (_waitTimer >= WAIT_TIME_AT_BOUNDARY)
		{
			ReverseDirection();
			_isWaitingAtBoundary = false;
			_waitTimer = 0f;
			
			string newDirection = _moveDirection > 0 ? "→ 向右" : "← 向左";
			_log.Info($"[MovePlatformController] ▶️ 等待结束，开始{newDirection}移动");
		}
	}

	/// <summary>
	///     反转移动方向
	/// </summary>
	private void ReverseDirection()
	{
		_moveDirection *= -1;
		_log.Debug($"[MovePlatformController] 🔄 方向已反转 (当前方向: {_moveDirection})");
	}

	#endregion

	#region 状态回调（可被子类重写）

	/// <summary>
	///     平台开始移动时的回调
	///     <para>
	///         可被子类重写以添加额外逻辑（如音效、动画等）
	 ///     </para>
	/// </summary>
	protected virtual void OnPlatformStarted()
	{
		_log.Info("════════════ 平台开始移动 ════════════");
		_log.Info("[MovePlatformController] ▶️ 平台移动已启动");
		_log.Info($"[MovePlatformController] 方向: {(_moveDirection > 0 ? "向右" : "向左")}, 速度: {PLATFORM_SPEED} px/s");
	}

	/// <summary>
	///     平台停止移动时的回调
	///     <para>
	///         可被子类重写以添加额外逻辑（如音效、动画等）
	 ///     </para>
	/// </summary>
	protected virtual void OnPlatformStopped()
	{
		_log.Info("════════════ 平台停止移动 ════════════");
		_log.Info("[MovePlatformController] ⏹️ 平台移动已停止");
		_log.Debug($"[MovePlatformController] 当前位置: {PlatformMove?.Position}");
	}

	#endregion
}
