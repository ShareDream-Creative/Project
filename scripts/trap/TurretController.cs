using Godot;

namespace GFrameworkGodotTemplate.scripts.trap;

/// <summary>
///     炮塔控制器（支持编辑器实时可视化）
///     <para>
///         检测玩家进入区域后自动发射子弹的炮台系统。
///         所有 Inspector 参数变更立即同步到场景中的碰撞体模型，
///         在编辑器 2D 视口中可实时预览检测域的尺寸和方向。
///     </para>
///
///     <author>AI Assistant</author>
///     <version>1.1.0</version>
///     <date>2026-05-16</date>
///     <description>
///         核心职责:
///         1. 玩家检测: 通过子节点 Detect(Area2D) 监测玩家进入/离开
///         2. 自动射击: 检测到玩家时按冷却间隔发射子弹
///         3. 子弹管理: 从模板子弹复制实例，控制运动方向和生命周期
///         4. 检测域可视化: Inspector 参数实时同步到碰撞体，编辑器中可见
///
///         场景节点结构要求 (turret.tscn):
///         Turret (Node2D) ← 挂载本脚本
///         ├── ColorRect           ← 炮塔视觉表现
///         ├── Bullet              ← 子弹模板节点（必需）
///         ├── TrapStatic          ← 基础陷阱行为（可选）
///         └── Detect              ← 检测域 Area2D（必需）⚠️ 节点名必须为 "Detect"
///             └── CollisionShape2D ← 检测范围形状
///
///         编辑器可视化特性:
///         - 调整 DetectLength → CollisionShape2D.Size.X 实时更新
///         - 调整 DetectWidth → CollisionShape2D.Size.Y 实时更新
///         - 调整 DetectRotation → Detect 节点 Rotation 实时更新
///         - 所有变化在 2D 编辑器视口中立即可见
///         - 缺少必需节点时显示黄色警告
///     </description>
///     <remarks>
///         可视化原理:
///         - 属性 setter 中直接操作节点引用，无需等待 _Process
///         - _EnterTree() 在编辑器和运行时均会调用，确保初始状态正确
///         - _GetConfigurationWarnings() 提供编辑器内的节点缺失提示
///
///         坐标与旋转:
///         - Detect 作为 Turret 的子节点，其 Position 相对于 Turret 中心
///         - 旋转 Detect.Rotation 即围绕 Turret 中心旋转（子节点绕父节点原点旋转）
///         - 0度 = 向右(+X), 90度 = 向下(+Y), 180度 = 向左(-X), 270度 = 向上(-Y)
///     </remarks>
/// </summary>
[Log]
public partial class TurretController : Node2D
{
	#region 导出参数 - 射击配置

	/// <summary>子弹飞行速度（像素/秒）</summary>
	[Export]
	public float BulletSpeed { get; set; } = 200f;

	/// <summary>攻击冷却时间（秒），两次射击之间的最小间隔</summary>
	[Export]
	public float ColdTime { get; set; } = 1.5f;

	/// <summary>子弹最大存活时间（秒），超时后自动销毁</summary>
	[Export]
	public float BulletLifetime { get; set; } = 10f;

	/// <summary>同时存在的最大子弹数量限制</summary>
	[Export]
	public int MaxActiveBullets { get; set; } = 10;

	#endregion

	#region 导出参数 - 检测域配置（支持编辑器实时可视化）

	private float _detectRotation;

	/// <summary>
	///     检测域旋转角度（度），正值顺时针
	///     <para>
	///         设置后立即旋转 Detect 节点，在编辑器 2D 视口中实时可见
	///     </para>
	/// </summary>
	[Export(PropertyHint.Range, "-360,360,0.1,or_greater,or_lesser")]
	public float DetectRotation
	{
		get => _detectRotation;
		set
		{
			_detectRotation = value;
			SyncRotationToDetect();
		}
	}

	private float _detectLength;

