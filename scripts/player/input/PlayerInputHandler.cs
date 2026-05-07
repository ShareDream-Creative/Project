using GFrameworkGodotTemplate.global;
using GFrameworkGodotTemplate.scripts.player.interfaces;

namespace GFrameworkGodotTemplate.scripts.player.input;

/// <summary>
///     玩家输入处理器实现(委托模式)
///     负责将全局输入服务的数据转换为玩家特定的输入意图
///     
///     架构重构说明:
///     - 原始版本: 直接调用 Godot Input API (已迁移至 GlobalGameplayInputService)
///     - 当前版本: 通过 IGlobalGameplayInputService 接口获取输入数据
///     - 职责转变: 从"输入检测器"变为"输入数据适配器"
///     
///     设计优势:
///     1. 解耦: Player 模块不再直接依赖 Godot Input 系统
///     2. 可测试: 可注入 Mock 输入服务进行单元测试
///     3. 统一: 所有 Gameplay 组件共享同一输入数据源
///     4. 扩展: 未来可轻松支持 AI 输入、网络同步等
/// </summary>
public class PlayerInputHandler : IPlayerInputHandler
{
	#region 全局输入服务引用

	private readonly IGlobalGameplayInputService _globalInputService;

	#endregion

	#region 构造函数

	/// <summary>
	///     创建玩家输入处理器实例
	///     需要注入全局游戏玩法输入服务
	/// </summary>
	/// <param name="globalInputService">全局输入服务实例</param>
	public PlayerInputHandler(IGlobalGameplayInputService globalInputService)
	{
		_globalInputService = globalInputService ?? throw new ArgumentNullException(nameof(globalInputService));
	}

	#endregion

	#region 接口实现

	/// <inheritdoc />
	/// <remarks>
	///     从全局输入服务获取水平方向数据
	///     数据来源: GlobalGameplayInputService.DetectHorizontalInput()
	/// </remarks>
	public float HorizontalDirection => _globalInputService.HorizontalDirection;

	/// <inheritdoc />
	/// <remarks>
	///     从全局输入服务获取跳跃按键状态
	///     数据来源: GlobalGameplayInputService.DetectJumpInput()
	/// </remarks>
	public bool IsJumpPressed => _globalInputService.IsJumpPressed;

	/// <inheritdoc />
	/// <remarks>
	///     更新操作已委托给 GlobalInputController._Input()
	///     此方法保留接口兼容性，实际为空操作
	/// </remarks>
	public void UpdateInput()
	{
	}

	#endregion
}
