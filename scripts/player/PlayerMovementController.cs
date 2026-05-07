using GFramework.Core.Abstractions.State;
using GFrameworkGodotTemplate.global;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.player.interfaces;
using GFrameworkGodotTemplate.scripts.player.input;
using GFrameworkGodotTemplate.scripts.player.physics;
using GFrameworkGodotTemplate.scripts.player.state;
using Godot;

namespace GFrameworkGodotTemplate.scripts.player;

/// <summary>
///     玩家角色移动控制器(组合器模式)
///     协调输入处理、物理运动、状态控制三个子模块的工作
///     本身不包含具体业务逻辑，仅负责模块组装和生命周期管理
///     
///     架构设计(重构后):
///     - 遵循单一职责原则(SRP): 每个模块只负责一个关注点
///     - 使用依赖倒置原则(DIP): 通过接口解耦具体实现
///     - 采用组合优于继承: 通过组合子模块扩展功能
///     - 全局服务解耦: 输入检测已迁移至 global 层
///     
///     模块依赖关系:
///     ┌─────────────────────────────────────┐
///     │   PlayerMovementController          │  ← 组合器 (本类)
///     │                                     │
///     ├── IPlayerInputHandler               │ ← 接口适配层
///     │   └── PlayerInputHandler            │    委托给全局服务
///     │        └── IGlobalGameplayInputService ← 全局输入源(global)
///     │             └── GlobalGameplayInputService
///     │                  └── Godot Input API
///     │                                     │
///     ├── IPlayerPhysicsMovement            │ ← 物理计算层
///     │   └── PlayerPhysicsMovement         │
///     │        └── CharacterBody2D API      │
///     │                                     │
///     └── IPlayerStateController            │ ← 状态控制层
///         └── PlayerStateController         │
///              └── IStateMachineSystem      │
/// </summary>
[ContextAware]
[Log]
public partial class PlayerMovementController : CharacterBody2D, IController
{
	#region 导出属性配置 (同步到Physics模块)

	/// <summary>
	///     移动速度 (像素/秒)
	/// </summary>
	[Export] public float Speed { get; set; } = 300.0f;

	/// <summary>
	///     跳跃力度 (像素/秒)
	/// </summary>
	[Export] public float JumpVelocity { get; set; } = -500.0f;

	/// <summary>
	///     重力加速度 (由Godot项目设置获取)
	/// </summary>
	[Export] public float Gravity { get; set; } = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	#endregion

	#region 子模块实例 (组合关系)

	private IPlayerInputHandler _inputHandler;

	private IPlayerPhysicsMovement _physicsMovement;

	private IPlayerStateController _stateController;

	#endregion

	#region 全局服务引用

	private IGlobalGameplayInputService? _globalInputService;

	#endregion

	#region 生命周期方法

	public override void _Ready()
	{
		base._Ready();
		
		InitializeGlobalServices();
		InitializeModules();
		SyncConfigurationToModules();
		
		_log.Debug("PlayerMovementController初始化完成 (组合器模式 + 全局服务解耦)");
		_log.Debug($"子模块状态: Input={_inputHandler.GetType().Name}, Physics={_physicsMovement.GetType().Name}, State={_stateController.GetType().Name}");
	}

	public override void _PhysicsProcess(double delta)
	{
		var deltaF = (float)delta;
		
		UpdateStateAndInput(deltaF);
		
		if (!_stateController.IsInputEnabled)
		{
			_physicsMovement.StopImmediately();
			_physicsMovement.Move(this);
			return;
		}

		ProcessMovement(deltaF);
	}

	#endregion

	#region 私有方法 - 全局服务初始化

	/// <summary>
	///     初始化全局输入服务引用
	///     GlobalInputController 是 AutoLoad 节点，挂载在 /root/ 路径下
	///     使用多策略查找确保兼容性
	/// </summary>
	private void InitializeGlobalServices()
	{
		_globalInputService = FindGameplayInputService();
		
		if (_globalInputService != null)
		{
			_log.Debug("成功获取全局游戏玩法输入服务");
		}
		else
		{
			_log.Error("无法找到全局游戏玩法输入服务! 输入功能将不可用。");
		}
	}

