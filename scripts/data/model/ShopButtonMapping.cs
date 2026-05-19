  using Godot;

namespace GFrameworkGodotTemplate.scripts.data.model;

/// <summary>
///     商城按钮映射数据模型
///     <para>
///         存储商城按钮与商品的绑定关系及交互状态
///         实现商品基础数据表与UI按钮的一一映射
///     </para>
///     <author>AI Assistant</author>
///     <version>2.0.0</version>
///     <date>2026-05-19</date>
///     <description>
///         数据结构:
///         - ButtonRef: 按钮节点实体引用
///         - TouchRectRef: 按钮内部的选中蒙版 (ColorRect)
///         - PokerId: 绑定的商品唯一序号（对应 Pokerlibrary.Id）
///         - IsSelected: 按钮选中状态（控制 TouchRect 显隐）
///         
///         场景结构对应关系:
///         └── Button (ButtonRef)
///             ├── TextureRect (商品图片)
///             └── TouchRect (TouchRectRef) ← 选中时显示的蒙版
///         
///         设计原则:
///         - 唯一映射：一个按钮仅对应一个商品序号
///         - 状态独立：按钮选中状态独立于商品拥有状态
///         - 职责单一：仅存储数据，不包含业务逻辑
///     </description>
///     <remarks>
///         使用场景:
///         - Shop.cs 初始化时创建映射表
///         - UI交互时读取/修改选中状态
///         - 购买流程中传递商品序号
///     </remarks>
/// </summary>
public class ShopButtonMapping
{
	#region 私有字段

	/// <summary>按钮节点引用</summary>
	private Button? _buttonRef;

	/// <summary>按钮内部选中蒙版 (TouchRect/ColorRect)</summary>
	private ColorRect? _touchRectRef;

	/// <summary>绑定的商品唯一序号</summary>
	private int _pokerId;

	/// <summary>按钮选中状态（默认false未选中）</summary>
	private bool _isSelected;

	#endregion

	#region 公开属性

	/// <summary>
	///     按钮节点引用
	///     <para>对应场景中的 Button 节点实体</para>
	/// </summary>
	public Button? ButtonRef
	{
		get => _buttonRef;
		set => _buttonRef = value;
	}

	/// <summary>
	///     选中蒙版节点引用
	///     <para>对应按钮子节点 "TouchRect" (ColorRect 类型)</para>
	///     <remarks>
	///         此蒙版用于视觉反馈，显示当前选中的卡片
	///         默认隐藏(visible=false)，选中时显示(visible=true)
	///     </remarks>
	/// </summary>
	public ColorRect? TouchRectRef
	{
		get => _touchRectRef;
		set => _touchRectRef = value;
	}

	/// <summary>
	///     绑定的商品唯一序号
	///     <para>对应 Pokerlibrary.Id，用于数据表匹配</para>
	/// </summary>
	public int PokerId
	{
		get => _pokerId;
		set => _pokerId = value;
	}

	/// <summary>
	///     按钮选中状态
	///     <para>
	///         false: 未选中 → 点击后显示 TouchRect 蒙版，传递商品序号
	///         true: 已选中 → 点击后隐藏 TouchRect 蒙版，取消选择
	///     </para>
	///     <remarks>
	///         此状态独立于商品拥有状态(IsGet)，仅控制 TouchRect 显隐
	///     </remarks>
	/// </summary>
	public bool IsSelected
	{
		get => _isSelected;
		set => _isSelected = value;
	}

	#endregion

	#region 构造函数

	/// <summary>
	///     创建商城按钮映射实例
	/// </summary>
	/// <param name="button">按钮节点引用</param>
	/// <param name="pokerId">绑定的商品唯一序号</param>
	public ShopButtonMapping(Button? button, int pokerId)
	{
		_buttonRef = button;
		_pokerId = pokerId;
		_isSelected = false;
		_touchRectRef = null;
	}

	/// <summary>
	///     创建完整映射实例（含蒙版引用）
	/// </summary>
	public ShopButtonMapping(Button? button, ColorRect? touchRect, int pokerId)
	{
		_buttonRef = button;
		_touchRectRef = touchRect;
		_pokerId = pokerId;
		_isSelected = false;
	}

	/// <summary>
	///     默认构造函数（用于序列化等场景）
	/// </summary>
	public ShopButtonMapping()
	{
		_buttonRef = null;
		_touchRectRef = null;
		_pokerId = -1;
		_isSelected = false;
	}

	#endregion

	#region 公开方法

	/// <summary>
	///     切换按钮选中状态并同步更新 TouchRect 显示
	/// </summary>
	/// <returns>切换后的状态值</returns>
	public bool ToggleSelection()
	{
		_isSelected = !_isSelected;
		
		SyncTouchRectVisibility();
		
		return _isSelected;
	}

	/// <summary>
	///     设置选中状态并同步更新 TouchRect
	/// </summary>
	/// <param name="selected">目标选中状态</param>
	public void SetSelection(bool selected)
	{
		_isSelected = selected;
		SyncTouchRectVisibility();
	}
	
	/// <summary>
	///     重置按钮状态为未选中（隐藏蒙版）
	/// </summary>
	public void ResetState()
	{
		_isSelected = false;
		SyncTouchRectVisibility();
	}

	/// <summary>
	///     同步 TouchRect 显示状态（内部方法）
	/// </summary>
	private void SyncTouchRectVisibility()
	{
		if (_touchRectRef == null) return;
		
		try
		{
			_touchRectRef.Visible = _isSelected;
		}
		catch (Exception)
		{
			// 节点可能已被释放，忽略错误
		}
	}

	/// <summary>
	///     检查映射数据是否有效
	/// </summary>
	/// <returns>如果按钮引用和商品序号都有效则返回true</returns>
	public bool IsValid()
	{
		return _buttonRef != null && _pokerId >= 0;
	}

	/// <summary>
	///     检查是否已完全初始化（包含蒙版引用）
	/// </summary>
	public bool IsFullyInitialized()
	{
		return IsValid() && _touchRectRef != null;
	}

	#endregion
}
