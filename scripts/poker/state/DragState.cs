using GFrameworkGodotTemplate.scripts.enums.poker;
using Godot;

namespace GFrameworkGodotTemplate.scripts.poker.state;

public partial class DragState : PokerState
{
    private Vector2 _lastMousePosition;
    private float _targetRotationRad;
    
    public override void Process(double delta)
    {
        Poker.SetGlobalPosition(Poker.GetGlobalMousePosition() - Poker.GetSize() / 2);
        
        Vector2 currentMousePosition =  Poker.GetGlobalMousePosition();
        Vector2 gap = currentMousePosition - _lastMousePosition;
        _lastMousePosition = currentMousePosition;
        _targetRotationRad = Mathf.DegToRad(Mathf.Clamp(gap.X * 15f, -30f, 30f));
        Poker.SetRot(Mathf.LerpAngle(Poker.GetRotation(), _targetRotationRad, 10f * (float)delta));
    }

    public override void Enter()
    {
        // 隐藏并锁定鼠标在窗口范围内
        Input.SetMouseMode(Input.MouseModeEnum.ConfinedHidden);
    }

    public override void Exit()
    {
        // 显示鼠标
        Input.SetMouseMode(Input.MouseModeEnum.Visible);
    }

    public override void MouseDown()
    {
        
    }

    public override void MouseUp()
    {
        bool placed = Poker.TryPlaceObstacle();
        RequestStateChange(placed ? StateType.Placed : StateType.Idle);
    }

    public override void MouseEnter()
    {
        
    }
    
    public override void MouseExit()
    {
        
    }
}