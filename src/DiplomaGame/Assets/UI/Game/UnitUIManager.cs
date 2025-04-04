using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitUIManager : MonoBehaviour
{
    private GameObject _unitPrefab;
    private GameObject _unitSkillPrefab;
    private GameObject _unitStatPrefab;
    private GameObject _resourcePrefab;

    private GameObject _unitInfoPanel;
    private TextMeshProUGUI _unitNameText;
    private TextMeshProUGUI _unitTownText;

    private TextMeshProUGUI _unitBackpackCount;
    private Transform _resourcePanel;

    private Transform _unitStatsPanel;
    private Transform _unitSkillsPanel;

    private Dictionary<Type, UnitSkillObj> _skillsDictionary = new();
    private Dictionary<Type, UnitStatObj> _statsDictionary = new();
    private Dictionary<int, GameObject> _playerResourceItems = new();

    private Canvas _mainCanvas;
    private UnitItem _currentUnit;

    private static UnitUIManager _instance;

    public static UnitUIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UnitUIManager>();
                if (_instance == null)
                {
                    var gameObject = new GameObject("UnitUIManager");
                    _instance = gameObject.AddComponent<UnitUIManager>();
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

            if (_unitPrefab == null)
                _unitPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/UnitUIPrefab");

            if (_unitSkillPrefab == null)
                _unitSkillPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/UnitSkillPrefab");

            if (_unitStatPrefab == null)
                _unitStatPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/UnitStatPrefab");

            if (_resourcePrefab == null)
                _resourcePrefab = Resources.Load<GameObject>("UI/Game/Prefabs/ResourceUIPrefab");

            InitializeUI();
            HideUnitInfo();
        }
    }

    private void InitializeUI()
    {
        _mainCanvas = MainCanvasUI.MainCanvas;

        InitializeUnitUI();
    }

    private void InitializeUnitUI()
    {
        var unitInfoPanel = _unitPrefab.transform.Find("Canvas/Panel");
        _unitInfoPanel = Instantiate(unitInfoPanel.gameObject, _mainCanvas.transform);
        _unitInfoPanel.name = "UnitInfoPanel";

        _unitNameText = _unitInfoPanel.transform.Find("UnitName").GetComponent<TextMeshProUGUI>();
        _unitTownText = _unitInfoPanel.transform.Find("UnitTown").GetComponent<TextMeshProUGUI>();

        _unitBackpackCount = _unitInfoPanel.transform.Find("BackpackCount").GetComponent<TextMeshProUGUI>();

        _unitStatsPanel = _unitInfoPanel.transform.Find("SkillsStatsPanel/StatsPanel");
        _unitSkillsPanel = _unitInfoPanel.transform.Find("SkillsStatsPanel/SkillsPanel");

        _resourcePanel = _unitInfoPanel.transform.Find("ScrollView/Viewport/Content");

        var layoutElement = _resourcePanel.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            var viewport = _resourcePanel.transform.Find("ScrollView/Viewport");
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

    private void CreateStatUI(BaseStat stat)
    {
        if (!_statsDictionary.ContainsKey(stat.GetType()))
        {
            var statPrefab = _unitStatPrefab.transform.Find("Panel");
            var statObj = Instantiate(statPrefab, _unitStatsPanel);
            var statUI = new UnitStatObj(statObj.transform);
            _statsDictionary[stat.GetType()] = statUI;

            //statUI.Image.sprite = stat.Image;
            statUI.Slider.maxValue = stat.MaxValue;
            statUI.Slider.value = stat.CurrentValue;
            statUI.StatText.text = $"{stat.CurrentValue:F0}/{stat.MaxValue:F0}";
        }
    }

    private void UpdateStatUI(BaseStat stat)
    {
        if (_statsDictionary.TryGetValue(stat.GetType(), out var statObj))
        {
            //statObj.Image.sprite = stat.Image;
            statObj.Slider.maxValue = stat.MaxValue;
            statObj.Slider.value = stat.CurrentValue;
            statObj.StatText.text = $"{stat.CurrentValue:F0}/{stat.MaxValue:F0}";
        }
        else
        {
            CreateStatUI(stat);
        }
    }

    private void CreateSkillUI(BaseSkill skill)
    {
        var skillPrefab = _unitSkillPrefab.transform.Find("Panel");
        var skillObj = Instantiate(skillPrefab, _unitSkillsPanel);
        var skillUI = new UnitSkillObj(skillObj.transform);
        _skillsDictionary[skill.GetType()] = skillUI;

        //skillUI.Image.sprite = skill.Icon;
        skillUI.Slider.maxValue = skill.ExperienceToNextLevel;
        skillUI.Slider.value = skill.Experience;
        skillUI.SkillText.text = $"Lvl.{skill.CurrentLevel}";
    }

    private void UpdateSkillUI(BaseSkill skill)
    {
        if (_skillsDictionary.TryGetValue(skill.GetType(), out var skillObj))
        {
            //skillObj.Image.sprite = stat.Image;
            skillObj.Slider.maxValue = skill.ExperienceToNextLevel;
            skillObj.Slider.value = skill.Experience;
            skillObj.SkillText.text = $"Lvl. {skill.CurrentLevel}";
        }
        else
        {

            CreateSkillUI(skill);
        }
    }

    private void UpdateBackpackUI(UnitItem unit)
    {
        if (unit.Backpack == null)
        {
            _unitBackpackCount.text = "0 / 0";
        }
        else
        {
            _unitBackpackCount.text = $"{unit.Backpack.CurrentCapacity} / {unit.Backpack.MaxCapacity}";

            var resourcesToRemove = new HashSet<int>(_playerResourceItems.Keys);

            foreach (var resource in unit.Backpack.GetDetailedItems())
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

        var resource = _resourcePrefab.transform.Find("Panel").gameObject;

        var resourceItem = Instantiate(resource, _resourcePanel);

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

        LayoutRebuilder.ForceRebuildLayoutImmediate(_resourcePanel as RectTransform);
    }

    private void RemoveResourceFromPanel(int resourceId)
    {
        if (_playerResourceItems.TryGetValue(resourceId, out var resourceItem))
        {
            Destroy(resourceItem);
            _playerResourceItems.Remove(resourceId);
        }
    }

    public void ShowUnitInfo(UnitItem unit)
    {
        _currentUnit = unit;

        if (unit == null)
        {
            HideUnitInfo();
            return;
        }

        _unitNameText.text = unit.Unit.Name;
        _unitTownText.text = unit.HomeTown != null ? $"{unit.HomeTown.Name}" : "No hometown";

        UpdateStatsUI(unit.Stats);
        UpdateSkillsUI(unit.Skills);
        UpdateBackpackUI(unit);

        _unitInfoPanel.SetActive(true);
    }

    public void HideUnitInfo()
    {
        _unitInfoPanel.SetActive(false);
        _currentUnit = null;
    }

    public void UpdateStatsUI(UnitStats stats)
    {
        if (stats == null) return;

        UpdateStatUI(stats.Health);
        UpdateStatUI(stats.Armor); 
        UpdateStatUI(stats.Hunger); 
        UpdateStatUI(stats.Stamina);
    }

    public void UpdateSkillsUI(UnitSkills skills)
    {
        foreach (var skill in skills.Skills)
        {
            UpdateSkillUI(skill);
        }
    }

    public void UpdateUnitInfo(UnitItem unit)
    {
        if (_currentUnit == unit)
        {
            UpdateStatsUI(unit.Stats);
            UpdateSkillsUI(unit.Skills);
            UpdateBackpackUI(unit);
        }
    }

    public void UpdateUnitInfo(object sender, UnitItem.UnitUIToChangeEventArgs args)
    {
        UpdateUnitInfo(args.Unit);
    }

    private class UnitStatObj
    {
        private Transform Panel { get; }
        public Image Image { get; }
        public Slider Slider { get; }
        public TextMeshProUGUI StatText { get; }

        public UnitStatObj(Transform panel)
        {
            Panel = panel;

            Image = Panel.Find("StatImage").GetComponent<Image>();
            Slider = Panel.Find("Slider").GetComponent<Slider>();
            StatText = Panel.Find("StatText").GetComponent<TextMeshProUGUI>();
        }
    }

    private class UnitSkillObj
    {
        private Transform Panel { get; }
        public Image Image { get; }
        public Slider Slider { get; }
        public TextMeshProUGUI SkillText { get; }

        public UnitSkillObj(Transform panel)
        {
            Panel = panel;

            Image = Panel.Find("SkillImage").GetComponent<Image>();
            Slider = Panel.Find("Slider").GetComponent<Slider>();
            SkillText = Panel.Find("SkillText").GetComponent<TextMeshProUGUI>();
        }
    }
}
