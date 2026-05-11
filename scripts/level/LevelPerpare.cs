using GFramework.Godot.Scene;
using GFrameworkGodotTemplate.scripts.core.scene;
using GFrameworkGodotTemplate.scripts.enums.scene;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level;

/// <summary>
///     关卡准备场景控制器
/// </summary>
[ContextAware]
[Log]
public partial class LevelPerpare : Control, IController, ISceneBehaviorProvider, ISimpleScene
{
	#region 私有字段

	private ISceneBehavior? _scene;

	#endregion

	#region 节点引用

	/// <summary>
	///     关卡准备UI子节点引用
	/// </summary>
	private Control LevelPrepareUi => GetNode<Control>("LevelPrepareUi");

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
		InitializeSubUi();
		
		_log.Info("[LevelPerpare] 场景初始化完成");
		_log.Debug($"[LevelPerpare] 子UI状态: {(LevelPrepareUi != null ? "已加载" : "未找到")}");
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
		_scene ??= SceneBehaviorFactory.Create<Control>(this, SceneKeyStr);
		return _scene;
	}

	#endregion

	#region 私有方法 - UI初始化

	/// <summary>
	///     初始化子UI组件
	///     确保LevelPrepareUi正确加载和显示
	/// </summary>
	private void InitializeSubUi()
	{
		if (LevelPrepareUi != null)
		{
			LevelPrepareUi.Show();
			_log.Debug("[LevelPerpare] LevelPrepareUi 子UI已激活");
		}
		else
		{
			_log.Error("[LevelPerpare] 未找到 LevelPrepareUi 子节点，请检查场景配置");
		}
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
		
		if (LevelPrepareUi != null)
		{
			_log.Debug("[LevelPerpare] 释放 LevelPrepareUi 资源");
		}
		
		GC.Collect();
		_log.Info("[LevelPerpare] 资源清理完成");
	}

	#endregion
}
