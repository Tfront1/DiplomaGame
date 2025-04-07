using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Town;
using UnityEngine;

public class TownUIManager : MonoBehaviour
{
    private GameObject _townUIPrefab;
    private GameObject _resourceItemPrefab;

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

    private TextMeshProUGUI _playerTownNameText;
    private TextMeshProUGUI _playerUnitsCountText;
    private TextMeshProUGUI _playerBuildingsCountText;
    private TextMeshProUGUI _playerDeadUnitsText;
    private Transform _playerResourcesPanel;
    private TextMeshProUGUI _playerResourcesCountText;
    private Dictionary<int, GameObject> _playerResourceItems = new();
    private TownItem _currentPlayerTown;

    private Canvas _mainCanvas;

    private void Awake()
    {
        if (_townUIPrefab == null)
            _townUIPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Town/TownUIPrefab");

        if (_resourceItemPrefab == null)
            _resourceItemPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Resource/ResourceUIPrefab"); 

        CreateUI();
    }

    private void CreateUI()
    {
        _mainCanvas = MainCanvasUI.MainCanvas;

        var topBarPrefab = _townUIPrefab.transform.Find("Canvas/Background");
        var scrollViewPrefab = _townUIPrefab.transform.Find("Canvas/ScrollView");

        var topBar = Instantiate(topBarPrefab, _mainCanvas.transform);
        topBar.name = "TownTopBar";

        _scrollView = Instantiate(scrollViewPrefab, _mainCanvas.transform);
        _scrollView.name = "TownResourcesScrollView";
        _scrollView.gameObject.SetActive(false);

        var background = topBar.GetComponent<Image>();

        _townNameText = background.transform.Find("TownName").GetComponent<TextMeshProUGUI>();
        _townBuildingsImage = background.transform.Find("BuildingsCountImage").GetComponent<Image>();

        _townBuildingsCountText = _townBuildingsImage.transform.Find("BuildingsCount").GetComponent<TextMeshProUGUI>();
        _townUnitsImage = background.transform.Find("UnitsCountImage").GetComponent<Image>();

        _townUnitsCountText = _townUnitsImage.transform.Find("UnitsCount").GetComponent<TextMeshProUGUI>();
        _townDiedUnitsImage = background.transform.Find("DiedUnitsCountImage").GetComponent<Image>();

        _townDiedUnitsCountText = _townDiedUnitsImage.transform.Find("DiedUnitsCount").GetComponent<TextMeshProUGUI>();

        _resourcesImage = background.transform.Find("ResourcesImage").GetComponent<Image>();

        _resourcesPanel = _scrollView.transform.Find("Viewport/Panel");

        var layoutElement = _resourcesPanel.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            var viewport = _scrollView.transform.Find("Viewport");
            if (viewport != null)
            {
                var viewportRect = viewport.GetComponent<RectTransform>();
                if (viewportRect != null)
                {
                    layoutElement.minHeight = viewportRect.rect.height;
                }
            }
        }

        _townResourcesCountText = _resourcesPanel.transform.Find("BackpackCapacityText").GetComponent<TextMeshProUGUI>();

        _playerTownNameText = _townNameText;
        _playerUnitsCountText = _townUnitsCountText;
        _playerBuildingsCountText = _townBuildingsCountText;
        _playerDeadUnitsText = _townDiedUnitsCountText;
        _playerResourcesPanel = _resourcesPanel;
        _playerResourcesCountText = _townResourcesCountText;

        _resourcesImage.GetComponent<Button>().onClick.AddListener(() => { ToggleResourcesPanel(_scrollView); });
    }

    public void SetPlayerTown(TownItem town)
    {
        _currentPlayerTown = town;
        InitializePlayerTownUI();
    }

    private void InitializePlayerTownUI()
    {
        if (_currentPlayerTown != null)
        {
            _playerTownNameText.text = _currentPlayerTown.Name;
            InitializeResourceDisplay();
            UpdateUI(_currentPlayerTown);
        }
    }

    public void UpdateUI(TownItem town)
    {
        if (town == null)
            return;

        _playerTownNameText.text = town.Name;
        _playerBuildingsCountText.text = town.TownHall != null ? $" : {town.Buildings.Count + 1}" : $" : {town.Buildings.Count}";
        _playerUnitsCountText.text = $" : {town.Units.Count}";
        _playerDeadUnitsText.text = $" : {town.DiedUnits}";

        if (town.TotalBackpack == null)
        {
            _playerResourcesCountText.text = "0 / 0";
        }
        else
        {
            _playerResourcesCountText.text = $"{town.TotalBackpack.CurrentCapacity} / {town.TotalBackpack.MaxCapacity}";

            var resourcesToRemove = new HashSet<int>(_playerResourceItems.Keys);

            foreach (var resource in town.TotalBackpack.GetDetailedItems())
            {
                var resourceId = resource.Key.Id;
                var quantity = resource.Value;

                resourcesToRemove.Remove(resourceId);

                if (quantity <= 0)
                {
                    RemoveResourceFromPanel(resourceId);
                }
                else if (_playerResourceItems.TryGetValue(resourceId, out var resourceItem))
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

        var resourceItem = Instantiate(resource, _playerResourcesPanel);

        var rectTransform = resourceItem.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(0, 30);
            resourceItem.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        var resourceText = resourceItem.GetComponentInChildren<TextMeshProUGUI>();
        var resourceImage = resourceItem.GetComponentInChildren<Image>();
        //resourceImage.sprite = ResourceManager.Instance.GetResourceSprite(resourceId);

        _playerResourceItems[resourceId] = resourceItem;
        resourceText.text = quantity.ToString();

        LayoutRebuilder.ForceRebuildLayoutImmediate(_playerResourcesPanel as RectTransform);
    }

    private void RemoveResourceFromPanel(int resourceId)
    {
        if (_playerResourceItems.TryGetValue(resourceId, out var resourceItem))
        {
            Destroy(resourceItem);
            _playerResourceItems.Remove(resourceId);
        }
    }

    private void InitializeResourceDisplay()
    {
        foreach (var resourceItem in _playerResourceItems.Values)
        {
            Destroy(resourceItem);
        }
        _playerResourceItems.Clear();

        foreach (Transform child in _playerResourcesPanel)
        {
            if (child.GetComponent<TextMeshProUGUI>() != _playerResourcesCountText)
            {
                Destroy(child.gameObject);
            }
        }

        if (_currentPlayerTown.TotalBackpack != null && _currentPlayerTown.TotalBackpack.CurrentCapacity > 0)
        {
            foreach (var resource in _currentPlayerTown.TotalBackpack.GetDetailedItems())
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
        UpdateUI(town ?? _currentPlayerTown);
    }

    private void ToggleResourcesPanel(Transform panel)
    {
        panel.gameObject.SetActive(!panel.gameObject.activeSelf);
    }
}