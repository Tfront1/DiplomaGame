using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GroupManager : MonoBehaviour
{
    private static GroupManager _instance;
    private Dictionary<Guid, UnitGroup> groups = new();

    public static GroupManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GroupManager>();

                if (_instance == null)
                {
                    var obj = new GameObject("GroupManager");
                    _instance = obj.AddComponent<GroupManager>();
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
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void Initialize()
    {
        groups = new Dictionary<Guid, UnitGroup>();
    }

    public UnitGroup CreateGroup(Guid id, UnitItem leader)
    {
        var group = new UnitGroup(id, leader);
        groups[id] = group;
        return group;
    }

    public void RemoveGroup(Guid id)
    {
        if (groups.ContainsKey(id))
            groups.Remove(id);
    }

    public UnitGroup GetGroup(Guid id)
    {
        return groups.GetValueOrDefault(id);
    }

    public bool HasGroup(Guid id)
    {
        return groups.ContainsKey(id);
    }

    public List<UnitGroup> GetAllGroups()
    {
        return groups.Values.ToList();
    }
}