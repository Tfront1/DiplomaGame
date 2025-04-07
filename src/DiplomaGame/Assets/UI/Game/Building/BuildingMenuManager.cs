using Assets.Items.Crafts;
using TMPro;
using Town;
using UnityEngine.UI;
using UnityEngine;

public class BuildingMenuManager : MonoBehaviour
{
    public GameObject _buildingButtonPrefab;
    public GameObject _buildingMenuPrefab;
    public GameObject _buildingItemPrefab;

    private Canvas _mainCanvas;

    public Transform _buildingButtonTransform;
    private Image _buildingButtonImage;

    private Transform _buildingMenu;
    private Transform _buildingContentMenu;
    private Button _closeBuildingMenuButton;
    private Image _closeBuildingMenuImage;

    private bool _isBuildingMenuOpen;

    private TownItem _currentTown;

    private static BuildingMenuManager _instance;

    public static BuildingMenuManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BuildingMenuManager>();
                if (_instance == null)
                {
                    var gameObject = new GameObject("BuildingMenuManager");
                    _instance = gameObject.AddComponent<BuildingMenuManager>();
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

            if (_buildingButtonPrefab == null)
                _buildingButtonPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Building/BuildingButtonPrefab");

            if (_buildingMenuPrefab == null)
                _buildingMenuPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Building/BuildingMenuPrefab");

            if (_buildingItemPrefab == null)
                _buildingItemPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Building/BuildingItemPrefab");

            InitializeUI();
        }
    }

    private void InitializeUI()
    {
        _mainCanvas = MainCanvasUI.MainCanvas;

        InitializeBuildingButton();
        CreateBuildingMenuPanel();
    }

    private void InitializeBuildingButton()
    {
        var buttonTransform = _buildingButtonPrefab.transform.Find("Canvas/BuildingImage");
        _buildingButtonTransform = Instantiate(buttonTransform, _mainCanvas.transform);
        _buildingButtonTransform.name = "BuildingButton";

        _buildingButtonImage = _buildingButtonTransform.GetComponent<Image>();

        var buildingButton = _buildingButtonTransform.GetComponent<Button>();

        buildingButton.onClick.AddListener(ToggleBuildingMenu);
    }

    private void CreateBuildingMenuPanel()
    {
        var scrollViewPrefab = _buildingMenuPrefab.transform.Find("Canvas/ScrollView");

        _buildingMenu = Instantiate(scrollViewPrefab, _mainCanvas.transform);
        _buildingMenu.name = "BuildingMenu";
        _buildingMenu.gameObject.SetActive(false);

        _closeBuildingMenuImage = _buildingMenu.transform.Find("CloseButton").GetComponent<Image>();
        _closeBuildingMenuButton = _buildingMenu.transform.Find("CloseButton").GetComponent<Button>();
        _closeBuildingMenuButton.onClick.AddListener(ToggleBuildingMenu);

        _isBuildingMenuOpen = false;
    }

    public void ToggleBuildingMenu()
    {
        _isBuildingMenuOpen = !_isBuildingMenuOpen;
        _buildingMenu.gameObject.SetActive(_isBuildingMenuOpen);
        _buildingButtonTransform.gameObject.SetActive(!_isBuildingMenuOpen);

        if (_isBuildingMenuOpen && _currentTown != null)
        {
            GameplayInputHandler.Instance.OnMouseRightClick += OnRightMouseClick;

            PopulateBuildingMenu();
        }
        else
        {
            GameplayInputHandler.Instance.OnMouseRightClick -= OnRightMouseClick;
        }
    }

    private void PopulateBuildingMenu()
    {
        if (_currentTown == null) return;

        if (_buildingContentMenu == null)
        {
            _buildingContentMenu = _buildingMenu.transform.Find("Viewport/Content");

            var layoutElement = _buildingContentMenu.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                var viewport = _buildingMenu.transform.Find("ScrollView/Viewport");
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

        foreach (Transform child in _buildingContentMenu)
        {
            Destroy(child.gameObject);
        }

        foreach (var building in BuildingsConfig.Buildings)
        {
            if (building.Id == 1)
            {
                continue;
            }
            CraftingRecipesConfig.CraftingRecipesDictionary.TryGetValue(building.BuildingCraftId, out var craft);
            if (craft != null)
            {
                CreateBuildingItem(_buildingContentMenu, building, craft);
            }
        }
    }

    private void CreateBuildingItem(Transform parentTransform, Building building, CraftingRecipe craft)
    {
        var buildingPanel = _buildingItemPrefab.transform.Find("Panel");

        var buildingItemObj = Instantiate(buildingPanel, parentTransform);
        buildingItemObj.gameObject.SetActive(true);
        
        var buildingImage = buildingItemObj.transform.Find("BuildingImage").GetComponent<Image>();
        if (BuildingManager._buildingSpriteCache.TryGetValue(building.Id, out var buildingSprite) && buildingSprite != null)
        {
            buildingImage.sprite = buildingSprite;
            buildingImage.preserveAspect = true;
        }
        else
        {
            buildingImage.sprite = null;
        }

        var buildingText = buildingItemObj.transform.Find("BuildingText").GetComponent<TextMeshProUGUI>();

        var buildButton = buildingItemObj.transform.Find("BuildButton").GetComponent<Button>();
        var buildImage = buildingItemObj.transform.Find("BuildButton").GetComponent<Image>();

        buildingText.text = $"{building.Name}\n{craft.GetComponentsToString()}\n{craft.CraftingTime:F1} s";

        buildButton.onClick.AddListener(() => StartBuilding(building));
    }

    private void StartBuilding(Building building)
    {
        if (_currentTown != null)
        {
            BuildingManager.PrePlacementBuilding(building, _currentTown);
            ToggleBuildingMenu();
            _currentTown.NotifyUIChanged();
        }
    }

    private void OnRightMouseClick(Vector2 position)
    {
        ToggleBuildingMenu();
    }

    public void SetCurrentTown(TownItem town)
    {
        _currentTown = town;
    }

    public void RefreshUI()
    {
        if (_isBuildingMenuOpen && _currentTown != null)
        {
            PopulateBuildingMenu();
        }
    }
}