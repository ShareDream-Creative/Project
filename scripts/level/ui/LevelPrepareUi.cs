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
using GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command.input;
using GFrameworkGodotTemplate.scripts.enums.scene;
using GFrameworkGodotTemplate.scripts.enums.ui;
using GFrameworkGodotTemplate.scripts.level;
using GFrameworkGodotTemplate.scripts.enums;
using GFrameworkGodotTemplate.scripts.level.controllers;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level.ui;

/// <summary>
///     关卡准备UI控制器
///     负责处理关卡准备界面的按钮交互和导航逻辑
/// </summary>
[ContextAware]
[Log]
public partial class LevelPrepareUi : Control, IController, IUiPageBehaviorProvider, ISimpleUiPage
{
	#region 私有字段

	/// <summary>
	///     页面行为实例的私有字段
	/// </summary>
	private IUiPageBehavior? _page;

	/// <summary>
	///     状态机系统服务
	/// </summary>
	private IStateMachineSystem? _stateMachineSystem;

	/// <summary>
	///     场景路由器引用
	/// </summary>
	private ISceneRouter? _sceneRouter;

	/// <summary>
	///     UI路由器引用
	/// </summary>
	private IUiRouter? _uiRouter;

	#endregion

	#region 节点引用

	/// <summary>
	///     "开始构建"按钮引用
	///     使用unique_name_in_owner标识，可通过%访问
	/// </summary>
	private Button EnterButton => GetNode<Button>("%EnterButton");

	/// <summary>
	///     "退回"按钮引用
	///     使用unique_name_in_owner标识，可通过%访问
	/// </summary>
	private Button BackButton => GetNode<Button>("%BackButton");

	#endregion

	#region UI键值

	/// <summary>
	///     获取UI键值字符串
	/// </summary>
	public static string UiKeyStr => nameof(UiKey.LevelPrepareUi);

	#endregion

	#region 生命周期方法

	public override void _Ready()
	{
		_stateMachineSystem = this.GetSystem<IStateMachineSystem>();
		_sceneRouter = this.GetSystem<ISceneRouter>();
		_uiRouter = this.GetSystem<IUiRouter>();
		
		if (_stateMachineSystem == null)
		{
			_log.Warn("[LevelPrepareUi] ⚠ IStateMachineSystem服务不可用");
		}
		
		if (_sceneRouter == null)
		{
			_log.Warn("[LevelPrepareUi] ⚠ ISceneRouter服务不可用，场景切换将受限");
		}
		
		if (_uiRouter == null)
		{
			_log.Warn("[LevelPrepareUi] ⚠ IUiRouter服务不可用，UI切换将受限");
		}
	
		SetupEventHandlers();
		
		_log.Info("[LevelPrepareUi] UI初始化完成");
		_log.Info($"[LevelPrepareUi] 当前选中关卡: {LevelChoose.CurrentGameLevel}");
	}

	public override void _Input(InputEvent @event)
	{
		if (!@event.IsActionPressed("ui_cancel") || !Visible) return;

		this.SendCommand(new ResumeGameWithClosePauseMenuCommand(new ClosePauseMenuCommandInput
		{
			Handle = GetPage().Handle!.Value
		}));
		AcceptEvent();
	}

	#endregion

	#region 公开API - UI页面行为

	/// <summary>
	///     获取UI页面行为实例
	/// </summary>
	public IUiPageBehavior GetPage()
	{
		_page ??= UiPageBehaviorFactory.Create<Control>(this, UiKeyStr, UiLayer.Modal);
		return _page;
	}

	#endregion

	

	#region 私有方法 - 事件处理

	/// <summary>
	///     设置按钮事件处理器
	/// </summary>
	private void SetupEventHandlers()
	{
		if (EnterButton != null)
		{
			EnterButton.Pressed += OnEnterButtonPressed;
			_log.Debug("[LevelPrepareUi] '开始构建'按钮事件已绑定");
		}
		else
		{
			_log.Error("[LevelPrepareUi] 无法找到 '开始构建' 按钮!");
		}

		if (BackButton != null)
		{
			BackButton.Pressed += OnBackButtonPressed;
			_log.Debug("[LevelPrepareUi] '退回'按钮事件已绑定");
		}
		else
		{
			_log.Error("[LevelPrepareUi] 无法找到 '退回' 按钮!");
		}
	}

