using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Assets.Items.Interfaces;
using Bots;
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

    private Image _menuImage;
    private GameObject _menuPrefab;
    private Transform _menuPanel;
    private Button _resumeButton;
    private Button _exitButton;

    private TextMeshProUGUI _playerTownNameText;
    private TextMeshProUGUI _playerUnitsCountText;
    private TextMeshProUGUI _playerBuildingsCountText;
    private TextMeshProUGUI _playerDeadUnitsText;
    private Transform _playerResourcesPanel;
    private TextMeshProUGUI _playerResourcesCountText;
    private Dictionary<IBackpackItem, GameObject> _playerResourceItems = new();
    private TownItem _currentPlayerTown;

    private Canvas _mainCanvas;

    private void Awake()
    {
        if (_townUIPrefab == null)
            _townUIPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Town/TownUIPrefab");

        if (_resourceItemPrefab == null)
            _resourceItemPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Resource/ResourceUIPrefab"); 

        if(_menuPrefab == null)
            _menuPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Town/MenuPrefab");

        CreateUI();
    }

    private void CreateUI()
    {
        _mainCanvas = MainCanvasUI.MainCanvas;

        var topBarPrefab = _townUIPrefab.transform.Find("Canvas/Background");
        var scrollViewPrefab = _townUIPrefab.transform.Find("Canvas/ScrollView");
        var menuPrefab = _menuPrefab.transform.Find("Canvas/BlurPanel");

        var topBar = Instantiate(topBarPrefab, _mainCanvas.transform);
        topBar.name = "TownTopBar";

        _scrollView = Instantiate(scrollViewPrefab, _mainCanvas.transform);
        _scrollView.name = "TownResourcesScrollView";
        _scrollView.gameObject.SetActive(false);

        var background = topBar.GetComponent<Image>();

        _townNameText = background.transform.Find("TownName").GetComponent<TextMeshProUGUI>();
        var townNameButton = background.transform.Find("TownName").GetComponent<Button>();
        townNameButton.onClick.AddListener(MoveToTown);
        _townBuildingsImage = background.transform.Find("BuildingsCountImage").GetComponent<Image>();
        _townBuildingsImage.sprite = UITextureManager.Instance.Sprites["Town/Buildings"];
        _townBuildingsCountText = _townBuildingsImage.transform.Find("BuildingsCount").GetComponent<TextMeshProUGUI>();

        _townUnitsImage = background.transform.Find("UnitsCountImage").GetComponent<Image>();
        _townUnitsImage.sprite = UITextureManager.Instance.Sprites["Town/AliveUnits"];
        _townUnitsCountText = _townUnitsImage.transform.Find("UnitsCount").GetComponent<TextMeshProUGUI>();

        _townDiedUnitsImage = background.transform.Find("DiedUnitsCountImage").GetComponent<Image>();
        _townDiedUnitsImage.sprite = UITextureManager.Instance.Sprites["Town/DiedUnits"];
        _townDiedUnitsCountText = _townDiedUnitsImage.transform.Find("DiedUnitsCount").GetComponent<TextMeshProUGUI>();

        _resourcesImage = background.transform.Find("ResourcesImage").GetComponent<Image>();
        
        _menuImage = background.transform.Find("MenuImage").GetComponent<Image>();
        _menuImage.sprite = UITextureManager.Instance.Sprites["Town/Settings"];
        var menuButton = _menuImage.GetComponent<Button>();
        menuButton.onClick.AddListener(OpenMenuPanel);
        _menuPanel = Instantiate(menuPrefab, _mainCanvas.transform);
        _resumeButton = _menuPanel.Find("MenuPanel/ButtonsPanel/ResumeButton").GetComponent<Button>();
        _exitButton = _menuPanel.Find("MenuPanel/ButtonsPanel/ExitButton").GetComponent<Button>();
        _menuPanel.gameObject.SetActive(false);

        _resumeButton.onClick.AddListener(CloseMenuPanel);
        _exitButton.onClick.AddListener(Exit);

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
        _playerUnitsCountText.text = $" : {town.UnitsCount} / {town.MaxUnits}";
        _playerDeadUnitsText.text = $" : {town.DiedUnits}";

        if (town.TotalBackpack == null)
        {
            _playerResourcesCountText.text = "0 / 0";
        }
        else
        {
            _playerResourcesCountText.text = $"{town.TotalBackpack.CurrentCapacity} / {town.TotalBackpack.MaxCapacity}";

            var resourcesToRemove = new HashSet<IBackpackItem>(_playerResourceItems.Keys);

            foreach (var (resourceItem, quantity) in town.TotalBackpack.GetDetailedItems())
            {
                resourcesToRemove.Remove(resourceItem);
                
                if (quantity <= 0)
                {
                    RemoveResourceFromPanel(resourceItem);
                }
                else if (_playerResourceItems.TryGetValue(resourceItem, out var resourceGO))
                {
                    var resourceText = resourceGO.GetComponentInChildren<TextMeshProUGUI>();
                    resourceText.text = quantity.ToString();
                }
                else
                {
                    AddResourceToPanel(resourceItem, quantity);
                }
            }

            foreach (var resourceToRemove in resourcesToRemove)
            {
                RemoveResourceFromPanel(resourceToRemove);
            }
        }
    }

    private void AddResourceToPanel(IBackpackItem resourceItem, int quantity)
    {
        if (quantity <= 0)
            return;

        var resource = _resourceItemPrefab.transform.Find("Panel").gameObject;

        var resourceGO = Instantiate(resource, _playerResourcesPanel);

        var rectTransform = resourceGO.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(0, 30);
            resourceGO.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        var resourceText = resourceGO.GetComponentInChildren<TextMeshProUGUI>();
        var resourceImage = resourceGO.transform.Find("ResourceImage").GetComponent<Image>();
        resourceImage.sprite = UITextureManager.Instance.GetResourceSprite(resourceItem);
        _playerResourceItems[resourceItem] = resourceGO;
        resourceText.text = quantity.ToString();

        LayoutRebuilder.ForceRebuildLayoutImmediate(_playerResourcesPanel as RectTransform);
    }

    private void RemoveResourceFromPanel(IBackpackItem resourceItem)
    {
        if (_playerResourceItems.TryGetValue(resourceItem, out var resourceGO))
        {
            Destroy(resourceGO);
            _playerResourceItems.Remove(resourceItem);
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
                    AddResourceToPanel(resource.Key, resource.Value);
                }
            }
        }
    }

    private void MoveToTown()
    {
        if(_currentPlayerTown != null && _currentPlayerTown.TownHall != null)
        {
            CameraManager.Instance.SetCameraPositionToMove(
                GridService.GetWorldPosition(TownRegistry.UserTown.TownHall.CenterCoords));
            CameraManager.Instance.SetCameraZoom(CameraConfig.MinZoom);
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

    private void OpenMenuPanel()
    {
        TickRateSystem.Instance.StopTicking();
        UnitActionManager.Instance.PauseAllActions();
        BotBrainManager.Instance.PauseBots();
        CameraManager.Instance.enabled = false;
        _menuPanel.gameObject.SetActive(true);
    }

    private void CloseMenuPanel()
    {
        TickRateSystem.Instance.StartTicking();
        UnitActionManager.Instance.ResumeAllActions();
        BotBrainManager.Instance.ResumeBots();
        CameraManager.Instance.enabled = true;
        _menuPanel.gameObject.SetActive(false);
    }

    private void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}