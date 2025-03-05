public class Armor : BaseStat
{
    public float _damageReduction;
    public float _damageReductionPerPoint;

    public Armor(float initialArmor, float maxArmor, float damageReductionPerPoint = 0.06f)
        : base("Armor", initialArmor, maxArmor)
    {
        _damageReductionPerPoint = damageReductionPerPoint;
        _damageReduction = CurrentValue * damageReductionPerPoint;
    }

    public new void SetValue(float value)
    {
        base.SetValue(value);
        _damageReduction = CurrentValue * _damageReductionPerPoint;
    }
}