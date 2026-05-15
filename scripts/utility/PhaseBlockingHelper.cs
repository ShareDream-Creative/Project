using Godot;
using GFrameworkGodotTemplate.global;
using GFrameworkGodotTemplate.scripts.enums;
using GFrameworkGodotTemplate.scripts.level;
using GFrameworkGodotTemplate.scripts.utility;

namespace GFrameworkGodotTemplate.scripts.utility;

/// <summary>
///     关卡阶段输入阻断辅助工具类
///     <para>
///         提供统一的关卡阶段设置和输入阻断功能
///         封装双重策略（BaseLevelController + GlobalInputController备用）
///         
///         设计目的:
///         - 统一Success/Defeat/Build等阶段的设置逻辑
///         - 提供容错机制（控制器不可用时使用备用方案）
///         - 消除UI组件中的重复代码
///         - 确保全项目阶段管理一致性
///     </para>
/// </summary>
/// <author>AI Assistant</author>
/// <version>1.0.0</version>
/// <date>2026-05-15</date>
/// <description>
///     功能特性:
///     - SetPhaseAndBlockInput(): 设置阶段并阻断输入（双重策略）
///     - 自动日志输出: 详细记录阶段变更过程
///     - 容错处理: 任一策略失败不影响另一策略
///     
///     使用场景:
///     - LevelSuccessUi显示时设置Success阶段
///     - LevelDefeatUi显示时设置Defeat阶段
///     - LevelBuildUi显示时设置Build阶段
///     - 任何需要切换关卡阶段并阻断输入的场景
///     
///     双重策略机制:
///     策略1 - 通过BaseLevelController (首选):
///       - 路径: UI → BaseLevelController.SetCurrentPhase() → GlobalGameplayInputService
///       - 优点: 统一的阶段管理中心，自动同步到全局服务
///       - 缺点: UI必须能找到BaseLevelController（通过Owner或父节点）
///       
///     策略2 - 直接操作GlobalInputController (备用):
///       - 路径: UI → GlobalInputController.GameplayInputService.SetCurrentPhase()
///       - 优点: 不依赖BaseLevelController引用，适用于UI通过Router显示的情况
///       - 缺点: 绕过了BaseLevelController的统一管理
///       
///     选择逻辑:
///     - 优先尝试策略1（如果BaseLevelController可用）
///     - 如果策略1失败或BaseLevelController为null，自动降级到策略2
///     - 两个策略都失败时输出错误日志但不抛异常
/// </description>
/// <remarks>
///     使用示例:
///     <code>
///     // 在LevelSuccessUi._Ready()中调用
///     PhaseBlockingHelper.SetPhaseAndBlockInput(
///         this,
///         LevelPhase.Success,
///         "[LevelSuccessUi]"
///     );
///     
///     // 在LevelDefeatUi._Ready()中调用
///     PhaseBlockingHelper.SetPhaseAndBlockInput(
///         this,
///         LevelPhase.Defeat,
///         "[LevelDefeatUi]"
///     );
///     </code>
///     
///     数据流向图:
///     ┌─────────────────────┐
///     │  UI Component       │
///     │  (调用SetPhase...)   │
///     └─────────┬───────────┘
///               │
///     ┌─────────▼───────────┐
///     │  PhaseBlockingHelper│
///     │  (策略选择与执行)    │
///     └─────────┬───────────┘
///               │
///     ┌─────────▼───────────┐     ┌─────────────────────┐
///     │ Strategy 1:          │     │ Strategy 2:          │
///     │ BaseLevelController  │ OR  │ GlobalInputController│
///     │ .SetCurrentPhase()   │     │ .SetCurrentPhase()   │
///     └─────────┬───────────┘     └─────────┬───────────┘
///               │                           │
///               └───────────┬───────────────┘
///                           │
///                 ┌─────────▼───────────┐
///                 │GlobalGameplayInput  │
///                 │Service              │
///                 │ ._currentPhase = X  │
///                 │ .IsInputEnabled = F  │
///                 └─────────┬───────────┘
///                           │
///                 ┌─────────▼───────────┐
///                 │ Player Module       │
///                 │ (输入被阻断)         │
///                 └─────────────────────┘
///                 
///     性能影响:
///     - 单次调用耗时: <1ms（主要是节点查找和日志输出）
///     - 内存占用: 无（静态方法，无状态）
///     - 建议调用时机: UI._Ready()方法末尾
/// </remarks>
public static class PhaseBlockingHelper
{
	#region 公共方法

