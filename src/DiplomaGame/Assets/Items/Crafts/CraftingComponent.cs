using Assets.Items.Interfaces;

namespace Assets.Items.Crafts
{
    public class CraftingComponent
    {
        public IBackpackItem BackpackItem { get; set; }
        public int Quantity { get; set; }
        
        public CraftingComponent(IBackpackItem backpackItem, int quantity)
        {
            BackpackItem = backpackItem;
            Quantity = quantity;
        }
    }
}
