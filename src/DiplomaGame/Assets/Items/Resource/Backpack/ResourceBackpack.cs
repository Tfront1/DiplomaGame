using System.Collections.Generic;
using System.Linq;

namespace Items.Resource.BackPack
{
    public class ResourceBackpack
    {
        private List<ResourceBackpackItem> _items;
        private readonly int _maxCapacity;
        private int _currentCapacity;

        public ResourceBackpack(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
            _currentCapacity = 0;
            _items = new List<ResourceBackpackItem>();
        }

        public bool AddResource(int resourceId, int quantity)
        {
            if (quantity <= 0)
                return false;

            if (_currentCapacity + quantity > _maxCapacity)
                return false;

            if (ResourceConfig.ResourceElements.Find(x => x.Id == resourceId) == null)
                return false;

            var existingItem = _items.FirstOrDefault(item => item.ResourceId == resourceId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _items.Add(new ResourceBackpackItem(resourceId, quantity));
            }

            _currentCapacity += quantity;
            return true;
        }

        public bool RemoveResource(int resourceId, int quantity)
        {
            if (quantity <= 0)
                return false;

            var existingItem = _items.FirstOrDefault(item => item.ResourceId == resourceId);

            if (existingItem == null)
                return false;

            if (existingItem.Quantity < quantity)
                return false;

            existingItem.Quantity -= quantity;
            _currentCapacity -= quantity;

            if (existingItem.Quantity == 0)
                _items.Remove(existingItem);

            return true;
        }

        public int GetResourceQuantity(int resourceId)
        {
            var item = _items.FirstOrDefault(item => item.ResourceId == resourceId);
            return item?.Quantity ?? 0;
        }

        public bool HasResource(int resourceId, int quantity)
        {
            return GetResourceQuantity(resourceId) >= quantity;
        }

        public List<ResourceBackpackItem> GetAllItems()
        {
            return _items;
        }

        public int CurrentCapacity => _currentCapacity;

        public int MaxCapacity => _maxCapacity;

        public float GetFillPercentage()
        {
            return (float)_currentCapacity / _maxCapacity;
        }

        public List<(ResourceElement Resource, int Quantity)> GetDetailedItems()
        {
            var detailedItems = new List<(ResourceElement, int)>();

            foreach (var item in _items)
            {
                var resourceElement = ResourceConfig.ResourceElements.Find(x => x.Id == item.ResourceId);
                if (resourceElement != null)
                {
                    detailedItems.Add((resourceElement, item.Quantity));
                }
            }

            return detailedItems;
        }

        public bool IsFull()
        {
            return _currentCapacity >= _maxCapacity;
        }

        public void Clear()
        {
            _items.Clear();
            _currentCapacity = 0;
        }
    }
}
