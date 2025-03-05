using Assets.Items.Crafts;
using Assets.Items.Interfaces;

namespace Assets.Items.Ammunition
{
    public class AmmunitionElement : IBackpackItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float Damage { get; set; }
        public float ArmorPenetration { get; set; }
        public CraftingRecipe CraftingRecipe { get; set; }
    }
}
