using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Items.Interfaces;
using UnityEngine;

namespace Items.Resource.BackPack
{
    public class Backpack
    {
        private List<BackpackItem> _items;
        private int _maxCapacity;
        private int _currentCapacity;

        public int CurrentCapacity => _currentCapacity;
        public int MaxCapacity => _maxCapacity;

        public event EventHandler<BackpackChangedEventArgs> BackpackChanged;

        public Backpack(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
            _currentCapacity = 0;
            _items = new List<BackpackItem>();
        }
        
        public bool AddItem(IBackpackItem backpackItem, int quantity = 1)
        {
            if (quantity <= 0)
                return false;

            if (_currentCapacity + quantity > _maxCapacity)
                return false;

            var existingItem = _items.FirstOrDefault(item => item.Item.Id == backpackItem.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _items.Add(new BackpackItem(backpackItem, quantity));
            }

            _currentCapacity += quantity;

            OnBackpackChanged(new BackpackChangedEventArgs(backpackItem, quantity));

            return true;
        }

        public bool RemoveResource(IBackpackItem backpackItem, int quantity)
        {
            if (quantity <= 0)
                return false;

            var existingItem = _items.FirstOrDefault(item => item.Item.Id == backpackItem.Id);

            if (existingItem == null)
                return false;

            if (existingItem.Quantity < quantity)
                return false;

            existingItem.Quantity -= quantity;
            _currentCapacity -= quantity;

            if (existingItem.Quantity == 0)
                _items.Remove(existingItem);

            Debug.Log(ToString());
            OnBackpackChanged(new BackpackChangedEventArgs(backpackItem, quantity));

            return true;
        }

        public int GetResourceQuantity(IBackpackItem backpackItem)
        {
            var item = _items.FirstOrDefault(item => item.Item.Id == backpackItem.Id);
            return item?.Quantity ?? 0;
        }

        public bool HasResourceCount(IBackpackItem backpackItem, int quantity)
        {
            return GetResourceQuantity(backpackItem) >= quantity;
        }

        public bool HasResource(IBackpackItem backpackItem)
        {
            return GetResourceQuantity(backpackItem) >= 1;
        }

        public List<BackpackItem> GetAllItems()
        {
            return _items;
        }

        public float GetFillPercentage()
        {
            return (float)_currentCapacity / _maxCapacity;
        }

        public int GetFreeQuantity()
        {
            return MaxCapacity - CurrentCapacity;
        }
        
        public Dictionary<IBackpackItem, int> GetDetailedItems()
        {
            var detailedItems = new Dictionary<IBackpackItem, int>();

            foreach (var item in _items)
            {
                detailedItems.Add(item.Item, item.Quantity);
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

        public bool FillWithSingleItem(IBackpackItem backpackItem)
        {
            Clear();
            return AddItem(backpackItem, MaxCapacity);
        }

        public void SetMaxCapacity(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
            OnBackpackChanged(new BackpackChangedEventArgs(null, 0));
        }

        public override string ToString()
        {
            var res = $"Backpack ({_currentCapacity}/{_maxCapacity}): ";

            if (_items == null || _items.Count == 0)
            {
                res += "empty";
            }
            else
            {
                res += string.Join(", ", _items.Select(item =>
                    $"{item.Item.Name} x{item.Quantity}"));
            }

            return res;
        }

        public class BackpackChangedEventArgs : EventArgs
        {
            public IBackpackItem Item { get; }
            public int Quantity { get; }

            public BackpackChangedEventArgs(IBackpackItem item, int quantity)
            {
                Item = item;
                Quantity = quantity;
            }
        }

        protected virtual void OnBackpackChanged(BackpackChangedEventArgs e)
        {
            BackpackChanged?.Invoke(this, e);
        }
    }
}
