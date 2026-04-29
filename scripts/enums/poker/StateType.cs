namespace GFrameworkGodotTemplate.scripts.enums.poker;

/// <summary>
///     定义卡牌状态类型的键值枚举
///     用于标识和管理不同的卡牌状态类型
/// </summary>
public enum StateType
{
    /// <summary>
    ///     选择中
    /// </summary>
    OnSelect,
    
    /// <summary>
    ///     未选择
    /// </summary>
    UnSelect,
    
    /// <summary>
    ///     闲置中，默认的初始状态
    /// </summary>
    Idle,
    
    /// <summary>
    ///     拖拽中
    /// </summary>
    Drag,
}