using GFrameworkGodotTemplate.scripts.enums.poker;
using Godot;

namespace GFrameworkGodotTemplate.scripts.poker;

public partial class PokerStateMachine : Node, IPokerStateMachine
{
    private PokerState PreviousState { get; set; } = null!;
    private PokerState CurrentState { get; set; } = null!;
    private Dictionary<StateType, PokerState> States { get; set; } = new();

    public void Init(Poker poker)
    {
        foreach (var node in GetChildren())
        {
            var state = (PokerState)node;
            States.Add(state.StateType, state);
            state.Poker = poker;
            state.StateChangeRequested += OnStateChangeRequested;
        }
    }

    public void ChangeTo(StateType stateType)
    {
        if (CurrentState == null!)
        {
            CurrentState = States[stateType];
            CurrentState.Enter();
            return;
        }
        
        // 如果新状态与旧状态相同，返回
        if (CurrentState == States[stateType]) return;
        
        CurrentState.Exit();
        PreviousState = CurrentState;
        CurrentState = States[stateType];
        CurrentState.Enter();
    }
    
    public void Process(double delta)
    {
        if (CurrentState != null!)
        {
            CurrentState.Process(delta);
        }
    }
    
    public void MouseDown()
    {
        if (CurrentState != null!)
        {
            CurrentState.MouseDown();
        }
    }

    public void MouseUp()
    {
        if (CurrentState != null!)
        {
            CurrentState.MouseUp();
        }
    }

    public void MouseEnter()
    {
        if (CurrentState != null!)
        {
            CurrentState.MouseEnter();
        }
    }

    public void MouseExit()
    {
        if (CurrentState != null!)
        {
            CurrentState.MouseExit();
        }
    }

    private void OnStateChangeRequested(StateType stateType)
    {
        ChangeTo(stateType);
    }
}