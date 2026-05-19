using Godot;
using System.Collections.Generic;
using GFrameworkGodotTemplate.scripts.events.poker;

namespace GFrameworkGodotTemplate.scripts.player;

/// <summary>
///     玩家动作能力控制器 v1.0 - 二段跳系统核心
///     <para>
///         功能:
///         1. 接收 ActionPokerData（分类后的动作卡牌数据）
///         2. 管理每种动作的可用次数（如二段跳次数）
///         3. 实现完整的跳跃状态机（跳起→空中→落地）
///         4. 防止多次连跳，确保完整跳跃周期
 ///        
 ///        核心机制 - 二段跳规则:
 ///        - 每张 DoubleJump 卡牌 = 1次二段跳机会
 ///        - 完整跳跃周期: 跳起 → 空中 → 落地
 ///        - 落地前不能用第2次二段跳
 ///        - 用完所有卡牌 → 只能普通跳跃
 ///        
 ///        跳跃状态机:
 ///        Grounded (地面) → Jumping (跳起) → Airborne (空中) → Landing (落地)
 ///        ↑_______________________________________________________________|
 ///     </para>
/// </summary>
[Log]
public partial class PlayerActionController : Node
{
	#region 枚举定义

	/// <summary>
	///     跳跃状态枚举
	/// </summary>
	public enum JumpState
	{
		Grounded,    // 地面：可以开始跳跃
		Jumping,     // 跳起：刚离开地面
		Airborne,    // 空中：在空中飞行
		Landing      // 落地：即将接触地面
	}

	#endregion

	#region 私有字段

	/// <summary>当前接收到的动作卡牌数据</summary>
	private ActionPokerData? _actionData;

	/// <summary>
	///     v5.1新增：TeachLevel 锁定标志
	///     <para>
	 ///        当 TeachLevel 传入数据后设置为 true，
	 ///        阻止其他组件覆盖教程数据
	 ///     </para>
	/// </summary>
	private bool _isLockedByTeachLevel;

	/// <summary>当前跳跃状态</summary>
	private JumpState _currentJumpState = JumpState.Grounded;

	/// <summary>当前跳跃周期内是否已使用过二段跳</summary>
	private bool _hasUsedDoubleJumpInCurrentJump;

	/// <summary>是否正在地面</summary>
	private bool _isGrounded = true;

	#endregion

	#region 公开属性

	/// <summary>
	///     二段跳剩余次数（只读）
	/// </summary>
	public int DoubleJumpRemaining => _actionData?.GetActionCount("DoubleJump") ?? 0;

	/// <summary>
	///     Rush 剩余次数（只读）
	/// </summary>
	public int RushRemaining => _actionData?.GetActionCount("Rush") ?? 0;

	/// <summary>
	///     当前跳跃状态（只读）
	/// </summary>
	public JumpState CurrentJumpState => _currentJumpState;

	/// <summary>
	///     是否可以使用二段跳（只读）
	/// </summary>
	public bool CanUseDoubleJump =>
		DoubleJumpRemaining > 0 &&
		_currentJumpState == JumpState.Airborne &&
		!_hasUsedDoubleJumpInCurrentJump;

	/// <summary>
	///     是否可以普通跳跃（只读）
	/// </summary>
	public bool CanNormalJump => _currentJumpState == JumpState.Grounded && _isGrounded;

	#endregion

	#region 生命周期方法

	public override void _Ready()
	{
		// 订阅动作卡牌就绪事件
		ClassifiedPokerEvents.ActionDataReady += OnActionDataReady;
		
		GD.Print("[PlayerActionController] ✓ v1.1 二段跳系统初始化完成");
		GD.Print("[PlayerActionController] 等待 ActionPokerData...");
		
		// v4.1新增：检查全局缓存（解决时序断裂问题）
		CheckAndApplyCachedData();
	}

	public override void _ExitTree()
	{
		// 取消订阅事件
		ClassifiedPokerEvents.ActionDataReady -= OnActionDataReady;
		
		GD.Print("[PlayerActionController] 已清理事件订阅");
	}

	#endregion

