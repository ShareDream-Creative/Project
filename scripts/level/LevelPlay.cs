using GFramework.Godot.Scene;
using GFrameworkGodotTemplate.scripts.core.scene;
using GFrameworkGodotTemplate.scripts.enums.scene;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level;

/// <summary>
///     关卡游戏场景控制器
///     负责管理关卡游戏进行中的主场景和UI状态切换
///     
///     功能说明:
///     - 场景加载完成后自动激活LevelBuildUi(构建模式)
///     - 点击"完成!"按钮(FinishButton)后:
///       1. 失活LevelBuildUi
///       2. 激活LevelPlayUi(游戏模式)
///     - 在游戏模式下可点击BackButton返回HomeUi场景
///     
///     场景节点结构 (来自 level_play.tscn):
///     LevelPlay (Node2D) ← 挂载本脚本
///     ├── LevelBuildUi (Control) - 构建阶段UI
///     │   ├── ColorRect (背景)
///     │   └── FinishButton (Button) - "完成!"
///     ├── LevelDefateUi (Control) - 失败UI (备用)
///     ├── LevelSuccessUi (Control) - 成功UI (备用)
///     └── LevelPlayUi (Control) - 游戏进行中UI
///         └── BackButton (Button) - "退回" (可选)
///     
///     UI状态机:
///     ┌─────────────┐    点击完成    ┌─────────────┐    点击退回    ┌──────────┐
///     │ LevelBuildUi │ ────────────→ │ LevelPlayUi │ ──────────→ │  HomeUi  │
///     │  (构建模式)   │              │  (游戏模式)   │              │ (返回)    │
///     └─────────────┘              └─────────────┘              └──────────┘
/// </summary>
[ContextAware]
[Log]
public partial class LevelPlay : Node2D, IController, ISceneBehaviorProvider, ISimpleScene
{
	#region 私有字段

	private ISceneBehavior? _scene;

	private ISceneRouter _sceneRouter = null!;

	#endregion

	#region 节点引用

	/// <summary>
	///     构建阶段UI引用
	///     初始激活，用于关卡构建/布置阶段
	/// </summary>
	private Control LevelBuildUi => GetNode<Control>("LevelBuildUi");

	/// <summary>
	///     游戏进行中UI引用
	///     完成构建后激活，用于实际游戏操作
	/// </summary>
	private Control LevelPlayUi => GetNode<Control>("LevelPlayUi");

	/// <summary>
	///     "完成!"按钮引用
	///     位于LevelBuildUi中，点击后切换到游戏模式
	/// </summary>
	private Button FinishButton => GetNode<Button>("LevelBuildUi/FinishButton");

	#endregion

	#region 场景键值

	/// <summary>
	///     获取场景键值字符串
	/// </summary>
	public static string SceneKeyStr => nameof(SceneKey.LevelPlay);

	#endregion

	#region 生命周期方法

	public override void _Ready()
	{
		_sceneRouter = this.GetSystem<ISceneRouter>()!;
		
		InitializeUiState();
		SetupEventHandlers();
		
		_log.Debug("[LevelPlay] 场景初始化完成，当前处于构建模式");
	}

	public override void _ExitTree()
	{
		CleanupResources();
	}

	#endregion

	#region 公开API - 场景行为

	/// <summary>
	///     获取场景行为实例
	///     使用工厂模式创建场景行为，确保单例模式
	/// </summary>
	/// <returns>ISceneBehavior接口的场景行为实例</returns>
	public ISceneBehavior GetScene()
	{
		_scene ??= SceneBehaviorFactory.Create<Node2D>(this, SceneKeyStr);
		return _scene;
	}

	#endregion

	#region 私有方法 - UI初始化

