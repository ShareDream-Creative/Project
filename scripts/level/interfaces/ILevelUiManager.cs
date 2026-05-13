using Godot;

namespace GFrameworkGodotTemplate.scripts.level.interfaces;

/// <summary>
///     关卡UI管理器接口
///     <para>
///         定义关卡场景中UI管理的标准契约，遵循单一职责原则(SRP)
///         负责UI界面的加载、切换、清理和状态管理
///     </para>
/// </summary>
public interface ILevelUiManager
{
    /// <summary>显示构建界面</summary>
    Task ShowBuildUiAsync();

    /// <summary>切换到游玩阶段</summary>
    void OnBuildFinished();

    /// <summary>显示成功界面</summary>
    void ShowSuccessUi();

    /// <summary>显示失败界面</summary>
    Task ShowDefeatUiAsync();

    /// <summary>清除现有UI</summary>
    Task ClearExistingUiAsync();

    /// <summary>连接BuildFinished信号</summary>
    void ConnectBuildFinishedSignal();
}
