using GFrameworkGodotTemplate.scripts.enums.poker;
using Godot;

namespace GFrameworkGodotTemplate.scripts.poker.state;

public partial class PlacedState : PokerState
{
    public override void Enter()
    {
        Poker.Visible = false;
        Poker.MouseFilter = Control.MouseFilterEnum.Ignore;
        Poker.EmitSignal(Poker.SignalName.CardPlaced);
    }

    public override void Exit() { }
    public override void Process(double delta) { }
    public override void MouseDown() { }
    public override void MouseUp() { }
    public override void MouseEnter() { }
    public override void MouseExit() { }
}
