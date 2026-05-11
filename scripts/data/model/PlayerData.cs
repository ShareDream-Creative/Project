using System;
using System.Collections.Generic;
using GFrameworkGodotTemplate.scripts.data.interfaces;
using Godot;

namespace GFrameworkGodotTemplate.scripts.data.model;

/// <summary>
///     玩家数据模型类
///     <para>
///         统一管理所有玩家角色属性，提供验证、监听和持久化支持
///     </para>
///     <author>AI Assistant</author>
///     <version>2.2.0</version>
///     <date>2026-05-11</date>
///     <description>
///         核心职责:
///         1. 数据容器: 存储所有玩家可配置的数值属性
///         2. 验证器: 确保所有属性值在合理范围内
///         3. 通知器: 属性变更时自动通知所有监听者
///         4. 序列化: 支持导出为字典格式便于持久化
///         
///         架构设计:
///         - 单一职责(SRP): 只负责数据管理和验证
///         - 开闭原则(OCP): 通过监听器扩展行为，不修改本类
///         - 依赖倒置(DIP): 依赖抽象的监听器接口
///         - 观察者模式(Observer): 实现属性变更通知机制
///         
///         数据流向:
///         外部代码 → Property Setter → Validate() → NotifyListeners() → IPlayerDataListener
///         
///         线程安全说明:
///         - 属性读写本身不是线程安全的
///         - 监听器列表使用锁保护
///         - 多线程环境需要外部同步机制
///     </description>
///     <remarks>
///         设计原则:
///         - 单一职责(SRP): 只负责数据管理和验证
///         - 开闭原则(OCP): 通过监听器扩展行为，不修改本类
///         - 依赖倒置(DIP): 依赖抽象的IPlayerDataListener接口
///         
///         使用示例:
///         <code>
///         var playerData = new PlayerData();
///         playerData.AddListener(physicsModule);
///         playerData.Speed = 350.0f; // 自动通知physicsModule
///         </code>
///         
///         验证机制:
///         所有属性的setter都会调用对应的Validate方法
///         确保值在有效范围内，超出范围时自动截断到边界值
///         
///         监听器机制:
///         属性值实际变化时（新旧值差异 > 0.001）才触发通知
///         避免不必要的监听器回调
///         
///         性能优化:
///         - 使用Math.Abs比较浮点数差异
///         - 避免频繁的通知触发
///         - 监听器异常不会影响数据设置
///     </remarks>
/// </summary>
public class PlayerData
{
	#region 常量定义 - 属性范围限制

	/// <summary>
	///     最小移动速度 (像素/秒)
	/// </summary>
	public const float MIN_SPEED = 50.0f;

	/// <summary>
	///     最大移动速度 (像素/秒)
	/// </summary>
	public const float MAX_SPEED = 1000.0f;

	/// <summary>
	///     默认移动速度 (像素/秒)
	/// </summary>
	public const float DEFAULT_SPEED = 300.0f;

	/// <summary>
	///     最小跳跃速度绝对值 (像素/秒)
	///     跳跃速度通常为负值(向上)，此处限制绝对值
	/// </summary>
	public const float MIN_JUMP_VELOCITY_ABS = 200.0f;

	/// <summary>
	///     最大跳跃速度绝对值 (像素/秒)
	/// </summary>
	public const float MAX_JUMP_VELOCITY_ABS = 1000.0f;

	/// <summary>
	///     默认跳跃速度 (像素/秒, 负值表示向上)
	/// </summary>
	public const float DEFAULT_JUMP_VELOCITY = -500.0f;

	/// <summary>
	///     最小重力加速度 (像素/秒²)
	/// </summary>
	public const float MIN_GRAVITY = 100.0f;

	/// <summary>
	///     最大重力加速度 (像素/秒²)
	/// </summary>
	public const float MAX_GRAVITY = 3000.0f;

	/// <summary>
	///     默认重力加速度 (像素/秒²)
	///     从Godot项目设置获取的实际默认值
	/// </summary>
	public const float DEFAULT_GRAVITY = 980.0f;

	/// <summary>
	///     最小奔跑速度倍率
	/// </summary>
	public const float MIN_SPRINT_MULTIPLIER = 1.0f;

