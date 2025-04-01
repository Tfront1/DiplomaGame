using Items.Resource.BackPack;
using System;
using System.Collections.Generic;
using Assets.Items.Crafts;
using Selection.Interfaces;
using Town;
using UnityEngine;
using UnityEngine.UI;
using static Items.Resource.BackPack.Backpack;

public class BuildingItem : MonoBehaviour, IItemListObject, ISelectable
{
    public Guid Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public Vector2Int Coords => new(X, Y);
    public Vector2 CenterCoords => new(X + Building.WidthCell / 2.0f, Y + Building.HeightCell / 2.0f);
    public Building Building { get; set; }
    public GameObject BuildingGameObject { get; set; }
    public SpriteRenderer SpriteRenderer { get; set; }
    public BoxCollider2D Collider { get; set; }

    //Gameplay
    public Building Construction { get; set; }
    public bool IsBuilt { get; set; }
    public bool IsAllDelivered { get; set; } = false;
    public float  BuildingTimePassed { get; set; }
    private CraftingRecipe _buildingCraft;

    public event EventHandler<ResourceDeliveredArgs> OnResourcesDelivered;
    public event EventHandler<BuildingCompletedEventArgs> OnBuildingComplete;
    public event EventHandler<BuildingDestroyedEventArgs> OnDestroyed;

    public delegate void UIToChangeHandler(BuildingItem buildingItem);
    public event UIToChangeHandler UIToChange;

    public float HP { get; set; }
    public Backpack Backpack { get; set; }
    public List<CraftingRecipe> Crafts { get; set; } = new();
    public BuildingCraftingSystem BuildingCraftingSystem { get; set; }
    public TownItem HomeTown { get; set; }

    public bool IsSelected { get; set; } = false;
    public GameObject SelectionIndicator { get; set; }

    private GameObject _progressBarObject;
    private Slider _progressSlider;

    public bool IsDestroyed { get; private set; } = false;
    private readonly object _buildingLock = new();

    public static BuildingItem Create(Vector2Int position, Guid guid, Building building, GameObject buildingGameObject, TownItem townItem, Backpack backpack)
    {
        var buildingItem = buildingGameObject.AddComponent<BuildingItem>();
        buildingItem.Initialize(position, guid, building, buildingGameObject, backpack, townItem);
        return buildingItem;
    }

    public void Initialize(Vector2Int position, Guid guid, Building building,
        GameObject buildingGameObject, Backpack backpack, TownItem townItem)
    {
        SetBasicProperties(position, guid, building, buildingGameObject);
        SetupComponents();
        CreateSelectionIndicator();
        SetupGameplayProperties(building, backpack, townItem);
        LoadCrafts(building);
        CreateProgressBar();
    }

    public void OnSelect()
    {
        IsSelected = true;
        SelectionIndicator.SetActive(true);
    }

    public void OnDeselect()
    {
        IsSelected = false;
        SelectionIndicator.SetActive(false);
    }

    public void AddBuildingMaterials(UnitItem unit)
    {
        lock (_buildingLock)
        {
            var unitBackpack = unit.Backpack;
            if (_buildingCraft != null)
            {
                foreach (var component in _buildingCraft.Components)
                {
                    if (unitBackpack.HasResource(component.BackpackItem))
                    {
                        var resourceHave = Backpack.GetResourceQuantity(component.BackpackItem);
                        var resourceNeed = component.Quantity - resourceHave;
                        if (resourceNeed > 0)
                        {
                            var unitResourceCount = unitBackpack.GetResourceQuantity(component.BackpackItem);
                            var resourceCount = Math.Min(unitResourceCount, resourceNeed);
                            var deliveredResources =
                                new List<CraftingComponent> { new(component.BackpackItem, resourceCount) };
                            var deliverArgs = new ResourceDeliveredArgs(unit, deliveredResources);
                            OnResourcesDelivered?.Invoke(this, deliverArgs);
                            unitBackpack.RemoveItem(component.BackpackItem, resourceCount);
                            Backpack.AddItem(component.BackpackItem, resourceCount);
                        }
                    }
                }
            }
        }
    }

