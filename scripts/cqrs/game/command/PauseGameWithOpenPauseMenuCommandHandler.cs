using GFramework.Core.Cqrs.Command;
using GFramework.Game.Abstractions.UI;
using GFrameworkGodotTemplate.scripts.core.utils;
using GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command;
using Godot;

namespace GFrameworkGodotTemplate.scripts.cqrs.game.command;

/// <summary>
///     暂停游戏并打开暂停菜单命令处理器
///     <para>
///         实现事务性保护：如果打开菜单失败则自动回滚暂停状态
///         防止"僵尸暂停"状态（游戏已冻结但无UI可交互）
///     </para>
/// </summary>
public class
    PauseGameWithOpenPauseMenuCommandHandler : AbstractCommandHandler<PauseGameWithOpenPauseMenuCommand, UiHandle>
{
    public override ValueTask<UiHandle> Handle(PauseGameWithOpenPauseMenuCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            this.SendCommand(new PauseGameCommand());

            var handle = this.SendCommand(new OpenPauseMenuCommand(command.Input));
            return ValueTask.FromResult(handle);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[PauseGameWithOpenPauseMenu] ❌ 打开暂停菜单失败，执行回滚: {ex.Message}");
            
            RollbackPauseState();
            
            throw;
        }
    }

    /// <summary>回滚暂停状态到运行中</summary>
    private void RollbackPauseState()
    {
        try
        {
            var tree = GameUtil.GetTree();
            if (tree != null)
            {
                tree.Paused = false;
                GD.Print("[PauseGameWithOpenPauseMenu] ✓ 已回滚: Tree.Paused = false");
            }
            
            var stateMachine = this.GetSystem<GFramework.Core.Abstractions.State.IStateMachineSystem>();
            if (stateMachine != null)
            {
                stateMachine.ChangeToAsync<GFrameworkGodotTemplate.scripts.core.state.impls.PlayingState>()
                    .ToCoroutineEnumerator()
                    .RunCoroutine();
                GD.Print("[PauseGameWithOpenPauseMenu] ✓ 已回滚: 状态切换回 PlayingState");
            }
        }
        catch (Exception rollbackEx)
        {
            GD.PrintErr($"[PauseGameWithOpenPauseMenu] ⚠ 回滚操作也失败: {rollbackEx.Message}");
            GD.PrintErr("[PauseGameWithOpenPauseMenu] ⚠ 系统可能处于不一致状态，建议重启游戏");
        }
    }
}