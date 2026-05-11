namespace GFrameworkGodotTemplate.scripts.player.interfaces;

using GFramework.Core.Abstractions.State;

/// <summary>
///     玩家状态控制器接口
///     <para>
///         定义游戏全局状态感知和输入控制权的标准契约
///         负责判断当前是否允许玩家进行操作
///     </para>
///     <author>AI Assistant</author>
///     <version>2.1.0</version>
///     <date>2026-05-11</date>
///     <description>
///         核心职责:
///         1. 状态感知: 检测当前游戏全局状态(Playing/Paused/Menu等)
///         2. 输入控制: 根据状态决定是否允许玩家输入
///         3. 依赖注入: 通过SetStateMachineSystem接收框架服务
///         4. 帧更新: 提供每帧刷新状态的入口点
///         
///         设计目的:
///         - 将输入控制逻辑与具体状态实现解耦
///         - 提供统一的输入启用/禁用机制
///         - 支持运行时动态状态切换
///     </description>
///     <remarks>
///         设计原则:
///         - 单一职责(SRP): 只负责状态检测和输入控制决策
///         - 接口隔离(ISP): 提供最小化的方法集合
///         - 依赖倒置(DIP): 依赖抽象的IStateMachineSystem
///         
///         使用场景:
///         - PlayerMovementController在每帧检查IsInputEnabled
///         - 非PlayingState时自动禁用所有玩家输入
///         - 支持暂停、菜单、过场动画等场景
///         
///         实现要求:
///         - 必须通过SetStateMachineSystem接收IStateMachineSystem实例
///         - UpdateState()应在每帧物理更新时调用一次
///         - IsInputEnabled应基于当前状态实时计算
///         
///         数据流向:
///         IStateMachineSystem.Current → PlayerStateController.UpdateState()
///         → IsInputEnabled属性 → PlayerMovementController._PhysicsProcess()
///         
///         使用示例:
///         <code>
///         // 在物理更新循环中使用
///         stateController.UpdateState();
///         
///         if (stateController.IsInputEnabled)
///         {
///             // 处理玩家输入
///             ProcessPlayerInput();
///         }
///         else
///         {
///             // 禁用输入，停止移动
///             physicsMovement.StopImmediately();
///         }
///         </code>
///         
///         状态映射:
///         - PlayingState: IsInputEnabled = true (允许输入)
///         - 其他所有状态: IsInputEnabled = false (禁止输入)
///     </remarks>
/// </summary>
public interface IPlayerStateController
{
	/// <summary>
	///     检测当前是否允许玩家输入
	///     <para>
	///         基于游戏全局状态(如PlayingState)决定输入是否生效
	///     </para>
	///     <remarks>
	///         判断逻辑:
	///         return _stateMachineSystem.Current is PlayingState;
	///         
	///         返回值含义:
	///         - true: 当前处于PlayingState，允许玩家操作
	///         - false: 当前不在PlayingState，禁用所有输入
	///         
	///         影响范围:
	///         当返回false时，PlayerMovementController会:
	///         - 调用StopImmediately()立即停止移动
	 ///         - 跳过所有输入处理逻辑
	///         - 仅执行MoveAndSlide保持碰撞检测
	///         
	///         使用场景:
	///         <code>
	///         if (_stateController.IsInputEnabled)
	///         {
	///             ProcessMovement(delta);
	///         }
	///         else
	///         {
	///             _physicsMovement.StopImmediately();
	///             _physicsMovement.Move(this);
	///         }
	///         </code>
	///         
	///         注意: 此属性应在UpdateState()之后访问，确保状态是最新的
	///     </remarks>
	/// </summary>
	bool IsInputEnabled { get; }

	/// <summary>
	///     初始化状态控制器
	///     <para>
	///         在节点Ready时调用以获取必要的系统服务引用
	///         执行一次性初始化操作
	///     </para>
	///     <remarks>
	///         调用时机:
	///         通常在PlayerMovementController._Ready()中调用
	///         在创建实例后、首次使用前调用
	///         
	///         可能的初始化操作:
	///         - 验证依赖项是否就绪
	///         - 设置初始状态标志
	///         - 预加载必要资源
	///         
	///         当前实现:
	///         空实现（预留扩展点）
	///         未来可能添加初始化逻辑
	///         
	///         注意: 不应在此方法中进行耗时操作
	///     </remarks>
	/// </summary>
	void Initialize();

	/// <summary>
	///     使用依赖注入设置状态机系统引用
	///     <para>
	///         由PlayerMovementController在_Ready()中调用
	///         注入GFramework的状态机系统服务
	///     </para>
	///     <param name="stateMachineSystem">
	///     框架状态机系统实例
	///     通过this.GetSystem&lt;IStateMachineSystem&gt;()获取
	///     </param>
	///     <remarks>
	///         注入时机:
	///         在PlayerMovementController._Ready()中调用
	///         在InitializeModules()阶段执行
	///         
	///         注入来源:
	///         <code>
	///         var stateMachineSystem = this.GetSystem&lt;IStateMachineSystem&gt;();
	///         _stateController.SetStateMachineSystem(stateMachineSystem);
	///         </code>
	///         
	///         使用方式:
	///         存储此引用后在UpdateState()中使用
	///         通过Current属性获取当前状态实例
	///         
	///         注意事项:
	///         - 可能为null（如果系统未注册）
	///         - 应在使用前进行null检查
	///         - 不应为null时记录错误日志
	///     </remarks>
	/// </summary>
	void SetStateMachineSystem(IStateMachineSystem stateMachineSystem);

	/// <summary>
	///     更新状态检测
	///     <para>
	///         每帧调用以刷新状态缓存
	///         基于当前状态机状态更新IsInputEnabled属性
	///     </para>
	///     <remarks>
	///         执行操作:
	///         1. 检查_stateMachineSystem是否有效
	///         2. 如果无效: 设置IsInputEnabled = false
	///         3. 如果有效: 检测Current是否为PlayingState
	///         4. 更新IsInputEnabled属性
	///         
	///         调用时机:
	///         应在每帧_PhysicsProcess()开始时调用
	///         在读取IsInputEnabled之前调用
	///         
	///         性能说明:
	///         - 此方法应该非常轻量（仅类型检查）
	///         - 避免在此方法中进行复杂计算
	///         - 缓存结果避免重复计算
	///         
	///         错误处理:
	///         当_stateMachineSystem为null时:
	///         - 安全地设置IsInputEnabled = false
	///         - 记录警告日志（仅首次）
	///         - 不抛出异常（保证健壮性）
	///         
	///         使用示例:
	///         <code>
	///         public override void _PhysicsProcess(double delta)
	///         {
	///             var deltaF = (float)delta;
	///             
	///             _stateController.UpdateState(); // 先更新状态
	///             
	///             if (!_stateController.IsInputEnabled)
	///             {
	///                 // 禁用输入
	///                 _physicsMovement.StopImmediately();
	///                 _physicsMovement.Move(this);
	///                 return;
	///             }
	///             
	///             // 正常处理输入
	///             ProcessMovement(deltaF);
	///         }
	///         </code>
	///     </remarks>
	/// </summary>
	void UpdateState();
}
