using Items.Resource.BackPack;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UnitUIManager : MonoBehaviour
{
    private GameObject _unitInfoPanel;
    private TextMeshProUGUI _unitNameText;
    private TextMeshProUGUI _homeTownText;

    private Slider _healthSlider;
    private TextMeshProUGUI _healthText;
    private Slider _armorSlider;
    private TextMeshProUGUI _armorText;
    private Slider _staminaSlider;
    private TextMeshProUGUI _staminaText;
    private Slider _hungerSlider;
    private TextMeshProUGUI _hungerText;

    private Transform _skillsContainer;
    private GameObject _skillPrefab;

    private TextMeshProUGUI _backpackCapacityText;
    private Transform _backpackItemsContainer;
    private GameObject _backpackItemPrefab;

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
            InitializeUI();
            HideUnitInfo();
        }
    }

    private void InitializeUI()
    {
        _mainCanvas = MainCanvasUI.MainCanvas;

        if (_unitInfoPanel == null)
        {
            CreateUnitInfoUI();
        }
    }

    private void CreateUnitInfoUI()
    {
        _unitInfoPanel = new GameObject("UnitInfoPanel");
        _unitInfoPanel.transform.SetParent(_mainCanvas.transform, false);

        var panelRect = _unitInfoPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.75f, 0);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = _unitInfoPanel.AddComponent<Image>();
        panelImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        var verticalLayout = _unitInfoPanel.AddComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.spacing = 10;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = false;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;

        CreateHeaderUI();
        CreateStatsUI();
        CreateSkillsUI();
        CreateBackpackUI();
    }

    private void CreateHeaderUI()
    {
        var headerObj = new GameObject("HeaderSection");
        headerObj.transform.SetParent(_unitInfoPanel.transform, false);

        var headerLayout = headerObj.AddComponent<VerticalLayoutGroup>();
        headerLayout.spacing = 5;

        var nameObj = new GameObject("UnitName");
        nameObj.transform.SetParent(headerObj.transform, false);
        _unitNameText = nameObj.AddComponent<TextMeshProUGUI>();
        _unitNameText.fontSize = 24;
        _unitNameText.alignment = TextAlignmentOptions.Center;
        _unitNameText.color = Color.white;

        var townObj = new GameObject("HomeTown");
        townObj.transform.SetParent(headerObj.transform, false);
        _homeTownText = townObj.AddComponent<TextMeshProUGUI>();
        _homeTownText.fontSize = 18;
        _homeTownText.alignment = TextAlignmentOptions.Center;
        _homeTownText.color = Color.white;

        var layoutElement = headerObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 80;
        layoutElement.preferredHeight = 80;
    }

    private void CreateStatsUI()
    {
        var statsObj = new GameObject("StatsSection");
        statsObj.transform.SetParent(_unitInfoPanel.transform, false);

        var statsLayout = statsObj.AddComponent<VerticalLayoutGroup>();
        statsLayout.spacing = 10;

        var titleObj = new GameObject("StatsTitle");
        titleObj.transform.SetParent(statsObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "STATS";
        titleText.fontSize = 20;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        CreateStatBar(statsObj.transform, "Health", out _healthSlider, out _healthText, Color.green);
        CreateStatBar(statsObj.transform, "Armor", out _armorSlider, out _armorText, Color.blue);
        CreateStatBar(statsObj.transform, "Stamina", out _staminaSlider, out _staminaText, Color.yellow);
        CreateStatBar(statsObj.transform, "Hunger", out _hungerSlider, out _hungerText, Color.red);

        var layoutElement = statsObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 200;
        layoutElement.preferredHeight = 200;
    }

    private void CreateStatBar(Transform parent, string statName, out Slider slider, out TextMeshProUGUI valueText, Color fillColor)
    {
        var statObj = new GameObject(statName);
        statObj.transform.SetParent(parent, false);

        var horizontalLayout = statObj.AddComponent<HorizontalLayoutGroup>();
        horizontalLayout.spacing = 10;
        horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
        horizontalLayout.childControlWidth = true;
        horizontalLayout.childForceExpandWidth = true;

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(statObj.transform, false);
        var labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = statName + ":";
        labelText.fontSize = 16;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = Color.white;

        var sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(statObj.transform, false);
        slider = sliderObj.AddComponent<Slider>();

        var sliderRect = sliderObj.GetComponent<RectTransform>();

        var backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(sliderObj.transform, false);
        var background = backgroundObj.AddComponent<Image>();
        background.color = new Color(0.2f, 0.2f, 0.2f);

        var backgroundRect = backgroundObj.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        var fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);

        var fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);

        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        var fill = fillObj.AddComponent<Image>();
        fill.color = fillColor;

        var fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;
        slider.targetGraphic = background;
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.value = 50;
        slider.interactable = false;

        var valueObj = new GameObject("Value");
        valueObj.transform.SetParent(statObj.transform, false);
        valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.fontSize = 16;
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.color = Color.white;

        var labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.minWidth = 80;
        labelLayout.preferredWidth = 80;

        var sliderLayout = sliderObj.AddComponent<LayoutElement>();
        sliderLayout.flexibleWidth = 1;

        var valueLayout = valueObj.AddComponent<LayoutElement>();
        valueLayout.minWidth = 80;
        valueLayout.preferredWidth = 80;
    }

    private void CreateSkillsUI()
    {
        var skillsObj = new GameObject("SkillsSection");
        skillsObj.transform.SetParent(_unitInfoPanel.transform, false);

        var skillsLayout = skillsObj.AddComponent<VerticalLayoutGroup>();
        skillsLayout.spacing = 10;

        var titleObj = new GameObject("SkillsTitle");
        titleObj.transform.SetParent(skillsObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "SKILLS";
        titleText.fontSize = 20;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        var containerObj = new GameObject("SkillsContainer");
        containerObj.transform.SetParent(skillsObj.transform, false);

        var containerLayout = containerObj.AddComponent<VerticalLayoutGroup>();
        containerLayout.spacing = 5;

        _skillPrefab = CreateSkillPrefab();

        var layoutElement = skillsObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 200;
        layoutElement.preferredHeight = 200;
        layoutElement.flexibleHeight = 1;

        _skillsContainer = containerObj.transform;
    }

    private GameObject CreateSkillPrefab()
    {
        var prefab = new GameObject("SkillPrefab");
        prefab.SetActive(false);
        prefab.transform.SetParent(_unitInfoPanel.transform, false);

        var horizontalLayout = prefab.AddComponent<HorizontalLayoutGroup>();
        horizontalLayout.spacing = 10;
        horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
        horizontalLayout.childControlWidth = true;
        horizontalLayout.childForceExpandWidth = true;

        var nameObj = new GameObject("SkillName");
        nameObj.transform.SetParent(prefab.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 16;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.color = Color.white;

        var levelObj = new GameObject("SkillLevel");
        levelObj.transform.SetParent(prefab.transform, false);
        var levelText = levelObj.AddComponent<TextMeshProUGUI>();
        levelText.fontSize = 16;
        levelText.alignment = TextAlignmentOptions.Right;
        levelText.color = Color.white;

        var progressObj = new GameObject("ProgressBar");
        progressObj.transform.SetParent(prefab.transform, false);

        var progressImage = progressObj.AddComponent<Image>();
        progressImage.color = new Color(0.2f, 0.2f, 0.2f);

        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(progressObj.transform, false);
        var fill = fillObj.AddComponent<Image>();
        fill.color = new Color(0, 0.7f, 1);

        var fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(0.5f, 1);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        var nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.minWidth = 100;
        nameLayout.preferredWidth = 100;

        var levelLayout = levelObj.AddComponent<LayoutElement>();
        levelLayout.minWidth = 50;
        levelLayout.preferredWidth = 50;

        var progressLayout = progressObj.AddComponent<LayoutElement>();
        progressLayout.flexibleWidth = 1;

        return prefab;
    }

    private void CreateBackpackUI()
    {
        var backpackObj = new GameObject("BackpackSection");
        backpackObj.transform.SetParent(_unitInfoPanel.transform, false);

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
        var prefab = new GameObject("BackpackItemPrefab");
        prefab.SetActive(false);
        prefab.transform.SetParent(_unitInfoPanel.transform, false);

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

    public void ShowUnitInfo(UnitItem unit)
    {
        _currentUnit = unit;

        if (unit == null)
        {
            HideUnitInfo();
            return;
        }

        _unitNameText.text = unit.Unit.Name;
        _homeTownText.text = unit.HomeTown != null ? $"Hometown: {unit.HomeTown.Name}" : "No hometown";

        UpdateStatsUI(unit.Stats);
        UpdateSkillsUI(unit.Skills);
        UpdateBackpackUI(unit.Backpack);

        _unitInfoPanel.SetActive(true);
    }

    public void HideUnitInfo()
    {
        _unitInfoPanel.SetActive(false);
        _currentUnit = null;
    }

    private void UpdateStatsUI(UnitStats stats)
    {
        if (stats == null) return;

        // Health
        _healthSlider.maxValue = stats.Health.MaxValue;
        _healthSlider.value = stats.Health.CurrentValue;
        _healthText.text = $"{stats.Health.CurrentValue:F0}/{stats.Health.MaxValue:F0}";

        // Armor
        _armorSlider.maxValue = stats.Armor.MaxValue;
        _armorSlider.value = stats.Armor.CurrentValue;
        _armorText.text = $"{stats.Armor.CurrentValue:F0}/{stats.Armor.MaxValue:F0}";

        // Stamina
        _staminaSlider.maxValue = stats.Stamina.MaxValue;
        _staminaSlider.value = stats.Stamina.CurrentValue;
        _staminaText.text = $"{stats.Stamina.CurrentValue:F0}/{stats.Stamina.MaxValue:F0}";

        // Hunger
        _hungerSlider.maxValue = stats.Hunger.MaxValue;
        _hungerSlider.value = stats.Hunger.CurrentValue;
        _hungerText.text = $"{stats.Hunger.CurrentValue:F0}/{stats.Hunger.MaxValue:F0}";
    }

    private void UpdateSkillsUI(UnitSkills skills)
    {
        for (var i = _skillsContainer.childCount - 1; i >= 0; i--)
        {
            var child = _skillsContainer.GetChild(i);
            if (!child.IsDestroyed() && child != null)
            {
                Destroy(child.gameObject);
            }
            
        }

        if (skills == null || skills.Skills.Count == 0)
        {
            var noSkillsObj = new GameObject("NoSkills");
            noSkillsObj.transform.SetParent(_skillsContainer, false);
            var noSkillsText = noSkillsObj.AddComponent<TextMeshProUGUI>();
            noSkillsText.text = "No skills";
            noSkillsText.fontSize = 16;
            noSkillsText.alignment = TextAlignmentOptions.Center;
            noSkillsText.color = Color.white;
            return;
        }

        foreach (var skill in skills.Skills)
        {
            var skillObj = Instantiate(_skillPrefab, _skillsContainer);
            skillObj.SetActive(true);

            var nameText = skillObj.transform.Find("SkillName").GetComponent<TextMeshProUGUI>();
            var levelText = skillObj.transform.Find("SkillLevel").GetComponent<TextMeshProUGUI>();
            var progressFill = skillObj.transform.Find("ProgressBar/Fill").GetComponent<RectTransform>();

            nameText.text = skill.Name;
            levelText.text = $"Lvl {skill.CurrentLevel}";

            var progressPercent = skill.GetProgressPercentage();
            progressFill.anchorMax = new Vector2(progressPercent / 100f, 1);
        }
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
}
