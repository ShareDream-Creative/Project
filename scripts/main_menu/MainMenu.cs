using GFramework.Core.Coroutine.Instructions;
using GFramework.Core.Abstractions.State;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.Scene;
using GFramework.Godot.UI;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.core.ui;
using GFrameworkGodotTemplate.scripts.cqrs.game.command;
using GFrameworkGodotTemplate.scripts.cqrs.menu.command;
using GFrameworkGodotTemplate.scripts.credits;
using GFrameworkGodotTemplate.scripts.enums.scene;
using GFrameworkGodotTemplate.scripts.enums.ui;
using GFrameworkGodotTemplate.scripts.level;
using GFrameworkGodotTemplate.scripts.level.controllers;
using Godot;

namespace GFrameworkGodotTemplate.scripts.main_menu;

/// <summary>
///     主菜单控制器类，继承自Control并实现IController、IUiPageBehaviorProvider和ISimpleUiPage接口
///     负责处理主菜单界面的逻辑和生命周期管理
/// </summary>
[ContextAware]
[Log]
public partial class MainMenu : Control, IController, IUiPageBehaviorProvider, ISimpleUiPage
{
	/// <summary>
	///     页面行为实例的私有字段
	/// </summary>
	private IUiPageBehavior? _page;

	private IStateMachineSystem _stateMachineSystem = null!;

	private IUiRouter _uiRouter = null!;
	
	private ISceneRouter _sceneRouter = null!;
	private Button NewGameButton => GetNode<Button>("%NewGameButton");
	private Button ContinueGameButton => GetNode<Button>("%ContinueGameButton");
	private Button ChooseLevelButton => GetNode<Button>("%ChooseLevelButton");
	private Button OptionsMenuButton => GetNode<Button>("%OptionsMenuButton");
	private Button CreditsButton => GetNode<Button>("%CreditsButton");
	private Button ExitButton => GetNode<Button>("%ExitButton");

	/// <summary>
	///     Ui Key的字符串形式
	/// </summary>
	public static string UiKeyStr => nameof(UiKey.MainMenu);

	/// <summary>
	///     获取页面行为实例，如果不存在则创建新的CanvasItemUiPageBehavior实例
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

	private void SetupEventHandlers()
	{
		// 绑定退出游戏按钮点击事件
		ExitButton.Pressed += () => this.RunCommandCoroutine(new ExitGameCommand());
		// 绑定制作组按钮点击事件
		CreditsButton.Pressed += () =>
		{
			_uiRouter.PushAsync(Credits.UiKeyStr).AsTask().ToCoroutineEnumerator().RunCoroutine();
		};
		OptionsMenuButton.Pressed += () => { this.RunCommandCoroutine(new OpenOptionsMenuCommand()); };
		NewGameButton.Pressed += () =>
		{
			_stateMachineSystem.ChangeToAsync<PlayingState>().ToCoroutineEnumerator().RunCoroutine();
			
			_sceneRouter.ReplaceAsync(nameof(SceneKey.Home)).AsTask().ToCoroutineEnumerator().RunCoroutine();
		};
		
		ChooseLevelButton.Pressed += () =>
		{
			_log.Info("[MainMenu] 开始切换到关卡选择状态...");
			ChooseLevelCoroutine().RunCoroutine();
		};
	}

	/// <summary>
	///     关卡选择导航协程
	/// </summary>
	private IEnumerator<IYieldInstruction> ChooseLevelCoroutine()
	{
		_log.Info("[MainMenu] ═══════════ 开始关卡选择流程 ═══════════");

		// ═══ 阶段1: 状态机驱动（业务逻辑层）═══
		// 目的: 切换应用状态，触发状态生命周期钩子
		// 可靠性: 取决于框架实现（可能不完整）
		// 作用域: 状态管理、全局事件、权限控制
		
		_log.Info("[MainMenu] 阶段1[状态机]: 正在切换到LevelChooseState...");
		_log.Debug("[MainMenu] → 触发LevelChooseState.OnEnterAsync()");
		
		yield return _stateMachineSystem.ChangeToAsync<LevelChooseState>().AsCoroutineInstruction();
		
		_log.Debug("[MainMenu] ✓ 状态切换Task完成");
		_log.Debug("[MainMenu] ⚠️ 注意: OnEnterAsync中的UI操作可能未执行");

		// ═══ 阶段2: 手动保障（功能保证层）═══
		// 目的: 确保目标UI页面被正确加载和显示
		// 可靠性: 100%（直接控制系统）
		// 作用域: UI加载、界面显示、用户交互
		//
		// 为什么需要这一步?
		// 实测发现ChangeToAsync返回后，OnEnterAsync中的UI操作可能:
		//   1. 完全没有执行（fire-and-forget模式）
		//   2. 延迟执行（异步调度问题）
		//   3. 被其他操作覆盖（并发冲突）
		//
		// 因此必须显式调用UI Router来保证功能正确性
		
		_log.Info("[MainMenu] 阶段2[手动保障]: 正在加载LevelChoose UI...");
		_log.Debug("[MainMenu] → 调用 _uiRouter.ReplaceAsync(LevelChoose.UiKeyStr)");
		_log.Debug("[MainMenu] → 此操作100%可靠，将确保关卡选择界面显示");
		
		yield return _uiRouter.ReplaceAsync(LevelChoose.UiKeyStr).AsTask().AsCoroutineInstruction();
		
		_log.Info("[MainMenu] ✓ 阶段2完成: LevelChoose UI已加载并显示");

		// ═══ 阶段3: 场景管理（资源管理层）═══
		// 目的: 加载对应的底层场景
		// 说明: 场景不属于UI层，需独立管理
		
		_log.Info("[MainMenu] 阶段3[场景]: 正在替换为Choose场景...");
		yield return _sceneRouter.ReplaceAsync(nameof(SceneKey.Choose)).AsTask().AsCoroutineInstruction();
		
		_log.Info("[MainMenu] ✓ 阶段3完成: Choose场景已加载");
		_log.Info("[MainMenu] ═══════════ 关卡选择流程完成 ═══════════");
	}
}
