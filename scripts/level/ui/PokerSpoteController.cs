using Godot;
using System;
using System.Collections.Generic;
using GFrameworkGodotTemplate.scripts.data.model;

namespace GFrameworkGodotTemplate.scripts.level.ui
{

/// <summary>
///     卡牌插槽控制器 v4.0 - 基于 pokerId 的克隆模式
///     <para>
///        核心设计原则:
///        - 9个原生按钮作为模板（通过 _nativeButtonPaths 配置）
///        - 按 pokerId 索引：pokerId=0 → Button[0], pokerId=1 → Button[1], ...
///        - 收到卡牌时，根据 pokerId 克隆对应模板并显示
///        
///        工作流程:
///        Library 发送 pokerId=3 → 查找 _nativeButtons[3] 模板 → 克隆 → 显示在容器中
///        
///        与 v3.1 的区别:
///        - v3.1: 按顺序分配按钮（第1张→Button[0]）❌
///        - v4.0: 根据 pokerId 克隆对应按钮（pokerId=3→克隆Button[3]）✅
///     </para>
///     <author>AI Assistant</author>
///     <version>4.0.0</version>
///     <date>2026-05-20</date>
/// </summary>
public partial class PokerSpoteController : MarginContainer
{
	#region ==================== 核心数据 ====================

	/// <summary>
	///     🎯 核心：当前显示的卡牌列表（存储 pokerId 和对应的克隆按钮）
	/// </summary>
	private readonly List<(int pokerId, Button clonedBtn)> _displayedCards = new();

	#endregion

	#region ==================== 物理资源：模板按钮 ====================

	/// <summary>
	///     9个原生按钮的路径（在Inspector中配置，作为模板使用）
	/// </summary>
	[Export]
	private NodePath[]? _nativeButtonPaths;

	/// <summary>
	///     原生按钮模板列表（从场景加载，按 pokerId 索引）
	///     _nativeButtons[0] 对应 pokerId=0 的模板
	///     _nativeButtons[1] 对应 pokerId=1 的模板
	///     ...
	/// </summary>
	private readonly List<Button> _nativeButtons = new();

	/// <summary>最大模板数（硬性上限）</summary>
	private const int MAX_TEMPLATES = 9;

	#endregion

	#region ==================== 显示容器 ====================

	/// <summary>
	///     显示容器的路径（用于放置克隆的按钮）
	/// </summary>
	[Export]
	private NodePath? _displayContainerPath;

	/// <summary>
	///     显示容器引用（HBoxContainer，用于布局克隆的按钮）
	/// </summary>
	private HBoxContainer? _displayContainer;

	#endregion

	#region ==================== 数据模型 ====================

	/// <summary>插槽数据模型（复用原有类）</summary>
	private PokerSpote? _slotDataModel;

	#endregion

	#region ==================== 生命周期 ====================

	public override void _Ready()
	{
		try
		{
			InitializeSystem();
			GD.Print("[PokerSpote] ✓ v4.0 基于pokerId的克隆模式初始化完成");
			GD.Print($"[PokerSpote] 模板数量: {_nativeButtons.Count}/{MAX_TEMPLATES}");
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[PokerSpote] ❌ 初始化失败: {ex.Message}");
		}
	}

	public override void _ExitTree()
	{
		UnbindAllEvents();
		CleanupAllClonedButtons();
	}

	#endregion

	#region ==================== 初始化系统 ====================

	private void InitializeSystem()
	{
		InitDataModel();
		FindDisplayContainer();
		LoadNativeButtonsFromScene();  // 加载原生按钮作为模板
		SubscribeToLibraryEvents();
		
		PrintInitializationSummary();
		
		// 📢 触发数据就绪事件（将数据模型地址发送给 LevelPrepareUi）
		SlotDataEvents.DataReady?.Invoke(_slotDataModel);
		
		// 更新全局缓存
		SlotDataEvents.UpdateCache(_slotDataModel);
		
		GD.Print("[PokerSpote] 已发送数据模型引用到 LevelPrepareUi (SlotDataEvents.DataReady)");
	}

	/// <summary>
	///     初始化空的数据模型
	/// </summary>
	private void InitDataModel()
	{
		_slotDataModel = new PokerSpote();
		_slotDataModel.GetPokerlibrary = new List<Pokerlibrary>();
		_slotDataModel.Number = 0;
	}

