using GFramework.Core.Abstractions.State;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.player.interfaces;

namespace GFrameworkGodotTemplate.scripts.player.state;

/// <summary>
///     玩家状态控制器实现
///     基于GFramework状态机系统的输入控制权管理
///     仅在PlayingState时允许玩家操作，其他状态完全禁用
/// </summary>
public class PlayerStateController : IPlayerStateController
{
	private IStateMachineSystem? _stateMachineSystem;

	/// <inheritdoc />
	public bool IsInputEnabled { get; private set; }

	/// <inheritdoc />
	public void Initialize()
	{
	}

	/// <summary>
	///     使用依赖注入设置状态机系统引用
	///     由PlayerMovementController在_Ready()中调用
	/// </summary>
	/// <param name="stateMachineSystem">框架状态机系统实例</param>
	public void SetStateMachineSystem(IStateMachineSystem stateMachineSystem)
	{
		_stateMachineSystem = stateMachineSystem;
	}

	/// <inheritdoc />
	public void UpdateState()
	{
		if (_stateMachineSystem == null)
		{
			IsInputEnabled = false;
			return;
		}

		IsInputEnabled = _stateMachineSystem.Current is PlayingState;
	}
}
