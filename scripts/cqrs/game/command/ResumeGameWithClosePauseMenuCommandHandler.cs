using GFramework.Core.Cqrs.Command;
using GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command;
using GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command.input;
using GFrameworkGodotTemplate.scripts.core.utils;
using Godot;
using Unit = Mediator.Unit;

namespace GFrameworkGodotTemplate.scripts.cqrs.game.command;

/// <summary>
///     恢复游戏并关闭暂停菜单命令处理器
///     <para>
///         实现事务性保护：如果恢复游戏失败则重新打开菜单
///         防止"菜单消失但仍暂停"状态（UI已隐藏但游戏仍冻结）
///     </para>
/// </summary>
public class ResumeGameWithClosePauseMenuCommandHandler : AbstractCommandHandler<ResumeGameWithClosePauseMenuCommand>
{
    public override ValueTask<Unit> Handle(ResumeGameWithClosePauseMenuCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            this.SendCommand(new ClosePauseMenuCommand(command.Input));

            this.SendCommand(new ResumeGameCommand());

            return ValueTask.FromResult(Unit.Value);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ResumeGameWithClosePauseMenu] ❌ 恢复游戏失败，尝试补偿: {ex.Message}");
            
            CompensateMenuClosedState(command.Input);
            
            throw;
        }
    }

    /// <summary>补偿：菜单已关闭但游戏未恢复时重新显示菜单</summary>
    private void CompensateMenuClosedState(ClosePauseMenuCommandInput originalInput)
    {
        try
        {
            var openInput = new GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command.input.OpenPauseMenuCommandInput
            {
                Handle = originalInput.Handle
            };
            
            this.SendCommand(new GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command.OpenPauseMenuCommand(openInput));
            GD.Print("[ResumeGameWithClosePauseMenu] ✓ 已补偿: 重新打开暂停菜单以保持可用状态");
        }
        catch (Exception compensateEx)
        {
            GD.PrintErr($"[ResumeGameWithClosePauseMenu] ⚠ 补偿操作失败: {compensateEx.Message}");
            GD.PrintErr("[ResumeGameWithClosePauseMenu] ⚠ 系统可能处于不一致状态");
        }
    }
}