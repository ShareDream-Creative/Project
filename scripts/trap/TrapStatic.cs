using Godot;

namespace GFrameworkGodotTemplate.scripts.trap;

/// <summary>
///     静态陷阱控制器（v2.0 - 全局静态事件版）
///     <para>
///         当玩家进入陷阱碰撞区域时，立即隐藏玩家角色
///         通过全局静态事件通知外部处理器执行后续的重生逻辑
///         
///         设计原理（v2.0 更新）:
///         - 遵循单一职责原则(SRP)：仅负责检测和隐藏
///         - 重生逻辑由 BaseLevelController 通过静态事件统一管理
///         - 使用全局静态事件而非实例信号，避免递归遍历
///     </para>
///     
///     <author>AI Assistant</author>
///     <version>2.0.0</version>
///     <date>2026-05-16</date>
///     
///     <description>
///         功能特性:
///         - Area2D 碰撞检测玩家进入
///         - 立即隐藏玩家节点 (Visible = false)
///         - 调用全局静态事件 TrapEventManager.TriggerPlayerReset()
///         - 支持配置是否自动重置玩家
///         - 完整的日志记录和错误处理
///         
///         v2.0 变更（遵循规范）:
///         - ✅ 移除实例信号 (TrapTriggered, PlayerResetReady)
///         - ✅ 改用全局静态事件 TrapEventManager.PlayerResetRequired
///         - ✅ 简化架构：无需递归遍历连接信号
///         - ✅ 统一入口：所有陷阱共享同一静态事件源
///         
///         使用场景:
///         - 教程关卡 (Teach_Level.tscn) 的陷阱区域
///         - 任何需要隐藏玩家的危险区域
///         
///         Godot配置要求:
///         1. 将此脚本挂载到 Trap_static.tscn 的根节点或 Area2D 子节点
///         2. 确保父节点包含 CollisionShape2D 定义碰撞区域
///         3. 玩家层需与陷阱的 CollisionMask 匹配
///     </description>
/// </summary>
[Log]
public partial class TrapStatic : Node2D
{
	#region 私有字段

	/// <summary>陷阱碰撞区域</summary>
	private Area2D? _trapArea;

	/// <summary>是否已触发（防止重复触发）</summary>
	private bool _isTriggered = false;

	/// <summary>是否在触发后自动重置状态</summary>
	[Export]
	public bool AutoReset { get; set; } = true;

	/// <summary>重置延迟时间（秒）</summary>
	[Export]
	public float ResetDelaySeconds { get; set; } = 0.5f;

	#endregion

	#region 生命周期方法

	/// <summary>节点就绪时初始化</summary>
	public override void _Ready()
	{
		InitializeTrapArea();
		
		_log.Info("[TrapStatic] ══════════ 陷阱初始化完成 ══════════");
		_log.Info($"[TrapStatic] 版本: v2.0 (全局静态事件模式)");
		_log.Info($"[TrapStatic] 自动重置: {AutoReset}, 重置延迟: {ResetDelaySeconds}秒");
	}

	#endregion

	#region 私有方法 - 初始化

	/// <summary>初始化陷阱碰撞区域</summary>
	private void InitializeTrapArea()
	{
		try
		{
			_trapArea = GetNodeOrNull<Area2D>("Area2D");
			
			if (_trapArea == null)
			{
				_log.Warn("[TrapStatic] ⚠️ 未找到 Area2D 子节点！");
				_log.Warn("[TrapStatic] 尝试使用自身作为碰撞区域...");
				
				if (this is Area2D)
				{
					_trapArea = (Area2D)(object)this;
					_log.Info("[TrapStatic] ✓ 使用自身作为碰撞区域");
				}
				else
				{
					_log.Error("[TrapStatic] ❌ 无法初始化碰撞区域！陷阱功能已禁用");
					return;
				}
			}
			
			_trapArea.BodyEntered += OnBodyEntered;
			
			_log.Info($"[TrapStatic] ✅ 陷阱碰撞区域已初始化");
			_log.Debug($"[TrapStatic] 节点名称: {_trapArea.Name}");
			_log.Debug($"[TrapStatic] 位置: {_trapArea.GlobalPosition}");
			_log.Debug($"[TrapStatic] 碰撞层: {_trapArea.CollisionLayer}, 掩码: {_trapArea.CollisionMask}");
		}
		catch (Exception ex)
		{
			_log.Error($"[TrapStatic] ❌ 初始化陷阱异常: {ex.Message}");
			_log.Error($"[TrapStatic] 异常类型: {ex.GetType().FullName}");
		}
	}

	#endregion

	#region 私有方法 - 事件处理

