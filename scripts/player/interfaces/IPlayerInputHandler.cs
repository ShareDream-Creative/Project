namespace GFrameworkGodotTemplate.scripts.player.interfaces;

/// <summary>
///     玩家输入处理器接口
///     <para>
///         定义玩家角色输入读取的标准契约，遵循单一职责原则(SRP)
///         负责将原始按键输入转换为标准化的移动意图数据
///     </para>
///     <author>AI Assistant</author>
///     <version>2.1.0</version>
///     <date>2026-05-11</date>
/// <description>
    ///         核心职责:
    ///         1. 输入标准化: 将原始按键转换为方向、跳跃等意图数据
    ///         2. 状态管理: 维护奔跑状态等临时输入状态
    ///         3. 数据缓存: 缓存从PlayerData同步的配置参数
    ///         4. 帧更新: 提供每帧刷新输入状态的入口点
    ///         5. 交互检测: 支持交互键(E键)的输入状态查询
    ///         
    ///         架构增强(v2.0):
    ///         - 新增奔跑状态支持，配合PlayerData.SprintMultiplier使用
    ///         - 支持数据驱动的速度计算
    ///         - 实现IPlayerDataListener接口实现自动数据同步
    ///         
    ///         架构增强(v2.1):
    ///         - 新增IsInteractPressed属性支持交互键(E键)
    ///         - 委托给IGlobalGameplayInputService.IsInteractPressed
    ///         - 统一通过GlobalGameplayInputService进行所有输入检测
    ///     </description>
///     <remarks>
///         设计原则:
///         - 单一职责(SRP): 只负责输入数据的读取和转换
///         - 接口隔离(ISP): 提供最小化的方法集合
///         - 依赖倒置(DIP): 依赖抽象的IGlobalGameplayInputService
///         
///         数据流向:
///         GlobalGameplayInputService → PlayerInputHandler (适配) → PlayerMovementController
///         PlayerDataManager.PlayerData → (监听器) → PlayerInputHandler.SprintMultiplier缓存
///         
///         实现要求:
///         - 必须实现IPlayerDataListener接口以支持数据自动同步
///         - 必须通过构造函数注入IGlobalGameplayInputService
///         - UpdateInput()应在每帧物理更新时调用一次
///         
///         使用示例:
        ///         <code>
        ///         // 在物理更新循环中使用
        ///         inputHandler.UpdateInput();
        ///         
        ///         // 读取输入状态
        ///         float direction = inputHandler.HorizontalDirection;
        ///         bool jump = inputHandler.IsJumpPressed;
        ///         bool sprint = inputHandler.IsSprinting;
        ///         bool interact = inputHandler.IsInteractPressed;
        ///         
        ///         // 获取缓存的配置参数
        ///         float sprintMultiplier = inputHandler.CachedSprintMultiplier;
        ///         
        ///         // 检测交互输入
        ///         if (interact)
        ///         {
        ///             // 执行交互逻辑
        ///         }
        ///         </code>
///     </remarks>
/// </summary>
public interface IPlayerInputHandler
{
	/// <summary>
	///     获取水平方向的输入值
	///     <para>
	///         范围: [-1.0, 1.0]
	///         - 负数表示向左移动
	///         - 正数表示向右移动
	///         - 0表示无水平输入
	///     </para>
	///     <remarks>
	///         数据来源:
	///         从IGlobalGameplayInputService.HorizontalDirection获取
	///         该值已经过标准化处理，确保在有效范围内
	///     </remarks>
	/// </summary>
	float HorizontalDirection { get; }

	/// <summary>
	///     检测是否按下跳跃键
	///     <para>
	///         单次触发模式: 在调用后自动重置为false
	///         避免在一帧内重复触发多次跳跃
	 ///     </para>
	///     <remarks>
	///         数据来源:
	///         从IGlobalGameplayInputService.IsJumpPressed获取
	///         使用单次触发模式防止连跳
	         
	///         使用场景:
	///         - 在PhysicsProcess中检查此属性
	///         - 配合TryJump()方法使用
	 ///     </remarks>
	/// </summary>
	bool IsJumpPressed { get; }

	/// <summary>
	///     检测是否按下交互键(E键)
	///     <para>
	///         单次触发模式: 在调用后自动重置为false
	///         用于与场景中的交互对象进行互动（按钮、开关、NPC对话等）
	 ///     </para>
	///     <remarks>
	///         数据来源:
	///         从IGlobalGameplayInputService.IsInteractPressed获取
	///         支持E键和ui_interact动作(如果配置)
	///         
	///         使用场景:
	///         - 与场景中的可交互对象进行互动
	///         - 触发按钮、开关等机制
	///         - 与NPC对话等交互行为
	 ///     </remarks>
	/// </summary>
	bool IsInteractPressed { get; }

	/// <summary>
	///     检测是否处于奔跑状态(按住Shift等加速键)
	///     <para>
	///         当返回true时，物理模块应应用SprintMultiplier倍率
	///     </para>
	///     <remarks>
	///         触发条件:
	///         - 按住左Shift键或右Shift键
	///         - 可扩展支持手柄输入(如左摇杆按下)
	///         
	///         数据流向:
	///         PlayerInputHandler.IsSprinting → PlayerPhysicsMovement.UpdateHorizontalVelocity()
	///         → Speed * (IsSprinting ? SprintMultiplier : 1.0)
	///         
	///         使用示例:
	///         <code>
	///         if (inputHandler.IsSprinting)
	///         {
	///             actualSpeed = baseSpeed * inputHandler.CachedSprintMultiplier;
	///         }
	///         </code>
	///     </remarks>
	/// </summary>
	bool IsSprinting { get; }

	/// <summary>
	///     获取当前缓存的奔跑速度倍率
	///     <para>
	///         该值从PlayerData.SprintMultiplier同步而来
	///         通过IPlayerDataListener接口自动更新
	///     </para>
	///     <remarks>
	///         同步机制:
	///         1. PlayerDataManager初始化时注册监听器
	///         2. 当PlayerData.SprintMultiplier变更时触发OnSprintMultiplierChanged
	///         3. 监听器实现更新本地缓存值
	///         4. 后续通过此属性访问最新值
	///         
	///         默认值: PlayerData.DEFAULT_SPRINT_MULTIPLIER (1.5)
	///         取值范围: [PlayerData.MIN_SPRINT_MULTIPLIER, PlayerData.MAX_SPRINT_MULTIPLIER] = [1.0, 3.0]
	///         
	///         使用场景:
	///         - 物理模块计算实际奔跑速度时使用
	///         - UI显示当前倍率时使用
	///         - 调试和日志记录时使用
	///     </remarks>
	/// </summary>
	float CachedSprintMultiplier { get; }

	/// <summary>
	///     更新输入状态
	///     <para>
	///         应在每帧物理更新时调用一次以刷新输入缓存
	///     </para>
	///     <remarks>
	///         执行操作:
	///         1. 委托给GlobalInputController._Input()刷新全局输入
	///         2. 检测奔跑键状态(Shift)并更新IsSprinting
	///         3. 重置单次触发的输入标志(如跳跃)
	///         
	///         调用时机:
	///         - 在CharacterBody2D._PhysicsProcess()中调用
	///         - 每帧调用一次，不应多次调用
	///         
	///         注意事项:
	///         - 必须在主线程调用(Godot Input API限制)
	///         - 应在读取其他属性之前调用
	///     </remarks>
	/// </summary>
	void UpdateInput();
}
