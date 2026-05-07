using Godot;

namespace GFrameworkGodotTemplate.scripts.player.interfaces;

/// <summary>
///     玩家物理运动接口
///     定义角色物理移动的标准契约，封装速度计算和碰撞响应逻辑
///     与具体的物理引擎实现解耦
/// </summary>
public interface IPlayerPhysicsMovement
{
	/// <summary>
	///     水平移动速度 (像素/秒)
	/// </summary>
	float Speed { get; set; }

	/// <summary>
	///     跳跃初速度 (像素/秒, 通常为负值表示向上)
	/// </summary>
	float JumpVelocity { get; set; }

	/// <summary>
	///     重力加速度 (像素/秒²)
	/// </summary>
	float Gravity { get; set; }

	/// <summary>
	///     获取当前速度向量
	/// </summary>
	Vector2 CurrentVelocity { get; }

	/// <summary>
	///     检测角色是否在地面上
	/// </summary>
	bool IsOnFloor { get; }

	/// <summary>
	///     应用重力效果
	/// </summary>
	/// <param name="delta">帧间隔时间(秒)</param>
	void ApplyGravity(float delta);

	/// <summary>
	///     根据输入方向更新水平速度
	/// </summary>
	/// <param name="direction">水平方向值 [-1.0, 1.0]</param>
	void UpdateHorizontalVelocity(float direction);

	/// <summary>
	///     执行跳跃操作
	///     仅在地面时有效
	/// </summary>
	/// <returns>是否成功执行跳跃</returns>
	bool TryJump();

	/// <summary>
	///     应用计算后的速度并执行物理移动
	///     内部调用MoveAndSlide处理碰撞
	/// </summary>
	/// <param name="body">要移动的CharacterBody2D节点</param>
	void Move(CharacterBody2D body);

	/// <summary>
	///     立即停止所有移动(归零速度)
	///     用于状态切换或暂停时立即制动
	/// </summary>
	void StopImmediately();
}
