using GFrameworkGodotTemplate.global;
using GFrameworkGodotTemplate.scripts.data.interfaces;
using GFrameworkGodotTemplate.scripts.data.model;
using GFrameworkGodotTemplate.scripts.player.interfaces;
using Godot;

namespace GFrameworkGodotTemplate.scripts.player.input;

/// <summary>
///     玩家输入处理器实现(委托模式)
///     <para>
///         负责将全局输入服务的数据转换为玩家特定的输入意图
///     </para>
///     <author>AI Assistant</author>
///     <version>2.1.0</version>
///     <date>2026-05-11</date>
/// <description>
	///         核心职责:
	///         1. 输入适配: 将IGlobalGameplayInputService数据转换为本模块格式
	///         2. 状态管理: 维护奔跑状态等临时输入状态
	///         3. 数据缓存: 缓存从PlayerData同步的配置参数(SprintMultiplier)
	///         4. 速度计算: 提供考虑奔跑状态的实际速度计算方法
	///         5. 交互检测: 支持交互键(E键)的输入检测和状态查询
	///         
	///         架构重构说明:
	///         - 原始版本: 直接调用 Godot Input API (已迁移至 GlobalGameplayInputService)
	///         - 当前版本: 通过 IGlobalGameplayInputService 接口获取输入数据
	///         - 职责转变: 从"输入检测器"变为"输入数据适配器"
	///         
	///         架构增强(v2.0):
	///         - 实现IPlayerDataListener接口，支持数据自动同步
	///         - 新增奔跑状态检测(Shift键)
	///         - 集成SprintMultiplier，提供实际速度计算
	///         
	///         架构增强(v2.1):
	///         - 新增交互键支持(E键)
	///         - 委托给GlobalGameplayInputService.DetectInteractInput()
	///         - 支持场景对象交互（按钮、开关、NPC对话等）
	///     </description>
///     <remarks>
///         设计优势:
///         1. 解耦: Player 模块不再直接依赖 Godot Input 系统
///         2. 可测试: 可注入 Mock 输入服务进行单元测试
///         3. 统一: 所有 Gameplay 组件共享同一输入数据源
///         4. 扩展: 未来可轻松支持 AI 输入、网络同步等
///         
///         数据流向:
///         GlobalGameplayInputService → PlayerInputHandler (适配) → PlayerMovementController
///         PlayerDataManager.PlayerData → (监听器) → PlayerInputHandler.SprintMultiplier缓存
///         
///         设计模式:
///         - 适配器模式(Adapter): 将GlobalInputService接口适配为IPlayerInputHandler接口
///         - 观察者模式(Observer): 实现IPlayerDataListener自动同步配置参数
///         - 委托模式(Delegate): 将实际的输入检测委托给GlobalGameplayInputService
///         
///         使用示例:
		///         <code>
		///         // 创建实例 (注入全局输入服务)
		///         var inputHandler = new PlayerInputHandler(globalInputService);
		///         
		///         // 在物理更新循环中使用
		///         inputHandler.UpdateInput();
		///         
		///         // 读取输入状态
		///         float direction = inputHandler.HorizontalDirection;
		///         bool jump = inputHandler.IsJumpPressed;
		///         bool sprint = inputHandler.IsSprinting;
		///         bool interact = inputHandler.IsInteractPressed;
		///         
		///         // 计算实际速度
		///         float actualSpeed = inputHandler.CalculateActualSpeed(baseSpeed);
		///         
		///         // 检测交互输入
		///         if (interact)
		///         {
		///             // 执行交互逻辑（如触发按钮、开关等）
		///         }
		///         </code>
///         
///         线程安全性:
///         - UpdateInput()应在主线程调用(Godot Input API限制)
///         - 属性读取是线程安全的(只读操作)
///         - 数据同步通过监听器在主线程完成
///     </remarks>
/// </summary>
public class PlayerInputHandler : IPlayerInputHandler, IPlayerDataListener
{
	#region 全局输入服务引用

