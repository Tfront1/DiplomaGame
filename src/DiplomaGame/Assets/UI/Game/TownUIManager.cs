using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Town;

public class TownUIManager : MonoBehaviour
{
    public GameObject _townUIPrefab;
    public GameObject _resourceItemPrefab;

    private TextMeshProUGUI _townNameText;

    private TextMeshProUGUI _townBuildingsCountText;
    private Image _townBuildingsImage;

    private TextMeshProUGUI _townUnitsCountText;
    private Image _townUnitsImage;

    private TextMeshProUGUI _townDiedUnitsCountText;
    private Image _townDiedUnitsImage;

    private Transform _scrollView;
    private TextMeshProUGUI _townResourcesCountText;
    private Transform _resourcesPanel;

    private Image _resourcesImage;

    private TextMeshProUGUI playerTownNameText;
    private TextMeshProUGUI playerUnitsCountText;
    private TextMeshProUGUI playerBuildingsCountText;
    private TextMeshProUGUI playerDeadUnitsText;
    private Transform playerResourcesPanel;
    private TextMeshProUGUI playerResourcesCountText;
    private Dictionary<int, GameObject> playerResourceItems = new();
    private TownItem currentPlayerTown;
    private Canvas mainCanvas;

    private void Awake()
    {
        if (_townUIPrefab == null)
            _townUIPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/TownUIPrefab");

        if (_resourceItemPrefab == null)
            _resourceItemPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/TownResourceUIPrefab"); 

        CreateUI();
    }

    private void CreateUI()
    {
        var townUI = Instantiate(_townUIPrefab, transform);

        mainCanvas = townUI.GetComponentInChildren<Canvas>();

        var background = mainCanvas.transform.Find("Background").GetComponent<Image>();

        _townNameText = background.transform.Find("TownName").GetComponent<TextMeshProUGUI>();

        _townBuildingsImage = background.transform.Find("BuildingsCountImage").GetComponent<Image>();
        _townBuildingsCountText = _townBuildingsImage.transform.Find("BuildingsCount").GetComponent<TextMeshProUGUI>();

        _townUnitsImage = background.transform.Find("UnitsCountImage").GetComponent<Image>();
        _townUnitsCountText = _townUnitsImage.transform.Find("UnitsCount").GetComponent<TextMeshProUGUI>();

        _townDiedUnitsImage = background.transform.Find("DiedUnitsCountImage").GetComponent<Image>();
        _townDiedUnitsCountText = _townDiedUnitsImage.transform.Find("DiedUnitsCount").GetComponent<TextMeshProUGUI>();

        _resourcesImage = background.transform.Find("ResourcesImage").GetComponent<Image>();

        _scrollView = mainCanvas.transform.Find("ScrollView");

        _resourcesPanel = _scrollView.transform.Find("Viewport/Panel");

        var layoutElement = _resourcesPanel.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            var viewportRect = _scrollView.GetComponent<RectTransform>();
            if (viewportRect != null)
            {
                layoutElement.minHeight = viewportRect.rect.height;
            }
        }

        _townResourcesCountText = _resourcesPanel.transform.Find("BackpackCapacityText").GetComponent<TextMeshProUGUI>();

        playerTownNameText = _townNameText;
        playerUnitsCountText = _townUnitsCountText;
        playerBuildingsCountText = _townBuildingsCountText;
        playerDeadUnitsText = _townDiedUnitsCountText;
        playerResourcesPanel = _resourcesPanel;
        playerResourcesCountText = _townResourcesCountText;

        _resourcesImage.GetComponent<Button>().onClick.AddListener(() => { ToggleResourcesPanel(_scrollView); });
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

    public void UpdateUI(TownItem town)
    {
        if (town == null)
            return;

        playerTownNameText.text = town.Name;
        playerBuildingsCountText.text = town.TownHall != null ? $" : {town.Buildings.Count + 1}" : $" : {town.Buildings.Count}";
        playerUnitsCountText.text = $" : {town.Units.Count}";
        playerDeadUnitsText.text = $" : {town.DiedUnits}";

        if (town.TotalBackpack == null)
        {
            playerResourcesCountText.text = "0 / 0";
        }
        else
        {
            playerResourcesCountText.text = $"{town.TotalBackpack.CurrentCapacity} / {town.TotalBackpack.MaxCapacity}";

            var resourcesToRemove = new HashSet<int>(playerResourceItems.Keys);

            foreach (var resource in town.TotalBackpack.GetDetailedItems())
            {
                var resourceId = resource.Key.Id;
                var quantity = resource.Value;

                resourcesToRemove.Remove(resourceId);

                if (quantity <= 0)
                {
                    RemoveResourceFromPanel(resourceId);
                }
                else if (playerResourceItems.TryGetValue(resourceId, out var resourceItem))
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

        var resource = _resourceItemPrefab.transform.Find("Panel").gameObject;

        var resourceItem = Instantiate(resource, playerResourcesPanel);

        var rectTransform = resourceItem.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(0, 30);
            resourceItem.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        var resourceText = resourceItem.GetComponentInChildren<TextMeshProUGUI>();
        var resourceImage = resourceItem.GetComponentInChildren<Image>();
        //resourceImage.sprite = ResourceManager.Instance.GetResourceSprite(resourceId);

        playerResourceItems[resourceId] = resourceItem;
        resourceText.text = quantity.ToString();

        LayoutRebuilder.ForceRebuildLayoutImmediate(playerResourcesPanel as RectTransform);
    }

    private void RemoveResourceFromPanel(int resourceId)
    {
        if (playerResourceItems.TryGetValue(resourceId, out var resourceItem))
        {
            Destroy(resourceItem);
            playerResourceItems.Remove(resourceId);
        }
    }

    private void InitializeResourceDisplay()
    {
        foreach (var resourceItem in playerResourceItems.Values)
        {
            Destroy(resourceItem);
        }
        playerResourceItems.Clear();

        foreach (Transform child in playerResourcesPanel)
        {
            if (child.GetComponent<TextMeshProUGUI>() != playerResourcesCountText)
            {
                Destroy(child.gameObject);
            }
        }

        if (currentPlayerTown.TotalBackpack != null && currentPlayerTown.TotalBackpack.CurrentCapacity > 0)
        {
            foreach (var resource in currentPlayerTown.TotalBackpack.GetDetailedItems())
            {
                if (resource.Value > 0)
                {
                    AddResourceToPanel(resource.Key.Id, resource.Value);
                }
            }
        }
    }

    public void RefreshUI(TownItem town = null)
    {
        UpdateUI(town ?? currentPlayerTown);
    }

    private void ToggleResourcesPanel(Transform panel)
    {
        panel.gameObject.SetActive(!panel.gameObject.activeSelf);
    }
}