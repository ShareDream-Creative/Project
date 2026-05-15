using GFramework.Core.Abstractions.State;
using GFramework.Core.Cqrs.Command;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.core.utils;
using Godot;
using Unit = Mediator.Unit;

namespace GFrameworkGodotTemplate.scripts.cqrs.game.command;

/// <summary>
///     恢复游戏命令处理器
/// </summary>
public class ResumeGameCommandHandler : AbstractCommandHandler<ResumeGameCommand>
{
    private IStateMachineSystem? _stateMachineSystem;

    public override ValueTask<Unit> Handle(ResumeGameCommand command, CancellationToken cancellationToken)
    {
        var tree = GameUtil.GetTree();
        
        if (!tree.Paused)
        {
            GD.Print("[ResumeGame] ⚠ 游戏未处于暂停状态，忽略恢复请求");
            return ValueTask.FromResult(Unit.Value);
        }

        tree.Paused = false;
        (_stateMachineSystem ??= this.GetSystem<IStateMachineSystem>())!
            .ChangeToAsync<PlayingState>()
            .ToCoroutineEnumerator()
            .RunCoroutine();
        return ValueTask.FromResult(Unit.Value);
    }
}