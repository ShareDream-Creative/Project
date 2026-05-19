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
using GFrameworkGodotTemplate.scripts.enums;
using GFrameworkGodotTemplate.scripts.level;
using GFrameworkGodotTemplate.scripts.level.controllers;
using GFrameworkGodotTemplate.scripts.data.model;
using GFrameworkGodotTemplate.scripts.poker;
using GFrameworkGodotTemplate.scripts.events.poker;
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

	/// <summary>导航互斥锁：防止快速连续点击导致状态混乱</summary>
	private bool _isNavigating = false;

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

		try
		{
			SetupEventHandlers();
			_log.Info("[MainMenu] ✓ 所有按钮事件绑定完成");
		}
		catch (Exception ex)
		{
			_log.Error($"[MainMenu] ❌ 按钮事件绑定失败: {ex.Message}");
			_log.Error($"[MainMenu] 异常类型: {ex.GetType().FullName}");
			_log.Error($"[MainMenu] 堆栈跟踪:\n{ex.StackTrace}");

			_log.Warn("[MainMenu] ⚠️ 尝试使用安全模式重新绑定按钮事件...");
			SetupEventHandlersSafe();
		}
	}

	private void SetupEventHandlers()
	{
		if (NewGameButton == null) throw new NullReferenceException("NewGameButton 节点未找到 (%NewGameButton)");
		if (ContinueGameButton == null) throw new NullReferenceException("ContinueGameButton 节点未找到 (%CotinueGameButton)");
		if (ChooseLevelButton == null) throw new NullReferenceException("ChooseLevelButton 节点未找到 (%ChooseLevelButton)");
		if (OptionsMenuButton == null) throw new NullReferenceException("OptionsMenuButton 节点未找到 (%OptionsMenuButton)");
		if (CreditsButton == null) throw new NullReferenceException("CreditsButton 节点未找到 (%CreditsButton)");
		if (ExitButton == null) throw new NullReferenceException("ExitButton 节点未找到 (%ExitButton)");

		ExitButton.Pressed += OnExitButtonPressed;
		CreditsButton.Pressed += OnCreditsButtonPressed;
		OptionsMenuButton.Pressed += OnOptionsMenuButtonPressed;
		NewGameButton.Pressed += OnNewGameButtonPressed;
		ContinueGameButton.Pressed += OnContinueGameButtonPressed;
		ChooseLevelButton.Pressed += OnChooseLevelButtonPressed;

		_log.Info("[MainMenu] ✓ 6个按钮节点验证通过");
	}

	private void SetupEventHandlersSafe()
	{
		try
		{
			if (NewGameButton != null) NewGameButton.Pressed += OnNewGameButtonPressed;
			else _log.Error("[MainMenu] ❌ NewGameButton 不可用");

			if (ContinueGameButton != null) ContinueGameButton.Pressed += OnContinueGameButtonPressed;
			else _log.Error("[MainMenu] ❌ ContinueGameButton 不可用 (请检查场景文件中的节点名称是否为 %CotinueGameButton)");

			if (ChooseLevelButton != null) ChooseLevelButton.Pressed += OnChooseLevelButtonPressed;
			else _log.Error("[MainMenu] ❌ ChooseLevelButton 不可用");

			if (ExitButton != null) ExitButton.Pressed += OnExitButtonPressed;
			else _log.Error("[MainMenu] ❌ ExitButton 不可用");

			if (CreditsButton != null) CreditsButton.Pressed += OnCreditsButtonPressed;
			else _log.Error("[MainMenu] ❌ CreditsButton 不可用");

			if (OptionsMenuButton != null) OptionsMenuButton.Pressed += OnOptionsMenuButtonPressed;
			else _log.Error("[MainMenu] ❌ OptionsMenuButton 不可用");

			var boundCount = new[] { NewGameButton, ContinueGameButton, ChooseLevelButton, ExitButton, CreditsButton, OptionsMenuButton }.Count(b => b != null);
			_log.Info($"[MainMenu] ✓ 安全模式完成: 成功绑定 {boundCount}/6 个按钮");
		}
		catch (Exception ex)
		{
			_log.Error($"[MainMenu] ❌ 安全模式也失败: {ex.Message}");
		}
	}

	#region 按钮事件处理器（带导航互斥锁）

	private void OnExitButtonPressed()
	{
		if (_isNavigating) return;

		this.RunCommandCoroutine(new ExitGameCommand());
	}

	private void OnCreditsButtonPressed()
	{
		if (_isNavigating) return;

		_uiRouter.PushAsync(Credits.UiKeyStr).AsTask().ToCoroutineEnumerator().RunCoroutine();
	}

	private void OnOptionsMenuButtonPressed()
	{
		if (_isNavigating) return;

		this.RunCommandCoroutine(new OpenOptionsMenuCommand());
	}

	private void OnNewGameButtonPressed()
	{
		if (_isNavigating)
		{
			_log.Debug("[MainMenu] 导航进行中，忽略'新游戏'按钮重复点击");
			return;
		}

		_log.Info("[MainMenu] ═══════════ 开始新游戏流程 ═══════════");
		_log.Info("[MainMenu] → 直接进入教程关卡 (Teach_Level)");

		StartNavigation(NewGameCoroutine());
	}

	private void OnContinueGameButtonPressed()
	{
		if (_isNavigating)
		{
			_log.Debug("[MainMenu] 导航进行中，忽略'继续游戏'按钮重复点击");
			return;
		}

		_log.Info("[MainMenu] ═══════════ 开始继续游戏流程 ═══════════");

		var currentLevel = LevelChoose.CurrentGameLevel;
		_log.Info($"[MainMenu] 当前关卡状态: {currentLevel}");

		if (currentLevel == GameLevel.None)
		{
			_log.Warn("[MainMenu] ⚠️ 当前无存档关卡 (GameLevel.None)，默认进入第一关");
			LevelChoose.SetCurrentGameLevel(GameLevel.Level1);
		}
		else
		{
			_log.Info($"[MainMenu] ✓ 检测到有效存档关卡: {currentLevel}，准备恢复游戏");
		}

		StartNavigation(ContinueGameCoroutine());
	}

	private void OnChooseLevelButtonPressed()
	{
		if (_isNavigating)
		{
			_log.Debug("[MainMenu] 导航进行中，忽略'关卡选择'按钮重复点击");
			return;
		}

		_log.Info("[MainMenu] 开始切换到关卡选择状态...");
		StartNavigation(ChooseLevelCoroutine());
	}

	#endregion

	#region 导航控制

	/// <summary>
	///     启动导航协程（带互斥锁保护）
	/// </summary>
	private void StartNavigation(IEnumerator<IYieldInstruction> coroutine)
	{
		_isNavigating = true;
		DisableAllButtons();
		coroutine.RunCoroutine();
	}

	/// <summary>
	///     清理导航状态（解锁 + 启用按钮）
	/// </summary>
	private void CleanupNavigation()
	{
		_isNavigating = false;
		if (GodotObject.IsInstanceValid(this))
		{
			EnableAllButtons();
		}
	}

	#endregion

	#region 按钮禁用/启用

	private void DisableAllButtons()
	{
		if (!GodotObject.IsInstanceValid(this))
		{
			_log.Debug("[MainMenu] ⚠ 节点已释放，跳过 DisableAllButtons()");
			return;
		}

		if (NewGameButton != null && GodotObject.IsInstanceValid(NewGameButton)) NewGameButton.Disabled = true;
		if (ContinueGameButton != null && GodotObject.IsInstanceValid(ContinueGameButton)) ContinueGameButton.Disabled = true;
		if (ChooseLevelButton != null && GodotObject.IsInstanceValid(ChooseLevelButton)) ChooseLevelButton.Disabled = true;
		if (OptionsMenuButton != null && GodotObject.IsInstanceValid(OptionsMenuButton)) OptionsMenuButton.Disabled = true;
		if (CreditsButton != null && GodotObject.IsInstanceValid(CreditsButton)) CreditsButton.Disabled = true;
		if (ExitButton != null && GodotObject.IsInstanceValid(ExitButton)) ExitButton.Disabled = true;

		_log.Debug("[MainMenu] ✓ 所有按钮已禁用");
	}

	private void EnableAllButtons()
	{
		if (!GodotObject.IsInstanceValid(this))
		{
			_log.Debug("[MainMenu] ⚠ 节点已释放，跳过 EnableAllButtons()");
			return;
		}

		if (NewGameButton != null && GodotObject.IsInstanceValid(NewGameButton)) NewGameButton.Disabled = false;
		if (ContinueGameButton != null && GodotObject.IsInstanceValid(ContinueGameButton)) ContinueGameButton.Disabled = false;
		if (ChooseLevelButton != null && GodotObject.IsInstanceValid(ChooseLevelButton)) ChooseLevelButton.Disabled = false;
		if (OptionsMenuButton != null && GodotObject.IsInstanceValid(OptionsMenuButton)) OptionsMenuButton.Disabled = false;
		if (CreditsButton != null && GodotObject.IsInstanceValid(CreditsButton)) CreditsButton.Disabled = false;
		if (ExitButton != null && GodotObject.IsInstanceValid(ExitButton)) ExitButton.Disabled = false;

		_log.Debug("[MainMenu] ✓ 所有按钮已启用");
	}

	#endregion

	#region 导航协程

	/// <summary>
	///     关卡选择导航协程
	/// </summary>
	private IEnumerator<IYieldInstruction> ChooseLevelCoroutine()
	{
		_log.Info("[MainMenu] ═══════════ 开始关卡选择流程 ═══════════");

		yield return null;  // 等待一帧，避免"flushing queries"错误

		_log.Info("[MainMenu] 阶段1[状态机]: 正在切换到LevelChooseState...");
		_log.Debug("[MainMenu] → 触发LevelChooseState.OnEnterAsync()");

		yield return _stateMachineSystem.ChangeToAsync<LevelChooseState>().AsCoroutineInstruction();

		if (!GodotObject.IsInstanceValid(this)) { CleanupNavigation(); yield break; }

		_log.Debug("[MainMenu] ✓ 状态切换Task完成");
		_log.Debug("[MainMenu] ⚠️ 注意: OnEnterAsync中的UI操作可能未执行");

		_log.Info("[MainMenu] 阶段2[手动保障]: 正在加载LevelChoose UI...");
		_log.Debug("[MainMenu] → 调用 _uiRouter.ReplaceAsync(LevelChoose.UiKeyStr)");
		_log.Debug("[MainMenu] → 此操作100%可靠，将确保关卡选择界面显示");

		yield return _uiRouter.ReplaceAsync(LevelChoose.UiKeyStr).AsTask().AsCoroutineInstruction();

		if (!GodotObject.IsInstanceValid(this)) { CleanupNavigation(); yield break; }

		_log.Info("[MainMenu] ✓ 阶段2完成: LevelChoose UI已加载并显示");

		_log.Info("[MainMenu] 阶段3[场景]: 正在替换为Choose场景...");
		yield return _sceneRouter.ReplaceAsync(nameof(SceneKey.Choose)).AsTask().AsCoroutineInstruction();

		_log.Info("[MainMenu] ✓ 阶段3完成: Choose场景已加载");
		_log.Info("[MainMenu] ═══════════ 关卡选择流程完成 ═══════════");

		CleanupNavigation();
	}

	/// <summary>
	///     继续游戏导航协程
	///     <para>
	///         从主菜单直接跳转到关卡准备界面，恢复上次的游戏进度
	///         自动检测当前关卡状态（LevelChoose.CurrentGameLevel）
	///         如果无存档则默认进入第一关
	///     </para>
	/// </summary>
	private IEnumerator<IYieldInstruction> ContinueGameCoroutine()
	{
		_log.Info("[MainMenu] ═══════════ 开始继续游戏导航流程 ═══════════");
		var targetLevel = LevelChoose.CurrentGameLevel;
		_log.Info($"[MainMenu] 目标关卡: {targetLevel}");

		yield return null;  // 等待一帧，避免"flushing queries"错误

		_log.Info("[MainMenu] 阶段1[状态机]: 正在切换到PlayingState...");

		yield return _stateMachineSystem.ChangeToAsync<PlayingState>().AsCoroutineInstruction();

		if (!GodotObject.IsInstanceValid(this)) { CleanupNavigation(); yield break; }

		_log.Debug("[MainMenu] ✓ 状态切换完成");

		_log.Info("[MainMenu] 阶段2[UI]: 正在加载LevelPrepareUi...");
		yield return _uiRouter.ReplaceAsync(nameof(UiKey.LevelPrepareUi)).AsTask().AsCoroutineInstruction();

		if (!GodotObject.IsInstanceValid(this)) { CleanupNavigation(); yield break; }

		_log.Info("[MainMenu] ✓ 阶段2完成: LevelPrepareUi已加载并显示");

		_log.Info("[MainMenu] 阶段3[场景]: 正在切换到LevelPerpare场景...");
		yield return _sceneRouter.ReplaceAsync(nameof(SceneKey.LevelPerpare)).AsTask().AsCoroutineInstruction();

		_log.Info("[MainMenu] ✓ 阶段3完成: LevelPerpare场景已加载");

		_log.Info($"[MainMenu] ✅ 继续游戏导航完成 - 已进入关卡 {targetLevel} 准备阶段");
		_log.Info("[MainMenu] ═══════════ 继续游戏流程完成 ═══════════");

		CleanupNavigation();
	}

	/// <summary>
	///     新游戏导航协程 - v8.0 预填充缓存方案
	///     <para>
	 ///        核心设计理念 (方案A):
	 ///        - 模拟 ContinueGame 的数据流：预填充全局缓存
	 ///        - 在切换到 TeachLevel 场景之前设置 ClassifiedPokerEvents.LastItemData
	 ///        - LevelBuildUi 创建时自动读取缓存并显示卡牌
	 ///        
	 ///        完整流程:
	 ///        1. 切换到 PlayingState（游戏状态）
	 ///        2. 预填充教程卡牌数据到全局缓存 ⭐ 关键！
	 ///        3. 切换到 TeachLevel 场景
	 ///        4. BaseLevelController.OnEnterAsync() → ShowBuildUiAsync()
	 ///        5. LevelBuildUi._Ready() → 检查 LastItemData → ✅ 有数据 → 显示卡牌
	 ///        
	 ///        与 ContinueGameCoroutine 的对比:
	 ///        - ContinueGame: PrepareUI(用户配置) → UpdateCache() → Level场景
	 ///        - NewGame:      [硬编码]   → UpdateCache() → TeachLevel场景
	 ///        
	 ///        数据流完全一致！只是来源不同：
	 ///        - ContinueGame: 用户在 PrepareUI 中选择的卡牌
	 ///        - NewGame:      硬编码的2张 platform 卡牌
	 ///     </para>
	/// </summary>
	private IEnumerator<IYieldInstruction> NewGameCoroutine()
	{
		_log.Info("[MainMenu] ═══════════ 开始新游戏流程 v8.0 ═══════════");
		_log.Info("[MainMenu] 目标: 教程关卡 (Teach_Level)");
		_log.Info("[MainMenu] 方案: 预填充全局缓存 (模拟ContinueGame数据流)");

		yield return null; // 等待一帧

		// ════════════════════════════════════════════════
		// 步骤1[状态机]: 切换到PlayingState
		// ════════════════════════════════════════════════
		_log.Info("[MainMenu] 步骤1: 切换到PlayingState...");
		yield return _stateMachineSystem.ChangeToAsync<PlayingState>().AsCoroutineInstruction();

		if (!GodotObject.IsInstanceValid(this)) { CleanupNavigation(); yield break; }

		_log.Info("[MainMenu] ✓ PlayingState 已激活");

		// ════════════════════════════════════════════════
		// 步骤2[数据准备]: 预填充教程卡牌数据到全局缓存 ⭐⭐⭐
		//
		// 原理:
		// ContinueGame 流程中, LevelPrepareUi 会调用:
		//   ClassifiedPokerEvents.UpdateCache(itemData, actionData)
		// 然后切换到Level场景后, LevelBuildUi 会检查:
		//   if (ClassifiedPokerEvents.LastItemData != null)
		//     → 传递给 PokerHand 显示
		//
		// 我们在这里模拟这个过程:
		//   1. 创建教程用的 ItemPokerData (2张platform)
		//   2. 调用 UpdateCache 写入全局缓存
		//   3. 后续 LevelBuildUi 会自动读取并显示
		// ════════════════════════════════════════════════
		_log.Info("[MainMenu] 步骤2: 预填充教程卡牌数据到全局缓存...");

		try
		{
			var tutorialItemData = new ItemPokerData
			{
				Items = new List<Pokerlibrary>()
			};

			// 添加2张 platform 卡牌（与真实 PrepareUI 配置一致）
			tutorialItemData.Items.Add(new Pokerlibrary(0, "platform", Pokerlibrary.PokerType.item, "可以踩踏的平台", true));
			tutorialItemData.Items.Add(new Pokerlibrary(0, "platform", Pokerlibrary.PokerType.item, "可以踩踏的平台", true));

			var tutorialActionData = new ActionPokerData(); // 空，无特殊能力

			_log.Info($"[MainMenu]   教程数据已创建: {tutorialItemData.TotalCount} 张 platform 卡牌");

			// 调用内部方法更新缓存（通过反射或公开API）
			PreFillTutorialCache(tutorialItemData, tutorialActionData);

			_log.Info("[MainMenu] ✓✓✓ 全局缓存已预填充！");
			_log.Info("[MainMenu]   后续 LevelBuildUi 将自动读取此缓存并显示卡牌");
		}
		catch (Exception ex)
		{
			_log.Error($"[MainMenu] ❌ 预填充缓存失败: {ex.Message}");
			_log.Error("[MainMenu]   TeachLevel 将无法显示卡牌！");
		}

		// ════════════════════════════════════════════════
		// 步骤3[场景]: 切换到教程关卡
		//
		// 此时全局缓存已有数据, LevelBuildUi 创建时会自动读取
		// 无需 TeachLevel 做任何额外配置!
		// ════════════════════════════════════════════════
		_log.Info("[MainMenu] 步骤3: 切换到 TeachLevel 场景...");
		_log.Info("[MainMenu]   (全局缓存已就绪, LevelBuildUi将自动显示卡牌)");
		
		yield return _sceneRouter.ReplaceAsync(nameof(SceneKey.TeachLevel)).AsTask().AsCoroutineInstruction();

		if (!GodotObject.IsInstanceValid(this)) { CleanupNavigation(); yield break; }

		_log.Info("════════════ [MainMenu] 新游戏流程完成 ═══════════");
		_log.Info("[MainMenu] ✓ TeachLevel 场景已加载");
		_log.Info("[MainMenu] ✓ 控制权已移交 TeachLevel");
		_log.Info("[MainMenu]");
		_log.Info("[MainMenu] 后续流程 (由BaseLevelController自动管理):");
		_log.Info("[MainMenu]   1. BaseLevelController.OnEnterAsync() [9步初始化]");
		_log.Info("[MainMenu]   2. ShowBuildUiAsync() → 创建 LevelBuildUi");
		_log.Info("[MainMenu]   3. LevelBuildUi._Ready() → 检查 LastItemData → ✅ 显示卡牌");
		_log.Info("[MainMenu]   4. 玩家构建 → Play → 胜利");

		CleanupNavigation();
	}

	/// <summary>
	///     预填充教程卡牌数据到全局缓存
	///     <para>
	 ///        通过反射调用 ClassifiedPokerEvents.UpdateCache() 内部方法，
	 ///        将教程用的卡牌数据写入全局静态缓存。
	 ///        
	 ///        这样 LevelBuildUi 创建时会自动检测到缓存数据，
	 ///        并传递给 PokerHand 显示，完全复用 ContinueGame 的数据流。
	 ///        
	 ///        为什么使用反射:
	 ///        - ClassifiedPokerEvents.UpdateCache() 是 internal 方法
	 ///        - 不希望将此方法公开（避免被误用）
	 ///        - 仅在 MainMenu.NewGameCoroutine() 中调用一次
	 ///     </para>
	/// </summary>
	private void PreFillTutorialCache(ItemPokerData itemData, ActionPokerData actionData)
	{
		try
		{
			var updateCacheMethod = typeof(ClassifiedPokerEvents).GetMethod(
				"UpdateCache",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
			);

			if (updateCacheMethod != null)
			{
				updateCacheMethod.Invoke(null, new object[] { itemData, actionData });
				_log.Info("[MainMenu] ✓✓✓ PreFillTutorialCache 成功！");
				_log.Info($"[MainMenu]   ItemData: {itemData.TotalCount} 张卡牌");
				_log.Info($"[MainMenu]   ActionData: {actionData.TotalCount} 个能力");
			}
			else
			{
				// 备用方案：直接设置属性（如果反射失败）
				_log.Warn("[MainMenu] ⚠️ UpdateCache 方法未找到，尝试直接设置属性...");
				
				var lastItemProp = typeof(ClassifiedPokerEvents).GetProperty("LastItemData");
				if (lastItemProp != null && lastItemProp.CanWrite)
				{
					lastItemProp.SetValue(null, itemData);
					_log.Info("[MainMenu] ✓ 已通过属性设置 LastItemData");
				}
				else
				{
					throw new InvalidOperationException("无法访问 ClassifiedPokerEvents 缓存");
				}
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[MainMenu] ❌ PreFillTutorialCache 异常: {ex.Message}");
			throw; // 重新抛出，让调用方处理
		}
	}

	#endregion
}
