using GFramework.Core.Abstractions.State;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.Scene;
using GFramework.Godot.UI;
using GFrameworkGodotTemplate.scripts.core.scene;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.core.ui;
using GFrameworkGodotTemplate.scripts.core.ui.level;
using GFrameworkGodotTemplate.scripts.core.controller.level;
using GFrameworkGodotTemplate.scripts.entities.level;
using GFrameworkGodotTemplate.scripts.level.interfaces;
using GFrameworkGodotTemplate.scripts.cqrs.game.command;
using GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command.input;
using GFrameworkGodotTemplate.scripts.enums.ui;
using GFrameworkGodotTemplate.scripts.enums;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level;

/// <summary>
///     通用关卡场景控制器基类（重构后版本）
///     <para>
///         可挂载到任意level场景的通用脚本系统，提供完整的关卡游戏流程管理
///         
///         重构说明：
///         - 已将UI管理、玩家管理、输入控制、规则集成拆分为独立组件
///         - 本类仅负责组件协调和核心流程控制
///         - 通过接口实例化方式调用各功能模块
///     </para>
///     <author>AI Assistant</author>
///     <version>2.0.0 (Refactored)</version>
///     <date>2026-05-13</date>
/// </summary>
[ContextAware]
[Log]
public partial class BaseLevelController : Node2D, IController, ISceneBehaviorProvider, ISimpleScene
{
	#region 私有字段

	/// <summary>场景行为实例</summary>
	protected ISceneBehavior? _scene;

	/// <summary>状态机系统引用</summary>
	private IStateMachineSystem? _stateMachineSystem;

	/// <summary>UI路由器引用</summary>
	private IUiRouter? _uiRouter;

	/// <summary>当前关卡阶段状态</summary>
	private LevelPhase _currentPhase = LevelPhase.Build;

	/// <summary>是否已完成游戏</summary>
	private bool _isGameCompleted;

	#region 重构后的组件接口实例

	/// <summary>UI管理器实例</summary>
	private ILevelUiManager? _uiManager;

	/// <summary>玩家管理器实例</summary>
	private ILevelPlayerManager? _playerManager;

	/// <summary>输入控制器实例</summary>
	private ILevelInputController? _inputController;

	/// <summary>规则集成器实例</summary>
	private ILevelRulesIntegration? _rulesIntegration;

	#endregion

	#endregion

	#region 节点引用

	/// <summary>玩家出生点位置节点</summary>
	protected Node2D BeginPosition => GetNode<Node2D>("%Begin");

	/// <summary>终点碰撞区域</summary>
	protected Area2D EndArea => GetNode<Area2D>("%End");

	#endregion

	#region 公开属性

	/// <summary>获取当前关卡阶段</summary>
	public LevelPhase CurrentPhase => _currentPhase;

	/// <summary>获取是否处于构建阶段（输入受限）</summary>
	public bool IsInBuildPhase => _currentPhase == LevelPhase.Build;

	/// <summary>获取是否处于游玩阶段（输入正常）</summary>
	public bool IsInPlayPhase => _currentPhase == LevelPhase.Play;

	/// <summary>获取是否已完成游戏</summary>
	public bool IsGameCompleted => _isGameCompleted;

	/// <summary>
	///     静态标志：当前是否处于关卡构建阶段
	/// </summary>
	public static bool IsBuildPhaseActive { get; private set; }

	/// <summary>
	///     静态标志：当前是否处于关卡成功阶段
	/// </summary>
	public static bool IsSuccessPhaseActive { get; private set; }

	/// <summary>
	///     重置所有关卡阶段标志
	/// </summary>
	public static void ResetPhaseFlags()
	{
		IsBuildPhaseActive = false;
		IsSuccessPhaseActive = false;
	}

	#endregion

	#region ISimpleScene 接口实现

	/// <summary>场景加载完成回调</summary>
	public ValueTask OnLoadAsync(ISceneEnterParam? param)
	{
		_log.Info($"[BaseLevelController] 场景开始加载, 参数: {param?.GetType().Name ?? "无"}");
		return ValueTask.CompletedTask;
	}

