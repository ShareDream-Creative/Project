using GFrameworkGodotTemplate.scripts.enums.poker;
using GFrameworkGodotTemplate.scripts.stateMachine;

namespace GFrameworkGodotTemplate.scripts.poker;

/// <summary>
/// 扑克状态机接口，定义了扑克状态机的基本属性和必须实现的功能
/// </summary>
public interface IPokerStateMachine : IStateMachine
{
    /// <summary>
    ///     初始化
    /// </summary>
    /// <param name="poker">要代理的卡牌 <see cref="Poker"/> 实例</param>
    void Init(Poker poker);

    /// <summary>
    ///     更新到指定状态
    /// </summary>
    /// <param name="state">目标状态 <see cref="StateType"/> </param>
    void ChangeTo(StateType state);
    
    /// <summary>
    ///     鼠标点击卡牌时调用的方法
    /// </summary>
    void MouseDown();
    
    /// <summary>
    ///     鼠标释放卡牌时调用的方法
    /// </summary>
    void MouseUp();
    
    /// <summary>
    ///     鼠标进入卡牌时调用的方法
    /// </summary>
    void MouseEnter();
    
    /// <summary>
    ///     鼠标离开卡牌时调用的方法
    /// </summary>
    void MouseExit();
}