	/// <summary>
	///     处理"开始构建"按钮点击事件
	///     
	 ///     智能路由系统:
	 ///     ┌─────────────────────────────────────────────────────────────┐
	 ///     │ 输入: LevelChoose.CurrentGameLevel                         │
	 ///     │ 处理: 根据枚举值映射到对应的关卡场景                        │
	 ///     │ 输出: 切换到目标关卡游戏场景                               │
	 ///     │                                                             │
	 ///     │ 支持的关卡:                                                │
	 ///     │   • Level1 → Level_1/level_play.tscn                      │
	 ///     │   • Level2 → Level_2/Level_2.tscn                        │
	 ///     │   • Level3 → Level_3/Level_3.tscn                        │
	 ///     │   • Level4 → level_4/Level_4.tscn                        │
	 ///     │   • Level5 → level_5/Level_5.tscn                        │
	 ///     │   • LevelTest → tests/gametest.tscn (测试场景)           │
	 ///     └─────────────────────────────────────────────────────────────┘
	 /// </summary>
	private void OnEnterButtonPressed()
	{
		_log.Info("[LevelPrepareUi] 用户点击 '开始构建'...");
		_log.Info($"[LevelPrepareUi] 目标关卡: {LevelChoose.CurrentGameLevel}");
		
		try
		{
			DisableAllButtons();
			
			var targetScene = GetTargetLevelScenePath();
			
			if (string.IsNullOrEmpty(targetScene))
			{
				_log.Error($"[LevelPrepareUi] 无法确定目标场景！当前关卡: {LevelChoose.CurrentGameLevel}");
				EnableAllButtons();
				return;
			}
			
			_log.Info($"[LevelPrepareUi] 正在导航到: {targetScene}");
			
			NavigateToLevelPlayCoroutine(targetScene).RunCoroutine();
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelPrepareUi] 导航失败: {ex.Message}");
			_log.Debug(ex.StackTrace);
			EnableAllButtons();
		}
	}

	/// <summary>
	 ///     获取目标关卡的场景键值
	 ///
	 ///     根据当前的GameLevel枚举值返回对应的SceneKey枚举名称
	 ///     使用switch表达式进行精确匹配和路由
	 ///
	 ///     ⚠️ 重要：返回的是SceneKey枚举值的字符串名称（如"Level1"）
	 ///     而不是直接的资源路径！ISceneRouter.ReplaceAsync()需要枚举名称
	 /// </summary>
	 /// <returns>SceneKey枚举的字符串名称</returns>
	private string GetTargetLevelScenePath()
	{
		return LevelChoose.CurrentGameLevel switch
		{
			GameLevel.Level1 => nameof(SceneKey.Level1),
			GameLevel.Level2 => nameof(SceneKey.Level2),
			GameLevel.Level3 => nameof(SceneKey.Level3),
			GameLevel.Level4 => nameof(SceneKey.Level4),
			GameLevel.Level5 => nameof(SceneKey.Level5),
			GameLevel.LevelTest => nameof(SceneKey.GameTest),
			_ => string.Empty
		};
	}

	/// <summary>
	///     导航到关卡游戏场景的协程
	 /// </summary>
	 /// <param name="targetScene">目标场景路径或SceneKey</param>
	private IEnumerator<IYieldInstruction> NavigateToLevelPlayCoroutine(string targetScene)
	{
		_log.Info("[LevelPrepareUi] ═══════════ 开始进入关卡游戏 ═══════════");
		_log.Info($"[LevelPrepareUi] 当前关卡状态: {LevelChoose.CurrentGameLevel}");
		_log.Info($"[LevelPrepareUi] 目标场景: {targetScene}");

		// ═══ 步骤1: 关闭准备UI ═══
		// 清除LevelPrepareUi，为游戏场景腾出空间
		
		_log.Info("[LevelPrepareUi] 步骤1[关闭UI]: 正在清除 LevelPrepareUi...");
		_log.Debug("[LevelPrepareUi] → 调用 _uiRouter.ClearAsync()");
		
		yield return _uiRouter.ClearAsync().AsTask().AsCoroutineInstruction();
		
		_log.Info("[LevelPrepareUi] ✓ 步骤1完成: 准备UI已关闭");

		// ═══ 步骤2: 切换到目标关卡场景 ═══
		// 加载用户选择的具体关卡
		
		_log.Info("[LevelPrepareUi] 步骤2[切换场景]: 正在加载关卡场景...");
		_log.Debug($"[LevelPrepareUi] → 调用 _sceneRouter.ReplaceAsync({targetScene})");
		_log.Debug("[LevelPrepareUi] → 此操作将:");
		_log.Debug("[LevelPrepareUi]   • 卸载 LevelPerpare 场景");
		_log.Debug("   • 加载目标关卡游戏场景");
		_log.Debug("   • 进入游戏玩法阶段");
		
		yield return _sceneRouter.ReplaceAsync(targetScene).AsTask().AsCoroutineInstruction();
		
		_log.Info("[LevelPrepareUi] ✓ 步骤2完成: 关卡场景已加载");
		_log.Info($"[LevelPrepareUi] ═══════════ 成功进入{LevelChoose.CurrentGameLevel}关卡 ═══════════");
	}

	/// <summary>
	///     处理"退回"按钮点击事件
	///     返回到关卡选择界面(LevelChoose UI + Choose场景)
	 /// </summary>
	private void OnBackButtonPressed()
	{
		_log.Info("[LevelPrepareUi] 用户点击 '退回'，返回关卡选择界面...");
		
		try
		{
			DisableAllButtons();
			
			ReturnToLevelChooseCoroutine().RunCoroutine();
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelPrepareUi] 返回失败: {ex.Message}");
			EnableAllButtons();
		}
	}

	/// <summary>
	///     返回关卡选择界面的协程
	///     
	 ///     严格四步执行顺序:
	 ///     1. 关闭旧UI → 2. 切换场景 → 3. 等待场景就绪 → 4. 加载新UI
	 /// </summary>
	private IEnumerator<IYieldInstruction> ReturnToLevelChooseCoroutine()
	{
		_log.Info("[LevelPrepareUi] ═══════════ 开始返回关卡选择界面 ═══════════");
		_log.Info($"[LevelPrepareUi] 当前位置: LevelPrepareUi + LevelPerpare");
		_log.Info($"[LevelPrepareUi] 目标位置: LevelChoose + Choose");

		// ════════════════════════════════════════════════
		// 步骤1: 关闭旧UI (LevelPrepareUi)
		// ════════════════════════════════════════════════
		// 目的: 清除当前的准备界面UI
		// 操作: 使用ClearAsync清除所有UI栈
		// 结果: UI栈为空，屏幕暂时无UI内容
		
		_log.Info("[LevelPrepareUi] 步骤1[关闭旧UI]: 正在清除 LevelPrepareUi...");
		_log.Debug("[LevelPrepareUi] → 调用 _uiRouter.ClearAsync()");
		_log.Debug("[LevelPrepareUi] → 此操作将:");
		_log.Debug("[LevelPrepareUi]   • 清除整个UI栈");
		_log.Debug("[LevelPrepareUi]   • 移除 LevelPrepareUi 及其所有子UI");
		_log.Debug("[LevelPrepareUi]   • 屏幕将暂时显示为空白（这是正常的）");
		
		yield return _uiRouter.ClearAsync().AsTask().AsCoroutineInstruction();
		
		_log.Info("[LevelPrepareUi] ✓ 步骤1完成: 旧UI已关闭并清除");

		// ════════════════════════════════════════════════
		// 步骤2: 切换场景 (LevelPerpare → Choose)
		// ════════════════════════════════════════════════
		// 目的: 将底层场景从关卡准备切换到关卡选择
		// 操作: 使用ReplaceAsync替换场景
		// 结果: Choose场景成为新的底层背景
		
		_log.Info("[LevelPrepareUi] 步骤2[切换场景]: 正在切换到 Choose 场景...");
		_log.Debug("[LevelPrepareUi] → 调用 _sceneRouter.ReplaceAsync(SceneKey.Choose)");
		_log.Debug("[LevelPrepareUi] → 此操作将:");
		_log.Debug("[LevelPrepareUi]   • 卸载 LevelPerpare 场景");
		_log.Debug("[LevelPrepareUi]   • 加载 Choose 场景作为新的底层背景");
		_log.Debug("[LevelPrepareUi]   • Choose场景包含选择界面的背景元素");
		
		yield return _sceneRouter.ReplaceAsync(nameof(SceneKey.Choose)).AsTask().AsCoroutineInstruction();
		
		_log.Info("[LevelPrepareUi] ✓ 步骤2完成: Choose场景已加载并激活");

		// ════════════════════════════════════════════════
		// 步骤3: 确认场景就绪（隐式等待）
		// ════════════════════════════════════════════════
		// 说明: ReplaceAsync会等待场景完全加载后才返回
		// 因此到达此处时，Choose场景已经100%就绪
		// 可以安全地在上面叠加UI
		
		_log.Info("[LevelPrepareUi] 步骤3[确认就绪]: 验证场景状态...");
		_log.Debug("[LevelPrepareUi] → Choose场景已完全加载");
		_log.Debug("[LevelPrepareUi] → 底层背景已准备就绪");
		_log.Debug("[LevelPrepareUi] → 可以安全地加载UI层");
		
		_log.Info("[LevelPrepareUi] ✓ 步骤3完成: 场景已确认就绪");

		// ════════════════════════════════════════════════
		// 步骤4: 加载新UI (LevelChoose)
		// ══════════════════════════════════════════════
		// 目的: 在已就绪的场景上显示关卡选择UI
		// 操作: 使用PushAsync将新UI压入栈
		// 结果: 用户看到完整的关卡选择界面
		
		_log.Info("[LevelPrepareUi] 步骤4[加载新UI]: 正在加载 LevelChoose UI...");
		_log.Debug("[LevelPrepareUi] → 调用 _uiRouter.PushAsync(UiKey.LevelChoose)");
		_log.Debug("[LevelPrepareUi] → 此操作将:");
		_log.Debug("[LevelPrepareUi]   • 创建 LevelChoose UI实例");
		_log.Debug("[LevelPrepareUi]   • 显示关卡选择按钮 (1-5, Test)");
		_log.Debug("[LevelPrepareUi]   • 显示返回按钮");
		_log.Debug("[LevelPrepareUi]   • 叠加在Choose场景之上");
		
		yield return _uiRouter.PushAsync(nameof(UiKey.LevelChoose)).AsTask().AsCoroutineInstruction();
		
		_log.Info("[LevelPrepareUi] ✓ 步骤4完成: LevelChoose UI已加载并显示");
		_log.Info("[LevelPrepareUi] ═══════════ 返回关卡选择界面完成 ═══════════");
		_log.Info("[LevelPrepareUi] 当前状态: LevelChoose UI + Choose 场景");
	}

	#endregion

	#region 私有方法 - 按钮状态管理

	/// <summary>
	///     禁用所有按钮，防止重复点击
	/// </summary>
	private void DisableAllButtons()
	{
		if (EnterButton != null) EnterButton.Disabled = true;
		if (BackButton != null) BackButton.Disabled = true;
	}

	/// <summary>
	///     启用所有按钮
	/// </summary>
	private void EnableAllButtons()
	{
		if (EnterButton != null) EnterButton.Disabled = false;
		if (BackButton != null) BackButton.Disabled = false;
	}

	#endregion
}
