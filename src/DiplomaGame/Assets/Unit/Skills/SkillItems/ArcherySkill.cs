using UnityEngine;

public class ArcherySkill : BaseSkill
{
    public float RangedDamageBonus => 1.0f + (CurrentLevel * 0.02f);
    public float AimPrecisionBonus => 1.0f - Mathf.Min(0.5f, CurrentLevel * 0.005f);
    public override string Name => "Archery";

    public ArcherySkill(float level, float maxLevel, float baseExperience) 
        : base("Archery", level, maxLevel, baseExperience, 1.15f)
    {
    }
}