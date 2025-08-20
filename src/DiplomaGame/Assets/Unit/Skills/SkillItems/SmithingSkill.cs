public class SmithingSkill : BaseSkill
{
    public float CraftSpeedBonus => 1.0f + (CurrentLevel * 0.015f);
    public override string Name => "Smithing";

    public SmithingSkill(float level, float maxLevel, float baseExperience)
        : base("Smithing", level, maxLevel, baseExperience, 1.15f)
    {
    }

}