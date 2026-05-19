using Godot;
using System;
using System.Security.Cryptography;
namespace GFrameworkGodotTemplate.scripts.data.model;
public partial class Pokerlibrary : Node
{
	private int _id;
	private string _name;
	private PokerType _type;
	private string _description;
	private bool _isGet;
	/// <summary>牌库ID</summary>
	public int Id{get=>_id;set=>_id=value;}
	
	/// <summary>牌库名称</summary>
	public string Name{get=>_name;set=>_name=value;}

	/// <summary>牌库类型</summary>
	public PokerType Type{get=>_type;set=>_type=value;} 

	/// <summary>牌库描述</summary>
	public string Description{get=>_description;set=>_description=value;}   

	/// <summary>玩家是否已拥有</summary>   
	public bool IsGet{get=>_isGet;set=>_isGet=value;}

	public Pokerlibrary(int id, string name, PokerType type, string description, bool isGet)
	{
		Id = id;
		Name = name;
		Type = type;
		Description = description;
		IsGet = isGet;
	}
	public static List<Pokerlibrary> Instance = new List<Pokerlibrary>
	{
		new Pokerlibrary(0, "platform", PokerType.item, "可以踩踏的平台", true),
		new Pokerlibrary(1, "wall", PokerType.item, "用来阻挡的墙", false),
		new Pokerlibrary(2, "Rush", PokerType.action, "快速移动", false),
		new Pokerlibrary(3, "DoubleJump", PokerType.action, "二段跳跃", false),
		new Pokerlibrary(99, "none", PokerType.none, "none", false),    
		
	};

	public  enum PokerType
	{
		item,
		action,
		none
	}
}
