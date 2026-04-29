using GFrameworkGodotTemplate.scripts.enums.poker;
using GFrameworkGodotTemplate.scripts.poker;

namespace GFrameworkGodotTemplate.scripts.cqrs.poker.@event;

/// <summary>
/// 状态机状态变更事件类
/// 用于表示状态机状态发生变化的事件
/// </summary>
public class StateChangedEvent
{
    /// <summary>
    ///     将要转换到的下个状态
    /// </summary>
    public StateType NextState { get; init; }
    
    /// <summary>
    ///     未转换前的当前状态
    /// </summary>
    public StateType CurrentState { get; set; }
    
    /// <summary>
    ///     响应事件的poker实例
    /// </summary>
    public IPoker Poker { get; init; } = null!;
}