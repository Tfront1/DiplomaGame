namespace Items.Resource.BackPack
{
    public class ResourceBackpackItem
    {
        public int ResourceId { get; set; }
        public int Quantity { get; set; }

        public ResourceBackpackItem(int resourceId, int quantity)
        {
            ResourceId = resourceId;
            Quantity = quantity;
        }
    }
}