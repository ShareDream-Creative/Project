using GFrameworkGodotTemplate.scripts.enums;
using GFrameworkGodotTemplate.scripts.level.config;

namespace GFrameworkGodotTemplate.scripts.core.controller.level.interfaces;

/// <summary>
///     关卡规则控制器接口
///     <para>
///         定义关卡全局规则管理的公共契约
///         包括阶段监测、计时系统、超时处理等核心功能
///     </para>
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-13</date>
///     <description>
///         核心职责:
///         1. 阶段监测: 检测当前关卡阶段状态（Play/Build/Success/Defeat）
///         2. 计时管理: 管理关卡计时器（启动/暂停/重置）
///         3. 超时检测: 监测是否超过最大时间限制
///         4. 事件通知: 通过事件机制通知状态变更
///         
///         设计原则:
///         - 接口隔离(ISP): 仅暴露必要的方法和属性
///         - 单一职责(SRP): 专注于规则管理，不涉及UI或业务逻辑
///         - 依赖倒置(DIP): 高层模块依赖此抽象而非具体实现
///         
///         使用场景:
///         - LevelRulesIntegrationImpl通过此接口管理规则控制器
///         - 可替换为不同的规则实现策略
///         - 支持单元测试时Mock实现
///     </description>
/// </summary>
public interface ILevelRulesController
{
	#region 事件

	/// <summary>
	///     计时器超时事件
	///     <para>
	///         当Play阶段的累计时间超过配置的MaxTimeMs阈值时触发
	///     </para>
	///     <param name="elapsedMs">已用时间（毫秒）</param>
	///     <param name="maxTimeMs">最大允许时间（毫秒）</param>
	/// </summary>
	event Action<long, long>? TimeOut;

	/// <summary>
	///     阶段状态变更事件
	///     <para>
	///         当检测到关卡阶段发生变化时触发
	 ///     </para>
	///     <param name="oldPhase">变更前的阶段</param>
	///     <param name="newPhase">变更后的阶段</param>
	/// </summary>
	event Action<LevelPhase, LevelPhase>? PhaseChanged;

	/// <summary>
	///     计时器Tick事件（每秒触发一次，用于UI显示更新）
	/// </summary>
	/// <param name="remainingMs">剩余时间（毫秒）</param>
	event Action<long>? TimerTick;

	#endregion

	#region 属性

	/// <summary>
	///     获取是否已初始化完成
	/// </summary>
	bool IsInitialized { get; }

	/// <summary>
	///     获取当前是否处于Play阶段
	/// </summary>
	bool IsInPlayPhase { get; }

	/// <summary>
	///     获取已用游戏时间（毫秒）
	/// </summary>
	long ElapsedGameTimeMs { get; }

	/// <summary>
	///     获取最大允许时间（毫秒）
	/// </summary>
	long MaxTimeMs { get; }

	/// <summary>
	///     获取计时器是否正在运行
	/// </summary>
	bool IsTimerRunning { get; }

	/// <summary>
	///     获取当前关卡配置（只读访问）
	/// </summary>
	LevelConfig Config { get; }

	#endregion

	#region 方法

	/// <summary>
	///     更新规则控制器状态
	///     <para>
	///         应在每帧调用以更新阶段检测和超时检查
	 ///     </para>
	void Update();

	/// <summary>
	///     初始化规则控制器
	///     <para>
	///         配置参数并启动初始监测
	 ///     </para>
	void Initialize();

	/// <summary>
	///     重置所有状态到初始值
	/// </summary>
	void Reset();

	/// <summary>
	///     重置计时器和超时状态
	///     <para>
	///         用于重新开始游戏或切换关卡时
	 ///     </para>
	/// </summary>
	void ResetTimer();

	/// <summary>
	///     清理资源
	/// </summary>
	void Cleanup();

	#endregion
}
