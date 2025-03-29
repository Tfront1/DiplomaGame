using System;
using Assets.Items.Crafts;
using Items.Resource.BackPack;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BuildingUIManager : MonoBehaviour
{
    private GameObject _buildingInfoPanel;
    private TextMeshProUGUI _buildingNameText;
    private TextMeshProUGUI _homeTownText;

    private RectTransform _healthFill;
    private TextMeshProUGUI _healthText;

    private TextMeshProUGUI _backpackCapacityText;
    private Transform _backpackItemsContainer;
    private GameObject _backpackItemPrefab;

    private Button _craftsButton;
    private GameObject _craftingPanel;
    private Transform _craftingRecipesContainer;
    private GameObject _craftingRecipePrefab;

    private Canvas _mainCanvas;
    private BuildingItem _currentBuilding;

    private GameObject _requiredResourcesPanel;
    private Transform _requiredResourcesContainer;
    private GameObject _resourceItemPrefab;

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
            InitializeUI();
            HideBuildingInfo();
            HideCraftingPanel();
        }
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (_healthFill != null && _healthText != null)
        {
            var healthPercentage = Mathf.Clamp01(currentHealth / maxHealth);
            _healthFill.anchorMax = new Vector2(healthPercentage, 1);
            _healthText.text = $"{Mathf.RoundToInt(currentHealth)}/{Mathf.RoundToInt(maxHealth)}";
        }
    }

    private void InitializeUI()
    {
        _mainCanvas = MainCanvasUI.MainCanvas;

        if (_buildingInfoPanel == null)
        {
            CreateBuildingUI();
        }
        if (_craftingPanel == null)
        {
            CreateCraftingUI();
        }
        if (_requiredResourcesPanel == null)
        {
            CreateRequiredResourcesUI();
        }
    }

    private void CreateBuildingUI()
    {
        _buildingInfoPanel = new GameObject("BuildingInfoPanel");
        _buildingInfoPanel.transform.SetParent(_mainCanvas.transform, false);

        var panelRect = _buildingInfoPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.75f, 0.5f);
        panelRect.anchorMax = new Vector2(1.0f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = _buildingInfoPanel.AddComponent<Image>();
        panelImage.color = new Color(0.4f, 0.4f, 0.6f, 0.8f);

        var verticalLayout = _buildingInfoPanel.AddComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.spacing = 5;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = false;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;

        CreateHeaderUI();
        CreateHealthBar();
        CreateBackpackUI();
    }

    private void CreateHeaderUI()
    {
        var headerObj = new GameObject("HeaderSection");
        headerObj.transform.SetParent(_buildingInfoPanel.transform, false);

        var headerLayout = headerObj.AddComponent<VerticalLayoutGroup>();
        headerLayout.spacing = 5;

        var nameObj = new GameObject("UnitName");
        nameObj.transform.SetParent(headerObj.transform, false);
        _buildingNameText = nameObj.AddComponent<TextMeshProUGUI>();
        _buildingNameText.fontSize = 24;
        _buildingNameText.alignment = TextAlignmentOptions.Right;
        _buildingNameText.color = Color.white;

        var townObj = new GameObject("HomeTown");
        townObj.transform.SetParent(headerObj.transform, false);
        _homeTownText = townObj.AddComponent<TextMeshProUGUI>();
        _homeTownText.fontSize = 18;
        _homeTownText.alignment = TextAlignmentOptions.Right;
        _homeTownText.color = Color.white;

        var layoutElement = headerObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 120;
        layoutElement.preferredHeight = 120;
    }

    private void CreateBackpackUI()
    {
        var backpackObj = new GameObject("BackpackSection");
        backpackObj.transform.SetParent(_buildingInfoPanel.transform, false);

        var backpackLayout = backpackObj.AddComponent<VerticalLayoutGroup>();
        backpackLayout.spacing = 10;

        var titleObj = new GameObject("BackpackTitle");
        titleObj.transform.SetParent(backpackObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "BACKPACK";
        titleText.fontSize = 20;
        titleText.alignment = TextAlignmentOptions.Right;
        titleText.color = Color.white;

        var capacityObj = new GameObject("BackpackCapacity");
        capacityObj.transform.SetParent(backpackObj.transform, false);
        _backpackCapacityText = capacityObj.AddComponent<TextMeshProUGUI>();
        _backpackCapacityText.fontSize = 16;
        _backpackCapacityText.alignment = TextAlignmentOptions.Right;
        _backpackCapacityText.color = Color.white;

        var containerObj = new GameObject("BackpackItemsContainer");
        containerObj.transform.SetParent(backpackObj.transform, false);

        var containerLayout = containerObj.AddComponent<VerticalLayoutGroup>();
        containerLayout.spacing = 5;

        _backpackItemPrefab = CreateBackpackItemPrefab();

        var layoutElement = backpackObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 150;
        layoutElement.preferredHeight = 150;
        layoutElement.flexibleHeight = 1;

        _backpackItemsContainer = containerObj.transform;
    }

    private void CreateHealthBar()
    {
        var healthBarObj = new GameObject("HealthBar");
        healthBarObj.transform.SetParent(_buildingInfoPanel.transform, false);

        var healthBarRect = healthBarObj.AddComponent<RectTransform>();
        healthBarRect.anchorMin = new Vector2(0, 1);
        healthBarRect.anchorMax = new Vector2(1, 1);
        healthBarRect.pivot = new Vector2(0.5f, 1);
        healthBarRect.anchoredPosition = new Vector2(0, -10);
        healthBarRect.sizeDelta = new Vector2(0, 25);

        var containerObj = new GameObject("HealthBarContainer");
        containerObj.transform.SetParent(healthBarObj.transform, false);

        var layout = containerObj.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.padding = new RectOffset(5, 5, 0, 0);
        layout.spacing = 10;

        var containerRect = containerObj.GetComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        var labelObj = new GameObject("HealthLabel");
        labelObj.transform.SetParent(containerObj.transform, false);

        var labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = "Health:";
        labelText.color = Color.white;
        labelText.fontSize = 16;
        labelText.alignment = TextAlignmentOptions.Left;

        var labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.minWidth = 70;
        labelLayout.preferredWidth = 70;

        var backgroundObj = new GameObject("HealthBarBackground");
        backgroundObj.transform.SetParent(containerObj.transform, false);

        var backgroundImage = backgroundObj.AddComponent<Image>();
        backgroundImage.color = new Color(0.2f, 0.2f, 0.2f);

        var backgroundLayout = backgroundObj.AddComponent<LayoutElement>();
        backgroundLayout.flexibleWidth = 1;
        backgroundLayout.minHeight = 20;

        var fillObj = new GameObject("HealthBarFill");
        fillObj.transform.SetParent(backgroundObj.transform, false);

        var fillImage = fillObj.AddComponent<Image>();
        fillImage.color = Color.red;

        var fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0.75f, 1);
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);

        var valueObj = new GameObject("HealthValue");
        valueObj.transform.SetParent(containerObj.transform, false);

        var valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = "75/100";
        valueText.color = Color.white;
        valueText.fontSize = 16;
        valueText.alignment = TextAlignmentOptions.Right;

        var valueLayout = valueObj.AddComponent<LayoutElement>();
        valueLayout.minWidth = 70;
        valueLayout.preferredWidth = 70;

        _healthFill = fillRect;
        _healthText = valueText;
    }

    private GameObject CreateBackpackItemPrefab()
    {
        var prefab = new GameObject("UnitBackpackItemPrefab");
        prefab.SetActive(false);
        prefab.transform.SetParent(_buildingInfoPanel.transform, false);

        var horizontalLayout = prefab.AddComponent<HorizontalLayoutGroup>();
        horizontalLayout.spacing = 10;
        horizontalLayout.childAlignment = TextAnchor.MiddleLeft;

        var iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(prefab.transform, false);
        var icon = iconObj.AddComponent<Image>();
        icon.color = Color.white;

        var nameObj = new GameObject("ItemName");
        nameObj.transform.SetParent(prefab.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 14;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.color = Color.white;

        var quantityObj = new GameObject("ItemQuantity");
        quantityObj.transform.SetParent(prefab.transform, false);
        var quantityText = quantityObj.AddComponent<TextMeshProUGUI>();
        quantityText.fontSize = 14;
        quantityText.alignment = TextAlignmentOptions.Right;
        quantityText.color = Color.white;

        var iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.minWidth = 30;
        iconLayout.preferredWidth = 30;
        iconLayout.minHeight = 30;
        iconLayout.preferredHeight = 30;

        var nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.flexibleWidth = 1;

        var quantityLayout = quantityObj.AddComponent<LayoutElement>();
        quantityLayout.minWidth = 40;
        quantityLayout.preferredWidth = 40;

        return prefab;
    }

    private void CreateCraftingUI()
    {
        _craftingPanel = new GameObject("CraftingPanel");
        _craftingPanel.transform.SetParent(_mainCanvas.transform, false);

        var panelRect = _craftingPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.25f, 0.1f);
        panelRect.anchorMax = new Vector2(0.75f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = _craftingPanel.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

        var headerObj = new GameObject("CraftingHeader");
        headerObj.transform.SetParent(_craftingPanel.transform, false);

        var headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.sizeDelta = new Vector2(0, 50);

        var headerImage = headerObj.AddComponent<Image>();
        headerImage.color = new Color(0.3f, 0.3f, 0.4f, 1f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(headerObj.transform, false);

        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "AVAILABLE CRAFTING RECIPES";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        var titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        var closeButtonObj = new GameObject("CloseButton");
        closeButtonObj.transform.SetParent(headerObj.transform, false);

        var closeButtonRect = closeButtonObj.AddComponent<RectTransform>();
        closeButtonRect.anchorMin = new Vector2(1, 0.5f);
        closeButtonRect.anchorMax = new Vector2(1, 0.5f);
        closeButtonRect.pivot = new Vector2(1, 0.5f);
        closeButtonRect.anchoredPosition = new Vector2(-15, 0);
        closeButtonRect.sizeDelta = new Vector2(30, 30);

        var closeButtonImage = closeButtonObj.AddComponent<Image>();
        closeButtonImage.color = new Color(0.8f, 0.2f, 0.2f);

        var closeButton = closeButtonObj.AddComponent<Button>();
        closeButton.onClick.AddListener(HideCraftingPanel);

        var closeTextObj = new GameObject("CloseText");
        closeTextObj.transform.SetParent(closeButtonObj.transform, false);

        var closeText = closeTextObj.AddComponent<TextMeshProUGUI>();
        closeText.text = "X";
        closeText.fontSize = 20;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.color = Color.white;

        var closeTextRect = closeText.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        var recipesContainerObj = new GameObject("RecipesContainer");
        recipesContainerObj.transform.SetParent(_craftingPanel.transform, false);

        var recipesContainerRect = recipesContainerObj.AddComponent<RectTransform>();
        recipesContainerRect.anchorMin = new Vector2(0, 0);
        recipesContainerRect.anchorMax = new Vector2(1, 1);
        recipesContainerRect.offsetMin = new Vector2(10, 10);
        recipesContainerRect.offsetMax = new Vector2(-10, -60);

        var scrollRect = recipesContainerObj.AddComponent<ScrollRect>();

        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(recipesContainerObj.transform, false);

        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);

        var verticalLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.spacing = 10;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = false;

        var contentSizeFitter = contentObj.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        _craftingRecipePrefab = CreateCraftingRecipePrefab();
        _craftingRecipesContainer = contentObj.transform;
    }

    private void AddCraftsButton()
    {
        var craftsButtonObj = new GameObject("CraftsButton");
        craftsButtonObj.transform.SetParent(_buildingInfoPanel.transform, false);

        var buttonRect = craftsButtonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 0);
        buttonRect.anchorMax = new Vector2(1, 0);
        buttonRect.pivot = new Vector2(0.5f, 0);
        buttonRect.sizeDelta = new Vector2(0, 40);
        buttonRect.anchoredPosition = new Vector2(0, 10);

        var buttonImage = craftsButtonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.6f, 0.3f);

        _craftsButton = craftsButtonObj.AddComponent<Button>();
        _craftsButton.onClick.AddListener(ShowCraftingPanel);

        var buttonText = new GameObject("ButtonText").AddComponent<TextMeshProUGUI>();
        buttonText.transform.SetParent(craftsButtonObj.transform, false);
        buttonText.text = "CRAFTS";
        buttonText.fontSize = 20;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        var textRect = buttonText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private GameObject CreateCraftingRecipePrefab()
    {
        var prefab = new GameObject("CraftingRecipePrefab");
        prefab.transform.SetParent(_buildingInfoPanel.transform, false);
        prefab.SetActive(false);

        var recipeRect = prefab.AddComponent<RectTransform>();
        recipeRect.sizeDelta = new Vector2(0, 120);

        var recipeBackground = prefab.AddComponent<Image>();
        recipeBackground.color = new Color(0.25f, 0.25f, 0.35f, 1f);

        var titleObj = new GameObject("RecipeTitle");
        titleObj.transform.SetParent(prefab.transform, false);

        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 30);

        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 18;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        var componentsObj = new GameObject("Components");
        componentsObj.transform.SetParent(prefab.transform, false);

        var componentsRect = componentsObj.AddComponent<RectTransform>();
        componentsRect.anchorMin = new Vector2(0, 0.5f);
        componentsRect.anchorMax = new Vector2(1, 0.5f);
        componentsRect.pivot = new Vector2(0.5f, 0.5f);
        componentsRect.sizeDelta = new Vector2(0, 50);

        var componentsLayout = componentsObj.AddComponent<HorizontalLayoutGroup>();
        componentsLayout.padding = new RectOffset(10, 10, 5, 5);
        componentsLayout.spacing = 10;
        componentsLayout.childAlignment = TextAnchor.MiddleCenter;

        var timeObj = new GameObject("CraftingTime");
        timeObj.transform.SetParent(prefab.transform, false);

        var timeRect = timeObj.AddComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0, 0);
        timeRect.anchorMax = new Vector2(0.5f, 0);
        timeRect.pivot = new Vector2(0.5f, 0);
        timeRect.sizeDelta = new Vector2(0, 25);

        var timeText = timeObj.AddComponent<TextMeshProUGUI>();
        timeText.fontSize = 14;
        timeText.alignment = TextAlignmentOptions.Left;
        timeText.color = Color.white;

        var craftButtonObj = new GameObject("CraftButton");
        craftButtonObj.transform.SetParent(prefab.transform, false);

        var craftButtonRect = craftButtonObj.AddComponent<RectTransform>();
        craftButtonRect.anchorMin = new Vector2(0.5f, 0);
        craftButtonRect.anchorMax = new Vector2(1, 0);
        craftButtonRect.pivot = new Vector2(0.5f, 0);
        craftButtonRect.sizeDelta = new Vector2(-20, 25);
        craftButtonRect.anchoredPosition = new Vector2(-10, 10);

        var craftButtonImage = craftButtonObj.AddComponent<Image>();
        craftButtonImage.color = new Color(0.3f, 0.5f, 0.8f);

        var craftButton = craftButtonObj.AddComponent<Button>();

        var craftButtonText = new GameObject("ButtonText").AddComponent<TextMeshProUGUI>();
        craftButtonText.transform.SetParent(craftButtonObj.transform, false);
        craftButtonText.text = "CRAFT";
        craftButtonText.fontSize = 16;
        craftButtonText.alignment = TextAlignmentOptions.Center;
        craftButtonText.color = Color.white;

        var craftButtonTextRect = craftButtonText.GetComponent<RectTransform>();
        craftButtonTextRect.anchorMin = Vector2.zero;
        craftButtonTextRect.anchorMax = Vector2.one;
        craftButtonTextRect.offsetMin = Vector2.zero;
        craftButtonTextRect.offsetMax = Vector2.zero;

        return prefab;
    }

    private void CreateRequiredResourcesUI()
    {
        var resourcesObj = new GameObject("RequiredResourcesSection");
        resourcesObj.transform.SetParent(_buildingInfoPanel.transform, false);

        var resourcesLayout = resourcesObj.AddComponent<VerticalLayoutGroup>();
        resourcesLayout.spacing = 10;

        var titleObj = new GameObject("ResourcesTitle");
        titleObj.transform.SetParent(resourcesObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "REQUIRED RESOURCES";
        titleText.fontSize = 20;
        titleText.alignment = TextAlignmentOptions.Right;
        titleText.color = Color.white;

        var containerObj = new GameObject("ResourcesItemsContainer");
        containerObj.transform.SetParent(resourcesObj.transform, false);

        var containerLayout = containerObj.AddComponent<VerticalLayoutGroup>();
        containerLayout.spacing = 5;

        _resourceItemPrefab = CreateResourceItemPrefab();

        var layoutElement = resourcesObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 150;
        layoutElement.preferredHeight = 150;
        layoutElement.flexibleHeight = 1;

        _requiredResourcesContainer = containerObj.transform;
        _requiredResourcesPanel = resourcesObj;
    }

    private GameObject CreateResourceItemPrefab()
    {
        var prefab = new GameObject("ResourceItemPrefab");
        prefab.SetActive(false);
        prefab.transform.SetParent(_buildingInfoPanel.transform, false);

        var horizontalLayout = prefab.AddComponent<HorizontalLayoutGroup>();
        horizontalLayout.spacing = 10;
        horizontalLayout.childAlignment = TextAnchor.MiddleLeft;

        var iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(prefab.transform, false);
        var icon = iconObj.AddComponent<Image>();
        icon.color = Color.white;

        var nameObj = new GameObject("ItemName");
        nameObj.transform.SetParent(prefab.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 14;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.color = Color.white;

        var quantityObj = new GameObject("ItemQuantity");
        quantityObj.transform.SetParent(prefab.transform, false);
        var quantityText = quantityObj.AddComponent<TextMeshProUGUI>();
        quantityText.fontSize = 14;
        quantityText.alignment = TextAlignmentOptions.Right;
        quantityText.color = Color.white;

        var iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.minWidth = 30;
        iconLayout.preferredWidth = 30;
        iconLayout.minHeight = 30;
        iconLayout.preferredHeight = 30;

        var nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.flexibleWidth = 1;

        var quantityLayout = quantityObj.AddComponent<LayoutElement>();
        quantityLayout.minWidth = 40;
        quantityLayout.preferredWidth = 40;

        return prefab;
    }

    private void UpdateRequiredResourcesUI(List<CraftingComponent> requiredResources, bool isVisible)
    {
        if (_requiredResourcesPanel != null)
        {
            _requiredResourcesPanel.SetActive(isVisible);

            if (!isVisible || requiredResources == null)
            {
                return;
            }

            for (var i = _requiredResourcesContainer.childCount - 1; i >= 0; i--)
            {
                var child = _requiredResourcesContainer.GetChild(i);
                if (!child.IsDestroyed() && child != null)
                {
                    Destroy(child.gameObject);
                }
            }

            if (requiredResources.Count == 0)
            {
                var emptyObj = new GameObject("EmptyResources");
                emptyObj.transform.SetParent(_requiredResourcesContainer, false);
                var emptyText = emptyObj.AddComponent<TextMeshProUGUI>();
                emptyText.text = "No resources required";
                emptyText.fontSize = 16;
                emptyText.alignment = TextAlignmentOptions.Center;
                emptyText.color = Color.white;
                return;
            }

            foreach (var resource in requiredResources)
            {
                var resourceObj = Instantiate(_resourceItemPrefab, _requiredResourcesContainer);
                resourceObj.SetActive(true);

                var nameText = resourceObj.transform.Find("ItemName").GetComponent<TextMeshProUGUI>();
                var quantityText = resourceObj.transform.Find("ItemQuantity").GetComponent<TextMeshProUGUI>();

                nameText.text = resource.BackpackItem.Name;
                quantityText.text = $"x{resource.Quantity}";
            }
        }
    }

    public void ShowCraftingPanel()
    {
        if (_currentBuilding == null || _currentBuilding.Crafts == null || _currentBuilding.Crafts.Count == 0)
        {
            Debug.Log("No crafting recipes available for this building");
            return;
        }

        for (var i = _craftingRecipesContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(_craftingRecipesContainer.GetChild(i).gameObject);
        }

        foreach (var recipe in _currentBuilding.Crafts)
        {
            AddCraftingRecipe(recipe);
        }

        _craftingPanel.SetActive(true);
    }

    public void HideCraftingPanel()
    {
        if (_craftingPanel != null)
        {
            _craftingPanel.SetActive(false);
        }
    }

    private void AddCraftingRecipe(CraftingRecipe recipe)
    {
        var recipeObj = Instantiate(_craftingRecipePrefab, _craftingRecipesContainer);
        recipeObj.SetActive(true);

        var titleText = recipeObj.transform.Find("RecipeTitle").GetComponent<TextMeshProUGUI>();
        titleText.text = recipe.Name;

        var timeText = recipeObj.transform.Find("CraftingTime").GetComponent<TextMeshProUGUI>();
        timeText.text = $"Time: {recipe.CraftingTime} sec";

        var componentsContainer = recipeObj.transform.Find("Components");

        foreach (var component in recipe.Components)
        {
            var componentObj = new GameObject("Component");
            componentObj.transform.SetParent(componentsContainer, false);

            var componentLayout = componentObj.AddComponent<VerticalLayoutGroup>();
            componentLayout.spacing = 2;
            componentLayout.childAlignment = TextAnchor.UpperCenter;

            var componentRect = componentObj.GetComponent<RectTransform>();
            componentRect.sizeDelta = new Vector2(60, 45);

            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(componentObj.transform, false);

            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(30, 30);

            var iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.gray;

            var quantityObj = new GameObject("Quantity");
            quantityObj.transform.SetParent(componentObj.transform, false);

            var quantityText = quantityObj.AddComponent<TextMeshProUGUI>();
            quantityText.text = $"x{component.Quantity}";
            quantityText.fontSize = 12;
            quantityText.alignment = TextAlignmentOptions.Center;
            quantityText.color = Color.white;
        }

        var craftButton = recipeObj.transform.Find("CraftButton").GetComponent<Button>();
        craftButton.onClick.AddListener(() => StartCrafting(recipe));
    }

    private void StartCrafting(CraftingRecipe recipe)
    {
        _currentBuilding.BuildingCraftingSystem.StartCraft(recipe);

        HideCraftingPanel();
    }

    private void UpdateBackpackUI(Backpack backpack)
    {
        for (var i = _backpackItemsContainer.childCount - 1; i >= 0; i--)
        {
            var child = _backpackItemsContainer.GetChild(i);
            if (!child.IsDestroyed() && child != null)
            {
                Destroy(child.gameObject);
            }
        }

        if (backpack == null)
        {
            _backpackCapacityText.text = "No backpack";
            return;
        }

        _backpackCapacityText.text = $"Capacity: {backpack.CurrentCapacity}/{backpack.MaxCapacity}";

        if (backpack.GetAllItems() == null || backpack.GetAllItems().Count == 0)
        {
            var emptyObj = new GameObject("EmptyBackpack");
            emptyObj.transform.SetParent(_backpackItemsContainer, false);
            var emptyText = emptyObj.AddComponent<TextMeshProUGUI>();
            emptyText.text = "Empty";
            emptyText.fontSize = 16;
            emptyText.alignment = TextAlignmentOptions.Center;
            emptyText.color = Color.white;
            return;
        }

        foreach (var itemStack in backpack.GetAllItems())
        {
            var itemObj = Instantiate(_backpackItemPrefab, _backpackItemsContainer);
            itemObj.SetActive(true);

            var nameText = itemObj.transform.Find("ItemName").GetComponent<TextMeshProUGUI>();
            var quantityText = itemObj.transform.Find("ItemQuantity").GetComponent<TextMeshProUGUI>();

            nameText.text = itemStack.Item.Name;
            quantityText.text = $"x{itemStack.Quantity}";
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
        _homeTownText.text = building.HomeTown != null ? $"Hometown: {building.HomeTown.Name}" : "No hometown";

        UpdateBackpackUI(building.Backpack);
        UpdateHealthBar(building.HP, building.Building.MaxHP);

        var showRequiredResources = !building.IsBuilt ||
                                     (building.BuildingCraftingSystem != null &&
                                      building.BuildingCraftingSystem.IsCrafting);

        List<CraftingComponent> requiredResources = new();
        if (showRequiredResources)
        {
            var calculatedResources = new List<CraftingComponent>();

            if (!building.IsBuilt && building.HomeTown != null)
            {
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
                            }
                        }
                    }
                }

                requiredResources = calculatedResources;
            }
            else if (building.IsBuilt && building.BuildingCraftingSystem != null && building.BuildingCraftingSystem.IsCrafting)
            {
                var craftingSystem = building.BuildingCraftingSystem;

                if (craftingSystem.RequiredResources != null && craftingSystem.RequiredResources.Count > 0)
                {
                    var craftRequiredResources = craftingSystem.RequiredResources;
                    var craftDeliveredResources = craftingSystem.DeliveredResources;

                    foreach (var required in craftRequiredResources)
                    {
                        var delivered = craftDeliveredResources?.FirstOrDefault(d => d.BackpackItem.Id == required.BackpackItem.Id);
                        var deliveredQuantity = delivered?.Quantity ?? 0;

                        var backpackQuantity = building.Backpack?.GetResourceQuantity(required.BackpackItem) ?? 0;

                        var remainingQuantity = Math.Max(0, required.Quantity - deliveredQuantity - backpackQuantity);

                        if (remainingQuantity > 0)
                        {
                            calculatedResources.Add(new CraftingComponent(required.BackpackItem, remainingQuantity));
                        }
                    }

                    requiredResources = calculatedResources;
                }
            }
        }

        if (requiredResources.Count == 0)
        {
            showRequiredResources = false;
        }

        UpdateRequiredResourcesUI(requiredResources, showRequiredResources);

        var hasCrafts = building.Crafts != null && building.Crafts.Count > 0 && building.IsBuilt;

        if (_craftsButton == null)
        {
            AddCraftsButton();
        }

        _craftsButton.gameObject.SetActive(hasCrafts);

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
            UpdateBackpackUI(building.Backpack);

            var showRequiredResources = !building.IsBuilt ||
                                        (building.BuildingCraftingSystem != null &&
                                         building.BuildingCraftingSystem.IsCrafting);

            List<CraftingComponent> requiredResources = new();
            if (showRequiredResources)
            {
                if (!building.IsBuilt && building.HomeTown != null)
                {
                    requiredResources = building.HomeTown.BuildingTownOrder.GetOrder(building).RequiredResources;
                }
                else if (building.IsBuilt && building.BuildingCraftingSystem != null && building.BuildingCraftingSystem.IsCrafting)
                {
                    requiredResources = building.BuildingCraftingSystem.RequiredResources;
                }
            }

            if (requiredResources.Count == 0)
            {
                showRequiredResources = false;
            }
            UpdateRequiredResourcesUI(requiredResources, showRequiredResources);
        }
    }

    public void UpdateBuildingInfo(object sender, BuildingItem.BuildingUIToChangeEventArgs args)
    {
        UpdateBuildingInfo(args.Building);
    }
}