	/// <summary>
	///     查找显示容器
	/// </summary>
	private void FindDisplayContainer()
	{
		if (_displayContainerPath != null)
		{
			_displayContainer = GetNodeOrNull<HBoxContainer>(_displayContainerPath);
		}
		
		if (_displayContainer == null)
		{
			_displayContainer = GetNodeOrNull<HBoxContainer>("VBoxContainer/HBoxContainer");
		}
		
		if (_displayContainer != null)
		{
			GD.Print($"[PokerSpote] ✓ 显示容器已找到: {_displayContainer.Name}");
		}
		else
		{
			GD.PrintErr("[PokerSpote] ❌ 未找到显示容器");
		}
	}

	/// <summary>
	///     从场景加载原生按钮作为模板（不显示，仅用于克隆）
	/// </summary>
	private void LoadNativeButtonsFromScene()
	{
		_nativeButtons.Clear();
		
		if (_nativeButtonPaths != null && _nativeButtonPaths.Length > 0)
		{
			foreach (var path in _nativeButtonPaths)
			{
				if (string.IsNullOrEmpty(path)) continue;
				
				var btn = GetNodeOrNull<Button>(path);
				if (btn != null && _nativeButtons.Count < MAX_TEMPLATES)
				{
					int idx = _nativeButtons.Count;
					_nativeButtons.Add(btn);
					
					btn.Visible = false; // 保持隐藏（作为模板）
					
					GD.Print($"[PokerSpote] ✓ 模板[{idx}] 已加载: {btn.Name} (pokerId={idx})");
				}
			}
		}
		else
		{
			GD.Print($"[PokerSpote] ⚠ 未配置 Native Button Paths，尝试自动发现...");
			AutoDiscoverNativeButtons();
		}
	}

	/// <summary>
	///     自动发现场景中的 Button 节点作为模板（备用方案）
	/// </summary>
	private void AutoDiscoverNativeButtons()
	{
		var container = GetNodeOrNull<HBoxContainer>("VBoxContainer/HBoxContainer");
		if (container == null) return;
		
		int idx = 0;
		foreach (Node child in container.GetChildren())
		{
			if (child is Button btn && idx < MAX_TEMPLATES)
			{
				_nativeButtons.Add(btn);
				btn.Visible = false;
				
				GD.Print($"[PokerSpote] ✓ 自动发现模板[{idx}]: {btn.Name}");
				idx++;
			}
		}
	}

	/// <summary>
	///     订阅 Library 事件
	/// </summary>
	private void SubscribeToLibraryEvents()
	{
		LibraryEvents.CardToSlot -= OnReceiveCardFromLibrary;
		LibraryEvents.CardToSlot += OnReceiveCardFromLibrary;
		
		GD.Print("[PokerSpote] 已订阅 LibraryEvents.CardToSlot");
	}

	private void PrintInitializationSummary()
	{
		GD.Print($"[PokerSpote] ══════ 初始化摘要 (v4.0基于pokerId克隆模式) ══════");
		GD.Print($"   模板数量: {_nativeButtons.Count}/{MAX_TEMPLATES}");
		GD.Print($"   当前显示: {_displayedCards.Count} 张");
		GD.Print($"═════════════════════════════");
	}

	#endregion

	#region ==================== 核心通讯：数据接收与克隆显示 ====================

	/// <summary>
	///     🎯 核心：接收来自 Library 的卡牌数据，根据 pokerId 克隆对应模板
	///     <para>
	///         处理流程:
	///         1. 验证 pokerId 有效性
	///         2. 验证模板是否存在（pokerId 是否在模板范围内）
	///         3. 克隆对应模板并添加到显示容器
	///         4. 更新内部数据和数据模型
	///         
	///         示例:
	///         输入 [0, 1, 1, 0] → 显示 [克隆0, 克隆1, 克隆1, 克隆0]
	///     </para>
	/// </summary>
	private void OnReceiveCardFromLibrary(int pokerId)
	{
		GD.Print("═════════════════════════");
		GD.Print($"[PokerSpote] 收到卡牌 Id:{pokerId}");
		
		// 步骤1: 验证有效性
		var globalInstance = Pokerlibrary.Instance;
		if (!IsValidPokerId(pokerId, globalInstance))
		{
			GD.PrintErr($"[PokerSpote] 无效的pokerId: {pokerId}");
			return;
		}
		
		var pokerInfo = globalInstance[pokerId];
		GD.Print($"[PokerSpote] 卡牌信息: {pokerInfo.Name} (Type:{pokerInfo.Type})");
		
		// 步骤2: 验证模板是否存在
		if (pokerId >= _nativeButtons.Count)
		{
			GD.PrintErr($"[PokerSpote] ❌ pokerId:{pokerId} 超出模板范围 (0-{_nativeButtons.Count - 1})");
			return;
		}
		
		// 步骤3: 检查容量限制（最多9个）
		if (_displayedCards.Count >= MAX_TEMPLATES)
		{
			GD.Print($"[PokerSpote] ⚠ 插槽已满 ({_displayedCards.Count}/{MAX_TEMPLATES})，无法接收");
			return;
		}
		
		// 步骤4: 克隆模板并显示
		Button? clonedBtn = CloneAndDisplayButton(pokerId);
		if (clonedBtn == null)
		{
			GD.PrintErr($"[PokerSpote] ❌ 克隆失败 (pokerId:{pokerId})");
			return;
		}
		
		// 步骤5: 更新内部数据（允许重复pokerId）
		_displayedCards.Add((pokerId, clonedBtn));
		GD.Print($"[PokerSpote] ✓ 已克隆并显示 (pokerId:{pokerId}, 总数:{_displayedCards.Count}/{MAX_TEMPLATES})");
		
		// 步骤6: 更新数据模型
		UpdateDataModel();
		
		PrintCurrentState();
		GD.Print("═════════════════════════");
	}

