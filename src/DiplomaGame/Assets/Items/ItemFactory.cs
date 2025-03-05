using Assets.Items.Ammunition;
using Assets.Items.Armor;
using Assets.Items.Interfaces;
using Assets.Items.Weapon;
using Items.Resource;
using System;

namespace Assets.Items
{
    public static class ItemFactory
    {
        public static IBackpackItem CreateItem(int id, Type itemType)
        {
            if (!typeof(IBackpackItem).IsAssignableFrom(itemType))
                throw new ArgumentException($"Type {itemType.Name} does not implement IBackpackItem");

            if (itemType == typeof(ResourceElement))
            {
                return CreateResourceElement(id);
            }
            if (itemType == typeof(WeaponElement))
            {
                return CreateWeaponElement(id);
            }
            if (itemType == typeof(ArmorElement))
            {
                return CreateArmorElement(id);
            }
            if (itemType == typeof(AmmunitionElement))
            {
                return CreateAmmunitionElement(id);
            }
            throw new ArgumentException($"Unknown item type: {itemType.Name}");
        }

        public static T CreateItem<T>(int id) where T : IBackpackItem
        {
            return (T)CreateItem(id, typeof(T));
        }

        private static ResourceElement CreateResourceElement(int id)
        {
            var resource = ResourceConfig.ResourceElements.Find(x => x.Id == id);
            if (resource == null)
                throw new ArgumentException($"Resource with ID {id} not found");

            return resource;
        }

        private static WeaponElement CreateWeaponElement(int id)
        {
            var weapon = WeaponConfig.WeaponElements.Find(x => x.Id == id);
            if (weapon == null)
                throw new ArgumentException($"Weapon with ID {id} not found");

            return weapon;
        }

        private static ArmorElement CreateArmorElement(int id)
        {
            var armor = ArmorConfig.ArmorElements.Find(x => x.Id == id);
            if (armor == null)
                throw new ArgumentException($"Armor with ID {id} not found");

            return armor;
        }

        private static AmmunitionElement CreateAmmunitionElement(int id)
        {
            var ammo = AmmunitionConfig.AmmunitionElements.Find(x => x.Id == id);
            if (ammo == null)
                throw new ArgumentException($"Ammunition with ID {id} not found");

            return ammo;
        }
    }
}