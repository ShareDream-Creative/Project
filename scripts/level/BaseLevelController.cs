using GFramework.Core.Abstractions.State;
using GFramework.Core.Coroutine.Instructions;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.Scene;
using GFramework.Godot.UI;
using GFrameworkGodotTemplate.scripts.core.scene;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.core.ui;
using GFrameworkGodotTemplate.scripts.cqrs.game.command;
using GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command.input;
using GFrameworkGodotTemplate.scripts.enums.ui;
using GFrameworkGodotTemplate.scripts.enums;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level;

/// <summary>
///     通用关卡场景控制器基类
///     <para>
///         可挂载到任意level场景的通用脚本系统，提供完整的关卡游戏流程管理
///     </para>
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-12</date>
///     <description>
///         核心功能:
///         1. 场景初始化：在Begin位置自动实例化玩家角色
///         2. UI状态机：Build→Play→Success三阶段UI切换
///         3. 输入控制：构建阶段禁用键盘（除ESC），游玩阶段恢复全部输入
///         4. 游戏流程：End区域检测触发成功界面
///         5. 暂停系统：ESC键打开暂停菜单
///         
///         设计模式:
///         - 模板方法模式：定义完整的关卡流程骨架，子类可重写特定步骤
///         - 状态模式：使用LevelPhase枚举管理UI和输入状态
///         - 观察者模式：通过信号检测玩家到达终点
///         
///         使用方式:
///         1. 将此脚本挂载到关卡场景的根节点
///         2. 在场景中添加名为"Begin"的Node2D作为玩家出生点
///         3. 在场景中添加名为"End"的Area2D作为终点区域
///         4. 确保player.tscn路径正确配置
///     </description>
///     <remarks>
///         继承要求:
///         - 必须继承此基类以获得完整的关卡功能
///         - 可重写虚拟方法自定义特定行为
///         - 遵循GFramework架构规范和ISimpleScene接口契约
///         
///         扩展点:
///         - OnPlayerSpawned(): 玩家生成后的自定义逻辑
///         - OnPhaseChanged(): UI阶段切换时的自定义逻辑
///         - OnGameComplete(): 游戏完成时的自定义逻辑
///     </remarks>
/// </summary>
[ContextAware]
[Log]
public partial class BaseLevelController : Node2D, IController, ISceneBehaviorProvider, ISimpleScene
{
	#region 枚举定义

	/// <summary>
	///     关卡阶段枚举，定义UI和输入控制的三个主要阶段
	/// </summary>
	public enum LevelPhase
	{
		/// <summary>构建阶段：显示BuildUI，禁用键盘输入</summary>
		Build,

		/// <summary>游玩阶段：显示PlayUI，恢复全部输入</summary>
		Play,

		/// <summary>成功阶段：显示SuccessUI，禁用全部输入</summary>
		Success
	}

	#endregion

	#region 私有字段

	/// <summary>场景行为实例</summary>
	protected ISceneBehavior? _scene;

	/// <summary>状态机系统引用</summary>
	private IStateMachineSystem? _stateMachineSystem;

	/// <summary>UI路由器引用</summary>
	private IUiRouter? _uiRouter;

	/// <summary>当前关卡阶段状态</summary>
	private LevelPhase _currentPhase = LevelPhase.Build;

	/// <summary>玩家角色实例引用</summary>
	private Node2D? _playerInstance;

	/// <summary>是否已完成游戏</summary>
	private bool _isGameCompleted;

	/// <summary>玩家场景资源路径</summary>
	private const string PlayerScenePath = "res://scenes/player/player.tscn";

	/// <summary>关卡Build UI场景路径（备用直接加载）</summary>
	private const string LevelBuildUiScenePath = "res://scenes/level/level_ui/level_build_ui.tscn";


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
	///     <para>
	///         用于PlayerMovementController查询以禁止物理移动
	///         在Build阶段为true，其他阶段为false
	 ///     </para>
	///     <remarks>
	///         使用场景:
	///         - PlayerStateController.UpdateState()检查此标志
	///         - Build阶段禁止玩家移动，即使状态机处于PlayingState
	///         
	///         线程安全: 仅主线程访问，无需同步
	///         生命周期: 随关卡场景存在，场景切换时重置
	///     </remarks>
	/// </summary>
	public static bool IsBuildPhaseActive { get; private set; }

