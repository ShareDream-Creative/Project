using Godot;

namespace GFrameworkGodotTemplate.scripts.player.interfaces;

/// <summary>
///     玩家物理运动接口
///     <para>
///         定义角色物理移动的标准契约，封装速度计算和碰撞响应逻辑
///         与具体的物理引擎实现解耦
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
///         - 单一职责(SRP): 只负责物理运动计算
///         - 接口隔离(ISP): 提供最小化的方法集合
///         - 依赖倒置(DIP): 依赖CharacterBody2D抽象而非具体实现
///         
///         数据流向:
///         PlayerDataManager.PlayerData → (监听器) → PlayerPhysicsMovement属性更新
///         PlayerInputHandler (输入数据) → PlayerPhysicsMovement (速度计算) → CharacterBody2D (移动执行)
///         
///         实现要求:
///         - 必须实现IPlayerDataListener接口以支持数据自动同步
///         - 必须正确处理CharacterBody2D的碰撞响应
///         - 速度计算应考虑奔跑状态和倍率
///         
///         使用示例:
///         <code>
///         // 在物理更新循环中使用
///         physicsMovement.ApplyGravity(delta);
///         physicsMovement.UpdateHorizontalVelocity(direction, isSprinting, sprintMultiplier);
///         
///         if (inputHandler.IsJumpPressed &amp;&amp; physicsMovement.TryJump())
///         {
///             // 跳跃成功
///         }
///         
///         physicsMovement.Move(characterBody);
///         </code>
///         
///         性能说明:
///         - 所有方法应在_PhysicsProcess中调用
///         - 避免在_Update中调用（物理不同步）
///         - MoveAndSlide会自动处理碰撞
///     </remarks>
/// </summary>
public interface IPlayerPhysicsMovement
{
	/// <summary>
	///     水平移动速度 (像素/秒)
	///     <para>
	///         控制角色在地面上行走时的最大水平速度
	 ///     </para>
	///     <remarks>
	///         取值范围: [PlayerData.MIN_SPEED, PlayerData.MAX_SPEED] = [50, 1000]
	///         默认值: PlayerData.DEFAULT_SPEED = 300.0
	///         数据来源: 通过IPlayerDataListener从PlayerData.Speed自动同步
	///         
	///         使用场景:
	///         - UpdateHorizontalVelocity()中使用此值计算实际速度
	///         - 可在运行时通过修改PlayerData.Speed动态调整
	///     </remarks>
	/// </summary>
	float Speed { get; set; }

	/// <summary>
	///     跳跃初速度 (像素/秒)
	///     <para>
	///         角色起跳时的初始垂直速度，通常为负值(向上为负Y方向)
	///     </para>
	///     <remarks>
	///         取值范围: [-PlayerData.MAX_JUMP_VELOCITY_ABS, -PlayerData.MIN_JUMP_VELOCITY_ABS]
	///         默认值: PlayerData.DEFAULT_JUMP_VELOCITY = -500.0
	///         数据来源: 通过IPlayerDataListener从PlayerData.JumpVelocity自动同步
	///         
	///         物理公式:
	///         jumpHeight = velocity² / (2 * gravity)
	///         当前配置跳跃高度 ≈ 127.5 像素
	///         
	///         注意: 此值为负数表示向上跳跃(符合Godot坐标系)
	///     </remarks>
	/// </summary>
	float JumpVelocity { get; set; }

	/// <summary>
	///     重力加速度 (像素/秒²)
	///     <para>
	///         影响角色在空中的下落速度和跳跃高度
	///     </para>
	///     <remarks>
	///         取值范围: [PlayerData.MIN_GRAVITY, PlayerData.MAX_GRAVITY] = [100, 3000]
	///         默认值: PlayerData.DEFAULT_GRAVITY = 980.0 (接近真实地球重力)
	///         数据来源: 通过IPlayerDataListener从PlayerData.Gravity自动同步
	///         
	///         使用场景:
	///         - ApplyGravity()中每帧累加到垂直速度
	///         - 值越大下落越快，跳跃高度越低
	///         - 可用于实现不同的"手感"
	///     </remarks>
	/// </summary>
	float Gravity { get; set; }

	/// <summary>
	///     获取当前速度向量
	///     <para>
	///         包含水平和垂直分量的完整速度状态
	///     </para>
	///     <remarks>
	///         返回值说明:
	///         - X分量: 水平速度 (正=右, 负=左)
	///         - Y分量: 垂直速度 (正=下, 负=上)
	///         
	///         使用场景:
	///         - 调试时显示当前速度
	///         - 其他系统需要读取速度信息时
	///         - 动画状态机根据速度选择动画
	///     </remarks>
	/// </summary>
	Vector2 CurrentVelocity { get; }

	/// <summary>
	///     检测角色是否在地面上
	///     <para>
	///         基于上一次MoveAndSlide()的碰撞检测结果
	///     </para>
	///     <remarks>
	///         检测机制:
	///         - 在Move()方法中调用body.IsOnFloor()更新此值
	///         - 依赖于Godot的碰撞检测系统
	///         - 需要正确的CollisionShape设置
	///         
	///         使用场景:
	///         - TryJump()检查是否允许跳跃
	///         - ApplyGravity()判断是否应用重力
	///         - 动画系统切换落地/空中动画
	///         
	///         注意: 此值在每次Move()后更新，不是实时检测
	///     </remarks>
	/// </summary>
	bool IsOnFloor { get; }

