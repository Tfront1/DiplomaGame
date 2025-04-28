using Assets.Items.Crafts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingMenuUIManager : MonoBehaviour
{
    public GameObject _craftingMenuPrefab;
    public GameObject _craftingItemPrefab;

    private Transform _craftingMenu;
    private Transform _craftingContentMenu;
    private Button _closeCraftingMenuButton;
    private Image _closeCraftingMenuImage;

    private bool _isCraftingMenuOpen;

    private BuildingItem _currentBuilding;

    private Canvas _mainCanvas;

    private static CraftingMenuUIManager _instance;

    public static CraftingMenuUIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CraftingMenuUIManager>();
                if (_instance == null)
                {
                    var gameObject = new GameObject("CraftingMenuManager");
                    _instance = gameObject.AddComponent<CraftingMenuUIManager>();
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

            if (_craftingMenuPrefab == null)
                _craftingMenuPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Crafting/CraftingMenuPrefab");

            if (_craftingItemPrefab == null)
                _craftingItemPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Crafting/CraftItemPrefab");

            InitializeUI();
        }
    }

    private void InitializeUI()
    {
        _mainCanvas = MainCanvasUI.MainCanvas;

        CreateCraftingMenuPanel();
    }

    private void CreateCraftingMenuPanel()
    {
        var scrollViewPrefab = _craftingMenuPrefab.transform.Find("Canvas/ScrollView");

        _craftingMenu = Instantiate(scrollViewPrefab, _mainCanvas.transform);
        _craftingMenu.name = "CraftingMenu";
        _craftingMenu.gameObject.SetActive(false);

        _closeCraftingMenuImage = _craftingMenu.transform.Find("CloseButton").GetComponent<Image>();
        _closeCraftingMenuImage.sprite = UITextureManager.Instance.Sprites["General/Close"];
        _closeCraftingMenuButton = _craftingMenu.transform.Find("CloseButton").GetComponent<Button>();
        _closeCraftingMenuButton.onClick.AddListener(ToggleCraftingMenu);

        _isCraftingMenuOpen = false;

    }

    public void ToggleCraftingMenu()
    {
        _isCraftingMenuOpen = !_isCraftingMenuOpen;
        _craftingMenu.gameObject.SetActive(_isCraftingMenuOpen);
        BuildingMenuUIManager.Instance._buildingButtonTransform.gameObject.SetActive(!_isCraftingMenuOpen);

        if (_isCraftingMenuOpen)
        {
            GameplayInputHandler.Instance.OnMouseRightClick += OnRightMouseClick;

            PopulateCraftingMenu();
        }
        else
        {
            GameplayInputHandler.Instance.OnMouseRightClick -= OnRightMouseClick;
        }
    }

    public void ShowBuildingCrafts(BuildingItem building)
    {
        if (building != null)
        {
            _currentBuilding = building;
            ToggleCraftingMenu();
        }
    }

    private void PopulateCraftingMenu()
    {
        if (_currentBuilding == null) return;

        if(_currentBuilding.Crafts.Count == 0) return;

        if (_craftingContentMenu == null)
        {
            _craftingContentMenu = _craftingMenu.transform.Find("Viewport/Content");

            var layoutElement = _craftingContentMenu.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                var viewport = _craftingMenu.transform.Find("ScrollView/Viewport");
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

        foreach (Transform child in _craftingContentMenu)
        {
            Destroy(child.gameObject);
        }

        foreach (var craft in _currentBuilding.Crafts)
        {
            CreateCraftingItem(_craftingContentMenu, craft);
        }
    }

    private void CreateCraftingItem(Transform parentTransform, CraftingRecipe recipe)
    {
        var buildingPanel = _craftingItemPrefab.transform.Find("Panel");

        var buildingItemObj = Instantiate(buildingPanel, parentTransform);
        buildingItemObj.gameObject.SetActive(true);

        var craftingImage = buildingItemObj.transform.Find("CraftingImage").GetComponent<Image>();
        craftingImage.sprite = UITextureManager.Instance.GetItemSprite(recipe.ResultId, recipe.ResultType);

        var craftingText = buildingItemObj.transform.Find("CraftingText").GetComponent<TextMeshProUGUI>();

        var craftButton = buildingItemObj.transform.Find("CraftingButton").GetComponent<Button>();
        var craftImage = buildingItemObj.transform.Find("CraftingButton").GetComponent<Image>();
        craftImage.sprite = UITextureManager.Instance.GetActionSprite("Craft");

        craftingText.text = $"{recipe.Name}\n{recipe.GetComponentsToString()}\n{recipe.CraftingTime:F1} s";

        craftButton.onClick.AddListener(() => StartCrafting(recipe));
    }

    private void StartCrafting(CraftingRecipe recipe)
    {
        _currentBuilding.BuildingCraftingSystem.StartCraft(recipe);
        ToggleCraftingMenu();
    }

    private void OnRightMouseClick(Vector2 position)
    {
        ToggleCraftingMenu();
    }
}