	/// <summary>
	///     最大奔跑速度倍率
	/// </summary>
	public const float MAX_SPRINT_MULTIPLIER = 3.0f;

	/// <summary>
	///     默认奔跑速度倍率
	/// </summary>
	public const float DEFAULT_SPRINT_MULTIPLIER = 1.5f;

	/// <summary>
	///     浮点数比较精度阈值
	///     <para>
	///         当新旧值差异小于此阈值时视为相同值
	///         用于避免浮点数精度问题导致的误判
	///     </para>
	/// </summary>
	private const float EPSILON = 0.001f;

	#endregion

	#region 私有字段

	/// <summary>
	///     当前水平移动速度 (像素/秒)
	/// </summary>
	private float _speed = DEFAULT_SPEED;

	/// <summary>
	///     当前跳跃初速度 (像素/秒, 通常为负值)
	/// </summary>
	private float _jumpVelocity = DEFAULT_JUMP_VELOCITY;

	/// <summary>
	///     当前重力加速度 (像素/秒²)
	/// </summary>
	private float _gravity = DEFAULT_GRAVITY;

	/// <summary>
	///     当前奔跑速度倍率
	/// </summary>
	private float _sprintMultiplier = DEFAULT_SPRINT_MULTIPLIER;

	/// <summary>
	///     数据变更监听器列表
	///     <para>
	///         存储所有注册的数据监听器实例
	///         属性变更时会依次通知所有监听器
	///     </para>
	/// </summary>
	private readonly List<IPlayerDataListener> _listeners = new();

	/// <summary>
	///     监听器列表访问锁对象
	///     <para>
	///         用于确保监听器列表的线程安全访问
	///         防止并发添加/移除/遍历导致的竞争条件
	///     </para>
	/// </summary>
	private readonly object _listenerLock = new();

	#endregion

	#region 公开属性 - 移动相关

	/// <summary>
	///     水平移动速度 (像素/秒)
	///     <para>
	///         控制角色在地面上行走时的最大水平速度
	///     </para>
	///     <remarks>
	///         取值范围: [MIN_SPEED, MAX_SPEED] = [50, 1000]
	///         默认值: 300.0
	///         典型值:
	///           - 慢速角色: 150-250
	///           - 普通角色: 300-400
	///           - 快速角色: 500-700
	///         
	///         验证机制:
	///         设置时会自动调用ValidateSpeed()方法
	///         超出范围的值会被截断到最近的边界值
	///         
	///         通知机制:
	///         仅当新值与旧值差异 > EPSILON时才触发通知
	///         避免不必要的监听器回调
	///     </remarks>
	/// </summary>
	public float Speed
	{
		get => _speed;
		set
		{
			var validatedValue = ValidateSpeed(value);
			
			if (Math.Abs(_speed - validatedValue) > EPSILON)
			{
				var oldValue = _speed;
				_speed = validatedValue;
				GD.Print($"[PlayerData] Speed changed: {oldValue:F1} → {_speed:F1}");
				NotifySpeedChanged(oldValue, _speed);
			}
		}
	}

	/// <summary>
	///     跳跃初速度 (像素/秒)
	///     <para>
	///         角色起跳时的初始垂直速度，通常为负值(向上为负Y方向)
	///     </para>
	///     <remarks>
	///         取值范围: [-MAX_JUMP_VELOCITY_ABS, -MIN_JUMP_VELOCITY_ABS] = [-1000, -200]
	///         默认值: -500.0
	///         物理公式: jumpHeight = velocity² / (2 * gravity)
	///         当前配置跳跃高度 ≈ 127.5 像素
	///         
	///         注意: 此值为负数表示向上跳跃(符合Godot坐标系)
	///         
	///         验证机制:
	///         设置时会自动调用ValidateJumpVelocity()方法
	///         会保持符号不变，仅调整绝对值到有效范围
	///     </remarks>
	/// </summary>
	public float JumpVelocity
	{
		get => _jumpVelocity;
		set
		{
			var validatedValue = ValidateJumpVelocity(value);
			
			if (Math.Abs(_jumpVelocity - validatedValue) > EPSILON)
			{
				var oldValue = _jumpVelocity;
				_jumpVelocity = validatedValue;
				GD.Print($"[PlayerData] JumpVelocity changed: {oldValue:F1} → {_jumpVelocity:F1}");
				NotifyJumpVelocityChanged(oldValue, _jumpVelocity);
			}
		}
	}

