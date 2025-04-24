using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Items.Interfaces;
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
    private Dictionary<IBackpackItem, GameObject> _playerResourceItems = new();

    private Transform _unitEquipmentPanel;

    private Transform _unitMainWeaponPanel;
    private Button _unitMainWeaponButton;
    private Image _unitMainWeaponImage;
    private TextMeshProUGUI _unitMainWeaponName;
    private UIElementContext _unitMainWeaponContext;

    private Transform _unitSecondaryWeaponPanel;
    private Button _unitSecondaryWeaponButton;
    private Image _unitSecondaryWeaponImage;
    private TextMeshProUGUI _unitSecondaryWeaponName;
    private UIElementContext _unitSecondaryWeaponContext;

    private Transform _unitArmorPanel;
    private Button _unitArmorButton;
    private Image _unitArmorImage;
    private TextMeshProUGUI _unitArmorName;
    private UIElementContext _unitArmorContext;

    private Transform _unitAmmoPanel;
    private Button _unitAmmoButton;
    private Image _unitAmmoImage;
    private TextMeshProUGUI _unitAmmoName;
    private UIElementContext _unitAmmoContext;

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
                _unitPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Unit/UnitUIPrefab");

            if (_unitSkillPrefab == null)
                _unitSkillPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Unit/UnitSkillPrefab");

            if (_unitStatPrefab == null)
                _unitStatPrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Unit/UnitStatPrefab");

            if (_resourcePrefab == null)
                _resourcePrefab = Resources.Load<GameObject>("UI/Game/Prefabs/Resource/ResourceUIPrefab");
            
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
        var unitNameButton = _unitInfoPanel.transform.Find("UnitName").GetComponent<Button>();
        unitNameButton.onClick.AddListener(MoveCameraToUnit);
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

        _unitEquipmentPanel = _unitInfoPanel.transform.Find("EquipmentPanel");

        _unitMainWeaponPanel = _unitEquipmentPanel.Find("MainWeapon");
        _unitMainWeaponButton = _unitMainWeaponPanel.GetComponent<Button>();
        _unitMainWeaponImage = _unitMainWeaponPanel.Find("ItemImage").GetComponent<Image>();
        _unitMainWeaponImage.sprite = UITextureManager.Instance.Sprites["Unit/Equipment/MainWeapon"];
        _unitMainWeaponName = _unitMainWeaponPanel.Find("ItemName").GetComponent<TextMeshProUGUI>();
        _unitMainWeaponContext = _unitMainWeaponPanel.gameObject.AddComponent<UIElementContext>();

        _unitSecondaryWeaponPanel = _unitEquipmentPanel.Find("SecWeapon");
        _unitSecondaryWeaponButton = _unitSecondaryWeaponPanel.GetComponent<Button>();
        _unitSecondaryWeaponImage = _unitSecondaryWeaponPanel.Find("ItemImage").GetComponent<Image>();
        _unitSecondaryWeaponImage.sprite = UITextureManager.Instance.Sprites["Unit/Equipment/SecWeapon"];
        _unitSecondaryWeaponName = _unitSecondaryWeaponPanel.Find("ItemName").GetComponent<TextMeshProUGUI>();
        _unitSecondaryWeaponContext = _unitSecondaryWeaponPanel.gameObject.AddComponent<UIElementContext>();

        _unitArmorPanel = _unitEquipmentPanel.Find("Armor");
        _unitArmorButton = _unitArmorPanel.GetComponent<Button>();
        _unitArmorImage = _unitArmorPanel.Find("ItemImage").GetComponent<Image>();
        _unitArmorImage.sprite = UITextureManager.Instance.Sprites["Unit/Equipment/Armor"];
        _unitArmorName = _unitArmorPanel.Find("ItemName").GetComponent<TextMeshProUGUI>();
        _unitArmorContext = _unitArmorPanel.gameObject.AddComponent<UIElementContext>();

        _unitAmmoPanel = _unitEquipmentPanel.Find("Ammo");
        _unitAmmoButton = _unitAmmoPanel.GetComponent<Button>();
        _unitAmmoImage = _unitAmmoPanel.Find("ItemImage").GetComponent<Image>();
        _unitAmmoImage.sprite = UITextureManager.Instance.Sprites["Unit/Equipment/Ammo"];
        _unitAmmoName = _unitAmmoPanel.Find("ItemName").GetComponent<TextMeshProUGUI>();
        _unitAmmoContext = _unitAmmoPanel.gameObject.AddComponent<UIElementContext>();
    }

    private void CreateStatUI(BaseStat stat)
    {
        if (!_statsDictionary.ContainsKey(stat.GetType()))
        {
            var statPrefab = _unitStatPrefab.transform.Find("Panel");
            var statObj = Instantiate(statPrefab, _unitStatsPanel);
            var statUI = new UnitStatObj(statObj.transform);
            _statsDictionary[stat.GetType()] = statUI;

            statUI.Image.sprite = UITextureManager.Instance.GetUnitStatSprite(stat);
            statUI.Slider.maxValue = stat.MaxValue;
            statUI.Slider.value = stat.CurrentValue;
            statUI.StatText.text = $"{stat.CurrentValue:F0}/{stat.MaxValue:F0}";
        }
    }

    private void UpdateStatUI(BaseStat stat)
    {
        if (_statsDictionary.TryGetValue(stat.GetType(), out var statObj))
        {
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

        skillUI.Image.sprite = UITextureManager.Instance.GetUnitSkillSprite(skill);
        skillUI.Slider.maxValue = skill.ExperienceToNextLevel;
        skillUI.Slider.value = skill.Experience;
        skillUI.SkillText.text = $"Lvl.{skill.CurrentLevel}";
    }

    private void UpdateSkillUI(BaseSkill skill)
    {
        if (_skillsDictionary.TryGetValue(skill.GetType(), out var skillObj))
        {
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

            var resourcesToRemove = new HashSet<IBackpackItem>(_playerResourceItems.Keys);

            foreach (var resource in unit.Backpack.GetDetailedItems())
            {
                var resourceItem = resource.Key;
                var quantity = resource.Value;

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

            foreach (var resourceItem in resourcesToRemove)
            {
                RemoveResourceFromPanel(resourceItem);
            }
        }
    }

    private void AddResourceToPanel(IBackpackItem resourceItem, int quantity)
    {
        if (quantity <= 0)
            return;

        var resource = _resourcePrefab.transform.Find("Panel").gameObject;

        var resourceGO = Instantiate(resource, _resourcePanel);

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

        LayoutRebuilder.ForceRebuildLayoutImmediate(_resourcePanel as RectTransform);

        resourceGO.GetComponent<Button>().onClick.AddListener(() =>
        {
            _currentUnit.UnitEquipment.TryAutoEquip(resourceItem);
        });
    }

    private void RemoveResourceFromPanel(IBackpackItem resourceItem)
    {
        if (_playerResourceItems.TryGetValue(resourceItem, out var resourceGO))
        {
            Destroy(resourceGO);
            _playerResourceItems.Remove(resourceItem);
        }
    }

    private void UpdateEquipmentUI(UnitItem unit)
    {
        var equipment = unit.UnitEquipment;

        //_unitMainWeaponImage
        _unitMainWeaponName.text = equipment.MainWeapon.Name;
        _unitMainWeaponContext.SetContextText(
            $"Name: {equipment.MainWeapon.Name}\n" +
            $"Damage: {equipment.MainWeapon.Damage:F1}\n" +
            $"Miss chance: {(equipment.MainWeapon.MissChance * 100):F0}%\n" +
            $"Stamina To Attack: {equipment.MainWeapon.StaminaToAttack:F1}\n" +
            $"Armor Penetration: {equipment.MainWeapon.ArmorPenetration:F1}\n" +
            $"Attack Distance: {equipment.MainWeapon.AttackDistance:F1} м\n" +
            $"Ammunition: {(equipment.MainWeapon.Ammunition != null ? equipment.MainWeapon.Ammunition.Name : "No")}\n" +
            $"CoolDown: {equipment.MainWeapon.CoolDown:F1} s"
        );
        if (equipment.MainWeapon.Id != 1)
        {
            _unitMainWeaponButton.onClick.AddListener(() =>
            {
                equipment.UnequipItem(UnitEquipment.EquipmentSlot.MainWeapon);
            });
        }
        else
        {
            _unitMainWeaponButton.onClick.RemoveAllListeners();
        }

        //_unitSecondaryWeaponImage
        _unitSecondaryWeaponName.text = equipment.SecondaryWeapon.Name;
        _unitSecondaryWeaponContext.SetContextText(
            $"Name: {equipment.SecondaryWeapon.Name}\n" +
            $"Damage: {equipment.SecondaryWeapon.Damage:F1}\n" +
            $"Miss chance: {(equipment.SecondaryWeapon.MissChance * 100):F0}%\n" +
            $"Stamina To Attack: {equipment.SecondaryWeapon.StaminaToAttack:F1}\n" +
            $"Armor Penetration: {equipment.SecondaryWeapon.ArmorPenetration:F1}\n" +
            $"Attack Distance: {equipment.SecondaryWeapon.AttackDistance:F1} м\n" +
            $"Ammunition: {(equipment.SecondaryWeapon.Ammunition != null ? equipment.SecondaryWeapon.Ammunition.Name : "No")}\n" +
            $"CoolDown: {equipment.SecondaryWeapon.CoolDown:F1} s"
        );
        if (equipment.SecondaryWeapon.Id != 1)
        {
            _unitSecondaryWeaponButton.onClick.AddListener(() =>
            {
                equipment.UnequipItem(UnitEquipment.EquipmentSlot.SecondaryWeapon);
            });
        }
        else
        {
            _unitSecondaryWeaponButton.onClick.RemoveAllListeners();
        }

        //_unitAmmoImage
        _unitArmorName.text = equipment.Armor.Name;
        _unitArmorContext.SetContextText(
            $"Name: {equipment.Armor.Name}\n" +
            $"Armor Resistance: {equipment.Armor.ArmorResistance:F1}"
        );
        if (equipment.Armor.Id != 1)
        {
            _unitArmorButton.onClick.AddListener(() =>
            {
                equipment.UnequipItem(UnitEquipment.EquipmentSlot.Armor);
            });
        }
        else
        {
            _unitArmorButton.onClick.RemoveAllListeners();
        }

        if (equipment.Ammunition.Count > 0)
        {
            var ammo = equipment.Ammunition.First();

            _unitAmmoImage.gameObject.SetActive(true);
            //_unitAmmoImage
            _unitAmmoName.text = $"{ammo.Name} / {equipment.Ammunition.Count}";
            _unitAmmoContext.SetContextText(
                $"Name: {ammo.Name}\n" +
                $"Damage: {ammo.Damage:F1}\n" +
                $"Armor Penetration: {ammo.ArmorPenetration:F1}"
            );
        }
        else
        {
            _unitAmmoImage.gameObject.SetActive(false);
            _unitAmmoName.text = "No ammunition";
            _unitAmmoContext.SetContextText("No ammunition");
        }
        if (equipment.Ammunition != null && equipment.Ammunition.Count > 0)
        {
            _unitAmmoButton.onClick.AddListener(() =>
            {
                equipment.UnequipItem(UnitEquipment.EquipmentSlot.Ammunition);
            });
        }
        else
        {
            _unitAmmoButton.onClick.RemoveAllListeners();
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

        _unitNameText.text = unit.Name;
        _unitTownText.text = unit.HomeTown != null ? $"{unit.HomeTown.Name}" : "No hometown";

        UpdateStatsUI(unit.Stats);
        UpdateSkillsUI(unit.Skills);
        UpdateBackpackUI(unit);
        UpdateEquipmentUI(unit);

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
        //UpdateStatUI(stats.Hunger); 
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
            UpdateEquipmentUI(unit);
        }
    }

    public void UpdateUnitInfo(object sender, UnitItem.UnitUIToChangeEventArgs args)
    {
        UpdateUnitInfo(args.Unit);
    }

    private void MoveCameraToUnit()
    {
        if (_currentUnit != null)
        {
            CameraManager.Instance.SetCameraPositionToMove(_currentUnit.CenterCoords);
        }
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
