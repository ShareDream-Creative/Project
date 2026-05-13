using GFramework.Core.Coroutine.Instructions;
using GFramework.Game.Abstractions.Enums;
using GFramework.Game.Abstractions.UI;
using GFramework.Godot.UI;
using GFrameworkGodotTemplate.scripts.constants;
using GFrameworkGodotTemplate.scripts.core.ui;
using GFrameworkGodotTemplate.scripts.enums.ui;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level;

/// <summary>
///     关卡构建界面控制器
///     <para>
///         显示在游戏场景加载后的初始界面
///         包含"完成！"按钮，点击后切换到游玩UI
///         在此界面显示期间，除ESC外禁止所有键盘输入
///     </para>
///     <author>AI Assistant</author>
///     <version>2.0.0</version>
///     <date>2026-05-12</date>
///     <description>
///         功能特性:
///         - 自主管理FinishButton按钮事件（符合单一职责原则）
///         - 通过信号通知BaseLevelController状态变更
///         - 实现完整的UI生命周期管理
///         - 符合GFramework架构规范
///         
///         设计原则:
///         - UI组件自主管理内部逻辑
///         - 控制器只负责流程协调和规则制定
///         - 松耦合通信机制
///         
///         使用场景:
///         - 玩家进入任意关卡时自动显示
///         - 作为关卡流程的第一阶段UI
///         
///         Godot配置要求:
///         1. 将此脚本挂载到 level_build_ui.tscn 的根节点
///         2. 确保根节点类型为 Control
///         3. 确保存在名为"FinishButton"的Button节点（设置unique_name_in_owner = true）
///     </description>
/// </summary>
[ContextAware]
[Log]
public partial class LevelBuildUi : Control, IController, IUiPageBehaviorProvider, ISimpleUiPage
{
	#region 信号定义

	/// <summary>构建完成信号：通知控制器切换到游玩阶段</summary>
	[Signal]
	public delegate void BuildFinishedEventHandler();

	#endregion

	#region 私有字段

	/// <summary>页面行为实例</summary>
	private IUiPageBehavior? _page;

	/// <summary>BaseLevelController引用</summary>
	private BaseLevelController? _levelController;

	/// <summary>是否正在处理构建完成（防止重复点击）</summary>
	private bool _isProcessingBuildFinished;

	#endregion

	#region 节点引用

	/// <summary>"完成"按钮</summary>
	private Button FinishButton => GetNode<Button>("%FinishButton");

	#endregion

	#region 公开属性

	/// <summary>Ui Key的字符串形式</summary>
	public static string UiKeyStr => nameof(UiKey.LevelBuildUi);

	#endregion

	#region IUiPageBehaviorProvider 接口实现

	/// <summary>
	///     获取页面行为实例
	///     如果不存在则创建新的CanvasItemUiPageBehavior实例
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
		_log.Info("[LevelBuildUi] ═══════════ 初始化构建界面 ═══════════");
		_log.Info($"[LevelBuildUi] UI Key: {UiKeyStr}");
		
		InitializeComponents();
		SetupEventHandlers();
		
