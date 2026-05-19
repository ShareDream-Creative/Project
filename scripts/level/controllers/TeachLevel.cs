using GFrameworkGodotTemplate.scripts.core.scene;
using GFrameworkGodotTemplate.scripts.enums.scene;
using GFrameworkGodotTemplate.scripts.events.poker;
using Godot;

namespace GFrameworkGodotTemplate.scripts.level.controllers;

/// <summary>
///     教程关卡控制器 v10.0 - 极简版（基于全局缓存方案）
///     <para>
///         设计理念:
///         - 完全依赖 BaseLevelController 的标准流程
///         - 数据由 MainMenu 在场景切换前预填充到全局缓存
///         - LevelBuildUi 创建时自动读取缓存并显示卡牌
///         - TeachLevel 只负责行为标志配置和游戏完成后的清理
///         
///         核心改进 (v10.0):
///         ✅ 移除所有异步配置逻辑（不再需要）
///         ✅ 完全复用 ContinueGame 的数据流
///         ✅ 代码量减少80%（从440行→100行）
///         
///         完整数据流:
///         MainMenu.NewGameCoroutine()
///           ├─ PreFillTutorialCache() → ClassifiedPokerEvents.LastItemData = 教程数据
///           └─ sceneRouter.ReplaceAsync(TeachLevel)
///               └─ TeachLevel._Ready()
///                   └─ base._Ready() (BaseLevelController)
///                       └─ BaseLevelController.OnEnterAsync() [9步]
///                           └─ 步骤7: ShowBuildUiAsync()
///                               └─ LevelBuildUi._Ready()
///                                   └─ FindPokerHandAndReceiveData()
///                                       └─ if (LastItemData != null) ✅
///                                           → PokerHand.ReceiveItemData()
///                                           → 显示2张platform卡牌
///     </para>
/// </summary>
[ContextAware]
[Log]
public partial class TeachLevel : BaseLevelController
{
	#region ==================== 常量 ====================

	/// <summary>场景标识</summary>
	public static string SceneKeyStr => nameof(SceneKey.TeachLevel);

	#endregion

	#region ==================== 构造函数 ====================

	/// <summary>
	///     教程关卡构造函数 - 配置行为标志
	/// </summary>
	public TeachLevel()
	{
		SkipBuildPhase = false;          // 保留构建阶段，玩家可使用2个平台
		ResetGameLevelOnVictory = true;   // 胜利后重置关卡选择
		EnableTrapSystem = true;          // 启用陷阱系统
	}

	#endregion

	#region ==================== 生命周期重写 ====================

	/// <summary>
	///     节点准备就绪回调
	/// </summary>
	public override void _Ready()
	{
		base._Ready();

		_log.Info("════════════ [TeachLevel v10.0] 初始化 ═══════════");
		_log.Info($"[TeachLevel] 场景路径: {SceneFilePath ?? "未知"}");
		_log.Info("[TeachLevel] 类型: 教程关卡 (基于BaseLevelController + 全局缓存方案)");
		_log.Info("[TeachLevel] 配置:");
		_log.Info($"[TeachLevel]   • SkipBuildPhase = {SkipBuildPhase}");
		_log.Info($"[TeachLevel]   • ResetGameLevelOnVictory = {ResetGameLevelOnVictory}");
		_log.Info("[TeachLevel]   • EnableTrapSystem = {EnableTrapSystem}");
		_log.Info("[TeachLevel]   • 数据来源: MainMenu预填充的全局缓存");
	}

	#endregion

	#region ==================== 虚方法重写 ====================

	/// <summary>
	///     重写游戏完成时的处理 - 清理教程数据缓存
	///     <para>
	 ///        当玩家完成教程后，清理全局缓存中的教程数据，
	 ///        防止影响后续进入的其他关卡。
	 ///     </para>
	/// </summary>
	protected override void OnGameCompleted()
	{
		base.OnGameCompleted();
		
		_log.Info("[TeachLevel] 🎉 教程关卡完成！");
		_log.Info("[TeachLevel] 正在清理教程数据缓存...");

		CleanupTutorialCache();
	}

	#endregion

	#region ==================== 私有方法 ====================

	/// <summary>
	///     清理教程数据缓存
	///     <para>
	 ///        将 ClassifiedPokerEvents 的全局缓存清空，
	 ///        确保不会影响其他关卡的数据读取。
	 ///     </para>
	/// </summary>
	private void CleanupTutorialCache()
	{
		try
		{
			var itemProp = typeof(ClassifiedPokerEvents).GetProperty("LastItemData");
			if (itemProp != null && itemProp.GetValue(null) != null)
			{
				itemProp.SetValue(null, null);
				_log.Info("[TeachLevel] ✓ 已清理 ItemPokerData 缓存");
			}

			var actionProp = typeof(ClassifiedPokerEvents).GetProperty("LastActionData");
			if (actionProp != null && actionProp.GetValue(null) != null)
			{
				actionProp.SetValue(null, null);
				_log.Info("[TeachLevel] ✓ 已清理 ActionPokerData 缓存");
			}
		}
		catch (Exception ex)
		{
			_log.Debug($"[TeachLevel] 缓存清理警告: {ex.Message}");
		}
	}

	#endregion
}
