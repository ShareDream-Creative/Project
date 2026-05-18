using Godot;

namespace GFrameworkGodotTemplate.scripts.trap;

[GlobalClass]
public partial class RayTrap : Node2D
{
	#region 导出参数

	[ExportGroup("射线参数")]
	[Export] public float MaxRayDistance { get; set; } = 200.0f;
	[Export] public float StartOffset { get; set; } = 1.0f;

	[ExportGroup("视觉效果")]
	[Export] public Color RayColor { get; set; } = new Color(1, 0, 0, 0.7f);

	[ExportGroup("消灭效果")]
	[Export] public bool EnableFlashEffect { get; set; } = true;
	[Export] public float FlashDuration { get; set; } = 0.3f;
	[Export] public int FlashCount { get; set; } = 3;

	#endregion

	#region 私有字段

	private Node2D? _rayEndPoint;
	private Node2D? _foundPoint;
	private ColorRect? _rayVisual;
	private Vector2 _fixedRayEndPosition;
	private float _currentLength;
	private float _colorRectHeight;
	private float _colorRectOffsetTop;
	private float _colorRectOffsetBottom;
	private Vector2 _initialRayGlobalOffset;
	private float _parentScaleY;
	private bool _isKillingPlayer;
	private Godot.Collections.Array<Rid>? _excludeList;

	#endregion

	#region 生命周期方法

	public override void _Ready()
	{
		InitializeNodes();
		InitializeVisuals();
		BuildExcludeList();
	}

	public override void _Process(double delta)
	{
		if (_isKillingPlayer) return;

		PerformRayDetection();
		UpdateRayTransform();
	}

	#endregion

	#region 初始化方法

	private void InitializeNodes()
	{
		_rayEndPoint = GetNodeOrNull<Node2D>("ray");
		_foundPoint = GetNodeOrNull<Node2D>("found");

		if (_rayEndPoint == null || _foundPoint == null)
			GD.PrintErr("[RayTrap] ❌ 节点结构不完整！");
	}

	private void InitializeVisuals()
	{
		if (_rayEndPoint == null || _foundPoint == null) return;

		var colorRect = _rayEndPoint.GetNodeOrNull<ColorRect>("ColorRect");
		if (colorRect != null)
			colorRect.Color = RayColor;

		_rayVisual = colorRect;
		_colorRectHeight = colorRect?.Size.Y ?? 40.0f;
		_colorRectOffsetTop = colorRect?.OffsetTop ?? -19.0f;
		_colorRectOffsetBottom = colorRect?.OffsetBottom ?? 21.0f;

		_initialRayGlobalOffset = _rayEndPoint.GlobalPosition - _foundPoint.GlobalPosition;
		_parentScaleY = Scale.Y;

		var from = _foundPoint.GlobalPosition;
		float effectiveOffset = StartOffset * _parentScaleY;
		from.Y -= effectiveOffset;

		_fixedRayEndPosition = from;
		_fixedRayEndPosition.Y -= MaxRayDistance;

		_currentLength = MaxRayDistance;

		UpdateRayTransform();

		GD.Print($"[RayTrap] ✅ 射线系统初始化完成");
		GD.Print($"[RayTrap]    最大检测距离: {MaxRayDistance}px");
		GD.Print($"[RayTrap]    ColorRect高度: {_colorRectHeight}px");
		GD.Print($"[RayTrap]    父级Y缩放: {_parentScaleY}");
		GD.Print($"[RayTrap]    有效起始偏移: {effectiveOffset:F2}px");
		GD.Print($"[RayTrap]    固定检测终点: {_fixedRayEndPosition}");
	}

	private void BuildExcludeList()
	{
		_excludeList = new Godot.Collections.Array<Rid>();
		CollectCollisionShapesFromNode(this);
		GD.Print($"[RayTrap] ✅ 已排除 {_excludeList.Count} 个自身碰撞体");
	}

	private void CollectCollisionShapesFromNode(Node node)
	{
		foreach (var child in node.GetChildren())
		{
			if (child is CollisionShape2D collisionShape && IsInstanceValid(collisionShape))
			{
				try
				{
					var rid = (Rid)collisionShape.Call("get_rid");
					if (rid.IsValid)
						_excludeList.Add(rid);
				}
				catch { }
			}
			else if (child is PhysicsBody2D physicsBody && IsInstanceValid(physicsBody))
			{
				try
				{
					var rid = (Rid)physicsBody.Call("get_rid");
					if (rid.IsValid)
						_excludeList.Add(rid);
				}
				catch { }
			}

			CollectCollisionShapesFromNode(child);
		}
	}

	#endregion

	#region 核心检测逻辑