		_log.Info("[LevelBuildUi] ✓ 构建界面初始化完成");
		_log.Info("[LevelBuildUi] 当前职责: 自主管理按钮事件 + 通知控制器");
	}

	#endregion

	#region 私有方法 - 初始化

	/// <summary>初始化组件和引用</summary>
	private void InitializeComponents()
	{
		FindLevelController();
		ValidateNodes();
	}

	/// <summary>查找BaseLevelController</summary>
	private void FindLevelController()
	{
		_log.Info("[LevelBuildUi] 正在查找BaseLevelController...");
		_log.Debug($"[LevelBuildUi] 当前节点路径: {GetPath()}");
		_log.Debug($"[LevelBuildUi] Owner: {(Owner != null ? $"{Owner.Name} ({Owner.GetType().Name})" : "NULL")}");
		
		// 方法1: 通过Owner属性向上遍历（最可靠）
		if (Owner != null)
		{
			_log.Debug("[LevelBuildUi] 尝试方法1: 通过Owner向上遍历...");
			_levelController = FindParentOfType<BaseLevelController>(Owner);
			
			if (_levelController != null)
			{
				_log.Info("[LevelBuildUi] ✓✓✓ 通过Owner找到BaseLevelController");
				_log.Debug($"[LevelBuildUi] 控制器路径: {_levelController.GetPath()}");
				return;
			}
		}

		// 方法2: 从当前节点向上遍历
		_log.Debug("[LevelBuildUi] 尝试方法2: 从当前节点向上遍历...");
		_levelController = FindParentOfType<BaseLevelController>(this);
		
		if (_levelController != null)
		{
			_log.Info("[LevelBuildUi] ✓✓✓ 通过父节点遍历找到BaseLevelController");
			_log.Debug($"[LevelBuildUi] 控制器路径: {_levelController.GetPath()}");
			return;
		}

		// 方法3: 查找失败，使用信号通信
		_log.Warn("[LevelBuildUi] ⚠ 未找到BaseLevelController");
		_log.Warn("[LevelBuildUi] 将使用信号方式通知控制器（需确保已连接信号）");
		_log.Debug("[LevelBuildUi] 备用方案: 发射BuildFinished信号");
	}

	/// <summary>从指定节点开始向上遍历，查找目标类型的父节点</summary>
	private T? FindParentOfType<T>(Node startNode) where T : Node
	{
		var current = startNode;
		var maxDepth = 20; // 防止无限循环
		var depth = 0;
		
		while (current != null && depth < maxDepth)
		{
			if (current is T target)
			{
				_log.Debug($"[LevelBuildUi] 在深度{depth}处找到: {current.Name} ({current.GetType().Name})");
				return target;
			}
			
			current = current.GetParent();
			depth++;
		}
		
		if (depth >= maxDepth)
		{
			_log.Warn($"[LevelBuildUi] 向上遍历超过最大深度({maxDepth})，停止搜索");
		}
		
		return null;
	}

	/// <summary>验证必要节点是否存在</summary>
	private void ValidateNodes()
	{
		if (FinishButton == null)
		{
			_log.Error("[LevelBuildUi] ✗ 致命错误: FinishButton未找到！");
			_log.Error("[LevelBuildUi] 请确保场景中存在名为'FinishButton'的Button节点");
			_log.Error("[LevelBuildUi] 并设置了 unique_name_in_owner = true");
			
			GD.PushError("LevelBuildUi: 缺少FinishButton节点！请检查场景配置。");
		}
		else
		{
			_log.Info("[LevelBuildUi] ✓ FinishButton已找到");
			_log.Debug($"[LevelBuildUi] 按钮路径: {FinishButton.GetPath()}");
		}
	}

	#endregion

	#region 私有方法 - 事件处理

	/// <summary>设置事件处理器</summary>
	private void SetupEventHandlers()
	{
		if (FinishButton != null)
		{
			FinishButton.Pressed += OnFinishButtonPressed;
			_log.Info("[LevelBuildUi] ✓ FinishButton点击事件已绑定");
		}
	}

	/// <summary>"完成"按钮点击处理</summary>
	private void OnFinishButtonPressed()
	{
		if (_isProcessingBuildFinished)
		{
			_log.Debug("[LevelBuildUi] ⚠ 忽略重复点击（正在处理中）");
			return;
		}

		_isProcessingBuildFinished = true;
		
		_log.Info("════════════ 用户点击'完成'按钮 ═══════════");
		_log.Info("[LevelBuildUi] 处理按钮点击事件...");
		_log.Info("[LevelBuildUi] 通知控制器切换到游玩阶段");
		
		NotifyBuildFinished();
		
		_log.Info("[LevelBuildUi] ✓ 按钮事件处理完成");
	}

	/// <summary>通知控制器构建已完成</summary>
	private void NotifyBuildFinished()
	{
		if (_levelController != null)
		{
			_log.Debug("[LevelBuildUi] 使用直接调用方式通知控制器");
			_levelController.OnBuildFinished();
		}
		else
		{
			_log.Debug("[LevelBuildUi] 使用信号方式通知控制器");
			EmitSignal(SignalName.BuildFinished);
		}
	}

	#endregion
}
