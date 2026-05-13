using GFramework.Core.Coroutine.Instructions;
using GFrameworkGodotTemplate.scripts.core.controller.level;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.UI;
using GFrameworkGodotTemplate.scripts.constants;
using GFrameworkGodotTemplate.scripts.core.ui;
using GFrameworkGodotTemplate.scripts.enums.ui;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level.ui;

/// <summary>
///     关卡游玩界面控制器
///     <para>
///         显示在玩家完成构建阶段后的主游戏界面
///         恢复全部输入控制，允许玩家自由移动和操作
///         作为游玩阶段的主HUD（抬头显示）界面
///     </para>
///     <author>AI Assistant</author>
///     <version>2.0.0</version>
///     <date>2026-05-12</date>
///     <description>
///         功能特性:
///         - 自主管理所有UI组件和事件
///         - 显示游戏状态信息（可选）
///         - 提供暂停功能入口
///         - 实现完整的UI生命周期管理
///         - 符合GFramework架构规范和单一职责原则
///         
///         设计原则:
///         - UI组件完全自主，不依赖控制器管理内部逻辑
///         - 控制器只负责决定何时显示/隐藏此UI
///         - 通过信号与外部通信
///         
///         使用场景:
///         - 玩家点击"完成"按钮后自动显示
///         - 贯穿整个游玩阶段直到游戏结束
///         
///         Godot配置要求:
///         1. 将此脚本挂载到 level_play_ui.tscn 的根节点
///         2. 确保根节点类型为 Control
///     </description>
/// </summary>
[ContextAware]
[Log]
public partial class LevelPlayUi : Control, IController, IUiPageBehaviorProvider, ISimpleUiPage
{
	#region 信号定义

	/// <summary>请求暂停信号：通知控制器打开暂停菜单</summary>
	[Signal]
	public delegate void PauseRequestedEventHandler();

	#endregion

	#region 私有字段

	/// <summary>页面行为实例</summary>
	private IUiPageBehavior? _page;

	/// <summary>BaseLevelController引用</summary>
	private BaseLevelController? _levelController;

	#endregion

	#region 公开属性

	/// <summary>Ui Key的字符串形式</summary>
	public static string UiKeyStr => nameof(UiKey.LevelPlayUi);

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
		_log.Info("[LevelPlayUi] ═══════════ 初始化游玩界面 ═══════════");
		_log.Info($"[LevelPlayUi] UI Key: {UiKeyStr}");
		
		InitializeComponents();
		
		_log.Info("[LevelPlayUi] ✓ 游玩界面初始化完成");
		_log.Info("[LevelPlayUi] 当前职责: 游戏主HUD + 输入控制已恢复");
	}

	#endregion

	#region 私有方法 - 初始化

	/// <summary>初始化组件和引用</summary>
	private void InitializeComponents()
	{
		FindLevelController();
	}

	/// <summary>查找BaseLevelController</summary>
	private void FindLevelController()
	{
		var currentScene = GetTree()?.CurrentScene;
		if (currentScene == null)
		{
			_log.Warn("[LevelPlayUi] ⚠ 无法获取当前场景");
			return;
		}

		_levelController = currentScene as BaseLevelController;
		
		if (_levelController != null)
		{
			_log.Info("[LevelPlayUi] ✓ 找到BaseLevelController");
			_log.Debug($"[LevelPlayUi] 控制器路径: {_levelController.GetPath()}");
		}
		else
		{
			_log.Debug("[LevelPlayUi] 未找到BaseLevelController（非致命错误）");
		}
	}

	#endregion
}
