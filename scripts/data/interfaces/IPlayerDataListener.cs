namespace GFrameworkGodotTemplate.scripts.data.interfaces;

/// <summary>
///     玩家数据变更监听器接口
///     定义当PlayerData属性发生变化时的回调契约
///     
///     使用场景:
///     - 物理模块需要实时更新速度参数
///     - UI界面需要显示当前属性值
///     - 存储系统需要保存修改后的数据
///     - 调试工具需要监控数据变化
///     
///     设计原则:
///     - 观察者模式: 解耦数据源和数据消费者
///     - 单一职责: 监听器只关注变化事件
///     - 松耦合: 通过接口而非具体类通信
/// </summary>
public interface IPlayerDataListener
{
	/// <summary>
	///     当玩家移动速度发生变化时调用
	/// </summary>
	/// <param name="oldValue">旧的速度值(像素/秒)</param>
	/// <param name="newValue">新的速度值(像素/秒)</param>
	void OnSpeedChanged(float oldValue, float newValue);

	/// <summary>
	///     当玩家跳跃速度发生变化时调用
	/// </summary>
	/// <param name="oldValue">旧的跳跃速度值(像素/秒)</param>
	/// <param name="newValue">新的跳跃速度值(像素/秒)</param>
	void OnJumpVelocityChanged(float oldValue, float newValue);

	/// <summary>
	///     当重力加速度发生变化时调用
	/// </summary>
	/// <param name="oldValue">旧的重力值(像素/秒²)</param>
	/// <param name="newValue">新的重力值(像素/秒²)</param>
	void OnGravityChanged(float oldValue, float newValue);

	/// <summary>
	///     当奔跑速度倍率发生变化时调用
	/// </summary>
	/// <param name="oldValue">旧的奔跑倍率</param>
	/// <param name="newValue">新的奔跑倍率</param>
	void OnSprintMultiplierChanged(float oldValue, float newValue);
}
