public class BuildingSkill : BaseSkill
{
    public float BuildSpeedBonus => 1.0f + (CurrentLevel * 0.015f);
    public float StructureHealthBonus => 1.0f + (CurrentLevel * 0.01f);
    public override string Name => "Building";

    public BuildingSkill(float level, float maxLevel, float baseExperience) 
        : base("Name", level, maxLevel, baseExperience, 1.15f)
    {
    }
}