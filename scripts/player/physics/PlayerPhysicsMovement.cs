using GFrameworkGodotTemplate.scripts.data.interfaces;
using GFrameworkGodotTemplate.scripts.data.model;
using GFrameworkGodotTemplate.scripts.player.interfaces;
using Godot;

namespace GFrameworkGodotTemplate.scripts.player.physics;

/// <summary>
///     玩家物理运动实现
///     <para>
	///         基于Godot CharacterBody2D的物理移动系统
	///         封装重力、速度计算、碰撞响应等核心逻辑
	///     </para>
///     <author>AI Assistant</author>
///     <version>2.1.0</version>
///     <date>2026-05-11</date>
///     <description>
///         核心职责:
///         1. 速度管理: 维护水平/垂直速度向量
///         2. 物理模拟: 实现重力、跳跃等物理效果
///         3. 碰撞处理: 调用MoveAndSlide处理碰撞响应
///         4. 状态追踪: 维护地面检测等状态标志
///         
///         架构增强(v2.0):
///         - 实现IPlayerDataListener接口，支持数据自动同步
///         - 从PlayerData统一获取属性值，确保数值一致性
///         - 支持运行时动态调整参数
///     </description>
///     <remarks>
///         设计原则:
///         - 单一职责(SRP): 只负责物理运动计算和执行
///         - 接口隔离(ISP): 通过IPlayerPhysicsMovement接口暴露最小化API
///         - 开闭原则(OCP): 通过监听器扩展行为，不修改本类
///         
///         数据流向:
///         PlayerDataManager.PlayerData → (IPlayerDataListener) → 本类属性更新
///         PlayerInputHandler (输入数据) → UpdateHorizontalVelocity() (速度计算)
///         → Move(CharacterBody2D) (移动执行)
///         
///         物理公式:
///         - 重力: velocity.Y += Gravity * delta (仅空中时)
///         - 水平速度: velocity.X = direction * Speed (* SprintMultiplier if sprinting)
///         - 跳跃: velocity.Y = JumpVelocity (仅地面时)
///         - 减速: velocity.X = MoveToward(velocity.X, 0, actualSpeed) (无输入时)
///         
///         使用示例:
///         <code>
///         // 创建实例
///         var physicsMovement = new PlayerPhysicsMovement();
///         
///         // 从PlayerData初始化
///         physicsMovement.InitializeFromData(playerDataManager.Data);
///         
///         // 在物理更新循环中使用
///         physicsMovement.ApplyGravity(delta);
///         physicsMovement.UpdateHorizontalVelocity(direction, isSprinting, sprintMultiplier);
///         
///         if (inputHandler.IsJumpPressed &amp;&amp; physicsMovement.TryJump())
///         {
///             // 跳跃成功
///         }
///         
///         physicsMovement.Move(playerCharacterBody);
///         </code>
///         
///         性能说明:
///         - 所有方法应在_PhysicsProcess中调用（非_Update）
///         - 避免在单帧内多次调用Move()
///         - MoveAndSlide会自动处理滑动碰撞
///     </remarks>
/// </summary>
public class PlayerPhysicsMovement : IPlayerPhysicsMovement, IPlayerDataListener
{
	#region 私有字段

	/// <summary>
	///     当前速度向量
	///     <para>
	///         X分量: 水平速度 (正=右, 负=左)
	///         Y分量: 垂直速度 (正=下, 负=上)
	///     </para>
	///     <remarks>
	///         此向量在每帧物理更新时被修改:
	///         - ApplyGravity(): 修改Y分量(累加重力)
	///         - UpdateHorizontalVelocity(): 修改X分量(设置水平速度)
	///         - TryJump(): 设置Y分量为跳跃初速度
	///         - StopImmediately(): 归零所有分量
	///         
	///         通过CurrentVelocity属性对外只读暴露
	///     </remarks>
	/// </summary>
	private Vector2 _velocity;

