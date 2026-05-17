using Godot;
using System.Collections.Generic;

namespace GFrameworkGodotTemplate.scripts.trap;

/// <summary>炮塔控制器（简化版 - 基础冷却计时器）</summary>
///     <para>
///         简化的发射逻辑：冷时间持续减少，
///         如果检测到玩家且冷时间<=0，则发射子弹并重置冷时间。
///     </para>
///     <version>4.0.0</version>
///     <date>2026-05-16</date>
/// </summary>
public partial class TurretController : Node2D
{
	#region 导出参数

	/// <summary>子弹飞行速度（像素/秒）</summary>
	[Export]
	public float BulletSpeed { get; set; } = 200f;

	/// <summary>攻击冷却时间（秒），两次射击间隔</summary>
	[Export]
	public float ColdTime { get; set; } = 1.5f;

	/// <summary>子弹生命周期（秒），超时自动销毁</summary>
	[Export]
	public float BulletLifetime { get; set; } = 10f;

	/// <summary>最大同时存在子弹数</summary>
	[Export(PropertyHint.Range, "1,50,1")]
	public int MaxActiveBullets { get; set; } = 10;

	/// <summary>是否反转发射方向</summary>
	[Export]
	public bool InvertDirection { get; set; } = true;

	/// <summary>画出检测区域</summary>
	[Export]
	public bool ShowDetectionArea { get; set; } = true;

	/// <summary>检测区域长度（像素）</summary>
	[Export(PropertyHint.Range, "10,500,1")]
	public float DetectLength
	{
		get => _detectLength;
		set
		{
			_detectLength = value;
			SyncCollisionShapeSize();
		}
	}

	/// <summary>检测区域宽度（像素）</summary>
	[Export(PropertyHint.Range, "5,200,1")]
	public float DetectWidth
	{
		get => _detectWidth;
		set
		{
			_detectWidth = value;
			SyncCollisionShapeSize();
		}
	}

	/// <summary>检测区域旋转角度（度）</summary>
	[Export(PropertyHint.Range, "-360,360,0.1")]
	public float DetectRotationDeg
	{
		get => _detectRotationDeg;
		set
		{
			_detectRotationDeg = value;
			SyncDetectRotation();
		}
	}

	#endregion

	#region 私有字段

	private bool _playerInZone;
	private float _cooldownTimer;

	private float _detectLength = 74f;
	private float _detectWidth = 20f;
	private float _detectRotationDeg = 0f;
	private bool _collisionVisible = true;

	private readonly List<ActiveBullet> _activeBullets = new(16);

	private Vector2 _fireDirection;

	#endregion

	#region 节点引用

	private Area2D? _detectArea;
	private CollisionShape2D? _detectCollisionShape;
	private Node2D? _bulletTemplate;

	#endregion

	#region 内部数据结构

	private struct ActiveBullet
	{
		public Node2D Node;
		public Vector2 Velocity;
		public float RemainingLife;
		public bool IsAlive;
	}

	#endregion

	#region 生命周期方法

