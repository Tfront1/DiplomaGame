using System;
using System.Collections.Generic;
using System.Linq;

public class UnitSkills
{
    private float _passiveExperienceRate = 1.0f;
    private BaseSkill _activeSkill;

    public HashSet<BaseSkill> Skills { get; set; } = new();

    public delegate void UpdateEventHandler();
    public event UpdateEventHandler OnUpdate;

    public UnitSkills()
    {
        Skills.Add(new ArcherySkill(1, 50, 100));
        Skills.Add(new SwordsmanshipSkill(1, 80, 90));
        Skills.Add(new BuildingSkill(1, 75, 80));
        Skills.Add(new FarmingSkill(1, 50, 75));
        Skills.Add(new SmithingSkill(1, 100, 120));
    }

    public void SetActiveSkill(Type skillType)
    {
        var skill = Skills.FirstOrDefault(s => s.GetType() == skillType);

        if (skill != null)
        {
            _activeSkill = skill;
        }
    }

    public void SetActiveSkill<T>() where T : ISkill
    {
        SetActiveSkill(typeof(T));
    }

    public void ResetActiveSkill()
    {
        _activeSkill = null;
    }

    public T GetSkill<T>() where T : BaseSkill
    {
        return Skills.OfType<T>().FirstOrDefault();
    }

    public void Update(float deltaTime)
    {
        if (_activeSkill != null)
        {
            _activeSkill.AddExperience(_passiveExperienceRate * deltaTime);
            OnUpdate?.Invoke();
        }
    }
}