using Assets.Items.Interfaces;

namespace Items.Resource
{
    public class ResourceElement : IBackpackItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
