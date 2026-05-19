
namespace GFrameworkGodotTemplate.scripts.data.model;

public  class PokerSpote 
{
	private int number;//总数
	private List<Pokerlibrary> list = new List<Pokerlibrary>();//列表，每个元素为一个牌库ID

   public int Number{get=>number;set=>number=value;}
	public List<Pokerlibrary> GetPokerlibrary
	{
		get=>list;
		set=>list=value;
	}

}
