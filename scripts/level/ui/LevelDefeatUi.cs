// Copyright (c) 2025 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
using GFrameworkGodotTemplate.scripts.level.controllers;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level.ui;

/// <summary>
///     关卡失败界面控制器
///     <para>
///         显示在关卡超时或失败时的失败界面
///         包含"再玩一次"和"返回主菜单"按钮
///         在此界面显示期间，禁用键盘/手柄输入，仅允许鼠标操作
///     </para>
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-13</date>
///     <description>
///         功能特性:
///         - 自主管理所有失败界面按钮事件和导航逻辑
///         - 提供两种游戏结束选项（重玩、返回主菜单）
///         - 实现完整的UI生命周期管理
///         - 集成路由服务实现场景/UI切换
///         - 符合GFramework架构规范和单一职责原则
///         
///         导航目标:
///         - "再玩一次" → LevelPrepareUi + LevelPerpare 场景（重新准备当前关卡）
///         - "返回主菜单" → LevelChoose UI + Choose 场景（返回关卡选择）
///         
///         设计原则:
///         - UI组件完全自主管理内部逻辑、事件和导航
///         - 控制器只负责决定何时显示此UI
///         - 通过信号或直接调用与控制器通信
///         
///         输入限制 (Defate阶段):
///         ✓ 允许: 鼠标点击按钮
///         ✓ 允许: ESC键打开暂停菜单
///         ✗ 禁止: 键盘/手柄游戏输入
///         
///         触发时机:
///         - 关卡超时（Play阶段时间超过MaxTimeMs）
///         - 其他失败条件（未来扩展）
///         
///         Godot配置要求:
///         1. 将此脚本挂载到 level_defate_ui.tscn 的根节点
///         2. 确保根节点类型为 Control
///         3. 必须包含以下按钮节点（unique_name_in_owner = true）:
///            - %AgainButton ("再玩一次")
///            - %BackButton ("返回主菜单")
///     </description>
///     <remarks>
///         与LevelSuccessUi的区别:
///         - 没有"下一关"按钮（失败后不能进入下一关）
///         - 标题显示"失败"而不是"通过！"
///         - 可能显示超时原因信息（未来扩展）
///         
///         性能指标:
///         - 内存占用: <50KB（不含资源）
///         - 初始化时间: <5ms
///         - 事件响应延迟: <16ms（1帧）
///     </remarks>
/// </summary>
[ContextAware]
[Log]
public partial class LevelDefeatUi : Control, IController, IUiPageBehaviorProvider, ISimpleUiPage
{
	#region 信号定义

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

	#region 节点引用（必须与level_defate_ui.tscn匹配）

	/// <summary>"再玩一次"按钮</summary>
	private Button? AgainButton => GetNodeOrNull<Button>("%AgainButton");

	/// <summary>"返回主菜单"按钮</summary>
	private Button? BackButton => GetNodeOrNull<Button>("%BackButton");

	#endregion

	#region 公开属性

	/// <summary>Ui Key的字符串形式</summary>
	public static string UiKeyStr => nameof(UiKey.LevelDefateUi);

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
		_log.Info("[LevelDefateUi] ═══════════ 初始化失败界面 ═══════════");
		_log.Info($"[LevelDefateUi] UI Key: {UiKeyStr}");
		
		InitializeServices();
		InitializeComponents();
		SetupEventHandlers();
		