	/// <summary>
	///     设置关卡阶段并阻断所有Gameplay输入
	 ///     <param name="uiNode">当前UI节点</param>
	 ///     <param name="targetPhase">目标关卡阶段（Success/Defeat/Build等）</param>
	 ///     <param name="logPrefix">日志前缀标识（如"[LevelSuccessUi]"）</param>
	 ///     <returns>是否成功设置了阶段（任一策略成功即返回true）</returns>
	 ///     <remarks>
	 ///         执行流程:
	 ///         1. 尝试通过BaseLevelController设置阶段（策略1）
	 ///         2. 如果策略1失败，直接操作GlobalInputController（策略2）
	 ///         3. 输出详细的日志记录过程和结果
	 ///         4. 返回是否成功
	 ///         
	 ///         阻断效果:
	 ///         设置非Play阶段后：
	 ///         - HorizontalDirection = 0 (玩家无法移动)
	 ///         - IsJumpPressed = false (玩家无法跳跃)
	 ///         - IsInteractPressed = false (无法交互)
	 ///         - IsInputEnabled = false (全局输入已禁用)
	 ///         
	 ///         错误处理:
	 ///         - uiNode为null: 输出警告并返回false
	 ///         - 两个策略都失败: 输出错误并返回false
	 ///         - 任一策略成功: 返回true
	 ///     </remarks>
	/// </summary>
	public static bool SetPhaseAndBlockInput(Node? uiNode, LevelPhase targetPhase, string logPrefix = "[PhaseBlocking]")
	{
		GD.Print($"{logPrefix} ═══════════ 设置{targetPhase}阶段并阻断输入 ═══════════");
		
		if (uiNode == null)
		{
			GD.Print($"{logPrefix} ⚠️ UI节点为null，无法设置阶段");
			return false;
		}

		bool success = false;

		// 策略1: 通过BaseLevelController设置（首选）
		success |= TrySetPhaseViaController(uiNode, targetPhase, logPrefix);

		// 策略2: 直接操作GlobalInputController（备用）
		if (!success)
		{
			success |= TrySetPhaseDirectly(uiNode, targetPhase, logPrefix);
		}

		if (success)
		{
			PrintBlockingEffect(targetPhase, logPrefix);
		}
		else
		{
			GD.Print($"{logPrefix} ❌ 所有策略均失败，输入可能不会被完全阻断");
		}

		GD.Print($"{logPrefix} ═══════════ 阶段设置完成 ═══════════");
		
		return success;
	}

	#endregion

	#region 私有方法 - 策略实现

	/// <summary>
	///     通过BaseLevelController设置阶段（策略1）
	/// </summary>
	private static bool TrySetPhaseViaController(Node uiNode, LevelPhase targetPhase, string logPrefix)
	{
		try
		{
			var controller = NodeTreeHelper.FindLevelController(uiNode, logPrefix);
			
			if (controller == null)
			{
				GD.Print($"{logPrefix} ⚠️ BaseLevelController不可用，跳过策略1");
				return false;
			}

			var previousPhase = controller.CurrentPhase;
			
			GD.Print($"{logPrefix} 正在通过BaseLevelController切换到 {targetPhase} 阶段...");
			
			controller.SetCurrentPhase(targetPhase);
			
			GD.Print($"{logPrefix} ✓✓✓ 策略1成功！阶段变更: {previousPhase} → {targetPhase}");
			return true;
		}
		catch (Exception ex)
		{
			GD.Print($"{logPrefix} ⚠️ 策略1异常: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	///     直接通过GlobalInputController设置阶段（策略2-备用）
	/// </summary>
	private static bool TrySetPhaseDirectly(Node uiNode, LevelPhase targetPhase, string logPrefix)
	{
		try
		{
			GD.Print($"{logPrefix} 使用备用方案: 直接操作GlobalInputController...");
			
			var inputService = NodeTreeHelper.GetGlobalInputService(uiNode);
			
			if (inputService == null)
			{
				GD.Print($"{logPrefix} ❌ GlobalGameplayInputService不可用");
				return false;
			}

			var previousPhase = inputService.CurrentPhase;
			
			inputService.SetCurrentPhase(targetPhase);
			
			GD.Print($"{logPrefix} ✓✓✓ 备用方案成功！阶段变更: {previousPhase} → {targetPhase} (直接)");
			return true;
		}
		catch (Exception ex)
		{
			GD.Print($"{logPrefix} ❌ 备用方案异常: {ex.Message}");
			return false;
		}
	}

	#endregion

	#region 私有方法 - 日志输出

	/// <summary>
	///     打印阻断效果信息
	/// </summary>
	private static void PrintBlockingEffect(LevelPhase phase, string logPrefix)
	{
		GD.Print($"{logPrefix} 输入阻断效果:");
		GD.Print($"{logPrefix}   • 当前阶段: {phase}");
		GD.Print($"{logPrefix}   • HorizontalDirection = 0 (玩家无法移动)");
		GD.Print($"{logPrefix}   • IsJumpPressed = false (玩家无法跳跃)");
		GD.Print($"{logPrefix}   • IsInteractPressed = false (无法交互按钮/开关)");
		GD.Print($"{logPrefix}   • IsInputEnabled = false (全局输入已禁用)");
	}

	#endregion
}
