using GFramework.Core.Abstractions.State;
using GFramework.Game.Abstractions.UI;
using GFrameworkGodotTemplate.scripts.core.controller;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.cqrs.game.command;
using GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command.input;
using GFrameworkGodotTemplate.scripts.enums;
using Godot;

namespace GFrameworkGodotTemplate.global;

/// <summary>
///     全局输入控制器类，继承自 GameInputController。
///     负责处理游戏中的全局输入事件，包括暂停和恢复游戏的功能。
///     
///     扩展职责:
///     - 管理全局游戏玩法输入服务(IGlobalGameplayInputService)
///     - 在每帧输入事件时同步更新全局输入状态
///     - 为所有 Gameplay 组件提供统一的输入数据源
/// </summary>
[ContextAware]
[Log]
public partial class GlobalInputController : GameInputController
{
	private UiHandle? _pauseMenuUiHandle;

	/// <summary>
	///     状态机系统实例，用于管理游戏状态。
	/// </summary>
	private IStateMachineSystem _stateMachineSystem = null!;

	/// <summary>
	///     全局游戏玩法输入服务实例
	///     负责统一处理角色移动相关的输入检测和状态缓存
	/// </summary>
	private IGlobalGameplayInputService _gameplayInputService = null!;

	/// <summary>
	///     初始化方法，在节点准备就绪时调用。
	///     获取并初始化状态机系统和全局输入服务实例。
	/// </summary>
	public override void _Ready()
	{
		_stateMachineSystem = this.GetSystem<IStateMachineSystem>()!;
		
		InitializeGameplayInputService();
	}

	protected override bool AcceptPhase(InputPhase phase)
	{
		return phase is InputPhase.Global or InputPhase.Paused;
	}

	protected override void Handle(InputPhase phase, InputEvent @event)
	{
		// 每次输入事件都更新全局游戏玩法输入状态
		UpdateGameplayInputState();

		if (!@event.IsActionPressed("ui_cancel"))return;

		if (_stateMachineSystem.Current is not PlayingState) return;
		_log.Debug("暂停游戏");
		_pauseMenuUiHandle = this.SendCommand(new PauseGameWithOpenPauseMenuCommand(new OpenPauseMenuCommandInput
			{ Handle = _pauseMenuUiHandle }));
		GetViewport().SetInputAsHandled();
	}

	#region 公开API - 全局输入服务访问

	/// <summary>
	///     获取全局游戏玩法输入服务实例
	///     供 PlayerMovementController 等 Gameplay 组件使用
	/// </summary>
	public IGlobalGameplayInputService GameplayInputService => _gameplayInputService;

	#endregion

	#region 私有方法 - 初始化与更新

	/// <summary>
	///     初始化全局游戏玩法输入服务
	///     创建并配置输入检测逻辑
	/// </summary>
	private void InitializeGameplayInputService()
	{
		_gameplayInputService = new GlobalGameplayInputService();
		_log.Debug("全局游戏玩法输入服务已初始化");
	}

	/// <summary>
	///     更新全局游戏玩法输入状态缓存
	///     在每次输入事件分发时调用，确保输入数据实时同步
	/// </summary>
	private void UpdateGameplayInputState()
	{
		_gameplayInputService.UpdateInputState();
	}

	#endregion
}
