using Items.Resource.BackPack;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SupplyUIManager : MonoBehaviour
{
    private GameObject _supplyInfoPanel;

    private TextMeshProUGUI _backpackCapacityText;
    private Transform _backpackItemsContainer;
    private GameObject _backpackItemPrefab;

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
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUI();
            HideSupplyInfo();
        }
    }

    private void InitializeUI()
    {
        _mainCanvas = MainCanvasUI.MainCanvas;

        if (_supplyInfoPanel == null)
        {
            CreateSupplyInfoUI();
        }
    }
    
    private void CreateSupplyInfoUI()
    {
        _supplyInfoPanel = new GameObject("SupplyInfoPanel");
        _supplyInfoPanel.transform.SetParent(_mainCanvas.transform, false);

        var panelRect = _supplyInfoPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.75f, 0);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = _supplyInfoPanel.AddComponent<Image>();
        panelImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        var verticalLayout = _supplyInfoPanel.AddComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.spacing = 10;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = false;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;

        CreateBackpackUI();
    }

    private void CreateBackpackUI()
    {
        var backpackObj = new GameObject("BackpackSection");
        backpackObj.transform.SetParent(_supplyInfoPanel.transform, false);

        var backpackLayout = backpackObj.AddComponent<VerticalLayoutGroup>();
        backpackLayout.spacing = 10;

        var titleObj = new GameObject("BackpackTitle");
        titleObj.transform.SetParent(backpackObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "BACKPACK";
        titleText.fontSize = 20;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        var capacityObj = new GameObject("BackpackCapacity");
        capacityObj.transform.SetParent(backpackObj.transform, false);
        _backpackCapacityText = capacityObj.AddComponent<TextMeshProUGUI>();
        _backpackCapacityText.fontSize = 16;
        _backpackCapacityText.alignment = TextAlignmentOptions.Center;
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

    private GameObject CreateBackpackItemPrefab()
    {
        var prefab = new GameObject("SupplyBackpackItemPrefab");
        prefab.SetActive(false);
        prefab.transform.SetParent(_supplyInfoPanel.transform, false);

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
    
    public void ShowSupplyInfo(SupplyItem supply)
    {
        _currentSupply = supply;

        if (_currentSupply == null)
        {
            HideSupplyInfo();
            return;
        }

        UpdateBackpackUI(_currentSupply.Backpack);

        _supplyInfoPanel.SetActive(true);
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
        _supplyInfoPanel.SetActive(false);
        _currentSupply = null;
    }
}