	/// <summary>
	///     应用重力效果
	///     <para>
	///         根据当前是否在地面决定是否累加重力加速度
	///     </para>
	///     <param name="delta">帧间隔时间(秒)，来自_PhysicsProcess参数</param>
	///     <remarks>
	///         实现逻辑:
	///         if (!IsOnFloor)
	///         {
	///             velocity.Y += Gravity * delta;
	///         }
	///         
	///         物理原理:
	///         - 仅在空中时应用重力(地面时有摩擦力平衡)
	///         - 使用欧拉积分法累加速度
	///         - delta确保帧率无关的运动
	///         
	///         调用时机:
	///         应在UpdateHorizontalVelocity()之前调用
	///         确保垂直速度先于水平速度更新
	///     </remarks>
	/// </summary>
	void ApplyGravity(float delta);

	/// <summary>
	///     根据输入方向更新水平速度
	///     <para>
	///         支持奔跑状态: 当isSprinting为true时，自动应用SprintMultiplier
	///     </para>
	///     <param name="direction">水平方向值 [-1.0, 1.0]</param>
	///     <param name="isSprinting">是否处于奔跑状态</param>
	///     <param name="sprintMultiplier">
	///         奔跑速度倍率(默认1.0表示不加速)
	///         来自IPlayerInputHandler.CachedSprintMultiplier
	///     </param>
	///     <remarks>
	///         速度计算公式:
	///         - 普通状态: velocity.X = direction * Speed
	///         - 奔跑状态: velocity.X = direction * Speed * SprintMultiplier
	///         
	///         减速处理:
	///         当direction=0时使用MoveToward平滑减速:
	///         velocity.X = MoveToward(velocity.X, 0, actualSpeed)
	///         
	///         使用示例:
	///         <code>
	///         // 普通移动
	///         physicsMovement.UpdateHorizontalVelocity(inputHandler.HorizontalDirection);
	///         
	///         // 奔跑移动
	///         physicsMovement.UpdateHorizontalVelocity(
	///             inputHandler.HorizontalDirection,
	///             inputHandler.IsSprinting,
	///             inputHandler.CachedSprintMultiplier
	///         );
	///         </code>
	///     </remarks>
	/// </summary>
	void UpdateHorizontalVelocity(float direction, bool isSprinting = false, float sprintMultiplier = 1.0f);

	/// <summary>
	///     执行跳跃操作
	///     <para>
	///         仅在地面时有效，防止空中二段跳
	///     </para>
	///     <returns>
	///     bool: 是否成功执行跳跃
	///     - true: 成功跳跃 (在地面上)
	///     - false: 跳跃失败 (在空中或条件不满足)
	///     </returns>
	///     <remarks>
	///         实现逻辑:
	///         if (IsOnFloor)
	///         {
	///             velocity.Y = JumpVelocity;
	///             IsOnFloor = false; // 立即标记为空中
	///             return true;
	///         }
	///         return false;
	///         
	///         安全措施:
	///         - 地面检测防止空中跳跃
	///         - 跳跃后立即更新IsOnFloor状态
	///         - 返回值允许调用者进行日志记录或音效播放
	///         
	///         使用示例:
	///         <code>
	///         if (inputHandler.IsJumpPressed &amp;&amp; physicsMovement.TryJump())
	///         {
	///             _audioSystem.PlayJumpSound();
	///             _log.Debug("玩家跳跃");
	///         }
	///         </code>
	///     </remarks>
	/// </summary>
	bool TryJump();

	/// <summary>
	///     应用计算后的速度并执行物理移动
	///     <para>
	///         内部调用MoveAndSlide处理滑动碰撞
	///     </para>
	///     <param name="body">要移动的CharacterBody2D节点实例</param>
	///     <remarks>
	///         执行流程:
	///         1. 将内部速度向量赋值给body.Velocity
	///         2. 调用body.MoveAndSlide()执行移动和碰撞
	///         3. 更新IsOnFloor状态(基于碰撞结果)
	///         
	///         MoveAndSlide特性:
	///         - 自动处理滑动碰撞(沿墙壁滑动)
	///         - 自动处理斜坡运动
	///         - 更新IsOnWall(), IsOnCeiling()等状态
	///         
	///         调用时机:
	///         应在所有速度更新完成后调用一次
	///         通常是一帧中最后一个调用的方法
	///         
	///         注意事项:
	///         - 必须传入有效的CharacterBody2D实例
	///         - 不应在一次物理更新中多次调用
	///     </remarks>
	/// </summary>
	void Move(CharacterBody2D body);

	/// <summary>
	///     立即停止所有移动(归零速度)
	///     <para>
	///         用于状态切换或暂停时立即制动
	///     </para>
	///     <remarks>
	///         使用场景:
	///         - 游戏暂停时停止角色移动
	///         - 场景切换时清除速度状态
	///         - 死亡或受击时立即停止
	///         - 输入禁用时(非PlayingState)
	///         
	///         实现细节:
	///         直接将速度向量设置为Vector2.Zero
	///         不使用减速过程，而是瞬间停止
	///         
	///         后续影响:
	///         下次Move()时会保持静止状态
	///         直到收到新的输入指令
	///     </remarks>
	/// </summary>
	void StopImmediately();
}
