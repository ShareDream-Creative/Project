using GFramework.Godot.Scene;
using GFrameworkGodotTemplate.scripts.core.scene;
using GFrameworkGodotTemplate.scripts.enums.scene;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level.controllers;

/// <summary>
///     关卡结束场景控制器
///     <para>
///         作为关卡结算界面的底层场景
///         负责管理LevelEndUi的生命周期
///     </para>
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-12</date>
///     <description>
///         功能特性:
///         - 提供关卡结束场景的基础功能
///         - 实现ISimpleScene接口以支持场景路由系统
///         - 作为LevelEndUi的容器和背景场景
///         
///         场景结构:
///         ┌─────────────────────────────────────┐
///         │          LevelEnd (Node2D)           │
///         │  └─ LevelEndUi (Control)             │
///         │     ├─ PurchaseButton                │
///         │     ├─ NextLevelButton               │
///         │     └─ ExitButton                    │
///         └─────────────────────────────────────┘
///         
///         使用场景:
///         - 从LevelSuccessUi的"下一步"按钮进入
///         - 显示关卡结算、商店购买等界面
///         - 可选择继续下一关或返回主菜单
///         
///         Godot配置要求:
///         1. 将此脚本挂载到 level_end.tscn 的根节点
///         2. 确保根节点类型为 Node2D
///         3. 子节点包含 LevelEndUi 实例
///     </description>
/// </summary>
[ContextAware]
[Log]
public partial class LevelEnd : Node2D, ISimpleScene
{
	#region 常量定义

	/// <summary>场景键值字符串</summary>
	public static string SceneKeyStr => nameof(SceneKey.LevelEnd);

	#endregion

	#region 生命周期方法

	/// <summary>节点就绪时调用</summary>
	public override void _Ready()
	{
		_log.Info("[LevelEnd] ══════════ 初始化关卡结束场景 ══════════");
		_log.Info($"[LevelEnd] 场景标识: {SceneKeyStr}");
		
		_log.Info("[LevelEnd] ✓✓✓ LevelEnd 初始化完成");
	}

	#endregion

	#region ISimpleScene 接口实现

	/// <summary>场景加载完成回调</summary>
	public ValueTask OnLoadAsync(ISceneEnterParam? param)
	{
		_log.Info($"[LevelEnd] 场景开始加载, 参数: {param?.GetType().Name ?? "无"}");
		return ValueTask.CompletedTask;
	}

	/// <summary>场景激活回调</summary>
	public ValueTask OnEnterAsync(ISceneEnterParam? param)
	{
		_log.Info("[LevelEnd] 场景已激活并可见");
		return ValueTask.CompletedTask;
	}

	/// <summary>场景退出回调</summary>
	public ValueTask OnExitAsync()
	{
		_log.Info("[LevelEnd] 场景正在退出...");
		return ValueTask.CompletedTask;
	}

	/// <summary>场景卸载回调</summary>
	public ValueTask OnUnloadAsync()
	{
		_log.Info("[LevelEnd] 场景已卸载");
		return ValueTask.CompletedTask;
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
		return SceneBehaviorFactory.Create<Node2D>(this, SceneKeyStr);
	}

	#endregion
}

