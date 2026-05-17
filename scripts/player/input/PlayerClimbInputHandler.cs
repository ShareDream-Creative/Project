using Godot;
using GFrameworkGodotTemplate.global;
using GFrameworkGodotTemplate.scripts.player.interfaces;

namespace GFrameworkGodotTemplate.scripts.player.input;

/// <summary>
///     玩家攀爬输入处理器实现
///     <para>
///         处理攀爬状态下的输入映射，与正常移动状态的输入分离。
///     </para>
/// </summary>
public class PlayerClimbInputHandler : IPlayerClimbInputHandler
{
	#region 私有字段

	private readonly IGlobalGameplayInputService _globalInputService;
	private float _verticalClimbInput;
	private float _horizontalDirection;
	private bool _isJumpOffPressed;
	private bool _hasClimbTriggerInput;
	private bool _isJumpOffPressedLastFrame;

	#endregion

	#region 构造函数

	/// <summary>
	///     创建玩家攀爬输入处理器实例
	///     <para>
	///         需要注入全局游戏玩法输入服务
	///     </para>
	/// </summary>
	public PlayerClimbInputHandler(IGlobalGameplayInputService globalInputService)
	{
		_globalInputService = globalInputService;
	}

	#endregion

	#region IPlayerClimbInputHandler 接口实现

	public float VerticalClimbInput => _verticalClimbInput;

	public float HorizontalDirection => _horizontalDirection;

	public bool IsJumpOffPressed => _isJumpOffPressed;

	public bool HasClimbTriggerInput => _hasClimbTriggerInput;

	#endregion

	#region 公开方法

	public void UpdateClimbInput()
	{
		// 垂直攀爬输入：A键=向上(-1)，D键=向下(+1)
		bool aPressed = Input.IsKeyPressed(Key.A);
		bool dPressed = Input.IsKeyPressed(Key.D);

		if (aPressed && !dPressed)
		_verticalClimbInput = -1f;
		else if (dPressed && !aPressed)
		_verticalClimbInput = 1f;
		else
		_verticalClimbInput = 0f;

		// 水平方向输入（用于跳跃脱离）
		bool leftPressed = Input.IsKeyPressed(Key.Left) || aPressed;
		bool rightPressed = Input.IsKeyPressed(Key.Right) || dPressed;

		if (leftPressed && !rightPressed)
		_horizontalDirection = -1f;
		else if (rightPressed && !leftPressed)
		_horizontalDirection = 1f;
		else
		_horizontalDirection = 0f;

		// 跳跃脱离键（单次触发检测
		bool spacePressed = Input.IsKeyPressed(Key.Space) || Input.IsActionPressed("ui_accept");
		_isJumpOffPressed = spacePressed && !_isJumpOffPressedLastFrame;
		_isJumpOffPressedLastFrame = spacePressed;

		// 攀爬触发输入检测
		_hasClimbTriggerInput = aPressed || dPressed;
	}

	#endregion
}
