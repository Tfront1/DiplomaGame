using System;
using System.Collections.Generic;
using Assets.Items.Ammunition;
using Assets.Items.Armor;
using Assets.Items.Interfaces;
using Assets.Items.Weapon;

public class UnitEquipment
{
    public WeaponElement MainWeapon { get; set; }
    public WeaponElement SecondaryWeapon { get; set; }
    public ArmorElement Armor { get; set; }
    public List<AmmunitionElement> Ammunition { get; set; } = new();

    private UnitItem _unit;
    private int _maxAmmunitionCount = 100;

    public UnitEquipment(UnitItem unit)
    {
        _unit = unit;
        MainWeapon = WeaponConfig.WeaponElements.Find(x => x.Id == 1);
        SecondaryWeapon = MainWeapon;
        Armor = ArmorConfig.ArmorElements.Find(x => x.Id == 1);
    }

    public UnitEquipment(UnitItem unit, WeaponElement mainWeapon, WeaponElement secondaryWeapon, ArmorElement armor, List<AmmunitionElement> ammunition)
    {
        _unit = unit;
        MainWeapon = mainWeapon;
        SecondaryWeapon = secondaryWeapon;
        Armor = armor;
        Ammunition = ammunition;
    }

    private void AddAmmunition(AmmunitionElement ammunition, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (Ammunition.Count < _maxAmmunitionCount)
            {
                Ammunition.Add(ammunition);
            }
            else
            {
                break;
            }
        }
    }

    public void ChangeUnitMainWeapon(WeaponElement newWeapon)
    {
        if (newWeapon == null || !newWeapon.IsMainWeapon)
            return;

        var removed = _unit.Backpack.RemoveItem(newWeapon, 1);
        if (!removed)
            return;

        var oldWeapon = MainWeapon;
        MainWeapon = newWeapon;

        if (oldWeapon != null && oldWeapon.Id != 1)
        {
            _unit.Backpack.AddItem(oldWeapon);
        }
    }

    public void ChangeUnitSecondaryWeapon(WeaponElement newWeapon)
    {
        if (newWeapon == null || newWeapon.IsMainWeapon)
            return;

        var removed = _unit.Backpack.RemoveItem(newWeapon, 1);
        if (!removed)
            return;

        var oldWeapon = SecondaryWeapon;
        SecondaryWeapon = newWeapon;

        if (oldWeapon != null && oldWeapon.Id != 1)
        {
            _unit.Backpack.AddItem(oldWeapon);
        }
    }

    public void ChangeUnitArmor(ArmorElement newArmor)
    {
        if (newArmor == null)
            return;

        var removed = _unit.Backpack.RemoveItem(newArmor, 1);
        if (!removed)
            return;

        var oldArmor = Armor;
        Armor = newArmor;

        if (oldArmor != null && oldArmor.Id != 1)
        {
            _unit.Backpack.AddItem(oldArmor);
        }
    }

    public void AddUnitAmmunition(AmmunitionElement ammunition, int quantity = 1)
    {
        if (ammunition == null || quantity <= 0)
            return;

        var canAdd = Math.Min(quantity, _maxAmmunitionCount - Ammunition.Count);

        if (canAdd <= 0)
            return;

        var removed = _unit.Backpack.RemoveItem(ammunition, canAdd);
        if (!removed)
            return;

        AddAmmunition(ammunition, canAdd);
    }

    public void TryAutoEquip(IBackpackItem item, int quantity = 1)
    {
        if (item is WeaponElement weapon)
        {
            if (weapon.IsMainWeapon && MainWeapon.Id == 1)
                ChangeUnitMainWeapon(weapon);
            else if (!weapon.IsMainWeapon && SecondaryWeapon.Id == 1)
                ChangeUnitSecondaryWeapon(weapon);
        }
        else if (item is ArmorElement armor && Armor.Id == 1)
        {
            ChangeUnitArmor(armor);
        }
        else if (item is AmmunitionElement ammo && Ammunition.Count == 0)
        {
            AddUnitAmmunition(ammo, quantity);
        }
    }

    public void UnequipItem(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.MainWeapon:
                if (MainWeapon != null && MainWeapon.Id != 1)
                {
                    var weapon = MainWeapon;
                    if (_unit.Backpack.AddItem(weapon))
                    {
                        MainWeapon = GetDefaultMainWeapon();
                    }
                }
                break;

            case EquipmentSlot.SecondaryWeapon:
                if (SecondaryWeapon != null && SecondaryWeapon.Id != 1)
                {
                    var weapon = SecondaryWeapon;
                    if (_unit.Backpack.AddItem(weapon))
                    {
                        SecondaryWeapon = GetDefaultSecondaryWeapon();
                    }
                }
                break;

            case EquipmentSlot.Armor:
                if (Armor != null && Armor.Id != 1)
                {
                    var armor = Armor;
                    if (_unit.Backpack.AddItem(armor))
                    {
                        Armor = GetDefaultArmor();
                    }
                }
                break;
        }
    }

    public enum EquipmentSlot
    {
        MainWeapon,
        SecondaryWeapon,
        Armor,
        Ammunition
    }

    public WeaponElement GetDefaultMainWeapon()
    {
        return WeaponConfig.WeaponElements.Find(x => x.Id == 1);
    }

    public WeaponElement GetDefaultSecondaryWeapon()
    {
        return WeaponConfig.WeaponElements.Find(x => x.Id == 1);
    }

    public ArmorElement GetDefaultArmor()
    {
        return ArmorConfig.ArmorElements.Find(x => x.Id == 1);
    }
}
