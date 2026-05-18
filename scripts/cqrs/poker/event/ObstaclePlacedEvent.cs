using GFrameworkGodotTemplate.scripts.poker;
using Godot;

namespace GFrameworkGodotTemplate.scripts.cqrs.poker.@event;

/// <summary>
///     障碍物放置事件
///     当 Poker 卡牌被成功拖拽到场地并放置障碍物时发布
/// </summary>
public class ObstaclePlacedEvent
{
    /// <summary>发起放置的扑克卡牌</summary>
    public IPoker Card { get; init; } = null!;

    /// <summary>实例化的障碍物节点</summary>
    public Node2D ObstacleInstance { get; init; } = null!;

    /// <summary>障碍物的世界坐标位置</summary>
    public Vector2 WorldPosition { get; init; }
}
