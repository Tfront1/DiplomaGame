using System.Collections.Generic;
using System.Linq;
using StateMachine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UnitListUIManager : MonoBehaviour
{
    private GameObject _unitListInfoPanel;
    private Transform _unitsContainer;
    private GameObject _unitItemPrefab;
    private Dictionary<UnitItem, GameObject> _unitUIElements = new();
    private Dictionary<UnitItem, GameObject> _selectedUnits = new();

    private bool _isShiftHold = false;

    private Canvas _mainCanvas;
    private List<UnitItem> _units = new();

    private static UnitListUIManager _instance;

    public static UnitListUIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UnitListUIManager>();
                if (_instance == null)
                {
                    var gameObject = new GameObject("UnitListUIManager");
                    _instance = gameObject.AddComponent<UnitListUIManager>();
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
            HideUnitList();

            UIInputHandler.Instance.OnShiftStart += OnShiftStart;
            UIInputHandler.Instance.OnShiftEnd += OnShiftEnd;
        }
    }

    private void OnDestroy()
    {
        UIInputHandler.Instance.OnShiftStart -= OnShiftStart;
        UIInputHandler.Instance.OnShiftEnd -= OnShiftEnd;
    }

    private void InitializeUI()
    {
        _mainCanvas = MainCanvasUI.MainCanvas;

        if (_unitListInfoPanel == null)
        {
            CreateUnitListUI();
        }
    }

    private void CreateUnitListUI()
    {
        _unitListInfoPanel = new GameObject("UnitListInfoPanel");
        _unitListInfoPanel.transform.SetParent(_mainCanvas.transform, false);

        var panelRect = _unitListInfoPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.75f, 0.5f);
        panelRect.anchorMax = new Vector2(1.0f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = _unitListInfoPanel.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);

        var verticalLayout = _unitListInfoPanel.AddComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.spacing = 5;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = false;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;

        CreateUnitListHeader();
        AddSplitButton();
        AddNewGroupButton();
        CreateUnitsContainer();
    }

    private void AddSplitButton()
    {
        var splitButton = new GameObject("SplitButton");
        splitButton.transform.SetParent(_unitListInfoPanel.transform, false);

        var buttonRect = splitButton.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1, 1);
        buttonRect.anchorMax = new Vector2(1, 1);
        buttonRect.pivot = new Vector2(1, 1);
        buttonRect.sizeDelta = new Vector2(30, 30);
        buttonRect.anchoredPosition = new Vector2(-10, -10);

        var buttonImage = splitButton.AddComponent<Image>();
        buttonImage.color = new Color(0.5f, 0.5f, 0.6f, 1.0f);

        var button = splitButton.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.7f, 0.7f, 0.8f, 1.0f);
        colors.pressedColor = new Color(0.4f, 0.4f, 0.5f, 1.0f);
        button.colors = colors;

        var layoutElement = splitButton.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        button.onClick.AddListener(ChoseHalfOfSelected);
    }

    private void AddNewGroupButton()
    {
        var newGroupButton = new GameObject("SplitButton");
        newGroupButton.transform.SetParent(_unitListInfoPanel.transform, false);

        var buttonRect = newGroupButton.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1, 1);
        buttonRect.anchorMax = new Vector2(1, 1);
        buttonRect.pivot = new Vector2(1, 1);
        buttonRect.sizeDelta = new Vector2(30, 30);
        buttonRect.anchoredPosition = new Vector2(-50, -10);

        var buttonImage = newGroupButton.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1.0f);

        var button = newGroupButton.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.7f, 0.7f, 0.8f, 1.0f);
        colors.pressedColor = new Color(0.4f, 0.4f, 0.5f, 1.0f);
        button.colors = colors;

        var layoutElement = newGroupButton.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        button.onClick.AddListener(CreateNewGroup);
    }

    private void CreateUnitListHeader()
    {
        var headerObj = new GameObject("UnitListHeader");
        headerObj.transform.SetParent(_unitListInfoPanel.transform, false);

        var headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = "UNITS";
        headerText.fontSize = 18;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.color = Color.white;

        var headerLayout = headerObj.AddComponent<LayoutElement>();
        headerLayout.minHeight = 30;
        headerLayout.preferredHeight = 30;
    }

    private void CreateUnitsContainer()
    {
        var containerObj = new GameObject("UnitsContainer");
        containerObj.transform.SetParent(_unitListInfoPanel.transform, false);

        var containerRect = containerObj.GetComponent<RectTransform>();
        if (containerRect == null)
            containerRect = containerObj.AddComponent<RectTransform>();

        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(1, 1);

        containerRect.offsetMin = new Vector2(10, 10);
        containerRect.offsetMax = new Vector2(-10, -50);

        containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, 200);

        var containerLayout = containerObj.AddComponent<VerticalLayoutGroup>();
        containerLayout.spacing = 5;
        containerLayout.childAlignment = TextAnchor.UpperLeft;
        containerLayout.childControlWidth = true;
        containerLayout.childControlHeight = false;
        containerLayout.childForceExpandWidth = true;
        containerLayout.childForceExpandHeight = false;

        var containerElement = containerObj.AddComponent<LayoutElement>();
        containerElement.flexibleHeight = 0;
        containerElement.minHeight = 500;

        var scrollRect = containerObj.AddComponent<ScrollRect>();

        var mask = containerObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var maskImage = containerObj.AddComponent<Image>();
        maskImage.color = new Color(1, 1, 1, 0.1f);

        var scrollContent = new GameObject("Content");
        scrollContent.transform.SetParent(containerObj.transform, false);

        var contentRect = scrollContent.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = new Vector2(0, 0);
        contentRect.offsetMax = new Vector2(0, 0);

        var contentLayout = scrollContent.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 5;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandWidth = true;

        var contentElement = scrollContent.AddComponent<ContentSizeFitter>();
        contentElement.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 10;
        scrollRect.viewport = containerRect;

        CreateScrollbar(containerObj, scrollRect);

        _unitsContainer = scrollContent.transform;

        CreateUnitItemPrefab();
    }

    private void CreateScrollbar(GameObject containerObj, ScrollRect scrollRect)
    {
        var scrollbarObj = new GameObject("Scrollbar");
        scrollbarObj.transform.SetParent(containerObj.transform, false);

        var scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 0.5f);
        scrollbarRect.offsetMin = new Vector2(-10, 5);
        scrollbarRect.offsetMax = new Vector2(0, -5);

        var scrollbar = scrollbarObj.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        var scrollbarImage = scrollbarObj.AddComponent<Image>();
        scrollbarImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        var slidingAreaObj = new GameObject("SlidingArea");
        slidingAreaObj.transform.SetParent(scrollbarObj.transform, false);

        var slidingAreaRect = slidingAreaObj.AddComponent<RectTransform>();
        slidingAreaRect.anchorMin = Vector2.zero;
        slidingAreaRect.anchorMax = Vector2.one;
        slidingAreaRect.offsetMin = Vector2.zero;
        slidingAreaRect.offsetMax = Vector2.zero;

        var handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(slidingAreaObj.transform, false);

        var handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = new Vector2(1, 0.2f);
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        var handleImage = handleObj.AddComponent<Image>();
        handleImage.color = new Color(0.4f, 0.4f, 0.4f, 0.7f);

        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;

        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scrollRect.verticalScrollbarSpacing = 2;
    }

    private void CreateUnitItemPrefab()
    {
        _unitItemPrefab = new GameObject("UnitItemPrefab");
        _unitItemPrefab.SetActive(false);
        _unitItemPrefab.transform.SetParent(_unitListInfoPanel.transform, false);
        var itemRect = _unitItemPrefab.AddComponent<RectTransform>();
        var horizLayout = _unitItemPrefab.AddComponent<HorizontalLayoutGroup>();
        horizLayout.spacing = 8;
        horizLayout.childAlignment = TextAnchor.MiddleLeft;
        horizLayout.padding = new RectOffset(5, 5, 5, 5);
        var itemLayout = _unitItemPrefab.AddComponent<LayoutElement>();
        itemLayout.minHeight = 40;
        itemLayout.preferredHeight = 40;
        var button = _unitItemPrefab.AddComponent<Button>();

        var borderImage = _unitItemPrefab.AddComponent<Image>();
        borderImage.color = Color.white;

        var nameObj = new GameObject("UnitName");
        nameObj.transform.SetParent(_unitItemPrefab.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 14;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.color = Color.white;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        var nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.flexibleWidth = 1;
    }

    public void SetWhiteBorder(GameObject unitItem)
    {
        var borderImage = unitItem.GetComponent<Image>();
        if (borderImage != null)
        {
            borderImage.color = Color.white;
        }
    }

    public void SetGrayBorder(GameObject unitItem)
    {
        var borderImage = unitItem.GetComponent<Image>();
        if (borderImage != null)
        {
            borderImage.color = Color.gray;
        }
    }

    public void UpdateUnitList(List<UnitItem> units)
    {
        _units = units;

        var unitsToRemove = new List<UnitItem>();

        foreach (var kvp in _unitUIElements)
        {
            var unit = kvp.Key;
            if (_units.All(u => u.Id != unit.Id))
            {
                unitsToRemove.Add(unit);
            }
        }

        foreach (var unit in unitsToRemove)
        {
            if (_unitUIElements.TryGetValue(unit, out var uiElement))
            {
                Destroy(uiElement);
                _unitUIElements.Remove(unit);
            }
        }

        foreach (var unit in _units)
        {
            if (_unitUIElements.TryGetValue(unit, out var existingUI))
            {
                UpdateUnitItemUI(existingUI, unit);
            }
            else
            {
                var unitUI = CreateUnitItemUI(unit);
                _unitUIElements[unit] = unitUI;
            }
        }

        if (_units.Count == 0)
        {
            ShowEmptyUnitListMessage();
        }
    }

    private GameObject CreateUnitItemUI(UnitItem unit)
    {
        var unitObj = Instantiate(_unitItemPrefab, _unitsContainer);
        unitObj.SetActive(true);

        UpdateUnitItemUI(unitObj, unit);
        SetWhiteBorder(unitObj);

        var button = unitObj.GetComponent<Button>();
        button.onClick.AddListener(() => SelectUnit(unit));

        return unitObj;
    }

    private void UpdateUnitItemUI(GameObject unitObj, UnitItem unit)
    {
        var nameText = unitObj.transform.Find("UnitName").GetComponent<TextMeshProUGUI>();
        nameText.text = unit.Unit.Name;

    }

    private void ShowEmptyUnitListMessage()
    {
        foreach (Transform child in _unitsContainer)
        {
            Destroy(child.gameObject);
        }

        _unitUIElements.Clear();

        var emptyObj = new GameObject("EmptyUnitList");
        emptyObj.transform.SetParent(_unitsContainer, false);

        var emptyText = emptyObj.AddComponent<TextMeshProUGUI>();
        emptyText.text = "No units available";
        emptyText.fontSize = 14;
        emptyText.alignment = TextAlignmentOptions.Center;
        emptyText.color = Color.white;

        var emptyLayout = emptyObj.AddComponent<LayoutElement>();
        emptyLayout.minHeight = 40;
        emptyLayout.preferredHeight = 40;
    }

    private void CreateNewGroup()
    {
        if (_selectedUnits.Count != 0)
        {
            InputStateMachine.Instance.DeselectItems();
            var unitKeys = new HashSet<UnitItem>(_selectedUnits.Keys);
            InputStateMachine.Instance.SelectedItems.AddRange(unitKeys);
            InputStateMachine.Instance.HighlightSelectedItems(InputStateMachine.Instance.SelectedItems);
            InputStateMachine.Instance.ChangeState(SelectedStates.UnitSelect);
        }
    }

    private void SelectUnit(UnitItem unit)
    {
        if (!_isShiftHold)
        {
            InputStateMachine.Instance.DeselectItems();
            InputStateMachine.Instance.SelectedItems.Add(unit);
            InputStateMachine.Instance.HighlightSelectedItems(InputStateMachine.Instance.SelectedItems);
            InputStateMachine.Instance.ChangeState(SelectedStates.UnitSelect);
        }
        else
        {
            if (_selectedUnits.ContainsKey(unit))
            {
                _selectedUnits.Remove(unit);
                SetWhiteBorder(_selectedUnits[unit]);
            }
            else
            {
                _selectedUnits.Add(unit, _unitUIElements[unit]);
                SetGrayBorder(_selectedUnits[unit]);
            }
        }
    }

    public void ChoseHalfOfSelected()
    {
        var count = _unitUIElements.Count;
        var halfCount = (count + 1) / 2;

        var selectedItems = _unitUIElements
            .Take(halfCount)
            .Select(pair => pair.Key)
            .ToHashSet(EqualityComparer<UnitItem>.Default);


        InputStateMachine.Instance.DeselectItems();
        InputStateMachine.Instance.SelectedItems.AddRange(selectedItems);
        InputStateMachine.Instance.HighlightSelectedItems(InputStateMachine.Instance.SelectedItems);

        InputStateMachine.Instance.ChangeState(
            selectedItems.Count > 0 ? SelectedStates.UnitSelect : SelectedStates.NothingSelect
        );
    }

    public void RemoveDeadUnit(UnitItem deadUnit)
    {
        if (deadUnit == null)
            return;

        if (_unitUIElements.TryGetValue(deadUnit, out var unitUI))
        {
            Destroy(unitUI);
            _unitUIElements.Remove(deadUnit);
        }

        _units.RemoveAll(u => u.Id == deadUnit.Id);

        if (_units.Count == 0)
        {
            ShowEmptyUnitListMessage();
        }
    }

    public void ShowUnitList()
    {
        _unitListInfoPanel.SetActive(true);
        foreach (var unitUi in _unitUIElements)
        {
            SetWhiteBorder(unitUi.Value);
        }
    }

    public void HideUnitList()
    {
        _unitListInfoPanel.SetActive(false);
        _selectedUnits.Clear();
    }

    private void OnShiftStart()
    {
        _isShiftHold = true;
    }
    private void OnShiftEnd()
    {
        _isShiftHold = false;
    }
}
