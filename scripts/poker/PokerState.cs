using GFrameworkGodotTemplate.scripts.enums.poker;
using Godot;

namespace GFrameworkGodotTemplate.scripts.poker;

/// <summary>
/// 扑克状态抽象基类
/// </summary>
public abstract partial class PokerState : Node, IPokerState
{
    /// <summary>
    /// 请求切换到其他状态时发出的信号
    /// </summary>
    /// <param name="stateType">目标状态 <see cref="StateType"/> </param>
    [Signal] public delegate void StateChangeRequestedEventHandler(StateType stateType);
    
    /// <summary>
    /// 扑克状态
    /// </summary>
    [Export] public StateType StateType { get; set; }
    
    public Poker Poker { get; set; } = null!;

    public abstract void MouseDown();
    
    public abstract void MouseUp();
    
    public abstract void MouseEnter();
    
    public abstract void MouseExit();

    public abstract void Process(double delta);

    public abstract void Enter();
    
    public abstract void Exit();
    
    /// <summary>
    /// 请求切换到其他状态
    /// </summary>
    /// <param name="targetStateType">目标状态 <see cref="StateType"/> </param>
    protected void RequestStateChange(StateType targetStateType)
    {
        EmitSignal(SignalName.StateChangeRequested, (int)targetStateType);
    }
}