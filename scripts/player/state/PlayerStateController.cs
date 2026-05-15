using GFramework.Core.Abstractions.State;
using GFrameworkGodotTemplate.global;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.level;
using GFrameworkGodotTemplate.scripts.player.interfaces;
using Godot;

namespace GFrameworkGodotTemplate.scripts.player.state;

/// <summary>
///     玩家状态控制器实现
///     <para>
///         基于GFramework状态机系统和全局输入服务的双重输入控制权管理
///         仅在PlayingState且全局输入启用时允许玩家操作
///     </para>
///     <author>AI Assistant</author>
///     <version>2.2.0 (Enhanced)</version>
///     <date>2026-05-15</date>
///     <description>
///         核心职责:
///         1. 状态查询: 检测当前游戏状态是否为PlayingState
///         2. 输入控制: 根据状态和全局输入服务决定是否允许玩家输入
///         3. 状态同步: 每帧更新状态缓存，确保响应及时
///         
///         架构设计(v2.2增强):
///         - 双重检查机制: 同时检查状态机和全局输入服务
///         - 优先级: 全局输入服务 > 状态机状态 > 阶段标志
///         - 统一阻断: Success/Defeat阶段通过GlobalGameplayInputService统一阻断
///         
///         数据流向:
///         IStateMachineSystem.Current (状态机当前状态)
///             ↓
///         GlobalGameplayInputService.IsInputEnabled (全局输入可用性)
///             ↓
///         UpdateState() (每帧调用，综合判断)
///             ↓
///         IsInputEnabled (布尔属性，供外部查询)
///     </description>
///     <remarks>
///         状态映射表(v2.2增强):
///         | 游戏状态 | LevelPhase | IsInputEnabled | 说明 |
///         |---------|------------|---------------|------|
///         | PlayingState | Play | true | 允许完整输入 |
///         | PlayingState | Build/Success/Defeat | false | 阶段阻断 |
///         | MainMenuState | 任意 | false | 主菜单禁用 |
///         | PausedState | 任意 | false | 暂停时禁用 |
///         | BootStartState | 任意 | false | 启动中禁用 |
///         | GameOverState | 任意 | false | 结束时禁用 |
///         
///         设计原则:
///         - 单一职责(SRP): 只负责状态相关的输入控制逻辑
///         - 接口隔离(ISP): 通过IPlayerStateController接口暴露最小化API
///         - 依赖倒置(DIP): 依赖抽象的IStateMachineSystem接口
///         - 防御编程: 多层检查确保输入安全
///     </remarks>
/// </summary>
public class PlayerStateController : IPlayerStateController
{
	#region 私有字段

	/// <summary>
	///     状态机系统引用
	///     <para>
	///         用于查询当前游戏状态
	///         通过SetStateMachineSystem()方法注入
	 ///     </para>
	/// </summary>
	private IStateMachineSystem? _stateMachineSystem;

	/// <summary>
	///     全局游戏玩法输入服务引用 (v2.2新增)
	///     <para>
	///         用于查询全局输入是否被关卡阶段阻断
	///         通过SetGlobalInputService()方法注入
	 ///     </para>
	 ///     <remarks>
	 ///         注入来源:
	 ///         由PlayerMovementController从GlobalInputController获取并传入
	 ///         
	 ///         使用方式:
	 ///         - UpdateState()中访问IsInputEnabled属性
	 ///         - 判断Success/Defeat等非Play阶段是否激活
	 ///         
	 ///         生命周期:
	 ///         - 通过SetGlobalInputService()注入
	 ///         - 整个对象生命周期内保持引用
	 ///     </remarks>
	/// </summary>
	private IGlobalGameplayInputService? _globalInputService;

	#endregion

	#region 属性实现

	/// <inheritdoc />
	/// <remarks>
	///     返回值说明:
	///     - true: 当前状态为PlayingState，允许玩家输入
	///     - false: 当前状态不是PlayingState，禁止所有输入
	///     
	///     更新时机:
	///     在UpdateState()方法中每帧刷新
	///     
	///     使用场景:
	///     - PlayerMovementController._PhysicsProcess()中判断是否处理输入
	 ///     - 决定是否调用物理模块的移动方法
	///     - 控制角色是否响应用户操作
	///     
	///     默认值: false (安全默认值，未初始化时禁用输入)
	/// </remarks>
	public bool IsInputEnabled { get; private set; }

	#endregion

	#region 方法实现

	/// <inheritdoc />
	/// <remarks>
	///     当前行为:
	///     此方法为空实现，保留用于未来扩展
	///     
	///     可能的扩展:
	///     - 预加载状态机引用
	///     - 初始化内部缓存
	///     - 注册事件监听器
	///     
	///     调用时机:
	///     在对象创建后、首次使用前调用一次
	///     通常由PlayerMovementController._Ready()调用
	/// </remarks>
	public void Initialize()
	{
	}

