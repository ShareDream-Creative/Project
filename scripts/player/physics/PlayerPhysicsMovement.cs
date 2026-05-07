using GFrameworkGodotTemplate.scripts.player.interfaces;
using Godot;

namespace GFrameworkGodotTemplate.scripts.player.physics;

/// <summary>
///     玩家物理运动实现
///     基于Godot CharacterBody2D的物理移动系统
///     封装重力、速度计算、碰撞响应等核心逻辑
/// </summary>
public class PlayerPhysicsMovement : IPlayerPhysicsMovement
{
	private Vector2 _velocity;

	private bool _isOnFloor;

	/// <inheritdoc />
	public float Speed { get; set; } = 300.0f;

	/// <inheritdoc />
	public float JumpVelocity { get; set; } = -500.0f;

	/// <inheritdoc />
	public float Gravity { get; set; } = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	/// <inheritdoc />
	public Vector2 CurrentVelocity => _velocity;

	/// <inheritdoc />
	public bool IsOnFloor => _isOnFloor;

	/// <inheritdoc />
	public void ApplyGravity(float delta)
	{
		if (!_isOnFloor)
		{
			_velocity.Y += Gravity * delta;
		}
	}

	/// <inheritdoc />
	public void UpdateHorizontalVelocity(float direction)
	{
		if (direction != 0)
		{
			_velocity.X = direction * Speed;
		}
		else
		{
			_velocity.X = Mathf.MoveToward(_velocity.X, 0, Speed);
		}
	}

	/// <inheritdoc />
	public bool TryJump()
	{
		if (_isOnFloor)
		{
			_velocity.Y = JumpVelocity;
			_isOnFloor = false;
			return true;
		}

		return false;
	}

	/// <inheritdoc />
	public void Move(CharacterBody2D body)
	{
		body.Velocity = _velocity;
		body.MoveAndSlide();

		_isOnFloor = body.IsOnFloor();
	}

	/// <inheritdoc />
	public void StopImmediately()
	{
		_velocity = Vector2.Zero;
	}
}
