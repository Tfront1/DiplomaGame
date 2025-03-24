using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Items.Weapon;
using UnityEngine;

public static partial class ConfigLoader
{
    public static void LoadWeaponsConfig()
    {
        var json = File.ReadAllText(ConfigPaths.WeaponsConfig);
        var weaponsDto = JsonUtility.FromJson<WeaponsDto>(json);
        
        if (weaponsDto == null)
        {
            Debug.Log("Error weapons config");
            return;
        }

        var nonPositiveIds = weaponsDto.Weapons
            .Where(x => x.Id <= 0)
            .Select(x => x.Id)
            .ToList();

        if (nonPositiveIds.Any())
        {
            throw new Exception($"Weapons Id must be positive. Found non-positive IDs: {string.Join(", ", nonPositiveIds)}");
        }

        var hasDuplicates = weaponsDto.Weapons
            .GroupBy(x => x.Id)
            .Any(group => group.Count() > 1);

        var repeatedIds = weaponsDto.Weapons
            .GroupBy(x => x.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (hasDuplicates)
        {
            throw new Exception($"Weapons Id repeats: {repeatedIds}");
        }

        var weaponsWithMissingAmmunitionErrors = ValidateWeaponAmmunition(weaponsDto);

        if (weaponsWithMissingAmmunitionErrors != null)
        {
            throw new Exception(string.Join("\n", weaponsWithMissingAmmunitionErrors));
        }

        weaponsDto.Weapons.ForEach(x => WeaponConfig.WeaponElements.Add(new WeaponElement()
        {
            Id = x.Id,
            Name = x.Name,
            Damage = x.Damage,
            MissChance = x.MissChance,
            StaminaToAttack = x.StaminaToAttack,
            ArmorPenetration = x.ArmorPenetration,
            Durability = x.Durability,
            AttackDistance = x.AttackDistance,
            Ammunition = x.AmmunitionId > 0 ? AmmunitionConfig.AmmunitionElements.Find(a => a.Id == x.AmmunitionId) : null,
            CoolDown = x.CoolDown,
            IsMainWeapon = x.IsMainWeapon
        }));

        Debug.Log("Weapons config loaded");
    }

    private static List<string> ValidateWeaponAmmunition(WeaponsDto weaponsDto)
    {
        var errors = new List<string>();

        var weaponsWithMissingAmmunition = weaponsDto.Weapons
            .Where(weapon => weapon.AmmunitionId != 0 && AmmunitionConfig.AmmunitionElements
                .All(ammo => ammo.Id != weapon.AmmunitionId))
            .ToList();

        if (weaponsWithMissingAmmunition.Any())
        {
            errors.Add($"Ammunition in weapons: {string.Join(", ", weaponsWithMissingAmmunition.Select(w => w.Name))} not exists in AmmunitionConfig");
        }

        return errors.Any() ? errors : null;
    }
}
