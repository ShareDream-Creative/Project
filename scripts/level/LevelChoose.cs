using GFramework.Core.Coroutine.Instructions;
using GFramework.Core.Abstractions.State;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.UI;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.core.ui;
using GFrameworkGodotTemplate.scripts.cqrs.game.command;
using GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command.input;
using GFrameworkGodotTemplate.scripts.enums.scene;
using GFrameworkGodotTemplate.scripts.enums.ui;
using GFrameworkGodotTemplate.scripts.enums;
using GFrameworkGodotTemplate.scripts.main_menu;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level;

/// <summary>
///     关卡选择UI页面控制器类，继承自Control并实现IController、IUiPageBehaviorProvider和ISimpleUiPage接口
///     负责处理关卡选择界面的逻辑和生命周期管理
/// </summary>
[ContextAware]
[Log]
public partial class LevelChoose : Control, IController, IUiPageBehaviorProvider, ISimpleUiPage
{
	/// <summary>
	///     页面行为实例的私有字段
	/// </summary>
	private IUiPageBehavior? _page;

	private IStateMachineSystem _stateMachineSystem = null!;

	private IUiRouter _uiRouter = null!;

	private ISceneRouter _sceneRouter = null!;

	/// <summary>
	///     当前选中的游戏关卡状态（全局变量）
	///     用于系统判定当前所处的关卡
	/// </summary>
	public static GameLevel CurrentGameLevel { get; private set; } = GameLevel.None;

	/// <summary>
	///     设置当前游戏关卡（公共方法，供外部模块调用）
	///     <para>
	///         使用场景:
	///         - LevelEndUi 的"下一关"功能：自动递增关卡
	///         - 其他需要程序化切换关卡的逻辑
	///     </para>
	/// </summary>
	/// <param name="level">目标关卡枚举值</param>
	public static void SetCurrentGameLevel(GameLevel level)
	{
		CurrentGameLevel = level;
		GD.Print($"[LevelChoose] 全局关卡状态已更新为: {level}");
	}

	/// <summary>
	///     获取下一个合法的关卡
	///     <para>
	///         根据当前关卡返回下一个关卡
	 ///         如果当前已是最后一关(Level5)或测试关卡(LevelTest)，则返回null表示无下一关
	 ///     </para>
	/// </summary>
	/// <returns>下一关的GameLevel值，如果不存在则返回null</returns>
	public static GameLevel? GetNextLevel()
	{
		return CurrentGameLevel switch
		{
			GameLevel.Level1 => GameLevel.Level2,
			GameLevel.Level2 => GameLevel.Level3,
			GameLevel.Level3 => GameLevel.Level4,
			GameLevel.Level4 => GameLevel.Level5,
			_ => null
		};
	}

	private Button BackButton => GetNode<Button>("%BackButton");
	private Button Level1Button => GetNode<Button>("%Level1");
	private Button Level2Button => GetNode<Button>("%Level2");
	private Button Level3Button => GetNode<Button>("%Level3");
	private Button Level4Button => GetNode<Button>("%Level4");
	private Button Level5Button => GetNode<Button>("%Level5");
	private Button LevelTestButton => GetNode<Button>("%LevelTest");

	/// <summary>
	///     UI键值的字符串形式
	/// </summary>
	public static string UiKeyStr => nameof(UiKey.LevelChoose);

	/// <summary>
	///     获取页面行为实例，如果不存在则创建新的实例
	/// </summary>
	/// <returns>返回IUiPageBehavior类型的页面行为实例</returns>
	public IUiPageBehavior GetPage()
	{
		_page ??= UiPageBehaviorFactory.Create<Control>(this, UiKeyStr, UiLayer.Page);
		return _page;
	}

	/// <summary>
	///     节点准备就绪时的回调方法
	///     在节点添加到场景树后调用
	/// </summary>
	public override void _Ready()
	{
		_uiRouter = this.GetSystem<IUiRouter>()!;
		_stateMachineSystem = this.GetSystem<IStateMachineSystem>()!;
		_sceneRouter = this.GetSystem<ISceneRouter>()!;
		SetupEventHandlers();
	}

	public override void _Input(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel") || !Visible) return;

		_log.Info("[LevelChoose] 检测到Esc按键，弹出暂停菜单");
		this.SendCommand(new PauseGameWithOpenPauseMenuCommand(new OpenPauseMenuCommandInput()));
		AcceptEvent();
	}