	/// <summary>
	///     地面检测状态标志
	///     <para>
	///         基于上一次MoveAndSlide()的碰撞检测结果
	///         每次调用Move()后更新此值
	///     </para>
	///     <remarks>
	///         更新时机:
	///         在Move()方法中通过body.IsOnFloor()更新
	///         
	///         使用场景:
	///         - TryJump()检查是否允许跳跃
	///         - ApplyGravity()判断是否应用重力
	///         - 动画系统切换落地/空中动画
	///         
	///         初始值: false (假设初始在空中)
	///     </remarks>
	/// </summary>
	private bool _isOnFloor;

	#endregion

	#region 物理属性 (从PlayerData同步)

	/// <inheritdoc />
	/// <remarks>
	///     取值范围: [PlayerData.MIN_SPEED, PlayerData.MAX_SPEED] = [50, 1000]
	///     默认值: PlayerData.DEFAULT_SPEED = 300.0
	///     数据来源: 通过OnSpeedChanged()从PlayerData.Speed自动同步
	///     
	///     使用场景:
	///     UpdateHorizontalVelocity()中使用此值计算实际水平速度
	/// </remarks>
	public float Speed { get; set; } = PlayerData.DEFAULT_SPEED;

	/// <inheritdoc />
	/// <remarks>
	///     取值范围: [-MAX_JUMP_VELOCITY_ABS, -MIN_JUMP_VELOCITY_ABS]
	///     默认值: PlayerData.DEFAULT_JUMP_VELOCITY = -500.0
	///     数据来源: 通过OnJumpVelocityChanged()从PlayerData.JumpVelocity自动同步
	///     
	///     注意: 此值为负数表示向上跳跃(符合Godot坐标系)
	///     当前配置跳跃高度 ≈ 127.5 像素
	/// </remarks>
	public float JumpVelocity { get; set; } = PlayerData.DEFAULT_JUMP_VELOCITY;

	/// <inheritdoc />
	/// <remarks>
	///     取值范围: [PlayerData.MIN_GRAVITY, PlayerData.MAX_GRAVITY] = [100, 3000]
	///     默认值: PlayerData.DEFAULT_GRAVITY = 980.0 (接近真实地球重力)
	///     数据来源: 通过OnGravityChanged()从PlayerData.Gravity自动同步
	///     
	///     使用场景:
	///     ApplyGravity()中每帧累加到垂直速度
	/// </remarks>
	public float Gravity { get; set; } = PlayerData.DEFAULT_GRAVITY;

	#endregion

	#region 状态属性

	/// <inheritdoc />
	/// <remarks>
	///     返回内部_velocity字段的当前值
	///     包含完整的水平和垂直速度信息
	///     用于调试显示或外部系统读取速度状态
	/// </remarks>
	public Vector2 CurrentVelocity => _velocity;

	/// <inheritdoc />
	/// <remarks>
	///     返回_isOnFloor字段的当前值
	///     基于上一次MoveAndSlide()的碰撞检测结果
	///     用于判断角色是否在地面上
	/// </remarks>
	public bool IsOnFloor => _isOnFloor;

	#endregion

	#region IPlayerPhysicsMovement 实现

	/// <inheritdoc />
	/// <remarks>
	///     实现逻辑:
	///     仅在 !_isOnFloor 时应用重力加速度
	///     使用欧拉积分法累加速度到垂直速度
	///     
	///     公式: velocity.Y += Gravity * delta
	///     
	///     调用时机:
	///     应在UpdateHorizontalVelocity()之前调用
	///     确保垂直速度先于水平速度更新
	/// </remarks>
	public void ApplyGravity(float delta)
	{
		if (!_isOnFloor)
		{
			_velocity.Y += Gravity * delta;
		}
	}

