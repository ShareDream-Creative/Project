namespace GFrameworkGodotTemplate.scripts.enums.scene;

/// <summary>
///     定义游戏场景的键值枚举
///     用于标识和管理不同的游戏场景
/// </summary>
public enum SceneKey
{
	/// <summary>
	///     游戏启动场景
	///     通常用于初始化游戏环境、加载基础资源等操作
	/// </summary>
	Boot,

	/// <summary>
	///     主游戏场景
	///     核心游戏逻辑和主要界面显示的场景
	/// </summary>
	Main,
	Scene1,
	Scene2,
	Home,

	/// <summary>
	///     游戏测试场景
	///     用于游戏功能测试和开发的实验性场景
	/// </summary>
	GameTest,

	/// <summary>
	///     关卡准备场景
	///     显示关卡信息、卡牌选择等准备界面
	///     用户可在此选择开始构建或退回上一级菜单
	/// </summary>
	LevelPerpare,

	/// <summary>
	///     关卡游戏场景
	///     实际游戏进行中的主场景
	///     包含构建UI和游戏UI的切换管理
	/// </summary>
	LevelPlay,

	/// <summary>
	///     第一关游戏场景
	///     对应 GameLevel.Level1 枚举值
	///     场景文件: res://scenes/level/Level_1/level_play.tscn
	/// </summary>
	Level1,

	/// <summary>
	///     第二关游戏场景
	///     对应 GameLevel.Level2 枚举值
	///     场景文件: res://scenes/level/Level_2/Level_2.tscn
	/// </summary>
	Level2,

	/// <summary>
	///     第三关游戏场景
	///     对应 GameLevel.Level3 枚举值
	///     场景文件: res://scenes/level/Level_3/Level_3.tscn
	/// </summary>
	Level3,

	/// <summary>
	///     第四关游戏场景
	///     对应 GameLevel.Level4 枚举值
	///     场景文件: res://scenes/level/level_4/Level_4.tscn
	/// </summary>
	Level4,

	/// <summary>
	///     第五关游戏场景
	///     对应 GameLevel.Level5 枚举值
	///     场景文件: res://scenes/level/level_5/Level_5.tscn
	/// </summary>
	Level5,

	/// <summary>
	///     关卡选择场景
	///     显示所有可用关卡供玩家选择
	///     从主菜单的"关卡"按钮进入
	/// </summary>
	LevelChoose,

	/// <summary>
	///     关卡选择底层场景
	///     作为关卡选择UI的底层游戏场景
	/// </summary>
	Choose,

	/// <summary>
	///     关卡结束场景
	///     显示关卡结算、商店购买等界面
	///     从LevelSuccessUi的"下一步"按钮进入
	///     场景文件: res://scenes/level/level_end.tscn
	/// </summary>
	LevelEnd
}
