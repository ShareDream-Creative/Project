using GFramework.Core.Abstractions.State;
using GFramework.Core.State;
using GFramework.Game.Abstractions.UI;
using GFrameworkGodotTemplate.scripts.core.utils;
using GFrameworkGodotTemplate.scripts.utility;
using Godot;

namespace GFrameworkGodotTemplate.scripts.core.state.impls;

/// <summary>
///     游戏进行中状态
///     表示游戏当前处于运行阶段的状态管理类。
///     继承自ContextAwareStateBase，用于处理游戏运行时的逻辑。
///     
///     v2.0 修复:
///     - 不再硬编码加载HomeUi
///     - 使用PauseStateManager恢复到暂停前的UI界面
///     - 解决从暂停菜单返回时错误跳转到home.tscn的bug
/// </summary>
public class PlayingState : AsyncContextAwareStateBase
{
	public override async Task OnEnterAsync(IState? from)
	{
		var uiRouter = this.GetSystem<IUiRouter>();
		
		if (uiRouter == null)
		{
			GD.Print("[PlayingState] ⚠️ IUiRouter不可用，无法恢复UI");
			return;
		}

		if (PauseStateManager.HasSavedState())
		{
			var savedUiKey = PauseStateManager.GetSavedUiKey();
			
			GD.Print($"[PlayingState] 🔄 恢复到暂停前的UI: {savedUiKey}");
			
			await uiRouter.ReplaceAsync(savedUiKey).ConfigureAwait(true);
			
			PauseStateManager.ClearSavedState();
		}
		else
		{
			GD.Print("[PlayingState] ℹ️ 无保存的暂停状态，保持当前UI不变");
		}
	}
}