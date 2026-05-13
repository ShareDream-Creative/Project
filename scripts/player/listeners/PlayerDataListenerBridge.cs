using GFrameworkGodotTemplate.scripts.data.interfaces;

namespace GFrameworkGodotTemplate.scripts.player.listeners;

/// <summary>
///     玩家数据监听器桥接器
///     <para>
///         负责将PlayerData变更事件桥接到日志系统
///         实现IPlayerDataListener接口，支持数据变更监控
///     </para>
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-13</date>
///     <description>
///         核心职责:
///         1. 事件桥接: 将PlayerData变更事件转换为日志输出
///         2. 数据监控: 监控Speed/JumpVelocity/Gravity/SprintMultiplier变化
///         3. 解耦设计: 将监听逻辑从主控制器中分离，提升内聚性
///         
///         设计模式:
///         - 桥接模式(Bridge): 连接数据源和日志系统
///         - 观察者模式(Observer): 实现IPlayerDataListener接口
///         
///         使用场景:
///         - PlayerMovementController通过组合方式使用此监听器
///         - 可独立进行单元测试
///         - 未来可扩展为发送事件或更新UI
///     </description>
/// </summary>
public class PlayerDataListenerBridge : IPlayerDataListener
{
	#region 日志回调

	private readonly Action<string>? _logInfo;
	private readonly Action<string>? _logWarn;
	private readonly Action<string>? _logError;
	private readonly Action<string>? _logDebug;

	#endregion

	#region 构造函数

	/// <summary>
	///     创建玩家数据监听器实例
	/// </summary>
	/// <param name="logInfo">信息日志回调（可选）</param>
	/// <param name="logWarn">警告日志回调（可选）</param>
	/// <param name="logError">错误日志回调（可选）</param>
	/// <param name="logDebug">调试日志回调（可选）</param>
	public PlayerDataListenerBridge(
		Action<string>? logInfo = null,
		Action<string>? logWarn = null,
		Action<string>? logError = null,
		Action<string>? logDebug = null)
	{
		_logInfo = logInfo;
		_logWarn = logWarn;
		_logError = logError;
		_logDebug = logDebug;
	}

	#endregion

	#region IPlayerDataListener 实现

	/// <inheritdoc />
	public void OnSpeedChanged(float oldValue, float newValue)
	{
		_logInfo?.Invoke($"[PlayerMovementController] 检测到速度变化: {oldValue} → {newValue}");
	}

	/// <inheritdoc />
	public void OnJumpVelocityChanged(float oldValue, float newValue)
	{
		_logInfo?.Invoke($"[PlayerMovementController] 检测到跳跃速度变化: {oldValue} → {newValue}");
	}

	/// <inheritdoc />
	public void OnGravityChanged(float oldValue, float newValue)
	{
		_logInfo?.Invoke($"[PlayerMovementController] 检测到重力变化: {oldValue} → {newValue}");
	}

	/// <inheritdoc />
	public void OnSprintMultiplierChanged(float oldValue, float newValue)
	{
		_logInfo?.Invoke($"[PlayerMovementController] 检测到奔跑倍率变化: {oldValue} → {newValue}");
	}

	#endregion
}
