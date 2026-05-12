using GFramework.Godot.Scene;
using GFrameworkGodotTemplate.scripts.enums.scene;
using GFrameworkGodotTemplate.scripts.level;
using Godot;

namespace GFrameworkGodotTemplate.scripts.tests;

/// <summary>
///     游戏测试关卡控制器
///     继承自BaseLevelController，专门用于gametest.tscn场景
///     
///     功能特性:
///     - 自动在Begin位置生成玩家角色
///     - 显示BuildUI→PlayUI→SuccessUI的完整流程
///     - 构建阶段禁用键盘输入（除ESC）
///     - 检测玩家到达End区域并显示成功界面
///     - ESC键打开暂停菜单
/// </summary>
[ContextAware]
[Log]
public partial class GameTestLevelController : BaseLevelController
{
	/// <summary>场景键值字符串</summary>
	public static string SceneKeyStr => nameof(SceneKey.GameTest);

	/// <summary>获取场景行为实例（重写基类）</summary>
	public override ISceneBehavior GetScene()
	{
		_scene ??= SceneBehaviorFactory.Create<Node2D>(this, SceneKeyStr);
		return _scene;
	}

	/// <summary>
	///     玩家生成后的自定义逻辑
	///     可在此添加游戏测试特有的初始化代码
	/// </summary>
	protected override void OnPlayerSpawned(Node2D player)
	{
		base.OnPlayerSpawned(player);
		_log.Info("[GameTestLevelController] 游戏测试场景玩家已就绪");
	}

	/// <summary>
	///     阶段切换时的自定义逻辑
	///     可在此添加游戏测试特有的阶段处理代码
	/// </summary>
	protected override void OnPhaseChanged(LevelPhase oldPhase, LevelPhase newPhase)
	{
		base.OnPhaseChanged(oldPhase, newPhase);
		
		if (newPhase == LevelPhase.Play)
		{
			_log.Info("[GameTestLevelController] 🎮 游戏开始！玩家可以自由移动了！");
		}
		else if (newPhase == LevelPhase.Success)
		{
			_log.Info("[GameTestLevelController] 🏆 恭喜！你完成了游戏测试关卡！");
		}
	}

	/// <summary>
	///     游戏完成时的自定义逻辑
	///     可在此添加游戏测试特有的结束处理代码
	/// </summary>
	protected override void OnGameCompleted()
	{
		base.OnGameCompleted();
		_log.Info("[GameTestLevelController] 游戏测试关卡完成，等待用户操作...");
	}
}