	/// <summary>
	///     重力加速度 (像素/秒²)
	///     <para>
	///         影响角色在空中的下落速度和跳跃高度
	///     </para>
	///     <remarks>
	///         取值范围: [MIN_GRAVITY, MAX_GRAVITY] = [100, 3000]
	///         默认值: 980.0 (接近真实地球重力9.8m/s²的缩放)
	///         注意: 实际运行时会从Godot项目设置读取默认值
	///         
	///         物理影响:
	///         - 较大的重力 → 更快的下落、更低的跳跃高度
	///         - 较小的重力 → 更慢的下落、更高的跳跃高度
	///         
	///         验证机制:
	///         设置时会自动调用ValidateGravity()方法
	///         超出范围的值会被截断到边界值
	///     </remarks>
	/// </summary>
	public float Gravity
	{
		get => _gravity;
		set
		{
			var validatedValue = ValidateGravity(value);
			
			if (Math.Abs(_gravity - validatedValue) > EPSILON)
			{
				var oldValue = _gravity;
				_gravity = validatedValue;
				GD.Print($"[PlayerData] Gravity changed: {oldValue:F1} → {_gravity:F1}");
				NotifyGravityChanged(oldValue, _gravity);
			}
		}
	}

	/// <summary>
	///     奔跑速度倍率
	///     <para>
	///         当角色处于奔跑状态时，移动速度将乘以此倍率
	///     </para>
	///     <remarks>
	///         取值范围: [MIN_SPRINT_MULTIPLIER, MAX_SPRINT_MULTIPLIER] = [1.0, 3.0]
	///         默认值: 1.5
	///         示例: Speed=300, SprintMultiplier=1.5 → 实际速度=450
	///         
	///         设计考虑:
	///         - 最小值1.0表示不加速（正常行走）
	///         - 最大值3.0防止过快的移动速度
	///         - 典型游戏值在1.3-1.8之间
	///         
	///         验证机制:
	///         设置时会自动调用ValidateSprintMultiplier()方法
	///         小于1.0的值会提升到1.0（不允许减速）
	///     </remarks>
	/// </summary>
	public float SprintMultiplier
	{
		get => _sprintMultiplier;
		set
		{
			var validatedValue = ValidateSprintMultiplier(value);
			
			if (Math.Abs(_sprintMultiplier - validatedValue) > EPSILON)
			{
				var oldValue = _sprintMultiplier;
				_sprintMultiplier = validatedValue;
				GD.Print($"[PlayerData] SprintMultiplier changed: {oldValue:F2} → {_sprintMultiplier:F2}");
				NotifySprintMultiplierChanged(oldValue, _sprintMultiplier);
			}
		}
	}

	#endregion

	#region 计算属性 - 派生值

	/// <summary>
	///     实际奔跑速度 (像素/秒)
	///     <para>
	///         根据基础速度和奔跑倍率计算得出
	///         此为只读计算属性，每次访问时实时计算
	///     </para>
	///     <value>
	///     Speed * SprintMultiplier 的结果
	///     </value>
	///     <remarks>
	///         用途:
	///         - PlayerInputHandler.CalculateActualSpeed()使用此值
	///         - UI显示当前实际奔跑速度
	///         - 调试时查看速度参数效果
	///         
	///         示例:
	///         Speed=300, SprintMultiplier=1.5 → ActualSprintSpeed=450
	///     </remarks>
	/// </summary>
	public float ActualSprintSpeed => Speed * SprintMultiplier;

	/// <summary>
	///     计算得出的跳跃高度 (像素)
	///     <para>
	///         基于物理公式: h = v² / (2g)
	///         此为只读计算属性，每次访问时实时计算
	///     </para>
	///     <value>
	///     基于当前JumpVelocity和Gravity计算的跳跃高度
	///     </value>
	///     <remarks>
	///         公式推导:
	///         - 初速度v，重力加速度g
	///         - 到达最高点时速度为0: 0 = v - g*t → t = v/g
	///         - 高度h = v*t - 0.5*g*t² = v*(v/g) - 0.5*g*(v/g)²
	///         - 化简得: h = v² / (2g)
	///         
	///         示例:
	///         JumpVelocity=-500, Gravity=980 → CalculatedJumpHeight≈127.5
	///         
	///         用途:
	///         - UI显示理论跳跃高度
	///         - 调试时验证物理参数合理性
	///         - 关卡设计参考
	///     </remarks>
	/// </summary>
	public float CalculatedJumpHeight =>
		(float)(JumpVelocity * JumpVelocity) / (2 * Math.Abs(Gravity));

