public class BuildingSkill : BaseSkill
{
    public float BuildSpeedBonus => 1.0f + (CurrentLevel * 0.015f);
    public override string Name => "Building";

    public BuildingSkill(float level, float maxLevel, float baseExperience) 
        : base("Building", level, maxLevel, baseExperience, 1.15f)
    {
    }
}