using UnityEngine;

public class SwordsmanshipSkill : BaseSkill
{
    public float DamageBonus => 1.0f + (CurrentLevel * 0.02f);
    public float CriticalHitChance => Mathf.Min(0.3f, CurrentLevel * 0.003f);
    public override string Name => "Swordsmanship";

    public SwordsmanshipSkill(float level, float maxLevel, float baseExperience)
        : base("Name", level, maxLevel, baseExperience, 1.15f)
    {
    }
}