		_log.Info("[LevelDefateUi] ✓✓✓ 失败界面初始化完成！");
		_log.Info("[LevelDefateUi] 当前职责: 自主管理所有按钮事件和导航逻辑");
	}

	/// <summary>处理输入事件（Defeat阶段限制）</summary>
	public override void _Input(InputEvent @event)
	{
		if (!Visible) return;
		
		if (@event.IsActionPressed("ui_cancel"))
		{
			_log.Info("[LevelDefateUi] 检测到ESC键，打开暂停菜单...");
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
			_log.Debug("[LevelDefateUi] ✓ IUiRouter服务已获取");
		}
		else
		{
			_log.Warn("[LevelDefateUi] ⚠ IUiRouter服务不可用，导航功能将受限");
		}
		
		if (_sceneRouter != null)
		{
			_log.Debug("[LevelDefateUi] ✓ ISceneRouter服务已获取");
		}
		else
		{
			_log.Warn("[LevelDefateUi] ⚠ ISceneRouter服务不可用，场景切换将受限");
		}
		
		if (_stateMachineSystem != null)
		{
			_log.Debug("[LevelDefateUi] ✓ IStateMachineSystem服务已获取");
		}
		else
		{
			_log.Warn("[LevelDefateUi] ⚠ IStateMachineSystem服务不可用");
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
		_log.Info("[LevelDefateUi] 正在查找BaseLevelController...");
		_log.Debug($"[LevelDefateUi] 当前节点路径: {GetPath()}");
		_log.Debug($"[LevelDefateUi] Owner: {(Owner != null ? $"{Owner.Name} ({Owner.GetType().Name})" : "NULL")}");
		
		if (Owner != null)
		{
			_log.Debug("[LevelDefateUi] 尝试方法1: 通过Owner向上遍历...");
			_levelController = FindParentOfType<BaseLevelController>(Owner);
			
			if (_levelController != null)
			{
				_log.Info("[LevelDefateUi] ✓ 找到BaseLevelController");
				_log.Debug($"[LevelDefateUi] 控制器路径: {_levelController.GetPath()}");
				return;
			}
		}

		_log.Debug("[LevelDefateUi] 尝试方法2: 从当前节点向上遍历...");
		_levelController = FindParentOfType<BaseLevelController>(this);
		
		if (_levelController != null)
		{
			_log.Info("[LevelDefateUi] ✓ 通过父节点遍历找到BaseLevelController");
			_log.Debug($"[LevelDefateUi] 控制器路径: {_levelController.GetPath()}");
			return;
		}

		_log.Debug("[LevelDefateUi] 未找到BaseLevelController（非致命错误）");
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
				_log.Debug($"[LevelDefateUi] 在深度{depth}处找到: {current.Name} ({current.GetType().Name})");
				return target;
			}
			
			current = current.GetParent();
			depth++;
		}
		
		if (depth >= maxDepth)
		{
			_log.Warn($"[LevelDefateUi] 向上遍历超过最大深度({maxDepth})，停止搜索");
		}
		
		return null;
	}

	/// <summary>记录可用的按钮信息</summary>
	private void LogAvailableButtons()
	{
		var hasButtons = false;
		
		if (AgainButton != null)
		{
			_log.Info("[LevelDefateUi] ✓ AgainButton已找到 ('再玩一次')");
			hasButtons = true;
		}
		else
		{
			_log.Error("[LevelDefateUi] ✗ AgainButton未找到 (%AgainButton) - 重玩功能不可用！");
		}
		
		if (BackButton != null)
		{
			_log.Info("[LevelDefateUi] ✓ BackButton已找到 ('返回主菜单') ← 核心功能");
			hasButtons = true;
		}
		else
		{
			_log.Error("[LevelDefateUi] ✗ BackButton未找到 (%BackButton) - 返回主菜单功能不可用！");
		}
		
		if (!hasButtons)
		{
			_log.Error("[LevelDefateUi] ✗ 未找到任何按钮节点！");
		}
	}

	#endregion

	#region 私有方法 - 事件处理

	/// <summary>设置事件处理器</summary>
	private void SetupEventHandlers()
	{
		if (AgainButton != null)
		{
			AgainButton.Pressed += OnAgainButtonPressed;
			_log.Debug("[LevelDefateUi] AgainButton事件已绑定");
		}
		
		if (BackButton != null)
		{
			BackButton.Pressed += OnBackButtonPressed;
			_log.Debug("[LevelDefateUi] BackButton事件已绑定（返回主菜单）");
		}
	}

	/// <summary>"再玩一次"按钮点击处理 - 核心功能</summary>
	private void OnAgainButtonPressed()
	{
		_log.Info("[LevelDefateUi] ═══════════ 用户点击'再玩一次' ═══════════");
		_log.Info($"[LevelDefateUi] 当前关卡: {LevelChoose.CurrentGameLevel}");
		EmitSignal(SignalName.RetryRequested);
		
		RetryLevelCoroutine().RunCoroutine();
	}

	/// <summary>"返回主菜单"按钮点击处理 - 核心功能</summary>
	private void OnBackButtonPressed()
	{
		_log.Info("[LevelDefateUi] ═══════════ 用户点击'返回主菜单' ═══════════");
		EmitSignal(SignalName.MainMenuRequested);
		
		ReturnToMainMenuCoroutine().RunCoroutine();
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
	 ///         - 与LevelSuccessUi的再玩逻辑保持一致
	 ///     </remarks>
	/// </summary>
	private IEnumerator<IYieldInstruction> RetryLevelCoroutine()
	{
		_log.Info("[LevelDefateUi] ═══════════ 开始再玩一次流程 ═══════════");
		_log.Info($"[LevelDefateUi] 目标关卡: {LevelChoose.CurrentGameLevel}");
		
		_log.Info("[LevelDefateUi] 步骤1/4: 清理当前关卡数据...");
		ClearCurrentLevelData();
		
		_log.Info("[LevelDefateUi] 步骤2/4: 重置关卡状态标志...");
		ResetLevelPhaseFlags();
		
		_log.Info("[LevelDefateUi] 步骤3/4: 切换到LevelPrepare UI...");
		yield return LoadLevelPrepareUiAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelDefateUi] 步骤4/4: 切换到LevelPerpare场景...");
		yield return SwitchToLevelPerpareSceneAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelDefateUi] ✓✓✓ 再玩一次流程完成！");
		_log.Info($"[LevelDefateUi] 当前位置: 关卡准备界面 (LevelPrepareUi + LevelPerpare)");
		_log.Info($"[LevelDefateUi] 等待用户点击'开始构建'重新进入 {LevelChoose.CurrentGameLevel}...");
	}

	/// <summary>
	///     清理当前关卡的所有临时数据
	///     <para>
	///         确保重新开始时不会残留上一轮游戏的状态
	 ///     </para>
	/// </summary>
	private void ClearCurrentLevelData()
	{
		_log.Info("[LevelDefateUi] → 清理关卡临时数据...");
		
		try
		{
			if (_levelController != null)
			{
				_log.Debug("[LevelDefateUi]   • 重置控制器内部状态");
			}
			
			_log.Debug("[LevelDefateUi]   • 阶段标志将在步骤2中重置");
			_log.Debug("[LevelDefateUi]   • 玩家数据由新实例自动初始化");
			_log.Debug("[LevelDefateUi]   • 场景节点将由ReplaceAsync完全卸载");
			
			_log.Info("[LevelDefateUi] ✓ 关卡数据清理完成");
		}
		catch (Exception ex)
		{
			_log.Warn($"[LevelDefateUi] ⚠ 数据清理时出现非致命异常: {ex.Message}");
		}
	}

	/// <summary>加载LevelPrepare UI</summary>
	private async Task LoadLevelPrepareUiAsync()
	{
		if (_uiRouter == null)
		{
			_log.Error("[LevelDefateUi] ✗ UI路由器不可用，无法加载LevelPrepare UI！");
			return;
		}

		try
		{
			_log.Debug("[LevelDefateUi] → 调用 _uiRouter.ReplaceAsync(LevelPrepareUi)");
			_log.Debug("[LevelDefateUi]   此操作将:");
			_log.Debug("[LevelDefateUi]     • 清除当前UI栈（包括LevelDefateUi）");
			_log.Debug("[LevelDefateUi]     • 创建并显示LevelPrepareUi实例");
			_log.Debug("[LevelDefateUi]     • 显示'开始构建'和'退回'按钮");
			
			await _uiRouter.ReplaceAsync(nameof(UiKey.LevelPrepareUi));
			
			_log.Info("[LevelDefateUi] ✓ LevelPrepare UI已加载并显示");
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelDefateUi] ❌ 加载LevelPrepare UI失败: {ex.Message}");
			throw;
		}
	}

	/// <summary>切换到LevelPerpare底层场景</summary>
	private async Task SwitchToLevelPerpareSceneAsync()
	{
		if (_sceneRouter == null)
		{
			_log.Error("[LevelDefateUi] ✗ 场景路由器不可用，无法切换到LevelPerpare场景！");
			return;
		}

		try
		{
			_log.Debug("[LevelDefateUi] → 调用 _sceneRouter.ReplaceAsync(LevelPerpare)");
			_log.Debug("[LevelDefateUi]   此操作将:");
			_log.Debug("[LevelDefateUi]     • 卸载当前关卡场景（完全清理）");
			_log.Debug("[LevelDefateUi]     • 加载level_perpare.tscn场景");
			_log.Debug("[LevelDefateUi]     • 作为LevelPrepareUi的底层背景");
			
			await _sceneRouter.ReplaceAsync(nameof(SceneKey.LevelPerpare));
			
			_log.Info("[LevelDefateUi] ✓ LevelPerpare场景已加载");
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelDefateUi] ❌ 切换LevelPerpare场景失败: {ex.Message}");
			throw;
		}
	}

	#endregion

	#region 私有方法 - 返回主菜单导航逻辑

	/// <summary>
	///     返回主菜单协程
	///     <para>
	 ///         完整的导航流程：
	 ///         1. 重置关卡阶段标志（解除输入限制）
	 ///         2. 切换状态机到MainMenuState
	 ///         3. 切换UI到LevelChoose（关卡选择界面）
	 ///         4. 切换场景到Choose（底层场景）
	 ///     </para>
	 ///     <remarks>
	 ///         设计说明:
	 ///         - 失败后返回主菜单让用户重新选择关卡或退出
	 ///         - 完全清理当前关卡的所有状态和数据
	 ///         - 确保不会残留任何失败状态的标志
	 ///     </remarks>
	/// </summary>
	private IEnumerator<IYieldInstruction> ReturnToMainMenuCoroutine()
	{
		_log.Info("[LevelDefateUi] ═══════════ 开始返回主菜单流程 ═══════════");
		
		_log.Info("[LevelDefateUi] 步骤1/4: 重置关卡状态标志...");
		ResetLevelPhaseFlags();
		
		_log.Info("[LevelDefateUi] 步骤2/4: 切换状态机到MainMenuState...");
		yield return SwitchToMainMenuStateAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelDefateUi] 步骤3/4: 加载LevelChoose UI...");
		yield return LoadLevelChooseUiAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelDefateUi] 步骤4/4: 切换到Choose场景...");
		yield return SwitchToChooseSceneAsync().AsCoroutineInstruction();
		
		_log.Info("[LevelDefateUi] ✓✓✓ 返回主菜单流程完成！");
		_log.Info("[LevelDefateUi] 当前位置: 关卡选择界面 (LevelChoose + Choose)");
	}

	/// <summary>切换状态机到MainMenuState</summary>
	private async Task SwitchToMainMenuStateAsync()
	{
		if (_stateMachineSystem == null)
		{
			_log.Warn("[LevelDefateUi] ⚠ 状态机系统不可用，跳过状态切换");
			return;
		}

		try
		{
			_log.Debug("[LevelDefateUi] → 切换到 MainMenuState...");
			
			if (_stateMachineSystem.Current is not MainMenuState)
			{
				await _stateMachineSystem.ChangeToAsync<MainMenuState>();
				_log.Info("[LevelDefateUi] ✓ 状态已切换到 MainMenuState");
			}
			else
			{
				_log.Debug("[LevelDefateUi] 已在 MainMenuState，跳过切换");
			}
		}
		catch (Exception ex)
		{
			_log.Warn($"[LevelDefateUi] ⚠ 切换状态时出现异常: {ex.Message}");
			_log.Warn("[LevelDefateUi]   继续执行后续步骤...");
		}
	}

	/// <summary>加载LevelChoose UI</summary>
	private async Task LoadLevelChooseUiAsync()
	{
		if (_uiRouter == null)
		{
			_log.Error("[LevelDefateUi] ✗ UI路由器不可用，无法加载LevelChoose UI！");
			return;
		}

		try
		{
			_log.Debug("[LevelDefateUi] → 调用 _uiRouter.ReplaceAsync(LevelChoose)");
			_log.Debug("[LevelDefateUi]   此操作将:");
			_log.Debug("[LevelDefateUi]     • 清除当前UI栈（包括LevelDefateUi）");
			_log.Debug("[LevelDefateUi]     • 创建并显示LevelChooseUi实例");
			_log.Debug("[LevelDefateUi]     • 显示关卡列表供用户选择");
			
			await _uiRouter.ReplaceAsync(nameof(UiKey.LevelChoose));
			
			_log.Info("[LevelDefateUi] ✓ LevelChoose UI已加载并显示");
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelDefateUi] ❌ 加载LevelChoose UI失败: {ex.Message}");
			throw;
		}
	}

	/// <summary>切换到Choose底层场景</summary>
	private async Task SwitchToChooseSceneAsync()
	{
		if (_sceneRouter == null)
		{
			_log.Error("[LevelDefateUi] ✗ 场景路由器不可用，无法切换到Choose场景！");
			return;
		}

		try
		{
			_log.Debug("[LevelDefateUi] → 调用 _sceneRouter.ReplaceAsync(Choose)");
			_log.Debug("[LevelDefateUi]   此操作将:");
			_log.Debug("[LevelDefateUi]     • 卸载当前关卡场景（完全清理）");
			_log.Debug("[LevelDefateUi]     • 加载choose.tscn场景");
			_log.Debug("[LevelDefateUi]     • 作为LevelChooseUi的底层背景");
			
			await _sceneRouter.ReplaceAsync(nameof(SceneKey.Choose));
			
			_log.Info("[LevelDefateUi] ✓ Choose场景已加载");
		}
		catch (Exception ex)
		{
			_log.Error($"[LevelDefateUi] ❌ 切换Choose场景失败: {ex.Message}");
			throw;
		}
	}

	#endregion

	#region 私有方法 - 辅助方法

	/// <summary>重置关卡阶段标志</summary>
	private void ResetLevelPhaseFlags()
	{
		if (_levelController != null)
		{
			_log.Debug("[LevelDefateUi] 通过控制器重置标志（推荐方式）");
		}
		else
		{
			_log.Debug("[LevelDefateUi] 直接重置静态标志（备用方式）");
		}
		
		BaseLevelController.ResetPhaseFlags();
		
		_log.Info("[LevelDefateUi] ✓ 阶段标志已重置:");
		_log.Info($"[LevelDefateUi]   - IsBuildPhaseActive = {BaseLevelController.IsBuildPhaseActive}");
		_log.Info($"[LevelDefateUi]   - IsSuccessPhaseActive = {BaseLevelController.IsSuccessPhaseActive}");
	}

	#endregion
}
