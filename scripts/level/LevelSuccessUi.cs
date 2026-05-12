using GFramework.Core.Coroutine.Instructions;
using GFramework.Core.Abstractions.State;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.Scene;
using GFramework.Godot.UI;
using GFrameworkGodotTemplate.scripts.constants;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.core.ui;
using GFrameworkGodotTemplate.scripts.cqrs.game.command;
using GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command.input;
using GFrameworkGodotTemplate.scripts.enums.scene;
using GFrameworkGodotTemplate.scripts.enums.ui;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level;

/// <summary>
///     关卡成功界面控制器
///     <para>
///         显示在玩家到达终点区域时的胜利界面
///         包含"下一步"、"再玩一次"、"返回主菜单"按钮
///         在此界面显示期间，禁用键盘/手柄输入，仅允许鼠标操作
///     </para>
///     <author>AI Assistant</author>
///     <version>3.0.0</version>
///     <date>2026-05-12</date>
///     <description>
///         功能特性:
///         - 自主管理所有胜利界面按钮事件和导航逻辑
///         - 提供多种游戏结束选项（下一关、重玩、返回主菜单）
///         - 实现完整的UI生命周期管理
///         - 集成路由服务实现场景/UI切换
///         - 符合GFramework架构规范和单一职责原则
///         
///         导航目标:
///         - "返回主菜单" → level_Choose.tscn (LevelChoose UI) + choose.tscn (Choose 场景)
///         
///         设计原则:
///         - UI组件完全自主管理内部逻辑、事件和导航
///         - 控制器只负责决定何时显示此UI
///         - 通过信号或直接调用与控制器通信
///         
///         输入限制 (Success阶段):
///         ✓ 允许: 鼠标点击按钮
///         ✓ 允许: ESC键打开暂停菜单
///         ✗ 禁止: 键盘/手柄游戏输入
///         
///         Godot配置要求:
///         1. 将此脚本挂载到 level_success_ui.tscn 的根节点
///         2. 确保根节点类型为 Control
///         3. 必须包含以下按钮节点（unique_name_in_owner = true）:
///            - %NextButton ("下一步")
///            - %AgainButton ("再玩一次")
///            - %BackButton ("返回主菜单")
///     </description>
/// </summary>
[ContextAware]
[Log]
public partial class LevelSuccessUi : Control, IController, IUiPageBehaviorProvider, ISimpleUiPage
{
	#region 信号定义

	/// <summary>请求下一关信号</summary>
	[Signal]
	public delegate void NextLevelRequestedEventHandler();

	/// <summary>请求重玩信号</summary>
	[Signal]
	public delegate void RetryRequestedEventHandler();

	/// <summary>请求返回主菜单信号</summary>
	[Signal]
	public delegate void MainMenuRequestedEventHandler();

	#endregion

	#region 私有字段

	/// <summary>页面行为实例</summary>
	private IUiPageBehavior? _page;

	/// <summary>BaseLevelController引用</summary>
	private BaseLevelController? _levelController;

	/// <summary>UI路由器服务</summary>
	private IUiRouter? _uiRouter;

	/// <summary>场景路由器服务</summary>
	private ISceneRouter? _sceneRouter;

	/// <summary>状态机系统服务</summary>
	private IStateMachineSystem? _stateMachineSystem;

	#endregion

	#region 节点引用（必须与level_success_ui.tscn匹配）

	/// <summary>"下一步"按钮</summary>
	private Button? NextButton => GetNodeOrNull<Button>("%NextButton");

	/// <summary>"再玩一次"按钮</summary>
	private Button? AgainButton => GetNodeOrNull<Button>("%AgainButton");

	/// <summary>"返回主菜单"按钮</summary>
	private Button? BackButton => GetNodeOrNull<Button>("%BackButton");

	#endregion

	#region 公开属性

	/// <summary>Ui Key的字符串形式</summary>
	public static string UiKeyStr => nameof(UiKey.LevelSuccessUi);

	#endregion

	#region IUiPageBehaviorProvider 接口实现

	/// <summary>
	///     获取页面行为实例
	/// </summary>
	/// <returns>返回IUiPageBehavior类型的页面行为实例</returns>
	public IUiPageBehavior GetPage()
	{
		_page ??= UiPageBehaviorFactory.Create<Control>(this, UiKeyStr, UiLayer.Page);
		return _page;
	}

	#endregion

	#region 生命周期方法

