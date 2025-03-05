using Assets.Items.Interfaces;

namespace Items.Resource.BackPack
{
    public class BackpackItem
    {
        public IBackpackItem Item { get; set; }
        public int Quantity { get; set; }
        
        public BackpackItem(IBackpackItem item, int quantity = 1)
        {
            Item = item;
            Quantity = quantity;
        }
    }
}