	/// <inheritdoc />
	/// <remarks>
	///     速度计算逻辑:
	///     1. 根据奔跑状态计算实际速度:
	///        actualSpeed = isSprinting ? Speed * sprintMultiplier : Speed
	///     
	///     2. 根据方向设置或减速:
	///        - direction != 0: velocity.X = direction * actualSpeed
	///        - direction == 0: velocity.X = MoveToward(velocity.X, 0, actualSpeed)
	///     
	///     减速处理:
	///     使用MoveToward实现平滑减速而非瞬间停止
	///     减速率等于actualSpeed，确保减速手感一致
	///     
	///     参数说明:
	///     - direction: 来自IPlayerInputHandler.HorizontalDirection, 范围[-1, 1]
	///     - isSprinting: 来自IPlayerInputHandler.IsSprinting
	///     - sprintMultiplier: 来自IPlayerInputHandler.CachedSprintMultiplier
	/// </remarks>
	public void UpdateHorizontalVelocity(float direction, bool isSprinting = false, float sprintMultiplier = 1.0f)
	{
		var actualSpeed = isSprinting ? Speed * sprintMultiplier : Speed;
		
		if (direction != 0)
		{
			_velocity.X = direction * actualSpeed;
		}
		else
		{
			_velocity.X = Mathf.MoveToward(_velocity.X, 0, actualSpeed);
		}
	}

	/// <inheritdoc />
	/// <remarks>
	///     安全措施:
	///     1. 地面检测: 仅在IsOnFloor为true时允许跳跃
	///     2. 状态更新: 跳跃后立即设置IsOnFloor=false
	///     3. 返回值: 允许调用者知道是否成功执行跳跃
	///     
	///     跳跃操作:
	///     直接设置velocity.Y为JumpVelocity(负值=向上)
	///     
	///     使用场景:
	///     配合IsJumpPressed使用:
	///     <code>
	///     if (_inputHandler.IsJumpPressed &amp;&amp; _physicsMovement.TryJump())
	///     {
	///         _audioSystem.PlayJumpSound();
	///     }
	///     </code>
	/// </remarks>
	public bool TryJump()
	{
		if (_isOnFloor)
		{
			_velocity.Y = JumpVelocity;
			_isOnFloor = false;
			return true;
		}

		return false;
	}

	/// <inheritdoc />
	/// <remarks>
	///     执行流程:
	///     1. 将内部速度向量赋值给body.Velocity
	///     2. 调用body.MoveAndSlide()执行移动和碰撞
	///     3. 将MoveAndSlide()修改后的速度同步回内部_velocity（重要！）
	///     4. 更新_isOnFloor状态(基于碰撞结果)
	///     
	///     MoveAndSlide特性:
	///     - 自动处理滑动碰撞(沿墙壁滑动)
	///     - 自动处理斜坡运动
	///     - **会修改body.Velocity** (碰撞响应、地板修正等)
	///     
	///     为什么需要步骤3:
	///     MoveAndSlide()会根据物理碰撞修改速度(例如:撞到地板时将Y速度归零)
	///     如果不同步回来，内部_velocity和实际速度会产生偏差
	///     导致重置时无法正确清除所有残留速度
	///     
	///     注意事项:
	///     - 必须传入有效的CharacterBody2D实例
	///     - 不应在一次物理更新中多次调用
	///     - 应在所有速度更新完成后调用
	/// </remarks>
	public void Move(CharacterBody2D body)
	{
		body.Velocity = _velocity;
		body.MoveAndSlide();

		_velocity = body.Velocity;
		_isOnFloor = body.IsOnFloor();
	}

	/// <inheritdoc />
	/// <remarks>
	///     实现方式:
	///     1. 将速度向量设置为Vector2.Zero（清除累积速度）
	///     2. 重置地面检测状态为true（模拟刚放置在地面上）
	///     
	///     不使用减速过程，而是瞬间停止
	///     
	///     后续影响:
	///     - 下次Move()时会保持静止状态
	///     - ApplyGravity()不会立即应用重力（因为IsOnFloor=true）
	///     - 直到收到新的输入指令或离开地面
	///     
	///     使用场景:
	///     - 游戏暂停时停止角色移动
	///     - 输入禁用时(非PlayingState)
	///     - 场景切换时清除速度状态
	///     - 死亡或受击时立即停止
	///     - 玩家重生/重置到起点时完全清除物理状态
	/// </remarks>
	public void StopImmediately()
	{
		_velocity = Vector2.Zero;
		_isOnFloor = true;
	}

