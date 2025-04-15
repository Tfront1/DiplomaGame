using Items.Resource.BackPack;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SupplyUIManager : MonoBehaviour
{
    private GameObject _supplyUIPrefab;
    private GameObject _resourceItemPrefab;

    private Transform _supplyInfoPanel;
    private TextMeshProUGUI _supplyNameText;
    private TextMeshProUGUI _supplyBackpackCountText;
    private Transform _supplyResourcePanel;

    private Dictionary<int, GameObject> _playerResourceItems = new();

    private Canvas _mainCanvas;
    private SupplyItem _currentSupply;

    private static SupplyUIManager _instance;

    public static SupplyUIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SupplyUIManager>();
                if (_instance == null)
                {
                    var gameObject = new GameObject("SupplyUIManager");
                    _instance = gameObject.AddComponent<SupplyUIManager>();
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
            if (_supplyUIPrefab == null)
                _supplyUIPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Supply/SupplyUIPrefab");

            if (_resourceItemPrefab == null)
                _resourceItemPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Resource/ResourceUIPrefab");

            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUI();
            InitializeResourceDisplay();
            HideSupplyInfo();
        }
    }

    private void InitializeUI()
    {
        _mainCanvas = MainCanvasUI.MainCanvas;

        var supplyPanel = _supplyUIPrefab.transform.Find("Canvas/Panel");

        _supplyInfoPanel = Instantiate(supplyPanel, _mainCanvas.transform);
        _supplyInfoPanel.name = "SupplyInfoPanel";

        _supplyNameText = _supplyInfoPanel.transform.Find("SupplyName").GetComponent<TextMeshProUGUI>();
        var supplyNameButton = _supplyInfoPanel.transform.Find("SupplyName").GetComponent<Button>();
        supplyNameButton.onClick.AddListener(MoveCameraToSupply);

        _supplyBackpackCountText = _supplyInfoPanel.transform.Find("BackpackCount").GetComponent<TextMeshProUGUI>();
        _supplyResourcePanel = _supplyInfoPanel.transform.Find("ScrollView/Viewport/Content");

        var layoutElement = _supplyResourcePanel.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            var viewport = _supplyInfoPanel.transform.Find("ScrollView/Viewport");
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
            _supplyBackpackCountText.text = "0 / 0";
        }
        else 
        {
            _supplyBackpackCountText.text = $"{backpack.CurrentCapacity} / {backpack.MaxCapacity}";

            var resourcesToRemove = new HashSet<int>(_playerResourceItems.Keys);

            foreach (var (key, quantity) in backpack.GetDetailedItems())
            {
                var resourceId = key.Id;

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

        var resourceItem = Instantiate(resource, _supplyResourcePanel);

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

        LayoutRebuilder.ForceRebuildLayoutImmediate(_supplyResourcePanel as RectTransform);
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

        foreach (Transform child in _supplyResourcePanel)
        {
            Destroy(child.gameObject);
        }

        if (_currentSupply != null && _currentSupply.Backpack != null && _currentSupply.Backpack.CurrentCapacity > 0)
        {
            foreach (var resource in _currentSupply.Backpack.GetDetailedItems())
            {
                if (resource.Value > 0)
                {
                    AddResourceToPanel(resource.Key.Id, resource.Value);
                }
            }
        }
    }

    public void ShowSupplyInfo(SupplyItem supply)
    {
        _currentSupply = supply;

        if (_currentSupply == null)
        {
            HideSupplyInfo();
            return;
        }

        _supplyNameText.text = supply.Supply.Name;

        UpdateBackpackUI(_currentSupply.Backpack);

        _supplyInfoPanel.gameObject.SetActive(true);
    }

    public void UpdateSupplyInfo(SupplyItem supply)
    {
        if (_currentSupply == supply)
        {
            UpdateBackpackUI(supply.Backpack);
        }
    }

    public void UpdateSupplyInfo(object sender, SupplyItem.SupplyUIToChangeEventArgs args)
    {
        UpdateSupplyInfo(args.Supply);
    }

    public void HideSupplyInfo()
    {
        _supplyInfoPanel.gameObject.SetActive(false);
        _currentSupply = null;
    }

    private void MoveCameraToSupply()
    {
        if (_currentSupply != null)
        {
            CameraManager.Instance.SetCameraPositionToMove(GridService.GetWorldPosition(_currentSupply.CenterCoords));
        }
    }
}
