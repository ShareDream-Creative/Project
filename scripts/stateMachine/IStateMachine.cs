namespace GFrameworkGodotTemplate.scripts.stateMachine;

/// <summary>
/// 状态机接口，定义了状态机的基本属性和必须实现的功能
/// </summary>
public interface IStateMachine
{
    /// <summary>
    /// 每帧更新时调用的方法
    /// </summary>
    /// <param name="delta">帧率</param>
    void Process(double delta);
}