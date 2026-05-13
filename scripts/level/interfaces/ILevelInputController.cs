using Godot;

namespace GFrameworkGodotTemplate.scripts.level.interfaces;

/// <summary>
///     关卡输入控制器接口
///     <para>
///         定义关卡场景中输入控制的标准契约，遵循单一职责原则(SRP)
///         负责根据不同阶段限制或放行用户输入
///     </para>
/// </summary>
public interface ILevelInputController
{
    /// <summary>处理输入事件</summary>
    /// <param name="@event">输入事件</param>
    void HandleInput(InputEvent @event);

    /// <summary>处理ESC键按下</summary>
    void HandleEscapeKeyPress();
}
