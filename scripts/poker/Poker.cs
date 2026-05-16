using GFrameworkGodotTemplate.scripts.enums.poker;
using GFrameworkGodotTemplate.global;
using GFrameworkGodotTemplate.scripts.cqrs.poker.@event;
using GFrameworkGodotTemplate.scripts.level;
using GFrameworkGodotTemplate.scripts.utility;
using Godot;

namespace GFrameworkGodotTemplate.scripts.poker;

[ContextAware]
public partial class Poker : Button, IPoker, IController
{
    [Signal] public delegate void CardPlacedEventHandler();

    private IPokerStateMachine StateMachine => GetNode<PokerStateMachine>("%StateMachine");
    private TextureRect SurfaceRect => GetNode<TextureRect>("%SurfaceRect");

    private Guid Id { get; set; } = Guid.Empty;

    private Vector2 DefaultPosition { get; set; }
    private float DefaultRotation { get; set; }

    private Tween _tween = null!;

    [Export] public PackedScene? ObstacleScene { get; set; }

    private BaseLevelController? _levelController;

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
        _tween.TweenProperty( this, "position", DefaultPosition, 0.25f);
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
        _tween.TweenProperty( this, "position", DefaultPosition, 0.25f);
        _tween.TweenProperty(this, "rotation", Mathf.DegToRad(DefaultRotation), 0.25f);
    }

    public void MoveTo(Vector2 pos)
    {
        if (_tween != null && _tween.IsRunning()) _tween.Kill();
        
        _tween = CreateTween();
        _tween.SetParallel();
        _tween.SetEase(Tween.EaseType.InOut);
        _tween.SetTrans(Tween.TransitionType.Elastic);
        _tween.TweenProperty( this, "position", pos, 0.25f);
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

    public void SetObstacleScene(PackedScene scene)
    {
        ObstacleScene = scene;
    }

    public PackedScene? GetObstacleScene()
    {
        return ObstacleScene;
    }

    public bool TryPlaceObstacle()
    {
        if (ObstacleScene == null)
        {
            GD.Print("[Poker] ObstacleScene 未配置，无法放置障碍物");
            return false;
        }

        if (!BaseLevelController.IsBuildPhaseActive)
        {
            GD.Print("[Poker] 当前不在 Build 阶段，无法放置障碍物");
            return false;
        }

        try
        {
            var worldPos = GetViewport().GetMousePosition();

            _levelController ??= NodeTreeHelper.FindLevelController(this, "[Poker]");

            var parentNode = FindObstacleContainer(_levelController);

            var obstacle = ObstacleScene.Instantiate<Node2D>();
            parentNode.AddChild(obstacle);
            obstacle.GlobalPosition = worldPos;

            GD.Print($"[Poker] 障碍物已放置: {obstacle.Name} at {worldPos}");

            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Poker] 放置障碍物失败: {ex.Message}");
            return false;
        }
    }

    private Node FindObstacleContainer(BaseLevelController? levelController)
    {
        if (levelController != null)
            return GetOrCreateObstaclesContainer(levelController);

        levelController = FindLevelControllerFromSceneRoot();
        if (levelController != null)
        {
            _levelController = levelController;
            return GetOrCreateObstaclesContainer(levelController);
        }

        GD.PrintErr("[Poker] 无法找到关卡控制器，障碍物放置失败");
        return ((SceneTree)Engine.GetMainLoop()).CurrentScene;
    }

    private static BaseLevelController? FindLevelControllerFromSceneRoot()
    {
        var tree = ((SceneTree)Engine.GetMainLoop());
        var sceneRoot = tree.Root.GetNodeOrNull<Node>("SceneRoot");
        if (sceneRoot == null) return null;

        foreach (var child in sceneRoot.GetChildren())
        {
            if (child is BaseLevelController controller)
                return controller;
        }

        return null;
    }

    private static Node GetOrCreateObstaclesContainer(BaseLevelController levelController)
    {
        var container = levelController.GetNodeOrNull<Node>("Obstacles");
        if (container != null)
            return container;

        container = new Node2D { Name = "Obstacles" };
        levelController.AddChild(container);
        return container;
    }
}