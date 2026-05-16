using GFramework.Godot.Scene;
using GFrameworkGodotTemplate.scripts.core.scene;
using GFrameworkGodotTemplate.scripts.enums.scene;
using GFrameworkGodotTemplate.scripts.level.controllers;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level.controllers;

/// <summary>
///     教程关卡场景控制器
///     继承自 BaseLevelController，为新游戏（NewGame）流程提供教学引导
///     
///     设计说明（v3.0 - 精简版）:
///     ┌─────────────────────────────────────────────────┐
///     │              BaseLevelController (基类)          │
///     │  ├─ 完整的关卡生命周期管理                       │
///     │  ├─ 可配置行为标志:                              │
///     │  │   ├─ SkipBuildPhase (跳过构建阶段)           │
///     │  │   ├─ ResetGameLevelOnVictory (胜利重置)      │
///     │  │   └─ EnableTrapSystem (陷阱系统)             │
///     │  ├─ 陷阱系统初始化和处理                         │
///     │  └─ 虚方法扩展点                                │
///     ├─────────────────────────────────────────────────┤
///     │              TeachLevel (本类)                   │
///     │  ├─ 场景标识 (SceneKey.TeachLevel)               │
///     │  ├─ 配置三个行为标志为 true                      │
///     │  └─ 最小化代码，仅保留必要逻辑                   │
///     └─────────────────────────────────────────────────┘
///     
///     核心特性 - 配置驱动的教程行为:
///     通过设置基类的三个配置属性，实现教程关卡的完整功能：
///     
///     1. SkipBuildPhase = true
///        → OnEnterAsync() 自动调用 OnBuildFinished()
///        → 跳过构建界面，直接进入游玩阶段
///        
///     2. ResetGameLevelOnVictory = true  
///        → OnGameCompleted() 自动将 GameLevel 设为 None
///        → LevelEndUi 的"下一关"返回选关界面
///        
///     3. EnableTrapSystem = true
///        → 自动扫描并连接场景中的 TrapStatic 节点
///        → 处理玩家隐藏/重置机制
///     
///     场景节点结构 (Teach_Level.tscn):
///     TeachLevel (Node2D) ← 挂载本脚本
///     ├── End (Area2D) ← 终点检测区域 (%End)
///     │   └── collion (CollisionShape2D)
///     ├── Begin (Node2D) ← 玩家出生点 (%Begin)
///     ├── ground (Node2D)
///     │   ├── PlateDefault (平台实例)
///     │   ├── PlateDefault2 (平台实例)
///     │   ├── PlateDefault3 (平台实例)
///     │   └── TrapStatic (陷阱实例)
///     └── Prohibit (禁止区域实例)
///     
///     使用方式:
///     1. 将此脚本挂载到 Teach_Level.tscn 的根节点
///     2. 确保场景中有 %Begin 和 %End 节点
///     3. 从主菜单"新游戏"按钮进入
///     4. 基类自动处理所有核心流程（包括教程特殊逻辑）
/// </summary>
[ContextAware]
[Log]
public partial class TeachLevel : BaseLevelController
{
	#region 私有字段

	/// <summary>场景行为实例</summary>
	private ISceneBehavior? _scene;

	#endregion

	#region 构造函数 - 配置教程关卡特殊行为

	/// <summary>
	///     构造函数中配置基类的行为标志
	///     <para>
	///         在构造时即设置三个关键属性，使基类自动执行教程逻辑
	///         这样无需重写任何方法，即可获得完整的教程关卡行为
	 ///     </para>
	/// </summary>
	public TeachLevel()
	{
		SkipBuildPhase = true;
		ResetGameLevelOnVictory = true;
		EnableTrapSystem = true;
	}

	#endregion

	#region 场景标识

	/// <summary>
	///     获取场景键值字符串
	///     用于场景路由系统和行为工厂
	/// </summary>
	public static string SceneKeyStr => nameof(SceneKey.TeachLevel);

	#endregion

	#region 生命周期方法

	/// <summary>
	///     节点准备就绪回调
	///     输出初始化日志，其余由基类处理
	/// </summary>
	public override void _Ready()
	{
		_log.Info("[TeachLevel] ══════════ 教程关卡初始化 ══════════");
		_log.Info($"[TeachLevel] 场景路径: {SceneFilePath ?? "未知"}");
		_log.Info($"[TeachLevel] 关卡标识: {SceneKeyStr}");
		_log.Info("[TeachLevel] 类型: 教程关卡 (NewGame入口)");
		_log.Info("[TeachLevel] 已启用配置:");
		_log.Info($"[TeachLevel]   ✓ SkipBuildPhase = {SkipBuildPhase} (跳过构建阶段)");
		_log.Info($"[TeachLevel]   ✓ ResetGameLevelOnVictory = {ResetGameLevelOnVictory} (胜利后重置)");
		_log.Info($"[TeachLevel]   ✓ EnableTrapSystem = {EnableTrapSystem} (陷阱系统)");
		
		base._Ready();
		
		_log.Info("[TeachLevel] ✓✓✓ TeachLevel 初始化完成");
	}

	/// <summary>
	///     节点退出树时清理资源
	/// </summary>
	public override void _ExitTree()
	{
		_log.Info("[TeachLevel] 正在清理教程关卡资源...");
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

	#region 私有方法 - 资源管理

	/// <summary>
	///     清理 TeachLevel 特有的资源
	///     在场景退出时自动调用
	/// </summary>
	private void CleanupResources()
	{
		_log.Debug("[TeachLevel] 正在释放教程关卡资源...");
		
		if (_scene != null)
		{
			_log.Debug("[TeachLevel] 释放场景行为引用");
			_scene = null;
		}
		
		GC.Collect();
		_log.Info("[TeachLevel] ✓ TeachLevel 资源清理完成");
	}

	#endregion
}
