using GFramework.Game.Abstractions.UI;
using Godot;

namespace GFrameworkGodotTemplate.scripts.utility;

/// <summary>
///     暂停状态管理器
///     <para>
///         负责在游戏暂停前后保存和恢复UI/场景状态
///         确保从暂停菜单返回时能正确回到之前的界面
///         
///         设计目的:
///         - 解决PlayingState硬编码加载HomeUi的bug
///         - 提供统一的暂停状态保存/恢复接口
///         - 支持任意UI界面的暂停/恢复
///     </para>
/// </summary>
/// <author>AI Assistant</author>
/// <version>1.0.0</version>
/// <date>2026-05-15</date>
/// <description>
///     功能特性:
///     - SavePrePauseState(): 保存暂停前的UI键值
///     - GetSavedUiKey(): 获取保存的UI键值
///     - HasSavedState(): 检查是否有保存的状态
///     - ClearSavedState(): 清除保存的状态（恢复后调用）
///     
///     使用场景:
///     - UI打开暂停菜单前: 调用SavePrePauseState()
///     - 关闭暂停菜单后: PlayingState使用GetSavedUiKey()决定恢复到哪个UI
///     
///     线程安全:
///     - 使用静态字段存储，全局唯一
///     - 无需实例化，直接通过类名访问
/// </description>
/// <remarks>
///     数据流:
///     ┌─────────────┐    ┌──────────────────┐    ┌─────────────┐
///     │ LevelChoose │───▶│ PauseStateManager│───▶│ PlayingState│
///     │ /PlayUi    │    │ .SavePrePause()  │    │ .OnEnter   │
///     │ /SuccessUi │    │                  │    │ Async()   │
///     └─────────────┘    └──────────────────┘    └─────────────┘
///                              ↓                      ↓
///                         保存UiKey              读取UiKey
///                         ("LevelChoose")        恢复到LevelChoose
///                         
///     生命周期:
///     1. 用户按ESC → UI调用 SavePrePauseState(currentUiKey)
///     2. 暂停菜单打开
///     3. 用户再按ESC/点继续 → 触发ResumeGameCommand
///     4. ResumeGameCommandHandler → ChangeToAsync<PlayingState>()
///     5. PlayingState.OnEnterAsync() → GetSavedUiKey()
///     6. 如果有保存的UI → 恢复到该UI
///     7. 如果没有保存的UI → 使用默认行为(HomeUi或保持当前)
///     8. ClearSavedState() 清除保存
/// </remarks>
public static class PauseStateManager
{
	#region 私有字段

	/// <summary>暂停前的UI键值</summary>
	private static string? _savedPrePauseUiKey;

	#endregion

	#region 公共方法

	/// <summary>
	///     保存暂停前的UI状态
	 ///     <param name="uiKey">当前显示的UI键值（如 UiKey.LevelChoose, UiKey.LevelPlayUi 等）</param>
	 ///     <remarks>
	 ///     应该在打开暂停菜单之前调用此方法
	 ///     用于记录用户在哪个界面触发的暂停
	 ///     
	 ///     调用时机示例:
	 ///     <code>
	 ///     // 在LevelChoose._Input()中
	 ///     if (@event.IsActionPressed("ui_cancel"))
	 ///     {
	 ///         PauseStateManager.SavePrePauseState(UiKeyStr);  // 保存当前UI
	 ///         this.SendCommand(new PauseGameWithOpenPauseMenuCommand(...));
	 ///     }
	 ///     </code>
	 ///     
	 ///     覆盖语义:
	 ///     多次调用会覆盖之前的值（以最后一次为准）
	 ///     这是符合预期的，因为只关心"最近一次暂停前的界面"
	 ///     </remarks>
	/// </summary>
	public static void SavePrePauseState(string uiKey)
	{
		_savedPrePauseUiKey = uiKey;
		GD.Print($"[PauseStateManager] 💾 已保存暂停前UI状态: {uiKey}");
	}

	/// <summary>
	///     获取保存的暂停前UI键值
	 ///     <returns>保存的UI键值，如果没有保存则返回null</returns>
	 ///     <remarks>
	 ///     使用场景:
	 ///     PlayingState.OnEnterAsync()中调用
	 ///     用于决定恢复到哪个界面
	 ///     
	 ///     返回值处理:
	 ///     - 有值: 恢复到该UI
	 ///     - null: 使用默认行为（可能是HomeUi或保持当前UI）
	 ///     </remarks>
	/// </summary>
	public static string? GetSavedUiKey()
	{
		return _savedPrePauseUiKey;
	}

	/// <summary>
	///     检查是否有保存的暂停状态
	 ///     <returns>如果已保存返回true，否则返回false</returns>
	/// </summary>
	public static bool HasSavedState()
	{
		return !string.IsNullOrEmpty(_savedPrePauseUiKey);
	}

	/// <summary>
	///     清除保存的暂停状态
	 ///     <para>
	 ///         应该在成功恢复UI后调用此方法
	 ///         避免旧的保存数据影响后续操作
	 ///     </para>
	 ///     <remarks>
	 ///     调用时机:
	 ///     - PlayingState.OnEnterAsync()恢复UI完成后
	 ///     - 或在任何不需要恢复逻辑的场景
	 ///     </remarks>
	/// </summary>
	public static void ClearSavedState()
	{
		if (_savedPrePauseUiKey != null)
		{
			GD.Print($"[PauseStateManager] 🗑️ 已清除暂停状态: {_savedPrePauseUiKey}");
			_savedPrePauseUiKey = null;
		}
	}

	#endregion
}