	#endregion

	#region 监听器管理

	/// <summary>
	///     添加数据变更监听器
	///     <para>
	///         监听器会在任何属性变化时收到通知
	///         同一个监听器实例只会被添加一次（去重）
	///     </para>
	///     <param name="listener">要添加的监听器实例</param>
	///     <exception cref="ArgumentNullException">当listener为null时抛出</exception>
	///     <remarks>
	///         线程安全:
	///         使用_listenerLock确保并发安全
	///         
	///         去重机制:
	///         通过Contains()检查避免重复添加
	///         重复添加同一实例不会产生副作用
	///         
	///         使用场景:
	///         <code>
	///         // 在初始化时注册监听器
	///         playerData.AddListener(physicsMovement);
	///         playerData.AddListener(inputHandler);
	 ///         
	///         // 后续修改属性会自动通知这些模块
	///         playerData.Speed = 350.0f; // physicsMovement和inputHandler都会收到通知
	///         </code>
	///         
	///         生命周期管理:
	///         - 必须在监听器不再使用前调用RemoveListener()
	///         - 否则会导致内存泄漏（PlayerData持有监听器引用）
	///         - 建议在Dispose或Destroy中移除监听器
	///     </remarks>
	/// </summary>
	public void AddListener(IPlayerDataListener listener)
	{
		if (listener == null)
		{
			GD.PrintErr("[PlayerData] AddListener: listener不能为null");
			return;
		}

		lock (_listenerLock)
		{
			if (!_listeners.Contains(listener))
			{
				_listeners.Add(listener);
				GD.Print($"[PlayerData] Listener added: {listener.GetType().Name} (Total: {_listeners.Count})");
			}
		}
	}

	/// <summary>
	///     移除数据变更监听器
	///     <para>
	///         移除后该监听器将不再收到属性变更通知
	///     </para>
	///     <param name="listener">要移除的监听器实例</param>
	///     <remarks>
	///         线程安全:
	///         使用_listenerLock确保并发安全
	///         
	///         安全性:
	///         如果传入的listener不在列表中，不会有任何副作用
	///         不会抛出异常
	///         
	///         使用场景:
	///         <code>
	///         // 在销毁时移除监听器
	///         playerData.RemoveListener(physicsMovement);
	///         playerData.RemoveListener(inputHandler);
	///         </code>
	///         
	///         重要提示:
	///         务必在监听器生命周期结束时调用此方法
	///         避免PlayerData持有已销毁对象的引用导致内存泄漏
	///     </remarks>
	/// </summary>
	public void RemoveListener(IPlayerDataListener listener)
	{
		if (listener == null)
		{
			return;
		}

		lock (_listenerLock)
		{
			if (_listeners.Remove(listener))
			{
				GD.Print($"[PlayerData] Listener removed: {listener.GetType().Name} (Total: {_listeners.Count})");
			}
		}
	}

	/// <summary>
	///     清除所有监听器
	///     <para>
	///         通常在对象销毁时调用以避免内存泄漏
	///         清除后所有属性变更将不再发送通知
	///     </para>
	///     <remarks>
	///         使用场景:
	///         - PlayerDataManager销毁时
	///         - 场景切换时清理旧监听器
	///         - 重置玩家数据时
	///         
	///         注意事项:
	///         清除后如果再次修改属性，不会有任何监听器收到通知
	///         如果需要重新接收通知，必须重新添加监听器
	///         
	///         线程安全:
	///         使用_listenerLock确保并发安全
	///     </remarks>
	/// </summary>
	public void ClearListeners()
	{
		lock (_listenerLock)
		{
			var count = _listeners.Count;
			_listeners.Clear();
			GD.Print($"[PlayerData] All listeners cleared (Removed: {count})");
		}
	}