	/// <summary>
	///     初始化UI状态
	///     场景加载完成后默认激活构建UI，隐藏其他UI
	/// </summary>
	private void InitializeUiState()
	{
		if (LevelBuildUi != null)
		{
			LevelBuildUi.Show();
			_log.Debug("[LevelPlay] LevelBuildUi 已激活");
		}
		else
		{
			_log.Error("[LevelPlay] 无法找到 LevelBuildUi 节点!");
		}

		if (LevelPlayUi != null)
		{
			LevelPlayUi.Hide();
			_log.Debug("[LevelPlay] LevelPlayUi 已隐藏(等待激活)");
		}
		else
		{
			_log.Error("[LevelPlay] 无法找到 LevelPlayUi 节点!");
		}
	}

	#endregion

	#region 私有方法 - 事件处理

	/// <summary>
	///     设置按钮事件处理器
	/// </summary>
	private void SetupEventHandlers()
	{
		if (FinishButton != null)
		{
			FinishButton.Pressed += OnFinishButtonPressed;
			_log.Debug("[LevelPlay] '完成!'按钮事件已绑定");
		}
		else
		{
			_log.Error("[LevelPlay] 无法找到 '完成!' 按钮!");
		}
	}

	/// <summary>
	///     处理"完成!"按钮点击事件
	///     切换UI状态: 从构建模式切换到游戏模式
	 /// </summary>
	private void OnFinishButtonPressed()
	{
		_log.Info("[LevelPlay] 用户点击 '完成!'，切换到游戏模式...");
		
		SwitchToGameMode();
	}

	/// <summary>
	///     切换到游戏模式
	///     失活构建UI，激活游戏UI
	/// </summary>
	private void SwitchToGameMode()
	{
		if (LevelBuildUi != null)
		{
			LevelBuildUi.Hide();
			_log.Debug("[LevelPlay] LevelBuildUi 已失活");
		}

		if (LevelPlayUi != null)
		{
			LevelPlayUi.Show();
			_log.Debug("[LevelPlay] LevelPlayUi 已激活");
			_log.Info("[LevelPlay] 成功切换到游戏模式!");
			
			TrySetupBackButton();
		}
	}

	/// <summary>
	///     尝试设置返回按钮
	///     在LevelPlayUi中查找并绑定BackButton（如果存在）
	/// </summary>
	private void TrySetupBackButton()
	{
		try
		{
			var backButton = LevelPlayUi?.GetNodeOrNull<Button>("BackButton");
			
			if (backButton != null)
			{
				backButton.Pressed += OnBackButtonPressed;
				_log.Debug("[LevelPlay] BackButton 事件已绑定(位于LevelPlayUi中)");
			}
			else
			{
				_log.Debug("[LevelPlay] 未在LevelPlayUi中找到BackButton(可选功能)");
			}
		}
		catch (Exception ex)
		{
			_log.Debug($"[LevelPlay] BackButton设置跳过: {ex.Message}");
		}
	}

	/// <summary>
	///     处理"退回"按钮点击事件
	///     返回到HomeUi场景
	/// </summary>
	private void OnBackButtonPressed()
	{
		_log.Info("[LevelPlay] 用户点击 '退回'，返回 HomeUi 场景...");
		
		try
		{
			_sceneRouter.ReplaceAsync(nameof(SceneKey.Home))
				.AsTask()
				.ToCoroutineEnumerator()
				.RunCoroutine();
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelPlay] 返回失败: {ex.Message}");
		}
	}

	#endregion

	#region 私有方法 - 资源管理

	/// <summary>
	///     清理资源
	///     在场景退出时调用，确保正确释放所有UI资源
	/// </summary>
	private void CleanupResources()
	{
		_log.Debug("[LevelPlay] 正在清理场景资源...");
		
		if (LevelBuildUi != null)
		{
			_log.Debug("[LevelPlay] 释放 LevelBuildUi 资源");
		}
		
		if (LevelPlayUi != null)
		{
			_log.Debug("[LevelPlay] 释放 LevelPlayUi 资源");
		}
		
		GC.Collect();
		_log.Info("[LevelPlay] 资源清理完成");
	}

	#endregion
}
