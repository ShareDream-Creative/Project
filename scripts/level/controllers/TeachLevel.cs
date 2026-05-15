using GFramework.Godot.Scene;
using GFrameworkGodotTemplate.scripts.core.scene;
using GFrameworkGodotTemplate.scripts.core.ui;
using GFrameworkGodotTemplate.scripts.enums.scene;
using GFrameworkGodotTemplate.scripts.enums;
using GFrameworkGodotTemplate.scripts.level.controllers;
using GFrameworkGodotTemplate.scripts.trap;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level.controllers;

/// <summary>
///     教程关卡场景控制器
///     继承自 BaseLevelController，为新游戏（NewGame）流程提供教学引导
///     
///     特殊设计:
///     ┌─────────────────────────────────────────────────┐
///     │              BaseLevelController (基类)          │
///     │  ├─ 完整的关卡生命周期管理                       │
///     │  ├─ 玩家生成、UI状态机、输入控制                 │
///     │  ├─ 终点检测、暂停系统                           │
///     │  └─ 虚方法扩展点                                │
///     ├─────────────────────────────────────────────────┤
///     │              TeachLevel (本类)                   │
///     │  ├─ 场景标识 (SceneKey.TeachLevel)               │
///     │  ├─ 教程特定逻辑                                │
///     │  └─ 胜利后重置GameLevel为None ★核心特性★        │
///     └─────────────────────────────────────────────────┘
///     
///     核心特性 - 胜利后自动重置:
///     当玩家完成教程关卡后:
///     1. OnGameCompleted() 被调用
///     2. 自动将 LevelChoose.CurrentGameLevel 设为 GameLevel.None (0)
///     3. 在 LevelEndUi 中点击"下一关"时:
///        - GetNextLevel() 返回 null (因为当前是None)
///        - 触发 HandleNoNextLevelAvailable()
///        - 自动返回关卡选择界面
///        - 玩家可以选择正式第一关(Level1)
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
///     4. 基类自动处理所有核心流程
/// </summary>
[ContextAware]
[Log]
public partial class TeachLevel : BaseLevelController
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
	public static string SceneKeyStr => nameof(SceneKey.TeachLevel);

	#endregion

	#region 生命周期方法

	/// <summary>
	///     节点准备就绪回调
	///     在 _EnterAsync() 之前调用，用于初始化本地资源
	/// </summary>
	public override void _Ready()
	{
		_log.Info("[TeachLevel] ══════════ 教程关卡初始化 ══════════");
		_log.Info($"[TeachLevel] 场景路径: {SceneFilePath ?? "未知"}");
		_log.Info($"[TeachLevel] 关卡标识: {SceneKeyStr}");
		_log.Info("[TeachLevel] 类型: 独立教程关卡 (NewGame入口)");
		
		base._Ready();
		
		InitializeTrapSystem();
		
		_log.Info("[TeachLevel] ✓✓✓ TeachLevel 初始化完成");
		_log.Info("[TeachLevel] 等待 BaseLevelController.OnEnterAsync() 触发完整流程...");
	}

	/// <summary>
	///     节点退出树时清理资源
	/// </summary>
	public override void _ExitTree()
	{
		_log.Info("[TeachLevel] 正在清理教程关卡特有资源...");
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

	#region 重写核心流程 - 跳过BuildUi直接进入Play阶段

	/// <summary>
	///     场景进入完成回调 ★教程关卡特殊逻辑★
	///     <para>
	///         隐藏基类的 OnEnterAsync()，实现教程关卡的特殊流程：
	///         1. 调用基类完成标准初始化（包括显示 BuildUi）
	///         2. 立即调用 OnBuildFinished() 跳过构建阶段
	///         3. 自动进入 Play（游玩）阶段
	///         
	///         设计原理:
	///         教程关卡是新玩家的第一次游戏体验，
	///         不需要"构建→游玩"的两阶段流程，
	///         应该直接让玩家开始移动和操作。
	///         
	///         与基类的差异:
	///         基类: 初始化服务 → 生成玩家 → 显示BuildUi → [等待玩家点击"完成"] → Play
	///         本类: 初始化服务 → 生成玩家 → 显示BuildUi → [自动点击"完成"] → Play ★
	///     </para>
	/// </summary>
	public new async ValueTask OnEnterAsync()
	{
		_log.Info("════════════ 教程关卡场景进入事件触发 ═══════════");
		_log.Info($"[TeachLevel] 当前时间: {Time.GetTimeStringFromSystem()}");
		_log.Info($"[TeachLevel] 场景路径: {SceneFilePath ?? "未知"}");
		_log.Info("[TeachLevel] ⭐ 特殊模式: 自动跳过构建界面");

		try
		{
			_log.Info("[TeachLevel] 步骤1/2: 调用基类标准初始化流程...");
			await base.OnEnterAsync();
			
			_log.Info("[TeachLevel] 步骤2/2: ★自动跳过BuildUi，进入Play阶段★");
			
			OnBuildFinished();
			
			_log.Info("════════════ ✅ 教程关卡初始化完成 ═══════════");
			_log.Info("[TeachLevel] 🎮 教程关卡已就绪，玩家可以开始移动！");
			_log.Info("[TeachLevel] 提示: 到达终点区域即可完成教程");
		}
		catch (Exception ex)
		{
			_log.Error($"[TeachLevel] ❌ 教程关卡初始化失败: {ex.Message}");
			_log.Error($"[TeachLevel] 异常类型: {ex.GetType().FullName}");
			_log.Error($"[TeachLevel] 堆栈跟踪:\n{ex.StackTrace}");
			throw;
		}
	}

	#endregion

	#region 重写扩展点 - 教程关卡特有逻辑

	/// <summary>
	///     玩家生成后的自定义逻辑
	///     可在此处添加教程特定的玩家初始化逻辑
	///     例如: 显示教学提示、设置初始动画等
	/// </summary>
	/// <param name="player">生成的玩家节点实例</param>
	protected override void OnPlayerSpawned(Node2D player)
	{
		_log.Info("[TeachLevel] 🎓 玩家已生成到教程关卡中");
		_log.Debug($"[TeachLevel] 玩家名称: {player.Name}");
		_log.Debug($"[TeachLevel] 玩家位置: {player.GlobalPosition}");
		
		base.OnPlayerSpawned(player);
	}

	/// <summary>
	///     UI阶段切换时的自定义逻辑
	///     可在此处添加教程特定的阶段转换效果
	///     例如: 阶段性显示教学提示文字
	/// </summary>
	/// <param name="oldPhase">旧阶段</param>
	/// <param name="newPhase">新阶段</param>
	protected override void OnPhaseChanged(LevelPhase oldPhase, LevelPhase newPhase)
	{
		_log.Info($"[TeachLevel] 📋 阶段切换: {oldPhase} → {newPhase}");
		
		switch (newPhase)
		{
			case LevelPhase.Build:
				_log.Info("[TeachLevel] 🔨 进入构建阶段 - 请布置你的防御");
				break;
				
			case LevelPhase.Play:
				_log.Info("[TeachLevel] 🎮 进入游玩阶段 - 开始移动到终点！");
				break;
				
			case LevelPhase.Success:
				_log.Info("[TeachLevel] 🎉 进入成功阶段 - 教程完成！");
				break;
		}
		
		base.OnPhaseChanged(oldPhase, newPhase);
	}

	/// <summary>
	///     游戏完成时的自定义逻辑 ★核心特性★
	///     <para>
	///         重写此方法以实现教程关卡的特殊胜利逻辑：
	///         1. 将 GameLevel 枚举重置为 None (值=0)
	///         2. 这样在 LevelEndUi 中点击"下一关"时会：
	///            - GetNextLevel() 返回 null (因为当前是None)
	///            - 触发 HandleNoNextLevelAvailable()
	///            - 返回关卡选择界面
	///            - 玩家可以选择正式第一关
	///     </para>
	/// </summary>
	protected override void OnGameCompleted()
	{
		_log.Info("🎊 [TeachLevel] ═══════════════════════════");
		_log.Info("🎊 [TeachLevel] 🏆 恭喜！教程关卡完成！");
		_log.Info("🎊 [TeachLevel] ═══════════════════════════");
		
		_log.Info("[TeachLevel] ⭐ 执行教程关卡特殊胜利逻辑...");
		_log.Info("[TeachLevel] → 将 GameLevel 重置为 None (0)");
		
		LevelChoose.SetCurrentGameLevel(GameLevel.None);
		
		_log.Info("[TeachLevel] ✓ GameLevel 已重置为 None");
		_log.Info("[TeachLevel] → 后续在 LevelEndUi 点击'下一关'将:");
		_log.Info("[TeachLevel]   1. GetNextLevel() 返回 null");
		_log.Info("[TeachLevel]   2. 触发无下一关处理流程");
		_log.Info("[TeachLevel]   3. 返回关卡选择界面");
		_log.Info("[TeachLevel]   4. 玩家可选择正式第一关 (Level1)");
		
		base.OnGameCompleted();
	}

	#endregion

	#region 私有方法 - 陷阱系统初始化

	/// <summary>
	///     初始化陷阱系统
	///     查找场景中的 TrapStatic 节点并连接信号
	/// </summary>
	private void InitializeTrapSystem()
	{
		try
		{
			var trapNodes = FindTrapStaticNodes();
			
			if (trapNodes.Count == 0)
			{
				_log.Warn("[TeachLevel] ⚠️ 未找到任何 TrapStatic 节点");
				_log.Warn("[TeachLevel] 陷阱功能将不可用（这不影响正常游戏流程）");
				return;
			}
			
			_log.Info($"[TeachLevel] 找到 {trapNodes.Count} 个陷阱节点");
			
			foreach (var trap in trapNodes)
			{
				if (trap is TrapStatic trapStatic)
				{
					trapStatic.TrapTriggered += OnTrapTriggered;
					_log.Info($"[TeachLevel] ✓ 已连接陷阱信号: {trap.Name}");
				}
			}
			
			_log.Info("[TeachLevel] ✅ 陷阱系统初始化完成");
		}
		catch (Exception ex)
		{
			_log.Error($"[TeachLevel] ❌ 初始化陷阱系统异常: {ex.Message}");
			_log.Error($"[TeachLevel] 异常类型: {ex.GetType().FullName}");
		}
	}

	/// <summary>查找场景中所有 TrapStatic 节点</summary>
	private System.Collections.Generic.List<Node> FindTrapStaticNodes()
	{
		var traps = new System.Collections.Generic.List<Node>();
		
		try
		{
			var groundNode = GetNodeOrNull<Node2D>("ground");
			if (groundNode != null)
			{
				foreach (Node child in groundNode.GetChildren())
				{
					if (child is TrapStatic || 
					    (child.GetScript().Obj != null && 
					     child.GetScript().As<CSharpScript>()?.ResourcePath?.Contains("TrapStatic") == true))
					{
						traps.Add(child);
					}
				}
			}
			
			foreach (Node child in GetChildren())
			{
				if (child is TrapStatic ||
				    (child.GetScript().Obj != null &&
				     child.GetScript().As<CSharpScript>()?.ResourcePath?.Contains("TrapStatic") == true))
				{
					traps.Add(child);
				}
			}
		}
		catch (Exception ex)
		{
			_log.Error($"[TeachLevel] ⚠️ 查找陷阱节点异常: {ex.Message}");
		}
		
		return traps;
	}

	/// <summary>
	///     陷阱触发回调
	///     <param name="playerNode">被隐藏的玩家节点</param>
	/// </summary>
	private void OnTrapTriggered(Node playerNode)
	{
		_log.Info("🎯 [TeachLevel] 收到陷阱触发通知！");
		_log.Info("[TeachLevel] → 调用 BaseLevelController.HandleTrapTriggered() 处理重生...");
		
		HandleTrapTriggered(playerNode);
		
		_log.Info("[TeachLevel] ✓ 陷阱触发处理完成，玩家已重置到起点");
	}

	#endregion

	#region 私有方法 - 资源管理

	/// <summary>
	///     清理 TeachLevel 特有的资源
	///     在场景退出时自动调用
	/// </summary>
	private void CleanupResources()
	{
		_log.Debug("[TeachLevel] 正在释放教程关卡特有资源...");
		
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