	private void SetupEventHandlers()
	{
		// 绑定返回按钮点击事件
		BackButton.Pressed += () =>
		{
			_log.Info("[LevelChoose] 开始返回主菜单...");
			ReturnToMainMenuCoroutine().RunCoroutine();
		};
		
		Level1Button.Pressed += () =>
		{
			_log.Info("[LevelChoose] 选择了关卡1"+CurrentGameLevel);
			SelectLevelAndPrepare(GameLevel.Level1);
		};
		
		Level2Button.Pressed += () =>
		{
			_log.Info("[LevelChoose] 选择了关卡2");
			SelectLevelAndPrepare(GameLevel.Level2);
		};
		
		Level3Button.Pressed += () =>
		{
			_log.Info("[LevelChoose] 选择了关卡3");
			SelectLevelAndPrepare(GameLevel.Level3);
		};
		
		Level4Button.Pressed += () =>
		{
			_log.Info("[LevelChoose] 选择了关卡4");
			SelectLevelAndPrepare(GameLevel.Level4);
		};
		
		Level5Button.Pressed += () =>
		{
			_log.Info("[LevelChoose] 选择了关卡5");
			SelectLevelAndPrepare(GameLevel.Level5);
		};
		
		LevelTestButton.Pressed += () =>
		{
			_log.Info("[LevelChoose] 选择了测试关卡");
			SelectLevelAndPrepare(GameLevel.LevelTest);
		};
	}

	/// <summary>
	///     选择关卡并切换到准备界面的方法
	/// </summary>
	/// <param name="level">要选择的关卡枚举值</param>
	private void SelectLevelAndPrepare(GameLevel level)
	{
		// ═══ 步骤1: 更新全局关卡状态 ═══
		CurrentGameLevel = level;
		_log.Info($"[LevelChoose] 全局关卡状态已更新为: {level}");
		_log.Debug("[LevelChoose] → 其他模块可通过 LevelChoose.CurrentGameLevel 获取当前关卡");

		// 启动协程执行UI和场景切换
		NavigateToLevelPrepareCoroutine().RunCoroutine();
	}

	/// <summary>
	///     返回主菜单协程
	/// </summary>
	private IEnumerator<IYieldInstruction> ReturnToMainMenuCoroutine()
	{
		_log.Info("[LevelChoose] ═══════════ 开始返回主菜单流程 ═══════════");

		// ═══ 阶段1: 状态机驱动（业务逻辑层）═══
		// 目的: 从选择状态切换回主状态，触发状态生命周期钩子
		// 可靠性: 取决于框架实现（实测发现不可靠）
		// 作用域: 状态回退、资源清理准备、全局事件
		
		_log.Info("[LevelChoose] 阶段1[状态机]: 正在切换到MainMenuState...");
		_log.Debug("[LevelChoose] → 触发MainMenuState.OnEnterAsync()");
		_log.Debug("[LevelChoose] → OnEnterAsync应该执行:");
		_log.Debug("[LevelChoose]   1. ClearAsync() 清除所有UI");
		_log.Debug("[LevelChoose]   2. ClearAsync() 清除所有场景");
		_log.Debug("[LevelChoose]   3. PushAsync(MainMenu) 加载主菜单");
		
		yield return _stateMachineSystem.ChangeToAsync<MainMenuState>().AsCoroutineInstruction();
		
		_log.Debug("[LevelChoose] ✓ 状态切换Task完成");
		_log.Debug("[LevelChoose] ⚠️ 注意: OnEnterAsync中的UI操作可能未执行！");
		_log.Debug("[LevelChoose] ⚠️ 如果界面空白，说明需要阶段2的手动保障");

		// ═══ 阶段2: 手动保障（功能保证层）═══
		// 目的: 确保MainMenu UI被正确加载并显示
		// 可靠性: 100%（直接控制系统）
		// 作用域: UI替换、界面显示、用户交互恢复
		//
		// 实测证据:
		// 不加此步骤时:
		//   ✅ 日志显示"状态切换成功"
		//   ✅ 场景可以正常替换
		//   ❌ 但界面完全空白（无任何UI元素）
		//   ❌ 控制台无任何UiRouter操作日志
		//
		// 加上此步骤后:
		//   ✅ 所有操作都正常执行
		//   ✅ 主菜单完整显示
		//   ✅ 用户可以正常交互
		
		_log.Info("[LevelChoose] 阶段2[手动保障]: 正在加载MainMenu UI...");
		_log.Debug("[LevelChoose] → 调用 _uiRouter.ReplaceAsync(MainMenu.UiKeyStr)");
		_log.Debug("[LevelChoose] → 此操作将:");
		_log.Debug("[LevelChoose]   • 清除当前UI栈（包括LevelChoose）");
		_log.Debug("[LevelChoose]   • 创建并显示MainMenu实例");
		_log.Debug("[LevelChoose]   • 确保100%可靠的主菜单界面");
		
		yield return _uiRouter.ReplaceAsync(MainMenu.UiKeyStr).AsTask().AsCoroutineInstruction();
		
		_log.Info("[LevelChoose] ✓ 阶段2完成: MainMenu UI已加载并显示");

		// ═══ 阶段3: 场景管理（资源管理层）═══
		// 目的: 切换底层场景为主场景
		// 说明: MainMenuState.OnEnterAsync会Clear场景但不Load新场景
		//       因此需要在此处显式替换
		
		_log.Info("[LevelChoose] 阶段3[场景]: 正在替换为Main场景...");
		yield return _sceneRouter.ReplaceAsync(nameof(SceneKey.Main)).AsTask().AsCoroutineInstruction();
		
		_log.Info("[LevelChoose] ✓ 阶段3完成: Main场景已加载");
		_log.Info("[LevelChoose] ═══════════ 返回主菜单流程完成 ═══════════");
	}

