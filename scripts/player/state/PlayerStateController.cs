using GFramework.Core.Abstractions.State;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.level;
using GFrameworkGodotTemplate.scripts.player.interfaces;
using Godot;

namespace GFrameworkGodotTemplate.scripts.player.state;

/// <summary>
///     玩家状态控制器实现
///     <para>
///         基于GFramework状态机系统的输入控制权管理
///         仅在PlayingState时允许玩家操作，其他状态完全禁用
///     </para>
///     <author>AI Assistant</author>
///     <version>2.1.0</version>
///     <date>2026-05-11</date>
///     <description>
///         核心职责:
///         1. 状态查询: 检测当前游戏状态是否为PlayingState
///         2. 输入控制: 根据状态决定是否允许玩家输入
///         3. 状态同步: 每帧更新状态缓存，确保响应及时
///         
///         架构设计:
///         - 依赖注入: 通过SetStateMachineSystem()接收状态机实例
///         - 委托模式: 将实际的状态检测委托给IStateMachineSystem
///         - 缓存策略: 每帧更新IsInputEnabled属性，避免频繁查询
///     </description>
///     <remarks>
///         设计原则:
///         - 单一职责(SRP): 只负责状态相关的输入控制逻辑
///         - 接口隔离(ISP): 通过IPlayerStateController接口暴露最小化API
///         - 依赖倒置(DIP): 依赖抽象的IStateMachineSystem接口
///         
///         数据流向:
///         IStateMachineSystem.Current (状态机当前状态)
///             ↓
///         UpdateState() (每帧调用，检测状态)
///             ↓
///         IsInputEnabled (布尔属性，供外部查询)
///         
///         使用示例:
///         <code>
///         // 创建实例
///         var stateController = new PlayerStateController();
///         
///         // 注入状态机系统
///         var stateMachine = this.GetSystem&lt;IStateMachineSystem&gt;();
///         stateController.SetStateMachineSystem(stateMachine);
///         
///         // 在物理更新循环中使用
///         stateController.UpdateState();
///         
///         if (stateController.IsInputEnabled)
///         {
///             // 处理玩家输入
///         }
///         else
///         {
///             // 禁用输入，停止移动
///         }
///         </code>
///         
///         状态映射表:
///         | 游戏状态 | IsInputEnabled | 说明 |
///         |---------|---------------|------|
///         | PlayingState | true | 允许完整输入 |
///         | MainMenuState | false | 禁用所有输入 |
///         | PausedState | false | 暂停时禁用 |
///         | BootStartState | false | 启动中禁用 |
///         | GameOverState | false | 结束时禁用 |
///         
///         性能说明:
///         - UpdateState()应在_PhysicsProcess()中调用（非_Update）
///         - IsInputEnabled是缓存的布尔值，读取无性能开销
///         - 避免在一帧内多次调用UpdateState()
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
	///     <remarks>
	///         注入来源:
	///         由PlayerMovementController从GFramework容器获取并传入
	 ///         
	///         使用方式:
	///         - UpdateState()中访问Current属性获取当前状态
	///         - 判断当前状态是否为PlayingState
	///         
	///         生命周期:
	///         - 通过SetStateMachineSystem()注入
	///         - 整个对象生命周期内保持引用
	///         - 不负责此服务的创建和销毁
	///         
	///         初始值: null (未初始化状态)
	///     </remarks>
	/// </summary>
	private IStateMachineSystem? _stateMachineSystem;

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
	///     
	///     使用示例:
	///     <code>
	///     var stateMachine = this.GetSystem&lt;IStateMachineSystem&gt;();
	///     stateController.SetStateMachineSystem(stateMachine);
	///     </code>
	/// </remarks>
	public void SetStateMachineSystem(IStateMachineSystem stateMachineSystem)
	{
		_stateMachineSystem = stateMachineSystem;
	}

	/// <inheritdoc />
	/// <remarks>
	///     实现逻辑:
	///     1. 检查_stateMachineSystem是否为null
	///        - 如果为null: 设置IsInputEnabled=false，直接返回
	///        - 如果不为null: 继续下一步
	///     2. 获取_current状态
	///     3. 判断是否为PlayingState类型
	///        - 是: 设置IsInputEnabled=true
	///        - 否: 设置IsInputEnabled=false
	///     4. 额外检查关卡构建阶段标志
	///        - 如果BaseLevelController.IsBuildPhaseActive为true
	///        - 强制设置IsInputEnabled=false（禁止移动）
	///     5. 额外检查关卡成功阶段标志
	///        - 如果BaseLevelController.IsSuccessPhaseActive为true
	///        - 强制设置IsInputEnabled=false（禁止移动，但允许鼠标UI操作）
	///     
	///     调用时机:
	///     应在每帧_PhysicsProcess()开始时调用一次
	///     在读取IsInputEnabled属性之前调用
	///     
	///     注意事项:
	///     - 必须在主线程调用
	///     - 不应在一帧内多次调用
	///     - 应配合输入处理一起调用
	///     
	///     性能影响:
	///     此方法非常轻量，仅一次null检查和类型比较
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
