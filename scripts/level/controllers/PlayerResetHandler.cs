using Godot;
using GFrameworkGodotTemplate.scripts.level.config;
using GFrameworkGodotTemplate.scripts.level.interfaces;
using GFrameworkGodotTemplate.scripts.trap;
using GFrameworkGodotTemplate.scripts.player;

namespace GFrameworkGodotTemplate.scripts.level.controllers;

/// <summary>
///     玩家重置处理器实现类
///     <para>
///         从 BaseLevelController 中提取的玩家重置相关行为逻辑，
///         通过依赖 LevelControllerData 获取配置和节点引用。
///         
///         设计原理:
///         - 单一职责原则 (SRP)
///         - 依赖注入 (DI)
///         - 可测试性（可通过 Mock 数据进行单元测试）
///     </para>
///     
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-16</date>
///     
///     <description>
	///         功能特性:
	///         - 完整的玩家重置流程（4步原子操作）
	///         - 全局静态陷阱事件订阅管理
	///         - 物理状态完全清除
	///         - 防御性编程（多重有效性检查）
	///         
	///         使用方式:
	///         由 BaseLevelController 创建并持有，
	///         通过 IPlayerResetHandler 接口暴露功能。
	///         
	///         依赖项:
	///         - LevelControllerData: 配置和状态数据
	///         - TrapEventManager: 全局静态事件
	 ///     </description>
/// </summary>
[Log]
public partial class PlayerResetHandler : IPlayerResetHandler
{
	#region 私有字段

	/// <summary>关卡控制器数据引用</summary>
	private readonly LevelControllerData _data;

	/// <summary>全局陷阱事件回调引用</summary>
	private Action<Node>? _trapEventCallback;

	/// <summary>玩家重生完成回调</summary>
	public Action<Node2D, Vector2>? OnPlayerRespawnedCallback { get; set; }

	#endregion

	#region 构造函数

	/// <summary>
	///     构造函数
	///     <param name="data">关卡控制器数据实例</param>
	/// </summary>
	public PlayerResetHandler(LevelControllerData data)
	{
		_data = data ?? throw new ArgumentNullException(nameof(data));
	}

	#endregion

	#region IPlayerResetHandler 实现 - 核心重置方法

	/// <inheritdoc />
	public void ExecuteFullPlayerReset(Node playerNode)
	{
		try
		{
			if (_data.IsGameCompleted)
			{
				_log.Debug("[PlayerResetHandler] 游戏已完成，忽略重置请求");
				return;
			}

			if (!GodotObject.IsInstanceValid(playerNode))
			{
				_log.Warn("[PlayerResetHandler] ⚠️ 玩家节点已失效");
				return;
			}

			_log.Info("[PlayerResetHandler] ══════════ 开始执行完全重置 ══════════");

			_log.Info("[PlayerResetHandler] 步骤1/3: 恢复玩家可见性...");
			RestorePlayerVisibility(playerNode);

			_log.Info("[PlayerResetHandler] 步骤2/3: 重置玩家物理状态...");
			ResetPhysicsStateForPlayer(playerNode);

			_log.Info("[PlayerResetHandler] 步骤3/3: 移动玩家到 Begin 位置...");
			
			// 执行位置重置（MoveAndSlide 将在内部调用以确保碰撞有效）
			MovePlayerToBeginPosition(playerNode);

			// 触发生成完成回调
			var beginPos = _data.GetBeginPositionOrDefault();
			var playerNode2D = ResolveToNode2D(playerNode);
			if (playerNode2D != null)
			{
				OnPlayerRespawnedCallback?.Invoke(playerNode2D, beginPos);
			}
			else
			{
				_log.Warn("[PlayerResetHandler] ⚠️ 无法转换玩家节点为 Node2D，跳过回调");
			}

			_log.Info("[PlayerResetHandler] ✓✓✓ 完全重置完成 ══════════");
		}
		catch (Exception ex)
		{
			_log.Error($"[PlayerResetHandler] ❌ 执行重置异常: {ex.Message}");
		}
	}

	#endregion

	#region IPlayerResetHandler 实现 - 基础操作方法

