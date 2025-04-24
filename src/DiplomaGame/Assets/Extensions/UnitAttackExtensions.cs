using Assets.Items.Weapon;
using UnityEngine;

public static class UnitAttackExtensions
{
    public static float _durabilityPerHit = 1f;
    public static float _criticalMultiplier = 1.5f;

    private static bool PrepareWeaponForAttack(this UnitItem unit, WeaponElement weapon)
    {
        var ammunition = unit.UnitEquipment.Ammunition;

        if (weapon.Ammunition != null)
        {
            if (ammunition == null || !ammunition.Contains(weapon.Ammunition))
                return false;

            ammunition.Remove(weapon.Ammunition);
        }

        return true;
    }

    private static WeaponElement GetWeapon(this UnitItem unit, bool useMainWeapon)
    {
        return useMainWeapon ? unit.UnitEquipment.MainWeapon : unit.UnitEquipment.SecondaryWeapon;
    }

    private static float CalculateDamage(this UnitItem attacker, WeaponElement weapon)
    {
        var missChance = weapon.MissChance;

        if (Random.value < missChance)
        {
            return 0;
        }
        
        var baseDamage = weapon.Damage;

        float damageMultiplier;
        float criticalChance;

        if (weapon.Ammunition != null)
        {
            baseDamage += weapon.Ammunition.Damage;
            damageMultiplier = attacker.Skills.GetSkill<ArcherySkill>().RangedDamageBonus;
            criticalChance = attacker.Skills.GetSkill<ArcherySkill>().CriticalHitChance;
        }
        else
        {
            damageMultiplier = attacker.Skills.GetSkill<SwordsmanshipSkill>().DamageBonus;
            criticalChance = attacker.Skills.GetSkill<SwordsmanshipSkill>().CriticalHitChance;
        }

        var damage = baseDamage * damageMultiplier;

        if (Random.value < criticalChance)
        {
            damage *= _criticalMultiplier;
        }

        return damage;
    }

    public static void AttackBuilding(this UnitItem unit, BuildingItem target, bool attackWithMainWeapon = true)
    {
        if (target == null || target.IsDestroyed)
        {
            return;
        }

        var weapon = unit.GetWeapon(attackWithMainWeapon);

        if (!unit.PrepareWeaponForAttack(weapon))
            return;

        var damage = unit.CalculateDamage(weapon);

        ApplyDamageToBuilding(unit, target, damage);

        unit.Stats.Stamina.Modify(-weapon.StaminaToAttack);
    }

    public static void AttackUnit(this UnitItem unit, UnitItem target, bool attackWithMainWeapon = true)
    {
        if (target == null || target.IsDestroyed)
        {
            return;
        }

        var weapon = unit.GetWeapon(attackWithMainWeapon);

        if (!unit.PrepareWeaponForAttack(weapon))
            return;

        var damage = unit.CalculateDamage(weapon);

        ApplyDamageToUnit(unit, target, weapon, damage);

        unit.Stats.Stamina.Modify(-weapon.StaminaToAttack);
    }

    private static void ApplyDamageToBuilding(UnitItem attacker, BuildingItem target, float damage)
    {
        var effectiveDamage = CalculateEffectiveDamageToBuilding(damage, target);

        target.ApplyDamage(effectiveDamage);
    }

    private static void ApplyDamageToUnit(UnitItem attacker, UnitItem target, WeaponElement weapon, float damage)
    {
        var effectiveDamage = CalculateEffectiveDamageToUnit(weapon, damage, target);

        target.ApplyDamage(effectiveDamage);
    }

    private static float CalculateEffectiveDamageToBuilding(float rawDamage, BuildingItem building)
    {
        return rawDamage / 2;
    }
    
    private static float CalculateEffectiveDamageToUnit(WeaponElement weapon, float rawDamage, UnitItem target)
    {
        var armorPenetration = weapon.ArmorPenetration;
        if (weapon.Ammunition != null)
        {
            armorPenetration += weapon.Ammunition.ArmorPenetration;
        }

        var effectiveResistance = target.UnitEquipment.Armor.ArmorResistance * (1 - armorPenetration);
        var damageReduction = effectiveResistance / 100f;
        damageReduction = Mathf.Min(damageReduction, 0.9f);

        return rawDamage * (1 - damageReduction);
    }
}