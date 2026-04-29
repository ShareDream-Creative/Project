namespace GFrameworkGodotTemplate.scripts.stateMachine;

/// <summary>
/// 状态接口，定义了状态的基本属性和必须实现的功能
/// </summary>
public interface IState
{
    /// <summary>
    ///     每帧更新时调用的方法
    /// </summary>
    void Process(double delta);

    /// <summary>
    ///     进入状态时调用的方法
    /// </summary>
    void Enter();

    /// <summary>
    ///     退出状态时调用的方法
    /// </summary>
    void Exit();
}