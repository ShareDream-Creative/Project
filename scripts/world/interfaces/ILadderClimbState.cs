using Godot;

namespace GFrameworkGodotTemplate.scripts.world.interfaces;

/// <summary>
///     攀爬状态管理器接口
/// </summary>
public interface ILadderClimbState
{
	/// <summary>
	///     是否处于攀爬状态
	/// </summary>
	bool IsClimbing { get; }
	
	/// <summary>
	///     当前正在攀爬的梯子
	/// </summary>
	ILadderClimbable? CurrentLadder { get; }
	
	/// <summary>
	///     开始攀爬
	/// </summary>
	/// <param name="ladder">要攀爬的梯子</param>
	/// <param name="playerPosition">玩家位置</param>
	void StartClimbing(ILadderClimbable ladder, Vector2 playerPosition);
	
	/// <summary>
	///     停止攀爬
	/// </summary>
	void StopClimbing();
	
	/// <summary>
	///     跳跃脱离梯子
	/// </summary>
	/// <param name="horizontalDirection">水平方向（-1=左，0=垂直，1=右）</param>
	/// <param name="jumpForce">跳跃力度</param>
	void JumpOffLadder(float horizontalDirection, float jumpForce);
	
	/// <summary>
	///     检查是否需要自动脱离梯子
	/// </summary>
	/// <param name="playerPosition">玩家位置</param>
	/// <param name="verticalInput">垂直输入</param>
	bool CheckAutoDetach(Vector2 playerPosition, float verticalInput);
	
	/// <summary>
	///     获取攀爬时的移动速度
	/// </summary>
	/// <param name="verticalInput">垂直输入</param>
	Vector2 GetClimbVelocity(float verticalInput);
	
	/// <summary>
	///     获取玩家在梯子上的对齐位置
	/// </summary>
	Vector2 GetAlignedPosition(Vector2 playerPosition);
}
