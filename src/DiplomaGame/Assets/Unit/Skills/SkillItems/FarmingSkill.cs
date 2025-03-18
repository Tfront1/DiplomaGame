public class FarmingSkill : BaseSkill
{
    public override string Name => "Farming";
    public float CropYieldBonus => 1.0f + (CurrentLevel * 0.01f);

    public FarmingSkill(float level, float maxLevel, float baseExperience)
        : base("Farming", level, maxLevel, baseExperience, 1.15f)
    {
    }
}