    public bool HasAllRequiredResources()
    {
        if (_buildingCraft == null)
        {
            return false;
        }

        if (Backpack == null)
        {
            UpdateProgress(0);
            return true;
        }

        lock (_buildingLock)
        {
            foreach (var component in _buildingCraft.Components)
            {
                var resourceHave = Backpack.GetResourceQuantity(component.BackpackItem);

                if (resourceHave < component.Quantity)
                {
                    return false;
                }
            }
        }

        UpdateProgress(0);
        return true;
    }

    public bool UpdateBuildingProgress(float deltaTime, UnitItem builder)
    {
        lock (_buildingLock)
        {
            if (!IsBuilt && HasAllRequiredResources())
            {
                if (_buildingCraft == null)
                {
                    return false;
                }
                BuildingTimePassed += deltaTime;
                UpdateProgress(BuildingTimePassed / _buildingCraft.CraftingTime);
                if (BuildingTimePassed >= _buildingCraft.CraftingTime)
                {
                    IsBuilt = true;
                    CompleteBuildingConstruction();
                    return true;
                }
            }
            return false;
        }
    }

    private void CompleteBuildingConstruction()
    {
        var args = new BuildingCompletedEventArgs(this);

        UIToChange?.Invoke(this);
        OnBuildingComplete?.Invoke(this, args);

        Backpack?.Clear();
        IsBuilt = true;
    }

    public void ApplyDamage(float damage)
    {
        lock (_buildingLock)
        {
            if (!IsDestroyed)
            {
                HP -= damage;
                UIToChange?.Invoke(this);

                if (HP <= 0)
                {
                    Destroy();
                }
            }
        }
    }

    public void Destroy()
    {
        if (!IsDestroyed)
        {
            Backpack?.Clear();
            UnsubscribeFromBackpackEvents();

            var args = new BuildingDestroyedEventArgs(this);
            OnDestroyed?.Invoke(this, args);

            Destroy(BuildingGameObject);

            IsDestroyed = true;
        }
    }

    public void UpdateProgress(float progress)
    {
        if (_progressBarObject != null && _progressSlider != null)
        {
            if (!_progressBarObject.activeSelf)
                _progressBarObject.SetActive(true);

            _progressSlider.value = progress;

            if (progress >= 1f)
                _progressBarObject.SetActive(false);
        }
    }

    public Guid GetId()
    {
        return Id;
    }

    private void SetBasicProperties(Vector2Int position, Guid guid, Building building, GameObject buildingGameObject)
    {
        X = position.x;
        Y = position.y;
        Id = guid;
        Building = building;
        BuildingGameObject = buildingGameObject;
    }

    private void SetupComponents()
    {
        SpriteRenderer = BuildingGameObject.GetComponent<SpriteRenderer>();
        Collider = BuildingGameObject.GetComponent<BoxCollider2D>();
    }

    public void CreateSelectionIndicator()
    {
        if (SelectionIndicator != null)
        {
            Destroy(SelectionIndicator);
        }
        SelectionIndicator = new GameObject("SelectionIndicator");
        SelectionIndicator.transform.SetParent(transform);
        SelectionIndicator.transform.localPosition = Vector3.zero;

        var indicatorRenderer = SelectionIndicator.AddComponent<SpriteRenderer>();
        indicatorRenderer.sprite = GetComponent<SpriteRenderer>().sprite;
        indicatorRenderer.color = new Color(0, 1, 0, 0.6f);
        indicatorRenderer.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder - 1;
        SelectionIndicator.transform.localScale = new Vector3(1.2f, 1.2f, 1);

        SelectionIndicator.SetActive(false);
    }

    private void SetupGameplayProperties(Building building, Backpack backpack, TownItem townItem)
    {
        HP = building.MaxHP;
        Backpack = backpack;
        HomeTown = townItem;
        SubscribeToBackpackEvents();
    }

