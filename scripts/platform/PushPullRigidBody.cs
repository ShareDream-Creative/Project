using Godot;

namespace GFrameworkGodotTemplate.scripts.platform;

/// <summary>
///     可推动/拉动物体控制器 v3.0（精简有效版）
///     <para>
///         完全基于 Godot 原生物理引擎实现，代码简洁高效。
///         推动功能：利用 CharacterBody2D 与 RigidBody2D 的原生物理交互。
///         拉动功能：使用 PinJoint2D 物理关节实现自然拉动效果。
///         
///         设计原则：
///         - 纯原生物理：不硬编码位置/速度，完全依赖物理引擎
///         - 极简代码：移除所有无效的复杂检测机制
///         - 零侵入：不修改玩家任何代码
///         
///         节点结构要求：
///         rig (Node2D)
///         └── RigidBody2D ← 挂载本脚本
///             ├── CollisionShape2D
///             └── ColorRect
///     </para>
/// </summary>
public partial class PushPullRigidBody : RigidBody2D
{
	#region 导出参数

	[Export] public bool EnablePush { get; set; } = true;
	[Export] public bool EnablePull { get; set; } = true;
	
	[ExportGroup("物理参数")]
	[Export] public float Mass { get; set; } = 2.0f;
	[Export] public float LinearDamping { get; set; } = 0.8f;
	[Export] public float AngularDamping { get; set; } = 10.0f;
	[Export] public bool FreezeRotation { get; set; } = true;
	
	[ExportGroup("拉动参数")]
	[Export] public float InteractionDistance { get; set; } = 50f;
	[Export] public Key InteractionKey { get; set; } = Key.E;

	#endregion

	#region 私有字段

	private CharacterBody2D? _player;
	private PinJoint2D? _pullJoint;
	private bool _isPulling;

	#endregion

	#region 生命周期

	public override void _Ready()
	{
		InitializePhysics();
		SetupCollisionSignals();
		
		GD.Print($"[PushPullRigidBody] ✅ 初始化完成 | 质量:{Mass}kg | 推动:{EnablePush} | 拉动:{EnablePull}");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isPulling)
		{
			HandlePullLogic();
		}
		
		if (FreezeRotation)
		{
			AngularVelocity = 0;
		}
	}

	#endregion

	#region 初始化

	private void InitializePhysics()
	{
		this.Mass = Mass;
		this.LinearDamping = LinearDamping;
		this.AngularDamping = AngularDamping;
		
		if (FreezeRotation)
		{
			Inertia = 0;
			AngularVelocity = 0;
		}
		
		CollisionLayer = 1;
		CollisionMask = int.MaxValue;
	}

	private void SetupCollisionSignals()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	#endregion

	#region 碰撞检测

	private void OnBodyEntered(Node body)
	{
		if (IsPlayer(body))
		{
			_player = body as CharacterBody2D;
			GD.Print($"[PushPullRigidBody] 🎮 玩家进入: {_player?.Name}");
		}
	}

	private void OnBodyExited(Node body)
	{
		if (_player != null && body.GetInstanceId() == _player.GetInstanceId())
		{
			TerminatePull();
			_player = null;
			GD.Print("[PushPullRigidBody] 🚶 玩家离开");
		}
	}

	private static bool IsPlayer(Node body)
	{
		if (body is CharacterBody2D) return true;
		
		var parent = body.GetParent();
		int depth = 5;
		while (parent != null && depth > 0)
		{
			if (parent.Name.ToString().Equals("Player", System.StringComparison.OrdinalIgnoreCase))
				return true;
			parent = parent.GetParent();
			depth--;
		}
		
		return false;
	}

	#endregion

	#region 拉动功能

	private void HandlePullLogic()
	{
		if (!EnablePull || _player == null || !GodotObject.IsInstanceValid(_player)) return;
		
		bool keyPressed = Input.IsKeyPressed(InteractionKey);
		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
		
		if (keyPressed && distance <= InteractionDistance)
		{
			if (!_isPulling) StartPull();
		}
		else
		{
			if (_isPulling) TerminatePull();
		}
	}

	private void StartPull()
	{
		if (_player == null || !GodotObject.IsInstanceValid(_player)) return;
		
		try
		{
			_isPulling = true;
			
			_pullJoint = new PinJoint2D();
			_pullJoint.NodeA = GetPath();
			_pullJoint.NodeB = _player.GetPath();
			_pullJoint.Softness = 0.1f;
			
			GetTree().CurrentScene.AddChild(_pullJoint);
			
			var originalDamping = LinearDamping;
			LinearDamping = 0.3f;
			
			GD.Print($"[PushPullRigidBody] 🔗 开始拉动 | 关节已创建");
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[PushPullRigidBody] ❌ 开始拉动失败: {ex.Message}");
			_isPulling = false;
			_pullJoint = null;
		}
	}

	private void TerminatePull()
	{
		if (!_isPulling) return;
		
		_isPulling = false;
		LinearDamping = this.LinearDamping;
		
		if (_pullJoint != null && GodotObject.IsInstanceValid(_pullJoint))
		{
			_pullJoint.NodeA = new NodePath();
			_pullJoint.NodeB = new NodePath();
			_pullJoint.QueueFree();
			_pullJoint = null;
		}
		
		GD.Print("[PushPullRigidBody] 🔓 停止拉动");
	}

	#endregion

	#region 清理

	public override void _ExitTree()
	{
		TerminatePull();
		_player = null;
		base._ExitTree();
	}

	#endregion
}
