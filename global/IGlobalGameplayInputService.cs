namespace GFrameworkGodotTemplate.global;

/// <summary>
///     全局游戏玩法输入服务接口
///     定义游戏中角色移动相关的全局输入状态查询契约
///     
///     职责范围:
///     - 提供标准化的方向和动作输入查询
///     - 支持键盘、手柄等多设备输入源
///     - 统一输入映射策略，确保全项目一致性
///     
///     设计原则:
///     - 单例服务: 全局唯一实例，所有 Gameplay 组件共享同一输入状态
///     - 状态缓存: 每帧更新一次，避免多次查询 Godot Input API
///     - 接口隔离: 仅暴露必要的输入查询方法，隐藏实现细节
/// </summary>
public interface IGlobalGameplayInputService
{
	/// <summary>
	///     获取水平方向的输入值
	///     范围: [-1.0, 1.0], 负数向左, 正数向右, 0为无输入
	///     支持的按键: A/D键、左/右箭头键、手柄左摇杆
	/// </summary>
	float HorizontalDirection { get; }

	/// <summary>
	///     检测是否按下跳跃键(单次触发)
	///     支持的按键: Space空格键、ui_accept动作、手柄A按钮
	///     在调用后会自动重置，直到下次按下才返回true
	/// </summary>
	bool IsJumpPressed { get; }

	/// <summary>
	///     更新全局输入状态缓存
	///     应由全局控制器每帧调用一次，确保输入数据同步
	/// </summary>
	void UpdateInputState();
}