    private void LoadCrafts(Building building)
    {
        building.CraftsIds.ForEach(craftId =>
            Crafts.Add(CraftingRecipesConfig.CraftingRecipes.Find(recipe => recipe.Id == craftId))
        );

        CraftingRecipesConfig.CraftingRecipesDictionary.TryGetValue(Building.BuildingCraftId, out var buildingCraft);
        _buildingCraft = buildingCraft;

        if (Crafts.Count > 0)
        {
            BuildingCraftingSystem = new BuildingCraftingSystem(this);
        }
    }

    private void SubscribeToBackpackEvents()
    {
        if (Backpack != null)
        {
            Backpack.BackpackChanged += OnBackpackChanged;
        }
    }

    private void OnBackpackChanged(object sender, BackpackChangedEventArgs args)
    {
        UIToChange?.Invoke(this);
    }

    private void UnsubscribeFromBackpackEvents()
    {
        if (Backpack != null)
        {
            Backpack.BackpackChanged -= OnBackpackChanged;
        }
    }

    public void CreateProgressBar()
    {
        if (_progressBarObject != null)
        {
            Destroy(_progressBarObject);
        }

        _progressBarObject = new GameObject("ProgressBar");
        _progressBarObject.transform.SetParent(transform, false);

        var yOffset = 0.5f;
        var buildingHeight = 0f;
        var buildingWidth = 0f;
        var spriteRenderer = BuildingGameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            buildingWidth = spriteRenderer.bounds.size.x;
            buildingHeight = spriteRenderer.bounds.size.y;
        }

        _progressBarObject.transform.position = new Vector3(
            BuildingGameObject.transform.position.x + buildingWidth / 2,
            BuildingGameObject.transform.position.y + buildingHeight + yOffset,
            BuildingGameObject.transform.position.z
        );

        _progressBarObject.layer = LayerMask.NameToLayer("GameplayUI");

        var canvas = _progressBarObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        var canvasScaler = _progressBarObject.AddComponent<CanvasScaler>();
        canvasScaler.dynamicPixelsPerUnit = 100f;

        var canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(buildingWidth * 0.08f, buildingHeight * 0.01f);

        // Create Background
        var backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(_progressBarObject.transform, false);
        var bgImage = backgroundObject.AddComponent<Image>();
        bgImage.color = Color.gray;
        var bgRect = backgroundObject.GetComponent<RectTransform>();
        bgRect.sizeDelta = canvasRect.sizeDelta;

        // Create Slider
        var sliderObject = new GameObject("ProgressSlider");
        sliderObject.transform.SetParent(_progressBarObject.transform, false);
        _progressSlider = sliderObject.AddComponent<Slider>();
        _progressSlider.transition = Selectable.Transition.None;
        _progressSlider.interactable = false;
        var sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // Create Fill Area
        var fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(sliderObject.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0);
        fillAreaRect.anchorMax = new Vector2(1, 1);
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.green;
        _progressSlider.fillRect = fillImage.GetComponent<RectTransform>();
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        
        _progressBarObject.SetActive(false);
    }

    public class BuildingDestroyedEventArgs : EventArgs
    {
        public BuildingItem BuildingItem { get; }

        public BuildingDestroyedEventArgs(BuildingItem building)
        {
            BuildingItem = building;
        }
    }

    public class BuildingCompletedEventArgs : EventArgs
    {
        public BuildingItem BuildingItem { get; }

        public BuildingCompletedEventArgs(BuildingItem building)
        {
            BuildingItem = building;
        }
    }
    
    public class ResourceDeliveredArgs : EventArgs
    {
        public UnitItem Unit { get; private set; }
        public List<CraftingComponent> DeliveredComponents { get; private set; }

        public ResourceDeliveredArgs(UnitItem unit, List<CraftingComponent> deliveredComponents)
        {
            Unit = unit;
            DeliveredComponents = deliveredComponents;
        }
    }
}
