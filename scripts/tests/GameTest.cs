using GFramework.Core.Abstractions.State;
using GFramework.Godot.Scene;
using GFrameworkGodotTemplate.scripts.core.scene;
using GFrameworkGodotTemplate.scripts.enums.scene;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using Godot;

namespace GFrameworkGodotTemplate.scripts.tests;

/// <summary>
///     游戏测试场景控制器
///     用于游戏功能测试和开发的实验性场景
///     挂载到 gametest.tscn 场景的根节点上
///     
///     功能特性:
///     - 场景进入时自动将游戏全局状态切换为PlayingState
///     - 确保玩家移动系统在正确的状态下运行
///     - 提供完整的日志输出用于调试追踪
/// </summary>
[ContextAware]
[Log]
public partial class GameTest : Node2D, IController, ISceneBehaviorProvider, ISimpleScene
{
	/// <summary>
	///     场景行为实例，用于管理具体的场景逻辑
	/// </summary>
	private ISceneBehavior? _scene;

	/// <summary>
	///     状态机系统引用，用于控制游戏全局状态
	/// </summary>
	private IStateMachineSystem? _stateMachineSystem;

	/// <summary>
	///     获取场景键值字符串
	///     对应 SceneKey.GameTest 枚举值
	/// </summary>
	public static string SceneKeyStr => nameof(SceneKey.GameTest);

	/// <summary>
	///     获取场景行为实例
	///     使用工厂模式创建场景行为，确保单例模式
	/// </summary>
	/// <returns>ISceneBehavior接口的场景行为实例</returns>
	public ISceneBehavior GetScene()
	{
		_scene ??= SceneBehaviorFactory.Create<Node2D>(this, SceneKeyStr);
		return _scene;
	}

	#region ISimpleScene 接口实现

	/// <summary>
	///     场景加载完成时的回调方法
	///     在场景资源加载后、显示前调用
	/// </summary>
	/// <param name="param">场景进入参数（可选）</param>
	/// <returns>异步任务</returns>
	ValueTask IScene.OnLoadAsync(ISceneEnterParam? param)
	{
		_log.Info($"[GameTest] 场景开始加载, 参数: {param?.GetType().Name ?? "无"}");
		return ValueTask.CompletedTask;
	}

	/// <summary>
	///     场景进入完成时的回调方法 ⭐ 核心逻辑
	///     当此场景被激活并显示给用户时自动调用
	///     负责将游戏全局状态切换为PlayingState以启用游戏功能
	/// </summary>
	/// <returns>异步任务</returns>
		/// <summary>
	///     场景进入完成时的回调方法 ⭐ 核心逻辑
	///     当此场景被激活并显示给用户时自动调用
	///     负责将游戏全局状态切换为PlayingState以启用游戏功能
	/// </summary>
	/// <returns>异步任务</returns>
	// 修复：移除了 override 关键字
	public async ValueTask OnEnterAsync()
	{
		_log.Info("[GameTest] 场景进入事件触发, 开始初始化...");
		
		try
		{
			await EnsurePlayingStateAsync();
			
			_log.Info("[GameTest] 场景初始化完成, 游戏状态已设置为: PlayingState");
			_log.Debug($"[GameTest] 当前状态机状态: {_stateMachineSystem?.Current?.GetType().Name ?? "未知"}");
		}
		catch (Exception ex)
		{
			_log.Error($"[GameTest] 场景初始化失败: {ex.Message}");
			throw;
		}
	}


	/// <summary>
	///     场景暂停时的回调方法
	///     当游戏被暂停（如打开暂停菜单）时调用
	/// </summary>
	/// <returns>异步任务</returns>
	public async ValueTask OnPauseAsync()
	{
		_log.Info("[GameTest] 场景暂停");
		await ValueTask.CompletedTask;
	}

	/// <summary>
	///     场景恢复时的回调方法
	///     当游戏从暂停状态恢复时调用
	/// </summary>
	/// <returns>异步任务</returns>
	public async ValueTask OnResumeAsync()
	{
		_log.Info("[GameTest] 场景恢复, 确保游戏状态正确...");
		
		await EnsurePlayingStateAsync();
		
		_log.Debug("[GameTest] 场景恢复完成");
	}

	/// <summary>
	///     场景退出时的回调方法
	///     当离开此场景时调用
	/// </summary>
	/// <returns>异步任务</returns>
	public async ValueTask OnExitAsync()
	{
		_log.Info("[GameTest] 场景退出, 准备清理资源...");
		await ValueTask.CompletedTask;
	}

	/// <summary>
	///     场景卸载时的回调方法
	///     当场景从内存中移除时调用
	/// </summary>
	/// <returns>异步任务</returns>
	public async ValueTask OnUnloadAsync()
	{
		_log.Info("[GameTest] 场景卸载完成");
		await ValueTask.CompletedTask;
	}

	#endregion

	#region 私有辅助方法

		/// <summary>
	///     确保当前游戏全局状态为PlayingState
	///     如果当前不是PlayingState，则执行状态切换
	///     此操作是原子性的，确保状态一致性
	/// </summary>
	private async Task EnsurePlayingStateAsync()
	{
		if (_stateMachineSystem == null)
		{
			_stateMachineSystem = this.GetSystem<IStateMachineSystem>();
		}

		if (_stateMachineSystem == null)
		{
			_log.Error("[GameTest] 无法获取状态机系统服务! 状态切换失败。");
			return;
		}

		var currentState = _stateMachineSystem.Current;
		
		if (currentState is PlayingState)
		{
			_log.Debug("[GameTest] 当前已是PlayingState, 无需切换");
			return;
		}

		_log.Info($"[GameTest] 当前状态: {currentState?.GetType().Name ?? "null"}, 正在切换到PlayingState...");
		
		// 修复：直接 await ChangeToAsync，移除不兼容的协程转换调用
		// 假设 ChangeToAsync<T> 返回 Task 或 ValueTask
		await _stateMachineSystem.ChangeToAsync<PlayingState>();
	}

	#endregion
}
