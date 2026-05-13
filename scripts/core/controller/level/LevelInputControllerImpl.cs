using GFramework.Core.Abstractions.State;
using GFramework.Core.Abstractions.Rule;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.cqrs.game.command;
using GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command.input;
using GFrameworkGodotTemplate.scripts.enums;
using GFrameworkGodotTemplate.scripts.level.interfaces;
using Godot;

namespace GFrameworkGodotTemplate.scripts.core.controller.level;

/// <summary>
///     关卡输入控制器实现（从BaseLevelController提取）
///     <para>
///         负责关卡场景中根据不同阶段限制或放行用户输入
///         遵循单一职责原则(SRP)，专注于输入控制逻辑
///     </para>
/// </summary>
public class LevelInputControllerImpl : ILevelInputController
{
    #region 私有字段

    /// <summary>状态机系统引用</summary>
    private readonly IStateMachineSystem? _stateMachineSystem;

    /// <summary>所属的Node节点（用于发送命令和获取视口）</summary>
    private readonly Node _ownerNode;

    /// <summary>日志记录器</summary>
    private readonly Action<string> _logInfo;
    private readonly Action<string> _logDebug;

    /// <summary>获取当前阶段状态的委托（与BaseLevelController实时同步）</summary>
    private readonly Func<LevelPhase> _getCurrentPhase;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建关卡输入控制器实例
    /// </summary>
    /// <param name="stateMachineSystem">状态机系统</param>
    /// <param name="ownerNode">所属节点（必须实现IContextAware）</param>
    /// <param name="getCurrentPhase">获取当前阶段状态的委托</param>
    /// <param name="logInfo">信息日志</param>
    /// <param name="logDebug">调试日志</param>
    public LevelInputControllerImpl(
        IStateMachineSystem? stateMachineSystem,
        Node ownerNode,
        Func<LevelPhase> getCurrentPhase,
        Action<string>? logInfo = null,
        Action<string>? logDebug = null)
    {
        _stateMachineSystem = stateMachineSystem;
        _ownerNode = ownerNode;
        _getCurrentPhase = getCurrentPhase;

        _logInfo = logInfo ?? (msg => { });
        _logDebug = logDebug ?? (msg => { });
    }

    #endregion

    #region ILevelInputController 接口实现

    /// <summary>处理输入事件</summary>
    public void HandleInput(InputEvent @event)
    {
        switch (_getCurrentPhase())
        {
            case LevelPhase.Build:
                HandleBuildPhaseInput(@event);
                break;

            case LevelPhase.Play:
                HandlePlayPhaseInput(@event);
                break;

            case LevelPhase.Success:
                HandleSuccessPhaseInput(@event);
                break;
        }
    }

    /// <summary>处理ESC键按下</summary>
    public void HandleEscapeKeyPress()
    {
        if (_stateMachineSystem?.Current is not PlayingState)
        {
            return;
        }

        _logDebug("[LevelInputController] ESC键按下，打开暂停菜单...");

        if (_ownerNode is IContextAware contextAware)
        {
            contextAware.SendCommand(new PauseGameWithOpenPauseMenuCommand(new OpenPauseMenuCommandInput()));
        }
        _ownerNode.GetViewport()?.SetInputAsHandled();
    }

    #endregion

    #region 公开属性

    /// <summary>获取当前阶段（只读，通过委托实时同步）</summary>
    public LevelPhase CurrentPhase => _getCurrentPhase();

    #endregion

    #region 私有方法 - 阶段输入处理

    /// <summary>处理构建阶段的输入（严格限制模式）</summary>
    private void HandleBuildPhaseInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            var keyCode = keyEvent.Keycode;
            var physicalKey = keyEvent.PhysicalKeycode;

            var isEscape = keyCode == Key.Escape || physicalKey == Key.Escape;

            if (isEscape)
            {
                _logDebug("[LevelInputController] [Build阶段] ESC键已放行");
                HandleEscapeKeyPress();
                return;
            }

            _logDebug($"[LevelInputController] [Build阶段] 键盘输入已被拦截: Key={keyCode}, Physical={physicalKey}");
            _ownerNode.GetViewport()?.SetInputAsHandled();
            return;
        }

        if (@event is InputEventJoypadButton || @event is InputEventJoypadMotion)
        {
            _logDebug("[LevelInputController] [Build阶段] 手柄输入已被拦截");
            _ownerNode.GetViewport()?.SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("ui_accept") ||
            @event.IsActionPressed("ui_select") ||
            @event.IsActionPressed("ui_cancel"))
        {
            if (@event.IsActionPressed("ui_cancel"))
            {
                HandleEscapeKeyPress();
                return;
            }

            _logDebug("[LevelInputController] [Build阶段] UI动作输入已被拦截");
            _ownerNode.GetViewport()?.SetInputAsHandled();
            return;
        }
    }

    /// <summary>处理游玩阶段的输入（完全开放）</summary>
    private void HandlePlayPhaseInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            HandleEscapeKeyPress();
        }
    }

    /// <summary>处理成功阶段的输入（完全禁止）</summary>
    private void HandleSuccessPhaseInput(InputEvent @event)
    {
        if (@event is InputEventKey || @event is InputEventJoypadButton || @event is InputEventJoypadMotion)
        {
            _ownerNode.GetViewport()?.SetInputAsHandled();
        }

        if (@event.IsActionPressed("ui_cancel"))
        {
            HandleEscapeKeyPress();
        }
    }

    #endregion
}
