using Godot;
using GFrameworkGodotTemplate.scripts.world.interfaces;

namespace GFrameworkGodotTemplate.scripts.world;

/// <summary>
///     攀爬状态管理器实现
/// </summary>
public class LadderClimbState : ILadderClimbState
{
	#region 私有字段

	private bool _isClimbing;
	private ILadderClimbable? _currentLadder;
	private Vector2 _targetAlignedPosition;
	private bool _aligningToLadder;

	#endregion

	#region 属性

	public bool IsClimbing => _isClimbing;
	public ILadderClimbable? CurrentLadder => _currentLadder;

	#endregion

	#region 构造函数

	public LadderClimbState()
	{
		_isClimbing = false;
		_currentLadder = null;
		_aligningToLadder = false;
	}

	#endregion

	#region 公开方法

	public void StartClimbing(ILadderClimbable ladder, Vector2 playerPosition)
	{
		if (ladder == null)
		{
			GD.PrintErr("[LadderClimbState] StartClimbing: ladder cannot be null");
			return;
		}

		_currentLadder = ladder;
		_isClimbing = true;
		_aligningToLadder = true;
		_targetAlignedPosition = GetAlignedPosition(playerPosition);

		GD.Print($"[LadderClimbState] Started climbing ladder: {ladder.LadderId}");
	}

	public void StopClimbing()
	{
		_isClimbing = false;
		_currentLadder = null;
		_aligningToLadder = false;

		GD.Print("[LadderClimbState] Stopped climbing");
	}

	public void JumpOffLadder(float horizontalDirection, float jumpForce)
	{
		if (!_isClimbing)
		{
			return;
		}

		StopClimbing();

		GD.Print($"[LadderClimbState] Jumped off ladder with horizontal direction: {horizontalDirection}");
	}

	public bool CheckAutoDetach(Vector2 playerPosition, float verticalInput)
	{
		if (!_isClimbing || _currentLadder == null)
		{
			return false;
		}

		var bounds = _currentLadder.GetGlobalBounds();

		// 检查底部脱离
		if (playerPosition.Y >= bounds.End.Y && verticalInput > 0.1f)
		{
			GD.Print("[LadderClimbState] Auto-detach from bottom");
			StopClimbing();
			return true;
		}

		// 检查顶部脱离
		if (playerPosition.Y <= bounds.Position.Y && verticalInput < -0.1f)
		{
			// 如果允许顶部进入，允许继续向上攀爬，否则自动脱离
			if (!_currentLadder.AllowTopEntry)
			{
				GD.Print("[LadderClimbState] Auto-detach from top");
				StopClimbing();
				return true;
			}
		}

		return false;
	}

	public Vector2 GetClimbVelocity(float verticalInput)
	{
		if (!_isClimbing || _currentLadder == null)
		{
			return Vector2.Zero;
		}

		// 攀爬时水平速度为0，垂直速度根据输入和攀爬速度
		return new Vector2(0f, verticalInput * _currentLadder.ClimbSpeed);
	}

	public Vector2 GetAlignedPosition(Vector2 playerPosition)
	{
		if (_currentLadder == null)
		{
			return playerPosition;
		}

		var bounds = _currentLadder.GetGlobalBounds();

		// 将玩家水平位置对齐到梯子中心，垂直位置保持不变
		return new Vector2(bounds.Position.X + bounds.Size.X / 2f, playerPosition.Y);
	}

	#endregion
}
