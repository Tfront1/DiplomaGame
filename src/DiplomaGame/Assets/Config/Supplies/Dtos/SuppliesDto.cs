using System.Collections.Generic;

[System.Serializable]
public class SuppliesDto
{
    public int SupplyPerBlocks;
    public int MinSupplyResources;
    public int MaxSupplyResources;
	public List<SupplyDto> Supplies;
}
