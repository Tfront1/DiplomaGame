using Assets.Items.Ammunition;
using Assets.Items.Interfaces;

namespace Assets.Items.Weapon
{
    public class WeaponElement : IBackpackItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float Damage { get; set; }
        public float MissChance { get; set; }
        public float StaminaToAttack { get; set; }
        public float ArmorPenetration { get; set; }
        public float Durability { get; set; }
        public float AttackDistance { get; set; }
        public AmmunitionElement Ammunition { get; set; }
        public float CoolDown { get; set; }
        public bool IsMainWeapon { get; set; }
    }
}
