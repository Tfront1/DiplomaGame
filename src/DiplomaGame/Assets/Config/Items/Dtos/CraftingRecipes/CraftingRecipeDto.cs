using System.Collections.Generic;

[System.Serializable]
public class CraftingRecipeDto
{
    public int Id;
    public string Name;
    public List<CraftingComponentDto> Components;
    public float CraftingTime;
    public string ResultType;
    public int ResultId;
}
