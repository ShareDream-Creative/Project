using GFramework.Core.Abstractions.State;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.Scene;
using GFramework.Godot.UI;
using GFrameworkGodotTemplate.global;
using GFrameworkGodotTemplate.scripts.core.scene;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.core.ui;
using GFrameworkGodotTemplate.scripts.core.ui.level;
using GFrameworkGodotTemplate.scripts.core.controller.level;
using GFrameworkGodotTemplate.scripts.entities.level;
using GFrameworkGodotTemplate.scripts.level.interfaces;
using GFrameworkGodotTemplate.scripts.level.controllers;
using GFrameworkGodotTemplate.scripts.level.config;
using GFrameworkGodotTemplate.scripts.cqrs.game.command;
using GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command.input;
using GFrameworkGodotTemplate.scripts.enums.ui;
using GFrameworkGodotTemplate.scripts.enums;
using GFrameworkGodotTemplate.scripts.player;
using GFrameworkGodotTemplate.scripts.trap;
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
///         
///         架构增强(v2.1):
///         - 集成全局输入服务(IGlobalGameplayInputService)实现统一输入阻塞
///         - 关卡阶段变化自动同步到全局输入服务，确保全项目输入一致性
///     </para>
///     <author>AI Assistant</author>
///     <version>2.1.0 (Enhanced)</version>
///     <date>2026-05-15</date>
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

	/// <summary>
	///     全局游戏玩法输入服务
	///     <para>
	///         用于同步关卡阶段状态到全局输入系统
	///         实现基于LevelPhase的统一输入阻塞机制
	///     </para>
	/// </summary>
	private IGlobalGameplayInputService? _globalInputService;

	#region 重构后的组件接口实例

	/// <summary>UI管理器实例</summary>
	private ILevelUiManager? _uiManager;

	/// <summary>玩家管理器实例</summary>
	private ILevelPlayerManager? _playerManager;

	/// <summary>输入控制器实例</summary>
	private ILevelInputController? _inputController;

	/// <summary>规则集成器实例</summary>
	private ILevelRulesIntegration? _rulesIntegration;

	/// <summary>
	///     全局陷阱事件回调引用（用于取消订阅）
	 ///     <para>
	 ///         保存对 TrapEventManager 的订阅引用，
	 ///         以便在 _ExitTree 时正确取消订阅，避免内存泄漏
	 ///     </para>
	/// </summary>
	private Action<Node>? _trapEventCallback;

	/// <summary>关卡控制器数据实例</summary>
	protected readonly LevelControllerData Data = new();

	/// <summary>玩家重置处理器</summary>
	protected IPlayerResetHandler? ResetHandler;

	#endregion

	#endregion

	#region 节点引用

	/// <summary>玩家出生点位置节点</summary>
	protected Node2D BeginPosition => Data.BeginPositionNode!;

	/// <summary>终点碰撞区域</summary>
	protected Area2D EndArea => Data.EndAreaNode!;

	/// <summary>
	///     失败区域（掉落/危险区域）
	///     <para>
	///         当玩家进入此区域时，会被重置到起点位置
	///         所有关卡场景必须包含此节点（unique_name_in_owner = true）
	///     </para>
	/// </summary>
	protected Area2D DefeatArea => GetNode<Area2D>("%defeat");

	#endregion

	#region 公开属性

	/// <summary>获取当前关卡阶段</summary>
	public LevelPhase CurrentPhase => Data.CurrentPhase;

	/// <summary>获取是否处于构建阶段（输入受限）</summary>
	public bool IsInBuildPhase => Data.IsInBuildPhase;

	/// <summary>获取是否处于游玩阶段（输入正常）</summary>
	public bool IsInPlayPhase => Data.IsInPlayPhase;

	/// <summary>获取是否已完成游戏</summary>
	public bool IsGameCompleted => Data.IsGameCompleted;

	/// <summary>
	///     静态标志：当前是否处于关卡构建阶段
	/// </summary>
	public static bool IsBuildPhaseActive => LevelControllerData.IsBuildPhaseActive;

	/// <summary>
	///     静态标志：当前是否处于关卡成功阶段
	/// </summary>
	public static bool IsSuccessPhaseActive => LevelControllerData.IsSuccessPhaseActive;

	/// <summary>
	///     重置所有关卡阶段标志
	/// </summary>
	public static void ResetPhaseFlags()
	{
		LevelControllerData.ResetPhaseFlags();
	}

	#region 配置驱动的行为标志（子类可在构造函数中设置）

	/// <summary>
	///     是否跳过构建阶段（直接进入游玩阶段）
	///     <para>
	///         当设置为true时，OnEnterAsync()会自动调用OnBuildFinished()
	///         跳过构建界面，直接进入游玩阶段
	///     </para>
	///     <remarks>
	///         使用场景:
	///         - 教程关卡（TeachLevel）：不需要构建界面
	///         - 测试关卡：快速进入游戏测试
	///         - 特殊剧情关卡：固定布局无需构建
	///         
	///         默认值: false (正常流程，显示构建界面)
	///     </remarks>
	/// </summary>
	protected bool SkipBuildPhase
	{
		get => Data.SkipBuildPhase;
		set => Data.SkipBuildPhase = value;
	}

	/// <summary>
	///     胜利时是否重置GameLevel为None
	///     <para>
	///         当设置为true时，OnGameCompleted()会自动将GameLevel设为GameLevel.None
	///         LevelEndUi的"下一关"按钮会返回到选关界面而非下一关
	///     </para>
	///     <remarks>
	///         使用场景:
	///         - 教程关卡（TeachLevel）：完成后返回选关界面选第一关
	///         - 最后一关：没有下一关可选
	///         
	///         默认值: false (正常流程，GameLevel保持当前值)
	///     </remarks>
	/// </summary>
	protected bool ResetGameLevelOnVictory
	{
		get => Data.ResetGameLevelOnVictory;
		set => Data.ResetGameLevelOnVictory = value;
	}

	/// <summary>
	///     是否启用陷阱系统
	///     <para>
	///         当设置为true时，OnEnterAsync()会自动扫描场景中的TrapStatic节点
	///         并连接TrapTriggered信号到HandleTrapTriggered方法
	///     </para>
	///     <remarks>
	///         使用场景:
	///         - 教程关卡（TeachLevel）：包含陷阱教学区域
	///         - 任何需要隐藏/重置玩家机制的关卡
	///         
	///         默认值: false (不扫描陷阱节点)
	///         
	///         依赖条件:
	///         - 场景中必须存在 TrapStatic 节点实例
	///         - TrapStatic 必须正确配置碰撞区域
	///     </remarks>
	/// </summary>
	protected bool EnableTrapSystem
	{
		get => Data.EnableTrapSystem;
		set => Data.EnableTrapSystem = value;
	}

	#endregion

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
				Data.MarkGameCompleted();
				_uiManager!.ShowSuccessUi();
				OnGameCompleted();
			};

			if (SkipBuildPhase)
			{
				_log.Info("[BaseLevelController] 步骤6/9: 跳过构建阶段（SkipBuildPhase=true）...");
				OnBuildFinished();
			}
			else
			{
				_log.Info("[BaseLevelController] 步骤6/9: 加载构建界面...");
				await _uiManager.ShowBuildUiAsync();
			}

			_log.Info("[BaseLevelController] 步骤7/9: 初始化规则系统...");
			InitializeRulesIntegration();

			_log.Info("[BaseLevelController] 步骤8/9: 初始化失败区域检测...");
			InitializeDefeatAreaDetection();

			_log.Info("[BaseLevelController] 步骤9/9: 订阅全局陷阱事件...");
			SubscribeToGlobalTrapEvents();

			_log.Info("════════════ ✅ 场景初始化完成 ═══════════");
			_log.Info($"[BaseLevelController] 当前阶段: {Data.CurrentPhase}");
			_log.Info($"[BaseLevelController] 玩家状态: {(_playerManager.PlayerInstance != null ? "已生成" : "未生成")}");
			_log.Info($"[BaseLevelController] 配置状态: SkipBuildPhase={Data.SkipBuildPhase}, ResetGameLevelOnVictory={Data.ResetGameLevelOnVictory}, EnableTrapSystem={Data.EnableTrapSystem}");
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
	///     <para>
	///         当 ResetGameLevelOnVictory=true 时，会自动将 GameLevel 重置为 None
	///         使 LevelEndUi 的"下一关"按钮返回选关界面
	///     </para>
	/// </summary>
	protected virtual void OnGameCompleted()
	{
		_log.Info("[BaseLevelController] 🎉 游戏完成！");

		if (Data.ResetGameLevelOnVictory)
		{
			try
			{
				LevelChoose.SetCurrentGameLevel(GameLevel.None);
				_log.Info("[BaseLevelController] ✓ 已重置 GameLevel 为 None（ResetGameLevelOnVictory=true）");
			}
			catch (Exception ex)
			{
				_log.Error($"[BaseLevelController] ❌ 重置 GameLevel 异常: {ex.Message}");
			}
		}
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

		InitializeGlobalInputService();

		_log.Debug("[BaseLevelController] 服务引用已初始化");

		InitializeComponents();
	}

	/// <summary>
	///     初始化全局游戏玩法输入服务
	///     <para>
	///         从 GlobalInputController 获取 IGlobalGameplayInputService 实例
	///         用于同步关卡阶段状态，实现统一的输入阻塞机制
	 ///     </para>
	/// </summary>
	private void InitializeGlobalInputService()
	{
		try
		{
			var tree = GetTree();
			if (tree == null)
			{
				_log.Warn("[BaseLevelController] ⚠️ 无法获取 SceneTree，跳过全局输入服务初始化");
				return;
			}

			var globalController = tree.Root.GetNode<GlobalInputController>("GlobalInputController");
			
			if (globalController != null && globalController.GameplayInputService != null)
			{
				_globalInputService = globalController.GameplayInputService;

				_globalInputService.SetCurrentPhase(Data.CurrentPhase);

				_log.Info("[BaseLevelController] ✅ 全局输入服务已初始化并同步当前阶段");
			}
			else
			{
				_log.Warn("[BaseLevelController] ⚠️ GlobalInputController 或 GameplayInputService 不可用");
			}
		}
		catch (Exception ex)
		{
			_log.Warn($"[BaseLevelController] ⚠️ 初始化全局输入服务失败: {ex.Message}");
		}
	}

	/// <summary>
	///     设置当前关卡阶段（统一入口）
	///     <param name="phase">新的关卡阶段</param>
	///     <remarks>
	///         此方法为关卡阶段变更的唯一入口，负责:
	 ///         1. 更新 Data.CurrentPhase 状态
	 ///         2. 同步到全局输入服务(IGlobalGameplayInputService)
	 ///         3. 确保全项目输入阻塞行为一致
	 ///         
	 ///         使用方式:
	 ///         所有组件（UI管理器、规则集成、失败/成功UI等）应通过此方法更新阶段
	 ///         而非直接修改 _currentPhase 字段
	 ///         
	 ///         访问级别: public (允许外部组件如 LevelDefeatUi 调用)
	///     </remarks>
	/// </summary>
	public void SetCurrentPhase(LevelPhase phase)
	{
		if (Data.CurrentPhase == phase)
		{
			return;
		}

		var oldPhase = Data.CurrentPhase;
		Data.CurrentPhase = phase;

		if (_globalInputService != null)
		{
			_globalInputService.SetCurrentPhase(phase);
		}

		_log.Info($"[BaseLevelController] 📊 阶段切换: {oldPhase} → {phase} | 输入状态: {(phase == LevelPhase.Play ? "✅ 启用" : "🚫 禁用")}");
	}

	/// <summary>初始化重构后的组件实例</summary>
	private void InitializeComponents()
	{
		_log.Info("[BaseLevelController] 正在初始化重构后的组件...");

		// ⚠️ 重要：必须直接获取节点，不能通过属性（避免循环依赖）
		Data.BeginPositionNode = GetNodeOrNull<Node2D>("%Begin");
		Data.EndAreaNode = GetNodeOrNull<Area2D>("%End");

		if (Data.BeginPositionNode != null)
		{
			_log.Info($"[BaseLevelController] ✓ Begin 位置节点已设置: {Data.BeginPositionNode.GlobalPosition}");
		}
		else
		{
			_log.Error("[BaseLevelController] ❌ 未找到 %Begin 节点！玩家重生将使用默认位置(0,0)");
		}

		if (Data.EndAreaNode != null)
		{
			_log.Info($"[BaseLevelController] ✓ End 区域节点已设置: {Data.EndAreaNode.Name}");
		}
		else
		{
			_log.Warn("[BaseLevelController] ⚠️ 未找到 %End 节点");
		}

		_uiManager = new LevelUiManagerImpl(
			_uiRouter,
			this,
			() => Data.CurrentPhase,
			phase => SetCurrentPhase(phase),
			() => LevelControllerData.IsBuildPhaseActive,
			value => LevelControllerData.IsBuildPhaseActive = value,
			msg => _log.Info(msg),
			msg => _log.Warn(msg),
			msg => _log.Error(msg),
			msg => _log.Debug(msg)
		);

		_playerManager = new LevelPlayerManagerImpl(
			this,
			Data.BeginPositionNode!,
			Data.EndAreaNode!,
			() => Data.CurrentPhase,
			msg => _log.Info(msg),
			msg => _log.Error(msg),
			msg => _log.Debug(msg)
		);

		_inputController = new LevelInputControllerImpl(
			_stateMachineSystem,
			this,
			() => Data.CurrentPhase,
			msg => _log.Info(msg),
			msg => _log.Debug(msg)
		);

		_rulesIntegration = new LevelRulesIntegrationImpl(
			() => Data.CurrentPhase,
			phase => SetCurrentPhase(phase),
			() => LevelControllerData.IsSuccessPhaseActive,
			value => LevelControllerData.IsSuccessPhaseActive = value,
			msg => _log.Info(msg),
			msg => _log.Error(msg),
			msg => _log.Warn(msg),
			msg => _log.Debug(msg)
		);

		ResetHandler = new PlayerResetHandler(Data);
		ResetHandler.OnPlayerRespawnedCallback = OnPlayerRespawned;

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

		Data.ResetGameCompleted();
	}

	/// <summary>清理阶段标志</summary>
	private void CleanupPhaseFlags()
	{
		LevelControllerData.ResetPhaseFlags();
		_log.Debug("[BaseLevelController] 所有阶段标志已重置");
	}

	/// <summary>
	///     初始化失败区域检测
	///     <para>
	///         连接 defeat 区域的 body_entered 信号
	///         当玩家进入该区域时触发重生逻辑
	 ///         
	 ///         ⚠️ 重要：使用 CallDeferred 延迟信号连接
	 ///         避免 "Can't change this state while flushing queries" 错误
	 ///     </para>
	/// </summary>
	private void InitializeDefeatAreaDetection()
	{
		try
		{
			var defeatArea = DefeatArea;

			if (defeatArea == null)
			{
				_log.Warn("[BaseLevelController] ⚠️ 未找到 %defeat 节点！失败区域检测功能已禁用。");
				_log.Warn("[BaseLevelController] 请确保关卡场景包含 unique_name_in_owner=true 的 defeat Area2D 节点。");
				return;
			}

			// ⚠️ 使用 CallDeferred 延迟信号连接，避免 Godot 4 "flushing queries" 错误
			// 原因：在 OnEnterAsync (async) 中直接连接信号会与 Godot 内部状态刷新冲突
			CallDeferred(MethodName._DeferredConnectDefeatAreaSignal);

			_log.Info($"[BaseLevelController] ✅ 失败区域检测初始化已安排 (节点: {defeatArea.Name}, 位置: {defeatArea.Position})");
			_log.Debug($"[BaseLevelController] Defeat区域碰撞层: {defeatArea.CollisionLayer}, 掩码: {defeatArea.CollisionMask}");
		}
		catch (Exception ex)
		{
			_log.Error($"[BaseLevelController] ❌ 初始化失败区域检测异常: {ex.Message}");
			_log.Error($"[BaseLevelController] 异常类型: {ex.GetType().FullName}");
		}
	}

	/// <summary>
	///     延迟连接 DefeatArea 信号（由 CallDeferred 调用）
	///     <para>
	///         在下一帧执行，避免 "flushing queries" 错误
	///     </para>
	/// </summary>
	private void _DeferredConnectDefeatAreaSignal()
	{
		try
		{
			var defeatArea = DefeatArea;

			if (defeatArea == null || !GodotObject.IsInstanceValid(defeatArea) || !GodotObject.IsInstanceValid(this))
			{
				_log.Warn("[BaseLevelController] ⚠️ DefeatArea 或 Controller 已失效，跳过信号连接");
				return;
			}

			defeatArea.BodyEntered += OnDefeatAreaBodyEntered;
			_log.Debug("[BaseLevelController] ✓ Defeat区域 BodyEntered 信号已连接（延迟）");
		}
		catch (Exception ex)
		{
			_log.Error($"[BaseLevelController] ❌ 延迟连接 DefeatArea 信号异常: {ex.Message}");
		}
	}

	/// <summary>
	///     订阅全局陷阱事件
	///     <para>
	///         委托给 ResetHandler 处理
	///     </para>
	/// </summary>
	private void SubscribeToGlobalTrapEvents()
	{
		try
		{
			ResetHandler?.SubscribeToGlobalTrapEvents();
			_log.Info("[BaseLevelController] ✅ 已成功订阅全局陷阱事件");
		}
		catch (Exception ex)
		{
			_log.Error($"[BaseLevelController] ❌ 订阅全局陷阱事件异常: {ex.Message}");
			_log.Error($"[BaseLevelController] 异常类型: {ex.GetType().FullName}");
		}
	}

	/// <summary>
	///     取消订阅全局陷阱事件
	///     <para>
	///         委托给 ResetHandler 处理
	///     </para>
	/// </summary>
	private void UnsubscribeFromGlobalTrapEvents()
	{
		try
		{
			ResetHandler?.UnsubscribeFromGlobalTrapEvents();
			_log.Info("[BaseLevelController] ✓ 已成功取消订阅全局陷阱事件");
		}
		catch (Exception ex)
		{
			_log.Error($"[BaseLevelController] ❌ 取消订阅全局陷阱事件异常: {ex.Message}");
		}
	}

	/// <summary>
	///     当物体进入失败区域时的回调
	///     <param name="body">进入区域的物理实体</param>
	///     <remarks>
	///         触发条件:
	///         - 物体与 defeat Area2D 发生碰撞
	///         - Godot 自动调用此回调
	///         
	///         判断逻辑:
	///         1. 检查 body 是否为玩家实例（通过 PlayerInstance 比较）
	///         2. 检查游戏是否已完成（避免重复触发）
	///         3. 检查是否处于游玩阶段（构建阶段不触发）
	///         4. 满足条件后执行重生逻辑
	///         
	///         边界情况:
	///         - 其他物体（如掉落的平台）进入：忽略
	///         - 游戏已完成：忽略
	///         - 构建阶段：忽略
	///         - 玩家为null：记录错误并返回
	///     </remarks>
	/// </summary>
	private void OnDefeatAreaBodyEntered(Node body)
	{
		try
		{
			if (Data.IsGameCompleted)
			{
				_log.Debug("[BaseLevelController] 游戏已完成，忽略失败区域触发");
				return;
			}

			if (Data.CurrentPhase != LevelPhase.Play)
			{
				_log.Debug($"[BaseLevelController] 当前阶段为 {Data.CurrentPhase}，非游玩阶段，忽略失败区域触发");
				return;
			}

			var playerInstance = _playerManager?.PlayerInstance;
			if (playerInstance == null)
			{
				_log.Warn("[BaseLevelController] ⚠️ 玩家实例为空，无法执行重生逻辑");
				return;
			}

			if (!IsPlayerOrChildOfPlayer(body, playerInstance))
			{
				_log.Debug($"[BaseLevelController] 非玩家物体 ({body.Name}) 进入失败区域，忽略");
				return;
			}

			_log.Info("════════════ 玩家进入失败区域 ═══════════");
			_log.Info($"[BaseLevelController] 💀 玩家 {body.Name} 进入 %defeat 区域！");
			
			var bodyNode2D = body as Node2D;
			if (bodyNode2D != null)
			{
				_log.Info($"[BaseLevelController] 当前位置: {bodyNode2D.GlobalPosition}");
			}

			RespawnPlayerToStart();
		}
		catch (Exception ex)
		{
			_log.Error($"[BaseLevelController] ❌ 处理失败区域进入事件异常: {ex.Message}");
			_log.Error($"[BaseLevelController] 堆栈跟踪:\n{ex.StackTrace}");
		}
	}

	/// <summary>
	///     判断节点是否为玩家或玩家的子节点
	///     <param name="node">待检查的节点</param>
	///     <param name="playerNode">玩家节点</param>
	///     <returns>如果是玩家或其子节点返回true，否则false</returns>
	///     <remarks>
	///         设计原因:
	///         玩家可能由多个子节点组成（CharacterBody2D、CollisionShape2D等）
	///         当任意部分进入 defeat 区域都应该触发重生
	///         
	///         实现方式:
	///         通过向上遍历父节点树检查是否到达玩家根节点
	///     </remarks>
	/// </summary>
	private bool IsPlayerOrChildOfPlayer(Node node, Node playerNode)
	{
		if (node == null || playerNode == null) return false;

		var current = node;
		while (current != null)
		{
			if (current == playerNode) return true;
			current = current.GetParent();
		}

		return false;
	}

	/// <summary>
	///     将玩家重置到起点位置
	///     <para>
	///         当玩家进入失败区域、掉落出界等情况下调用
	///         将玩家位置重置到 %Begin 节点的位置
	///     </para>
	///     <remarks>
	///         重置流程:
	///         1. 检查玩家实例和起点是否存在
	///         2. 停止玩家当前的运动状态（清零速度）
	///         3. 将玩家移动到起点位置
	///         4. 记录详细日志用于调试
	///         
	///         错误处理:
	///         - 玩家为null: 记录错误并返回
	///         - 起点为null: 使用默认位置 (0,0)
	///         - CharacterBody2D获取失败: 使用Node2D位置设置
	///         
	///         扩展性:
	///         可在子类中重写此方法实现自定义重生逻辑
	///         （例如：播放死亡动画、扣减生命值等）
	///     </remarks>
	/// </summary>
	protected virtual void RespawnPlayerToStart()
	{
		try
		{
			var playerInstance = _playerManager?.PlayerInstance;
			if (playerInstance == null)
			{
				_log.Error("[BaseLevelController] ❌ 无法重生玩家：玩家实例为空");
				return;
			}

			var beginPosition = Data.BeginPositionNode;
			if (beginPosition == null)
			{
				_log.Error("[BaseLevelController] ❌ 无法重生玩家：起点 %Begin 节点为空");
				return;
			}

			Vector2 targetPosition = beginPosition.GlobalPosition;

			_log.Info($"[BaseLevelController] 🔄 正在将玩家重置到起点...");
			_log.Info($"[BaseLevelController] 目标位置: {targetPosition}");

			ResetHandler?.ExecuteFullPlayerReset(playerInstance);

			_log.Info("[BaseLevelController] ✅ 已完成玩家重置（通过 ResetHandler）");
		}
		catch (Exception ex)
		{
			_log.Error($"[BaseLevelController] ❌ 重生玩家异常: {ex.Message}");
			_log.Error($"[BaseLevelController] 异常类型: {ex.GetType().FullName}");
			_log.Error($"[BaseLevelController] 堆栈跟踪:\n{ex.StackTrace}");
		}
	}

	/// <summary>
	///     玩家重生完成后的回调（可被子类重写）
	///     <param name="player">玩家实例</param>
	///     <param name="respawnPosition">重生位置</param>
	///     <remarks>
	///         用途:
	///         - 子类可重写此方法添加额外逻辑
	///         - 例如：播放音效、显示特效、更新UI等
	///         
	///         默认行为:
	///         仅输出日志记录
	///     </remarks>
	/// </summary>
	protected virtual void OnPlayerRespawned(Node2D player, Vector2 respawnPosition)
	{
		_log.Info($"[BaseLevelController] 🎮 玩家重生完成！位置: {respawnPosition}");
	}

	#endregion

	#region 公开API - 陷阱处理

	/// <summary>
	///     处理陷阱触发事件（由 TrapStatic 信号调用）
	///     <param name="playerNode">被陷阱隐藏的玩家节点</param>
	///     <remarks>
	///         功能说明:
	///         1. 恢复玩家可见性（Visible = true）
	///         2. 重置玩家到起点位置 (%Begin)
	///         3. 重置玩家速度和物理状态
	///         4. 触发 OnPlayerRespawned 回调
	///         
	///         调用时机:
	///         - TrapStatic 发送 TrapTriggered 信号后
	///         - TeachLevel 等子类在需要时也可直接调用
	///         
	///         设计原理:
	///         将陷阱触发的后续处理集中管理，确保:
	///         - 玩家状态一致性（可见性+位置+速度）
	///         - 日志记录完整性
	///         - 子类可重写扩展
	///     </remarks>
	/// </summary>
	public virtual void HandleTrapTriggered(Node playerNode)
	{
		try
		{
			if (Data.IsGameCompleted)
			{
				_log.Debug("[BaseLevelController] 游戏已完成，忽略陷阱触发");
				return;
			}

			_log.Info("══════════ 处理陷阱触发 ═════════");

			ResetHandler?.ExecuteFullPlayerReset(playerNode);

			_log.Info("══════════ 陷阱处理完成 ═════════");
		}
		catch (Exception ex)
		{
			_log.Error($"[BaseLevelController] ❌ 处理陷阱触发异常: {ex.Message}");
			_log.Error($"[BaseLevelController] 堆栈跟踪:\n{ex.StackTrace}");
		}
	}

	/// <summary>查找玩家节点中的 CharacterBody2D</summary>
	private static CharacterBody2D? FindCharacterBodyInPlayer(Node playerNode)
	{
		if (playerNode is CharacterBody2D body) return body;

		return playerNode.GetNodeOrNull<CharacterBody2D>("CharacterBody2D") ??
			   playerNode.GetNodeOrNull<CharacterBody2D>("character_body_2d");
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
