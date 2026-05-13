namespace GFrameworkGodotTemplate.scripts.level.interfaces;

/// <summary>
///     关卡规则集成器接口
///     <para>
///         定义关卡全局规则系统的标准契约，遵循单一职责原则(SRP)
///         负责规则控制器的初始化、更新和超时处理
///     </para>
/// </summary>
public interface ILevelRulesIntegration
{
    /// <summary>初始化规则控制器</summary>
    void Initialize();

    /// <summary>每帧更新规则状态</summary>
    void Update();

    /// <summary>清理规则系统资源</summary>
    void Cleanup();

    /// <summary>获取是否已初始化</summary>
    bool IsInitialized { get; }

    /// <summary>设置显示失败界面回调</summary>
    Func<Task>? ShowDefeatUiCallback { set; }

    /// <summary>设置禁止玩家输入回调</summary>
    Action? DisablePlayerInputCallback { set; }

    /// <summary>获取或设置是否已完成游戏</summary>
    bool IsGameCompleted { get; set; }
}