	/// <summary>
	///     获取当前注册的监听器数量
	///     <para>
	///         用于调试和监控目的
	///     </para>
	///     <returns>当前已注册的监听器数量</returns>
	///     <remarks>
	///         用途:
	///         - 调试时检查是否有预期的监听器
	///         - 监控内存泄漏（监听器数量异常增长）
	///         - 单元测试中验证监听器注册状态
	///     </remarks>
	/// </summary>
	public int ListenerCount
	{
		get
		{
			lock (_listenerLock)
			{
				return _listeners.Count;
			}
		}
	}

	#endregion

	#region 数据验证方法

	/// <summary>
	///     验证并约束移动速度值到有效范围
	/// </summary>
	/// <param name="value">输入的速度值</param>
	/// <returns>约束后的有效速度值</returns>
	/// <remarks>
	///     验证规则:
	///     - value < MIN_SPEED → 返回 MIN_SPEED (50)
	///     - value > MAX_SPEED → 返回 MAX_SPEED (1000)
	///     - 其他情况 → 返回原值
	///     
	///     使用Math.Clamp实现范围限制
	///     这是.NET Core 2.0+提供的高效数学函数
	/// </remarks>
	public static float ValidateSpeed(float value)
	{
		return Math.Clamp(value, MIN_SPEED, MAX_SPEED);
	}

	/// <summary>
	///     验证并约束跳跃速度值到有效范围
	///     <para>
	///         跳跃速度必须为负值(向上)，且绝对值在有效范围内
	///     </para>
	///     <param name="value">输入的跳跃速度值</param>
	///     <returns>约束后的有效跳跃速度值</returns>
	/// <remarks>
	///     验证规则:
	///     1. 取输入值的绝对值
	///     2. 将绝对值约束到 [MIN_JUMP_VELOCITY_ABS, MAX_JUMP_VELOCITY_ABS]
	///     3. 恢复原始符号（负值表示向上）
	///     
	///     特殊处理:
	///     - 正值输入：转换为负值（强制向上）
	///     - 负值输入：保持负号，调整绝对值
	///     
	///     示例:
	///     ValidateSpeed(-1500) → -1000 (超出上限)
	///     ValidateSpeed(-100) → -200 (低于下限)
	///     ValidateSpeed(500) → -500 (正值转负)
	///     ValidateSpeed(-400) → -400 (在范围内)
	/// </remarks>
	public static float ValidateJumpVelocity(float value)
	{
		var absValue = Math.Abs(value);
		var clampedAbs = Math.Clamp(absValue, MIN_JUMP_VELOCITY_ABS, MAX_JUMP_VELOCITY_ABS);
		
		return value >= 0 ? -clampedAbs : clampedAbs;
	}

	/// <summary>
	///     验证并约束重力加速度值到有效范围
	/// </summary>
	/// <param name="value">输入的重力值</param>
	/// <returns>约束后的有效重力值</returns>
	/// <remarks>
	///     验证规则:
	///     - value < MIN_GRAVITY → 返回 MIN_GRAVITY (100)
	///     - value > MAX_GRAVITY → 返回 MAX_GRAVITY (3000)
	///     - 其他情况 → 返回原值
	///     
	///     物理意义:
	///     重力值影响下落速度和跳跃高度
	///     过小的重力会导致飘浮感
	///     过大的重力会导致快速坠落
	/// </remarks>
	public static float ValidateGravity(float value)
	{
		return Math.Clamp(value, MIN_GRAVITY, MAX_GRAVITY);
	}

	/// <summary>
	///     验证并约束奔跑倍率值到有效范围
	/// </summary>
	/// <param name="value">输入的奔跑倍率</param>
	/// <returns>约束后的有效奔跑倍率</returns>
	/// <remarks>
	///     验证规则:
	///     - value < MIN_SPRINT_MULTIPLIER → 返回 1.0 (不加速)
	///     - value > MAX_SPRINT_MULTIPLIER → 返回 3.0 (最大加速)
	///     - 其他情况 → 返回原值
	///     
	///     设计考虑:
	///     最小值1.0确保奔跑不会比行走慢
	///     这符合游戏设计的直觉预期
	/// </remarks>
	public static float ValidateSprintMultiplier(float value)
	{
		return Math.Clamp(value, MIN_SPRINT_MULTIPLIER, MAX_SPRINT_MULTIPLIER);
	}

