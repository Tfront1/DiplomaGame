using UnityEngine;

public class BaseSkill : ISkill
{
    private string _name;
    private float _currentLevel;
    private float _maxLevel;
    private float _experience;
    private float _baseExperienceToLevel;
    private float _experienceMultiplier;

    public virtual string Name => _name;
    public float CurrentLevel { get => _currentLevel; set => _currentLevel = Mathf.Clamp(value, 1, _maxLevel); }
    public float MaxLevel { get => _maxLevel; set => _maxLevel = value; }
    public float Experience { get => _experience; set => _experience = Mathf.Max(0, value); }

    public float ExperienceToNextLevel =>
        _baseExperienceToLevel * Mathf.Pow(_experienceMultiplier, _currentLevel);

    public BaseSkill(string name, float initialLevel = 1, float maxLevel = 100f,
                    float baseExperienceToLevel = 100f, float experienceMultiplier = 1.1f)
    {
        _name = name;
        _maxLevel = maxLevel;
        _currentLevel = Mathf.Clamp(initialLevel, 1, maxLevel);
        _experience = 0f;
        _baseExperienceToLevel = baseExperienceToLevel;
        _experienceMultiplier = experienceMultiplier;
    }

    public virtual void AddExperience(float amount)
    {
        if (_currentLevel >= _maxLevel)
            return;

        _experience += amount;

        while (CanLevelUp() && _currentLevel < _maxLevel)
        {
            LevelUp();
        }
    }

    public bool CanLevelUp()
    {
        return _experience >= ExperienceToNextLevel && _currentLevel < _maxLevel;
    }

    public virtual void LevelUp()
    {
        if (_currentLevel >= _maxLevel)
            return;

        _experience -= ExperienceToNextLevel;
        _currentLevel++;

        Debug.Log($"{_name} skill leveled up to {_currentLevel}!");
    }

    public float GetProgressPercentage()
    {
        if (_currentLevel >= _maxLevel)
            return 1.0f;

        return _experience / ExperienceToNextLevel;
    }

    public override string ToString()
    {
        if (_currentLevel >= _maxLevel)
            return $"{_name}: Level {_currentLevel} (MAX)";

        return $"{_name}: Level {_currentLevel} - {_experience:F1}/{ExperienceToNextLevel:F1} ({GetProgressPercentage():P1})";
    }
}