using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitSkills
{
    private float _passiveExperienceRate = 0.1f;
    private BaseSkill _activeSkill;

    private HashSet<BaseSkill> _skills = new();

    public UnitSkills()
    {
        _skills.Add(new ArcherySkill(1, 50, 100));
        _skills.Add(new BuildingSkill(1, 75, 80));
        _skills.Add(new FarmingSkill(1, 50, 75));
        _skills.Add(new SmithingSkill(1, 100, 120));
        _skills.Add(new SwordsmanshipSkill(1, 80, 90));
    }

    public void SetActiveSkill(Type skillType)
    {
        var skill = _skills.FirstOrDefault(s => s.GetType() == skillType);

        if (skill != null)
        {
            _activeSkill = skill;
            Debug.Log($"Active skill set to {skill.Name}");
        }
        else
        {
            Debug.LogWarning($"Skill of type {skillType.Name} not found in skills collection");
        }
    }

    public void SetActiveSkill<T>() where T : ISkill
    {
        SetActiveSkill(typeof(T));
    }

    public void ResetActiveSkill()
    {
        _activeSkill = null;
        Debug.Log($"Active skill reset to null");

    }

    public T GetSkill<T>() where T : BaseSkill
    {
        return _skills.OfType<T>().FirstOrDefault();
    }

    public void Update(float deltaTime)
    {
        _activeSkill?.AddExperience(_passiveExperienceRate * deltaTime);
    }
}