	private void PerformRayDetection()
	{
		if (_rayEndPoint == null || _foundPoint == null || _excludeList == null) return;

		var spaceState = GetWorld2D().DirectSpaceState;
		if (spaceState == null) return;

		var from = _foundPoint.GlobalPosition;
		float effectiveOffset = StartOffset * _parentScaleY;
		from.Y -= effectiveOffset;

		var to = _fixedRayEndPosition;

		var query = PhysicsRayQueryParameters2D.Create(from, to);
		query.CollideWithAreas = false;
		query.CollideWithBodies = true;
		query.HitFromInside = false;
		query.Exclude = _excludeList;

		var result = spaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			var collisionPoint = result["position"].AsVector2();
			var collider = result["collider"].As<GodotObject>();
			Node? collisionNode = collider as Node;

			_currentLength = Mathf.Abs(collisionPoint.Y - from.Y);

			GD.Print($"[RayTrap] 检测到物体: {collisionNode?.Name ?? "未知"} (距离: {_currentLength:F1}px)");

			if (collisionNode != null && IsPlayerBody(collisionNode))
			{
				GD.Print("💀 [RayTrap] 检测到玩家！");
				KillPlayer(collisionNode);
			}
		}
		else
		{
			_currentLength = MaxRayDistance;
		}
	}

	#endregion

	#region Transform更新（Scale.Y + Position）

	private void UpdateRayTransform()
	{
		if (_rayEndPoint == null || _foundPoint == null) return;

		float effectiveColorRectHeight = _colorRectHeight * _parentScaleY;
		float scaleY = _currentLength / effectiveColorRectHeight;
		_rayEndPoint.Scale = new Vector2(_rayEndPoint.Scale.X, scaleY);

		float scaledOffsetTop = _colorRectOffsetTop * scaleY * _parentScaleY;
		float scaledOffsetBottom = _colorRectOffsetBottom * scaleY * _parentScaleY;

		var targetPos = _foundPoint.GlobalPosition + _initialRayGlobalOffset;
		targetPos.Y -= (scaledOffsetBottom - (_colorRectOffsetBottom * _parentScaleY));

		_rayEndPoint.GlobalPosition = targetPos;
	}

	#endregion

	#region 玩家消灭逻辑

	private void KillPlayer(Node playerNode)
	{
		if (_isKillingPlayer) return;
		_isKillingPlayer = true;

		HidePlayer(playerNode);

		if (EnableFlashEffect)
			TriggerFlashEffect(playerNode);

		TrapEventManager.TriggerPlayerReset(playerNode, "RayTrap");

		GetTree().CreateTimer(0.5).Timeout += () => _isKillingPlayer = false;
	}

	private void HidePlayer(Node playerNode)
	{
		try
		{
			var canvasItem = playerNode as CanvasItem;
			if (canvasItem != null)
				canvasItem.Visible = false;
			else
				playerNode.ProcessMode = ProcessModeEnum.Disabled;

			var characterBody = FindCharacterBody(playerNode);
			if (characterBody != null)
			{
				characterBody.Visible = false;
				var collisionShape = characterBody.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
				if (collisionShape != null)
					CallDeferred(MethodName.DeferredDisableCollisionShape, collisionShape);
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[RayTrap] ❌ 隐藏玩家异常: {ex.Message}");
		}
	}

	private static void DeferredDisableCollisionShape(CollisionShape2D collisionShape)
	{
		if (!IsInstanceValid(collisionShape)) return;
		collisionShape.Disabled = true;
	}

	private void TriggerFlashEffect(Node playerNode)
	{
		CanvasItem? sprite = playerNode.GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite == null)
			sprite = playerNode.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		if (sprite == null) return;

		var originalModulate = sprite.Modulate;
		var flashTimer = GetTree().CreateTimer(FlashDuration);
		var flashInterval = FlashDuration / (FlashCount * 2);
		var isRed = false;

		void UpdateFlash()
		{
			isRed = !isRed;
			sprite.Modulate = isRed ? Colors.Red : originalModulate;

			if (isRed)
				GetTree().CreateTimer(flashInterval).Timeout += UpdateFlash;
		}

		UpdateFlash();
		flashTimer.Timeout += () => sprite.Modulate = originalModulate;
	}

	#endregion

	#region 辅助方法

	private bool IsPlayerBody(Node body)
	{
		if (body == null) return false;

		return body.Name.ToString().Equals("Player", StringComparison.OrdinalIgnoreCase) ||
			   body.Name.ToString().Equals("player", StringComparison.OrdinalIgnoreCase) ||
			   body.GetParent()?.Name.ToString().Equals("Player", StringComparison.OrdinalIgnoreCase) == true ||
			   body.GetParent()?.Name.ToString().Equals("player", StringComparison.OrdinalIgnoreCase) == true;
	}

	private static CharacterBody2D? FindCharacterBody(Node node)
	{
		if (node is CharacterBody2D body) return body;

		return node.GetNodeOrNull<CharacterBody2D>("CharacterBody2D") ??
			   node.GetNodeOrNull<CharacterBody2D>("character_body_2d");
	}

	#endregion
}
