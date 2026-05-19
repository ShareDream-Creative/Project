using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using GFrameworkGodotTemplate.scripts.data.model;

namespace GFrameworkGodotTemplate.scripts.level.ui
{

/// <summary>
///     卡牌仓库控制器 v1.1 - 已拥有卡牌展示（⭐支持多次点击发送）
///     <para>
///         功能：显示玩家已拥有的卡牌（IsGet=true）
///         支持点击选中（TouchRect蒙版视觉反馈）
///         单选模式：同时只能选中一张卡牌
///         ⭐ v1.1核心特性: 同一卡牌可无限次点击，每次都发送到插槽
///     </para>
///     <author>AI Assistant</author>
///     <version>1.1.0</version>
///     <date>2026-05-19</date>
///     <description>
///         与 Shop.cs 的区别:
///         - Shop: 显示 IsGet=false + 购买功能
///         - Library v1.1: 显示 IsGet=true + ⭐多次发送到插槽功能
///
///         核心功能:
///         - 初始化按钮映射（复用 ShopButtonMapping）
///         - 根据拥有状态过滤显示（IsGet=true 显示）
///         - 点击显示蒙版（TouchRect）作为视觉反馈
///         - 单选模式（自动取消其他选中）
///         - ⭐ 多次点击: 每次点击都触发 CardToSlot 事件
///         - 通过 LibraryEvents 通知 PokerSpoteController 接收数据
///
///         ⭐ v1.1 交互逻辑:
///         点击 Rush 卡牌第1次 → 发送 pokerId=2 到插槽 ✅
///         点击 Rush 卡牌第2次 → 再次发送 pokerId=2 到插槽 ✅
///         点击 Rush 卡牌第3次 → 继续发送... ✅
///         插槽结果: [Rush(原生), Rush(克隆#1), Rush(克隆#2)]
///
///         场景结构:
///         └── Library (MarginContainer)
///             ├── Button[0] (CardButton) ← 可重复点击
///             │   ├── TextureRect (卡牌图片)
///             │   └── TouchRect (选中蒙版-闪烁反馈)
///             └── ... (最多18张)
///     </description>
/// </summary>
public partial class Library : MarginContainer
{
	#region 私有字段

	[Export]
	private NodePath[]? _buttonPath;
	
	private readonly List<Button> _buttons = new();
	private readonly List<ShopButtonMapping> _buttonMappings = new();
	
	/// <summary>当前选中的卡牌 ID/索引（默认99未选中）</summary>
	private int _selectedId = 99;
	
	/// <summary>蒙版闪烁计时器（帧计数器）</summary>
	private int _flashTimer = 0;
	
	/// <summary>蒙版闪烁持续时间（帧数，约0.1秒=6帧@60FPS）</summary>
	private const int FLASH_DURATION_FRAMES = 6;

	#endregion

	#region 生命周期

	public override void _Ready()
	{
		try
		{
			InitAll();
			GD.Print("[Library] ✓ 初始化完成");
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[Library] ❌ 失败: {ex.Message}");
		}
	}

	/// <summary>
	///     每帧更新：处理蒙版闪烁效果
	/// </summary>
	public override void _Process(double delta)
	{
		if (_flashTimer > 0)
		{
			_flashTimer--;
			
			if (_flashTimer <= 0)
			{
				// 闪烁结束，隐藏所有蒙版
				foreach (var m in _buttonMappings)
				{
					m.SetSelection(false);
				}
			}
		}
	}

	#endregion

	#region 初始化

	private void InitAll()
	{
		InitButtons();
		InitMappings();
		UpdateUI();
		BindEvents();
		
		GD.Print($"[Library] 按钮:{_buttons.Count} | 映射:{_buttonMappings.Count}");
	}

	private void InitButtons()
	{
		_buttons.Clear();
		if (_buttonPath == null) return;
		
		foreach (var path in _buttonPath)
		{
			if (string.IsNullOrEmpty(path)) continue;
			var btn = GetNodeOrNull<Button>(path);
			if (btn != null) _buttons.Add(btn);
		}
	}

	/// <summary>
	///     初始化映射：buttons[i] 对应 Instance[i]，且 Instance[i].Id == i
	/// </summary>
	private void InitMappings()
	{
		_buttonMappings.Clear();
		var instance = Pokerlibrary.Instance;
		
		for (int i = 0; i < _buttons.Count; i++)
		{
			var btn = _buttons[i];
			if (btn == null) continue;
			
			var touchRect = btn.GetNodeOrNull<ColorRect>("TouchRect");
			if (touchRect != null) touchRect.Visible = false;
			
			var mapping = new ShopButtonMapping(btn, touchRect, i);
			_buttonMappings.Add(mapping);
			
			if (i < instance.Count)
			{
				GD.Print($"[Library] ✓ 按钮[{i}] ↔ {instance[i].Name} (Id:{instance[i].Id})");
			}
		}
	}

