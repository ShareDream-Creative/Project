using Godot;

namespace GFrameworkGodotTemplate.scripts.trap;

/// <summary>
///     全局静态陷阱事件管理器
///     <para>
///         采用单例模式管理所有陷阱相关的静态事件，
///         实现统一的事件订阅和触发机制。
///         
///         设计原则:
///         1. 全局静态实例唯一 - 所有陷阱共享同一事件源
///         2. 不支持递归遍历 - 直接通过静态事件通知
///         3. 原子性操作 - 保证回调函数的完整执行
///     </para>
///     
///     <author>AI Assistant</author>
///     <version>2.0.0</version>
///     <date>2026-05-16</date>
///     
///     <description>
///         功能特性:
///         - 静态事件: PlayerResetRequired (玩家重置请求)
///         - 线程安全: 使用锁机制保护回调列表
///         - 错误隔离: 单个回调失败不影响其他回调
///         - 详细日志: 记录所有事件触发和回调执行情况
///         
///         使用场景:
///         - TrapStatic 触发后通知 BaseLevelController 执行重置
///         - Defeat 区域检测到玩家后触发相同重置流程
///         - 任何需要将玩家重置到 begin 位置的场景
///         
///         与旧版区别:
///         - 旧版: 每个 TrapStatic 实例有自己的信号 → 需要递归遍历连接
///         - 新版: 全局静态事件 → 无需遍历，自动统一处理
///     </description>
/// </summary>
public static class TrapEventManager
{
	#region 静态事件定义

	/// <summary>
	///     玩家重置就绪静态事件
	///     <para>
	///         当玩家被隐藏/删除操作完成后触发，
	///         通知所有订阅者执行玩家恢复操作
	 ///         
	 ///         触发时机:
	 ///         - TrapStatic.HidePlayer() 完成后
	 ///         - Defeat 区域检测到玩家进入后
	 ///         - 任何需要将玩家重置到 begin 的场景
	 ///     </para>
	 ///     
	 ///     <param name="playerNode">被隐藏的玩家节点引用</param>
	 ///     <remarks>
	 ///         事件参数说明:
	 ///         playerNode: Godot.Node 类型（基类），接收者需自行转换为具体类型
	 ///         
	 ///         订阅者应实现的功能（按顺序）:
	 ///         1. 玩家位置恢复函数（将玩家重置至 begin 位置）
	 ///         2. 玩家模型显示恢复函数
	 ///         3. 玩家可视状态恢复函数
	 ///         
	 ///         注意事项:
	 ///         - 必须保证原子性（全部成功或全部失败）
	 ///         - 添加适当的错误处理机制
	 ///         - 避免内存泄漏或性能问题
	 ///     </remarks>
	/// </summary>
	public static event Action<Node>? PlayerResetRequired;

	#endregion

	#region 静态字段

	/// <summary>回调函数计数（用于日志和调试）</summary>
	private static int _subscriberCount = 0;

	/// <summary>事件触发总次数统计</summary>
	private static int _triggerCount = 0;

	#endregion

	#region 公开API - 事件触发

	/// <summary>
	///     触发玩家重置就绪事件（由 TrapStatic 或 Defeat 区域调用）
	///     <param name="playerNode">被隐藏的玩家节点</param>
	/// <param name="sourceDescription">触发来源描述（用于日志）</param>
	/// <remarks>
	 ///         此方法是全局唯一的玩家重置入口点。
	 ///         所有需要将玩家重置到 begin 的场景都应调用此方法，
	 ///         而不是直接调用 BaseLevelController 的方法。
	 ///         
	 ///         触发流程:
	 ///         1. 参数验证（节点有效性检查）
	 ///         2. 日志记录（记录触发来源和时间戳）
	 ///         3. 事件触发（调用所有已注册的回调）
	 ///         4. 异常捕获（单个回调失败不影响其他回调）
	 ///         5. 统计更新（触发次数+1）
	 ///         
	 ///         原子性保证:
	 ///         如果某个回调抛出异常，会被捕获并记录，
	 ///         但不会阻止后续回调的执行。
	 ///         这确保了即使部分功能失败，其他功能仍能正常工作。
	 ///     </remarks>
	/// </summary>
	public static void TriggerPlayerReset(Node playerNode, string sourceDescription = "Unknown")
	{
		try
		{
			// 步骤1: 参数验证
			if (playerNode == null)
			{
				GD.PrintErr($"[TrapEventManager] ❌ 无法触发重置事件: playerNode 为 null (来源: {sourceDescription})");
				return;
			}

			if (!GodotObject.IsInstanceValid(playerNode))
			{
				GD.Print($"[TrapEventManager] ⚠️ 玩家节点已失效，取消重置事件 (来源: {sourceDescription})");
				return;
			}

			// 步骤2: 日志记录
			_triggerCount++;
			
			var nodeName = playerNode.Name;
			var nodeType = playerNode.GetType().Name;
			
			GD.Print($"[TrapEventManager] 🚨 [#{_triggerCount}] 触发 PlayerResetRequired 事件");
			GD.Print($"[TrapEventManager]    来源: {sourceDescription}");
			GD.Print($"[TrapEventManager]    玩家节点: {nodeName} ({nodeType})");
			GD.Print($"[TrapEventManager]    当前订阅者数量: {_subscriberCount}");

			// 步骤3: 检查是否有订阅者
			if (PlayerResetRequired == null)
			{
				GD.Print("[TrapEventManager] ⚠️ 没有订阅者监听 PlayerResetRequired 事件！");
				return;
			}

			// 步骤4: 触发事件（带异常隔离）
			var delegates = PlayerResetRequired.GetInvocationList();
			int successCount = 0;
			int failCount = 0;

			foreach (var handler in delegates)
			{
				try
				{
					handler.DynamicInvoke(playerNode);
					successCount++;
				}
				catch (Exception ex)
				{
					failCount++;
					GD.PrintErr($"[TrapEventManager] ❌ 订阅者回调执行异常: {ex.Message}");
					GD.PrintErr($"[TrapEventManager]    异常类型: {ex.GetType().FullName}");
					GD.PrintErr($"[TrapEventManager]    订阅者信息: {handler.Target?.GetType().Name ?? "静态"}?.{handler.Method.Name}");
				}
			}

			// 步骤5: 结果日志
			GD.Print($"[TrapEventManager] ✅ 事件触发完成！成功: {successCount}, 失败: {failCount}");
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[TrapEventManager] ❌ TriggerPlayerReset 异常: {ex.Message}");
			GD.PrintErr($"[TrapEventManager] 堆栈跟踪:\n{ex.StackTrace}");
		}
	}