	/// <summary>
	///     验证 pokerId 是否有效
	/// </summary>
	private bool IsValidPokerId(int pokerId, List<Pokerlibrary> globalLib)
	{
		return pokerId >= 0 && pokerId < globalLib.Count && pokerId != 99;
	}

	/// <summary>
	///     🎯 核心：根据 pokerId 克隆对应的模板按钮并显示
	/// </summary>
	private Button? CloneAndDisplayButton(int pokerId)
	{
		if (_displayContainer == null)
		{
			GD.PrintErr("[PokerSpote] 显示容器为空");
			return null;
		}
		
		Button template = _nativeButtons[pokerId];
		if (template == null)
		{
			GD.PrintErr($"[PokerSpote] 模板[{pokerId}] 为空");
			return null;
		}
		
		// 使用 Duplicate() 克隆整个按钮节点（包括子节点 TextureRect、TouchRect）
		Button clonedBtn = (Button)template.Duplicate();
		clonedBtn.Visible = true;
		clonedBtn.Name = $"ClonedCard_{pokerId}";
		
		// 绑定点击事件（用于移除）
		clonedBtn.Pressed += () => OnClonedButtonClick(pokerId, clonedBtn);
		
		// 添加到显示容器
		_displayContainer.AddChild(clonedBtn);
		
		GD.Print($"[PokerSpote] ✓ 已克隆模板[{pokerId}] → {clonedBtn.Name}");
		
		return clonedBtn;
	}

	#endregion

	#region ==================== 移除逻辑：点击克隆的卡牌 ====================

	/// <summary>
	///     克隆按钮点击 → 移除该卡牌
	/// </summary>
	private void OnClonedButtonClick(int pokerId, Button clonedBtn)
	{
		GD.Print("═════════════════════════");
		GD.Print($"[PokerSpote] 点击克隆卡牌 (pokerId:{pokerId})");
		
		RemoveClonedCard(pokerId, clonedBtn);
	}

	/// <summary>
	///     🎯 核心：移除指定的克隆卡牌
	/// </summary>
	private void RemoveClonedCard(int pokerId, Button clonedBtn)
	{
		int beforeCount = _displayedCards.Count;
		GD.Print($"[PokerSpote] 移除前总数: {beforeCount}");
		
		// 从内部数据中移除
		bool removed = _displayedCards.RemoveAll(item => item.pokerId == pokerId && item.clonedBtn == clonedBtn) > 0;
		if (!removed)
		{
			GD.Print("[PokerSpote] 未在数据中找到该卡牌");
			return;
		}
		
		GD.Print($"[PokerSpote] 已从数据中移除 pokerId:{pokerId}");
		
		// 从显示容器中移除并释放
		clonedBtn.Pressed -= () => OnClonedButtonClick(pokerId, clonedBtn);
		clonedBtn.QueueFree();
		
		GD.Print($"[PokerSpote] ✓ 克隆卡牌(pokerId:{pokerId}) 已移除并释放");
		
		UpdateDataModel();
		
		int afterCount = _displayedCards.Count;
		GD.Print($"[PokerSpote] 移除后总数: {afterCount}");
		
		PrintCurrentState();
		GD.Print("═════════════════════════");
	}

	#endregion

	#region ==================== 辅助方法 ====================

