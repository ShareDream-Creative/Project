using Godot;

namespace GFrameworkGodotTemplate.scripts.trap;

/// <summary>
///     子弹陷阱控制器
///     <para>
///         继承自 TrapStatic 的子弹型陷阱，在保留全部基础陷阱功能（隐藏玩家、触发重置事件）的基础上，
///         增加碰撞后自我销毁的行为：当子弹与玩家发生碰撞时，立即清除子弹自身。
///         
///         ✨ 新增功能（v2.0）：
///         撞墙消失 - 子弹碰到任何物理实体（StaticBody, RigidBody, CharacterBody 等）都会消失，
///         模拟真实的子弹撞墙效果。
///     </para>
///
///     <author>AI Assistant</author>
///     <version>2.0.0</version>
///     <date>2026-05-17</date>
///     <description>
///         继承关系:
///         BulletTrap → TrapStatic (继承所有基础陷阱功能)
///
///         基类提供的能力（无需重复实现）:
///         ✅ Area2D 碰撞检测玩家进入
///         ✅ 隐藏玩家节点 (Visible = false / ProcessMode = Disabled)
///         ✅ 调用全局静态事件 TrapEventManager.TriggerPlayerReset()
///         ✅ 自动重置状态支持 (AutoReset + ResetDelaySeconds)
///         ✅ 完整的日志记录和错误处理
///         ✅ 玩家节点识别 (IsPlayerBody)
///
///         本类新增能力:
///         🔫 子弹碰撞后自我销毁（QueueFree 或隐藏）
///         🔫 可配置的销毁模式（立即移除 vs 仅隐藏）
///         🔫 销毁前的延迟选项（用于视觉反馈）
///         🧱 **撞墙消失功能** - 碰到任何物理实体都会消失
///         🧱 独立的撞墙销毁模式和延迟配置
///
///         场景节点结构要求 (bullet.tscn):
///         Bullet (Node2D) ← 挂载本脚本
///         ├── Area2D           ← 碰撞检测区域（必需）
///         │   └── CollisionShape2D
///         └── ColorRect        ← 视觉表现
///
///         使用方式:
///         1. 将此脚本挂载到 bullet.tscn 的根节点(Bullet, Node2D类型)
///         2. 确保 Area2D 子节点的 CollisionShape2D 正确配置
///         3. 在 Inspector 中调整销毁相关参数：
///            - DestroyMode: 玩家碰撞后的销毁模式
///            - EnableWallHitDestruction: 是否启用撞墙消失（默认 true）
///            - WallHitDestroyMode: 撞墙时的销毁模式（默认 HideOnly）
///            - WallHitDestroyDelay: 撞墙延迟时间（秒）
///         4. 运行后子弹碰撞行为：
///            - 玩家 → 隐藏玩家 + 触发重置 + 销毁子弹
///            - 墙壁/障碍物 → 仅销毁/隐藏子弹（模拟撞墙）
///            - Area2D → 忽略（不响应）
///
///         执行流程（碰撞发生时）:
///         1. Base: OnBodyEntered() → HidePlayer() + TriggerPlayerReset() [仅对玩家]
///         2. Derived: OnBulletHitPlayer() → 分支处理:
///            a) 玩家 → HandlePlayerHit() → ClearBullet()
///            b) 墙壁 → HandleWallHit() → ClearBullet(撞墙模式)
///            
///         撞墙检测的物体类型:
///         ✅ StaticBody2D - 静态墙壁、平台、地面
///         ✅ RigidBody2D - 动态障碍物、可移动方块
///         ✅ CharacterBody2D - 敌人、NPC、其他角色
///         ✅ AnimatableBody2D - 动画实体
///         ❌ Area2D - 触发区域、传感器（忽略）
///     </description>
///     <remarks>
///         设计原则:
///         - 继承复用(DRY): 通过继承 TrapStatic 避免重复实现陷阱核心逻辑
///         - 开闭原则(OCP): 通过虚方法扩展而非修改基类代码
///         - 里氏替换(LSP): BulletTrap 可在任何使用 TrapStatic 的场景中替换使用
///         - 单一职责(SRP): 玩家碰撞和撞墙碰撞分离处理
///
///         信号处理策略:
///         由于基类的 OnBodyEntered 为 private 方法无法直接覆写，
///         本类通过在 _Ready 中额外连接 BodyEnded 信号的方式，
///         实现与基类处理逻辑并行执行的扩展行为。
///         Godot 的信号机制保证多播委托按连接顺序依次执行。
///
///         与直接使用 TrapStatic 的区别:
///         | 特性          | TrapStatic       | BulletTrap              |
///         |---------------|------------------|------------------------|
///         | 碰撞后行为     | 仅隐藏+触发重置   | 隐藏+触发重置+销毁自身    |
///         | 对象生命周期   | 持续存在可重复触发 | 碰撞后销毁或隐藏         |
///         | 适用场景      | 固定陷阱区域      | 发射物/投射物陷阱        |
///         | 撞墙反应      | 无               | ✅ 可配置的撞墙消失       |
///         
///         配置示例:
///         // 场景1：标准子弹陷阱（立即消失）
///         DestroyMode = QueueFree
///         EnableWallHitDestruction = true
///         WallHitDestroyMode = QueueFree
///         
///         // 场景2：带特效的子弹（撞墙显示特效）
///         DestroyMode = DelayedQueueFree, DestroyDelay = 0.3
///         EnableWallHitDestruction = true
///         WallHitDestroyMode = DelayedHide, WallHitDestroyDelay = 0.5
///         
///         // 场景3：仅对玩家有效（穿透墙壁）
///         EnableWallHitDestruction = false
///     </remarks>
/// </summary>
[Log]
public partial class BulletTrap : TrapStatic
{
	#region 导出参数