	/// <summary>
	///     全局游戏玩法输入服务实例
	///     <para>
	///         负责检测所有 Gameplay 相关的按键输入
	///         通过构造函数注入，支持依赖替换和单元测试
	///     </para>
	///     <remarks>
	///         注入来源:
	///         由PlayerMovementController从GlobalInputController获取并传入
	///         
	///         使用方式:
	///         - HorizontalDirection属性委托给此服务
	///         - IsJumpPressed属性委托给此服务
	///         - 不直接调用Godot Input API
	///         
	///         生命周期:
	///         - 构造时注入，整个对象生命周期内保持引用
	///         - 不负责此服务的创建和销毁
	///     </remarks>
	/// </summary>
	private readonly IGlobalGameplayInputService _globalInputService;

	#endregion

	#region 奔跑状态相关

	/// <summary>
	///     当前帧的奔跑状态标志
	///     <para>
	///         标识玩家是否按住奔跑键(Shift)
	///         每帧通过DetectSprintInput()更新
	///     </para>
	///     <remarks>
	///         更新时机:
	///         在UpdateInput()方法中每帧刷新
	///         
	///         触发条件:
	///         - 按住左Shift键或右Shift键
	///         
	///         使用场景:
	///         - IsSprinting属性返回此值
	///         - 物理模块据此决定是否应用奔跑倍率
	///         
	///         初始值: false (未按下奔跑键)
	///     </remarks>
	/// </summary>
	private bool _isSprinting;

	/// <summary>
	///     从PlayerData缓存的奔跑倍率
	///     <para>
	///         通过IPlayerDataListener接口自动同步更新
	///         当PlayerData.SprintMultiplier变更时自动刷新
	///     </para>
	///     <remarks>
	///         同步机制:
	///         1. PlayerDataManager初始化时注册此实例为监听器
	///         2. 当PlayerData.SprintMultiplier变更时触发OnSprintMultiplierChanged
	///         3. 此回调更新_cachedSprintMultiplier字段
	///         4. CachedSprintMultiplier属性暴露给外部使用
	///         
	///         默认值: PlayerData.DEFAULT_SPRINT_MULTIPLIER (1.5)
	///         取值范围: [PlayerData.MIN_SPRINT_MULTIPLIER, PlayerData.MAX_SPRINT_MULTIPLIER]
	///         
	///         使用场景:
	///         - CalculateActualSpeed()方法中使用此值计算实际速度
	///         - CachedSprintMultiplier属性对外暴露供其他模块使用
	///     </remarks>
	/// </summary>
	private float _cachedSprintMultiplier = PlayerData.DEFAULT_SPRINT_MULTIPLIER;

	#endregion

	#region 构造函数

	/// <summary>
	///     创建玩家输入处理器实例
	///     <para>
	///         需要注入全局游戏玩法输入服务
	///     </para>
	///     <param name="globalInputService">
	///     全局输入服务实例，负责检测Gameplay相关的按键输入
	///     通常从GlobalInputController.GameplayInputService获取
	///     </param>
	///     <exception cref="ArgumentNullException">
	///     当globalInputService为null时抛出
	///     输入服务是必需依赖，不能为空
	///     </exception>
	///     <remarks>
	///         注入方式:
	///         由PlayerMovementController.InitializeModules()中创建并注入
	///         
	///         依赖要求:
	///         - 必须实现IGlobalGameplayInputService接口
	///         - 不能为null（强制非空检查）
	///         - 应在整个生命周期内有效
	///         
	///         初始化状态:
	///         - _isSprinting = false (默认不奔跑)
	///         - _cachedSprintMultiplier = DEFAULT_SPRINT_MULTIPLIER (1.5)
	///     </remarks>
	/// </summary>
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
	///     
	///     返回值说明:
	///     - -1.0: 向左移动 (A键或左箭头)
	///     - 0.0: 无水平输入
	///     - 1.0: 向右移动 (D键或右箭头)
	///     
	///     注意: 此值已经过标准化处理，确保在[-1.0, 1.0]范围内
	/// </remarks>
	public float HorizontalDirection => _globalInputService.HorizontalDirection;

	/// <inheritdoc />
	/// <remarks>
	///     从全局输入服务获取跳跃按键状态
	///     数据来源: GlobalGameplayInputService.DetectJumpInput()
	///     
	///     特性:
	///     - 单次触发模式: 读取后自动重置为false
	///     - 防止连跳: 一帧内不会重复触发
	///     
	///     使用场景:
	///     配合TryJump()方法使用:
	///     <code>
	///     if (_inputHandler.IsJumpPressed &amp;&amp; _physicsMovement.TryJump())
	///     {
	///         // 跳跃成功
	///     }
	///     </code>
	/// </remarks>
	public bool IsJumpPressed => _globalInputService.IsJumpPressed;

