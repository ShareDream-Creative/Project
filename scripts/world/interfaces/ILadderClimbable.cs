using Godot;

namespace GFrameworkGodotTemplate.scripts.world.interfaces;

/// <summary>
///     可攀爬物体接口
/// </summary>
public interface ILadderClimbable
{
	/// <summary>
	///     获取梯子碰撞区域的全局边界
	/// </summary>
	Rect2 GetGlobalBounds();
	
	/// <summary>
	///     攀爬速度（像素/秒）
	/// </summary>
	float ClimbSpeed { get; }
	
	/// <summary>
	///     是否允许从顶部进入攀爬
	/// </summary>
	bool AllowTopEntry { get; }
	
	/// <summary>
	///     获取梯子的ID
	/// </summary>
	string LadderId { get; }
}
