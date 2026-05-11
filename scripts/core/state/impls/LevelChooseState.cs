using GFramework.Core.Abstractions.State;
using GFramework.Core.State;
using GFramework.Game.Abstractions.UI;
using GFrameworkGodotTemplate.scripts.level;

namespace GFrameworkGodotTemplate.scripts.core.state.impls;

/// <summary>
///     关卡选择状态
///     负责管理关卡选择界面的显示逻辑
///     从主菜单的"关卡"按钮进入此状态
/// </summary>
public class LevelChooseState : AsyncContextAwareStateBase
{
    /// <summary>
    ///     状态进入时的处理方法
    ///     在进入关卡选择状态时，自动推入LevelChoose UI页面
    /// </summary>
    /// <param name="from">从哪个状态切换过来，可能为空</param>
    public override async Task OnEnterAsync(IState? from)
    {
        var uiRouter = this.GetSystem<IUiRouter>()!;
        await uiRouter.ReplaceAsync(LevelChoose.UiKeyStr).ConfigureAwait(true);
    }

    /// <summary>
    ///     判断是否可以切换到下一个状态
    /// </summary>
    /// <param name="target">目标状态</param>
    /// <returns>始终返回true，表示可以切换到任意状态</returns>
    public override Task<bool> CanTransitionToAsync(IState target)
    {
        return Task.FromResult(true);
    }
}