	#region 缓存恢复机制 (v4.1)

	/// <summary>
	///     v4.1新增：检查并应用缓存的动作数据
	///     <para>
	 ///        解决时序断裂问题：当 ActionDataReady 事件在本控制器加载前就已触发时，
	 ///        通过读取 LastActionData 缓存来恢复数据。
	 ///        
	 ///        与 PokerHand.ReceiveItemData() 使用相同的模式。
	 ///        
	 ///        v4.2优化: 明确标记此数据来自缓存恢复（非实时事件）
	 ///     </para>
	/// </summary>
	private void CheckAndApplyCachedData()
	{
		if (ClassifiedPokerEvents.LastActionData != null && ClassifiedPokerEvents.LastActionData.HasActions)
		{
			GD.Print("[PlayerActionController] 🔍 发现已有的动作数据缓存，立即应用（来源: 全局缓存）...");
			ReceiveActionData(ClassifiedPokerEvents.LastActionData, isFromRealtimeEvent: false);
			GD.Print("[PlayerActionController] ℹ️ 如果稍后收到实时事件，将使用实时数据覆盖此缓存数据");
		}
		else
		{
			GD.Print("[PlayerActionController] ℹ 全局缓存为空，等待 LevelPrepareUi 发送数据...");
		}
	}

	#endregion

	#region 数据接收

	/// <summary>
	///     接收分类后的动作卡牌数据（事件处理器）
	 ///     <para>
	 ///        v4.2优化: 标记为实时事件数据（最高优先级）
	 ///     </para>
	/// </summary>
	private void OnActionDataReady(ActionPokerData? actionData)
	{
		if (actionData == null || !actionData.HasActions)
		{
			GD.Print("[PlayerActionController] ⚠ 收到空的 ActionPokerData (来自实时事件)");
			return;
		}
		
		GD.Print("[PlayerActionController] ✓ 接收到实时事件数据 (ActionDataReady)，优先处理...");
		ReceiveActionData(actionData, isFromRealtimeEvent: true);
	}

	/// <summary>
	///     v4.1新增：公开的数据接收接口
	///     <para>
	 ///        允许外部组件（如 LevelBuildUi）直接传递数据
	 ///        解决事件时序断裂问题
	 ///        
	 ///        调用方式:
	 ///        1. 事件自动触发（正常流程）→ isFromRealtimeEvent=true
	 ///        2. CheckAndApplyCachedData() 从缓存恢复 → isFromRealtimeEvent=false
	 ///        3. TeachLevel 直接调用 → isFromTeachLevel=true (强制模式)
	 ///        
	 ///        v4.2优化: 添加数据来源标识，实现优先级控制
	 ///        - 实时事件数据（true）：**始终接受**，最高优先级
	 ///        - 缓存恢复数据（false）：仅在无实时数据时使用
	 ///        
	 ///        v5.1优化: 添加 TeachLevel 锁定机制
	 ///        - 一旦被 TeachLevel 锁定，拒绝所有其他数据源
	 ///     </para>
	/// </summary>
	public void ReceiveActionData(ActionPokerData? actionData, bool isFromRealtimeEvent = false, bool isFromTeachLevel = false)
	{
		// v5.1锁定检查：如果已被 TeachLevel 锁定，只接受 TeachLevel 自己的数据
		if (_isLockedByTeachLevel && !isFromTeachLevel)
		{
			GD.Print("[PlayerActionController] 🔒 已被 TeachLevel 锁定，拒绝外部数据覆盖！");
			GD.Print($"[PlayerActionController]   尝试来源: {(isFromRealtimeEvent ? "实时事件" : "全局缓存恢复")}");
			return;
		}
		
		// 如果是 TeachLevel 传入的数据，设置锁定标志
		if (isFromTeachLevel)
		{
			_isLockedByTeachLevel = true;
			GD.Print("[PlayerActionController] 🔒 已激活 TeachLevel 锁定模式！");
		}
		
		_actionData = actionData;
		
		if (_actionData != null && _actionData.HasActions)
		{
			GD.Print("═════════════════════════");
			GD.Print($"[PlayerActionController] 📥 接收到动作卡牌数据 (Action): {_actionData.TotalCount} 张");
			
			if (isFromTeachLevel)
			{
				GD.Print("[PlayerActionController] 📥 数据来源: 🎓 TeachLevel (强制模式 + 锁定)");
			}
			else if (isFromRealtimeEvent)
			{
				GD.Print("[PlayerActionController] 📥 数据来源: ✅ 实时事件 (高优先级)");
			}
			else
			{
				GD.Print("[PlayerActionController] 📥 数据来源: 🔄 全局缓存恢复");
				GD.Print("[PlayerActionController] ℹ️ 注意: 此数据来自缓存，如果已被 TeachLevel 锁定，将不会生效");
			}
			
			var allActions = _actionData.GetAllActions();
			foreach (var kvp in allActions)
			{
				GD.Print($"   • {kvp.Key}: {kvp.Value} 次");
				
				if (kvp.Key.Equals("DoubleJump", System.StringComparison.OrdinalIgnoreCase))
				{
					GD.Print($"[PlayerActionController] ⚡ 二段跳可用次数: {kvp.Value}");
				}
			}
			
			GD.Print("═════════════════════════");
			
			// 重置跳跃状态
			ResetJumpState();
		}
		else
		{
			GD.Print("[PlayerActionController] ⚠ 收到空的动作卡牌数据或无可用动作");
			GD.Print("[PlayerActionController] 💡 玩家只能使用普通跳跃和移动");
		}
	}

