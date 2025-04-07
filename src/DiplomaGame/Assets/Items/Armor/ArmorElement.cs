using Assets.Items.Interfaces;

namespace Assets.Items.Armor
{
    public class ArmorElement : IBackpackItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float ArmorResistance { get; set; }
    }
}
