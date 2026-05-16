using Godot;

namespace GFrameworkGodotTemplate.scripts.level.interfaces;

/// <summary>
///     玩家重置处理器接口
///     <para>
///         定义玩家重置相关的所有行为操作，
///         实现数据与行为的分离。
///         
///         设计原理:
///         - 接口隔离原则 (ISP)
///         - 便于单元测试（可 Mock）
///         - 支持不同的重置策略实现
///     </para>
///     
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-16</date>
///     
///     <description>
	///         职责范围:
	///         - 玩家位置恢复（→begin位置）
	///         - 玩家可见性恢复
	///         - 物理状态清除
	///         - 完整重置流程协调
	///         
	///         使用场景:
	///         - 陷阱触发后的玩家重置
	///         - Defeat 区域的玩家重生
	///         - 任何需要将玩家重置到初始位置的场景
	///         
	///         实现要求:
	///         必须保证原子性操作，避免部分恢复导致异常状态。
	///     </description>
/// </summary>
public interface IPlayerResetHandler
{
	#region 回调属性

	/// <summary>玩家重生完成回调</summary>
	Action<Node2D, Vector2>? OnPlayerRespawnedCallback { get; set; }

	#endregion

	#region 核心重置方法

	/// <summary>
	///     执行完整的玩家重置流程
	///     <param name="playerNode">要重置的玩家节点</param>
	/// <remarks>
	 ///         执行顺序（严格遵循规范）:
	 ///         1. 恢复可见性（先显示，再移动）
	 ///         2. 移动到 Begin 位置
	 ///         3. 重置物理状态（速度清零 + 地面状态）
	 ///         4. 触发生成完成回调
	 ///         
	 ///         原子性保证:
	 ///         所有步骤包裹在异常处理中，
	 ///         单个步骤失败不阻止后续步骤。
	 ///     </remarks>
	/// </summary>
	void ExecuteFullPlayerReset(Node playerNode);

	#endregion

	#region 基础操作方法

	/// <summary>
	///     将玩家移动到 Begin 位置
	///     <param name="playerNode">要移动的玩家节点</param>
	/// </summary>
	void MovePlayerToBeginPosition(Node playerNode);

	/// <summary>
	///     恢复玩家可见性
	///     <param name="playerNode">要显示的玩家节点</param>
	/// </summary>
	void RestorePlayerVisibility(Node playerNode);

	/// <summary>
	///     重置玩家的物理状态
	///     <param name="playerNode">要重置物理状态的玩家节点</param>
	/// </summary>
	void ResetPhysicsStateForPlayer(Node playerNode);

	/// <summary>
	///     重置 CharacterBody2D 的物理状态
	///     <param name="characterBody">要重置的物理实体</param>
	/// </summary>
	void ResetPlayerPhysicsState(CharacterBody2D characterBody);

	#endregion

	#region 事件订阅管理

	/// <summary>
	///     订阅全局陷阱事件
	/// </summary>
	void SubscribeToGlobalTrapEvents();

	/// <summary>
	///     取消订阅全局陷阱事件
	/// </summary>
	void UnsubscribeFromGlobalTrapEvents();

	#endregion
}
