using System.Collections.Generic;
using Godot;
using GFrameworkGodotTemplate.scripts.data.model;

namespace GFrameworkGodotTemplate.scripts.events.poker;

/// <summary>
///     动作卡牌数据模型（分类后的 Action 类型卡牌）
///     <para>
///         用途: 从 PokerSpoteData 中提取的纯动作类卡牌数据
 ///        数据来源: LevelPrepareUi 根据 PokerType.action 筛选
 ///        
 ///        包含的动作类型:
 ///        - Rush (快速移动)
 ///        - DoubleJump (二段跳)
 ///        - 未来扩展的其他动作类型
 ///     </para>
/// </summary>
public class ActionPokerData
{
	/// <summary>动作卡牌列表</summary>
	public List<Pokerlibrary> Actions { get; set; } = new();

	/// <summary>
	///     获取指定动作类型的数量
	/// </summary>
	/// <param name="actionName">动作名称（如 "DoubleJump"）</param>
	/// <returns>该动作的可用次数</returns>
	public int GetActionCount(string actionName)
	{
		int count = 0;
		foreach (var action in Actions)
		{
			if (action.Name.Equals(actionName, System.StringComparison.OrdinalIgnoreCase))
			{
				count++;
			}
		}
		return count;
	}

	/// <summary>
	///     消耗一个动作（使用一次后调用）
	/// </summary>
	/// <param name="actionName">动作名称</param>
	/// <returns>是否成功消耗</returns>
	public bool ConsumeAction(string actionName)
	{
		for (int i = 0; i < Actions.Count; i++)
		{
			if (Actions[i].Name.Equals(actionName, System.StringComparison.OrdinalIgnoreCase))
			{
				Actions.RemoveAt(i);
				return true;
			}
		}
		return false;
	}

	/// <summary>
	///     获取所有可用动作及其次数的字典
	/// </summary>
	public Dictionary<string, int> GetAllActions()
	{
		var actionDict = new Dictionary<string, int>();
		foreach (var action in Actions)
		{
			if (!actionDict.ContainsKey(action.Name))
			{
				actionDict[action.Name] = 0;
			}
			actionDict[action.Name]++;
		}
		return actionDict;
	}

	/// <summary>总动作数</summary>
	public int TotalCount => Actions.Count;

	/// <summary>是否有可用动作</summary>
	public bool HasActions => Actions.Count > 0;
}

/// <summary>
///     物品卡牌数据模型（分类后的 Item 类型卡牌）
///     <para>
///         用途: 从 PokerSpoteData 中提取的纯物品类卡牌数据
 ///        数据来源: LevelPrepareUi 根据 PokerType.item 筛选
 ///        
 ///        包含的物品类型:
 ///        - platform (平台)
 ///        - wall (墙壁)
 ///        - 未来扩展的其他物品类型
 ///     </para>
/// </summary>
public class ItemPokerData
{
	/// <summary>物品卡牌列表（保持原有结构）</summary>
	public List<Pokerlibrary> Items { get; set; } = new();

	/// <summary>总物品数</summary>
	public int TotalCount => Items.Count;
}

/// <summary>
///     分类后的卡牌数据事件类
///     <para>
///         用于传输从 PokerSpoteData 分类后的数据:
 ///        - ItemPokerData → PokerHand (手牌显示)
 ///        - ActionPokerData → Player (能力系统)
 ///        
 ///        触发时机: LevelPrepareUi 完成分类后立即触发
 ///     </para>
/// </summary>
public static class ClassifiedPokerEvents
{
	/// <summary>
	///     物品卡牌就绪事件（参数：ItemPokerData）
	///     订阅者: PokerHand
	/// </summary>
	public static Action<ItemPokerData?>? ItemDataReady;

	/// <summary>
	///     动作卡牌就绪事件（参数：ActionPokerData）
	///     订阅者: PlayerActionController
	/// </summary>
	public static Action<ActionPokerData?>? ActionDataReady;

	/// <summary>
	///     动作消耗通知事件
	///     <para>
	///         当玩家使用一个动作时触发（如使用一次二段跳）
	 ///        参数: 动作名称 + 剩余次数
 ///     </para>
	/// </summary>
	public static Action<string, int>? ActionConsumed;

	/// <summary>
	///     全局缓存：最后一次的分类数据
	/// </summary>
	public static ItemPokerData? LastItemData { get; private set; }
	public static ActionPokerData? LastActionData { get; private set; }

	/// <summary>
	///     更新全局缓存
	/// </summary>
	internal static void UpdateCache(ItemPokerData? itemData, ActionPokerData? actionData)
	{
		LastItemData = itemData;
		LastActionData = actionData;
		GD.Print($"[ClassifiedPokerEvents] 📦 缓存已更新 (Item:{itemData?.TotalCount ?? 0}, Action:{actionData?.TotalCount ?? 0})");
	}
}
