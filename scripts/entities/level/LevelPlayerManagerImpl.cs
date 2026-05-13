using Godot;
using GFrameworkGodotTemplate.scripts.level;
using GFrameworkGodotTemplate.scripts.level.interfaces;
using GFrameworkGodotTemplate.scripts.enums;

namespace GFrameworkGodotTemplate.scripts.entities.level;

/// <summary>
///     关卡玩家管理器实现（从BaseLevelController提取）
///     <para>
///         负责关卡场景中玩家角色的生成、生命周期管理和终点检测
///         遵循单一职责原则(SRP)，专注于玩家实体管理
///     </para>
/// </summary>
public class LevelPlayerManagerImpl : ILevelPlayerManager
{
    #region 私有字段

    /// <summary>所属的Node2D节点</summary>
    private readonly Node2D _ownerNode;

    /// <summary>玩家出生点位置节点</summary>
    private readonly Node2D? _beginPosition;

    /// <summary>终点碰撞区域</summary>
    private readonly Area2D? _endArea;

    /// <summary>日志记录器</summary>
    private readonly Action<string> _logInfo;
    private readonly Action<string> _logError;
    private readonly Action<string> _logDebug;

    /// <summary>玩家角色实例引用</summary>
    private Node2D? _playerInstance;

    /// <summary>是否已完成游戏</summary>
    private bool _isGameCompleted;

    /// <summary>获取当前阶段状态的委托（与BaseLevelController实时同步）</summary>
    private readonly Func<LevelPhase> _getCurrentPhase;

    /// <summary>玩家场景资源路径</summary>
    private const string PlayerScenePath = "res://scenes/player/player.tscn";

    /// <summary>游戏完成回调</summary>
    private Action? _onGameCompleteCallback;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建关卡玩家管理器实例
    /// </summary>
    /// <param name="ownerNode">所属节点</param>
    /// <param name="beginPosition">出生点位置</param>
    /// <param name="endArea">终点区域</param>
    /// <param name="getCurrentPhase">获取当前阶段状态的委托</param>
    /// <param name="logInfo">信息日志</param>
    /// <param name="logError">错误日志</param>
    /// <param name="logDebug">调试日志</param>
    public LevelPlayerManagerImpl(
        Node2D ownerNode,
        Node2D? beginPosition,
        Area2D? endArea,
        Func<LevelPhase> getCurrentPhase,
        Action<string>? logInfo = null,
        Action<string>? logError = null,
        Action<string>? logDebug = null)
    {
        _ownerNode = ownerNode;
        _beginPosition = beginPosition;
        _endArea = endArea;
        _getCurrentPhase = getCurrentPhase;
        
        _logInfo = logInfo ?? (msg => { });
        _logError = logError ?? (msg => { });
        _logDebug = logDebug ?? (msg => { });
    }

    #endregion

    #region ILevelPlayerManager 接口实现

    /// <summary>生成玩家角色</summary>
    public void SpawnPlayer()
    {
        _logInfo("[LevelPlayerManager] 正在生成玩家角色...");

        var playerScene = GD.Load<PackedScene>(PlayerScenePath);
        if (playerScene == null)
        {
            _logError($"[LevelPlayerManager] 无法加载玩家场景: {PlayerScenePath}");
            return;
        }

        _playerInstance = playerScene.Instantiate<Node2D>();

        if (_beginPosition != null)
        {
            _playerInstance.GlobalPosition = _beginPosition.GlobalPosition;
            _logDebug($"[LevelPlayerManager] 玩家位置设置为: {_beginPosition.GlobalPosition}");
        }

        _ownerNode.AddChild(_playerInstance);

        _logInfo("[LevelPlayerManager] ✓ 玩家角色已生成并添加到场景");
    }

    /// <summary>设置终点区域检测</summary>
    public void SetupEndAreaDetection()
    {
        _logInfo("[LevelPlayerManager] 正在设置终点区域检测...");

        if (_endArea == null)
        {
            _logError("[LevelPlayerManager] ✗ 未找到End区域节点（%End），游戏完成检测将不可用");
            _logError("[LevelPlayerManager] 请确保场景中存在名为'End'的Area2D节点，并设置了unique_name_in_owner = true");
            return;
        }

        _endArea.BodyEntered += OnPlayerEnteredEndArea;
        _logInfo("[LevelPlayerManager] ✓ End区域检测已成功设置");
        _logDebug($"[LevelPlayerManager] End区域位置: {_endArea.GlobalPosition}");
    }

    /// <summary>获取玩家实例</summary>
    public Node2D? PlayerInstance => _playerInstance;

    /// <summary>禁止玩家输入</summary>
    public void DisablePlayerInput()
    {
        if (_playerInstance != null)
        {
            _playerInstance.SetProcessInput(false);
            _logDebug("[LevelPlayerManager] ✓ 玩家输入已被禁用");
        }
    }

    /// <summary>清理玩家相关资源</summary>
    public void Cleanup()
    {
        if (_endArea != null)
        {
            _endArea.BodyEntered -= OnPlayerEnteredEndArea;
        }

        _isGameCompleted = false;
        _logDebug("[LevelPlayerManager] ✓ 玩家管理器已清理");
    }

    #endregion

    #region 公开属性

    /// <summary>获取或设置是否已完成游戏</summary>
    public bool IsGameCompleted
    {
        get => _isGameCompleted;
        set => _isGameCompleted = value;
    }

    /// <summary>获取当前阶段（实时从BaseLevelController获取）</summary>
    public LevelPhase CurrentPhase => _getCurrentPhase();

    /// <summary>设置游戏完成回调</summary>
    public Action? OnGameCompleteCallback
    {
        set => _onGameCompleteCallback = value;
    }

    #endregion

    #region 私有方法

    /// <summary>玩家进入终点的处理</summary>
    private void OnPlayerEnteredEndArea(Node body)
    {
        if (_isGameCompleted)
        {
            return;
        }

        var currentPhase = _getCurrentPhase();
        if (currentPhase != LevelPhase.Play)
        {
            _logDebug($"[LevelPlayerManager] 非游玩阶段忽略碰撞检测, 当前阶段: {currentPhase}");
            return;
        }

        var bodyName = body.Name.ToString();
        var isPlayer = bodyName.Contains("Player", StringComparison.OrdinalIgnoreCase) ||
                       bodyName.Contains("Character", StringComparison.OrdinalIgnoreCase) ||
                       body is CharacterBody2D;

        if (!isPlayer)
        {
            _logDebug($"[LevelPlayerManager] 忽略非玩家物体进入终点: {bodyName} (类型: {body.GetType().Name})");
            return;
        }

        _logInfo($"[LevelPlayerManager] ✓✓✓ 检测到玩家({bodyName})进入终点区域！类型: {body.GetType().Name}");
        _logInfo("[LevelPlayerManager] 触发游戏完成流程...");

        _onGameCompleteCallback?.Invoke();
    }

    #endregion
}