	/// <summary>
	///     子弹销毁模式
	///     <para>
	///         控制碰撞玩家后的子弹处理方式
	///     </para>
	/// </summary>
	[Export]
	public BulletDestroyMode DestroyMode { get; set; } = BulletDestroyMode.QueueFree;

	/// <summary>
	///     销毁延迟时间（秒）
	///     <para>
	///         仅当 DestroyMode 为 DelayedHide 或 DelayedQueueFree 时生效。
	///         用于在销毁前提供短暂的视觉反馈（如命中特效播放时间）
	///     </para>
	/// </summary>
	[Export]
	public float DestroyDelay { get; set; } = 0f;

	/// <summary>
	///     是否启用撞墙消失功能
	///     <para>
	///         当启用时，子弹碰到任何非 Area2D 的物理实体（StaticBody, RigidBody, CharacterBody 等）
	///         都会触发销毁/隐藏操作，模拟真实的子弹撞墙效果
	///         
	///         默认值: true（推荐开启）
	///         关闭后：子弹只会对玩家产生反应，穿过其他物体
	///     </para>
	/// </summary>
	[Export]
	public bool EnableWallHitDestruction { get; set; } = true;

	/// <summary>
	///     撞墙时的销毁模式（独立于玩家碰撞的销毁模式）
	///     <para>
	///         控制子弹碰到墙壁/障碍物时的处理方式
	///         可以与 DestroyMode 不同，实现差异化行为：
	///         - 玩家：QueueFree（立即消失）
	///         - 墙壁：DelayedHide（延迟消失，显示撞墙特效）
	 ///     </para>
	/// </summary>
	[Export]
	public BulletDestroyMode WallHitDestroyMode { get; set; } = BulletDestroyMode.HideOnly;

	/// <summary>
	///     撞墙销毁延迟时间（秒）
	///     <para>
	///         仅当 WallHitDestroyMode 为 DelayedHide 或 DelayedQueueFree 时生效
	///         用于播放撞墙特效、粒子效果等视觉反馈
	 ///     </para>
	/// </summary>
	[Export]
	public float WallHitDestroyDelay { get; set; } = 0f;

	#endregion

	#region 枚举定义