	/// <inheritdoc />
	/// <remarks>
	///     从全局输入服务获取交互键(E键)状态
	///     数据来源: GlobalGameplayInputService.DetectInteractInput()
	///     
	///     特性:
	///     - 单次触发模式: 读取后自动重置为false
	///     - 支持E键和ui_interact动作(如果配置)
	///     
	///     使用场景:
	///     与场景中的可交互对象进行互动:
	///     <code>
	///     if (_inputHandler.IsInteractPressed)
	///     {
	///         // 触发交互（如按钮、开关、NPC对话等）
	///     }
	///     </code>
	/// </remarks>
	public bool IsInteractPressed => _globalInputService.IsInteractPressed;

	/// <inheritdoc />
	/// <remarks>
	///     返回当前帧的奔跑状态
	///     基于_isSprinting字段的值
	///     
	///     更新机制:
	///     每帧调用UpdateInput()时通过DetectSprintInput()刷新此值
	///     
	///     触发条件:
	///     - 按住左Shift键(Key.Shift)或右Shift键
	///     - 支持同时检测两个Shift键
	///     
	///     数据流向:
	///     IsSprinting → PlayerPhysicsMovement.UpdateHorizontalVelocity()
	///     → 决定是否应用SprintMultiplier倍率
	/// </remarks>
	public bool IsSprinting => _isSprinting;

	/// <inheritdoc />
	/// <remarks>
	///     返回当前缓存的奔跑速度倍率
	///     基于_cachedSprintMultiplier字段的值
	///     
	///     同步来源:
	///     通过OnSprintMultiplierChanged()从PlayerData自动同步
	///     
	///     使用场景:
	///     - PlayerMovementController.ProcessMovement()中读取
	///     - 传递给PlayerPhysicsMovement.UpdateHorizontalVelocity()
	///     - 用于计算实际奔跑速度: Speed * SprintMultiplier
	///     
	///     默认值: 1.5 (PlayerData.DEFAULT_SPRINT_MULTIPLIER)
	/// </remarks>
	public float CachedSprintMultiplier => _cachedSprintMultiplier;

	/// <inheritdoc />
	/// <remarks>
	///     执行的操作:
	///     1. 检测奔跑键状态(Shift)并更新_isSprinting
	///     2. 全局输入刷新由GlobalInputController._Input()统一处理
	///     
	///     调用时机:
	///     应在每帧_PhysicsProcess()开始时调用一次
	///     在读取任何其他属性之前调用
	///     
	///     注意事项:
	///     - 必须在主线程调用
	///     - 不应在一帧内多次调用
	///     - 应配合UpdateState()一起调用
	/// </remarks>
	public void UpdateInput()
	{
		_isSprinting = DetectSprintInput();
	}

	#endregion

	#region IPlayerDataListener 实现

	/// <summary>
	///     速度变化时的处理
	///     <para>
	///         输入模块暂不直接使用此属性
	///         仅记录日志用于调试和监控
	///     </para>
	///     <param name="oldValue">变化前的速度值</param>
	///     <param name="newValue">变化后的速度值</param>
	///     <remarks>
	///         当前行为: 仅输出日志
	///         未来扩展: 可能用于输入灵敏度调整
	///     </remarks>
	/// </summary>
	public void OnSpeedChanged(float oldValue, float newValue)
	{
		GD.Print($"[PlayerInputHandler] 检测到速度变化: {oldValue} → {newValue}");
	}

	/// <summary>
	///     跳跃速度变化时的处理
	///     <para>
	///         输入模块暂不直接使用此属性
	///         仅记录日志用于调试和监控
	///     </para>
	///     <param name="oldValue">变化前的跳跃速度值</param>
	///     <param name="newValue">变化后的跳跃速度值</param>
	///     <remarks>
	///         当前行为: 仅输出日志
	///         未来扩展: 可能用于跳跃高度预览
	///     </remarks>
	/// </summary>
	public void OnJumpVelocityChanged(float oldValue, float newValue)
	{
		GD.Print($"[PlayerInputHandler] 检测到跳跃速度变化: {oldValue} → {newValue}");
	}