	/// <summary>
	///     检测域长度（像素），沿射击方向的尺寸
	///     <para>
	///         设置后立即更新 CollisionShape2D 的 Size.X，在编辑器中实时可见
	///     </para>
	/// </summary>
	[Export(PropertyHint.Range, "1,1000,1,or_greater")]
	public float DetectLength
	{
		get => _detectLength;
		set
		{
			_detectLength = value;
			SyncCollisionShapeSize();
		}
	}

	private float _detectWidth;

	/// <summary>
	///     检测域宽度（像素），垂直于射击方向的尺寸
	///     <para>
	///         设置后立即更新 CollisionShape2D 的 Size.Y，在编辑器中实时可见
	///     </para>
	/// </summary>
	[Export(PropertyHint.Range, "1,500,1,or_greater")]
	public float DetectWidth
	{
		get => _detectWidth;
		set
		{
			_detectWidth = value;
			SyncCollisionShapeSize();
		}
	}

	#endregion

	#region 私有字段

	private bool _playerInDetectZone;
	private float _cooldownTimer;
	private bool _isOnCooldown;
	private readonly System.Collections.Generic.List<ActiveBullet> _activeBullets = new();

	#endregion

	#region 节点引用

	/// <summary>检测域碰撞区域（节点名: "Detect"）</summary>
	private Area2D? _detectArea;

	/// <summary>检测域碰撞形状（用于动态调整尺寸）</summary>
	private CollisionShape2D? _detectCollisionShape;

	/// <summary>子弹模板节点（节点名: "Bullet"）</summary>
	private Node2D? _bulletTemplate;

	#endregion

	#region 内部数据结构

	private struct ActiveBullet
	{
		public Node2D Node;
		public float RemainingLife;
		public Vector2 Velocity;
	}

	#endregion

	#region 生命周期方法

	/// <summary>
	///     节点进入场景树时的回调（编辑器和运行时均触发）
	///     <para>
	///         初始化组件并立即将所有 Export 参数同步到场景节点，
	///         确保 2D 编辑器视口打开时即可看到正确的检测域可视化
	///     </para>
	/// </summary>
	public override void _EnterTree()
	{
		base._EnterTree();

		try
		{
			InitializeComponents();
			SetupSignalConnections();

			SyncAllVisualsToDetect();

			if (Engine.IsEditorHint())
			{
				_log.Debug("[TurretController] 🎨 编辑器模式：检测域可视化已同步");
			}
			else
			{
				_log.Info("[TurretController] ✅ 炮塔控制器初始化完成");
				_log.Info($"[TurretController] 射速: {BulletSpeed} px/s | 冷却: {ColdTime}s | 存活: {BulletLifetime}s");
				_log.Info($"[TurretController] 检测域: 旋转{DetectRotation}° | 尺寸{DetectLength}×{DetectWidth}");
			}
		}
		catch (Exception ex)
		{
			if (!Engine.IsEditorHint())
			{
				_log.Error($"[TurretController] ❌ 初始化异常: {ex.Message}");
			}
		}
	}