	/// <summary>节点就绪时调用</summary>
	public override void _Ready()
	{
		_log.Info("[LevelSuccessUi] ═══════════ 初始化成功界面 ═══════════");
		_log.Info($"[LevelSuccessUi] UI Key: {UiKeyStr}");
		
		InitializeServices();
		InitializeComponents();
		SetupEventHandlers();
		
		_log.Info("[LevelSuccessUi] ✓✓✓ 成功界面初始化完成！🎉");
		_log.Info("[LevelSuccessUi] 当前职责: 自主管理所有按钮事件和导航逻辑");
	}

	/// <summary>处理输入事件（Success阶段限制）</summary>
	public override void _Input(InputEvent @event)
	{
		if (!Visible) return;
		
		if (@event.IsActionPressed("ui_cancel"))
		{
			_log.Info("[LevelSuccessUi] 检测到ESC键，打开暂停菜单...");
			this.SendCommand(new PauseGameWithOpenPauseMenuCommand(new OpenPauseMenuCommandInput()));
			AcceptEvent();
		}
	}

	#endregion

	#region 私有方法 - 服务初始化

	/// <summary>初始化框架服务引用</summary>
	private void InitializeServices()
	{
		_uiRouter = this.GetSystem<IUiRouter>();
		_sceneRouter = this.GetSystem<ISceneRouter>();
		_stateMachineSystem = this.GetSystem<IStateMachineSystem>();
		
		if (_uiRouter != null)
		{
			_log.Debug("[LevelSuccessUi] ✓ IUiRouter服务已获取");
		}
		else
		{
			_log.Warn("[LevelSuccessUi] ⚠ IUiRouter服务不可用，导航功能将受限");
		}
		
		if (_sceneRouter != null)
		{
			_log.Debug("[LevelSuccessUi] ✓ ISceneRouter服务已获取");
		}
		else
		{
			_log.Warn("[LevelSuccessUi] ⚠ ISceneRouter服务不可用，场景切换将受限");
		}
		
		if (_stateMachineSystem != null)
		{
			_log.Debug("[LevelSuccessUi] ✓ IStateMachineSystem服务已获取");
		}
		else
		{
			_log.Warn("[LevelSuccessUi] ⚠ IStateMachineSystem服务不可用");
		}
	}

	#endregion

	#region 私有方法 - 初始化

	/// <summary>初始化组件和引用</summary>
	private void InitializeComponents()
	{
		FindLevelController();
		LogAvailableButtons();
	}

	/// <summary>查找BaseLevelController</summary>
	private void FindLevelController()
	{
		_log.Info("[LevelSuccessUi] 正在查找BaseLevelController...");
		_log.Debug($"[LevelSuccessUi] 当前节点路径: {GetPath()}");
		_log.Debug($"[LevelSuccessUi] Owner: {(Owner != null ? $"{Owner.Name} ({Owner.GetType().Name})" : "NULL")}");
		
		if (Owner != null)
		{
			_log.Debug("[LevelSuccessUi] 尝试方法1: 通过Owner向上遍历...");
			_levelController = FindParentOfType<BaseLevelController>(Owner);
			
			if (_levelController != null)
			{
				_log.Info("[LevelSuccessUi] ✓ 找到BaseLevelController");
				_log.Debug($"[LevelSuccessUi] 控制器路径: {_levelController.GetPath()}");
				return;
			}
		}

		_log.Debug("[LevelSuccessUi] 尝试方法2: 从当前节点向上遍历...");
		_levelController = FindParentOfType<BaseLevelController>(this);
		
		if (_levelController != null)
		{
			_log.Info("[LevelSuccessUi] ✓ 通过父节点遍历找到BaseLevelController");
			_log.Debug($"[LevelSuccessUi] 控制器路径: {_levelController.GetPath()}");
			return;
		}

		_log.Debug("[LevelSuccessUi] 未找到BaseLevelController（非致命错误）");
	}

	/// <summary>从指定节点开始向上遍历，查找目标类型的父节点</summary>
	private T? FindParentOfType<T>(Node startNode) where T : Node
	{
		var current = startNode;
		var maxDepth = 20;
		var depth = 0;
		
		while (current != null && depth < maxDepth)
		{
			if (current is T target)
			{
				_log.Debug($"[LevelSuccessUi] 在深度{depth}处找到: {current.Name} ({current.GetType().Name})");
				return target;
			}
			
			current = current.GetParent();
			depth++;
		}
		
		if (depth >= maxDepth)
		{
			_log.Warn($"[LevelSuccessUi] 向上遍历超过最大深度({maxDepth})，停止搜索");
		}
		
		return null;
	}