	#endregion

	#region 跳跃状态机

	/// <summary>
	///     尝试执行跳跃（由 PlayerMovementController 调用）
	///     <para>
	 ///        返回值含义:
	 ///        - true: 成功执行了某种类型的跳跃
	 ///        - false: 无法跳跃（不在地面或没有可用次数）
	 ///        
	 ///        优先级:
	 ///        1. 如果在空中且可以用二段跳 → 执行二段跳
	 ///        2. 如果在地面 → 执行普通跳跃
	 ///        3. 否则 → 无法跳跃
	 ///     </para>
	/// </summary>
	/// <returns>是否成功跳跃</returns>
	public bool TryJump()
	{
		// 优先尝试二段跳（如果在空中）
		if (_currentJumpState == JumpState.Airborne && CanUseDoubleJump)
		{
			return ExecuteDoubleJump();
		}
		
		// 尝试普通跳跃（如果在地面）
		if (CanNormalJump)
		{
			return ExecuteNormalJump();
		}
		
		// 无法跳跃
		return false;
	}

	/// <summary>
	///     执行普通跳跃
	/// </summary>
	private bool ExecuteNormalJump()
	{
		if (!CanNormalJump) return false;
		
		GD.Print($"[PlayerActionController] 🦘 执行普通跳跃");
		
		// 更新状态
		_currentJumpState = JumpState.Jumping;
		_hasUsedDoubleJumpInCurrentJump = false;
		_isGrounded = false;
		
		return true;
	}

	/// <summary>
	///     执行二段跳
	/// </summary>
	private bool ExecuteDoubleJump()
	{
		if (!CanUseDoubleJump) return false;
		
		GD.Print($"[PlayerActionController] ⚡ 执行二段跳 (剩余:{DoubleJumpRemaining - 1})");
		
		// 消耗一个二段跳机会
		bool consumed = _actionData!.ConsumeAction("DoubleJump");
		
		if (consumed)
		{
			// 更新状态
			_hasUsedDoubleJumpInCurrentJump = true;
			_currentJumpState = JumpState.Jumping; // 重新进入跳起状态
			
			// 通知消耗事件
			ClassifiedPokerEvents.ActionConsumed?.Invoke("DoubleJump", DoubleJumpRemaining);
			
			GD.Print($"[PlayerActionController] ✓ 二段跳已消耗，剩余: {DoubleJumpRemaining}");
			
			return true;
		}
		
		GD.PrintErr("[PlayerActionController] ❌ 二段跳消耗失败");
		return false;
	}

