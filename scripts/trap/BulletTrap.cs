using Godot;

namespace GFrameworkGodotTemplate.scripts.trap;

/// <summary>
///     子弹陷阱控制器
///     <para>
///         继承自 TrapStatic 的子弹型陷阱，在保留全部基础陷阱功能（隐藏玩家、触发重置事件）的基础上，
///         增加碰撞后自我销毁的行为：当子弹与玩家发生碰撞时，立即清除子弹自身。
///     </para>
///
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-16</date>
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
///         3. 在 Inspector 中调整销毁相关参数
///         4. 运行后子弹与玩家碰撞时：隐藏玩家 + 触发重置 + 销毁子弹
///
///         执行流程（碰撞发生时）:
///         1. Base: OnBodyEntered() → HidePlayer() + TriggerPlayerReset()
///         2. Derived: OnPlayerHit() → ClearBullet()
///            └── 根据 DestroyMode 执行 QueueFree 或 Visible=false
///     </description>
///     <remarks>
///         设计原则:
///         - 继承复用(DRY): 通过继承 TrapStatic 避免重复实现陷阱核心逻辑
///         - 开闭原则(OCP): 通过虚方法扩展而非修改基类代码
///         - 里氏替换(LSP): BulletTrap 可在任何使用 TrapStatic 的场景中替换使用
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
			_log.Info($"[BulletTrap] 销毁模式: {DestroyMode} | 延迟: {DestroyDelay}s");
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
	///     子弹碰撞玩家时的扩展处理
	///     <param name="body">进入碰撞区域的物理实体</param>
	///     <remarks>
	///         此方法作为基类 OnBodyEntered 的补充处理器：
	///         - 基类已处理: 隐藏玩家、触发全局重置事件
	 ///         - 本方法处理: 销毁/隐藏子弹自身
	///         
	///         执行顺序由信号连接顺序决定（先基类后派生类）
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
			if (!CheckIsPlayerBody(body))
			{
				return;
			}

			_isCleared = true;

			_log.Info("════════════ 子弹命中玩家 ═══════════");
			_log.Info($"[BulletTrap] 🔫 子弹 {Name} 与玩家 {body.Name} 发生碰撞");
			_log.Info($"[BulletTrap] 基础陷阱功能已由 TrapStatic 处理完成");
			_log.Info($"[BulletTrap] 现在执行子弹自我销毁 (模式: {DestroyMode})");

			ExecuteBulletClear();
		}
		catch (Exception ex)
		{
			_log.Error($"[BulletTrap] ❌ 子弹碰撞处理异常: {ex.Message}");
		}
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
	///     隐藏子弹的所有视觉元素
	///     <para>
	///         同时禁用碰撞检测防止持续触发
	///     </para>
	/// </summary>
	private void HideBullet()
	{
		_log.Info("[BulletTrap] 👁️ 隐藏子弹 (HideOnly 模式)");

		foreach (var child in GetChildren())
		{
			if (!GodotObject.IsInstanceValid(child))
			{
				continue;
			}

			if (child is CanvasItem canvasItem)
			{
				canvasItem.Visible = false;
			}
			else if (child is CollisionShape2D collisionShape)
			{
				collisionShape.Disabled = true;
			}
			else if (child is Area2D area)
			{
				area.Monitoring = false;
				var areaCollision = area.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
				if (areaCollision != null)
				{
					areaCollision.Disabled = true;
				}
			}
		}

		Visible = false;

		var ownArea = GetNodeOrNull<Area2D>("Area2D");
		if (ownArea != null && GodotObject.IsInstanceValid(ownArea))
		{
			ownArea.Monitoring = false;
		}

		_log.Debug("[BulletTrap] ✓ 子弹已完全隐藏且碰撞已禁用");
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