	/// <summary>
	///     场景进入完成回调 ⭐ 核心初始化入口
	/// </summary>
	public async ValueTask OnEnterAsync()
	{
		_log.Info("════════════ 场景进入事件触发 ═══════════");
		_log.Info($"[BaseLevelController] 当前时间: {Time.GetTimeStringFromSystem()}");
		_log.Info($"[BaseLevelController] 场景路径: {SceneFilePath ?? "未知"}");

		try
		{
			_log.Info("[BaseLevelController] 步骤1/7: 初始化服务引用...");
			InitializeServices();

			_log.Info("[BaseLevelController] 步骤2/7: 确保游戏状态...");
			await EnsurePlayingStateAsync();

			_log.Info("[BaseLevelController] 步骤3/7: 清理残留UI...");
			await _uiManager!.ClearExistingUiAsync();

			_log.Info("[BaseLevelController] 步骤4/7: 生成玩家角色...");
			_playerManager!.SpawnPlayer();

			OnPlayerSpawned(_playerManager.PlayerInstance!);

			_log.Info("[BaseLevelController] 步骤5/7: 设置终点检测...");
			_playerManager.SetupEndAreaDetection();

			_playerManager.OnGameCompleteCallback = () =>
			{
				_isGameCompleted = true;
				_uiManager!.ShowSuccessUi();
				OnGameCompleted();
			};

			_log.Info("[BaseLevelController] 步骤6/7: 加载构建界面...");
			await _uiManager.ShowBuildUiAsync();

			_log.Info("[BaseLevelController] 步骤7/7: 初始化规则系统...");
			InitializeRulesIntegration();

			_log.Info("════════════ ✅ 场景初始化完成 ═══════════");
			_log.Info($"[BaseLevelController] 当前阶段: {_currentPhase}");
			_log.Info($"[BaseLevelController] 玩家状态: {(_playerManager.PlayerInstance != null ? "已生成" : "未生成")}");
		}
		catch (Exception ex)
		{
			_log.Error($"[BaseLevelController] ❌ 场景初始化失败: {ex.Message}");
			_log.Error($"[BaseLevelController] 异常类型: {ex.GetType().FullName}");
			_log.Error($"[BaseLevelController] 堆栈跟踪:\n{ex.StackTrace}");
			throw;
		}
	}

	/// <summary>场景暂停回调</summary>
	public async ValueTask OnPauseAsync()
	{
		_log.Info("[BaseLevelController] 场景暂停");
		await ValueTask.CompletedTask;
	}

	/// <summary>场景恢复回调</summary>
	public async ValueTask OnResumeAsync()
	{
		_log.Info("[BaseLevelController] 场景恢复, 确保状态正确...");
		await EnsurePlayingStateAsync();
		_log.Debug("[BaseLevelController] 场景恢复完成");
	}

	/// <summary>场景退出回调</summary>
	public async ValueTask OnExitAsync()
	{
		_log.Info("[BaseLevelController] 场景退出, 清理资源...");

		_playerManager?.Cleanup();
		_rulesIntegration?.Cleanup();

		CleanupPhaseFlags();

		await ValueTask.CompletedTask;
	}

	/// <summary>场景卸载回调</summary>
	public async ValueTask OnUnloadAsync()
	{
		_log.Info("[BaseLevelController] 场景卸载完成");
		await ValueTask.CompletedTask;
	}

	#endregion

	#region 公开API - 场景行为

	/// <summary>获取场景行为实例</summary>
	public virtual ISceneBehavior GetScene()
	{
		_scene ??= SceneBehaviorFactory.Create<Node2D>(this, "BaseLevel");
		return _scene;
	}

	#endregion

	#region 虚方法 - 扩展点

	/// <summary>
	///     玩家生成后的自定义逻辑（子类可重写）
	/// </summary>
	protected virtual void OnPlayerSpawned(Node2D player)
	{
		_log.Debug($"[BaseLevelController] 玩家已生成: {player.Name}");
	}

	/// <summary>
	///     UI阶段切换时的自定义逻辑（子类可重写）
	/// </summary>
	protected virtual void OnPhaseChanged(LevelPhase oldPhase, LevelPhase newPhase)
	{
		_log.Info($"[BaseLevelController] 阶段切换: {oldPhase} → {newPhase}");
	}

