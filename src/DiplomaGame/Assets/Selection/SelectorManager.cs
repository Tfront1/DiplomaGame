using System.Collections.Generic;
using System.Linq;
using GameUtilities.Utils;
using Selection.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace Selection
{
    public class SelectorManager : MonoBehaviour
    {
        private static SelectorManager _instance;
        private static readonly object _lock = new();

        public static HashSet<ISelectable> SelectedItems { get; set; } = new();
        private LayerMask _selectableLayerMask;

        private Vector2 _startSelectPosition;
        private GameObject _selectionAreaVisual;
        private Vector3 _selectionAreaPosition = new();
        private RectTransform _selectionRectTransform;

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

            GameplayInputHandler.Instance.OnMouseLeftClick += SelectItem;
            GameplayInputHandler.Instance.OnMouseLeftHoldStart += StartSelectArea;
            GameplayInputHandler.Instance.OnMouseLeftHoldEnd += EndSelectArea;
            GameplayInputHandler.Instance.OnMouseLeftHold += ContinueSelectingArea;
        }

        private void OnDestroy()
        {
            GameplayInputHandler.Instance.OnMouseLeftClick -= SelectItem;
            GameplayInputHandler.Instance.OnMouseLeftHoldStart -= StartSelectArea;
            GameplayInputHandler.Instance.OnMouseLeftHoldEnd -= EndSelectArea;
            GameplayInputHandler.Instance.OnMouseLeftHold -= ContinueSelectingArea;
        }

        public void SelectItem(Vector2 position)
        {
            DeselectItems();

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
                            SelectUnitGroup(unitItem);
                        }
                        else
                        {
                            SelectedItems.Add(unitItem);
                            unitItem.OnSelect();
                        }
                        return;
                    }

                    var buildingItem = collider.GetComponent<BuildingItem>();
                    if (buildingItem != null)
                    {
                        SelectedItems.Add(buildingItem);
                        buildingItem.OnSelect();
                        return;
                    }

                    var supplyItem = collider.GetComponent<SupplyItem>();
                    if (supplyItem != null)
                    {
                        SelectedItems.Add(supplyItem);
                        supplyItem.OnSelect();
                    }
                }
            }
        }
        
        public List<ISelectable> GetSelectedItem(Vector2 position)
        {
            var result = new List<ISelectable>();

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
                        return result;
                    }

                    var buildingItem = collider.GetComponent<BuildingItem>();
                    if (buildingItem != null)
                    {
                        result.Add(buildingItem);
                        return result;
                    }

                    var supplyItem = collider.GetComponent<SupplyItem>();
                    if (supplyItem != null)
                    {
                        result.Add(supplyItem);
                        return result;
                    }
                }
            }

            return result;
        }

        public void StartSelectArea(Vector2 startPosition)
        {
            _startSelectPosition = startPosition;

            if (_selectionAreaVisual == null)
            {
                _selectionAreaVisual = new GameObject("SelectionArea");

                var canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    var canvasObj = new GameObject("SelectionCanvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.WorldSpace;
                    canvasObj.AddComponent<CanvasScaler>();
                    canvasObj.AddComponent<GraphicRaycaster>();
                }

                _selectionAreaVisual.transform.SetParent(canvas.transform, false);

                _selectionRectTransform = _selectionAreaVisual.AddComponent<RectTransform>();
                var selectionImage = _selectionAreaVisual.AddComponent<Image>();
                selectionImage.color = new Color(0f, 1f, 0f, 0.1f);
            }

            _selectionAreaVisual.SetActive(true);
            _selectionAreaPosition.x = startPosition.x;
            _selectionAreaPosition.y = startPosition.y;
            _selectionAreaPosition.z = -29f;

            _selectionRectTransform.position = _selectionAreaPosition;
            _selectionRectTransform.sizeDelta = Vector2.zero;
        }

        public void EndSelectArea(Vector2 endPosition)
        {
            if (_selectionAreaVisual != null)
            {
                _selectionAreaVisual.SetActive(false);
            }

            if (UtilsClass.CalculateDistance(_startSelectPosition, endPosition) > 1f)
            {
                SelectArea(_startSelectPosition, endPosition);
            }
        }

        public void SelectArea(Vector2 startPosition, Vector2 endPosition, bool unitsOnly = true)
        {
            DeselectItems();

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
                    var unitItem = collider.GetComponent<UnitItem>();
                    if (unitItem != null)
                    {
                        if (unitItem.IsInGroup)
                        {
                            SelectUnitGroup(unitItem);
                        }
                        else
                        {
                            SelectedItems.Add(unitItem);
                            unitItem.OnSelect();
                        }
                    }
                }
                else
                {
                    var buildingItem = collider.GetComponent<BuildingItem>();
                    if (buildingItem != null)
                    {
                        SelectedItems.Add(buildingItem);
                        buildingItem.OnSelect();
                    }
                }
            }
        }

        public void ContinueSelectingArea(Vector2 position)
        {
            if (_selectionAreaVisual != null && _selectionAreaVisual.activeSelf)
            {
                UpdateSelectionRect(_startSelectPosition, position);
            }
        }

        public void DeselectItems()
        {
            foreach (var item in SelectedItems)
            {
                item.OnDeselect();
            }
            SelectedItems.Clear();
        }

        public void ChoseHalfOfSelected()
        {
            var count = SelectedItems.Count;
            var halfCount = count / 2;

            var selectedList = SelectedItems.ToList();

            for (var i = 0; i < halfCount; i++)
            {
                SelectedItems.Remove(selectedList[i]);
            }
        }

        private void UpdateSelectionRect(Vector2 startPos, Vector2 currentPos)
        {
            var center = (startPos + currentPos) / 2;
            var size = new Vector2(
                Mathf.Abs(currentPos.x - startPos.x),
                Mathf.Abs(currentPos.y - startPos.y)
            );

            _selectionAreaPosition.x = center.x;
            _selectionAreaPosition.y = center.y;
            _selectionAreaPosition.z = -29f;

            _selectionRectTransform.position = _selectionAreaPosition;
            _selectionRectTransform.sizeDelta = size;
        }

        private void SelectUnitGroup(UnitItem unit)
        {
            var group = GroupManager.Instance.GetGroup(unit.GroupId);
            foreach (var groupUnit in group.GroupUnits)
            {
                SelectedItems.Add(groupUnit);
                groupUnit.OnSelect();
            }
            SelectedItems.Add(group.UnitLeader);
            group.UnitLeader.OnSelect();
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