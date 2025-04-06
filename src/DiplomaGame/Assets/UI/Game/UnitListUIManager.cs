using System.Collections.Generic;
using System.Linq;
using StateMachine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UnitListUIManager : MonoBehaviour
{
    private GameObject _unitListUIPrefab;
    private GameObject _unitItemPrefab;

    private GameObject _unitListPanel;
    private Button _unitHalfButton;
    private Button _unitSelectButton;

    private Transform _unitsContainer;
    private Color _unitNotSelectedColor = Color.white;

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

            if (_unitListUIPrefab == null)
                _unitListUIPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/UnitListUIPrefab");

            if (_unitItemPrefab == null)
                _unitItemPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/UnitItemPrefab");

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

        CreateUnitListUI();
    }

    private void CreateUnitListUI()
    {
        var unitListPanel = _unitListUIPrefab.transform.Find("Canvas/Panel");
        _unitListPanel = Instantiate(unitListPanel.gameObject, _mainCanvas.transform);
        _unitListPanel.name = "UnitListPanel";

        _unitHalfButton = _unitListPanel.transform.Find("Half").GetComponent<Button>();
        _unitHalfButton.onClick.AddListener(ChoseHalfOfSelected);
        var halfImage = _unitListPanel.transform.Find("Half").GetComponent<Image>();

        _unitSelectButton = _unitListPanel.transform.Find("Select").GetComponent<Button>();
        _unitSelectButton.onClick.AddListener(CreateNewGroup);
        var selectImage = _unitListPanel.transform.Find("Select").GetComponent<Image>();

        _unitsContainer = _unitListPanel.transform.Find("ScrollView/Viewport/Content");
    }

    public void SetDefaultColor(GameObject unitItem)
    {
        var borderImage = unitItem.GetComponent<Image>();
        if (borderImage != null && _unitNotSelectedColor != Color.white)
        {
            borderImage.color = _unitNotSelectedColor;
        }
    }

    public void SetSelectedColor(GameObject unitItem)
    {
        var borderImage = unitItem.GetComponent<Image>();
        if (borderImage != null && _unitNotSelectedColor != Color.white)
        {
            borderImage.color = _unitNotSelectedColor * 0.7f;
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
    }

    private GameObject CreateUnitItemUI(UnitItem unit)
    {
        var unitPrefab = _unitItemPrefab.transform.Find("Panel");
        var unitObj = Instantiate(unitPrefab.gameObject, _unitsContainer);
        unitObj.SetActive(true);

        UpdateUnitItemUI(unitObj, unit);
        if (_unitNotSelectedColor == Color.white)
        {
            _unitNotSelectedColor = unitPrefab.GetComponent<Image>().color;
        }

        SetDefaultColor(unitObj);

        var button = unitObj.GetComponent<Button>();
        button.onClick.AddListener(() => SelectUnit(unit));

        return unitObj;
    }

    private void UpdateUnitItemUI(GameObject unitObj, UnitItem unit)
    {
        var nameText = unitObj.transform.Find("UnitName").GetComponent<TextMeshProUGUI>();
        nameText.text = unit.Unit.Name;
        
        var unitBackpack = unitObj.transform.Find("UnitBackpack").GetComponent<TextMeshProUGUI>();
        unitBackpack.text = $"{unit.Backpack.CurrentCapacity} / {unit.Backpack.MaxCapacity}";

        var unitHP = unitObj.transform.Find("UnitHP").GetComponent<TextMeshProUGUI>();
        unitHP.text = $"{unit.Stats.Health.CurrentValue:F1} / {unit.Stats.Health.MaxValue:F1}";
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
                SetDefaultColor(_selectedUnits[unit]);
            }
            else
            {
                _selectedUnits.Add(unit, _unitUIElements[unit]);
                SetSelectedColor(_selectedUnits[unit]);
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
    }

    public void ShowUnitList()
    {
        _unitListPanel.SetActive(true);
        foreach (var unitUi in _unitUIElements)
        {
            SetDefaultColor(unitUi.Value);
        }
    }

    public void HideUnitList()
    {
        _unitListPanel.SetActive(false);
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
