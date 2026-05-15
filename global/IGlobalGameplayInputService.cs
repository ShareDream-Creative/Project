using GFrameworkGodotTemplate.scripts.enums;

namespace GFrameworkGodotTemplate.global;

/// <summary>
///     全局游戏玩法输入服务接口
///     定义游戏中角色移动相关的全局输入状态查询契约
///     
///     职责范围:
///     - 提供标准化的方向和动作输入查询
///     - 支持键盘、手柄等多设备输入源
///     - 统一输入映射策略，确保全项目一致性
///     - 基于游戏状态（LevelPhase）的输入阻塞控制
///     
///     设计原则:
///     - 单例服务: 全局唯一实例，所有 Gameplay 组件共享同一输入状态
///     - 状态缓存: 每帧更新一次，避免多次查询 Godot Input API
///     - 接口隔离: 仅暴露必要的输入查询方法，隐藏实现细节
///     - 状态感知: 自动根据当前关卡阶段控制输入可用性
///     
///     架构增强(v2.1):
///     - 新增输入阻塞机制: 基于 LevelPhase 枚举自动禁用/启用输入
///     - 统一入口: 所有 Gameplay 组件通过此接口获取受控的输入状态
///     - 双重保障: 与 PlayerStateController 形成双重输入验证
/// </summary>
public interface IGlobalGameplayInputService
{
	/// <summary>
	///     获取水平方向的输入值
	///     范围: [-1.0, 1.0], 负数向左, 正数向右, 0为无输入
	///     支持的按键: A/D键、左/右箭头键、手柄左摇杆
	///     <para>
	///         当输入被禁用时(非Play阶段)，始终返回0
	///     </para>
	/// </summary>
	float HorizontalDirection { get; }

	/// <summary>
	///     检测是否按下跳跃键(单次触发)
	///     支持的按键: Space空格键、ui_accept动作、手柄A按钮
	///     在调用后会自动重置，直到下次按下才返回true
	///     <para>
	///         当输入被禁用时(非Play阶段)，始终返回false
	///     </para>
	/// </summary>
	bool IsJumpPressed { get; }

	/// <summary>
	///     检测是否按下交互键(单次触发)
	///     支持的按键: E键、ui_interact动作(如果配置)
	///     用于与场景中的交互对象进行互动（按钮、开关、NPC对话等）
	///     在调用后会自动重置，直到下次按下才返回true
	///     <para>
	///         当输入被禁用时(非Play阶段)，始终返回false
	///     </para>
	/// </summary>
	bool IsInteractPressed { get; }

	/// <summary>
	///     获取当前输入是否可用
	///     <para>
	///         基于当前关卡阶段(LevelPhase)判断:
	///         - Play阶段: 返回true (允许完整输入)
	///         - 其他阶段(Build/Success/Failure): 返回false (禁止游戏玩法输入)
	///         
	///         用途:
	///         供外部组件快速判断是否应该处理用户输入
	///         可用于UI显示提示信息或调试日志
	///     </para>
	/// </summary>
	bool IsInputEnabled { get; }

	/// <summary>
	///     获取当前的关卡阶段状态
	///     <para>
	///         用于内部判断输入是否应该被阻塞
	///         由外部组件(BaseLevelController)负责同步更新
	///     </para>
	/// </summary>
	LevelPhase CurrentPhase { get; }

	/// <summary>
	///     设置当前关卡阶段状态
	///     <param name="phase">新的关卡阶段</param>
	///     <remarks>
	///         应由 BaseLevelController 在阶段切换时调用
	///         调用后会立即影响后续的输入检测结果:
	///         - Phase=Play: 输入正常工作
	///         - Phase≠Play: 所有输入返回默认值(方向=0, 动作=false)
	///     </remarks>
	/// </summary>
	void SetCurrentPhase(LevelPhase phase);

	/// <summary>
	///     更新全局输入状态缓存
	///     应由全局控制器每帧调用一次，确保输入数据同步
	///     <para>
	///         当输入被禁用时(非Play阶段):
	///         - HorizontalDirection = 0
	///         - IsJumpPressed = false
	///         - IsInteractPressed = false
	///     </para>
	/// </summary>
	void UpdateInputState();
}
