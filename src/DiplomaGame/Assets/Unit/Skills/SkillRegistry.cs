using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillRegistry
{
    private static Dictionary<string, Func<ISkill>> _skillFactories = new();

    public static void RegisterSkill(Func<ISkill> factory)
    {
        var tempSkill = factory();
        _skillFactories[tempSkill.Name] = factory;
    }

    public static ISkill CreateSkillByName(string skillName)
    {
        if (_skillFactories.TryGetValue(skillName, out var factory))
        {
            return factory();
        }

        Debug.LogWarning($"Skill {skillName} not found in registry");
        return null;
    }

    public static List<ISkill> CreateAllSkills()
    {
        return _skillFactories.Values.Select(factory => factory()).ToList();
    }

    public static List<string> GetAvailableSkills()
    {
        return _skillFactories.Keys.ToList();
    }
}