	/// <summary>
	///     导航到关卡准备界面的协程
	/// </summary>
	private IEnumerator<IYieldInstruction> NavigateToLevelPrepareCoroutine()
	{
		_log.Info("[LevelChoose] ═══════════ 开始进入关卡准备界面 ═══════════");
		_log.Info($"[LevelChoose] 当前关卡状态: {CurrentGameLevel}");

		// ═══ 阶段1: UI切换（界面层）═══
		// 目的: 从关卡选择UI切换到关卡准备UI
		// 唯一性保证: ReplaceAsync会清除旧UI，确保只有一个LevelPrepareUi实例
		
		_log.Info("[LevelChoose] 阶段1[UI]: 正在切换到LevelPrepareUi...");
		_log.Debug("[LevelChoose] → 调用 _uiRouter.ReplaceAsync(UiKey.LevelPrepareUi)");
		_log.Debug("[LevelChoose] → 此操作将:");
		_log.Debug("[LevelChoose]   • 清除当前UI栈（包括LevelChoose）");
		_log.Debug("[LevelChoose]   • 创建唯一的LevelPrepareUi实例");
		_log.Debug("[LevelChoose]   • 显示关卡准备界面（开始构建/退回按钮）");
		
		yield return _uiRouter.ReplaceAsync(nameof(UiKey.LevelPrepareUi)).AsTask().AsCoroutineInstruction();
		
		_log.Info("[LevelChoose] ✓ 阶段1完成: LevelPrepareUi已加载并显示");

		// ═══ 阶段2: 场景切换（资源层）═══
		// 目的: 切换到底层关卡准备场景
		// 唯一性保证: ReplaceAsync会清除旧场景，确保只有一个LevelPerpare实例
		
		_log.Info("[LevelChoose] 阶段2[场景]: 正在切换到LevelPerpare场景...");
		_log.Debug("[LevelChoose] → 调用 _sceneRouter.ReplaceAsync(SceneKey.LevelPerpare)");
		_log.Debug("[LevelChoose] → 此操作将:");
		_log.Debug("[LevelChoose]   • 清除当前场景（Choose场景）");
		_log.Debug("[LevelChoose]   • 加载唯一的LevelPerpare场景实例");
		_log.Debug("[LevelChoose]   • 作为LevelPrepareUi的底层背景");
		
		yield return _sceneRouter.ReplaceAsync(nameof(SceneKey.LevelPerpare)).AsTask().AsCoroutineInstruction();
		
		_log.Info("[LevelChoose] ✓ 阶段2完成: LevelPerpare场景已加载");
		_log.Info($"[LevelChoose] ═══════════ 进入关卡{CurrentGameLevel}准备界面完成 ═══════════");
	}
}