	/// <summary>
	///     更新地面状态（由物理引擎调用）
	///     <para>
	 ///        应该在每一帧的物理更新中调用
	 ///        用于检测玩家是否接触地面
	 ///     </para>
	/// </summary>
	/// <param name="grounded">是否在地面上</param>
	public void UpdateGroundState(bool grounded)
	{
		bool wasGrounded = _isGrounded;
		_isGrounded = grounded;
		
		if (grounded && !wasGrounded)
		{
			// 从空中落到地面
			OnLanded();
		}
		else if (!grounded && wasGrounded)
		{
			// 从地面离开（可能是跳跃或走下平台）
			if (_currentJumpState == JumpState.Grounded)
			{
				_currentJumpState = JumpState.Airborne;
				GD.Print("[PlayerActionController] 📍 离开地面 → 进入空中状态");
			}
		}
	}

	/// <summary>
	///     落地处理
	/// </summary>
	private void OnLanded()
	{
		GD.Print("═════════════════════════");
		GD.Print("[PlayerActionController] 🛬 玩家已落地！");
		
		// 重置跳跃周期状态
		ResetJumpCycle();
		
		// 更新状态为地面
		_currentJumpState = JumpState.Grounded;
		
		GD.Print($"[PlayerActionController] 📊 当前状态:");
		GD.Print($"   • 跳跃状态: {_currentJumpState}");
		GD.Print($"   • 二段跳剩余: {DoubleJumpRemaining}");
		GD.Print($"   • 本轮已用二段跳: {_hasUsedDoubleJumpInCurrentJump}");
		GD.Print("═════════════════════════");
	}

	/// <summary>
	///     重置整个跳跃状态机（用于重新初始化）
	/// </summary>
	public void ResetJumpState()
	{
		_currentJumpState = JumpState.Grounded;
		_isGrounded = true;
		_hasUsedDoubleJumpInCurrentJump = false;
		
		GD.Print("[PlayerActionController] 🔄 跳跃状态已重置");
	}

	/// <summary>
	///     重置当前跳跃周期（不重置计数器）
	/// </summary>
	private void ResetJumpCycle()
	{
		_hasUsedDoubleJumpInCurrentJump = false;
	}

	#endregion

	#region 物理更新接口

	/// <summary>
	///     物理帧更新（应在 _PhysicsProcess 中调用）
	/// </summary>
	public void PhysicsUpdate(double delta)
	{
		// 更新跳跃状态的时间演化
		UpdateJumpStateTransition(delta);
	}

	/// <summary>
	///     跳跃状态转换逻辑
	/// </summary>
	private void UpdateJumpStateTransition(double delta)
	{
		switch (_currentJumpState)
		{
			case JumpState.Jumping:
				// 跳起后短暂延迟进入空中状态
				// 这里可以添加动画或特效触发
				break;
				
			case JumpState.Airborne:
				// 在空中持续检查
				break;
				
			case JumpState.Landing:
				// 落地过渡状态
				break;
		}
	}

	#endregion

	#region 公开API

	/// <summary>
	///     强制设置跳跃状态（用于调试或特殊场景）
	/// </summary>
	public void SetJumpState(JumpState state)
	{
		_currentJumpState = state;
		GD.Print($"[PlayerActionController] 🔧 手动设置跳跃状态: {state}");
	}

	/// <summary>
	///     获取详细的诊断信息
	/// </summary>
	public string GetDiagnosticInfo()
	{
		var info = new List<string>
		{
			"══ PlayerActionController 诊断 ══",
			$"跳跃状态: {_currentJumpState}",
			$"是否在地面: {_isGrounded}",
			$"本轮已用二段跳: {_hasUsedDoubleJumpInCurrentJump}",
			""
		};
		
		if (_actionData != null && _actionData.HasActions)
		{
			info.Add("可用动作:");
			var actions = _actionData.GetAllActions();
			foreach (var kvp in actions)
			{
				info.Add($"  • {kvp.Key}: {kvp.Value} 次");
			}
		}
		else
		{
			info.Add("无可用动作卡牌");
		}
		
		info.Add("═══════════════════════════");
		
		return string.Join("\n", info);
	}

	#endregion
}
