using System.Collections.Generic;
using UnityEngine;

public class UnitSkills : IUnitSkills
{
    private float _passiveExperienceRate = 0.1f;
    private BaseSkill _activeSkill;

    public HashSet<ISkill> _skills = new();

    public UnitSkills()
    {
        _skills.Add(new ArcherySkill(1, 50, 100));
        _skills.Add(new BuildingSkill(1, 75, 80));
        _skills.Add(new FarmingSkill(1, 50, 75));
        _skills.Add(new SmithingSkill(1, 100, 120));
        _skills.Add(new SwordsmanshipSkill(1, 80, 90));
    }

    public void SetActiveSkill(BaseSkill baseSkill)
    {
        _activeSkill = baseSkill;
        Debug.Log($"Active skill set to {baseSkill}");
    }

    public void ResetActiveSkill()
    {
        _activeSkill = null;
        Debug.Log($"Active skill reset to null");

    }

    public void Update(float deltaTime)
    {
        _activeSkill?.AddExperience(_passiveExperienceRate * deltaTime);
    }
}