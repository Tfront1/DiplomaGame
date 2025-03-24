using System.Collections.Generic;
using Selection.Interfaces;
using StateMachine;
using Unity.VisualScripting;
using UnityEngine;

namespace Selection
{
    public class SelectorManager : MonoBehaviour
    {
        private static SelectorManager _instance;
        private static readonly object _lock = new();
        private LayerMask _selectableLayerMask;

        public static SelectorManager Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new SelectorManager();
                    }
                    return _instance;
                }
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _selectableLayerMask = LayerMask.GetMask("Units", "Objects");
        }

        public (HashSet<ISelectable>, SelectedStates) GetSelectedItem(Vector2 position)
        {
            HashSet<ISelectable> result = new();

            var hit = Physics2D.OverlapPoint(position, _selectableLayerMask);
            if (hit != null)
            {
                var collider = hit.GetComponent<BoxCollider2D>();

                if (collider)
                {
                    var unitItem = collider.GetComponent<UnitItem>();
                    if (unitItem != null)
                    {
                        if (unitItem.IsInGroup)
                        {
                            result.AddRange(GetSelectedUnitGroup(unitItem));
                        }
                        else
                        {
                            result.Add(unitItem);
                        }
                        return (result, SelectedStates.UnitSelect);
                    }

                    var buildingItem = collider.GetComponent<BuildingItem>();
                    if (buildingItem != null)
                    {
                        result.Add(buildingItem);
                        return (result, SelectedStates.BuildingSelect);
                    }

                    var supplyItem = collider.GetComponent<SupplyItem>();
                    if (supplyItem != null)
                    {
                        result.Add(supplyItem);
                        return (result, SelectedStates.SupplySelect);
                    }
                }
            }

            return (result, SelectedStates.NothingSelect);
        }

        public (HashSet<ISelectable>, SelectedStates) GetSelectedArea(Vector2 startPosition, Vector2 endPosition, bool unitsOnly = true)
        {
            HashSet<ISelectable> result = new();
            var state = SelectedStates.NothingSelect;

            var selectionRect = new Rect
            {
                min = new Vector2(Mathf.Min(startPosition.x, endPosition.x), Mathf.Min(startPosition.y, endPosition.y)),
                max = new Vector2(Mathf.Max(startPosition.x, endPosition.x), Mathf.Max(startPosition.y, endPosition.y))
            };

            var colliders = Physics2D.OverlapAreaAll(selectionRect.min, selectionRect.max, _selectableLayerMask);

            foreach (var collider in colliders)
            {
                if (unitsOnly)
                {
                    state = SelectedStates.UnitSelect;

                    var unitItem = collider.GetComponent<UnitItem>();
                    if (unitItem != null)
                    {
                        if (unitItem.IsInGroup)
                        {
                            result.AddRange(GetSelectedUnitGroup(unitItem));
                        }
                        else
                        {
                            result.Add(unitItem);
                            unitItem.OnSelect();
                        }
                    }
                }
                else
                {
                    state = SelectedStates.BuildingSelect;

                    var buildingItem = collider.GetComponent<BuildingItem>();
                    if (buildingItem != null)
                    {
                        result.Add(buildingItem);
                    }
                }
            }

            if (result.Count == 0)
            {
                state = SelectedStates.NothingSelect;
            }

            return (result, state);
        }
        
        private List<ISelectable> GetSelectedUnitGroup(UnitItem unit)
        {
            var group = GroupManager.Instance.GetGroup(unit.GroupId);
            var selectedUnits = new List<ISelectable>();
            foreach (var groupUnit in group.GroupUnits)
            {
                selectedUnits.Add(groupUnit);
            }
            selectedUnits.Add(group.UnitLeader);

            return selectedUnits;
        }
    }
}