	/// <summary>
	///     游戏完成时的自定义逻辑（子类可重写）
	/// </summary>
	protected virtual void OnGameCompleted()
	{
		_log.Info("[BaseLevelController] 🎉 游戏完成！");
	}

	/// <summary>
	///     构建完成时的处理逻辑（由LevelBuildUi调用）
	///     <para>
	///         当玩家点击"开始游戏"按钮后触发
	///         切换到游玩阶段并恢复玩家输入
	///     </para>
	/// </summary>
	public void OnBuildFinished()
	{
		_log.Info("════════════ 构建完成事件触发 ═══════════");
		_uiManager?.OnBuildFinished();
	}

	#endregion

	#region 私有方法 - 初始化

	/// <summary>初始化服务引用</summary>
	private void InitializeServices()
	{
		_stateMachineSystem = this.GetSystem<IStateMachineSystem>();
		_uiRouter = this.GetSystem<IUiRouter>();

		_log.Debug("[BaseLevelController] 服务引用已初始化");

		InitializeComponents();
	}

	/// <summary>初始化重构后的组件实例</summary>
	private void InitializeComponents()
	{
		_log.Info("[BaseLevelController] 正在初始化重构后的组件...");

		_uiManager = new LevelUiManagerImpl(
			_uiRouter,
			this,
			() => _currentPhase,
			phase => _currentPhase = phase,
			() => IsBuildPhaseActive,
			value => IsBuildPhaseActive = value,
			msg => _log.Info(msg),
			msg => _log.Warn(msg),
			msg => _log.Error(msg),
			msg => _log.Debug(msg)
		);

		_playerManager = new LevelPlayerManagerImpl(
			this,
			BeginPosition,
			EndArea,
			() => _currentPhase,
			msg => _log.Info(msg),
			msg => _log.Error(msg),
			msg => _log.Debug(msg)
		);

		_inputController = new LevelInputControllerImpl(
			_stateMachineSystem,
			this,
			() => _currentPhase,
			msg => _log.Info(msg),
			msg => _log.Debug(msg)
		);

		_rulesIntegration = new LevelRulesIntegrationImpl(
			() => _currentPhase,
			phase => _currentPhase = phase,
			() => IsSuccessPhaseActive,
			value => IsSuccessPhaseActive = value,
			msg => _log.Info(msg),
			msg => _log.Error(msg),
			msg => _log.Warn(msg),
			msg => _log.Debug(msg)
		);

		_log.Info("[BaseLevelController] ✓✓✓ 所有组件已成功初始化");
	}

	/// <summary>确保当前为PlayingState</summary>
	private async Task EnsurePlayingStateAsync()
	{
		if (_stateMachineSystem == null)
		{
			_log.Error("[BaseLevelController] 无法获取状态机系统！");
			return;
		}

		if (_stateMachineSystem.Current is PlayingState)
		{
			_log.Debug("[BaseLevelController] 当前已是PlayingState");
			return;
		}

		_log.Info("[BaseLevelController] 切换到PlayingState...");
		await _stateMachineSystem.ChangeToAsync<PlayingState>();
	}

	/// <summary>初始化规则集成器</summary>
	private void InitializeRulesIntegration()
	{
		_rulesIntegration!.ShowDefeatUiCallback = async () =>
		{
			await _uiManager!.ShowDefeatUiAsync();
		};

		_rulesIntegration.DisablePlayerInputCallback = () =>
		{
			_playerManager?.DisablePlayerInput();
		};

		_rulesIntegration.IsGameCompleted = false;

		_rulesIntegration.Initialize();

		_isGameCompleted = false;
	}

	/// <summary>清理阶段标志</summary>
	private void CleanupPhaseFlags()
	{
		IsBuildPhaseActive = false;
		IsSuccessPhaseActive = false;
		_log.Debug("[BaseLevelController] 所有阶段标志已重置");
	}

	#endregion

	#region 输入处理

	/// <summary>重写输入处理，委托给输入控制器</summary>
	public override void _Input(InputEvent @event)
	{
		_inputController?.HandleInput(@event);
	}

	#endregion

	#region 每帧更新

	/// <summary>每帧更新方法，委托给规则集成器</summary>
	public override void _Process(double delta)
	{
		base._Process(delta);

		_rulesIntegration?.Update();
	}

	#endregion
}
