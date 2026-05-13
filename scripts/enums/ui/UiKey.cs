namespace GFrameworkGodotTemplate.scripts.enums.ui;

/// <summary>
///     定义用户界面键值枚举，用于标识不同的UI面板或菜单
/// </summary>
public enum UiKey
{
    /// <summary>
    ///     主菜单界面键值
    /// </summary>
    MainMenu,

    /// <summary>
    ///     保存菜单界面键值
    /// </summary>
    SaveMenu,

    /// <summary>
    ///     加载菜单界面键值
    /// </summary>
    /// <remarks>
    ///     表示加载菜单相关的界面状态或操作类型
    /// </remarks>
    LoadMenu,

    /// <summary>
    ///     选项菜单界面键值
    /// </summary>
    /// <remarks>
    ///     表示选项菜单相关的界面状态或操作类型
    /// </remarks>
    OptionsMenu,

    /// <summary>
    ///     版权信息界面键值
    /// </summary>
    /// <remarks>
    ///     表示显示版权信息的界面状态或操作类型
    /// </remarks>
    Credits,
    HomeUi,
    PauseMenu,
    /// <summary>
    ///     关卡选择界面键值
    /// </summary>
    LevelChoose,

    /// <summary>
	///     关卡准备UI界面键值
	///     显示关卡信息、开始构建按钮和退回按钮
	/// </summary>
	LevelPrepareUi,

	/// <summary>
	///     关卡构建UI界面键值
	///     显示在游戏场景加载后的初始界面
	///     包含"完成！"按钮，点击后切换到游玩UI
	///     在此界面显示期间，除ESC外禁止所有键盘输入
	/// </summary>
	LevelBuildUi,

	/// <summary>
	///     关卡游玩UI界面键值
	///     显示在玩家完成构建阶段后的主游戏界面
	///     恢复全部输入控制，允许玩家自由移动和操作
	/// </summary>
	LevelPlayUi,

	/// <summary>
	///     关卡成功UI界面键值
	///     显示在玩家到达终点区域时的胜利界面
	///     包含"下一步"、"再玩一次"、"返回主菜单"按钮
	/// </summary>
	LevelSuccessUi,

	/// <summary>
	///     关卡结束UI界面键值
	///     显示在关卡成功后的结算/商店界面
	///     包含"购买"、"下一关"、"返回主菜单"按钮
	/// </summary>
	LevelEndUi,

	/// <summary>
	///     关卡失败UI界面键值
	///     显示在关卡超时或失败时的失败界面
	///     包含"再玩一次"和"返回主菜单"按钮
	///     在此界面显示期间，禁用键盘/手柄输入，仅允许鼠标操作
	/// </summary>
	LevelDefateUi
}