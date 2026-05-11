using GFramework.Godot.Scene;
using GFrameworkGodotTemplate.scripts.core.scene;
using GFrameworkGodotTemplate.scripts.enums.scene;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level;

/// <summary>
///     关卡选择底层场景控制器
/// </summary>
[ContextAware]
[Log]
public partial class Choose : Node2D, IController, ISceneBehaviorProvider, ISimpleScene
{
    #region 私有字段

    private ISceneBehavior? _scene;

    #endregion

    #region 场景键值

    /// <summary>
    ///     获取场景键值字符串
    /// </summary>
    public static string SceneKeyStr => nameof(SceneKey.Choose);

    #endregion

    #region 生命周期方法

    public override void _Ready()
    {
        _log.Info("[Choose] 场景初始化完成");
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
        _log.Debug("[Choose] 正在清理场景资源...");
        GC.Collect();
        _log.Info("[Choose] 资源清理完成");
    }

    #endregion
}