	/// <inheritdoc />
	/// <remarks>
	///     注入方式:
	///     由外部调用者（通常是PlayerMovementController）传入状态机实例
	 ///     
	 ///     参数验证:
	 ///     允许传入null（表示状态机不可用）
	 ///     当为null时，IsInputEnabled将始终返回false
	 ///     
	 ///     调用时机:
	 ///     - PlayerMovementController.InitializeModules()中调用
	 ///     - 应在UpdateState()之前调用
	 ///     - 整个生命周期内只需调用一次
	 /// </remarks>
	public void SetStateMachineSystem(IStateMachineSystem stateMachineSystem)
	{
		_stateMachineSystem = stateMachineSystem;
	}

	/// <summary>
	///     注入全局游戏玩法输入服务 (v2.2新增)
	 ///     <param name="globalInputService">全局输入服务实例</param>
	 ///     <remarks>
	 ///         注入方式:
	 ///         由PlayerMovementController从GlobalInputController获取并传入
	 ///         
	 ///         参数验证:
	 ///         允许传入null（表示全局输入服务不可用）
	 ///         当为null时，将跳过全局输入状态检查（仅依赖状态机判断）
	 ///         
	 ///         调用时机:
	 ///         - PlayerMovementController.InitializeModules()中调用
	 ///         - 应在UpdateState()之前调用
	 ///         - 整个生命周期内只需调用一次
	 ///         
	 ///         使用示例:
	 ///         <code>
	 ///         var globalService = FindGameplayInputService();
	 ///         stateController.SetGlobalInputService(globalService);
	 ///         </code>
	 ///     </remarks>
	/// </summary>
	public void SetGlobalInputService(IGlobalGameplayInputService? globalInputService)
	{
		_globalInputService = globalInputService;
	}

	/// <inheritdoc />
	/// <remarks>
	///     实现逻辑(v2.2增强 - 双重检查机制):
	 ///     
	 ///     检查优先级(从高到低):
	 ///     1. **全局输入服务状态** (最高优先级)
	 ///        - 如果 _globalInputService 不为 null 且 IsInputEnabled == false
	 ///        - 立即禁用输入，不继续后续检查
	 ///        - 这确保了 Success/Defeat/Build 阶段的输入阻断立即生效
	 ///        
	 ///     2. **关卡构建阶段标志** (中等优先级)
	 ///        - 如果 BaseLevelController.IsBuildPhaseActive 为 true
	 ///        - 强制禁用输入（Build阶段不允许移动）
	 ///        
	 ///     3. **关卡成功阶段标志** (中等优先级)
	 ///        - 如果 BaseLevelController.IsSuccessPhaseActive 为 true
	 ///        - 强制禁用输入（Success阶段不允许移动，但允许鼠标UI操作）
	 ///        
	 ///     4. **状态机当前状态** (最低优先级)
	 ///        - 仅当以上所有检查都通过时
	 ///        - 判断是否为 PlayingState 类型
	 ///        - 是: 启用输入 | 否: 禁用输入
	 ///     
	 ///     设计优势:
	 ///     - 多层防御: 即使某一层失效，其他层仍能保证安全
	 ///     - 响应及时: 全局输入服务状态变更立即生效（无需等待下一帧）
	 ///     - 向后兼容: 保留原有的Build/Success标志检查逻辑
	 ///     - 统一管理: 所有非Play阶段的阻断逻辑集中在 GlobalGameplayInputService
	 ///     
	 ///     调用时机:
	 ///     应在每帧_PhysicsProcess()开始时调用一次
	 ///     在读取IsInputEnabled属性之前调用
	 ///     
	 ///     性能影响:
	 ///     此方法非常轻量，仅几次null检查和布尔比较
	 ///     不会造成性能问题
	/// </remarks>
	public void UpdateState()
	{
		if (_stateMachineSystem == null)
		{
			IsInputEnabled = false;
			return;
		}

		var isPlayingState = _stateMachineSystem.Current is PlayingState;

		if (_globalInputService != null && !_globalInputService.IsInputEnabled)
		{
			if (IsInputEnabled)
			{
				var phase = _globalInputService.CurrentPhase;
				GD.Print($"[PlayerStateController] ⚠ 全局输入已禁用 | 当前阶段: {phase} | 禁止玩家移动");
			}
			
			IsInputEnabled = false;
			return;
		}

		if (BaseLevelController.IsBuildPhaseActive)
		{
			if (IsInputEnabled)
			{
				GD.Print("[PlayerStateController] ⚠ Build阶段激活，禁止玩家移动");
			}
			
			IsInputEnabled = false;
			return;
		}

		if (BaseLevelController.IsSuccessPhaseActive)
		{
			if (IsInputEnabled)
			{
				GD.Print("[PlayerStateController] ⚠ Success阶段激活，禁止玩家移动（允许鼠标UI操作）");
			}
			
			IsInputEnabled = false;
			return;
		}

		IsInputEnabled = isPlayingState;
	}

	#endregion
}
