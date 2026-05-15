using GFramework.Core.Abstractions.State;
using GFramework.Core.Cqrs.Command;
using GFrameworkGodotTemplate.global;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.core.utils;
using GFrameworkGodotTemplate.scripts.enums;
using Godot;
using Unit = Mediator.Unit;

namespace GFrameworkGodotTemplate.scripts.cqrs.game.command;

/// <summary>
///     恢复游戏命令处理器
///     <para>
///         负责从暂停状态恢复游戏运行
///         
///         修复说明(v2.1):
///         - 添加关卡阶段检查，避免在非Play阶段错误恢复
///         - Success/Defeat阶段时不应该恢复到PlayingState
///         - 防止场景状态不一致导致的跳转错误
///     </para>
/// </summary>
public class ResumeGameCommandHandler : AbstractCommandHandler<ResumeGameCommand>
{
	private IStateMachineSystem? _stateMachineSystem;

	public override ValueTask<Unit> Handle(ResumeGameCommand command, CancellationToken cancellationToken)
	{
		var tree = GameUtil.GetTree();
		
		if (tree == null)
		{
			GD.Print("[ResumeGameCommand] ❌ 无法获取SceneTree");
			return ValueTask.FromResult(Unit.Value);
		}

		var globalInputService = GetGlobalInputService(tree);
		
		if (globalInputService != null && globalInputService.CurrentPhase != LevelPhase.Play)
		{
			GD.Print($"[ResumeGameCommand] ⚠️ 当前阶段为 {globalInputService.CurrentPhase}，非Play阶段，跳过恢复到PlayingState");
			GD.Print("[ResumeGameCommand]   原因: Success/Defeat阶段不应恢复游戏，应通过UI按钮导航");
			
			tree.Paused = false;
			return ValueTask.FromResult(Unit.Value);
		}

		tree.Paused = false;
		
		(_stateMachineSystem ??= this.GetSystem<IStateMachineSystem>())!
			.ChangeToAsync<PlayingState>()
			.ToCoroutineEnumerator()
			.RunCoroutine();
		
		GD.Print("[ResumeGameCommand] ✅ 游戏已恢复 (当前阶段: Play)");
		
		return ValueTask.FromResult(Unit.Value);
	}

	/// <summary>
	///     获取全局输入服务（用于检查关卡阶段）
	/// </summary>
	private IGlobalGameplayInputService? GetGlobalInputService(Godot.SceneTree tree)
	{
		try
		{
			var globalController = tree.Root.GetNode<GlobalInputController>("GlobalInputController");
			return globalController?.GameplayInputService;
		}
		catch (Exception)
		{
			return null;
		}
	}
}