using Godot;

namespace GFrameworkGodotTemplate.scripts.level.interfaces;

/// <summary>
///     关卡玩家管理器接口
///     <para>
///         定义关卡场景中玩家角色管理的标准契约，遵循单一职责原则(SRP)
///         负责玩家的生成、生命周期管理和终点检测
///     </para>
/// </summary>
public interface ILevelPlayerManager
{
    /// <summary>生成玩家角色</summary>
    void SpawnPlayer();

    /// <summary>设置终点区域检测</summary>
    void SetupEndAreaDetection();

    /// <summary>获取玩家实例</summary>
    Node2D? PlayerInstance { get; }

    /// <summary>禁止玩家输入</summary>
    void DisablePlayerInput();

    /// <summary>清理玩家相关资源</summary>
    void Cleanup();

    /// <summary>设置游戏完成回调</summary>
    Action? OnGameCompleteCallback { set; }
}
