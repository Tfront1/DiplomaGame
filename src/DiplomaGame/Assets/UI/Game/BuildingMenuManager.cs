using TMPro;
using Town;
using UnityEngine.UI;
using UnityEngine;

public class BuildingMenuManager : MonoBehaviour
{
    private Canvas mainCanvas;
    private GameObject buildingButton;
    private GameObject buildingMenuPanel;
    private GameObject buildingItemPrefab;
    private bool isBuildingMenuOpen;

    private TownItem currentTown;

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
            InitializeUI();
        }
    }

    private void InitializeUI()
    {
        mainCanvas = MainCanvasUI.MainCanvas;

        CreateBuildingButton();
        CreateBuildingMenuPanel();
        buildingItemPrefab = CreateBuildingItemPrefab();
    }

    private void CreateBuildingButton()
    {
        buildingButton = new GameObject("BuildingButton");
        buildingButton.transform.SetParent(mainCanvas.transform, false);

        var rectTransform = buildingButton.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0, 0);
        rectTransform.anchoredPosition = new Vector2(20, 20);
        rectTransform.sizeDelta = new Vector2(80, 80);

        var image = buildingButton.AddComponent<Image>();
        image.color = new Color(0.7f, 0.7f, 0.7f);

        var button = buildingButton.AddComponent<Button>();
        button.onClick.AddListener(ToggleBuildingMenu);
        button.interactable = true;

        var textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(buildingButton.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Build";
        text.fontSize = 16;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
    }

    private void CreateBuildingMenuPanel()
    {
        buildingMenuPanel = new GameObject("BuildingMenuPanel");
        buildingMenuPanel.transform.SetParent(mainCanvas.transform, false);

        var rectTransform = buildingMenuPanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0, 0);
        rectTransform.anchoredPosition = new Vector2(110, 20);
        rectTransform.sizeDelta = new Vector2(300, 400);

        var image = buildingMenuPanel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.8f);

        var headerObj = new GameObject("BuildingMenuHeader");
        headerObj.transform.SetParent(buildingMenuPanel.transform, false);

        var headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.anchoredPosition = new Vector2(0, -20);
        headerRect.sizeDelta = new Vector2(-20, 40);

        var headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = "ДОСТУПНІ БУДІВЛІ";
        headerText.fontSize = 20;
        headerText.color = new Color(1f, 0.8f, 0.2f);
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.fontStyle = FontStyles.Bold;

        var contentObj = new GameObject("BuildingMenuContent");
        contentObj.transform.SetParent(buildingMenuPanel.transform, false);

        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = new Vector2(0, -20);
        contentRect.sizeDelta = new Vector2(-20, -60);

        var scrollRect = contentObj.AddComponent<ScrollRect>();
        var scrollViewport = new GameObject("Viewport");
        scrollViewport.transform.SetParent(contentObj.transform, false);

        var viewportRect = scrollViewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        var mask = scrollViewport.AddComponent<Mask>();
        var maskImage = scrollViewport.AddComponent<Image>();
        maskImage.color = Color.white;

        var scrollContent = new GameObject("Content");
        scrollContent.transform.SetParent(scrollViewport.transform, false);

        var contentRectTransform = scrollContent.AddComponent<RectTransform>();
        contentRectTransform.anchorMin = new Vector2(0, 1);
        contentRectTransform.anchorMax = new Vector2(1, 1);
        contentRectTransform.pivot = new Vector2(0.5f, 1);
        contentRectTransform.anchoredPosition = Vector2.zero;
        contentRectTransform.sizeDelta = new Vector2(0, 0);

        var layout = scrollContent.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        scrollRect.content = contentRectTransform;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        scrollContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        buildingMenuPanel.SetActive(false);
        isBuildingMenuOpen = false;
    }

    private GameObject CreateBuildingItemPrefab()
    {
        var prefab = new GameObject("BuildingItemPrefab");
        prefab.SetActive(false);
        var rectTransform = prefab.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0, 130); // Збільшуємо висоту, щоб вмістити нове поле
        var background = prefab.AddComponent<Image>();
        background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        var layout = prefab.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 5;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;

        var nameObj = new GameObject("BuildingName");
        nameObj.transform.SetParent(prefab.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(0, 25);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 18;
        nameText.color = new Color(1f, 0.8f, 0.2f);
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontStyle = FontStyles.Bold;

        var descObj = new GameObject("BuildingDescription");
        descObj.transform.SetParent(prefab.transform, false);
        var descRect = descObj.AddComponent<RectTransform>();
        descRect.sizeDelta = new Vector2(0, 40);
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.fontSize = 14;
        descText.color = Color.white;
        descText.alignment = TextAlignmentOptions.Center;

        var timeObj = new GameObject("BuildingTime");
        timeObj.transform.SetParent(prefab.transform, false);
        var timeRect = timeObj.AddComponent<RectTransform>();
        timeRect.sizeDelta = new Vector2(0, 20);
        var timeText = timeObj.AddComponent<TextMeshProUGUI>();
        timeText.fontSize = 14;
        timeText.color = new Color(0.7f, 0.7f, 1f);
        timeText.alignment = TextAlignmentOptions.Center;
        timeText.text = "Час: 0.00 с";

        var buttonObj = new GameObject("BuildButton");
        buttonObj.transform.SetParent(prefab.transform, false);
        var buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(120, 30);
        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.6f, 0.3f);
        var buildButton = buttonObj.AddComponent<Button>();

        var buttonTextObj = new GameObject("ButtonText");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        var buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        var buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "ПОБУДУВАТИ";
        buttonText.fontSize = 16;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        DontDestroyOnLoad(prefab);
        return prefab;
    }

    public void ToggleBuildingMenu()
    {
        isBuildingMenuOpen = !isBuildingMenuOpen;
        buildingMenuPanel.SetActive(isBuildingMenuOpen);

        if (isBuildingMenuOpen && currentTown != null)
        {
            PopulateBuildingMenu();
        }
    }

    private void PopulateBuildingMenu()
    {
        if (currentTown == null) return;

        var contentPanel = buildingMenuPanel.transform
            .Find("BuildingMenuContent")
            .Find("Viewport")
            .Find("Content");

        foreach (Transform child in contentPanel)
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
                CreateBuildingItem(contentPanel, building.Name, craft.GetComponentsToString(), craft.CraftingTime, building.Id);
            }
        }
    }

    private void CreateBuildingItem(Transform parentTransform, string buildingName, string description, float time, int buildingId)
    {
        var buildingItemObj = Instantiate(buildingItemPrefab, parentTransform);
        buildingItemObj.SetActive(true);

        var nameText = buildingItemObj.transform.Find("BuildingName").GetComponent<TextMeshProUGUI>();
        var descText = buildingItemObj.transform.Find("BuildingDescription").GetComponent<TextMeshProUGUI>();
        var timeText = buildingItemObj.transform.Find("BuildingTime").GetComponent<TextMeshProUGUI>();
        var buildButton = buildingItemObj.transform.Find("BuildButton").GetComponent<Button>();

        nameText.text = buildingName;
        descText.text = description;
        timeText.text = $"Time: {time:F2} s";

        buildButton.onClick.AddListener(() => StartBuilding(buildingId));
    }

    private void StartBuilding(int buildingId)
    {
        if (currentTown != null)
        {
            var building = BuildingsConfig.Buildings.Find(building => building.Id == buildingId);

            BuildingManager.PrePlacementBuilding(building, currentTown);

            buildingMenuPanel.SetActive(false);
            isBuildingMenuOpen = false;

            currentTown.NotifyUIChanged();
        }
    }

    public void SetCurrentTown(TownItem town)
    {
        currentTown = town;
    }

    public void RefreshUI()
    {
        if (isBuildingMenuOpen && currentTown != null)
        {
            PopulateBuildingMenu();
        }
    }
}