	/// <summary>
	///     静态标志：当前是否处于关卡成功阶段
	///     <para>
	///         用于PlayerMovementController查询以禁止物理移动
	///         在Success阶段（显示SuccessUI时）为true，其他阶段为false
	///     </para>
	///     <remarks>
	///         使用场景:
	///         - PlayerStateController.UpdateState()检查此标志
	///         - Success阶段禁止玩家移动，即使状态机处于PlayingState
	///         - 允许鼠标点击UI按钮（Next、Again、Back等）
	///         
	///         线程安全: 仅主线程访问，无需同步
	///         生命周期: 随关卡场景存在，场景切换或重玩时重置
	///         
	///         触发时机:
	///         - 设置为true: ShowSuccessUiCoroutine()中显示成功界面时
	///         - 设置为false: 场景退出(OnExitAsync)或重新开始游戏时
	///     </remarks>
	/// </summary>
	public static bool IsSuccessPhaseActive { get; private set; }

	/// <summary>
	///     重置所有关卡阶段标志
	///     <para>
	///         将 IsBuildPhaseActive 和 IsSuccessPhaseActive 都设置为 false
	///         用于返回主菜单或重新开始游戏时解除输入限制
	///     </para>
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
	///     当场景被激活并显示时自动调用
	/// </summary>
	public async ValueTask OnEnterAsync()
	{
		_log.Info("════════════ 场景进入事件触发 ═══════════");
		_log.Info($"[BaseLevelController] 当前时间: {Time.GetTimeStringFromSystem()}");
		_log.Info($"[BaseLevelController] 场景路径: {SceneFilePath ?? "未知"}");
		
		try
		{
			_log.Info("[BaseLevelController] 步骤1/6: 初始化服务引用...");
			InitializeServices();
			
			_log.Info("[BaseLevelController] 步骤2/6: 确保游戏状态...");
			await EnsurePlayingStateAsync();
			
			_log.Info("[BaseLevelController] 步骤3/6: 清理残留UI（防止重复键错误）...");
			await ClearExistingUiAsync();
			
			_log.Info("[BaseLevelController] 步骤4/6: 生成玩家角色...");
			SpawnPlayer();
			
			_log.Info("[BaseLevelController] 步骤5/6: 设置终点检测...");
			SetupEndAreaDetection();
			
			_log.Info("[BaseLevelController] 步骤6/6: 加载构建界面...");
			await ShowBuildUiAsync();
			
			_log.Info("════════════ ✅ 场景初始化完成 ═══════════");
			_log.Info($"[BaseLevelController] 当前阶段: {_currentPhase}");
			_log.Info($"[BaseLevelController] 玩家状态: {(_playerInstance != null ? "已生成" : "未生成")}");
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
		Cleanup();
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
	///     用于在玩家实例化后执行额外的初始化操作
	/// </summary>
	/// <param name="player">生成的玩家节点实例</param>
	protected virtual void OnPlayerSpawned(Node2D player)
	{
		_log.Debug($"[BaseLevelController] 玩家已生成: {player.Name}");
	}

	/// <summary>
	///     UI阶段切换时的自定义逻辑（子类可重写）
	///     用于在阶段转换时执行特定的业务逻辑
	/// </summary>
	/// <param name="oldPhase">旧阶段</param>
	/// <param name="newPhase">新阶段</param>
	protected virtual void OnPhaseChanged(LevelPhase oldPhase, LevelPhase newPhase)
	{
		_log.Info($"[BaseLevelController] 阶段切换: {oldPhase} → {newPhase}");
	}

	/// <summary>
	///     游戏完成时的自定义逻辑（子类可重写）
	///     用于在玩家到达终点后执行额外的结束逻辑
	/// </summary>
	protected virtual void OnGameCompleted()
	{
		_log.Info("[BaseLevelController] 🎉 游戏完成！");
	}

	#endregion

	#region 私有方法 - 初始化

	/// <summary>初始化服务引用</summary>
	private void InitializeServices()
	{
		_stateMachineSystem = this.GetSystem<IStateMachineSystem>();
		_uiRouter = this.GetSystem<IUiRouter>();
		
		_log.Debug("[BaseLevelController] 服务引用已初始化");
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

	/// <summary>生成玩家角色</summary>
	private void SpawnPlayer()
	{
		_log.Info("[BaseLevelController] 正在生成玩家角色...");
		
		var playerScene = GD.Load<PackedScene>(PlayerScenePath);
		if (playerScene == null)
		{
			_log.Error($"[BaseLevelController] 无法加载玩家场景: {PlayerScenePath}");
			return;
		}

		_playerInstance = playerScene.Instantiate<Node2D>();
		
		if (BeginPosition != null)
		{
			_playerInstance.GlobalPosition = BeginPosition.GlobalPosition;
			_log.Debug($"[BaseLevelController] 玩家位置设置为: {BeginPosition.GlobalPosition}");
		}
		
		AddChild(_playerInstance);
		
		_log.Info("[BaseLevelController] ✓ 玩家角色已生成并添加到场景");
		
		OnPlayerSpawned(_playerInstance);
	}

	/// <summary>设置终点区域检测</summary>
	private void SetupEndAreaDetection()
	{
		_log.Info("[BaseLevelController] 正在设置终点区域检测...");
		
		if (EndArea == null)
		{
			_log.Error("[BaseLevelController] ✗ 未找到End区域节点（%End），游戏完成检测将不可用");
			_log.Error("[BaseLevelController] 请确保场景中存在名为'End'的Area2D节点，并设置了unique_name_in_owner = true");
			return;
		}

		EndArea.BodyEntered += OnPlayerEnteredEndArea;
		_log.Info("[BaseLevelController] ✓ End区域检测已成功设置");
		_log.Debug($"[BaseLevelController] End区域位置: {EndArea.GlobalPosition}");
	}

	#endregion

	#region 私有方法 - UI管理

	/// <summary>
	///     连接LevelBuildUi的BuildFinished信号
	///     <para>
	///         作为直接调用的备用方案，确保UI切换一定能触发
	///         当LevelBuildUi无法找到BaseLevelController引用时
	///         通过信号方式通知控制器切换到游玩阶段
	///     </para>
	/// </summary>
	private void ConnectBuildFinishedSignal()
	{
		_log.Info("[BaseLevelController] 正在连接BuildFinished信号...");
		
		try
		{
			var buildUi = FindLevelBuildUi();
			
			if (buildUi == null)
			{
				_log.Warn("[BaseLevelController] ⚠ 未找到LevelBuildUi节点，跳过信号连接");
				_log.Warn("[BaseLevelController] 将依赖LevelBuildUi的直接调用方式");
				return;
			}

			if (buildUi.IsConnected(LevelBuildUi.SignalName.BuildFinished, Callable.From(OnBuildFinished)))
			{
				_log.Debug("[BaseLevelController] BuildFinished信号已连接（跳过重复连接）");
				return;
			}

			buildUi.Connect(LevelBuildUi.SignalName.BuildFinished, Callable.From(OnBuildFinished));
			
			_log.Info("[BaseLevelController] ✓✓✓ BuildFinished信号已成功连接");
			_log.Info("[BaseLevelController] 双重保障机制已激活：直接调用 + 信号通知");
		}
		catch (Exception ex)
		{
			_log.Warn($"[BaseLevelController] ⚠ 连接BuildFinished信号时出现异常: {ex.Message}");
			_log.Debug($"[BaseLevelController] 异常类型: {ex.GetType().Name}");
		}
	}

	/// <summary>在场景树中查找LevelBuildUi节点</summary>
	private LevelBuildUi? FindLevelBuildUi()
	{
		_log.Debug("[BaseLevelController] 正在场景树中查找LevelBuildUi...");
		
		// 方法1: 在当前节点的子节点中直接查找
		var directChild = FindNodeOfType<LevelBuildUi>(this);
		if (directChild != null)
		{
			_log.Debug($"[BaseLevelController] 在子节点中找到: {directChild.GetPath()}");
			return directChild;
		}

		// 方法2: 延迟一帧后再次查找（等待UiRouter完成加载）
		_log.Debug("[BaseLevelController] 未在静态子节点中找到，尝试延迟查找...");
		
		// 使用CallDeferred延迟到下一帧执行
		Callable.From(() => 
		{
			var deferredResult = FindLevelBuildUiDeferred();
			if (deferredResult != null)
			{
				_log.Info($"[BaseLevelController] ✓✓✓ 延迟查找成功找到LevelBuildUi: {deferredResult.GetPath()}");
				ConnectBuildFinishedSignalToInstance(deferredResult);
			}
			else
			{
				_log.Warn("[BaseLevelController] ⚠ 延迟查找仍未找到LevelBuildUi");
			}
		}).CallDeferred();
		
		return null;
	}

	/// <summary>延迟查找LevelBuildUi（由CallDeferred调用）</summary>
	private LevelBuildUi? FindLevelBuildUiDeferred()
	{
		_log.Debug("[BaseLevelController] 执行延迟查找LevelBuildUi...");
		
		// 方法1: 在ui_page组中查找
		foreach (var node in GetTree().GetNodesInGroup("ui_page"))
		{
			if (node is LevelBuildUi buildUi)
			{
				_log.Debug($"[BaseLevelController] 在ui_page组中找到: {buildUi.GetPath()}");
				return buildUi;
			}
		}

		// 方法2: 从根节点开始递归查找
		foreach (var child in GetTree().Root.GetChildren())
		{
			var result = FindNodeOfType<LevelBuildUi>(child);
			if (result != null)
			{
				_log.Debug($"[BaseLevelController] 在场景树中找到: {result.GetPath()}");
				return result;
			}
		}

		_log.Debug("[BaseLevelController] 延迟查找未找到LevelBuildUi");
		return null;
	}

	/// <summary>连接信号到特定实例</summary>
	private void ConnectBuildFinishedSignalToInstance(LevelBuildUi buildUi)
	{
		try
		{
			if (buildUi.IsConnected(LevelBuildUi.SignalName.BuildFinished, Callable.From(OnBuildFinished)))
			{
				_log.Debug("[BaseLevelController] BuildFinished信号已连接（跳过重复连接）");
				return;
			}

			buildUi.Connect(LevelBuildUi.SignalName.BuildFinished, Callable.From(OnBuildFinished));
			
			_log.Info("[BaseLevelController] ✓✓✓ BuildFinished信号已成功连接（延迟模式）");
			_log.Info("[BaseLevelController] 双重保障机制已激活：直接调用 + 信号通知");
		}
		catch (Exception ex)
		{
			_log.Warn($"[BaseLevelController] ⚠ 连接信号时异常: {ex.Message}");
		}
	}

	/// <summary>递归查找指定类型的节点</summary>
	private T? FindNodeOfType<T>(Node root) where T : Node
	{
		if (root is T target)
		{
			return target;
		}

		foreach (var child in root.GetChildren())
		{
			var result = FindNodeOfType<T>(child);
			if (result != null)
			{
				return result;
			}
		}

		return null;
	}

	/// <summary>
	///     清除现有UI界面（防止HomeUi等残留）
	///     <para>
	///         此方法解决以下问题：
	///         1. MainMenu重复键异常
	///         2. 前一场景的UI未正确卸载
	///         3. UI路由栈中残留旧界面
	///     </para>
	/// </summary>
	private async Task ClearExistingUiAsync()
	{
		if (_uiRouter == null)
		{
			_log.Warn("[BaseLevelController] UI路由器为空，跳过UI清理");
			return;
		}

		try
		{
			_log.Info("════════════ 清理现有UI ═══════════");
			_log.Info("[BaseLevelController] 步骤1: 开始清除所有现有UI...");
			
			var clearTask = _uiRouter.ClearAsync().AsTask();
			await clearTask;
			
			_log.Debug("[BaseLevelController] ✓ UiRouter.ClearAsync() 完成");
			
			_log.Info("[BaseLevelController] 步骤2: 等待UI系统稳定（0.3秒）...");
			await Task.Delay(300);
			
			_log.Info("[BaseLevelController] ✓✓✓ 现有UI已完全清除");
			_log.Info("════════════ UI清理完成 ═══════════");
		}
		catch (Exception ex)
		{
			_log.Warn($"[BaseLevelController] ⚠ 清除UI时出现异常: {ex.Message}");
			_log.Debug($"[BaseLevelController] 异常类型: {ex.GetType().Name}");
			
			if (ex.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
				ex.Message.Contains("same key", StringComparison.OrdinalIgnoreCase))
			{
				_log.Error("[BaseLevelController] ❌ 检测到重复键错误！这通常意味着：");
				_log.Error("  1. 前一个场景的UI未正确卸载");
				_log.Error("  2. 场景切换时资源未完全释放");
				_log.Error("  3. UI路由器状态不一致");
				_log.Error("");
				_log.Error("【建议】尝试重启游戏或重新打开项目");
			}
		}
	}

	/// <summary>
	///     显示构建界面（异步协程）
	///     <para>
	///         核心功能：
	///         1. 通过UI路由器加载LevelBuildUi
	///         2. 设置构建阶段状态
	///         3. 绑定FinishButton事件
	///         4. 启用严格输入限制模式
	///         
	///         错误处理：
	///         - UI未注册：提供详细配置指南
	 ///         - UI加载失败：自动降级到Play模式
	///     </para>
	/// </summary>
	private async Task ShowBuildUiAsync()
	{
		_log.Info("════════════ 显示构建界面 ═══════════");
		_log.Info("[BaseLevelController] 正在加载 LevelBuildUi...");
		
		if (_uiRouter == null)
		{
			_log.Error("[BaseLevelController] ✗ 致命错误：UI路由器未初始化！");
			_log.Error("[BaseLevelController] 请检查：");
			_log.Error("  1. GFramework是否正确初始化");
			_log.Error("  2. IUiRouter服务是否已注册");
			_log.Error("  3. GameEntryPoint配置是否完整");
			
			_currentPhase = LevelPhase.Play;
			return;
		}

		try
		{
			var uiKeyName = nameof(UiKey.LevelBuildUi);
			_log.Debug($"[BaseLevelController] UI Key: {uiKeyName}");
			_log.Debug($"[BaseLevelController] 尝试通过 UiRouter.PushAsync() 加载...");
			
			await _uiRouter.PushAsync(uiKeyName).AsTask();
			
			_currentPhase = LevelPhase.Build;
			IsBuildPhaseActive = true;
			
			_log.Info("[BaseLevelController] ✓✓✓ LevelBuildUi 加载成功！");
			_log.Info("[BaseLevelController] 当前阶段: Build（输入限制已启用）");
			_log.Info($"[BaseLevelController] ⚠ IsBuildPhaseActive = {IsBuildPhaseActive}（移动限制已激活）");
			_log.Info("[BaseLevelController] 允许的操作: 鼠标点击 + ESC键");
			_log.Info("[BaseLevelController] 禁止的操作: 键盘/手柄输入");

			ConnectBuildFinishedSignal();

			OnPhaseChanged(LevelPhase.Build, _currentPhase);
		}
		catch (Exception ex) when (ex.Message.Contains("not registered", StringComparison.OrdinalIgnoreCase))
		{
			_log.Error("╔══════════════════════════════════════════╗");
			_log.Error("║  ❌ 致命错误：UI Key 未注册！              ║");
			_log.Error("╚══════════════════════════════════════════╝");
			_log.Error($"[BaseLevelController] 错误详情: {ex.Message}");
			_log.Error("");
			_log.Error("【解决方案 - 必须在Godot编辑器中完成以下配置】");
			_log.Error("");
			_log.Error("步骤1: 打开场景 scenes/main/GameEntryPoint.tscn");
			_log.Error("步骤2: 选择根节点 GameEntryPoint");
			_log.Error("步骤3: 在检查器中找到 'Ui Page Configs' 属性");
			_log.Error("步骤4: 找到或新增 UiPageConfig，设置以下值：");
			_log.Error("");
			_log.Error("  ┌─ 索引 8 (LevelBuildUi) ─┐");
			_log.Error("  │ Ui Key   : LevelBuildUi  │");
			_log.Error("  │ Scene    : [点击打开场景] │");
			_log.Error("  │          : 选择           │");
			_log.Error("  │  scenes/level/level_ui/   │");
			_log.Error("  │  level_build_ui.tscn      │");
			_log.Error("  └───────────────────────────┘");
			_log.Error("");
			_log.Error("  ┌─ 索引 9 (LevelPlayUi) ──┐");
			_log.Error("  │ Ui Key   : LevelPlayUi   │");
			_log.Error("  │ Scene    : [点击打开场景] │");
			_log.Error("  │          : 选择           │");
			_log.Error("  │  scenes/level/level_ui/   │");
			_log.Error("  │  level_play_ui.tscn       │");
			_log.Error("  └───────────────────────────┘");
			_log.Error("");
			_log.Error("  ┌─ 索引 10 (LevelSuccessUi) ┐");
			_log.Error("  │ Ui Key   : LevelSuccessUi│");
			_log.Error("  │ Scene    : [点击打开场景] │");
			_log.Error("  │          : 选择           │");
			_log.Error("  │  scenes/level/level_ui/   │");
			_log.Error("  │  level_success_ui.tscn    │");
			_log.Error("  └───────────────────────────┘");
			_log.Error("");
			_log.Error("步骤5: 保存场景 (Ctrl+S)");
			_log.Error("步骤6: 重新运行游戏");
			_log.Error("");
			_log.Error("【技术细节】");
			_log.Error($"  异常类型: {ex.GetType().FullName}");
			_log.Error($"  堆栈跟踪: {ex.StackTrace}");
			
			_log.Warn("[BaseLevelController] ⚠ UiRouter加载失败，尝试备用方案：直接加载场景...");
			
			if (await TryDirectLoadUiAsync(LevelBuildUiScenePath, "LevelBuildUi"))
			{
				_log.Info("[BaseLevelController] ✓✓✓ 备用方案成功！LevelBuildUi已通过直接加载显示");
				_currentPhase = LevelPhase.Build;
				IsBuildPhaseActive = true;
				
				ConnectBuildFinishedSignal();
				
				OnPhaseChanged(LevelPhase.Build, _currentPhase);
			}
			else
			{
				_currentPhase = LevelPhase.Play;
				_log.Warn("[BaseLevelController] ⚠ 所有加载方案均失败，已自动切换到游玩模式（无UI）");
				_log.Warn("[BaseLevelController] 请检查：");
				_log.Warn("  1. 文件路径是否正确: res://scenes/level/level_ui/level_build_ui.tscn");
				_log.Warn("  2. .tscn文件是否存在且未损坏");
				_log.Warn("  3. GameEntryPoint.tscn是否已保存");
			}
		}
		catch (Exception ex)
		{
			_log.Error("[BaseLevelController] ✗ 显示构建界面时发生未知异常");
			_log.Error($"[BaseLevelController] 异常类型: {ex.GetType().Name}");
			_log.Error($"[BaseLevelController] 异常消息: {ex.Message}");
			_log.Error($"[BaseLevelController] 堆栈跟踪: {ex.StackTrace}");
			
			_log.Warn("[BaseLevelController] ⚠ 尝试备用直接加载方案...");
			
			if (await TryDirectLoadUiAsync(LevelBuildUiScenePath, "LevelBuildUi"))
			{
				_currentPhase = LevelPhase.Build;
				IsBuildPhaseActive = true;
				
				ConnectBuildFinishedSignal();
				
				OnPhaseChanged(LevelPhase.Build, _currentPhase);
			}
			else
			{
				_currentPhase = LevelPhase.Play;
				_log.Warn("[BaseLevelController] ⚠ 由于加载失败，已自动切换到游玩模式");
			}
		}
	}

	/// <summary>
	///     备用方案：直接加载UI场景（绕过UiRouter）
	///     当UiRouter的注册表配置有问题时，使用此方法直接实例化UI
	/// </summary>
	/// <param name="scenePath">UI场景文件路径</param>
	/// <param name="uiName">UI名称（用于日志）</param>
	/// <returns>是否加载成功</returns>
	private async Task<bool> TryDirectLoadUiAsync(string scenePath, string uiName)
	{
		try
		{
			_log.Info($"[BaseLevelController] 🔄 启动备用加载方案: {uiName}");
			_log.Debug($"[BaseLevelController] 场景路径: {scenePath}");
			
			if (!ResourceLoader.Exists(scenePath))
			{
				_log.Error($"[BaseLevelController] ✗ 场景文件不存在: {scenePath}");
				return false;
			}
			
			var packedScene = GD.Load<PackedScene>(scenePath);
			if (packedScene == null)
			{
				_log.Error($"[BaseLevelController] ✗ 无法加载PackedScene: {scenePath}");
				return false;
			}
			
			var uiInstance = packedScene.Instantiate<Control>();
			if (uiInstance == null)
			{
				_log.Error($"[BaseLevelController] ✗ 无法实例化UI节点: {scenePath}");
				return false;
			}
			
			_log.Debug($"[BaseLevelController] UI实例类型: {uiInstance.GetType().Name}");
			
			AddChild(uiInstance);
			
			_log.Info($"[BaseLevelController] ✓✓ 备用方案成功: {uiName} 已添加到场景");
			_log.Info($"[BaseLevelController] UI路径: {uiInstance.GetPath()}");
			
			return true;
		}
		catch (Exception directEx)
		{
			_log.Error($"[BaseLevelController] ✗ 备用加载方案也失败了: {directEx.Message}");
			_log.Error($"[BaseLevelController] 备用方案异常详情: {directEx.StackTrace}");
			return false;
		}
	}

	/// <summary>
	///     公开API: 由LevelBuildUi调用，切换到游玩阶段
	///     <para>
	///         符合单一职责原则：UI自主管理按钮事件，控制器只负责流程切换
	///     </para>
	/// </summary>
	public void OnBuildFinished()
	{
		_log.Info("════════════ 收到构建完成通知 ═══════════");
		_log.Info($"[BaseLevelController] 当前阶段: {_currentPhase}");
		_log.Info($"[BaseLevelController] IsBuildPhaseActive: {IsBuildPhaseActive}");
		_log.Info($"[BaseLevelController] UiRouter状态: {(_uiRouter != null ? "可用" : "NULL")}");
		
		if (_currentPhase != LevelPhase.Build)
		{
			_log.Warn($"[BaseLevelController] ⚠ 当前不在Build阶段（{_currentPhase}），忽略切换请求");
			return;
		}
		
		_log.Info("[BaseLevelController] 开始切换到游玩阶段...");
		
		try
		{
			SwitchToPlayPhaseCoroutine().RunCoroutine();
			_log.Info("[BaseLevelController] ✓ 切换协程已启动");
		}
		catch (Exception ex)
		{
			_log.Error("[BaseLevelController] ❌ 启动切换协程时发生异常");
			_log.Error($"[BaseLevelController] 异常类型: {ex.GetType().Name}");
			_log.Error($"[BaseLevelController] 异常消息: {ex.Message}");
			_log.Error($"[BaseLevelController] 堆栈跟踪: {ex.StackTrace}");
		}
	}

	/// <summary>切换到游玩阶段的协程</summary>
	private IEnumerator<IYieldInstruction> SwitchToPlayPhaseCoroutine()
	{
		_log.Info("[BaseLevelController] ═══════════ 切换到游玩阶段 ═══════════");
		
		var oldPhase = _currentPhase;
		
		_log.Info("[BaseLevelController] 步骤1[关闭BuildUI]: 正在清除构建界面...");
		
		if (_uiRouter == null)
		{
			_log.Error("[BaseLevelController] ❌ UiRouter为NULL，无法清除UI");
			_forceSwitchToPlayPhase();
			yield break;
		}
		
		yield return _uiRouter.ClearAsync().AsTask().AsCoroutineInstruction();
		_log.Info("[BaseLevelController] ✓ BuildUI已关闭");

		_log.Info("[BaseLevelController] 步骤2[显示PlayUI]: 正在加载游玩界面...");
		yield return _uiRouter.PushAsync(nameof(UiKey.LevelPlayUi)).AsTask().AsCoroutineInstruction();
		_log.Info("[BaseLevelController] ✓ PlayUI加载完成");
		
		_currentPhase = LevelPhase.Play;
		IsBuildPhaseActive = false;
		
		_log.Info("[BaseLevelController] ✓ PlayUI已显示");
		_log.Info($"[BaseLevelController] ⚠ IsBuildPhaseActive = {IsBuildPhaseActive}（移动限制已解除）");
		_log.Info("[BaseLevelController] ✓ 输入控制已恢复");
		_log.Info("[BaseLevelController] ═══════════ 游玩阶段开始 ═══════════");
		
		OnPhaseChanged(oldPhase, _currentPhase);
	}

	/// <summary>强制切换到Play阶段（错误恢复）</summary>
	private void _forceSwitchToPlayPhase()
	{
		_log.Warn("[BaseLevelController] ⚠ 执行强制切换到Play阶段（错误恢复模式）");
		_currentPhase = LevelPhase.Play;
		IsBuildPhaseActive = false;
		_log.Warn("[BaseLevelController] ⚠ 状态已强制更新，但UI可能未正确切换");
	}

	/// <summary>显示成功界面的协程</summary>
	private IEnumerator<IYieldInstruction> ShowSuccessUiCoroutine()
	{
		_log.Info("[BaseLevelController] ═══════════ 显示成功界面 ═══════════");
		
		_log.Info("[BaseLevelController] 步骤1[清除当前UI]: 正在清除游玩界面...");
		yield return _uiRouter?.ClearAsync().AsTask().AsCoroutineInstruction();
		_log.Info("[BaseLevelController] ✓ 当前UI已清除");

		_log.Info("[BaseLevelController] 步骤2[显示SuccessUI]: 正在加载成功界面...");
		yield return _uiRouter?.PushAsync(nameof(UiKey.LevelSuccessUi)).AsTask().AsCoroutineInstruction();
		
		_currentPhase = LevelPhase.Success;
		_isGameCompleted = true;
		IsSuccessPhaseActive = true;
		
		_log.Info("[BaseLevelController] ✓✓✓ SuccessUI已成功显示！");
		_log.Info($"[BaseLevelController] ⚠ IsSuccessPhaseActive = {IsSuccessPhaseActive}（移动限制已激活）");
		_log.Info("[BaseLevelController] 允许的操作: 鼠标点击 + ESC键");
		_log.Info("[BaseLevelController] 禁止的操作: 键盘/手柄输入");
		_log.Info("[BaseLevelController] ═══════════ 🎉 恭喜通关！🎉 ═══════════");
		
		OnPhaseChanged(LevelPhase.Play, _currentPhase);
		OnGameCompleted();
	}

	#endregion

	#region 私有方法 - 游戏流程

	/// <summary>玩家进入终点的处理</summary>
	private void OnPlayerEnteredEndArea(Node body)
	{
		if (_isGameCompleted)
		{
			return;
		}

		if (_currentPhase != LevelPhase.Play)
		{
			_log.Debug($"[BaseLevelController] 非游玩阶段忽略碰撞检测, 当前阶段: {_currentPhase}");
			return;
		}

		var bodyName = body.Name.ToString();
		var isPlayer = bodyName.Contains("Player", StringComparison.OrdinalIgnoreCase) ||
					   bodyName.Contains("Character", StringComparison.OrdinalIgnoreCase) ||
					   body is CharacterBody2D;

		if (!isPlayer)
		{
			_log.Debug($"[BaseLevelController] 忽略非玩家物体进入终点: {bodyName} (类型: {body.GetType().Name})");
			return;
		}

		_log.Info($"[BaseLevelController] ✓✓✓ 检测到玩家({bodyName})进入终点区域！类型: {body.GetType().Name}");
		_log.Info("[BaseLevelController] 触发游戏完成流程...");
		
		ShowSuccessUiCoroutine().RunCoroutine();
	}

	#endregion

	#region 私有方法 - 输入控制

	/// <summary>
	///     重写输入处理，实现严格的输入限制策略
	///     <para>
	///         根据约束条件要求，构建阶段必须严格限制用户输入：
	///         ✓ 允许：鼠标操作（点击、移动、滚轮）
	///         ✓ 允许：ESC键（打开暂停菜单）
	///         ✗ 禁止：所有键盘输入（字母、数字、功能键）
	///         ✗ 禁止：所有手柄/控制器输入
	///         ✗ 禁止：Enter/Space等确认键
	///     </para>
	/// </summary>
	public override void _Input(InputEvent @event)
	{
		switch (_currentPhase)
		{
			case LevelPhase.Build:
				HandleBuildPhaseInput(@event);
				break;
			
			case LevelPhase.Play:
				HandlePlayPhaseInput(@event);
				break;
			
			case LevelPhase.Success:
				HandleSuccessPhaseInput(@event);
				break;
		}
	}

	/// <summary>处理构建阶段的输入（严格限制模式）</summary>
	private void HandleBuildPhaseInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			var keyCode = keyEvent.Keycode;
			var physicalKey = keyEvent.PhysicalKeycode;
			
			var isEscape = keyCode == Key.Escape || physicalKey == Key.Escape;
			
			if (isEscape)
			{
				_log.Debug("[BaseLevelController] [Build阶段] ESC键已放行");
				HandleEscapeKeyPress();
				return;
			}
			
			_log.Debug($"[BaseLevelController] [Build阶段] 键盘输入已被拦截: Key={keyCode}, Physical={physicalKey}");
			GetViewport()?.SetInputAsHandled();
			return;
		}
		
		if (@event is InputEventJoypadButton || @event is InputEventJoypadMotion)
		{
			_log.Debug("[BaseLevelController] [Build阶段] 手柄输入已被拦截");
			GetViewport()?.SetInputAsHandled();
			return;
		}
		
		if (@event.IsActionPressed("ui_accept") || 
			@event.IsActionPressed("ui_select") ||
			@event.IsActionPressed("ui_cancel"))
		{
			if (@event.IsActionPressed("ui_cancel"))
			{
				HandleEscapeKeyPress();
				return;
			}
			
			_log.Debug("[BaseLevelController] [Build阶段] UI动作输入已被拦截");
			GetViewport()?.SetInputAsHandled();
			return;
		}
	}

	/// <summary>处理游玩阶段的输入（完全开放）</summary>
	private void HandlePlayPhaseInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			HandleEscapeKeyPress();
		}
	}

	/// <summary>处理成功阶段的输入（完全禁止）</summary>
	private void HandleSuccessPhaseInput(InputEvent @event)
	{
		if (@event is InputEventKey || @event is InputEventJoypadButton || @event is InputEventJoypadMotion)
		{
			GetViewport()?.SetInputAsHandled();
		}
		
		if (@event.IsActionPressed("ui_cancel"))
		{
			HandleEscapeKeyPress();
		}
	}

	/// <summary>处理ESC键按下</summary>
	private void HandleEscapeKeyPress()
	{
		if (_stateMachineSystem?.Current is not PlayingState)
		{
			return;
		}

		_log.Debug("[BaseLevelController] ESC键按下，打开暂停菜单...");
		
		this.SendCommand(new PauseGameWithOpenPauseMenuCommand(new OpenPauseMenuCommandInput()));
		GetViewport()?.SetInputAsHandled();
	}

	#endregion

	#region 私有方法 - 清理

	/// <summary>清理资源</summary>
	private void Cleanup()
	{
		if (EndArea != null)
		{
			EndArea.BodyEntered -= OnPlayerEnteredEndArea;
		}

		_isGameCompleted = false;
		_currentPhase = LevelPhase.Build;
		IsBuildPhaseActive = false;
		IsSuccessPhaseActive = false;
		
		_log.Debug("[BaseLevelController] 资源清理完成，所有阶段标志已重置");
	}

	#endregion
}
