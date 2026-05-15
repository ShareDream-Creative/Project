namespace GFrameworkGodotTemplate.scripts.player.interfaces;

using GFramework.Core.Abstractions.State;
using GFrameworkGodotTemplate.global;

/// <summary>
///     玩家状态控制器接口
///     <para>
///         定义游戏全局状态感知和输入控制权的标准契约
///         负责判断当前是否允许玩家进行操作
///         
	 ///         v2.2增强: 支持基于全局输入服务的LevelPhase阻断机制
///     </para>
///     <author>AI Assistant</author>
///     <version>2.2.0 (Enhanced)</version>
///     <date>2026-05-15</date>
///     <description>
///         核心职责:
///         1. 状态感知: 检测当前游戏全局状态(Playing/Paused/Menu等)
///         2. 输入控制: 根据状态和全局输入服务决定是否允许玩家输入
///         3. 依赖注入: 通过SetStateMachineSystem和SetGlobalInputService接收框架服务
///         4. 帧更新: 提供每帧刷新状态的入口点
///         
///         设计目的:
///         - 将输入控制逻辑与具体状态实现解耦
///         - 提供统一的输入启用/禁用机制
///         - 支持运行时动态状态切换
///         - 集成全局输入服务的关卡阶段阻断功能
///     </description>
///     <remarks>
///         设计原则:
///         - 单一职责(SRP): 只负责状态检测和输入控制决策
///         - 接口隔离(ISP): 提供最小化的方法集合
///         - 依赖倒置(DIP): 依赖抽象的IStateMachineSystem和IGlobalGameplayInputService
///         
///         数据流向(v2.2增强):
///         IStateMachineSystem.Current → PlayerStateController.UpdateState()
///             ↓
///         GlobalGameplayInputService.IsInputEnabled → PlayerStateController.UpdateState()
///             ↓
///         IsInputEnabled属性 → PlayerMovementController._PhysicsProcess()
///     </remarks>
/// </summary>
public interface IPlayerStateController
{
	/// <summary>
	///     检测当前是否允许玩家输入
	///     <para>
	///         基于游戏全局状态(如PlayingState)和全局输入服务决定输入是否生效
	///     </para>
	/// </summary>
	bool IsInputEnabled { get; }

	/// <summary>
	///     初始化状态控制器
	/// </summary>
	void Initialize();

	/// <summary>
	///     使用依赖注入设置状态机系统引用
	///     <param name="stateMachineSystem">框架状态机系统实例</param>
	/// </summary>
	void SetStateMachineSystem(IStateMachineSystem stateMachineSystem);

	/// <summary>
	///     使用依赖注入设置全局输入服务引用 (v2.2新增)
	 ///     <param name="globalInputService">全局游戏玩法输入服务实例</param>
	 ///     <remarks>
	 ///         注入后，UpdateState()会检查 GlobalGameplayInputService.IsInputEnabled
	 ///         当 Success/Defeat/Build 等非 Play 阶段激活时自动禁用玩家输入
	 ///     </remarks>
	/// </summary>
	void SetGlobalInputService(IGlobalGameplayInputService? globalInputService);

	/// <summary>
	///     更新状态检测
	///     <para>
	 ///         每帧调用以刷新状态缓存
	 ///         v2.2增强: 同时检查全局输入服务状态
	 ///     </para>
	/// </summary>
	void UpdateState();
}