	#endregion

	#region IPlayerDataListener 实现

	/// <summary>
	///     当玩家速度变化时自动更新本地缓存
	///     <param name="oldValue">变化前的速度值</param>
	///     <param name="newValue">变化后的速度值</param>
	///     <remarks>
	///         同步机制:
	///         通过PlayerDataManager注册为监听器
	///         当PlayerData.Speed变更时自动触发
	///         立即更新本地Speed属性
	///     </remarks>
	/// </summary>
	public void OnSpeedChanged(float oldValue, float newValue)
	{
		Speed = newValue;
		GD.Print($"[PlayerPhysicsMovement] 速度已更新: {oldValue} → {newValue}");
	}

	/// <summary>
	///     当跳跃速度变化时自动更新本地缓存
	///     <param name="oldValue">变化前的跳跃速度值</param>
	///     <param name="newValue">变化后的跳跃速度值</param>
	///     <remarks>
	///         同步机制:
	///         通过PlayerDataManager注册为监听器
	///         当PlayerData.JumpVelocity变更时自动触发
	///         立即更新本地JumpVelocity属性
	///     </remarks>
	/// </summary>
	public void OnJumpVelocityChanged(float oldValue, float newValue)
	{
		JumpVelocity = newValue;
		GD.Print($"[PlayerPhysicsMovement] 跳跃速度已更新: {oldValue} → {newValue}");
	}

	/// <summary>
	///     当重力变化时自动更新本地缓存
	///     <param name="oldValue">变化前的重力值</param>
	///     <param name="newValue">变化后的重力值</param>
	///     <remarks>
	///         同步机制:
	///         通过PlayerDataManager注册为监听器
	///         当PlayerData.Gravity变更时自动触发
	///         立即更新本地Gravity属性
	///     </remarks>
	/// </summary>
	public void OnGravityChanged(float oldValue, float newValue)
	{
		Gravity = newValue;
		GD.Print($"[PlayerPhysicsMovement] 重力已更新: {oldValue} → {newValue}");
	}

	/// <summary>
	///     当奔跑倍率变化时的处理
	///     <param name="oldValue">变化前的奔跑倍率</param>
	///     <param name="newValue">变化后的奔跑倍率</param>
	///     <remarks>
	///         当前行为: 不直接使用此属性
	///         奔跑倍率由输入模块(PlayerInputHandler)缓存并使用
	///         物理模块在UpdateHorizontalVelocity()中接收sprintMultiplier参数
	///         
	///         未来扩展: 可能用于本地缓存优化
	///     </remarks>
	/// </summary>
	public void OnSprintMultiplierChanged(float oldValue, float newValue)
	{
		// 物理模块不直接使用奔跑倍率
		// 此属性由输入模块在计算实际速度时使用
	}

	#endregion

	#region 辅助方法

	/// <summary>
	///     从PlayerData实例初始化所有属性
	///     <para>
	///         通常在对象创建或重置时调用
	///         批量设置所有物理参数
	///     </para>
	///     <param name="playerData">数据源，包含所有玩家可配置的物理参数</param>
	///     <remarks>
	///         初始化内容:
	///         - Speed: 水平移动速度
	///         - JumpVelocity: 跳跃初速度
	///         - Gravity: 重力加速度
	///         
	///         调用时机:
	///         - PlayerMovementController._Ready()中的SyncConfigurationToModules()
	///         - 对象创建后的初始化阶段
	///         - 重置为默认值时
	///         
	///         安全检查:
	///         如果playerData为null则跳过初始化
	///         属性保持默认值不变
	///     </remarks>
	/// </summary>
	public void InitializeFromData(PlayerData playerData)
	{
		if (playerData == null) return;

		Speed = playerData.Speed;
		JumpVelocity = playerData.JumpVelocity;
		Gravity = playerData.Gravity;
	}

	#endregion
}
