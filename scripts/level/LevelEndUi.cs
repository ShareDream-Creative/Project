using GFramework.Core.Coroutine.Instructions;
using GFramework.Core.Abstractions.State;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.Scene;
using GFramework.Godot.UI;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.core.ui;
using GFrameworkGodotTemplate.scripts.cqrs.game.command;
using GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command.input;
using GFrameworkGodotTemplate.scripts.enums.scene;
using GFrameworkGodotTemplate.scripts.enums.ui;
using GFrameworkGodotTemplate.scripts.enums;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level;

/// <summary>
///     关卡结束UI控制器
///     负责处理关卡结束界面的按钮交互和导航逻辑
///     <para>
///         显示在关卡成功后的结算界面
///         包含"购买"、"下一关"、"返回主菜单"按钮
///         在此界面显示期间，禁用键盘/手柄输入，仅允许鼠标操作
///     </para>
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-12</date>
///     <description>
///         功能特性:
///         - 自主管理所有结算界面按钮事件和导航逻辑
///         - 提供多种游戏结束选项（购买道具、下一关、返回主菜单）
///         - 实现完整的UI生命周期管理
///         - 集成路由服务实现场景/UI切换
///         - 符合GFramework架构规范和单一职责原则
///         
///         设计原则:
///         - UI组件完全自主管理内部逻辑、事件和导航
///         - 控制器只负责决定何时显示此UI
///         - 通过信号或直接调用与外部通信
///         
///         输入限制 (End阶段):
///         ✓ 允许: 鼠标点击按钮
///         ✓ 允许: ESC键打开暂停菜单
///         ✗ 禁止: 键盘/手柄游戏输入
///         
///         Godot配置要求:
///         1. 将此脚本挂载到 level_end_ui.tscn 的根节点
///         2. 确保根节点类型为 Control
///         3. 必须包含以下按钮节点（unique_name_in_owner = true）:
///            - %PurchaseButton ("购买")
///            - %NextLevelButton ("下一关")
///            - %ExitButton ("返回主菜单")
///     </description>
/// </summary>
[ContextAware]
[Log]
public partial class LevelEndUi : Control, IController, IUiPageBehaviorProvider, ISimpleUiPage
{
	#region 信号定义

	/// <summary>请求购买信号</summary>
	[Signal]
	public delegate void PurchaseRequestedEventHandler();

	/// <summary>请求下一关信号</summary>
	[Signal]
	public delegate void NextLevelRequestedEventHandler();

	/// <summary>请求返回主菜单信号</summary>
	[Signal]
	public delegate void MainMenuRequestedEventHandler();

	#endregion

	#region 私有字段

	/// <summary>页面行为实例</summary>
	private IUiPageBehavior? _page;

	/// <summary>状态机系统服务</summary>
	private IStateMachineSystem? _stateMachineSystem;

	/// <summary>UI路由器服务</summary>
	private IUiRouter? _uiRouter;

	/// <summary>场景路由器服务</summary>
	private ISceneRouter? _sceneRouter;

	#endregion

	#region 节点引用（必须与level_end_ui.tscn匹配）

	/// <summary>"购买"按钮</summary>
	private Button? PurchaseButton => GetNodeOrNull<Button>("%PurchaseButton");

	/// <summary>"下一关"按钮</summary>
	private Button? NextLevelButton => GetNodeOrNull<Button>("%NextLevelButton");

	/// <summary>"返回主菜单"按钮</summary>
	private Button? ExitButton => GetNodeOrNull<Button>("%ExitButton");

	#endregion

	#region 公开属性

	/// <summary>Ui Key的字符串形式</summary>
	public static string UiKeyStr => nameof(UiKey.LevelEndUi);

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
		_log.Info("[LevelEndUi] ═══════════ 初始化关卡结束界面 ═══════════");
		_log.Info($"[LevelEndUi] UI Key: {UiKeyStr}");
		
		InitializeServices();
		SetupEventHandlers();
		