	/// <summary>
	///     更新显隐（与Shop相反：显示已拥有的卡牌）
	///     1. 检查 index == poker.Id（安全验证）
	///     2. IsGet=true 显示，IsGet=false 隐藏
	/// </summary>
	private void UpdateUI()
	{
		var instance = Pokerlibrary.Instance;
		
		GD.Print("═════════════════════════");
		GD.Print("[Library] 🔄 刷新显示");
		
		for (int i = 0; i < _buttons.Count && i < instance.Count; i++)
		{
			var btn = _buttons[i];
			if (btn == null) continue;
			
			var poker = instance[i];
			
			if (i != poker.Id)
			{
				btn.Visible = false;
				GD.Print($"   [{i}] ⚠ 索引不匹配! (idx:{i} ≠ id:{poker.Id}) → 隐藏");
				continue;
			}
			
			bool show = poker.IsGet;
			btn.Visible = show;
			
			GD.Print($"   [{i}] {poker.Name,-12} | Id={poker.Id} | IsGet={poker.IsGet,-5} | {(show?"✅显示":"❌隐藏")}");
		}
		
		GD.Print("═════════════════════════");
	}

	private void BindEvents()
	{
		foreach (var btn in _buttons)
		{
			if (btn != null)
			{
				btn.Pressed -= OnBtnClick;
				btn.Pressed += OnBtnClick;
			}
		}
	}

	#endregion

	#region 按钮点击（蒙版闪烁 + 多次发送）

	private void OnBtnClick()
	{
		var btn = FindClickedBtn();
		if (btn == null) return;
		
		int idx = _buttons.IndexOf(btn);
		if (idx < 0) return;
		
		var mapping = _buttonMappings[idx];
		
		// 🎯 v1.2 核心改动: 蒙版闪烁效果（出现→下一帧消失）
		// 1. 显示当前按钮的蒙版
		mapping.SetSelection(true);
		
		// 2. 启动闪烁计时器（_Process中自动隐藏）
		_flashTimer = FLASH_DURATION_FRAMES;
		
		_selectedId = idx;
		
		var instance = Pokerlibrary.Instance;
		if (idx < instance.Count)
		{
			var p = instance[idx];
			GD.Print("═════════════════════════");
			GD.Print($"[Library] 点击发送!");
			string owned = p.IsGet ? "Y" : "N";
			GD.Print($"   Idx:{idx} | Id:{p.Id} | {p.Name} | 持有:{owned}");
			GD.Print("═════════════════════════");
		}
		
		// 3. 触发选中事件
		LibraryEvents.ItemSelected?.Invoke(idx);
		
		// 4. 发送卡牌到卡槽（每次点击都发送）
		LibraryEvents.CardToSlot?.Invoke(idx);
		GD.Print($"[Library] 已发送卡牌到卡槽 (Id:{idx})");
	}

	private Button? FindClickedBtn()
	{
		return _buttons.FirstOrDefault(b => b != null && (b.HasFocus() || b.ButtonPressed));
	}

	#endregion

	#region 公开方法

	/// <summary>
	///     刷新仓库显示（可在购买后调用以更新已拥有列表）
	/// </summary>
	public void RefreshLibrary()
	{
		_selectedId = 99;
		foreach (var m in _buttonMappings) m.ResetState();
		InitMappings();
		UpdateUI();
	}

	/// <summary>
	///     获取当前选中的卡牌 ID
	/// </summary>
	public int GetSelectedId() => _selectedId;

	/// <summary>
	///     检查是否有选中的卡牌
	/// </summary>
	public bool HasSelection() => _selectedId != 99;

	#endregion

	#region 清理

	public override void _ExitTree()
	{
		foreach (var btn in _buttons)
		{
			if (btn != null) btn.Pressed -= OnBtnClick;
		}
	}

	#endregion
}

/// <summary>
///     Library 静态事件类
///     用于通知外部脚本（如LevelEndUi、PokerSpoteController）选中状态变化
/// </summary>
public static class LibraryEvents
{
	/// <summary>卡牌被选中时触发（参数：索引/ID）</summary>
	public static Action<int>? ItemSelected;
	
	/// <summary>卡牌取消选中时触发（参数：索引/ID）</summary>
	public static Action<int>? ItemDeselected;
	
	/// <summary>卡牌发送到卡槽触发（参数：pokerId）- 用于Library→PokerSpote通讯</summary>
	public static Action<int>? CardToSlot;
}  // end LibraryEvents

}  // end namespace
