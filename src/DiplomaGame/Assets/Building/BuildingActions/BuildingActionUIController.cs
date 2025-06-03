using Items.Resource.BackPack;
using System;
using System.Collections.Generic;
using Assets.Items.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BuildingAction
{
    public class BuildingActionUIController : MonoBehaviour
    {
        private BuildingItem _building;
        private BuildingController _controller;

        public GameObject _buildingActionPrefab;
        public GameObject _buildingActionItemPrefab;

        private Transform _buildingActionCanvas;
        public GraphicRaycaster CanvasRaycaster { get; private set; }

        private Transform _buildingActionPanel;

        public List<Transform> _actions = new();

        private Dictionary<Type, int> _actionOrder = new()
        {
            { typeof(MoveToBuildingAction), 0 },
            { typeof(AttackBuildingAction), 1 },
            { typeof(BringResourcesBuildingAction), 2 },
            { typeof(BuildStructureAction), 3 },
            { typeof(CraftItemBuildingAction), 4 },
            { typeof(MoveAndEquipUnitAction), 5}
        };

        public GameObject _resourcePrefab;
        public GameObject _equipPanelPrefab;

        private static Transform _resourceContent;
        private static Transform _resourcePanel = null;

        private static Dictionary<IBackpackItem, GameObject> _buildingResourceItems = new();

        private Canvas _mainCanvas;

        public void Initialize(BuildingItem building, BuildingController controller)
        {
            _building = building;
            _controller = controller;

            _buildingActionPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Building/BuildingActionPrefab");
            _buildingActionItemPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Building/BuildingActionItemPrefab");
            _resourcePrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Resource/ResourceUIPrefab");
            _equipPanelPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Building/EquipPanelPrefab");

            _mainCanvas = MainCanvasUI.MainCanvas;

            InitializeUI();
        }

        public void OpenActionPanel()
        {
            _buildingActionCanvas.gameObject.SetActive(true);
        }

        public void CloseActionPanel()
        {
            _buildingActionCanvas.gameObject.SetActive(false);
            _resourcePanel.gameObject.SetActive(false);
        }

        public void SetActions(List<Type> actions)
        {
            foreach (var action in _actions)
            {
                if (action != null)
                    Destroy(action.gameObject);
            }
            _actions.Clear();

            var sortedActions = new List<Type>(actions);
            sortedActions.Sort((a, b) =>
            {
                var orderA = _actionOrder.GetValueOrDefault(a, _actionOrder.Count + 1);
                var orderB = _actionOrder.GetValueOrDefault(b, _actionOrder.Count + 1);
                return orderA.CompareTo(orderB);
            });

            foreach (var actionType in sortedActions)
            {
                CreateActionButton(actionType);
            }
        }

        public void UpdateUIScale()
        {
            if (_buildingActionCanvas != null && _building != null)
            {
                var rect = _building.gameObject.GetComponent<RectTransform>();
                if (rect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                }

                var bottomCenter = _building.SpriteRenderer.bounds.center;
                bottomCenter.y = _building.SpriteRenderer.bounds.min.y;

                var topCenter = _building.SpriteRenderer.bounds.center;
                topCenter.y = _building.SpriteRenderer.bounds.max.y;

                if (GridService.IsWorldPositionInMapBounds(bottomCenter))
                {
                    _buildingActionCanvas.position = bottomCenter;
                }
                else if (GridService.IsWorldPositionInMapBounds(topCenter))
                {
                    _buildingActionCanvas.position = topCenter;
                }

                var parent = _building.BuildingGameObject.transform;
                var parentScale = parent.lossyScale;

                _buildingActionCanvas.localScale = new Vector3(
                    1 / parentScale.x,
                    1 / parentScale.y,
                    1 / parentScale.z
                );
            }
        }

        private void CreateActionButton(Type actionType)
        {
            var buildingActionItemPrefab = _buildingActionItemPrefab.transform.Find("Image");

            var actionItem = Instantiate(buildingActionItemPrefab, _buildingActionPanel);

            
            var iconComponent = actionItem.GetComponentInChildren<Image>();
            if (iconComponent != null)
            {
                iconComponent.sprite = UITextureManager.Instance.GetBuildingActionSprite(actionType);
                actionItem.localScale = new Vector3(MapConfig.CellSize, MapConfig.CellSize);
            }
            

            var button = actionItem.GetComponent<Button>();
            if (button != null)
            {
                if (actionType == typeof(MoveAndEquipUnitAction))
                {
                    button.onClick.AddListener(OnEquipButtonClicked);
                }
                else
                {
                    button.onClick.AddListener(() => OnActionButtonClicked(actionType));
                }
            }

            _actions.Add(actionItem.transform);
        }

        private void OnActionButtonClicked(Type actionType)
        {
            _controller.ExecuteAction(actionType);
            CloseActionPanel();
        }

        private void OnEquipButtonClicked()
        {
            if (_building.Backpack != null)
            {
                UpdateBackpackUI(_building.Backpack);
                _resourcePanel.gameObject.SetActive(true);
            }
        }

        private void InitializeUI()
        {
            var buildingAction = _buildingActionPrefab.transform.Find("Canvas");
            _buildingActionCanvas = Instantiate(buildingAction, _building.BuildingGameObject.transform);
            _buildingActionCanvas.name = "BuildingAction";
            _buildingActionCanvas.gameObject.SetActive(false);

            var bottomCenter = _building.SpriteRenderer.bounds.center;
            bottomCenter.y = _building.SpriteRenderer.bounds.min.y;

            var topCenter = _building.SpriteRenderer.bounds.center;
            topCenter.y = _building.SpriteRenderer.bounds.max.y;

            if (GridService.IsWorldPositionInMapBounds(bottomCenter))
            {
                _buildingActionCanvas.position = bottomCenter;
            }
            else if (GridService.IsWorldPositionInMapBounds(topCenter))
            {
                _buildingActionCanvas.position = topCenter;
            }

            var parent = _building.BuildingGameObject.transform;
            var parentScale = parent.lossyScale;

            _buildingActionCanvas.localScale = new Vector3(
                1 / parentScale.x,
                1 / parentScale.y,
                1 / parentScale.z
            );

            _buildingActionPanel = _buildingActionCanvas.Find("Panel");

            var resourcePanel = _equipPanelPrefab.transform.Find("Canvas/Panel");

            if (_resourcePanel == null)
            {
                _resourcePanel = Instantiate(resourcePanel, _mainCanvas.transform);
                _resourcePanel.name = "ItemsToEquip";
                _resourceContent = _resourcePanel.transform.Find("ScrollView/Viewport/Content");
                _resourcePanel.gameObject.SetActive(false);
            }
           
            
            CanvasRaycaster = _buildingActionCanvas.GetComponent<GraphicRaycaster>();
        }

        private void UpdateBackpackUI(Backpack backpack)
        {
            var resourcesToRemove = new HashSet<IBackpackItem>(_buildingResourceItems.Keys);

            foreach (var (resource, quantity) in backpack.GetDetailedItems())
            {
                resourcesToRemove.Remove(resource);

                if (quantity <= 0)
                {
                    RemoveResourceFromPanel(resource);
                }
                else if (_buildingResourceItems.TryGetValue(resource, out var resourceItem))
                {
                    var resourceText = resourceItem.GetComponentInChildren<TextMeshProUGUI>();
                    resourceText.text = quantity.ToString();
                }
                else
                {
                    var isElementExists = false;

                    isElementExists = WeaponConfig.WeaponElements.Find(item => item.Equals(resource)) != null;

                    if (!isElementExists)
                        isElementExists = ArmorConfig.ArmorElements.Find(item => item.Equals(resource)) != null;

                    if (!isElementExists)
                        isElementExists = AmmunitionConfig.AmmunitionElements.Find(item => item.Equals(resource)) != null;

                    if (isElementExists)
                    {
                        AddResourceToPanel(resource, quantity);
                    }
                }
            }

            foreach (var resource in resourcesToRemove)
            {
                RemoveResourceFromPanel(resource);
            }
        }

        private void AddResourceToPanel(IBackpackItem resource, int quantity)
        {
            if (quantity <= 0)
                return;

            var resourcePanel = _resourcePrefab.transform.Find("Panel").gameObject;

            var resourceItem = Instantiate(resourcePanel, _resourceContent);

            var rectTransform = resourceItem.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(0, 30);
                resourceItem.transform.localScale = new Vector3(1f, 1f, 1f);
            }

            var resourceText = resourceItem.GetComponentInChildren<TextMeshProUGUI>();
            var resourceImage = resourceItem.transform.Find("ResourceImage").GetComponent<Image>();
            resourceImage.sprite = UITextureManager.Instance.GetResourceSprite(resource);

            _buildingResourceItems[resource] = resourceItem;
            resourceText.text = quantity.ToString();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_resourceContent as RectTransform);

            var resourceButton = resourceItem.GetComponent<Button>();
            resourceButton.onClick.AddListener(() =>
            {
                _controller.Item = resource;
                OnActionButtonClicked(typeof(MoveAndEquipUnitAction));
            });
        }

        private void RemoveResourceFromPanel(IBackpackItem resource)
        {
            if (_buildingResourceItems.TryGetValue(resource, out var resourceItem))
            {
                Destroy(resourceItem);
                _buildingResourceItems.Remove(resource);
            }
        }
    }
}
