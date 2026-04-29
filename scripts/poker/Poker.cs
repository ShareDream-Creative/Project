using GFrameworkGodotTemplate.scripts.enums.poker;
using GFrameworkGodotTemplate.global;
using GFrameworkGodotTemplate.scripts.cqrs.poker.@event;
using Godot;

namespace GFrameworkGodotTemplate.scripts.poker;

[ContextAware]
public partial class Poker : Button, IPoker, IController
{
    private IPokerStateMachine StateMachine => GetNode<PokerStateMachine>("%StateMachine");
    private TextureRect SurfaceRect => GetNode<TextureRect>("%SurfaceRect");

    private Guid Id { get; set; } = Guid.Empty;
    
    private Vector2 DefaultPosition { get; set; }
    private float DefaultRotation { get; set; }
    
    private Tween _tween = null!;

    public override void _Ready()
    {
        _ = ReadyAsync();
        ConnectSignal();
        RegisterEvent();
    }
    
    public override void _Process(double delta)
    {
        StateMachine.Process(delta);
    }

    private async Task ReadyAsync()
    {
        await GameEntryPoint.Architecture.WaitUntilReadyAsync().ConfigureAwait(false);
        
        StateMachine.Init(this);
        StateMachine.ChangeTo(StateType.Idle);
    }
    
    private void ConnectSignal()
    {
        ButtonDown += OnButtonDown;
        ButtonUp += OnButtonUp;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    private void RegisterEvent()
    {
        // 注册对状态变更事件的监听
        this.RegisterEvent<StateChangedEvent>(e =>
        {
            OnStateChangedEvent(e.NextState,e.Poker);
        }).UnRegisterWhenNodeExitTree(this);
    }
    
    public Guid GetId()
    {
        return Id;
    }

    public Vector2 GetSpawnPosition()
    {
        return DefaultPosition;
    }

    public void SetGlobalPosition(Vector2 pos)
    {
        GlobalPosition = pos;
    }

    public void SetRot(float angle)
    {
        Rotation = angle;
    }

    public void SetDefaultRotation(float angle)
    {
        DefaultRotation = angle;
    }
    
    public void SetDefaultPosition(Vector2 pos)
    {
        DefaultPosition = pos;
    }
    
    public void ResetPos()
    {
        // 如果正在播放动画，使其终止
        if (_tween != null && _tween.IsRunning()) _tween.Kill();
        
        _tween = CreateTween();
        _tween.SetEase(Tween.EaseType.InOut);
        _tween.SetTrans(Tween.TransitionType.Elastic);
        _tween.TweenProperty( this, "global_position", DefaultPosition, 0.25f);
    }
    
    public void ResetRot()
    {
        // 如果正在播放动画，使其终止
        if (_tween != null && _tween.IsRunning()) _tween.Kill();
        
        _tween = CreateTween();
        _tween.SetEase(Tween.EaseType.InOut);
        _tween.SetTrans(Tween.TransitionType.Elastic);
        _tween.TweenProperty(this, "rotation", Mathf.DegToRad(DefaultRotation), 0.25f);
    }

    public void ResetPosAndRot()
    {
        if (_tween != null && _tween.IsRunning()) _tween.Kill();
        
        _tween = CreateTween();
        _tween.SetParallel();
        _tween.SetEase(Tween.EaseType.InOut);
        _tween.SetTrans(Tween.TransitionType.Elastic);
        _tween.TweenProperty( this, "global_position", DefaultPosition, 0.25f);
        _tween.TweenProperty(this, "rotation", Mathf.DegToRad(DefaultRotation), 0.25f);
    }

    public void MoveTo(Vector2 pos)
    {
        if (_tween != null && _tween.IsRunning()) _tween.Kill();
        
        _tween = CreateTween();
        _tween.SetParallel();
        _tween.SetEase(Tween.EaseType.InOut);
        _tween.SetTrans(Tween.TransitionType.Elastic);
        _tween.TweenProperty( this, "global_position", pos, 0.25f);
    }

    public void ChangeTo(StateType state)
    {
        StateMachine.ChangeTo(state);
    }

    private void OnButtonDown()
    {
        StateMachine.MouseDown();
    }
    
    private void OnButtonUp()
    {
        StateMachine.MouseUp();
    }

    private void OnMouseEntered()
    {
        StateMachine.MouseEnter();
    }

    private void OnMouseExited()
    {
        StateMachine.MouseExit();
    }
    
    private void OnStateChangedEvent(StateType stateType,IPoker poker)
    {
        // 如果不是触发事件的poker，返回
        if (poker != this) return;
        
        StateMachine.ChangeTo(stateType);
    }
}