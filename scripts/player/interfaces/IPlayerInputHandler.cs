namespace GFrameworkGodotTemplate.scripts.player.interfaces;

/// <summary>
///     玩家输入处理器接口
///     定义玩家角色输入读取的标准契约，遵循单一职责原则
///     负责将原始按键输入转换为标准化的移动意图数据
/// </summary>
public interface IPlayerInputHandler
{
	/// <summary>
	///     获取水平方向的输入值
	///     范围: [-1.0, 1.0], 负数向左, 正数向右, 0为无输入
	/// </summary>
	float HorizontalDirection { get; }

	/// <summary>
	///     检测是否按下跳跃键
	///     在调用后自动重置为false(单次触发)
	/// </summary>
	bool IsJumpPressed { get; }

	/// <summary>
	///     更新输入状态
	///     应在每帧物理更新时调用一次以刷新输入缓存
	/// </summary>
	void UpdateInput();
}
