using Town;
using UnityEngine;

public static class TownUIExtensions
{
    private static TownUIManager _uiInstance;

    public static TownUIManager GetOrCreateTownUIManager()
    {
        if (_uiInstance == null)
        {
            _uiInstance = Object.FindObjectOfType<TownUIManager>();
            if (_uiInstance == null)
            {
                var uiManagerObj = new GameObject("TownUIManager");
                _uiInstance = uiManagerObj.AddComponent<TownUIManager>();
            }
        }
        return _uiInstance;
    }

    public static void SetAsPlayerTown(this TownItem town)
    {
        var uiManager = GetOrCreateTownUIManager();
        uiManager.SetPlayerTown(town);

        BuildingMenuManager.Instance.SetCurrentTown(town);
    }

    public static void NotifyUIChanged(this TownItem town)
    {
        if (town.IsUnitControlTown)
        {
            var uiManager = GetOrCreateTownUIManager();
            uiManager.RefreshUI(town);

            BuildingMenuManager.Instance.RefreshUI();
        }
    }

    public static void OpenBuildingMenu(this TownItem town)
    {
        if (town.IsUnitControlTown)
        {
            BuildingMenuManager.Instance.SetCurrentTown(town);
            BuildingMenuManager.Instance.ToggleBuildingMenu();
        }
    }
}