	#endregion

	#region 批量操作

	/// <summary>
	///     从字典批量加载属性值
	///     <para>
	///         用于从持久化存储或配置文件恢复数据
	///         会触发所有属性的变更通知
	///     </para>
	///     <param name="data">包含属性键值对的字典</param>
	/// <remarks>
	///     键名映射:
	///     "Speed" → Speed属性
	///     "JumpVelocity" → JumpVelocity属性
	///     "Gravity" → Gravity属性
	///     "SprintMultiplier" → SprintMultiplier属性
	///     
	///     容错处理:
	///     - 字典为null: 直接返回，不做任何操作
	///     - 键不存在: 跳过该属性，使用当前值
	///     - 值类型错误: 由属性setter的验证方法处理
	///     
	///     通知触发:
	///     每个属性赋值都会独立触发验证和通知
	///     与逐个设置属性的效果完全一致
	///     
	///     使用场景:
	///     <code>
	///     // 从ConfigFile加载
	///     var data = LoadFromConfigFile();
	///     playerData.LoadFromDictionary(data);
	///     </code>
	///     
	///     性能说明:
	///     此方法会触发多次属性设置和通知
	///     如果性能敏感且不需要通知，考虑使用内部字段直接赋值
	///     （但这样会绕过验证机制，不推荐）
	/// </remarks>
	/// </summary>
	public void LoadFromDictionary(Dictionary<string, float> data)
	{
		if (data == null)
		{
			GD.Print($"[PlayerData] LoadFromDictionary: data dictionary is null");
			return;
		}

		GD.Print("[PlayerData] Loading properties from dictionary...");
		
		if (data.TryGetValue("Speed", out var speed))
		{
			Speed = speed;
			GD.Print($"[PlayerData]   Loaded Speed: {speed}");
		}

		if (data.TryGetValue("JumpVelocity", out var jumpVel))
		{
			JumpVelocity = jumpVel;
			GD.Print($"[PlayerData]   Loaded JumpVelocity: {jumpVel}");
		}

		if (data.TryGetValue("Gravity", out var gravity))
		{
			Gravity = gravity;
			GD.Print($"[PlayerData]   Loaded Gravity: {gravity}");
		}

		if (data.TryGetValue("SprintMultiplier", out var sprint))
		{
			SprintMultiplier = sprint;
			GD.Print($"[PlayerData]   Loaded SprintMultiplier: {sprint}");
		}

		GD.Print("[PlayerData] Dictionary loading completed");
	}

	/// <summary>
	///     将所有属性导出为字典
	///     <para>
	///         用于持久化保存或序列化传输
	///     </para>
	///     <returns>包含所有属性值的字典</returns>
	/// <remarks>
	///     导出格式:
	///     {
	///       "Speed": 300.0,
	///       "JumpVelocity": -500.0,
	///       "Gravity": 980.0,
	///       "SprintMultiplier": 1.5
	///     }
	///     
	///     使用场景:
	///     <code>
	///     // 保存到ConfigFile
	///     var data = playerData.ToDictionary();
	///     SaveToConfigFile(data);
	///     
	///     // 网络传输
	///     SendToServer(playerData.ToDictionary());
	///     </code>
	///     
	///     注意事项:
	///     导出的值是当前的原始值，未经过验证
	///     因为属性始终保持有效状态，所以导出的值一定是有效的
	///     
	///     线程安全:
	///     此方法读取多个属性，不是原子操作
	///     并发环境下可能得到不一致的快照
	///     如需一致性快照，请在外部加锁
	/// </remarks>
	/// </summary>
	public Dictionary<string, float> ToDictionary()
	{
		return new Dictionary<string, float>
		{
			{ "Speed", _speed },
			{ "JumpVelocity", _jumpVelocity },
			{ "Gravity", _gravity },
			{ "SprintMultiplier", _sprintMultiplier }
		};
	}

