using UnityEngine;

public class SmithingSkill : BaseSkill
{
    public float CraftQualityBonus => 1.0f + (CurrentLevel * 0.02f);
    public float ResourceSavingChance => Mathf.Min(0.5f, CurrentLevel * 0.005f);
    public override string Name => "Smithing";

    public SmithingSkill(float level, float maxLevel, float baseExperience)
        : base("Name", level, maxLevel, baseExperience, 1.15f)
    {
    }

}