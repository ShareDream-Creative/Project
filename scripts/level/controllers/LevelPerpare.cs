using GFramework.Godot.Scene;
using GFrameworkGodotTemplate.scripts.core.scene;
using GFrameworkGodotTemplate.scripts.enums.scene;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level.controllers;

/// <summary>
///     关卡准备场景控制器
/// </summary>
[ContextAware]
[Log]
public partial class LevelPerpare : Node2D, IController, ISceneBehaviorProvider, ISimpleScene
{
	#region 私有字段

	private ISceneBehavior? _scene;

	#endregion

	#region 场景键值

	/// <summary>
	///     获取场景键值字符串
	/// </summary>
	public static string SceneKeyStr => nameof(SceneKey.LevelPerpare);

	#endregion

	#region 生命周期方法

	public override void _Ready()
	{
		_log.Info("[LevelPerpare] 场景初始化完成");
		_log.Info($"[LevelPerpare] 当前关卡: {LevelChoose.CurrentGameLevel}");
		_log.Debug("[LevelPerpare] → UI层由UiRouter独立管理，本场景仅负责场景层逻辑");
	}

	public override void _ExitTree()
	{
		CleanupResources();
	}

	#endregion

	#region 公开API - 场景行为

	/// <summary>
	///     获取场景行为实例
	///     使用工厂模式创建场景行为，确保单例模式
	/// </summary>
	/// <returns>ISceneBehavior接口的场景行为实例</returns>
	public ISceneBehavior GetScene()
	{
		_scene ??= SceneBehaviorFactory.Create<Node2D>(this, SceneKeyStr);
		return _scene;
	}

	#endregion

	#region 私有方法 - 资源管理

	/// <summary>
	///     清理资源
	 ///     在场景退出时调用，确保正确释放资源
	 /// </summary>
	private void CleanupResources()
	{
		_log.Debug("[LevelPerpare] 正在清理场景资源...");
		
		_log.Debug("[LevelPerpare] 场景资源已释放");
		GC.Collect();
		_log.Info("[LevelPerpare] 资源清理完成");
	}

	#endregion
}