		_log.Info("[LevelEndUi] ✓✓✓ 关卡结束界面初始化完成！");
		_log.Info("[LevelEndUi] 当前职责: 自主管理所有按钮事件和导航逻辑");
	}

	/// <summary>处理输入事件（End阶段限制）</summary>
	public override void _Input(InputEvent @event)
	{
		if (!Visible) return;
		
		if (@event.IsActionPressed("ui_cancel"))
		{
			_log.Info("[LevelEndUi] 检测到ESC键，打开暂停菜单...");
			this.SendCommand(new PauseGameWithOpenPauseMenuCommand(new OpenPauseMenuCommandInput()));
			AcceptEvent();
		}
	}

	#endregion

	#region 私有方法 - 服务初始化

	/// <summary>初始化框架服务引用</summary>
	private void InitializeServices()
	{
		_stateMachineSystem = this.GetSystem<IStateMachineSystem>();
		_uiRouter = this.GetSystem<IUiRouter>();
		_sceneRouter = this.GetSystem<ISceneRouter>();
		
		if (_stateMachineSystem != null)
		{
			_log.Debug("[LevelEndUi] ✓ IStateMachineSystem服务已获取");
		}
		else
		{
			_log.Warn("[LevelEndUi] ⚠ IStateMachineSystem服务不可用");
		}
		
		if (_uiRouter != null)
		{
			_log.Debug("[LevelEndUi] ✓ IUiRouter服务已获取");
		}
		else
		{
			_log.Warn("[LevelEndUi] ⚠ IUiRouter服务不可用，导航功能将受限");
		}
		
		if (_sceneRouter != null)
		{
			_log.Debug("[LevelEndUi] ✓ ISceneRouter服务已获取");
		}
		else
		{
			_log.Warn("[LevelEndUi] ⚠ ISceneRouter服务不可用，场景切换将受限");
		}
	}

	#endregion

	#region 私有方法 - 事件处理

	/// <summary>设置事件处理器</summary>
	private void SetupEventHandlers()
	{
		if (PurchaseButton != null)
		{
			PurchaseButton.Pressed += OnPurchaseButtonPressed;
			_log.Debug("[LevelEndUi] PurchaseButton事件已绑定");
		}
		else
		{
			_log.Error("[LevelEndUi] ✗ PurchaseButton未找到 (%PurchaseButton)");
		}
		
		if (NextLevelButton != null)
		{
			NextLevelButton.Pressed += OnNextLevelButtonPressed;
			_log.Debug("[LevelEndUi] NextLevelButton事件已绑定");
		}
		else
		{
			_log.Error("[LevelEndUi] ✗ NextLevelButton未找到 (%NextLevelButton)");
		}
		
		if (ExitButton != null)
		{
			ExitButton.Pressed += OnExitButtonPressed;
			_log.Debug("[LevelEndUi] ExitButton事件已绑定（返回主菜单）");
		}
		else
		{
			_log.Error("[LevelEndUi] ✗ ExitButton未找到 (%ExitButton) - 返回主菜单功能不可用！");
		}
	}

	/// <summary>"购买"按钮点击处理</summary>
	private void OnPurchaseButtonPressed()
	{
		_log.Info("[LevelEndUi] 用户点击'购买'按钮");
		EmitSignal(SignalName.PurchaseRequested);
	}

	/// <summary>"下一关"按钮点击处理 - 核心功能</summary>
	private void OnNextLevelButtonPressed()
	{
		_log.Info("[LevelEndUi] ═══════════ 用户点击'下一关' ═══════════");
		
		var currentLevel = LevelChoose.CurrentGameLevel;
		_log.Info($"[LevelEndUi] 当前关卡: {currentLevel}");
		
		EmitSignal(SignalName.NextLevelRequested);
		
		GoToNextLevelCoroutine().RunCoroutine();
	}

	/// <summary>"返回主菜单"按钮点击处理 - 核心功能</summary>
	private void OnExitButtonPressed()
	{
		_log.Info("[LevelEndUi] ═══════════ 用户点击'返回主菜单' ═══════════");
		EmitSignal(SignalName.MainMenuRequested);
		
		ReturnToMainMenuCoroutine().RunCoroutine();
	}

	#endregion

	#region 私有方法 - 导航逻辑

	/// <summary>
	///     进入下一关协程
	///     <para>
	 ///         完整的导航流程：
	 ///         1. 获取当前关卡并计算下一关
	 ///         2. 验证下一关是否合法
	 ///         3. 更新全局关卡状态
	 ///         4. 重置关卡阶段标志（解除输入限制）
	 ///         5. 切换UI到LevelPrepareUi（准备界面）
	 ///         6. 切换场景到LevelPerpare（底层准备场景）
	 ///     </para>
	 ///     <remarks>
	 ///         设计说明:
	 ///         - 自动递增关卡：Level1 → Level2 → ... → Level5
	 ///         - 如果当前是Level5或LevelTest，则无下一关，提示用户
	 ///         - 复用LevelPrepareUi的"开始构建"流程进入新关卡
	 ///     </remarks>
	 /// </summary>
	private IEnumerator<IYieldInstruction> GoToNextLevelCoroutine()
	{
		_log.Info("[LevelEndUi] ═══════════ 开始进入下一关 ═══════════");
		
		var currentLevel = LevelChoose.CurrentGameLevel;
		_log.Info($"[LevelEndUi] 步骤1/6: 检测当前关卡 → {currentLevel}");
		
		_log.Info("[LevelEndUi] 步骤2/6: 计算下一关...");
		var nextLevel = LevelChoose.GetNextLevel();
		
		if (nextLevel == null)
		{
			_log.Warn("[LevelEndUi] ⚠ 当前已是最后一关或测试关卡，无法继续！");
			_log.Warn($"[LevelEndUi]   当前关卡: {currentLevel}");
			_log.Warn("[LevelEndUi]   建议: 请返回主菜单选择其他关卡");
			
			HandleNoNextLevelAvailable();
			yield break;
		}
		
		_log.Info($"[LevelEndUi] ✓ 找到下一关: {nextLevel.Value}");
		
		_log.Info("[LevelEndUi] 步骤3/6: 更新全局关卡状态...");
		LevelChoose.SetCurrentGameLevel(nextLevel.Value);
		_log.Info($"[LevelEndUi] ✓ 全局关卡已更新为: {LevelChoose.CurrentGameLevel}");
		
		_log.Info("[LevelEndUi] 步骤4/6: 重置关卡阶段标志...");
		BaseLevelController.ResetPhaseFlags();
		
		_log.Info("[LevelEndUi] 步骤5/6: 切换到LevelPrepare UI...");
		yield return LoadLevelPrepareUiAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelEndUi] 步骤6/6: 切换到LevelPerpare场景...");
		yield return SwitchToLevelPerpareSceneAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelEndUi] ✓✓✓ 进入下一关完成！");
		_log.Info($"[LevelEndUi] 当前位置: 关卡准备界面 (LevelPrepareUi + LevelPerpare)");
		_log.Info($"[LevelEndUi] 等待用户点击'开始构建'进入 {LevelChoose.CurrentGameLevel}...");
	}

	/// <summary>处理无下一关可用的情况</summary>
	private void HandleNoNextLevelAvailable()
	{
		_log.Error("[LevelEndUi] ❌ 无法进入下一关！");
		_log.Error("[LevelEndUi] 可能的原因:");
		_log.Error("   • 当前已是第5关（最后一关）");
		_log.Error("   • 当前处于测试关卡");
		_log.Error("   • 关卡枚举值异常");
		
		if (NextLevelButton != null)
		{
			NextLevelButton.Disabled = true;
			_log.Debug("[LevelEndUi] 已禁用'下一关'按钮");
		}
		
		_log.Info("[LevelEndUi] → 重置关卡为默认值 (GameLevel.None)");
		LevelChoose.SetCurrentGameLevel(GameLevel.None);
		
		_log.Info("[LevelEndUi] → 自动返回关卡选择界面...");
		ReturnToLevelChooseCoroutine().RunCoroutine();
	}

	/// <summary>
	///     返回关卡选择界面协程（无下一关时自动调用）
	///     <para>
	 ///         完整的导航流程：
	 ///         1. 重置关卡阶段标志
	 ///         2. 切换UI到LevelChoose（关卡选择UI）
	 ///         3. 切换场景到Choose（底层场景）
	 ///     </para>
	 ///     <remarks>
	 ///         使用场景:
	 ///         - 当前已是最后一关(Level5)或测试关卡(LevelTest)
	 ///         - 关卡枚举值异常或超出范围
	 ///         - 需要重置为默认状态并返回选择界面
	 ///         
	 ///         目标位置:
	 ///         - UI: level_Choose.tscn (LevelChoose.cs)
	 ///         - 场景: choose.tscn (Choose.cs)
	 ///     </remarks>
	 /// </summary>
	private IEnumerator<IYieldInstruction> ReturnToLevelChooseCoroutine()
	{
		_log.Info("[LevelEndUi] ═══════════ 开始返回关卡选择界面 ═══════════");
		_log.Info("[LevelEndUi] 原因: 当前关卡无下一关或枚举值超出范围");
		
		_log.Info("[LevelEndUi] 步骤1/3: 重置关卡状态标志...");
		BaseLevelController.ResetPhaseFlags();
		
		_log.Info("[LevelEndUi] 步骤2/3: 加载LevelChoose UI...");
		yield return LoadLevelChooseUiAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelEndUi] 步骤3/3: 切换到Choose场景...");
		yield return SwitchToChooseSceneAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelEndUi] ✓✓✓ 返回关卡选择界面完成！");
		_log.Info("[LevelEndUi] 当前位置: 关卡选择界面 (LevelChoose + Choose)");
		_log.Info($"[LevelEndUi] 当前关卡已重置为: {LevelChoose.CurrentGameLevel}");
	}

	/// <summary>加载LevelPrepare UI</summary>
	private async Task LoadLevelPrepareUiAsync()
	{
		if (_uiRouter == null)
		{
			_log.Error("[LevelEndUi] ✗ UI路由器不可用，无法加载LevelPrepare UI！");
			return;
		}

		try
		{
			_log.Debug("[LevelEndUi] → 调用 _uiRouter.ReplaceAsync(LevelPrepareUi)");
			_log.Debug("[LevelEndUi]   此操作将:");
			_log.Debug("[LevelEndUi]     • 清除当前UI栈（包括LevelEndUi）");
			_log.Debug("[LevelEndUi]     • 创建并显示LevelPrepareUi实例");
			_log.Debug("[LevelEndUi]     • 显示'开始构建'和'退回'按钮");
			
			await _uiRouter.ReplaceAsync(nameof(UiKey.LevelPrepareUi));
			
			_log.Info("[LevelEndUi] ✓ LevelPrepare UI已加载并显示");
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelEndUi] ❌ 加载LevelPrepare UI失败: {ex.Message}");
			throw;
		}
	}

	/// <summary>切换到LevelPerpare底层场景</summary>
	private async Task SwitchToLevelPerpareSceneAsync()
	{
		if (_sceneRouter == null)
		{
			_log.Error("[LevelEndUi] ✗ 场景路由器不可用，无法切换到LevelPerpare场景！");
			return;
		}

		try
		{
			_log.Debug("[LevelEndUi] → 调用 _sceneRouter.ReplaceAsync(LevelPerpare)");
			_log.Debug("[LevelEndUi]   此操作将:");
			_log.Debug("[LevelEndUi]     • 卸载当前LevelEnd场景");
			_log.Debug("[LevelEndUi]     • 加载level_perpare.tscn场景");
			_log.Debug("[LevelEndUi]     • 作为LevelPrepareUi的底层背景");
			
			await _sceneRouter.ReplaceAsync(nameof(SceneKey.LevelPerpare));
			
			_log.Info("[LevelEndUi] ✓ LevelPerpare场景已加载");
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelEndUi] ❌ 切换LevelPerpare场景失败: {ex.Message}");
			throw;
		}
	}

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
		_log.Info("[LevelEndUi] ═══════════ 开始返回主菜单流程 ═══════════");
		
		_log.Info("[LevelEndUi] 步骤1/3: 重置关卡状态标志...");
		BaseLevelController.ResetPhaseFlags();
		
		_log.Info("[LevelEndUi] 步骤2/3: 切换状态机到MainMenuState...");
		yield return SwitchToMainMenuStateAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelEndUi] 步骤3/3: 加载LevelChoose UI...");
		yield return LoadLevelChooseUiAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelEndUi] 步骤4/4: 切换到Choose场景...");
		yield return SwitchToChooseSceneAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelEndUi] ✓✓✓ 返回主菜单流程完成！");
		_log.Info("[LevelEndUi] 当前位置: 关卡选择界面 (LevelChoose + Choose)");
	}

	/// <summary>切换状态机到MainMenuState</summary>
	private async Task SwitchToMainMenuStateAsync()
	{
		if (_stateMachineSystem == null)
		{
			_log.Warn("[LevelEndUi] ⚠ 状态机系统不可用，跳过状态切换");
			return;
		}

		try
		{
			_log.Debug("[LevelEndUi] → 调用 ChangeToAsync<MainMenuState>()");
			await _stateMachineSystem.ChangeToAsync<MainMenuState>();
			_log.Debug("[LevelEndUi] ✓ 状态切换完成");
		}
		catch (Exception ex)
		{
			_log.Warn($"[LevelEndUi] ⚠ 状态切换异常: {ex.Message}");
		}
	}

	/// <summary>加载LevelChoose UI</summary>
	private async Task LoadLevelChooseUiAsync()
	{
		if (_uiRouter == null)
		{
			_log.Error("[LevelEndUi] ✗ UI路由器不可用，无法加载LevelChoose UI！");
			return;
		}

		try
		{
			_log.Debug("[LevelEndUi] → 调用 _uiRouter.ReplaceAsync(LevelChoose)");
			await _uiRouter.ReplaceAsync(nameof(UiKey.LevelChoose));
			
			_log.Info("[LevelEndUi] ✓ LevelChoose UI已加载并显示");
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelEndUi] ❌ 加载LevelChoose UI失败: {ex.Message}");
			throw;
		}
	}

	/// <summary>切换到Choose底层场景</summary>
	private async Task SwitchToChooseSceneAsync()
	{
		if (_sceneRouter == null)
		{
			_log.Error("[LevelEndUi] ✗ 场景路由器不可用，无法切换到Choose场景！");
			return;
		}

		try
		{
			_log.Debug("[LevelEndUi] → 调用 _sceneRouter.ReplaceAsync(Choose)");
			await _sceneRouter.ReplaceAsync(nameof(SceneKey.Choose));
			
			_log.Info("[LevelEndUi] ✓ Choose场景已加载");
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelEndUi] ❌ 切换Choose场景失败: {ex.Message}");
			throw;
		}
	}

	#endregion
}