	/// <summary>
	///     查找全局游戏玩法输入服务
	///     GlobalInputController 是 AutoLoad 节点，挂载在 /root/GlobalInputController
	/// </summary>
	/// <returns>找到的服务实例，未找到返回null</returns>
	private IGlobalGameplayInputService? FindGameplayInputService()
	{
		var tree = GetTree();
		if (tree == null)
		{
			_log.Error("无法获取 SceneTree!");
			return null;
		}

		try
		{
			var globalController = tree.Root.GetNode<GlobalInputController>("GlobalInputController");
			
			if (globalController != null && globalController.GameplayInputService != null)
			{
				_log.Debug($"成功找到全局游戏玩法输入服务: {globalController.GameplayInputService.GetType().Name}");
				return globalController.GameplayInputService;
			}
			
			_log.Error("找到 GlobalInputController 节点但 GameplayInputService 为空!");
			return null;
		}
		catch (Exception ex)
		{
			_log.Error($"查找 GlobalInputController 失败: {ex.Message}");
			return null;
		}
	}

	#endregion

	#region 私有方法 - 模块初始化

	/// <summary>
	///     初始化所有子模块
	///     使用默认实现，可通过Setter注入替换为自定义实现
	///     PlayerInputHandler 现在需要注入全局输入服务
	/// </summary>
	private void InitializeModules()
	{
		if (_globalInputService == null)
		{
			_log.Error("全局输入服务未初始化! 无法创建输入处理器。");
			throw new InvalidOperationException("IGlobalGameplayInputService 未就绪");
		}
		
		_inputHandler = new PlayerInputHandler(_globalInputService);
		_physicsMovement = new PlayerPhysicsMovement();
		_stateController = new PlayerStateController();
		
		var stateMachineSystem = this.GetSystem<IStateMachineSystem>();
		
		if (stateMachineSystem == null)
		{
			_log.Error("无法获取状态机系统服务! 输入将被完全禁用。");
		}
		else
		{
			((PlayerStateController)_stateController).SetStateMachineSystem(stateMachineSystem);
		}
	}

	/// <summary>
	///     将Inspector中的导出属性同步到物理运动模块
	///     确保用户在编辑器中修改的参数能够生效
	/// </summary>
	private void SyncConfigurationToModules()
	{
		_physicsMovement.Speed = Speed;
		_physicsMovement.JumpVelocity = JumpVelocity;
		_physicsMovement.Gravity = Gravity;
	}

	#endregion

	#region 私有方法 - 帧更新逻辑

	/// <summary>
	///     更新状态和输入缓存
	///     注意: 输入更新现在由 GlobalInputController._Input() 统一触发
	/// </summary>
	private void UpdateStateAndInput(float delta)
	{
		_stateController.UpdateState();
		_inputHandler.UpdateInput();
	}

	/// <summary>
	///     处理完整的移动逻辑流程
	///     按照物理引擎的标准顺序: 重力 → 输入 → 跳跃 → 应用移动
	/// </summary>
	private void ProcessMovement(float delta)
	{
		_physicsMovement.ApplyGravity(delta);
		_physicsMovement.UpdateHorizontalVelocity(_inputHandler.HorizontalDirection);
		
		if (_inputHandler.IsJumpPressed && _physicsMovement.TryJump())
		{
			_log.Debug("玩家跳跃");
		}
		
		_physicsMovement.Move(this);
	}

	#endregion

	#region 公开API - 模块访问 (用于测试或高级定制)

	/// <summary>
	///     获取输入处理器实例(只读)
	///     可用于单元测试或替换为模拟输入
	/// </summary>
	public IPlayerInputHandler InputHandler => _inputHandler;

	/// <summary>
	///     获取物理运动模块实例(只读)
	///     可用于调试或参数动态调整
	/// </summary>
	public IPlayerPhysicsMovement PhysicsMovement => _physicsMovement;

	/// <summary>
	///     获取状态控制器实例(只读)
	///     可用于外部查询当前是否允许输入
	/// </summary>
	public IPlayerStateController StateController => _stateController;

	#endregion
}
