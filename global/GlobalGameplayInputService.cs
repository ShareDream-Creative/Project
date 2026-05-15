using Godot;
using GFrameworkGodotTemplate.scripts.enums;

namespace GFrameworkGodotTemplate.global;

/// <summary>
///     全局游戏玩法输入服务实现
///     负责统一处理游戏中角色移动相关的全局输入检测
///     
///     架构定位:
///     - 属于 Global 层: 作为全局单例服务运行
///     - 输入源抽象层: 屏蔽底层 Godot Input API 差异
///     - 状态缓存中心: 避免多个组件重复查询 Input 系统
///     - 输入阻塞网关: 基于关卡阶段统一控制输入可用性
///     
///     输入映射策略(双策略模式):
///     策略1 - Input Map优先:
///       使用 Godot 的 ui_left/ui_right/ui_accept 动作
///       支持用户在编辑器中自定义键位绑定
///       兼容手柄和键盘的统一配置
///       
///     策略2 - 直接键盘后备:
///       当 Input Map 未配置或返回0时激活
///       检测 A/D 键、方向键、空格键等常用按键
///       确保"开箱即用"无需额外配置
///     
///     输入阻塞机制(v2.1):
///     基于 LevelPhase 枚举实现自动输入控制:
///     - Play阶段: 正常检测所有输入（方向、跳跃、交互）
///     - Build阶段: 阻塞所有游戏玩法输入（返回默认值）
///     - Success阶段: 阻塞所有游戏玩法输入
///     - Failure阶段: 阻塞所有游戏玩法输入
///     
///     与框架集成:
///     - 可由 GlobalInputController 调用 UpdateInputState()
///     - 或作为 AutoLoad 单例自动更新
///     - 所有 Gameplay 组件通过 IGlobalGameplayInputService 接口访问
///     - 由 BaseLevelController 通过 SetCurrentPhase() 同步关卡状态
/// </summary>
public class GlobalGameplayInputService : IGlobalGameplayInputService
{
	#region 输入状态缓存

	private float _horizontalDirection;

	private bool _jumpPressed;

	private bool _interactPressed;

	#endregion

	#region 关卡阶段状态

	/// <summary>当前关卡阶段（默认为Build，表示输入被阻塞）</summary>
	private LevelPhase _currentPhase = LevelPhase.Build;

	#endregion

	#region 公共属性实现

	/// <inheritdoc />
	public float HorizontalDirection => _horizontalDirection;

	/// <inheritdoc />
	public bool IsJumpPressed => _jumpPressed;

	/// <inheritdoc />
	public bool IsInteractPressed => _interactPressed;

	/// <inheritdoc />
	public bool IsInputEnabled => _currentPhase == LevelPhase.Play;

	/// <inheritdoc />
	public LevelPhase CurrentPhase => _currentPhase;

	#endregion

	#region 公共方法

	/// <inheritdoc />
	public void SetCurrentPhase(LevelPhase phase)
	{
		if (_currentPhase == phase)
		{
			return;
		}

		var oldPhase = _currentPhase;
		_currentPhase = phase;
		
		var isEnabled = IsInputEnabled;
		
		if (isEnabled)
		{
			GD.Print($"[GlobalGameplayInputService] ✅ 输入已启用 | 阶段: {oldPhase} → {phase}");
		}
		else
		{
			GD.Print($"[GlobalGameplayInputService] 🚫 输入已禁用 | 阶段: {oldPhase} → {phase}");
		}
	}

	/// <inheritdoc />
	public void UpdateInputState()
	{
		if (!IsInputEnabled)
		{
			_horizontalDirection = 0f;
			_jumpPressed = false;
			_interactPressed = false;
			return;
		}

		_horizontalDirection = DetectHorizontalInput();
		_jumpPressed = DetectJumpInput();
		_interactPressed = DetectInteractInput();
	}

	#endregion

	#region 私有方法 - 输入检测逻辑

	/// <summary>
	///     检测水平方向输入
	///     采用双策略: Input Map + 直接键盘检测
	/// </summary>
	/// <returns>方向值 [-1.0, 1.0]</returns>
	private float DetectHorizontalInput()
	{
		float axisValue = Input.GetAxis("ui_left", "ui_right");
		
		if (axisValue == 0)
		{
			bool leftPressed = Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left);
			bool rightPressed = Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right);
			
			if (leftPressed && !rightPressed)
			{
				axisValue = -1.0f;
			}
			else if (rightPressed && !leftPressed)
			{
				axisValue = 1.0f;
			}
		}
		
		return axisValue;
	}

	/// <summary>
	///     检测跳跃输入(单次触发)
	///     支持 ui_accept 动作和空格键
	/// </summary>
	/// <returns>是否按下跳跃键</returns>
	private bool DetectJumpInput()
	{
		if (Input.IsActionJustPressed("ui_accept"))
		{
			return true;
		}
		
		if (Input.IsKeyPressed(Key.Space))
		{
			return true;
		}
		
		return false;
	}

	/// <summary>
	///     检测交互输入(单次触发)
	///     采用双策略: Input Map + 直接键盘检测
	///     优先使用 ui_interact 动作（如果配置），后备使用 E 键
	///     
	///     注意: 使用 InputMap.HasAction() 先检查动作是否存在，避免 Godot 输出错误日志
	/// </summary>
	/// <returns>是否按下交互键</returns>
	private bool DetectInteractInput()
	{
		if (InputMap.HasAction("ui_interact") && Input.IsActionJustPressed("ui_interact"))
		{
			return true;
		}
		
		if (Input.IsKeyPressed(Key.E))
		{
			return true;
		}
		
		return false;
	}

	#endregion
}