	/// <summary>
	///     子弹销毁模式枚举
	/// </summary>
	public enum BulletDestroyMode
	{
		/// <summary>立即从场景树中移除并释放内存</summary>
		QueueFree,

		/// <summary>仅隐藏视觉元素，保留节点在场景中</summary>
		HideOnly,

		/// <summary>延迟后从场景树中移除</summary>
		DelayedQueueFree,

		/// <summary>延迟后隐藏视觉元素</summary>
		DelayedHide
	}

	#endregion

	#region 私有字段

	/// <summary>是否已执行过销毁操作（防止重复销毁）</summary>
	private bool _isCleared;

	#endregion

	#region 生命周期方法

	/// <summary>
	///     节点准备就绪时的回调
	///     <para>
	///         调用基类初始化完成基础陷阱功能后，
	///         额外连接 BodyEntered 信号以实现子弹自我销毁逻辑
	///     </para>
	/// </summary>
	public override void _Ready()
	{
		base._Ready();

		try
		{
			SetupBulletCollisionHandler();
			_isCleared = false;

			_log.Info("[BulletTrap] ✅ 子弹陷阱控制器初始化完成");
			_log.Info($"[BulletTrap] 玩家销毁模式: {DestroyMode} | 延迟: {DestroyDelay}s");
			_log.Info($"[BulletTrap] 撞墙消失: {(EnableWallHitDestruction ? "✅ 启用" : "❌ 禁用")}");
			if (EnableWallHitDestruction)
			{
				_log.Info($"[BulletTrap]   → 撞墙模式: {WallHitDestroyMode} | 延迟: {WallHitDestroyDelay}s");
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[BulletTrap] ❌ 初始化异常: {ex.Message}");
		}
	}

	#endregion

	#region 核心方法

	/// <summary>
	///     设置子弹碰撞处理器
	///     <para>
	///         在基类已连接的 BodyEntered 信号上附加本类的处理函数。
	///         Godot 信号的多播委托机制确保：
	///         基类 OnBodyEntered 先执行（隐藏玩家+触发重置），
	///         本类 OnBulletHitPlayer 后执行（销毁子弹自身）。
	///     </para>
	/// </summary>
	private void SetupBulletCollisionHandler()
	{
		var trapArea = GetNodeOrNull<Area2D>("Area2D");

		if (trapArea == null)
		{
			_log.Error("[BulletTrap] ❌ 未找到 Area2D 子节点！");
			return;
		}

		trapArea.BodyEntered += OnBulletHitPlayer;

		_log.Debug("[BulletTrap] ✓ 子弹碰撞处理器已连接到 BodyEntered 信号");
	}

	/// <summary>
	///     子弹碰撞处理（统一入口）
	///     <param name="body">进入碰撞区域的物理实体</param>
	///     <remarks>
	///         此方法作为基类 OnBodyEntered 的补充处理器：
	///         - 基类已处理: 隐藏玩家、触发全局重置事件（仅对玩家）
	 ///         - 本方法处理: 
	 ///           1. 玩家碰撞 → 销毁子弹自身
	 ///           2. 非玩家碰撞 → 撞墙消失（如果启用）
	 ///           
	 ///         执行顺序由信号连接顺序决定（先基类后派生类）
	 ///         
	 ///         撞墙检测逻辑:
	 ///         - Area2D: 忽略（通常是其他陷阱或区域）
	 ///         - StaticBody/RigidBody/CharacterBody: 触发撞墙消失
	 ///         - 玩家: 触发完整陷阱逻辑 + 子弹销毁
	 ///     </remarks>
	/// </summary>
	private void OnBulletHitPlayer(Node body)
	{
		if (_isCleared)
		{
			return;
		}

		try
		{
			// 区分玩家和非玩家碰撞
			bool isPlayer = CheckIsPlayerBody(body);
			
			if (isPlayer)
			{
				// ✅ 碰到玩家：执行完整的子弹命中逻辑
				HandlePlayerHit(body);
			}
			else if (EnableWallHitDestruction && IsPhysicalBody(body))
			{
				// 🧱 碰到墙壁：执行撞墙消失逻辑
				HandleWallHit(body);
			}
			// 其他情况（Area2D等）：忽略
		}
		catch (Exception ex)
		{
			_log.Error($"[BulletTrap] ❌ 子弹碰撞处理异常: {ex.Message}");
		}
	}

	/// <summary>
	///     处理玩家碰撞（原有逻辑）
	/// </summary>
	private void HandlePlayerHit(Node body)
	{
		_isCleared = true;

		_log.Info("════════════ 子弹命中玩家 ═══════════");
		_log.Info($"[BulletTrap] 🔫 子弹 {Name} 与玩家 {body.Name} 发生碰撞");
		_log.Info($"[BulletTrap] 基础陷阱功能已由 TrapStatic 处理完成");
		_log.Info($"[BulletTrap] 现在执行子弹自我销毁 (模式: {DestroyMode})");

		ExecuteBulletClear();
	}

	/// <summary>
	///     处理墙壁/障碍物碰撞（新增功能）
	///     <para>
	///         当子弹碰到 StaticBody, RigidBody, CharacterBody 等物理实体时调用
	 ///         模拟真实的子弹撞墙效果
	 ///     </para>
	/// </summary>
	private void HandleWallHit(Node wallBody)
	{
		_isCleared = true;

		_log.Info("════════════ 子弹撞墙 ═══════════");
		_log.Info($"[BulletTrap] 🧱 子弹 {Name} 撞到障碍物 {wallBody.Name}");
		_log.Info($"[BulletTrap] 障碍物类型: {wallBody.GetType().Name}");
		_log.Info($"[BulletTrap] 执行撞墙消失 (模式: {WallHitDestroyMode})");

		// 使用独立的撞墙销毁模式
		switch (WallHitDestroyMode)
		{
			case BulletDestroyMode.QueueFree:
				ImmediateQueueFree();
				break;

			case BulletDestroyMode.HideOnly:
				HideBullet();
				break;

			case BulletDestroyMode.DelayedQueueFree:
				ScheduleDelayedQueueForWall();
				break;

			case BulletDestroyMode.DelayedHide:
				ScheduleDelayedHideForWall();
				break;
		}

		_log.Info("[BulletTrap] ✓ 撞墙处理完成");
	}

	/// <summary>
	///     检测是否为物理实体（非 Area2D）
	///     <para>
	///         用于区分需要响应碰撞的物理实体和 Area2D 区域
	 ///         物理实体包括: StaticBody2D, RigidBody2D, CharacterBody2D 等
	 ///     </para>
	/// </summary>
	private static bool IsPhysicalBody(Node body)
	{
		if (body == null) return false;
		
		// 检查是否为 Godot 的物理实体类型
		return body is StaticBody2D || 
			   body is RigidBody2D || 
			   body is CharacterBody2D ||
			   body is AnimatableBody2D;
	}

	/// <summary>
	///     安排延迟后从场景树中移除子弹（撞墙专用）
	/// </summary>
	private void ScheduleDelayedQueueForWall()
	{
		float delay = Mathf.Max(WallHitDestroyDelay, 0f);

		_log.Info($"[BulletTrap] ⏳ {delay}秒后将销毁子弹 (撞墙-DelayedQueueFree)");

		var timer = GetTree().CreateTimer(delay);
		timer.Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(this))
			{
				_log.Info("[BulletTrap] 💥 撞墙延迟时间到，销毁子弹");
				QueueFree();
			}
		};
	}

	/// <summary>
	///     安排延迟后隐藏子弹（撞墙专用）
	/// </summary>
	private void ScheduleDelayedHideForWall()
	{
		float delay = Mathf.Max(WallHitDestroyDelay, 0f);

		_log.Info($"[BulletTrap] ⏳ {delay}秒后将隐藏子弹 (撞墙-DelayedHide)");

		var timer = GetTree().CreateTimer(delay);
		timer.Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(this))
			{
				_log.Info("[BulletTrap] 👁️ 撞墙延迟时间到，隐藏子弹");
				HideBullet();
			}
		};
	}

	/// <summary>
	///     根据当前销毁模式执行子弹清理操作
	/// </summary>
	private void ExecuteBulletClear()
	{
		switch (DestroyMode)
		{
			case BulletDestroyMode.QueueFree:
				ImmediateQueueFree();
				break;

			case BulletDestroyMode.HideOnly:
				HideBullet();
				break;

			case BulletDestroyMode.DelayedQueueFree:
				ScheduleDelayedQueueFree();
				break;

			case BulletDestroyMode.DelayedHide:
				ScheduleDelayedHide();
				break;
		}
	}

	#endregion

	#region 销毁操作

	/// <summary>
	///     立即从场景树中移除子弹并释放资源
	/// </summary>
	private void ImmediateQueueFree()
	{
		_log.Info("[BulletTrap] 💥 立即销毁子弹 (QueueFree)");

		if (GodotObject.IsInstanceValid(this))
		{
			QueueFree();
		}
	}

	/// <summary>
	///     隐藏子弹的所有视觉元素并**完全禁用碰撞检测**
	///     <para>
	///         关键改进（v2.1）：
	///         不仅隐藏视觉元素，还必须彻底禁用所有碰撞相关的组件，
	///         防止"隐形但仍然可以攻击玩家"的问题
	 ///         
	 ///         禁用层级（从外到内）:
	 ///         1. Area2D.Monitoring = false → 停止监控区域内的物体（使用 CallDeferred）
	 ///         2. CollisionShape2D.Disabled = true → 禁用碰撞形状
	 ///         3. CanvasItem.Visible = false → 隐藏视觉表现
	 ///         
	 ///         调用时机:
	 ///         - HideOnly 模式（立即或延迟）
	 ///         - 撞墙消失时（如果 WallHitDestroyMode = HideOnly）
	 ///         
	 ///         v2.1.1 修复:
	 ///         使用 CallDeferred 修改 Monitoring 属性，避免 "flushing queries" 错误
	 ///     </para>
	/// </summary>
	private void HideBullet()
	{
		_log.Info("[BulletTrap] 👁️ 隐藏子弹 (HideOnly 模式) - 完全禁用碰撞");

		// 步骤1: 禁用根级别的 Area2D 监控（使用 CallDeferred 避免 flushing queries 错误）
		var ownArea = GetNodeOrNull<Area2D>("Area2D");
		if (ownArea != null && GodotObject.IsInstanceValid(ownArea))
		{
			// 使用 CallDeferred 延迟修改 Monitoring，避免在信号回调中直接修改状态
			CallDeferred(MethodName.SetDeferred, "monitoring", false);
			_log.Debug("[BulletTrap]   ✓ 根级别 Area2D.Monitoring 已安排延迟禁用");
			
			// 禁用根级别 Area2D 内的碰撞形状
			var rootCollision = ownArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (rootCollision != null)
			{
				rootCollision.Disabled = true;
				_log.Debug("[BulletTrap]   ✓ 根级别 CollisionShape2D.Disabled = true");
			}
		}

		// 步骤2: 遍历所有子节点，逐个禁用
		foreach (var child in GetChildren())
		{
			if (!GodotObject.IsInstanceValid(child))
			{
				continue;
			}

			if (child is CanvasItem canvasItem)
			{
				canvasItem.Visible = false; // 隐藏视觉
			}
			else if (child is CollisionShape2D collisionShape)
			{
				collisionShape.Disabled = true; // 禁用碰撞形状（双重保障）
			}
			else if (child is Area2D area)
			{
				// 使用 CallDeferred 延迟修改 Monitoring
				area.CallDeferred("set_monitoring", false);
				
				// 禁用嵌套的碰撞形状
				foreach (var areaChild in area.GetChildren())
				{
					if (areaChild is CollisionShape2D nestedCollision && 
						GodotObject.IsInstanceValid(nestedCollision))
					{
						nestedCollision.Disabled = true;
					}
				}
			}
		}

		// 步骤3: 隐藏自身
		Visible = false;

		// 步骤4: 最终确认 - 强制再次检查所有碰撞组件
		ForceDisableAllCollisions();

		_log.Info("[BulletTrap] ✓✓✓ 子弹已完全隐藏且所有碰撞检测已禁用！");
	}

	/// <summary>
	///     强制禁用所有碰撞相关组件（最终保障）
	///     <para>
	 ///         作为最后的安全网，递归查找并禁用场景树中的所有：
	 ///         - Area2D 节点（使用 CallDeferred 设置 Monitoring = false）
	 ///         - CollisionShape2D 节点（设置 Disabled = true）
	 ///         
	 ///         这确保即使有遗漏的节点，也会被捕获并禁用
	 ///         
	 ///         v2.1.1: 使用 CallDeferred 避免 "flushing queries" 错误
	 ///     </para>
	/// </summary>
	private void ForceDisableAllCollisions()
	{
		try
		{
			// 查找所有 Area2D 并禁用（使用 CallDeferred）
			var allAreas = FindChildren("*", "Area2D", true, false);
			foreach (var area in allAreas)
			{
				if (area is Area2D areaNode && GodotObject.IsInstanceValid(areaNode))
				{
					areaNode.CallDeferred("set_monitoring", false);
					_log.Debug($"[BulletTrap]   ✓ 强制禁用 Area2D: {areaNode.Name} (CallDeferred)");
				}
			}

			// 查找所有 CollisionShape2D 并禁用
			var allShapes = FindChildren("*", "CollisionShape2D", true, false);
			foreach (var shape in allShapes)
			{
				if (shape is CollisionShape2D shapeNode && GodotObject.IsInstanceValid(shapeNode))
				{
					shapeNode.Disabled = true;
					_log.Debug($"[BulletTrap]   ✓ 强制禁用 CollisionShape2D: {shapeNode.Name}");
				}
			}

			_log.Debug("[BulletTrap] ✓ 强制碰撞禁用完成（安全网）");
		}
		catch (Exception ex)
		{
			_log.Warn($"[BulletTrap] ⚠️ 强制碰撞禁用异常（可忽略）: {ex.Message}");
		}
	}

	/// <summary>
	///     安排延迟后从场景树中移除子弹
	/// </summary>
	private void ScheduleDelayedQueueFree()
	{
		float delay = Mathf.Max(DestroyDelay, 0f);

		_log.Info($"[BulletTrap] ⏳ {delay}秒后将销毁子弹 (DelayedQueueFree)");

		var timer = GetTree().CreateTimer(delay);
		timer.Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(this))
			{
				_log.Info("[BulletTrap] 💥 延迟时间到，销毁子弹");
				QueueFree();
			}
		};
	}

	/// <summary>
	///     安排延迟后隐藏子弹
	/// </summary>
	private void ScheduleDelayedHide()
	{
		float delay = Mathf.Max(DestroyDelay, 0f);

		_log.Info($"[BulletTrap] ⏳ {delay}秒后将隐藏子弹 (DelayedHide)");

		var timer = GetTree().CreateTimer(delay);
		timer.Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(this))
			{
				_log.Info("[BulletTrap] 👁️ 延迟时间到，隐藏子弹");
				HideBullet();
			}
		};
	}

	/// <summary>
	///     检测传入节点是否为玩家角色
	///     <param name="body">待检测的节点</param>
	/// </summary>
	private static bool CheckIsPlayerBody(Node body)
	{
		if (body == null)
		{
			return false;
		}

		string name = body.Name.ToString();
		if (name.Equals("Player", System.StringComparison.OrdinalIgnoreCase) ||
			name.Equals("player", System.StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		var parent = body.GetParent();
		if (parent != null)
		{
			string parentName = parent.Name.ToString();
			if (parentName.Equals("Player", System.StringComparison.OrdinalIgnoreCase) ||
				parentName.Equals("player", System.StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	#endregion
}
