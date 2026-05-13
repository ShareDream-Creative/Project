using GFramework.Godot.Scene;
using GFrameworkGodotTemplate.scripts.core.scene;
using GFrameworkGodotTemplate.scripts.enums.scene;
using GFrameworkGodotTemplate.scripts.enums;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level.controllers;

/// <summary>
///     Level_1 游戏场景控制器
///     继承自 BaseLevelController，提供完整的关卡游戏流程管理
///     
///     架构设计:
///     ┌─────────────────────────────────────────────────┐
///     │              BaseLevelController (基类)          │
///     │  ├─ 场景初始化 (OnEnterAsync)                   │
///     │  ├─ 玩家生成 (SpawnPlayer)                      │
///     │  ├─ UI状态机 (Build → Play → Success)           │
///     │  ├─ 输入控制 (Build阶段限制输入)                 │
///     │  ├─ 终点检测 (End区域碰撞检测)                   │
///     │  └─ 暂停系统 (ESC暂停菜单)                       │
///     ├─────────────────────────────────────────────────┤
///     │              LevelPlay (本类)                    │
///     │  ├─ 关卡标识 (SceneKey.LevelPlay)                │
///     │  ├─ 场景行为工厂 (ISceneBehavior)               │
///     │  ├─ 关卡特定配置                                │
///     │  └─ 扩展点重写 (可选)                           │
///     └─────────────────────────────────────────────────┘
///     
///     场景节点结构 (level_play.tscn):
///     LevelPlay (Node2D) ← 挂载本脚本
///     ├── Sprite2D (背景/装饰)
///     │   └── TextEdit (显示关卡编号"1")
///     ├── End (Area2D) ← 终点检测区域 (%End)
///     │   └── a (CollisionShape2D)
///     ├── Begin (Node2D) ← 玩家出生点 (%Begin)
///     └── StaticBody2D ← 地面物理体
///         ├── CollisionShape2D
///         └── ColorRect
///     
///     UI加载方式:
///     所有UI通过 UiRouter 动态加载，不包含在场景文件中
///     - Build阶段: LevelBuildUi (由基类自动加载)
///     - Play阶段: LevelPlayUi (由基类自动切换)
///     - Success阶段: LevelSuccessUi (由基类自动加载)
///     
///     使用方式:
///     1. 将此脚本挂载到 level_play.tscn 的根节点
///     2. 确保场景中有 %Begin 和 %End 节点
///     3. 基类会自动处理所有核心流程
///     4. 仅在需要时重写虚方法添加自定义逻辑
/// </summary>
[ContextAware]
[Log]
public partial class LevelPlay : BaseLevelController
{
	#region 私有字段

	/// <summary>场景行为实例</summary>
	private ISceneBehavior? _scene;

	#endregion

	#region 场景标识

	/// <summary>
	///     获取场景键值字符串
	///     用于场景路由系统和行为工厂
	/// </summary>
	public static string SceneKeyStr => nameof(SceneKey.LevelPlay);

	#endregion

	#region 生命周期方法

	/// <summary>
	///     节点准备就绪回调
	///     在 _EnterAsync() 之前调用，用于初始化本地资源
	/// </summary>
	public override void _Ready()
	{
		_log.Info("[LevelPlay] ══════════ 节点初始化 ══════════");
		_log.Info($"[LevelPlay] 场景路径: {SceneFilePath ?? "未知"}");
		_log.Info($"[LevelPlay] 关卡标识: {SceneKeyStr}");
		
		base._Ready();
		
		_log.Info("[LevelPlay] ✓✓✓ LevelPlay 初始化完成");
		_log.Info("[LevelPlay] 等待 BaseLevelController.OnEnterAsync() 触发完整流程...");
	}

	/// <summary>
	///     节点退出树时清理资源
	/// </summary>
	public override void _ExitTree()
	{
		_log.Info("[LevelPlay] 正在清理 LevelPlay 特有资源...");
		CleanupResources();
		base._ExitTree();
	}

	#endregion

	#region 公开API - 场景行为

	/// <summary>
	///     获取场景行为实例
	///     使用工厂模式创建场景行为，确保单例模式
	///     此方法必须实现，用于框架的场景管理系统
	/// </summary>
	/// <returns>ISceneBehavior接口的场景行为实例</returns>
	public override ISceneBehavior GetScene()
	{
		_scene ??= SceneBehaviorFactory.Create<Node2D>(this, SceneKeyStr);
		return _scene;
	}

	#endregion

	#region 重写扩展点 - 关卡特定逻辑

	/// <summary>
	///     玩家生成后的自定义逻辑
	///     可在此处添加关卡特定的玩家初始化逻辑
	///     例如: 设置玩家初始属性、装备、动画等
	/// </summary>
	/// <param name="player">生成的玩家节点实例</param>
	protected override void OnPlayerSpawned(Node2D player)
	{
		_log.Info("[LevelPlay] 🎮 玩家已生成到关卡中");
		_log.Debug($"[LevelPlay] 玩家名称: {player.Name}");
		_log.Debug($"[LevelPlay] 玩家位置: {player.GlobalPosition}");
		
		base.OnPlayerSpawned(player);
	}

	/// <summary>
	///     UI阶段切换时的自定义逻辑
	///     可在此处添加关卡特定的阶段转换效果
	///     例如: 播放过渡动画、触发事件、更新HUD等
	/// </summary>
	/// <param name="oldPhase">旧阶段</param>
	/// <param name="newPhase">新阶段</param>
	protected override void OnPhaseChanged(LevelPhase oldPhase, LevelPhase newPhase)
	{
		_log.Info($"[LevelPlay] 📋 阶段切换: {oldPhase} → {newPhase}");
		
		switch (newPhase)
		{
			case LevelPhase.Build:
				_log.Info("[LevelPlay] 🔨 进入构建阶段 - 等待玩家完成布置");
				break;
				
			case LevelPhase.Play:
				_log.Info("[LevelPlay] 🎮 进入游玩阶段 - 游戏正式开始！");
				break;
				
			case LevelPhase.Success:
				_log.Info("[LevelPlay] 🎉 进入成功阶段 - 关卡完成！");
				break;
		}
		
		base.OnPhaseChanged(oldPhase, newPhase);
	}

	/// <summary>
	///     游戏完成时的自定义逻辑
	///     可在此处添加关卡特定的完成处理逻辑
	///     例如: 计算分数、保存进度、解锁下一关等
	/// </summary>
	protected override void OnGameCompleted()
	{
		_log.Info("🎊 [LevelPlay] ═══════════════════════════");
		_log.Info("🎊 [LevelPlay] 🏆 恭喜！Level_1 关卡完成！");
		_log.Info("🎊 [LevelPlay] ═══════════════════════════");
		
		base.OnGameCompleted();
	}

	#endregion

	#region 私有方法 - 资源管理

	/// <summary>
	///     清理 LevelPlay 特有的资源
	///     在场景退出时自动调用
	/// </summary>
	private void CleanupResources()
	{
		_log.Debug("[LevelPlay] 正在释放 LevelPlay 特有资源...");
		
		if (_scene != null)
		{
			_log.Debug("[LevelPlay] 释放场景行为引用");
			_scene = null;
		}
		
		GC.Collect();
		_log.Info("[LevelPlay] ✓ LevelPlay 资源清理完成");
	}

	#endregion
}