	/// <summary>记录可用的按钮信息</summary>
	private void LogAvailableButtons()
	{
		var hasButtons = false;
		
		if (NextButton != null)
		{
			_log.Info("[LevelSuccessUi] ✓ NextButton已找到 ('下一步')");
			hasButtons = true;
		}
		else
		{
			_log.Warn("[LevelSuccessUi] ⚠ NextButton未找到 (%NextButton)");
		}
		
		if (AgainButton != null)
		{
			_log.Info("[LevelSuccessUi] ✓ AgainButton已找到 ('再玩一次')");
			hasButtons = true;
		}
		else
		{
			_log.Warn("[LevelSuccessUi] ⚠ AgainButton未找到 (%AgainButton)");
		}
		
		if (BackButton != null)
		{
			_log.Info("[LevelSuccessUi] ✓ BackButton已找到 ('返回主菜单') ← 核心功能");
			hasButtons = true;
		}
		else
		{
			_log.Error("[LevelSuccessUi] ✗ BackButton未找到 (%BackButton) - 返回主菜单功能不可用！");
		}
		
		if (!hasButtons)
		{
			_log.Error("[LevelSuccessUi] ✗ 未找到任何按钮节点！");
		}
	}

	#endregion

	#region 私有方法 - 事件处理

	/// <summary>设置事件处理器</summary>
	private void SetupEventHandlers()
	{
		if (NextButton != null)
		{
			NextButton.Pressed += OnNextButtonPressed;
			_log.Debug("[LevelSuccessUi] NextButton事件已绑定");
		}
		
		if (AgainButton != null)
		{
			AgainButton.Pressed += OnAgainButtonPressed;
			_log.Debug("[LevelSuccessUi] AgainButton事件已绑定");
		}
		
		if (BackButton != null)
		{
			BackButton.Pressed += OnBackButtonPressed;
			_log.Debug("[LevelSuccessUi] BackButton事件已绑定（返回主菜单）");
		}
	}

	/// <summary>"下一步"按钮点击处理 - 核心功能</summary>
	private void OnNextButtonPressed()
	{
		_log.Info("[LevelSuccessUi] ═══════════ 用户点击'下一步' ═══════════");
		_log.Info($"[LevelSuccessUi] 当前关卡: {LevelChoose.CurrentGameLevel}");
		EmitSignal(SignalName.NextLevelRequested);
		
		GoToLevelEndCoroutine().RunCoroutine();
	}

	/// <summary>"再玩一次"按钮点击处理 - 核心功能</summary>
	private void OnAgainButtonPressed()
	{
		_log.Info("[LevelSuccessUi] ═══════════ 用户点击'再玩一次' ═══════════");
		_log.Info($"[LevelSuccessUi] 当前关卡: {LevelChoose.CurrentGameLevel}");
		EmitSignal(SignalName.RetryRequested);
		
		RetryLevelCoroutine().RunCoroutine();
	}

	/// <summary>"返回主菜单"按钮点击处理 - 核心功能</summary>
	private void OnBackButtonPressed()
	{
		_log.Info("[LevelSuccessUi] ═══════════ 用户点击'返回主菜单' ═══════════");
		EmitSignal(SignalName.MainMenuRequested);
		
		ReturnToMainMenuCoroutine().RunCoroutine();
	}

	#endregion

	#region 私有方法 - 下一步导航逻辑

