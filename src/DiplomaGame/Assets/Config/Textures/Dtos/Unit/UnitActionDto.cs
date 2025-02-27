using System.Collections.Generic;

[System.Serializable]
public class UnitActionDto
{
    public int UnitId;
    public string UnitFolder;
    public List<UnitActionGroupDto> ActionGroups;
}