	/// <inheritdoc />
	public void MovePlayerToBeginPosition(Node playerNode)
	{
		try
		{
			var targetPosition = _data.GetBeginPositionOrDefault();

			var characterBody = FindCharacterBodyInPlayer(playerNode);
			if (characterBody != null)
			{
				characterBody.Velocity = Vector2.Zero;
				characterBody.GlobalPosition = targetPosition;
				
				// 注意：不需要在这里调用 MoveAndSlide()
				// 因为 ResetFromTrap() 已经通过 _physicsMovement.StopImmediately()
				// 完全重置了物理模块的速度状态，下一帧 ProcessMovement() 会正确处理
				
				_log.Info("[PlayerResetHandler] ✓ 玩家已移动到 Begin 位置");
			}
			else
			{
				var playerNode2D = playerNode as Node2D;
				if (playerNode2D != null)
				{
					playerNode2D.GlobalPosition = targetPosition;
					_log.Info("[PlayerResetHandler] ✓ 玩家已移动到默认位置");
				}
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[PlayerResetHandler] ❌ 移动位置异常: {ex.Message}");
		}
	}

	/// <summary>
	///     恢复玩家可见性
	///     <para>
	///         关键改进：使用 Timer 延迟启用碰撞体
	///         避免 "flushing queries" 错误和时序冲突
	///         
	///         问题背景：
	///         TrapStatic 使用 CallDeferred 在下一帧禁用碰撞体
	///         如果我们立即启用，会被延迟的禁用覆盖
	///         解决方案：我们也延迟，确保在帧N+2才启用
	///     </para>
	///     <param name="playerNode">玩家节点</param>
	/// </summary>
	public void RestorePlayerVisibility(Node playerNode)
	{
		try
		{
			_log.Info("[PlayerResetHandler] 正在恢复玩家可见性...");

			var canvasItem = playerNode as CanvasItem;
			if (canvasItem != null)
			{
				canvasItem.Visible = true;
			}
			else
			{
				playerNode.ProcessMode = Node.ProcessModeEnum.Inherit;
			}

			var characterBody = FindCharacterBodyInPlayer(playerNode);
			if (characterBody != null)
			{
				characterBody.Visible = true;
				
				var collisionShape = characterBody.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
				if (collisionShape != null)
				{
					// ✅ 关键修复：使用 Timer 延迟启用碰撞体
					// 原因：
					// 1. TrapStatic 在 HidePlayer() 中使用 CallDeferred 禁用碰撞体（帧N+1）
					// 2. 如果我们立即启用（帧N），会在帧N+1被覆盖
					// 3. 如果我们也在帧N+1启用，可能与物理查询冲突导致 "flushing queries" 错误
					// 4. 解决方案：使用 0 秒 Timer 延迟到帧末尾或下一帧执行
					
					var tree = playerNode.GetTree();
					if (tree != null)
					{
						tree.CreateTimer(0).Timeout += () => 
						{
							if (GodotObject.IsInstanceValid(collisionShape))
							{
								collisionShape.Disabled = false;
							}
						};
						_log.Debug("[PlayerResetHandler] ✓ 碰撞体启用已安排（Timer延迟）");
					}
					else
					{
						// 如果无法获取场景树，则立即启用（回退方案）
						collisionShape.Disabled = false;
						_log.Warn("[PlayerResetHandler] ⚠️ 无法获取场景树，立即启用碰撞体");
					}
				}
			}

			_log.Info("[PlayerResetHandler] ✓ 可见性已恢复");
		}
		catch (Exception ex)
		{
			_log.Error($"[PlayerResetHandler] ❌ 恢复可见性异常: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public void ResetPhysicsStateForPlayer(Node playerNode)
	{
		try
		{
			var characterBody = FindCharacterBodyInPlayer(playerNode);
			if (characterBody != null)
			{
				ResetPlayerPhysicsState(characterBody);
				
				// 额外：调用 PlayerMovementController 的完整陷阱恢复
				var movementCtrl = characterBody.GetNodeOrNull<PlayerMovementController>("PlayerMovementController") ??
								   characterBody.GetNodeOrNull<PlayerMovementController>("CharacterBody2D");
				if (movementCtrl != null)
				{
					movementCtrl.ResetFromTrap();
					_log.Info("[PlayerResetHandler] ✓ PlayerMovementController 从陷阱中恢复");
				}
				else
				{
					_log.Debug("[PlayerResetHandler] 未找到 PlayerMovementController");
				}
				
				_log.Info("[PlayerResetHandler] ✓ 物理状态已重置");
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[PlayerResetHandler] ❌ 重置物理状态异常: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public void ResetPlayerPhysicsState(CharacterBody2D characterBody)
	{
		try
		{
			var movementCtrl = characterBody.GetNodeOrNull<PlayerMovementController>("PlayerMovementController") ??
							   characterBody.GetNodeOrNull<PlayerMovementController>("CharacterBody2D");

			if (movementCtrl != null)
			{
				var physicsMovement = movementCtrl.PhysicsMovement;
				if (physicsMovement != null)
				{
					physicsMovement.StopImmediately();
					_log.Debug("[PlayerResetHandler]   - PlayerPhysicsMovement 已重置");
				}
			}

			characterBody.Velocity = Vector2.Zero;
			_log.Debug("[PlayerResetHandler]   - CharacterBody2D.Velocity 已清零");
		}
		catch (Exception ex)
		{
			_log.Error($"[PlayerResetHandler] ❌ 重置物理状态异常: {ex.Message}");
			characterBody.Velocity = Vector2.Zero;
		}
	}

	#endregion

	#region IPlayerResetHandler 实现 - 事件订阅管理

	/// <inheritdoc />
	public void SubscribeToGlobalTrapEvents()
	{
		try
		{
			_log.Info("[PlayerResetHandler] 🔄 正在订阅全局陷阱事件...");

			if (_trapEventCallback != null)
			{
				_log.Warn("[PlayerResetHandler] 已订阅，跳过重复订阅");
				return;
			}

			_trapEventCallback = OnGlobalPlayerResetRequired;

			bool success = TrapEventManager.Subscribe(_trapEventCallback);

			if (success)
			{
				_log.Info($"[PlayerResetHandler] ✅ 订阅成功！当前订阅者: {TrapEventManager.SubscriberCount}");
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[PlayerResetHandler] ❌ 订阅异常: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public void UnsubscribeFromGlobalTrapEvents()
	{
		try
		{
			if (_trapEventCallback == null) return;

			TrapEventManager.Unsubscribe(_trapEventCallback);
			_trapEventCallback = null;
			_log.Info("[PlayerResetHandler] ✓ 已取消订阅");
		}
		catch (Exception ex)
		{
			_log.Error($"[PlayerResetHandler] ❌ 取消订阅异常: {ex.Message}");
			_trapEventCallback = null;
		}
	}

	#endregion

	#region 私有方法 - 回调处理

	/// <summary>
	///     全局陷阱事件的回调入口
	/// </summary>
	private void OnGlobalPlayerResetRequired(Node playerNode)
	{
		try
		{
			if (_data.IsGameCompleted) return;
			if (!GodotObject.IsInstanceValid(playerNode)) return;

			ExecuteFullPlayerReset(playerNode);
		}
		catch (Exception ex)
		{
			_log.Error($"[PlayerResetHandler] ❌ 全局回调异常: {ex.Message}");
		}
	}

	#endregion

	#region 私有辅助方法

	/// <summary>
	///     安全地将 Node 转换为 Node2D
	/// </summary>
	private static Node2D? ResolveToNode2D(Node playerNode)
	{
		if (playerNode == null) return null;

		if (playerNode is Node2D node2D) return node2D;

		var characterBody = FindCharacterBodyInPlayer(playerNode);
		if (characterBody != null) return characterBody;

		var parent = playerNode.GetParent();
		return parent as Node2D;
	}

	/// <summary>
	///     在玩家节点中查找 CharacterBody2D
	/// </summary>
	private static CharacterBody2D? FindCharacterBodyInPlayer(Node node)
	{
		if (node is CharacterBody2D body) return body;

		return node.GetNodeOrNull<CharacterBody2D>("CharacterBody2D") ?? 
			   node.GetNodeOrNull<CharacterBody2D>("character_body_2d");
	}

	#endregion
}