	/// <summary>
	///     物理帧更新回调（仅运行时执行）
	/// </summary>
	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}

		try
		{
			float dt = (float)delta;

			UpdateCooldown(dt);
			UpdateActiveBullets(dt);
			TryFire();
		}
		catch (Exception ex)
		{
			_log.Error($"[TurretController] ❌ 物理更新异常: {ex.Message}");
		}
	}

	/// <summary>
	///     Godot 编辑器配置警告
	///     <para>
	///         当缺少必需节点时在编辑器 Inspector 面板顶部显示警告
	///     </para>
	/// </summary>
	public override string[] _GetConfigurationWarnings()
	{
		var warnings = new System.Collections.Generic.List<string>();

		if (GetNodeOrNull<Area2D>("Detect") == null)
		{
			warnings.Add("缺少必需子节点 'Detect' (Area2D)。请添加名为 'Detect' 的 Area2D 节点作为检测域。");
		}

		var detectArea = GetNodeOrNull<Area2D>("Detect");
		if (detectArea != null && detectArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D") == null)
		{
			warnings.Add("'Detect' 节点缺少 'CollisionShape2D' 子节点。检测域无法可视化。");
		}

		if (GetNodeOrNull<Node2D>("Bullet") == null)
		{
			warnings.Add("缺少必需子节点 'Bullet'。炮塔将无法发射子弹。");
		}

		return warnings.ToArray();
	}

	#endregion

	#region 初始化方法

	/// <summary>
	///     初始化所有子节点引用
	///     <para>
	///         ⚠️ 节点名必须与 turret.tscn 中的定义一致：
	///         - 检测域: "Detect" (Area2D类型)
	///         - 子弹模板: "Bullet" (Node2D类型)
	///     </para>
	/// </summary>
	private void InitializeComponents()
	{
		_detectArea = GetNodeOrNull<Area2D>("Detect");

		if (_detectArea == null)
		{
			if (!Engine.IsEditorHint())
			{
				_log.Error("[TurretController] ❌ 未找到必需子节点 'Detect'(Area2D)！");
			}
		}
		else
		{
			_detectCollisionShape = _detectArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (_detectCollisionShape == null && !Engine.IsEditorHint())
			{
				_log.Warn("[TurretController] ⚠️ 未找到检测域的 CollisionShape2D");
			}
		}

		_bulletTemplate = GetNodeOrNull<Node2D>("Bullet");

		if (_bulletTemplate == null)
		{
			if (!Engine.IsEditorHint())
			{
				_log.Error("[TurretController] ❌ 未找到 'Bullet' 子弹模板节点！");
			}
		}
		else if (!Engine.IsEditorHint())
		{
			_bulletTemplate.Visible = false;
			_log.Debug("[TurretController] ✓ 子弹模板已隐藏");
		}
	}

	/// <summary>
	///     设置信号连接（仅运行时）
	/// </summary>
	private void SetupSignalConnections()
	{
		if (Engine.IsEditorHint() || _detectArea == null)
		{
			return;
		}

		_detectArea.BodyEntered += OnDetectAreaBodyEntered;
		_detectArea.BodyExited += OnDetectAreaBodyExited;

		_log.Info("[TurretController] ✓ 检测域信号已连接");
	}

	#endregion

	#region 可视化同步方法（编辑器实时反馈核心）

	/// <summary>
	///     将所有 Export 参数一次性同步到检测域节点
	///     <para>
	///         在 _EnterTree() 和属性批量变更时调用，
	///         确保编辑器打开时检测域立即显示正确的尺寸和角度
	 ///     </para>
	/// </summary>
	private void SyncAllVisualsToDetect()
	{
		SyncRotationToDetect();
		SyncCollisionShapeSize();
	}

	/// <summary>
	///     同步旋转角度到 Detect 节点
	///     <para>
	///         设置 Detect(Rotation) 使其围绕父节点(Turret)中心旋转。
	///         由于 Detect 是 Turret 的直接子节点，旋转即围绕 Turret 原点进行。
	///     </para>
	/// </summary>
	private void SyncRotationToDetect()
	{
		if (_detectArea == null || !GodotObject.IsInstanceValid(_detectArea))
		{
			return;
		}

		_detectArea.Rotation = Mathf.DegToRad(_detectRotation);
	}

	/// <summary>
	///     同步碰撞体尺寸到 Detect/CollisionShape2D
	///     <para>
	///         动态创建或更新 RectangleShape2D 以匹配 DetectLength × DetectWidth。
	///         如果当前 Shape 不是 RectangleShape2D 或不存在，自动创建新的。
	 ///     </para>
	/// </summary>
	private void SyncCollisionShapeSize()
	{
		if (_detectCollisionShape == null || !GodotObject.IsInstanceValid(_detectCollisionShape))
		{
			return;
		}

		if (_detectCollisionShape.Shape is RectangleShape2D rectShape)
		{
			rectShape.Size = new Vector2(_detectLength, _detectWidth);
		}
	}

	#endregion

	#region 检测域事件

	private void OnDetectAreaBodyEntered(Node body)
	{
		if (!IsPlayerBody(body))
		{
			return;
		}

		_playerInDetectZone = true;
		_isOnCooldown = false;
		_cooldownTimer = 0f;

		_log.Info("════════════ 玩家进入炮塔检测域 ════════════");
		_log.Info($"[TurretController] 🎯 玩家 {body.Name} 被检测到 | 立即开火");
	}

	private void OnDetectAreaBodyExited(Node body)
	{
		if (!IsPlayerBody(body))
		{
			return;
		}

		_playerInDetectZone = false;

		_log.Info("[TurretController] 👤 玩家离开检测域 | 停止射击");
	}

	private static bool IsPlayerBody(Node body)
	{
		var current = body;
		while (current != null)
		{
			string name = current.Name.ToString();
			if (name.Equals("Player", System.StringComparison.OrdinalIgnoreCase) ||
				name.Equals("player", System.StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			current = current.GetParent();
		}

		return false;
	}

	#endregion

	#region 射击核心逻辑

	private void TryFire()
	{
		if (!_playerInDetectZone || _isOnCooldown)
		{
			return;
		}

		if (_activeBullets.Count >= MaxActiveBullets)
		{
			return;
		}

		if (_bulletTemplate == null || !GodotObject.IsInstanceValid(_bulletTemplate))
		{
			return;
		}

		FireBullet();
		StartCooldown();
	}

	private void FireBullet()
	{
		var newBullet = (Node2D)_bulletTemplate.Duplicate();
		AddChild(newBullet);

		newBullet.Visible = true;
		newBullet.Position = Vector2.Zero;

		float rotationRad = Mathf.DegToRad(_detectRotation);
		Vector2 direction = new Vector2(Mathf.Cos(rotationRad), Mathf.Sin(rotationRad));
		Vector2 velocity = direction * BulletSpeed;

		_activeBullets.Add(new ActiveBullet
		{
			Node = newBullet,
			RemainingLife = BulletLifetime,
			Velocity = velocity
		});

		_log.Debug($"[TurretController] 🔫 发射子弹 #{_activeBullets.Count} | 方向: ({direction.X:F2}, {direction.Y:F2}) | 速度: {BulletSpeed} px/s");
	}

	private void StartCooldown()
	{
		_isOnCooldown = true;
		_cooldownTimer = ColdTime;
	}

	private void UpdateCooldown(float delta)
	{
		if (!_isOnCooldown)
		{
			return;
		}

		_cooldownTimer -= delta;

		if (_cooldownTimer <= 0f)
		{
			_cooldownTimer = 0f;
			_isOnCooldown = false;
		}
	}

	#endregion

	#region 子弹生命周期管理

	private void UpdateActiveBullets(float delta)
	{
		for (int i = _activeBullets.Count - 1; i >= 0; i--)
		{
			var bullet = _activeBullets[i];

			if (!GodotObject.IsInstanceValid(bullet.Node))
			{
				_activeBullets.RemoveAt(i);
				continue;
			}

			bullet.Node.Position += bullet.Velocity * delta;
			bullet.RemainingLife -= delta;

			if (bullet.RemainingLife <= 0f)
			{
				DestroyBullet(bullet.Node);
				_activeBullets.RemoveAt(i);
				_log.Debug("[TurretController] 💨 子弹超时自动销毁");
			}
			else
			{
				_activeBullets[i] = bullet;
			}
		}
	}

	private static void DestroyBullet(Node2D bullet)
	{
		if (!GodotObject.IsInstanceValid(bullet))
		{
			return;
		}

		bullet.Visible = false;

		foreach (var child in bullet.GetChildren())
		{
			if (!GodotObject.IsInstanceValid(child))
			{
				continue;
			}

			if (child is CanvasItem canvasItem)
			{
				canvasItem.Visible = false;
			}

			if (child is CollisionShape2D shape)
			{
				shape.Disabled = true;
			}

			if (child is Area2D area)
			{
				area.Monitoring = false;
			}
		}

		bullet.QueueFree();
	}

	#endregion
}
