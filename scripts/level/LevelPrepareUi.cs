using GFramework.Godot.Scene;
using GFrameworkGodotTemplate.scripts.enums.scene;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level;

/// <summary>
///     关卡准备UI控制器
///     负责处理关卡准备界面的按钮交互和导航逻辑
///     
///     功能说明:
///     - EnterButton ("开始构建"): 导航到LevelPlay场景(开始游戏)
///     - BackButton ("退回"): 返回HomeUi场景(主界面)
///     
///     UI节点结构 (来自 level_prepare_ui.tscn):
///     LevelPrepareUi (Control)
///     ├── ColorRect (背景)
///     │   └── option/MarginContainer/VBoxContainer
///     │       ├── EnterButton (Button) - "开始构建"
///     │       └── BackButton (Button) - "退回"
///     ├── ColorRect2 (卡牌区域)
///     │   └── cardspot
///     └── ColorRect3 (库存区域)
///         └── stock
/// </summary>
[ContextAware]
[Log]
public partial class LevelPrepareUi : Control, IController
{
	#region 私有字段

	private ISceneRouter _sceneRouter = null!;

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

	#region 生命周期方法

	public override void _Ready()
	{
		_sceneRouter = this.GetSystem<ISceneRouter>()!;
		
		SetupEventHandlers();
		
		_log.Debug("[LevelPrepareUi] UI初始化完成");
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
	///     导航到LevelPlay场景(关卡游戏场景)
	/// </summary>
	private void OnEnterButtonPressed()
	{
		_log.Info("[LevelPrepareUi] 用户点击 '开始构建'，导航到 LevelPlay 场景...");
		
		try
		{
			DisableAllButtons();
			
			_sceneRouter.ReplaceAsync(nameof(SceneKey.LevelPlay))
				.AsTask()
				.ToCoroutineEnumerator()
				.RunCoroutine();
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelPrepareUi] 导航失败: {ex.Message}");
			EnableAllButtons();
		}
	}

	/// <summary>
	///     处理"退回"按钮点击事件
	///     返回到HomeUi场景(主界面)
	/// </summary>
	private void OnBackButtonPressed()
	{
		_log.Info("[LevelPrepareUi] 用户点击 '退回'，返回 HomeUi 场景...");
		
		try
		{
			DisableAllButtons();
			
			_sceneRouter.ReplaceAsync(nameof(SceneKey.Home))
				.AsTask()
				.ToCoroutineEnumerator()
				.RunCoroutine();
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelPrepareUi] 返回失败: {ex.Message}");
			EnableAllButtons();
		}
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
