using UnityEngine;

namespace Unit.PathFinder
{
    public class Node
    {
        public Vector2 GridPosition { get; set; }
        public bool IsWalkable { get; set; }
        public bool HasMargin { get; set; }
        public float GCost { get; set; }
        public float HCost { get; set; }
        public float FCost => GCost + HCost;
        public Node Parent { get; set; }

        public Node(Vector2 gridPos, bool isWalkable, bool hasMargin)
        {
            GridPosition = gridPos;
            IsWalkable = isWalkable;
            HasMargin = hasMargin;
            GCost = float.MaxValue;
            HCost = 0;
        }
    }
}