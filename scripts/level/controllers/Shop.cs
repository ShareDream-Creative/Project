using Godot;
using System;
using System.Collections.Generic;
using GFrameworkGodotTemplate.scripts.data.model;

namespace GFrameworkGodotTemplate.scripts.level.controllers;

/// <summary>
///     商店控制器 v6.0 - 简化版（索引=ID）
///     前提：buttons[i] 对应的卡牌，其 Poker.Id == i
/// </summary>
public partial class Shop : MarginContainer
{
	#region 私有字段

	[Export]
	private NodePath[]? _buttonPath;
	
	private readonly List<Button> _buttons = new();
	private readonly List<ShopButtonMapping> _buttonMappings = new();
	
	/// <summary>当前选中的商品 ID/索引（默认99未选中）</summary>
	private int _selectedId = 99;

	#endregion

	#region 生命周期

	public override void _Ready()
	{
		try
		{
			InitAll();
			GD.Print("[Shop] ✓ 初始化完成");
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[Shop] ❌ 失败: {ex.Message}");
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
		
		GD.Print($"[Shop] 按钮:{_buttons.Count} | 映射:{_buttonMappings.Count}");
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
			
			// ✅ 直接用 i 作为 ID（因为 buttons[i] 对应 Id==i 的卡牌）
			var mapping = new ShopButtonMapping(btn, touchRect, i);
			_buttonMappings.Add(mapping);
			
			if (i < instance.Count)
			{
				GD.Print($"[Shop] ✓ 按钮[{i}] ↔ {instance[i].Name} (Id:{instance[i].Id})");
			}
		}
	}

	/// <summary>
	///     更新显隐：
	///     1. 检查 index == poker.Id（安全验证）
	///     2. IsGet=false 显示，IsGet=true 隐藏
	/// </summary>
	private void UpdateUI()
	{
		var instance = Pokerlibrary.Instance;
		
		GD.Print("═════════════════════════");
		GD.Print("[Shop] 🔄 刷新显示");
		
		for (int i = 0; i < _buttons.Count && i < instance.Count; i++)
		{
			var btn = _buttons[i];
			if (btn == null) continue;
			
			var poker = instance[i];
			
			// ✅ 安全检查：index 必须等于 poker.Id
			if (i != poker.Id)
			{
				btn.Visible = false;
				GD.Print($"   [{i}] ⚠ 索引不匹配! (idx:{i} ≠ id:{poker.Id}) → 隐藏");
				continue;
			}
			
			// IsGet=false 显示，IsGet=true 隐藏
			bool show = !poker.IsGet;
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
		
		ShopEvents.PurchaseExecuted -= OnPurchase;
		ShopEvents.PurchaseExecuted += OnPurchase;
		ShopEvents.PurchaseCompleted -= OnPurchased;
		ShopEvents.PurchaseCompleted += OnPurchased;
	}

	#endregion

	#region 按钮点击

	private void OnBtnClick()
	{
		var btn = FindClickedBtn();
		if (btn == null) return;
		
		int idx = _buttons.IndexOf(btn);
		if (idx < 0) return;
		
		var mapping = _buttonMappings[idx];
		bool selected = mapping.ToggleSelection();
		
		if (!selected)
		{
			_selectedId = 99;
			GD.Print($"[Shop] 🔒 取消 | Idx:{idx}");
		}
		else
		{
			// 单选：取消其他
			foreach (var m in _buttonMappings)
			{
				if (m != mapping && m.IsSelected) m.SetSelection(false);
			}
			
			_selectedId = idx;  // ✅ 存储索引（=Id）
			
			var instance = Pokerlibrary.Instance;
			if (idx < instance.Count)
			{
				var p = instance[idx];
				GD.Print("═════════════════════════");
				GD.Print($"[Shop] 🎯 选中!");
				GD.Print($"   Idx:{idx} | Id:{p.Id} | {p.Name} | 持有:{(p.IsGet?"✅":"❌")}");
				GD.Print("═════════════════════════");
			}
			
			ShopEvents.ItemSelected?.Invoke(idx);
		}
	}

	private Button? FindClickedBtn()
	{
		return _buttons.FirstOrDefault(b => b != null && (b.HasFocus() || b.ButtonPressed));
	}

	#endregion

	#region 购买流程

	private void OnPurchase()
	{
		GD.Print("═════════════════════════");
		GD.Print("[Shop] 💰 收到购买指令");
		GD.Print($"   选中Id: {_selectedId}");
		
		if (_selectedId == 99)
		{
			GD.Print("[Shop] ⚠ 未选中");
			return;
		}
		
		var instance = Pokerlibrary.Instance;
		if (_selectedId >= instance.Count)
		{
			GD.PrintErr($"[Shop] ❌ 越界: {_selectedId}");
			return;
		}
		
		// ✅ 直接用索引访问：Instance[_selectedId]
		var poker = instance[_selectedId];
		
		GD.Print("───────────────────────────");
		GD.Print($"[Shop] 🛒 购买: {poker.Name} (Id:{poker.Id})");
		GD.Print($"   前: IsGet={poker.IsGet}");
		
		poker.IsGet = true;  // ✅ 直接修改静态实例
		
		GD.Print($"   后: IsGet={poker.IsGet} ✓");
		GD.Print("───────────────────────────");
		
		// 触发刷新
		ShopEvents.PurchaseCompleted?.Invoke(_selectedId);
	}

	private void OnPurchased(int id)
	{
		GD.Print("═════════════════════════");
		GD.Print($"[Shop] 🔄 购买完成回调 (Id:{id})");
		
		// 1. 重置蒙版
		foreach (var m in _buttonMappings) m.ResetState();
		
		// 2. 清除选中
		_selectedId = 99;
		
		// 3. 刷新UI（根据最新的IsGet）
		UpdateUI();
		
		// 输出最终状态
		GD.Print("\n📋 最终状态:");
		var instance = Pokerlibrary.Instance;
		for (int i = 0; i < instance.Count; i++)
		{
			var p = instance[i];
			GD.Print($"   [{i}] Id:{p.Id,-3} | {p.Name,-12} | {(p.IsGet?"✅持":"❌未")}");
		}
		
		GD.Print("═════════════════════════");
	}

	#endregion

	#region 公开方法

	public void RefreshShop()
	{
		_selectedId = 99;
		InitMappings();
		UpdateUI();
	}

	public int GetSelectedId() => _selectedId;

	#endregion

	#region 清理

	public override void _ExitTree()
	{
		ShopEvents.PurchaseExecuted -= OnPurchase;
		ShopEvents.PurchaseCompleted -= OnPurchased;
		
		foreach (var btn in _buttons)
		{
			if (btn != null) btn.Pressed -= OnBtnClick;
		}
	}

	#endregion
}

public static class ShopEvents
{
	public static Action<int>? ItemSelected;
	public static Action? PurchaseExecuted;
	public static Action<int>? PurchaseCompleted;
}