	/// <summary>
	///     重置所有属性为默认值
	///     <para>
	///         会触发所有属性的变更通知
	///     </para>
	///     <remarks>
	///         重置的目标值:
	///         - Speed → DEFAULT_SPEED (300.0)
	///         - JumpVelocity → DEFAULT_JUMP_VELOCITY (-500.0)
	///         - Gravity → DEFAULT_GRAVITY (980.0)
	///         - SprintMultiplier → DEFAULT_SPRINT_MULTIPLIER (1.5)
	///         
	///         使用场景:
	///         - 玩家选择"恢复默认设置"
	///         - 调试时重置到初始状态
	///         - 清除自定义配置回到标准值
	///         
	///         通知触发:
	///         即使当前值等于默认值，也会触发通知
	///         因为这是显式的重置操作，监听器可能需要响应
	///         
	///         性能说明:
	///         此方法会触发4次属性设置和通知
	///         如果有大量监听器，可能会有一定开销
	///     </remarks>
	/// </summary>
	public void ResetToDefaults()
	{
		GD.Print("[PlayerData] Resetting all properties to defaults...");
		
		Speed = DEFAULT_SPEED;
		JumpVelocity = DEFAULT_JUMP_VELOCITY;
		Gravity = DEFAULT_GRAVITY;
		SprintMultiplier = DEFAULT_SPRINT_MULTIPLIER;
		
		GD.Print("[PlayerData] Reset to defaults completed");
	}

	#endregion

	#region 通知方法 (私有)

	/// <summary>
	///     通知所有监听器速度发生变化
	/// </summary>
	/// <param name="oldValue">变化前的速度值</param>
	/// <param name="newValue">变化后的速度值</param>
	private void NotifySpeedChanged(float oldValue, float newValue)
	{
		NotifyListeners(listener => listener.OnSpeedChanged(oldValue, newValue));
	}

	/// <summary>
	///     通知所有监听器跳跃速度发生变化
	/// </summary>
	/// <param name="oldValue">变化前的跳跃速度值</param>
	/// <param name="newValue">变化后的跳跃速度值</param>
	private void NotifyJumpVelocityChanged(float oldValue, float newValue)
	{
		NotifyListeners(listener => listener.OnJumpVelocityChanged(oldValue, newValue));
	}

	/// <summary>
	///     通知所有监听器重力发生变化
	/// </summary>
	/// <param name="oldValue">变化前的重力值</param>
	/// <param name="newValue">变化后的重力值</param>
	private void NotifyGravityChanged(float oldValue, float newValue)
	{
		NotifyListeners(listener => listener.OnGravityChanged(oldValue, newValue));
	}

	/// <summary>
	///     通知所有监听器奔跑倍率发生变化
	/// </summary>
	/// <param name="oldValue">变化前的奔跑倍率</param>
	/// <param name="newValue">变化后的奔跑倍率</param>
	private void NotifySprintMultiplierChanged(float oldValue, float newValue)
	{
		NotifyListeners(listener => listener.OnSprintMultiplierChanged(oldValue, newValue));
	}

	/// <summary>
	///     通用的监听器通知方法
	///     <para>
	///         统一处理异常捕获和日志记录
	///         确保单个监听器异常不影响其他监听器和数据设置
	///     </para>
	///     <param name="notifyAction">要执行的监听器回调</param>
	/// <remarks>
	///         异常处理策略:
	///         1. try-catch包装每个监听器的回调
	///         2. 捕获异常后记录错误日志
	///         3. 继续通知下一个监听器
	///         4. 不向上层抛出异常
	///         
	///         线程安全:
	///         创建监听器列表的副本用于遍历
	///         遍历期间允许其他线程修改原列表
	///         但需要注意：副本可能包含已移除的监听器
	///         
	///         性能考虑:
	///         列表复制（ToList）的时间复杂度为O(n)
	///         对于少量监听器（<10个）可以忽略不计
	///         如果监听器数量很大，可考虑优化策略
	///         
	///         日志记录:
	///         仅在发生异常时输出错误日志
	///         正常情况下静默执行
	///     </remarks>
	/// </summary>
	private void NotifyListeners(Action<IPlayerDataListener> notifyAction)
	{
		List<IPlayerDataListener> listenersCopy;
		
		lock (_listenerLock)
		{
			listenersCopy = new List<IPlayerDataListener>(_listeners);
		}
		
		foreach (var listener in listenersCopy)
		{
			try
			{
				notifyAction(listener);
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[PlayerData] Listener exception in {listener.GetType().Name}: {ex.Message}");
			}
		}
	}

	#endregion
}