	/// <summary>
	///     当物体进入陷阱碰撞区域时的回调
	///     <param name="body">进入区域的物理实体</param>
	///     
	///     触发流程（符合规范）:
	 ///     1. 检查是否为玩家
	 ///     2. 执行 HidePlayer() 隐藏玩家
	 ///     3. 调用全局静态事件 TriggerPlayerReset()
	 ///        → 自动执行所有已注册的回调函数:
	 ///          a) 玩家位置恢复函数（→begin位置）
	 ///          b) 玩家模型显示恢复函数
	 ///          c) 玩家可视状态恢复函数
	 /// </summary>
	private void OnBodyEntered(Node body)
	{
		try
		{
			if (_isTriggered && !AutoReset)
			{
				_log.Debug("[TrapStatic] 陷阱已触发且不自动重置，忽略");
				return;
			}

			if (!IsPlayerBody(body))
			{
				_log.Debug($"[TrapStatic] 非玩家物体 ({body.Name}) 进入陷阱，忽略");
				return;
			}

			_isTriggered = true;
			
			_log.Info("💀 [TrapStatic] ═══════════ 陷阱触发 ═══════════");
			_log.Info($"[TrapStatic] 🎯 玩家 {body.Name} 进入陷阱区域！");
			
			var bodyNode2D = body as Node2D;
			if (bodyNode2D != null)
			{
				_log.Info($"[TrapStatic] 玩家位置: {bodyNode2D.GlobalPosition}");
			}

			HidePlayer(body);
			
			_log.Info("[TrapStatic] ✓ 玩家已隐藏，现在调用全局静态事件...");
			
			// 调用全局静态事件（统一入口）
			TrapEventManager.TriggerPlayerReset(body, "TrapStatic");

			_log.Info("[TrapStatic] ✓ 陷阱触发完成，全局事件已发送");

			if (AutoReset)
			{
				ScheduleReset();
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[TrapStatic] ❌ 处理陷阱触发异常: {ex.Message}");
			_log.Error($"[TrapStatic] 堆栈跟踪:\n{ex.StackTrace}");
		}
	}

	/// <summary>
	///     隐藏玩家节点
	///     <param name="playerNode">要隐藏的玩家节点</param>
	/// </summary>
	private void HidePlayer(Node playerNode)
	{
		try
		{
			_log.Info("[TrapStatic] 👁️ 正在隐藏玩家...");
			
			var canvasItem = playerNode as CanvasItem;
			if (canvasItem != null)
			{
				canvasItem.Visible = false;
				_log.Info("[TrapStatic] ✓ 玩家已隐藏 (Visible = false)");
			}
			else
			{
				playerNode.ProcessMode = Node.ProcessModeEnum.Disabled;
				_log.Info("[TrapStatic] ✓ 玩家已禁用（非CanvasItem节点）");
			}
			
			var characterBody = FindCharacterBody(playerNode);
			if (characterBody != null)
			{
				characterBody.Visible = false;
				_log.Debug("[TrapStatic] ✓ CharacterBody2D 也已隐藏");
				
				var collisionShape = characterBody.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
				if (collisionShape != null)
				{
					collisionShape.Disabled = true;
					_log.Debug("[TrapStatic] ✓ 碰撞形状已禁用（防止持续触发）");
				}
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[TrapStatic] ❌ 隐藏玩家异常: {ex.Message}");
		}
	}

	/// <summary>安排延迟重置</summary>
	private void ScheduleReset()
	{
		GetTree().CreateTimer(ResetDelaySeconds).Timeout += () =>
		{
			ResetState();
		};
	}

	/// <summary>重置陷阱状态</summary>
	private void ResetState()
	{
		_isTriggered = false;
		_log.Debug("[TrapStatic] ✓ 陷阱状态已重置，可再次触发");
	}

	#endregion

	#region 公开API

	/// <summary>
	///     手动显示玩家（保留向后兼容性，但推荐使用静态事件机制）
	///     <param name="playerNode">要显示的玩家节点</param>
	/// </summary>
	public void ShowPlayer(Node playerNode)
	{
		try
		{
			_log.Info("[TrapStatic] 👁️ 正在恢复玩家可见性...");
			
			var canvasItem = playerNode as CanvasItem;
			if (canvasItem != null)
			{
				canvasItem.Visible = true;
				_log.Info("[TrapStatic] ✓ 玩家已恢复可见");
			}
			else
			{
				playerNode.ProcessMode = Node.ProcessModeEnum.Inherit;
				_log.Info("[TrapStatic] ✓ 玩家已恢复处理（非CanvasItem节点）");
			}
			
			var characterBody = FindCharacterBody(playerNode);
			if (characterBody != null)
			{
				characterBody.Visible = true;
				
				var collisionShape = characterBody.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
				if (collisionShape != null)
				{
					collisionShape.Disabled = false;
				}
			}
			
			_log.Info("[TrapStatic] ✓ 玩家恢复可见性完成");
		}
		catch (Exception ex)
		{
			_log.Error($"[TrapStatic] ❌ 恢复玩家可见性异常: {ex.Message}");
		}
	}

	/// <summary>获取当前触发状态</summary>
	public bool IsTriggered => _isTriggered;

	#endregion

	#region 私有辅助方法

	/// <summary>检查是否为玩家节点</summary>
	private bool IsPlayerBody(Node body)
	{
		if (body == null) return false;
		
		return body.Name == "Player" || 
			   body.Name == "player" || 
			   body.GetParent()?.Name == "Player" ||
			   body.GetParent()?.Name == "player";
	}

	/// <summary>查找 CharacterBody2D 子节点</summary>
	private static CharacterBody2D? FindCharacterBody(Node node)
	{
		if (node is CharacterBody2D body) return body;
		
		return node.GetNodeOrNull<CharacterBody2D>("CharacterBody2D") ?? 
			   node.GetNodeOrNull<CharacterBody2D>("character_body_2d");
	}

	#endregion
}
