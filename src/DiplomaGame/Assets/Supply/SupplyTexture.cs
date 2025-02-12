using UnityEngine;

namespace Supplies
{
    public class SupplyTexture
    {
        public int Id { get; set; }
        public int SupplyId { get; set; }
        public Texture2D Texture { get; set; }
        public int VisualWidthCell { get; set; }
        public int VisualHeightCell { get; set; }
    }
}