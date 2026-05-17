using Godot;
using GFrameworkGodotTemplate.scripts.world.interfaces;

namespace GFrameworkGodotTemplate.scripts.world;

/// <summary>
///     梯子攀爬控制器（v3.1 - 自动检测版）
///     <para>
///         检测玩家进入/离开，并自动找到玩家控制器调用方法，无需编辑器配置！
///     </para>
/// </summary>
public partial class LadderClimbController : Node2D, ILadderClimbable
{
	#region 导出参数

	/// <summary>攀爬速度（像素/秒）</summary>
	[Export]
	public float ClimbSpeed { get; set; } = 120f;

	/// <summary>是否允许从顶部进入攀爬</summary>
	[Export]
	public bool AllowTopEntry { get; set; } = false;

	#endregion

	#region 私有字段

	/// <summary>梯子唯一标识符</summary>
	private string _ladderId = string.Empty;

	#endregion

	#region 节点引用

	private Area2D? _detectionArea;
	private CollisionShape2D? _detectCollisionShape;

	#endregion

	#region ILadderClimbable 接口实现

	public string LadderId => _ladderId;

	public Rect2 GetGlobalBounds()
	{
		if (_detectionArea == null || !GodotObject.IsInstanceValid(_detectionArea))
		{
			return new Rect2(GlobalPosition, new Vector2(20f, 294f));
		}

		Vector2 areaGlobalPos = _detectionArea.GlobalPosition;
		Vector2 shapeSize = GetCollisionShapeSize();

		return new Rect2(areaGlobalPos - shapeSize / 2f, shapeSize);
	}

	#endregion

	#region 生命周期方法

	public override void _Ready()
	{
		try
		{
			_ladderId = $"{Name}_{GetInstanceId()}";
			InitializeComponents();
			SetupSignalConnections();

			GD.Print($"[LadderClimbController] ✅ 梯子初始化完成 (ID: {_ladderId}) - 无需编辑器配置！");
			GD.Print($"[LadderClimbController] 攀爬速度: {ClimbSpeed} px/s");
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[LadderClimbController] ❌ 初始化异常: {ex.Message}");
		}
	}

	#endregion

	#region 初始化方法

	private void InitializeComponents()
	{
		_detectionArea = GetNodeOrNull<Area2D>("Area2D");

		if (_detectionArea == null)
		{
			GD.PrintErr("[LadderClimbController] ❌ 未找到必需子节点 'Area2D'！");
			return;
		}

		_detectCollisionShape = _detectionArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

		if (_detectCollisionShape == null)
		{
			GD.Print("[LadderClimbController] ⚠️ 未找到 CollisionShape2D，将使用默认尺寸");
		}

		GD.Print("[LadderClimbController] ✓ 组件初始化完成");
	}

	private void SetupSignalConnections()
	{
		if (_detectionArea == null) return;

		_detectionArea.BodyEntered += OnDetectionAreaBodyEntered;
		_detectionArea.BodyExited += OnDetectionAreaBodyExited;
		GD.Print("[LadderClimbController] ✓ 检测信号已连接");
	}

	/// <summary>
	///     获取碰撞形状的尺寸
	/// </summary>
	private Vector2 GetCollisionShapeSize()
	{
		if (_detectCollisionShape != null && GodotObject.IsInstanceValid(_detectCollisionShape))
		{
			if (_detectCollisionShape.Shape is RectangleShape2D rect)
			{
				return rect.Size;
			}
		}

		return new Vector2(20f, 294f);
	}

	#endregion

	#region 区域检测事件（自动找到玩家并调用）

	/// <summary>
	///     标记玩家是否正在攀爬（防止进入/离开循环）
	/// </summary>
	private bool _playerIsClimbing = false;

	private void OnDetectionAreaBodyEntered(Node body)
	{
		if (_playerIsClimbing) return; // 玩家正在攀爬，忽略

		var playerController = FindPlayerMovementController(body);
		if (playerController == null) return;

		GD.Print($"[LadderClimbController] 🪜 玩家进入梯子区域 (ID: {_ladderId})");
		
		// 直接调用玩家的方法
		playerController.Call("OnPlayerEnteredLadder", this);
	}

	private void OnDetectionAreaBodyExited(Node body)
	{
		if (_playerIsClimbing) return; // 玩家正在攀爬，忽略

		var playerController = FindPlayerMovementController(body);
		if (playerController == null) return;

		GD.Print($"[LadderClimbController] 👤 玩家离开梯子区域 (ID: {_ladderId})");
		
		// 直接调用玩家的方法
		playerController.Call("OnPlayerExitedLadder");
	}

	/// <summary>
	///     由玩家调用，标记开始攀爬
	/// </summary>
	public void OnStartClimbing()
	{
		_playerIsClimbing = true;
	}

	/// <summary>
	///     由玩家调用，标记结束攀爬
	/// </summary>
	public void OnEndClimbing()
	{
		_playerIsClimbing = false;
	}

	/// <summary>
	///     从碰撞体中找到 PlayerMovementController
	/// </summary>
	private Node? FindPlayerMovementController(Node body)
	{
		// 方法1：检查当前节点是否就是玩家
		if (body.HasMethod("OnPlayerEnteredLadder"))
		{
			return body;
		}

		// 方法2：向上遍历父节点查找
		var current = body.GetParentOrNull<Node>();
		while (current != null)
		{
			if (current.HasMethod("OnPlayerEnteredLadder"))
			{
				return current;
			}

			// 检查子节点（因为 Player 在 Node2D 下面有 CharacterBody2D）
			foreach (Node child in current.GetChildren())
			{
				if (child.HasMethod("OnPlayerEnteredLadder"))
				{
					return child;
				}
			}

			current = current.GetParentOrNull<Node>();
		}

		return null;
	}

	#endregion
}
