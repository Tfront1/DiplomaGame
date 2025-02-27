public class Armor : BaseStat
{
    public float DamageReduction { get; private set; }
    public float DamageReductionPerPoint { get; private set; }

    public Armor(float initialArmor, float maxArmor, float damageReductionPerPoint = 0.06f)
        : base(initialArmor, maxArmor)
    {
        DamageReductionPerPoint = damageReductionPerPoint;
        DamageReduction = CurrentValue * damageReductionPerPoint;
    }

    public new void SetValue(float value)
    {
        base.SetValue(value);
        DamageReduction = CurrentValue * DamageReductionPerPoint;
    }
}