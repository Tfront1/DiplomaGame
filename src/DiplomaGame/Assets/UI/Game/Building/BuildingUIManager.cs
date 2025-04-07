using System;
using Assets.Items.Crafts;
using Items.Resource.BackPack;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingUIManager : MonoBehaviour
{
    private GameObject _buildingPrefab;
    private GameObject _resourcePrefab;

    private GameObject _buildingInfoPanel;
    private TextMeshProUGUI _buildingNameText;
    private TextMeshProUGUI _buildingTownText;

    private TextMeshProUGUI _buildingHP;

    private TextMeshProUGUI _buildingBackpackCount;

    private Transform _resourcePanel;
    private Dictionary<int, GameObject> _buildingResourceItems = new();

    private Canvas _mainCanvas;
    private BuildingItem _currentBuilding;

    private TextMeshProUGUI _actionText;
    private Button _action1Button;
    private Button _action2Button;
    private Image _action1Image;
    private Image _action2Image;

    private static BuildingUIManager _instance;

    public static BuildingUIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BuildingUIManager>();
                if (_instance == null)
                {
                    var gameObject = new GameObject("BuildingUIManager");
                    _instance = gameObject.AddComponent<BuildingUIManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (_buildingPrefab == null)
                _buildingPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Building/BuildingUIPrefab");

            if (_resourcePrefab == null)
                _resourcePrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Resource/ResourceUIPrefab");

            InitializeUI();
            HideBuildingInfo();
        }
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (_buildingHP != null)
        {
            _buildingHP.text = $"HP : {Mathf.RoundToInt(currentHealth)} / {Mathf.RoundToInt(maxHealth)}";
        }
    }

    private void InitializeUI()
    {
        _mainCanvas = MainCanvasUI.MainCanvas;

        var buildingInfoPanel = _buildingPrefab.transform.Find("Canvas/Panel");
        _buildingInfoPanel = Instantiate(buildingInfoPanel.gameObject, _mainCanvas.transform);
        _buildingInfoPanel.name = "BuildingInfoPanel";

        _buildingNameText = _buildingInfoPanel.transform.Find("BuildingName").GetComponent<TextMeshProUGUI>();
        _buildingTownText = _buildingInfoPanel.transform.Find("BuildingTown").GetComponent<TextMeshProUGUI>();

        _buildingHP = _buildingInfoPanel.transform.Find("HP").GetComponent<TextMeshProUGUI>();

        _buildingBackpackCount = _buildingInfoPanel.transform.Find("BackpackCount").GetComponent<TextMeshProUGUI>();

        _actionText = _buildingInfoPanel.transform.Find("BuildingActionText").GetComponent<TextMeshProUGUI>();

        _action1Button = _buildingInfoPanel.transform.Find("ActionButton1").GetComponent<Button>();
        _action1Image = _buildingInfoPanel.transform.Find("ActionButton1").GetComponent<Image>();

        _action2Button = _buildingInfoPanel.transform.Find("ActionButton2").GetComponent<Button>();
        _action2Image = _buildingInfoPanel.transform.Find("ActionButton2").GetComponent<Image>();

        _resourcePanel = _buildingInfoPanel.transform.Find("ScrollView/Viewport/Content");

        var layoutElement = _resourcePanel.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            var viewport = _resourcePanel.transform.Find("ScrollView/Viewport");
            if (viewport != null)
            {
                var viewportRect = viewport.GetComponent<RectTransform>();
                if (viewportRect != null)
                {
                    layoutElement.minHeight = viewportRect.rect.height;
                }
            }
        }
    }

    private void UpdateBackpackUI(Backpack backpack)
    {
        if (backpack == null)
        {
            _buildingBackpackCount.text = "0 / 0";
        }
        else
        {
            _buildingBackpackCount.text = $"{backpack.CurrentCapacity} / {backpack.MaxCapacity}";

            var resourcesToRemove = new HashSet<int>(_buildingResourceItems.Keys);

            foreach (var (key, quantity) in backpack.GetDetailedItems())
            {
                var resourceId = key.Id;
                
                resourcesToRemove.Remove(resourceId);

                if (quantity <= 0)
                {
                    RemoveResourceFromPanel(resourceId);
                }
                else if (_buildingResourceItems.TryGetValue(resourceId, out var resourceItem))
                {
                    var resourceText = resourceItem.GetComponentInChildren<TextMeshProUGUI>();
                    resourceText.text = quantity.ToString();
                }
                else
                {
                    AddResourceToPanel(resourceId, quantity);
                }
            }

            foreach (var resourceId in resourcesToRemove)
            {
                RemoveResourceFromPanel(resourceId);
            }
        }
    }

    private void AddResourceToPanel(int resourceId, int quantity)
    {
        if (quantity <= 0)
            return;

        var resource = _resourcePrefab.transform.Find("Panel").gameObject;

        var resourceItem = Instantiate(resource, _resourcePanel);

        var rectTransform = resourceItem.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(0, 30);
            resourceItem.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        var resourceText = resourceItem.GetComponentInChildren<TextMeshProUGUI>();
        var resourceImage = resourceItem.GetComponentInChildren<Image>();
        //resourceImage.sprite = ResourceManager.Instance.GetResourceSprite(resourceId);

        _buildingResourceItems[resourceId] = resourceItem;
        resourceText.text = quantity.ToString();

        LayoutRebuilder.ForceRebuildLayoutImmediate(_resourcePanel as RectTransform);
    }

    private void RemoveResourceFromPanel(int resourceId)
    {
        if (_buildingResourceItems.TryGetValue(resourceId, out var resourceItem))
        {
            Destroy(resourceItem);
            _buildingResourceItems.Remove(resourceId);
        }
    }

    public void ShowBuildingInfo(BuildingItem building)
    {
        if (building == null)
        {
            HideBuildingInfo();
            return;
        }

        _currentBuilding = building;
        _buildingNameText.text = building.Building.Name;
        _buildingTownText.text = building.HomeTown != null ? $"{building.HomeTown.Name}" : "No hometown";
        UpdateHealthBar(building.HP, building.Building.MaxHP);
        UpdateBackpackUI(building.Backpack);
        UpdateBuildingResourcesInfo(building);

        _buildingInfoPanel.SetActive(true);
    }

    public void HideBuildingInfo()
    {
        _buildingInfoPanel.SetActive(false);
        _currentBuilding = null;
    }

    public void UpdateBuildingInfo(BuildingItem building)
    {
        if (_currentBuilding == building)
        {
            UpdateHealthBar(building.HP, building.Building.MaxHP);
            UpdateBackpackUI(building.Backpack);
            UpdateBuildingResourcesInfo(building);
        }
    }

    private void UpdateBuildingResourcesInfo(BuildingItem building)
    {
        ResetUIElements();

        if (IsUnbuiltBuildingWithResources(building))
        {
            ShowBuildResourcesInfo(building);
        }
        else if (IsCraftingBuilding(building))
        {
            ShowCraftingBuildingInfo(building);
        }
        else if (IsBuiltBuildingWithCrafts(building))
        {
            ShowCraftingOptions(building);
        }
        else if (IsBuiltBuildingWithResources(building))
        {
            ShowStoredResourcesInfo(building);
        }
        else
        {
            HideResourcesInfo();
        }
    }

    private void ResetUIElements()
    {
        _action1Button.gameObject.SetActive(false);
        _action2Button.gameObject.SetActive(false);
        _buildingBackpackCount.gameObject.SetActive(true);
        _resourcePanel.gameObject.SetActive(true);
        _actionText.gameObject.SetActive(true);
    }

    private bool IsUnbuiltBuildingWithResources(BuildingItem building)
    {
        return !building.IsBuilt && building.HomeTown != null;
    }

    private void ShowBuildResourcesInfo(BuildingItem building)
    {
        if (!building.IsAllDelivered)
        {
            var requiredResources = CalculateResourcesToBuild(building);
            _actionText.text = "Resources to build";
            UpdateBackpackUI(requiredResources);
        }
        else
        {
            HideResourcesInfo();
        }
    }

    private bool IsCraftingBuilding(BuildingItem building)
    {
        return building.BuildingCraftingSystem != null && building.BuildingCraftingSystem.IsCrafting;
    }

    private void ShowCraftingBuildingInfo(BuildingItem building)
    {
        _action1Button.gameObject.SetActive(true);
        //_action1Image.sprite = ...

        
            var requiredResources = CalculateResourcesToCraft(building);
            _actionText.text = "Resources for craft";
            UpdateBackpackUI(requiredResources);
        
    }

    private bool IsBuiltBuildingWithCrafts(BuildingItem building)
    {
        return building.IsBuilt && building.Crafts != null && building.Crafts.Count > 0;
    }

    private void ShowCraftingOptions(BuildingItem building)
    {
        _action1Button.gameObject.SetActive(true);

        _action1Button.onClick.RemoveAllListeners();

        _action1Button.onClick.AddListener(() =>
        {
            CraftingMenuManager.Instance.ShowBuildingCrafts(building);
        });

        // _action1Image.sprite = ...

        if (IsBuiltBuildingWithResources(building))
        {
            ShowStoredResourcesInfo(building);
        }
        else
        {
            HideResourcesInfo();
        }
    }

    private bool IsBuiltBuildingWithResources(BuildingItem building)
    {
        return building.IsBuilt && building.Backpack != null;
    }

    private void ShowStoredResourcesInfo(BuildingItem building)
    {
        _actionText.text = "Resources";
        UpdateBackpackUI(building.Backpack);
    }

    private void HideResourcesInfo()
    {
        _actionText.gameObject.SetActive(false);
        _buildingBackpackCount.gameObject.SetActive(false);
        _resourcePanel.gameObject.SetActive(false);
    }

    private Backpack CalculateResourcesToCraft(BuildingItem building)
    {
        var craftingSystem = building.BuildingCraftingSystem;
        var resourceCount = 0;

        var craftRequiredResources = craftingSystem.RequiredResources;
        var craftDeliveredResources = craftingSystem.DeliveredResources;

        var calculatedResources = new List<CraftingComponent>();

        foreach (var required in craftRequiredResources)
        {
            var delivered = craftDeliveredResources?.FirstOrDefault(d => d.BackpackItem.Id == required.BackpackItem.Id);
            var deliveredQuantity = delivered?.Quantity ?? 0;

            var backpackQuantity = building.Backpack?.GetResourceQuantity(required.BackpackItem) ?? 0;

            var remainingQuantity = Math.Max(0, required.Quantity - deliveredQuantity - backpackQuantity);

            if (remainingQuantity > 0)
            {
                calculatedResources.Add(new CraftingComponent(required.BackpackItem, remainingQuantity));
                resourceCount += remainingQuantity;
            }
        }

        var requiredResources = new Backpack(resourceCount);

        foreach (var resource in calculatedResources)
        {
            requiredResources.AddItem(resource.BackpackItem, resource.Quantity);
        }

        return requiredResources;
    }

    private Backpack CalculateResourcesToBuild(BuildingItem building)
    {
        var calculatedResources = new List<CraftingComponent>();
        var resourceCount = 0;

        var townOrder = building.HomeTown.BuildingTownOrder.GetOrder(building);

        if (townOrder != null)
        {
            var requiredBuildingResources = townOrder.RequiredResources;
            var deliveredBuildingResources = townOrder.DeliveredResources;

            if (requiredBuildingResources != null && requiredBuildingResources.Count > 0)
            {
                foreach (var required in requiredBuildingResources)
                {
                    var delivered = deliveredBuildingResources?.FirstOrDefault(d => d.BackpackItem.Id == required.BackpackItem.Id);
                    var deliveredQuantity = delivered?.Quantity ?? 0;

                    var remainingQuantity = Math.Max(0, required.Quantity - deliveredQuantity);

                    if (remainingQuantity > 0)
                    {
                        calculatedResources.Add(new CraftingComponent(required.BackpackItem, remainingQuantity));
                        resourceCount += remainingQuantity;
                    }
                }
            }
        }

        var requiredResources = new Backpack(resourceCount);

        foreach (var resource in calculatedResources)
        {
            requiredResources.AddItem(resource.BackpackItem, resource.Quantity);
        }

        return requiredResources;
    }
}
