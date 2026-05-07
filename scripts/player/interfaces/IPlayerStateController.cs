namespace GFrameworkGodotTemplate.scripts.player.interfaces;

/// <summary>
///     玩家状态控制器接口
///     定义游戏全局状态感知和输入控制权的标准契约
///     负责判断当前是否允许玩家进行操作
/// </summary>
public interface IPlayerStateController
{
	/// <summary>
	///     检测当前是否允许玩家输入
	///     基于游戏全局状态(如PlayingState)决定输入是否生效
	/// </summary>
	bool IsInputEnabled { get; }

	/// <summary>
	///     初始化状态控制器
	///     在节点Ready时调用以获取必要的系统服务引用
	/// </summary>
	void Initialize();

	/// <summary>
	///     更新状态检测
	///     每帧调用以刷新状态缓存
	/// </summary>
	void UpdateState();
}