	/// <summary>
	///     进入关卡结束界面协程
	///     <para>
	///         完整的导航流程：
	///         1. 重置关卡阶段标志（解除输入限制）
	///         2. 切换UI到LevelEndUi（结算/商店界面）
	 ///         3. 切换场景到LevelEnd（底层结算场景）
	 ///     </para>
	///     <remarks>
	///         设计说明:
	///         - 从成功界面进入结算/商店界面
	///         - 用户可在此购买道具、查看下一关信息或返回主菜单
	///     </remarks>
	/// </summary>
	private IEnumerator<IYieldInstruction> GoToLevelEndCoroutine()
	{
		_log.Info("[LevelSuccessUi] ═══════════ 开始进入关卡结束界面 ═══════════");
		_log.Info($"[LevelSuccessUi] 当前关卡: {LevelChoose.CurrentGameLevel}");
		
		_log.Info("[LevelSuccessUi] 步骤1/3: 重置关卡状态标志...");
		ResetLevelPhaseFlags();
		
		_log.Info("[LevelSuccessUi] 步骤2/3: 切换到LevelEnd UI...");
		yield return LoadLevelEndUiAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelSuccessUi] 步骤3/3: 切换到LevelEnd场景...");
		yield return SwitchToEndSceneAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelSuccessUi] ✓✓✓ 进入关卡结束界面完成！");
		_log.Info($"[LevelSuccessUi] 当前位置: 关卡结束界面 (LevelEndUi + LevelEnd)");
	}

	/// <summary>加载LevelEnd UI</summary>
	private async Task LoadLevelEndUiAsync()
	{
		if (_uiRouter == null)
		{
			_log.Error("[LevelSuccessUi] ✗ UI路由器不可用，无法加载LevelEnd UI！");
			return;
		}

		try
		{
			_log.Debug("[LevelSuccessUi] → 调用 _uiRouter.ReplaceAsync(LevelEndUi)");
			_log.Debug("[LevelSuccessUi]   此操作将:");
			_log.Debug("[LevelSuccessUi]     • 清除当前UI栈（包括LevelSuccessUi）");
			_log.Debug("[LevelSuccessUi]     • 创建并显示LevelEndUi实例");
			_log.Debug("[LevelSuccessUi]     • 显示'购买'、'下一关'、'返回主菜单'按钮");
			
			await _uiRouter.ReplaceAsync(nameof(UiKey.LevelEndUi));
			
			_log.Info("[LevelSuccessUi] ✓ LevelEnd UI已加载并显示");
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelSuccessUi] ❌ 加载LevelEnd UI失败: {ex.Message}");
			throw;
		}
	}

	/// <summary>切换到LevelEnd底层场景</summary>
	private async Task SwitchToEndSceneAsync()
	{
		if (_sceneRouter == null)
		{
			_log.Error("[LevelSuccessUi] ✗ 场景路由器不可用，无法切换到LevelEnd场景！");
			return;
		}

		try
		{
			_log.Debug("[LevelSuccessUi] → 调用 _sceneRouter.ReplaceAsync(LevelEnd)");
			_log.Debug("[LevelSuccessUi]   此操作将:");
			_log.Debug("[LevelSuccessUi]     • 卸载当前关卡场景");
			_log.Debug("[LevelSuccessUi]     • 加载level_end.tscn场景");
			_log.Debug("[LevelSuccessUi]     • 作为LevelEndUi的底层背景");
			
			await _sceneRouter.ReplaceAsync(nameof(SceneKey.LevelEnd));
			
			_log.Info("[LevelSuccessUi] ✓ LevelEnd场景已加载");
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelSuccessUi] ❌ 切换LevelEnd场景失败: {ex.Message}");
			throw;
		}
	}

	#endregion

	#region 私有方法 - 再玩一次导航逻辑

	/// <summary>
	///     再玩一次协程
	///     <para>
	///         完整的重玩流程：
	///         1. 清理当前关卡所有临时数据
	///         2. 重置关卡阶段标志（解除输入限制）
	///         3. 切换UI到LevelPrepareUi（准备界面）
	///         4. 切换场景到LevelPerpare（底层准备场景）
 ///     </para>
	///     <remarks>
	///         设计说明:
	///         - 保留 LevelChoose.CurrentGameLevel 不变（重新玩同一关）
	///         用户在LevelPrepareUi点击"开始构建"后重新进入关卡
	///         - 完全清理上一轮游戏的所有临时状态
 ///     </remarks>
	/// </summary>
	private IEnumerator<IYieldInstruction> RetryLevelCoroutine()
	{
		_log.Info("[LevelSuccessUi] ═══════════ 开始再玩一次流程 ═══════════");
		_log.Info($"[LevelSuccessUi] 目标关卡: {LevelChoose.CurrentGameLevel}");
		
		_log.Info("[LevelSuccessUi] 步骤1/4: 清理当前关卡数据...");
		ClearCurrentLevelData();
		
		_log.Info("[LevelSuccessUi] 步骤2/4: 重置关卡状态标志...");
		ResetLevelPhaseFlags();
		
		_log.Info("[LevelSuccessUi] 步骤3/4: 切换到LevelPrepare UI...");
		yield return LoadLevelPrepareUiAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelSuccessUi] 步骤4/4: 切换到LevelPerpare场景...");
		yield return SwitchToLevelPerpareSceneAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelSuccessUi] ✓✓✓ 再玩一次流程完成！");
		_log.Info($"[LevelSuccessUi] 当前位置: 关卡准备界面 (LevelPrepareUi + LevelPerpare)");
		_log.Info($"[LevelSuccessUi] 等待用户点击'开始构建'重新进入 {LevelChoose.CurrentGameLevel}...");
	}

	/// <summary>
	///     清理当前关卡的所有临时数据
	///     <para>
	///         确保重新开始时不会残留上一轮游戏的状态
	///     </para>
	/// </summary>
	private void ClearCurrentLevelData()
	{
		_log.Info("[LevelSuccessUi] → 清理关卡临时数据...");
		
		try
		{
			if (_levelController != null)
			{
				_log.Debug("[LevelSuccessUi]   • 重置控制器内部状态");
			}
			
			_log.Debug("[LevelSuccessUi]   • 阶段标志将在步骤2中重置");
			_log.Debug("[LevelSuccessUi]   • 玩家数据由新实例自动初始化");
			_log.Debug("[LevelSuccessUi]   • 场景节点将由ReplaceAsync完全卸载");
			
			_log.Info("[LevelSuccessUi] ✓ 关卡数据清理完成");
		}
		catch (Exception ex)
		{
			_log.Warn($"[LevelSuccessUi] ⚠ 数据清理时出现非致命异常: {ex.Message}");
		}
	}

	/// <summary>加载LevelPrepare UI</summary>
	private async Task LoadLevelPrepareUiAsync()
	{
		if (_uiRouter == null)
		{
			_log.Error("[LevelSuccessUi] ✗ UI路由器不可用，无法加载LevelPrepare UI！");
			return;
		}

		try
		{
			_log.Debug("[LevelSuccessUi] → 调用 _uiRouter.ReplaceAsync(LevelPrepareUi)");
			_log.Debug("[LevelSuccessUi]   此操作将:");
			_log.Debug("[LevelSuccessUi]     • 清除当前UI栈（包括LevelSuccessUi）");
			_log.Debug("[LevelSuccessUi]     • 创建并显示LevelPrepareUi实例");
			_log.Debug("[LevelSuccessUi]     • 显示'开始构建'和'退回'按钮");
			
			await _uiRouter.ReplaceAsync(nameof(UiKey.LevelPrepareUi));
			
			_log.Info("[LevelSuccessUi] ✓ LevelPrepare UI已加载并显示");
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelSuccessUi] ❌ 加载LevelPrepare UI失败: {ex.Message}");
			throw;
		}
	}

	/// <summary>切换到LevelPerpare底层场景</summary>
	private async Task SwitchToLevelPerpareSceneAsync()
	{
		if (_sceneRouter == null)
		{
			_log.Error("[LevelSuccessUi] ✗ 场景路由器不可用，无法切换到LevelPerpare场景！");
			return;
		}

		try
		{
			_log.Debug("[LevelSuccessUi] → 调用 _sceneRouter.ReplaceAsync(LevelPerpare)");
			_log.Debug("[LevelSuccessUi]   此操作将:");
			_log.Debug("[LevelSuccessUi]     • 卸载当前关卡场景（完全清理）");
			_log.Debug("[LevelSuccessUi]     • 加载level_perpare.tscn场景");
			_log.Debug("[LevelSuccessUi]     • 作为LevelPrepareUi的底层背景");
			
			await _sceneRouter.ReplaceAsync(nameof(SceneKey.LevelPerpare));
			
			_log.Info("[LevelSuccessUi] ✓ LevelPerpare场景已加载");
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelSuccessUi] ❌ 切换LevelPerpare场景失败: {ex.Message}");
			throw;
		}
	}

	#endregion

	#region 私有方法 - 辅助方法

	/// <summary>重置关卡阶段标志</summary>

	/// <summary>
	///     返回主菜单协程
	///     <para>
	///         完整的导航流程：
	///         1. 重置关卡阶段标志（解除输入限制）
	///         2. 切换UI到LevelChoose（关卡选择界面）
	///         3. 切换场景到Choose（底层场景）
	 ///     </para>
	/// </summary>
	private IEnumerator<IYieldInstruction> ReturnToMainMenuCoroutine()
	{
		_log.Info("[LevelSuccessUi] ═══════════ 开始返回主菜单流程 ═══════════");
		
		_log.Info("[LevelSuccessUi] 步骤1/4: 重置关卡状态标志...");
		ResetLevelPhaseFlags();
		
		_log.Info("[LevelSuccessUi] 步骤2/4: 切换状态机到MainMenuState...");
		yield return SwitchToMainMenuStateAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelSuccessUi] 步骤3/4: 加载LevelChoose UI...");
		yield return LoadLevelChooseUiAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelSuccessUi] 步骤4/4: 切换到Choose场景...");
		yield return SwitchToChooseSceneAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelSuccessUi] ✓✓✓ 返回主菜单流程完成！");
		_log.Info("[LevelSuccessUi] 当前位置: 关卡选择界面 (LevelChoose + Choose)");
	}

	/// <summary>重置关卡阶段标志</summary>
	private void ResetLevelPhaseFlags()
	{
		if (_levelController != null)
		{
			_log.Debug("[LevelSuccessUi] 通过控制器重置标志（推荐方式）");
		}
		else
		{
			_log.Debug("[LevelSuccessUi] 直接重置静态标志（备用方式）");
		}
		
		BaseLevelController.ResetPhaseFlags();
		
		_log.Info("[LevelSuccessUi] ✓ 阶段标志已重置:");
		_log.Info($"[LevelSuccessUi]   - IsBuildPhaseActive = {BaseLevelController.IsBuildPhaseActive}");
		_log.Info($"[LevelSuccessUi]   - IsSuccessPhaseActive = {BaseLevelController.IsSuccessPhaseActive}");
	}

	/// <summary>切换状态机到MainMenuState</summary>
	private async Task SwitchToMainMenuStateAsync()
	{
		if (_stateMachineSystem == null)
		{
			_log.Warn("[LevelSuccessUi] ⚠ 状态机系统不可用，跳过状态切换");
			return;
		}

		try
		{
			_log.Debug("[LevelSuccessUi] → 调用 ChangeToAsync<MainMenuState>()");
			await _stateMachineSystem.ChangeToAsync<MainMenuState>();
			_log.Debug("[LevelSuccessUi] ✓ 状态切换完成");
		}
		catch (Exception ex)
		{
			_log.Warn($"[LevelSuccessUi] ⚠ 状态切换异常: {ex.Message}");
		}
	}

	/// <summary>加载LevelChoose UI</summary>
	private async Task LoadLevelChooseUiAsync()
	{
		if (_uiRouter == null)
		{
			_log.Error("[LevelSuccessUi] ✗ UI路由器不可用，无法加载LevelChoose UI！");
			return;
		}

		try
		{
			_log.Debug("[LevelSuccessUi] → 调用 _uiRouter.ReplaceAsync(LevelChoose)");
			_log.Debug("[LevelSuccessUi]   此操作将:");
			_log.Debug("[LevelSuccessUi]     • 清除当前UI栈（包括LevelSuccessUi）");
			_log.Debug("[LevelSuccessUi]     • 创建并显示LevelChoose实例");
			_log.Debug("[LevelSuccessUi]     • 显示关卡选择按钮（1-5, test）");
			
			await _uiRouter.ReplaceAsync(nameof(UiKey.LevelChoose));
			
			_log.Info("[LevelSuccessUi] ✓ LevelChoose UI已加载并显示");
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelSuccessUi] ❌ 加载LevelChoose UI失败: {ex.Message}");
			throw;
		}
	}

	/// <summary>切换到Choose底层场景</summary>
	private async Task SwitchToChooseSceneAsync()
	{
		if (_sceneRouter == null)
		{
			_log.Error("[LevelSuccessUi] ✗ 场景路由器不可用，无法切换到Choose场景！");
			return;
		}

		try
		{
			_log.Debug("[LevelSuccessUi] → 调用 _sceneRouter.ReplaceAsync(Choose)");
			_log.Debug("[LevelSuccessUi]   此操作将:");
			_log.Debug("[LevelSuccessUi]     • 清除当前关卡场景");
			_log.Debug("[LevelSuccessUi]     • 加载choose.tscn场景");
			_log.Debug("[LevelSuccessUi]     • 作为LevelChoose UI的底层背景");
			
			await _sceneRouter.ReplaceAsync(nameof(SceneKey.Choose));
			
			_log.Info("[LevelSuccessUi] ✓ Choose场景已加载");
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelSuccessUi] ❌ 切换Choose场景失败: {ex.Message}");
			throw;
		}
	}

	#endregion
}
