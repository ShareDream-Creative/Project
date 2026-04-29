using GFrameworkGodotTemplate.scripts.enums.poker;

namespace GFrameworkGodotTemplate.scripts.poker.state;

public partial class IdleState : PokerState
{
    public override void Process(double delta)
    {
        
    }

    public override void Enter()
    {
        Poker.ResetPosAndRot();
    }

    public override void Exit()
    {

    }

    public override void MouseDown()
    {
        RequestStateChange(StateType.Drag);
    }

    public override void MouseUp()
    {
        
    }

    public override void MouseEnter()
    {
        
    }
    
    public override void MouseExit()
    {
        
    }
}