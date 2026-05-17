namespace GFrameworkGodotTemplate.scripts.player.interfaces;

/// <summary>
///     玩家攀爬输入处理器接口
///     <para>
///         负责处理攀爬状态下的输入映射，与正常移动状态的输入分离。
///     </para>
/// </summary>
public interface IPlayerClimbInputHandler
{
	/// <summary>
	///     垂直攀爬输入 (-1=向上, 0=停止, 1=向下)
	/// </summary>
	float VerticalClimbInput { get; }

	/// <summary>
	///     水平方向输入（用于跳跃脱离时的方向选择）
	/// </summary>
	float HorizontalDirection { get; }

	/// <summary>
	///     跳跃脱离键是否按下
	/// </summary>
	bool IsJumpOffPressed { get; }

	/// <summary>
	///     是否有攀爬输入触发（用于从正常状态切换到攀爬状态）
	/// </summary>
	bool HasClimbTriggerInput { get; }

	/// <summary>
	///     更新攀爬输入状态
	/// </summary>
	void UpdateClimbInput();
}