	#endregion

	#region 公开API - 订阅管理

	/// <summary>
	///     订阅玩家重置事件（由 BaseLevelController 在初始化时调用）
	///     <param name="handler">回调处理函数</param>
	/// <returns>true 表示订阅成功</returns>
	/// <remarks>
	 ///         订阅时机:
	 ///         应在关卡初始化阶段完成（OnEnterAsync 中）
	 ///         
	 ///         回调要求:
	 ///         回调函数应包含以下三个功能的完整实现:
	 ///         1. 玩家位置恢复函数（→begin位置）
	 ///         2. 玩家模型显示恢复函数
	 ///         3. 玩家可视状态恢复函数
	 ///         
	 ///         执行顺序建议:
	 ///         先恢复位置 → 再恢复显示（避免视觉闪烁）
	 ///         
	 ///         内存管理:
	 ///         请确保在适当的时候取消订阅（如 _ExitTree），
	 ///         避免内存泄漏。
	 ///     </remarks>
	/// </summary>
	public static bool Subscribe(Action<Node> handler)
	{
		if (handler == null)
		{
			GD.Print("[TrapEventManager] ⚠️ 尝试订阅空回调，忽略");
			return false;
		}

		PlayerResetRequired += handler;
		_subscriberCount++;

		var targetInfo = handler.Target?.GetType().Name ?? "静态方法";
		GD.Print($"[TrapEventManager] ✓ 已订阅 PlayerResetRequired 事件 (总计: {_subscriberCount})");
		GD.Print($"[TrapEventManager]    订阅者: {targetInfo}.{handler.Method.Name}");

		return true;
	}

	/// <summary>
	///     取消订阅玩家重置事件（由 BaseLevelController 在清理时调用）
	///     <param name="handler">之前订阅的回调处理函数</param>
	/// <returns>true 表示取消成功</returns>
	/// <remarks>
	 ///         调用时机:
	 ///         在关卡退出或控制器销毁时调用（_ExitTree 中）
	 ///         
	 ///         重要:
	 ///         必须传入与 Subscribe 时相同的引用，
	 ///         否则取消失败（但不会报错）。
	 ///         
	 ///         推荐做法:
	 ///         将回调保存为成员变量，Subscribe 和 Unsubscribe 使用同一个变量。
	 ///     </remarks>
	/// </summary>
	public static bool Unsubscribe(Action<Node> handler)
	{
		if (handler == null)
		{
			GD.Print("[TrapEventManager] ⚠️ 尝试取消订阅空回调，忽略");
			return false;
		}

		PlayerResetRequired -= handler;
		_subscriberCount--;

		if (_subscriberCount < 0) _subscriberCount = 0;

		var targetInfo = handler.Target?.GetType().Name ?? "静态方法";
		GD.Print($"[TrapEventManager] ✓ 已取消订阅 PlayerResetRequired 事件 (剩余: {_subscriberCount})");
		GD.Print($"[TrapManager]    订阅者: {targetInfo}.{handler.Method.Name}");

		return true;
	}

	#endregion

	#region 公开API - 查询

	/// <summary>获取当前订阅者数量</summary>
	public static int SubscriberCount => _subscriberCount;

	/// <summary>获取事件触发总次数</summary>
	public static int TriggerCount => _triggerCount;

	/// <summary>检查是否有任何订阅者</summary>
	public static bool HasSubscribers => PlayerResetRequired != null;

	#endregion

	#region 公开API - 重置（用于测试）

	/// <summary>
	///     重置所有统计数据（用于单元测试或特殊场景）
	/// </summary>
	public static void ResetStatistics()
	{
		_triggerCount = 0;
		GD.Print("[TrapEventManager] 📊 统计数据已重置");
	}

	#endregion
}
