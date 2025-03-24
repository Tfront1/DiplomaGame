using System.Collections.Generic;

public class Building
{
	public int Id { get; set; }
	public string Name { get; set; }
	public int WidthCell { get; set; }
	public int HeightCell { get; set; }
	public bool HasMargin { get; set; }
	public float MaxHP { get; set; }
	public int BackpackCapacity { get; set; }
	public BuildingTypes BuildingType { get; set; }
	public bool HasCrafts { get; set; }
    public int BuildingCraftId { get; set; }
    public List<int> CraftsIds { get; set; } = new();

	public enum BuildingTypes
    {
        Construction,
        TownHall,
        Blacksmith,
		Vault,
        Fence,
		None
    }
}
