using Godot;
using GFrameworkGodotTemplate.scripts.enums;

namespace GFrameworkGodotTemplate.scripts.level.config;

/// <summary>
///     关卡控制器数据配置类
///     <para>
///         从 BaseLevelController 中提取的数据相关逻辑，
///         集中管理关卡控制器的状态、配置和引用信息。
///         
///         设计原理:
///         - 数据与行为分离 (SRP)
///         - 便于序列化和持久化
///         - 支持运行时动态修改配置
///     </para>
///     
///     <author>AI Assistant</author>
///     <version>1.0.0</version>
///     <date>2026-05-16</date>
///     
///     <description>
///         职责范围:
///         - 关卡阶段状态管理 (Build/Play/Success/Defeat)
///         - 游戏完成状态跟踪
///         - 配置标志 (SkipBuildPhase/ResetGameLevelOnVictory/EnableTrapSystem)
///         - 静态阶段标志同步
///         - 场景节点引用缓存
///         - 组件接口实例持有
///         
///         使用方式:
///         由 BaseLevelController 持有和初始化，
///         通过属性暴露给内部行为类使用。
///     </description>
/// </summary>
public class LevelControllerData
{
	#region 私有字段

	/// <summary>当前关卡阶段状态</summary>
	private LevelPhase _currentPhase = LevelPhase.Build;

	/// <summary>是否已完成游戏</summary>
	private bool _isGameCompleted;

	#endregion

	#region 配置驱动的行为标志

	/// <summary>
	///     是否跳过构建阶段（直接进入游玩阶段）
	///     <para>
	///         当设置为true时，控制器会自动调用 OnBuildFinished()
	///         跳过构建界面，直接进入游玩阶段
	 ///     </para>
	/// </summary>
	public bool SkipBuildPhase { get; set; } = false;

	/// <summary>
	///     胜利时是否重置GameLevel为None
	///     <para>
	///         当设置为true时，胜利后会自动将 GameLevel 设为 None
	 ///     </para>
	/// </summary>
	public bool ResetGameLevelOnVictory { get; set; } = false;

	/// <summary>
	///     是否启用陷阱系统
	///     <para>
	///         当设置为true时，会订阅全局静态陷阱事件
	 ///     </para>
	/// </summary>
	public bool EnableTrapSystem { get; set; } = false;

	#endregion

	#region 阶段状态管理

	/// <summary>获取或设置当前关卡阶段</summary>
	public LevelPhase CurrentPhase
	{
		get => _currentPhase;
		set
		{
			if (_currentPhase == value) return;
			
			var oldPhase = _currentPhase;
			_currentPhase = value;
			
			UpdateStaticFlags(value);
			OnPhaseChanged?.Invoke(oldPhase, value);
		}
	}

	/// <summary>获取是否处于构建阶段</summary>
	public bool IsInBuildPhase => _currentPhase == LevelPhase.Build;

	/// <summary>获取是否处于游玩阶段</summary>
	public bool IsInPlayPhase => _currentPhase == LevelPhase.Play;

	/// <summary>获取是否已完成游戏</summary>
	public bool IsGameCompleted
	{
		get => _isGameCompleted;
		internal set => _isGameCompleted = value;
	}

	#endregion

	#region 静态阶段标志（全局同步）

	/// <summary>静态标志：当前是否处于关卡构建阶段</summary>
	public static bool IsBuildPhaseActive { get; set; }

	/// <summary>静态标志：当前是否处于关卡成功阶段</summary>
	public static bool IsSuccessPhaseActive { get; set; }

	/// <summary>
	///     重置所有静态阶段标志
	/// </summary>
	public static void ResetPhaseFlags()
	{
		IsBuildPhaseActive = false;
		IsSuccessPhaseActive = false;
	}

	/// <summary>
	///     根据阶段更新静态标志
	/// </summary>
	private void UpdateStaticFlags(LevelPhase phase)
	{
		IsBuildPhaseActive = phase == LevelPhase.Build;
		IsSuccessPhaseActive = phase == LevelPhase.Success;
	}

	#endregion

	#region 事件定义

	/// <summary>阶段变化事件</summary>
	public event Action<LevelPhase, LevelPhase>? OnPhaseChanged;

	#endregion

	#region 场景节点引用（由外部设置）

	/// <summary>玩家出生点位置节点</summary>
	public Node2D? BeginPositionNode { get; set; }

	/// <summary>终点碰撞区域</summary>
	public Area2D? EndAreaNode { get; set; }

	/// <summary>失败区域列表</summary>
	public Godot.Collections.Array<Area2D> DefeatAreas { get; } = new();

	#endregion

	#region 组件接口实例（由外部注入）

	/// <summary>UI管理器实例</summary>
	public object? UiManager { get; set; }

	/// <summary>玩家管理器实例</summary>
	public object? PlayerManager { get; set; }

	/// <summary>输入控制器实例</summary>
	public object? InputController { get; set; }

	/// <summary>规则集成器实例</summary>
	public object? RulesIntegration { get; set; }

	#endregion

	#region 公开API

	/// <summary>
	///     标记游戏为已完成状态
	/// </summary>
	public void MarkGameCompleted()
	{
		_isGameCompleted = true;
	}

	/// <summary>
	///     重置游戏完成状态（用于重新开始）
	/// </summary>
	public void ResetGameCompleted()
	{
		_isGameCompleted = false;
	}

	/// <summary>
	///     安全获取 BeginPosition 的全局位置
	///     <returns>位置向量，如果节点无效则返回 Vector2.Zero</returns>
	/// </summary>
	public Vector2 GetBeginPositionOrDefault()
	{
		if (BeginPositionNode != null && GodotObject.IsInstanceValid(BeginPositionNode))
		{
			return BeginPositionNode.GlobalPosition;
		}

		return Vector2.Zero;
	}

	#endregion
}
