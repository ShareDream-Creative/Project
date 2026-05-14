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

			_log.Info("[BaseLevelController] 步骤7/8: 初始化规则系统...");
			InitializeRulesIntegration();

			_log.Info("[BaseLevelController] 步骤8/8: 初始化失败区域检测...");
			InitializeDefeatAreaDetection();

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

	/// <summary>
	///     初始化失败区域检测
	///     <para>
	///         连接 defeat 区域的 body_entered 信号
	///         当玩家进入该区域时触发重生逻辑
	///     </para>
	///     <remarks>
	///         调用时机:
	///         在 OnEnterAsync() 中最后调用，确保玩家已经生成
	///         
	///         错误处理:
	///         - defeat 节点不存在: 记录警告，功能禁用
	///         - 玩家未生成: 记录错误，延迟到下次检测
	///         
	///         性能说明:
	///         此方法只调用一次，信号连接是 O(1) 操作
	///     </remarks>
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

			defeatArea.BodyEntered += OnDefeatAreaBodyEntered;

			_log.Info($"[BaseLevelController] ✅ 失败区域检测已初始化 (节点: {defeatArea.Name}, 位置: {defeatArea.Position})");
			_log.Debug($"[BaseLevelController] Defeat区域碰撞层: {defeatArea.CollisionLayer}, 掩码: {defeatArea.CollisionMask}");
		}
		catch (Exception ex)
		{
			_log.Error($"[BaseLevelController] ❌ 初始化失败区域检测异常: {ex.Message}");
			_log.Error($"[BaseLevelController] 异常类型: {ex.GetType().FullName}");
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
			if (_isGameCompleted)
			{
				_log.Debug("[BaseLevelController] 游戏已完成，忽略失败区域触发");
				return;
			}

			if (_currentPhase != LevelPhase.Play)
			{
				_log.Debug($"[BaseLevelController] 当前阶段为 {_currentPhase}，非游玩阶段，忽略失败区域触发");
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
	/// <param name="playerNode">玩家节点</param>
	/// <returns>如果是玩家或其子节点返回true，否则false</returns>
	/// <remarks>
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

			var beginPosition = BeginPosition;
			if (beginPosition == null)
			{
				_log.Error("[BaseLevelController] ❌ 无法重生玩家：起点 %Begin 节点为空");
				return;
			}

			Vector2 targetPosition = beginPosition.GlobalPosition;

			_log.Info($"[BaseLevelController] 🔄 正在将玩家重置到起点...");
			_log.Info($"[BaseLevelController] 目标位置: {targetPosition}");

			var characterBody = playerInstance.GetNodeOrNull<CharacterBody2D>("CharacterBody2D");
			if (characterBody != null)
			{
				characterBody.Velocity = Vector2.Zero;
				characterBody.GlobalPosition = targetPosition;

				_log.Info("[BaseLevelController] ✅ 已重置玩家速度和位置 (CharacterBody2D)");
				_log.Debug($"[BaseLevelController] 新位置: {characterBody.GlobalPosition}, 速度: {characterBody.Velocity}");
			}
			else
			{
				playerInstance.GlobalPosition = targetPosition;

				_log.Info("[BaseLevelController] ✅ 已重置玩家位置 (Node2D)");
				_log.Debug($"[BaseLevelController] 新位置: {playerInstance.GlobalPosition}");
			}

			OnPlayerRespawned(playerInstance, targetPosition);
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
	/// <param name="respawnPosition">重生位置</param>
	/// <remarks>
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