	/// <summary>
	///     重力变化时的处理
	///     <para>
	///         输入模块暂不直接使用此属性
	///         仅记录日志用于调试和监控
	///     </para>
	///     <param name="oldValue">变化前的重力值</param>
	///     <param name="newValue">变化后的重力值</param>
	///     <remarks>
	///         当前行为: 仅输出日志
	///         未来扩展: 可能用于下落速度预估
	///     </remarks>
	/// </summary>
	public void OnGravityChanged(float oldValue, float newValue)
	{
		GD.Print($"[PlayerInputHandler] 检测到重力变化: {oldValue} → {newValue}");
	}

	/// <summary>
	///     奔跑倍率变化时自动更新本地缓存
	///     <para>
	///         确保速度计算始终使用最新的倍率值
	///     </para>
	///     <param name="oldValue">变化前的奔跑倍率</param>
	///     <param name="newValue">变化后的奔跑倍率</param>
	///     <remarks>
	///         核心功能:
	///         更新_cachedSprintMultiplier缓存值
	///         
	///         数据流向:
	///         PlayerDataManager.PlayerData.SprintMultiplier变更
	///         → PlayerDataManager.OnSprintMultiplierChanged()被调用
	///         → 此方法被触发
	///         → _cachedSprintMultiplier更新
	///         → 后续CachedSprintMultiplier属性返回新值
	///         
	///         同步保证:
	///         立即生效，无需等待下一帧
	///         确保数值一致性
	///     </remarks>
	/// </summary>
	public void OnSprintMultiplierChanged(float oldValue, float newValue)
	{
		_cachedSprintMultiplier = newValue;
		GD.Print($"[PlayerInputHandler] 奔跑倍率已更新: {oldValue} → {newValue}");
	}

	#endregion

	#region 公开API - 速度计算

	/// <summary>
	///     计算实际移动速度
	///     <para>
	///         根据奔跑状态应用相应的速度倍率
	///     </para>
	///     <param name="baseSpeed">基础移动速度(像素/秒)</param>
	///     <returns>
	///     考虑奔跑状态后的实际速度(像素/秒)
	///     - 未奔跑: 返回baseSpeed
	///     - 奔跑中: 返回baseSpeed * _cachedSprintMultiplier
	///     </returns>
	///     <remarks>
	///         计算公式:
	///         actualSpeed = baseSpeed * (isSprinting ? sprintMultiplier : 1.0)
	///         
	///         使用示例:
	///         <code>
	///         var baseSpeed = playerData.Speed; // 300.0
	///         var actualSpeed = inputHandler.CalculateActualSpeed(baseSpeed);
	///         // 如果在奔跑状态且sprintMultiplier=1.5
	///         // 返回 450.0
	///         </code>
	///         
	///         应用场景:
	///         - UI显示当前实际速度时
	///         - 其他系统需要知道实际移动速度时
	///         - 调试和性能分析时
	///         
	///         注意: 此方法是辅助方法，物理模块通常直接使用属性而非此方法
	///     </remarks>
	/// </summary>
	public float CalculateActualSpeed(float baseSpeed)
	{
		return _isSprinting ? baseSpeed * _cachedSprintMultiplier : baseSpeed;
	}

	#endregion

	#region 私有方法 - 输入检测

	/// <summary>
	///     检测奔跑键输入状态
	///     <para>
	///         支持左Shift和右Shift键
	///         可扩展支持手柄输入
	///     </para>
	///     <returns>
	///     bool: 是否按住奔跑键
	///     - true: 按住任意一个Shift键
	///     - false: 未按住任何Shift键
	///     </returns>
	///     <remarks>
	///         当前支持的按键:
	///         - Key.Shift (包含左Shift和右Shift)
	///         
	///         检测方式:
	///         使用Input.IsKeyPressed()检测按键按下状态
	///         返回持续按下的状态(非单次触发)
	///         
	///         扩展建议:
	///         - 支持手柄左摇杆按下
	///         - 支持自定义快捷键绑定
	///         - 支持双击方向键冲刺
	///         
	///         性能说明:
	///         此方法非常轻量，仅一次API调用
	///         不会造成性能问题
	///     </remarks>
	/// </summary>
	private bool DetectSprintInput()
	{
		return Input.IsKeyPressed(Key.Shift);
	}

	#endregion
}
