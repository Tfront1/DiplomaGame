using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Town;

public class TownUIManager : MonoBehaviour
{
    private TextMeshProUGUI playerTownNameText;
    private TextMeshProUGUI playerUnitsCountText;
    private TextMeshProUGUI playerBuildingsCountText;
    private TextMeshProUGUI playerDeadUnitsText;
    private Transform playerResourcesPanel;
    private GameObject resourcePrefab;

    private Dictionary<string, TextMeshProUGUI> playerResourceTexts = new();
    private TownItem currentPlayerTown;
    private Canvas mainCanvas;

    private void Awake()
    {
        CreateUI();
    }

    private void CreateUI()
    {
        CreateMainCanvas();
        var playerTownPanel = CreatePlayerTownPanel();
        CreatePlayerTownInfoTexts(playerTownPanel);
        playerResourcesPanel = CreateResourcesPanel();
        resourcePrefab = CreateResourcePrefab();
    }

    private void CreateMainCanvas()
    {
        mainCanvas = MainCanvasUI.MainCanvas;
    }

    private RectTransform CreatePlayerTownPanel()
    {
        var panelObj = new GameObject("PlayerTownPanel");
        panelObj.transform.SetParent(mainCanvas.transform, false);

        var rectTransform = panelObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(20, -20);
        rectTransform.sizeDelta = new Vector2(300, 200);

        var image = panelObj.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.5f);

        return rectTransform;
    }

    private void CreatePlayerTownInfoTexts(RectTransform parentPanel)
    {
        var headerObj = new GameObject("PlayerTownHeader");
        headerObj.transform.SetParent(parentPanel, false);

        var headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.anchoredPosition = new Vector2(0, -5);
        headerRect.sizeDelta = new Vector2(-20, 30);

        var headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = "YOUR TOWN";
        headerText.fontSize = 24;
        headerText.color = new Color(1f, 0.8f, 0.2f);
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.fontStyle = FontStyles.Bold;

        playerTownNameText = CreateTextElement("TownNameText", parentPanel, 1);
        playerTownNameText.fontSize = 20;
        playerTownNameText.fontStyle = FontStyles.Bold;

        playerUnitsCountText = CreateTextElement("UnitsCountText", parentPanel, 2);
        playerBuildingsCountText = CreateTextElement("BuildingsCountText", parentPanel, 3);
        playerDeadUnitsText = CreateTextElement("DeadUnitsText", parentPanel, 4);
    }

    private TextMeshProUGUI CreateTextElement(string gameObjectName, RectTransform parent, int order)
    {
        var textObj = new GameObject(gameObjectName);
        textObj.transform.SetParent(parent, false);

        var rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(10, -30 - (order * 30));
        rectTransform.sizeDelta = new Vector2(-20, 25);

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Left;
        text.fontSize = 18;
        text.color = Color.white;

        return text;
    }

    private RectTransform CreateResourcesPanel()
    {
        var panelObj = new GameObject("ResourcesPanel");
        panelObj.transform.SetParent(mainCanvas.transform, false);

        var rectTransform = panelObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(20, -240);
        rectTransform.sizeDelta = new Vector2(300, 200);

        var image = panelObj.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.5f);

        var headerObj = new GameObject("ResourcesHeader");
        headerObj.transform.SetParent(panelObj.transform, false);

        var headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.anchoredPosition = new Vector2(0, -5);
        headerRect.sizeDelta = new Vector2(-20, 30);

        var headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = "RESOURCES";
        headerText.fontSize = 20;
        headerText.color = new Color(1f, 0.8f, 0.2f);
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.fontStyle = FontStyles.Bold;

        var contentObj = new GameObject("ResourcesContent");
        contentObj.transform.SetParent(panelObj.transform, false);

        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = new Vector2(0, -15);
        contentRect.sizeDelta = new Vector2(-20, -40);

        var layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 35, 10);
        layout.spacing = 5;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return contentRect;
    }

    private GameObject CreateResourcePrefab()
    {
        var prefab = new GameObject("ResourceItemPrefab");
        prefab.SetActive(false);

        var rectTransform = prefab.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0, 25);

        var layout = prefab.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;

        var nameObj = new GameObject("ResourceName");
        nameObj.transform.SetParent(prefab.transform, false);

        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(150, 25);

        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 16;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.Left;

        var valueObj = new GameObject("ResourceValue");
        valueObj.transform.SetParent(prefab.transform, false);

        var valueRect = valueObj.AddComponent<RectTransform>();
        valueRect.sizeDelta = new Vector2(80, 25);

        var valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.fontSize = 16;
        valueText.color = Color.white;
        valueText.alignment = TextAlignmentOptions.Right;

        DontDestroyOnLoad(prefab);

        return prefab;
    }

    public void SetPlayerTown(TownItem town)
    {
        currentPlayerTown = town;
        InitializePlayerTownUI();
    }

    private void InitializePlayerTownUI()
    {
        if (currentPlayerTown != null)
        {
            playerTownNameText.text = currentPlayerTown.Name;
            InitializeResourceDisplay();
            UpdateUI(currentPlayerTown);
        }
    }
    private void InitializeResourceDisplay()
    {
        if (currentPlayerTown != null && currentPlayerTown.TotalBackpack != null)
        {
            foreach (Transform child in playerResourcesPanel)
            {
                if (child.gameObject != resourcePrefab)
                    Destroy(child.gameObject);
            }

            playerResourceTexts.Clear();

            foreach (var resourceItem in currentPlayerTown.TotalBackpack.GetAllItems())
            {
                var resourceObj = Instantiate(resourcePrefab, playerResourcesPanel);
                resourceObj.SetActive(true);

                var resourceNameText = resourceObj.transform.Find("ResourceName").GetComponent<TextMeshProUGUI>();
                var resourceValueText = resourceObj.transform.Find("ResourceValue").GetComponent<TextMeshProUGUI>();

                resourceNameText.text = resourceItem.Item.Name + ":";
                resourceValueText.text = resourceItem.Quantity.ToString();

                playerResourceTexts.Add(resourceItem.Item.Name, resourceValueText);
            }
        }
    }

    public void UpdateUI(TownItem town)
    {
        if (town != null)
        {
            currentPlayerTown = town;
        }

        if (currentPlayerTown != null)
        {
            playerTownNameText.text = currentPlayerTown.Name;
            playerUnitsCountText.text = "Units: " + currentPlayerTown.Units.Count.ToString();
            playerBuildingsCountText.text = "Buildings: " + currentPlayerTown.Buildings.Count.ToString();
            playerDeadUnitsText.text = "Died Units: " + currentPlayerTown.DiedUnits.ToString();

            UpdateResourceDisplay();
        }
    }

    private void UpdateResourceDisplay()
    {
        if (currentPlayerTown != null && currentPlayerTown.TotalBackpack != null)
        {
            var needReinitialize = false;

            var resources = currentPlayerTown.TotalBackpack.GetAllItems();
            if (resources.Count != playerResourceTexts.Count)
            {
                needReinitialize = true;
            }
            else
            {
                foreach (var resource in resources)
                {
                    if (!playerResourceTexts.ContainsKey(resource.Item.Name))
                    {
                        needReinitialize = true;
                        break;
                    }
                }
            }

            if (needReinitialize)
            {
                InitializeResourceDisplay();
            }
            else
            {
                foreach (var resource in resources)
                {
                    if (playerResourceTexts.TryGetValue(resource.Item.Name, out var valueText))
                    {
                        valueText.text = resource.Quantity.ToString();
                    }
                }
            }
        }
    }

    public void RefreshUI(TownItem town = null)
    {
        UpdateUI(town);
    }
}