	/// <summary>
	///     基于内部数据更新数据模型
	/// </summary>
	private void UpdateDataModel()
	{
		if (_slotDataModel == null) return;
		
		List<Pokerlibrary> currentCards = new();
		var globalLib = Pokerlibrary.Instance;
		
		foreach (var (pokerId, _) in _displayedCards)
		{
			if (pokerId >= 0 && pokerId < globalLib.Count)
			{
				currentCards.Add(globalLib[pokerId]);
			}
		}
		
		_slotDataModel.Number = currentCards.Count;
		_slotDataModel.GetPokerlibrary = currentCards;
		
		SlotDataEvents.DataUpdated?.Invoke(_slotDataModel);
		SlotDataEvents.UpdateCache(_slotDataModel);
	}

	/// <summary>
	///     打印当前完整状态
	/// </summary>
	private void PrintCurrentState()
	{
		GD.Print("\n[PokerSpote] 当前状态:");
		GD.Print($"   显示卡牌: {_displayedCards.Count}/9 张");
		if (_displayedCards.Count > 0)
		{
			GD.Print("      内容:");
			foreach (var (pokerId, btn) in _displayedCards)
			{
				GD.Print($"         - pokerId:{pokerId} → {btn.Name}");
			}
		}
		
		GD.Print($"   数据模型: Number={_slotDataModel?.Number ?? 0}");
	}

	/// <summary>
	///     清理所有事件
	/// </summary>
	private void UnbindAllEvents()
	{
		LibraryEvents.CardToSlot -= OnReceiveCardFromLibrary;
	}

	/// <summary>
	///     清理所有克隆按钮
	/// </summary>
	private void CleanupAllClonedButtons()
	{
		foreach (var (_, clonedBtn) in _displayedCards)
		{
			if (clonedBtn != null && IsInstanceValid(clonedBtn))
			{
				clonedBtn.QueueFree();
			}
		}
		
		_displayedCards.Clear();
		
		GD.Print("[PokerSpote] 所有克隆按钮已清理");
	}

	#endregion

	#region ==================== 公开API ====================

	/// <summary>
	///     手动刷新显示
	/// </summary>
	public void RefreshDisplay()
	{
		UpdateDataModel();
		PrintCurrentState();
	}

	/// <summary>
	///     获取当前显示的卡牌数量
	/// </summary>
	public int GetCurrentCardCount() => _displayedCards.Count;

	/// <summary>
	///     获取数据模型只读副本
	/// </summary>
	public PokerSpote? GetSlotData() => _slotDataModel;

	/// <summary>
	///     获取内部数据快照（pokerId 列表）
	/// </summary>
	public List<int> GetReceivedCardsSnapshot() => 
		_displayedCards.Select(item => item.pokerId).ToList();

	/// <summary>
	///     获取最大容量
	/// </summary>
	public int GetMaxCapacity() => MAX_TEMPLATES;

	/// <summary>
	///     清空整个插槽
	/// </summary>
	public void ClearAllSlots()
	{
		CleanupAllClonedButtons();
		UpdateDataModel();
		
		GD.Print("[PokerSpote] 插槽已完全清空");
	}

	#endregion
}

/// <summary>
///     插槽数据传输事件类
///     用于 PokerSpoteController → LevelPrepareUi → PokerHand 的数据传递链路
/// </summary>
public static class SlotDataEvents
{
	/// <summary>
	///     数据模型就绪事件（参数：PokerSpote数据模型引用）
	///     在 PokerSpoteController 初始化完成后触发
	/// </summary>
	public static Action<PokerSpote?>? DataReady;
	
	/// <summary>
	///     数据更新事件（参数：PokerSpote数据模型引用）
	///     在插槽数据变化时触发（添加/移除卡牌）
	/// </summary>
	public static Action<PokerSpote?>? DataUpdated;

	/// <summary>
	///     全局数据缓存
	///     <para>
	///         用途: 在场景切换间保持插槽数据引用
	///         解决: LevelPrepareUi → LevelBuildUi 的数据传递断裂问题
	///         
	///         使用方式:
	///         1. PokerSpoteController 触发事件时同时更新此缓存
	///         2. LevelBuildUi 在 _Ready() 时读取此缓存
	///         3. 数据为 null 表示无可用数据
	///     </para>
	/// </summary>
	public static PokerSpote? LastKnownData { get; private set; }

	/// <summary>
	///     更新全局数据缓存（内部方法，由事件触发时调用）
	/// </summary>
	internal static void UpdateCache(PokerSpote? data)
	{
		LastKnownData = data;
		GD.Print($"[SlotDataEvents] 📦 缓存已更新 (卡牌数:{data?.Number ?? 0})");
	}
}  // end SlotDataEvents

}  // end namespace