	public override void _EnterTree()
	{
		base._EnterTree();

		try
		{
			InitializeComponents();
			SetupSignalConnections();
			CalculateFireDirection();
			_cooldownTimer = 0f; // 初始化为0，确保一开始就能发射

			if (!Engine.IsEditorHint())
			{
				GD.Print("[TurretController] ✅ 初始化完成（简化版）");
			}
		}
		catch (Exception ex)
		{
			if (!Engine.IsEditorHint())
			{
				GD.PrintErr($"[TurretController] ❌ 初始化异常: {ex.Message}");
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Engine.IsEditorHint()) return;

		try
		{
			float dt = (float)delta;

			UpdateCooldown(dt); // coldtime持续减少
			TryFire(); // 检测是否可以发射
			UpdateActiveBullets(dt);
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[TurretController] ❌ 物理更新异常: {ex.Message}");
		}
	}

	#endregion

	#region 初始化方法

	private void InitializeComponents()
	{
		_detectArea = GetNodeOrNull<Area2D>("Detect");

		if (_detectArea == null && !Engine.IsEditorHint())
		{
			GD.PrintErr("[TurretController] ❌ 未找到 'Detect'(Area2D)");
		}

		if (_detectArea != null)
		{
			_detectCollisionShape = _detectArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (_detectCollisionShape == null && !Engine.IsEditorHint())
			{
				GD.PrintErr("[TurretController] ❌ 未找到 Detect 下的 'CollisionShape2D'");
			}
		}

		SyncCollisionShapeSize();
		SyncDetectRotation();

		_bulletTemplate = GetNodeOrNull<Node2D>("Bullet");

		if (_bulletTemplate == null && !Engine.IsEditorHint())
		{
			GD.PrintErr("[TurretController] ❌ 未找到 'Bullet'(子弹模板)");
		}
		else if (_bulletTemplate != null && !Engine.IsEditorHint())
		{
			_bulletTemplate.Visible = false;
		}
	}

	private void SetupSignalConnections()
	{
		if (Engine.IsEditorHint() || _detectArea == null) return;

		_detectArea.BodyEntered += OnDetectBodyEntered;
		_detectArea.BodyExited += OnDetectBodyExited;
	}

	private void CalculateFireDirection()
	{
		_fireDirection = Vector2.Right;
	}

	private void SyncCollisionShapeSize()
	{
		if (_detectCollisionShape == null || !GodotObject.IsInstanceValid(_detectCollisionShape)) return;
		if (_detectCollisionShape.Shape is not RectangleShape2D rectShape) return;

		rectShape.Size = new Vector2(_detectLength, _detectWidth);

		float rightEdgeAnchor = 10f;
		float newX = rightEdgeAnchor - _detectLength * 0.5f;
		Vector2 currentPos = _detectCollisionShape.Position;
		_detectCollisionShape.Position = new Vector2(newX, currentPos.Y);
	}

	private void SyncDetectRotation()
	{
		float rotRad = Mathf.DegToRad(_detectRotationDeg);
		Rotation = rotRad;

		if (_detectArea != null && GodotObject.IsInstanceValid(_detectArea))
		{
			_detectArea.Rotation = 0f;
		}

		SyncCollisionShapeSize();
		CalculateFireDirection();
		QueueRedraw();
	}

	#endregion

	#region 检测域事件

	private void OnDetectBodyEntered(Node body)
	{
		if (!IsPlayerBody(body)) return;

		_playerInZone = true;
		GD.Print("[TurretController] 🎯 检测到目标");
	}

	private void OnDetectBodyExited(Node body)
	{
		if (!IsPlayerBody(body)) return;

		_playerInZone = false;
		GD.Print("[TurretController] 👤 目标离开检测区域");
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

	#region 射击核心逻辑（简化版）

	/// <summary>
	/// 简化的发射逻辑：
	/// coldtime持续减少，
	/// 如果检测到玩家且coldtime<=0，则发射子弹并重置冷时间
	/// </summary>
	private void TryFire()
	{
		// 条件1：玩家在检测区域内
		// 条件2：冷时间 <= 0
		if (!_playerInZone) return;
		if (!(_cooldownTimer <= 0f)) return;

		// 额外检查
		if (_activeBullets.Count >= MaxActiveBullets) return;
		if (_bulletTemplate == null || !GodotObject.IsInstanceValid(_bulletTemplate)) return;

		// 发射
		FireBullet();

		// 重置冷时间
		_cooldownTimer = ColdTime;

		GD.Print("[TurretController] 🔫 发射成功");
	}

	private void FireBullet()
	{
		var newBullet = (Node2D)_bulletTemplate!.Duplicate();
		AddChild(newBullet);

		newBullet.Visible = true;
		newBullet.Position = Vector2.Zero;

		Vector2 direction = InvertDirection ? -_fireDirection : _fireDirection;
		Vector2 velocity = direction * BulletSpeed;

		_activeBullets.Add(new ActiveBullet
		{
			Node = newBullet,
			Velocity = velocity,
			RemainingLife = BulletLifetime,
			IsAlive = true
		});

		GD.Print($"[TurretController] 🔫 发射 | 方向({velocity.X:F0},{velocity.Y:F0}) | 速度{BulletSpeed} px/s");
	}

	private void UpdateCooldown(float delta)
	{
		// coldtime持续减少
		_cooldownTimer -= delta;
	}

	#endregion

	#region 子弹生命周期管理

	private void UpdateActiveBullets(float delta)
	{
		int count = _activeBullets.Count;
		for (int i = count - 1; i >= 0; i--)
		{
			var bullet = _activeBullets[i];

			if (!bullet.IsAlive) continue;
			if (!GodotObject.IsInstanceValid(bullet.Node))
			{
				bullet.IsAlive = false;
				_activeBullets.RemoveAt(i);
				continue;
			}

			bullet.Node.Position += bullet.Velocity * delta;
			bullet.RemainingLife -= delta;

			if (bullet.RemainingLife <= 0f)
			{
				DestroyBullet(i);
			}
			else
			{
				_activeBullets[i] = bullet;
			}
		}
	}

	private void DestroyBullet(int index)
	{
		if (index < 0 || index >= _activeBullets.Count) return;

		var bullet = _activeBullets[index];
		if (!GodotObject.IsInstanceValid(bullet.Node))
		{
			_activeBullets.RemoveAt(index);
			return;
		}

		HideBulletVisuals(bullet.Node);
		bullet.Node.QueueFree();

		bullet.IsAlive = false;
		_activeBullets.RemoveAt(index);

		GD.Print("[TurretController] 💨 子弹销毁");
	}

	private static void HideBulletVisuals(Node2D bulletNode)
	{
		bulletNode.Visible = false;

		foreach (var child in bulletNode.GetChildren())
		{
			if (!GodotObject.IsInstanceValid(child)) continue;

			if (child is CanvasItem canvasItem)
			{
				canvasItem.Visible = false;
			}
			else if (child is CollisionShape2D shape)
			{
				shape.Disabled = true;
			}
			else if (child is Area2D area)
			{
				area.Monitoring = false;
			}
		}
	}

	#endregion

	#region 碰撞体可视化控制

	public override void _Draw()
	{
		if(!ShowDetectionArea)	return;
		
			base._Draw();
		

		if (_detectCollisionShape == null || !GodotObject.IsInstanceValid(_detectCollisionShape)) return;

		bool shouldDraw = OS.IsDebugBuild() || _collisionVisible;
		if (!shouldDraw) return;

		Vector2 shapePos = _detectCollisionShape.Position;
		float halfLen = _detectLength * 0.5f;
		float halfWid = _detectWidth * 0.5f;

		Vector2 topLeft = shapePos + new Vector2(-halfLen, -halfWid);
		Vector2 topRight = shapePos + new Vector2(halfLen, -halfWid);
		Vector2 bottomRight = shapePos + new Vector2(halfLen, halfWid);
		Vector2 bottomLeft = shapePos + new Vector2(-halfLen, halfWid);

		Color outlineColor = _collisionVisible
			? new Color(0f, 1f, 0.3f, 0.8f)
			: new Color(1f, 0.85f, 0f, 0.6f);

		float lineWidth = 1.5f;

		DrawLine(topLeft, topRight, outlineColor, lineWidth);
		DrawLine(topRight, bottomRight, outlineColor, lineWidth);
		DrawLine(bottomRight, bottomLeft, outlineColor, lineWidth);
		DrawLine(bottomLeft, topLeft, outlineColor, lineWidth);

		DrawArc(shapePos, 3f, 0f, Mathf.Tau, 16, outlineColor, 1f);

		Color arrowColor = new Color(1f, 0.3f, 0.3f, 0.9f);
		float arrowEndX = shapePos.X + halfLen + 15f;
		DrawLine(new Vector2(shapePos.X + halfLen, shapePos.Y), new Vector2(arrowEndX, shapePos.Y), arrowColor, 1.8f);

		float arrowSize = 6f;
		Vector2 arrowBase = new Vector2(arrowEndX - arrowSize, shapePos.Y);
		DrawLine(new Vector2(arrowEndX, shapePos.Y), new Vector2(arrowBase.X, arrowBase.Y - arrowSize * 0.45f), arrowColor, 1.4f);
		DrawLine(new Vector2(arrowEndX, shapePos.Y), new Vector2(arrowBase.X, arrowBase.Y + arrowSize * 0.45f), arrowColor, 1.4f);
	}
	#endregion
}
