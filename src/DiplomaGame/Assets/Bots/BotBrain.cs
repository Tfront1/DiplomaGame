using Game;
using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Items.Interfaces;
using Items.Resource.BackPack;
using Town;
using UnitAction;
using static Building;
using UnityEngine;

namespace Bots
{
    public class BotBrain
    {
        private Bot _bot;
        private BotTypes _botType;
        
        private TownItem _town => _bot.Town;
        private int MaxUnitCount => _bot.Town.MaxUnits;
        private int UnitCount => _bot.Town.UnitsCount;
        private List<UnitItem> Units => _bot.Town.Units;
        private List<BuildingItem> Buildings => _bot.Town.Buildings;

        private bool _isSortedSupplies = false;
        private List<Guid> _suppliesKeys = new();

        private List<BuildingItem> NotBuildBuildings => _bot.Town.Buildings.FindAll(x => !x.IsBuilt);
        private Backpack ResourcesHave => _bot.Town.TotalBackpack;
        
        private int _dangerRadius = 15;

        private List<BuildingTypes> _buildingPriorities = new();
        
        private List<UnitItem> _builderUnits = new();
        private List<UnitItem> _crafterUnits = new();
        private List<UnitItem> _resourceUnits = new();
        private List<UnitItem> _warriorUnits = new();

        private HashSet<UnitItem> _enemyUnits = new();

        private Dictionary<UnitItem, BotTask> _unitAssignments = new();
        private Dictionary<UnitItem, BotTask> _previousAssignments = new();

        public BotBrain(Bot bot)
        {
            _bot = bot;
            InitializeBotType();
            ConfigureBotParameters();
            
        }

        public void Think()
        {
            if (!_isSortedSupplies)
            {
                SortNearestSupplies();
            }

            ProcessBuilding();
            AssignUnitRoles();
            ProcessUnits();
        }

        private int GetCrafterNeed()
        {
            var craftingBuildingsCount = 0;
            foreach (var building in Buildings)
            {
                if (building.BuildingCraftingSystem != null && building.BuildingCraftingSystem.IsCrafting)
                {
                    craftingBuildingsCount++;
                }
            }

            return craftingBuildingsCount;
        }

        private void AssignUnitRoles()
        {
            _builderUnits.Clear();
            _crafterUnits.Clear();
            _resourceUnits.Clear();
            _warriorUnits.Clear();

            var availableUnits = new List<UnitItem>(Units);
            var isUnderThreat = CheckForNearbyEnemies();

            if (isUnderThreat)
            {
                var neededDefenders = (int)(UnitCount * 0.8f);
                AssignUnitsWithPreferredRole(availableUnits, neededDefenders, BotTaskType.DefendTown, _warriorUnits);
            }

            for (var i = 0; i < availableUnits.Count; i++)
            {
                if (availableUnits[i].Stats.Stamina.CurrentValue < 60)
                {
                    availableUnits.Remove(availableUnits[i]);
                    i--;
                }
            }

            var buildingsInProgress = NotBuildBuildings.Count;
            if (buildingsInProgress > 0 && availableUnits.Count > 0)
            {
                var neededBuilders = Math.Min(
                    Math.Max(buildingsInProgress, 0),
                    Math.Max(1, availableUnits.Count / 3)
                );
                AssignUnitsWithPreferredRole(availableUnits, neededBuilders, BotTaskType.Build, _builderUnits);
            }

            var neededCrafters = GetCrafterNeed();
            if (neededCrafters > 0 && availableUnits.Count > 0)
            {
                var crafterCount = Math.Min(
                    neededCrafters,
                    availableUnits.Count / 4
                );
                AssignUnitsWithPreferredRole(availableUnits, crafterCount, BotTaskType.Craft, _crafterUnits);
            }

            if (availableUnits.Count > 0)
            {
                AssignUnitsWithPreferredRole(availableUnits, availableUnits.Count, BotTaskType.GatherResource, _resourceUnits);
            }

            _previousAssignments = new Dictionary<UnitItem, BotTask>(_unitAssignments);
        }

        private void AssignUnitsWithPreferredRole(
            List<UnitItem> availableUnits,
            int count,
            BotTaskType taskType,
            List<UnitItem> roleList)
        {
            var assigned = 0;

            for (var i = availableUnits.Count - 1; i >= 0; i--)
            {
                var unit = availableUnits[i];
                if (_previousAssignments.TryGetValue(unit, out var previousTask) && previousTask.Type == taskType)
                {
                    roleList.Add(unit);
                    _unitAssignments.TryAdd(unit, previousTask);
                    availableUnits.RemoveAt(i);
                    assigned++;
                    if (assigned >= count)
                    {
                        return;
                    }
                }
            }

            while (assigned < count && availableUnits.Count > 0)
            {
                var unit = availableUnits[0];
                roleList.Add(unit);
                _unitAssignments[unit] = new BotTask { Type = taskType, Target = null };
                availableUnits.RemoveAt(0);
                assigned++;
            }
        }

        private void ProcessUnits()
        {
            foreach (var warrior in _warriorUnits)
            {
                ProcessWarriorUnit(warrior);
            }

            foreach (var builder in _builderUnits)
            {
                ProcessBuilderUnit(builder);
            }

            foreach (var crafter in _crafterUnits)
            {
                ProcessCrafterUnit(crafter);
            }

            foreach (var resourceGatherer in _resourceUnits)
            {
                ProcessResourceGathererUnit(resourceGatherer);
            }
        }

        private bool IsUnitAlreadyPerformingTask(UnitItem unit, BotTaskType expectedTask, object target)
        {
            if (target == null)
            {
                return _unitAssignments.TryGetValue(unit, out var currentTask) &&
                       currentTask.Type == expectedTask &&
                       currentTask.Target == null;
            }

            return _unitAssignments.TryGetValue(unit, out var task) &&
                   task.Type == expectedTask &&
                   Equals(task.Target, target);
        }

        private void UpdateUnitTask(UnitItem unit, BotTaskType taskType, object target)
        {
            if (_unitAssignments.TryGetValue(unit, out var task))
            {
                task.Type = taskType;
                task.Target = target;
            }
            else
            {
                _unitAssignments[unit] = new BotTask { Type = taskType, Target = target };
            }
        }

        private void ProcessWarriorUnit(UnitItem warrior)
        {
            var nearestEnemy = FindNearestEnemy(warrior);

            if (!IsUnitAlreadyPerformingTask(warrior, BotTaskType.Attack, nearestEnemy))
            {
                if (nearestEnemy != null)
                {
                    var attackAction = new AttackUnitAction(warrior, nearestEnemy);
                    UnitActionManager.Instance.ExecuteImmediately(attackAction);
                    UpdateUnitTask(warrior, BotTaskType.Attack, nearestEnemy);
                }
            }
        }

        private void ProcessBuilderUnit(UnitItem builder)
        {
            if (NotBuildBuildings.Count > 0)
            {
                var targetBuilding = NotBuildBuildings[0];

                var isAlreadyBuilding = _town.BuildingTownOrder.IsUnitAssignedToAnyOrder(builder) &&
                                         IsUnitAlreadyPerformingTask(builder, BotTaskType.Build, targetBuilding);

                if (!isAlreadyBuilding)
                {
                    if (targetBuilding != null && _town.BuildingTownOrder.AssignUnitToOrder(builder, targetBuilding))
                    {
                        UpdateUnitTask(builder, BotTaskType.Build, targetBuilding);
                    }
                    else
                    {
                        ProcessResourceGathererUnit(builder);
                    }
                }
            }
            else
            {
                ProcessResourceGathererUnit(builder);
            }
        }

        private void ProcessCrafterUnit(UnitItem crafter)
        {
            var craftingBuilding = FindBuildingNeedingCraftingResources();

            if (craftingBuilding != null)
            {
                var isAlreadyCrafting = craftingBuilding.BuildingCraftingSystem.HasAssignedUnit(crafter) &&
                                         IsUnitAlreadyPerformingTask(crafter, BotTaskType.Craft, craftingBuilding);

                if (!isAlreadyCrafting)
                {
                    if (craftingBuilding.BuildingCraftingSystem.AssignUnitToCraft(crafter))
                    {
                        UpdateUnitTask(crafter, BotTaskType.Craft, craftingBuilding);
                    }
                    else
                    {
                        ProcessResourceGathererUnit(crafter);
                    }
                }
            }
            else
            {
                ProcessResourceGathererUnit(crafter);
            }
        }

        private void ProcessResourceGathererUnit(UnitItem gatherer)
        {
            if (gatherer.Backpack.IsFull())
            {
                if (!IsUnitAlreadyPerformingTask(gatherer, BotTaskType.BringBackResources, null))
                {
                    var bringBackResources = new BringBackResourcesAction(gatherer);
                    UnitActionManager.Instance.ExecuteImmediately(bringBackResources);
                    UpdateUnitTask(gatherer, BotTaskType.BringBackResources, null);
                }
            }
            else
            {
                var resourceToGather = DetermineResourcePriority();
                if (resourceToGather != null)
                {
                    var resource = FindNearestResource(resourceToGather);

                    if (!IsUnitAlreadyPerformingTask(gatherer, BotTaskType.GatherResource, resource))
                    {
                        if (resource != null)
                        {
                            var collectSupplyAction = new CollectSupplyAction(gatherer, resource);
                            UnitActionManager.Instance.ExecuteImmediately(collectSupplyAction);
                            UpdateUnitTask(gatherer, BotTaskType.GatherResource, resource);
                        }
                    }
                }
            }
        }

        private IBackpackItem DetermineResourcePriority()
        {
            var buildingNeeds = ResourcesNeedForBuilding();

            var craftingNeeds = ResourcesNeedForCrafting();

            foreach (var need in buildingNeeds.GetAllItems())
            {
                if (need.Quantity > 0)
                {
                    return need.Item;
                }
            }

            foreach (var need in craftingNeeds.GetAllItems())
            {
                if (need.Quantity > 0)
                {
                    return need.Item;
                }
            }

            return ResourcesConfig.ResourceElements.First();
        }

        private UnitItem FindNearestEnemy(UnitItem unit)
        {
            if (_town.TownHall == null) return null;

            var potentialEnemies = _enemyUnits.ToList();

            var enemies = potentialEnemies
                .Where(enemy => enemy != null && !enemy.IsDestroyed && IsEnemy(unit, enemy))
                .ToList();

            if (enemies.Count == 0) return null;
            
            enemies.Sort((a, b) =>
            {
                var distanceA = Vector2.Distance(unit.Coords, a.Coords);
                var distanceB = Vector2.Distance(unit.Coords, b.Coords);
                return distanceA.CompareTo(distanceB);
            });

            return enemies[0];
        }

        private bool IsEnemy(UnitItem unit, UnitItem otherUnit)
        {
            return unit.HomeTown.Id != otherUnit.HomeTown.Id;
        }

        private BuildingItem FindBuildingNeedingCraftingResources()
        {
            foreach (var building in Buildings)
            {
                if (building.BuildingCraftingSystem != null &&
                    building.BuildingCraftingSystem.IsCrafting &&
                    !building.BuildingCraftingSystem.IsAllDelivered)
                {
                    return building;
                }
            }
            return null;
        }

        private SupplyItem FindNearestResource(IBackpackItem resourceType)
        {
            var suppliesList = ItemListRegistry.GetList<SupplyItem>();

            for (var i = 0; i < _suppliesKeys.Count; i++)
            {
                var key = _suppliesKeys[i];

                if (suppliesList.TryGetValue(key, out var supply))
                {
                    if (supply.Backpack.HasResource(resourceType))
                    {
                        return supply;
                    }
                }
                else
                {
                    _suppliesKeys.RemoveAt(i);
                    i--;
                }
            }

            return null;
        }

        private bool CheckForNearbyEnemies()
        {
            var townHall = _bot.Town.TownHall;
            if (townHall != null)
            {
                var enemies = _enemyUnits
                    .Where(enemy => enemy != null && !enemy.IsDestroyed && enemy.HomeTown.Id != _town.Id)
                    .ToList();

                if (enemies.Count > 0)
                {
                    return true;
                }
            }
            
            return false;
        }

        private void ProcessBuilding()
        {
            var needToBuild = NeedToBuild();
            if (needToBuild != null)
            {
                if (_town.TownHall != null)
                {
                    var thCoords = _town.TownHall.Coords;
                    var isValidPosition = false;
                    var maxAttempts = 20;
                    var currentAttempt = 0;
                    var currentMaxDistance = 15;

                    while (!isValidPosition && currentAttempt < maxAttempts)
                    {
                        var buildingCoords = GameRandom.GetRandomCoords(thCoords, currentMaxDistance,
                            needToBuild.WidthCell, needToBuild.HeightCell);

                        if (GridService.CanPlaceAtPosition(buildingCoords,
                                new Vector2Int(needToBuild.WidthCell, needToBuild.HeightCell),
                                GridRegistry.GetAllGridsList().ToArray()))
                        {
                            isValidPosition = true;
                            BuildingManager.BuildWithFoundation(buildingCoords, needToBuild, _town);
                        }

                        currentAttempt++;

                        if (currentAttempt % 5 == 0)
                        {
                            currentMaxDistance += 5;
                        }
                    }

                    if (!isValidPosition)
                    {
                        Debug.Log($"No place fount for building: {needToBuild.Name}");
                    }
                }
            }
        }

        private Building NeedToBuild()
        {
            foreach (var buildingType in _buildingPriorities)
            {
                if(!HasBuilding(buildingType))
                {
                    var buildingToBuild = BuildingsConfig.Buildings.Find(x => x.BuildingType == buildingType);
                    if (buildingToBuild != null)
                    {
                        return buildingToBuild;
                    }
                }
            }

            if (UnitCount >= MaxUnitCount * 0.8f)
            {
                var housing = BuildingsConfig.Buildings.Find(x => x.BuildingType == BuildingTypes.Housing);
                if (housing != null)
                {
                    return housing;
                }
            }

            if (!HasBuilding(BuildingTypes.Blacksmith))
            {
                var blacksmith = BuildingsConfig.Buildings.Find(x => x.BuildingType == BuildingTypes.Blacksmith);
                if (blacksmith != null)
                {
                    return blacksmith;
                }
            }

            if (!HasBuilding(BuildingTypes.Vault) && ResourcesHave.CurrentCapacity > ResourcesHave.MaxCapacity * 0.7f)
            {
                var vault = BuildingsConfig.Buildings.Find(x => x.BuildingType == BuildingTypes.Vault);
                if (vault != null)
                {
                    return vault;
                }
            }

            return null;
        }

        private Backpack ResourcesNeedForBuilding()
        {
            var resourcesToNeed = _town.BuildingTownOrder.GetAllNeedResources();
            var updatedResources = new Dictionary<IBackpackItem, int>();

            foreach (var resource in resourcesToNeed.GetDetailedItems())
            {
                var resourceHave = ResourcesHave.GetResourceQuantity(resource.Key);
                var remainingNeeded = resource.Value - resourceHave;

                if (remainingNeeded > 0)
                {
                    updatedResources[resource.Key] = remainingNeeded;
                }
            }

            var resultBackpack = new Backpack(resourcesToNeed.MaxCapacity);
            foreach (var resource in updatedResources)
            {
                resultBackpack.AddItem(resource.Key, resource.Value);
            }

            return resultBackpack;
        }

        private Backpack ResourcesNeedForCrafting()
        {
            Backpack resourcesNeed = new(0);

            foreach (var building in Buildings)
            {
                if (building.BuildingCraftingSystem != null && building.BuildingCraftingSystem.IsCrafting)
                {
                    var craftSystem = building.BuildingCraftingSystem;
                    var resources = craftSystem.GetRemainingResources();

                    foreach (var resource in resources)
                    {
                        resourcesNeed.SetMaxCapacity(resourcesNeed.MaxCapacity + resource.Quantity);
                        resourcesNeed.AddItem(resource.BackpackItem, resource.Quantity);
                    }
                }
            }

            var updatedResources = new Dictionary<IBackpackItem, int>();

            foreach (var resource in resourcesNeed.GetDetailedItems())
            {
                var resourceHave = ResourcesHave.GetResourceQuantity(resource.Key);
                var remainingNeeded = resource.Value - resourceHave;

                if (remainingNeeded > 0)
                {
                    updatedResources[resource.Key] = remainingNeeded;
                }
            }

            resourcesNeed.Clear();

            var newMaxCapacity = 0;
            foreach (var resource in updatedResources)
            {
                newMaxCapacity += resource.Value;
            }
            resourcesNeed.SetMaxCapacity(newMaxCapacity);

            foreach (var resource in updatedResources)
            {
                resourcesNeed.AddItem(resource.Key, resource.Value);
            }

            return resourcesNeed;
        }

        private bool HasBuilding(BuildingTypes type)
        {
            return Buildings.Exists(x => x.Building.BuildingType == type);
        }

        private void SortNearestSupplies()
        {
            if (_town.TownHall != null)
            {
                var suppliesDict = ItemListRegistry.GetList<SupplyItem>();
                var currentPosition = _town.TownHall.CenterCoords;

                var sortedKeys = suppliesDict.Keys.ToList().OrderBy(guid =>
                    Vector2.Distance(currentPosition, suppliesDict.GetValue(guid).CenterCoords)).ToList();

                _suppliesKeys = sortedKeys;
                _isSortedSupplies = true;
            }
        }

        private void InitializeBotType()
        {
            var allTypes = (BotTypes[])Enum.GetValues(typeof(BotTypes));

            _botType = allTypes[GameRandom.Random.Next(0, allTypes.Length)];
        }

        private void ConfigureBotParameters()
        {
            switch (_botType)
            {
                case BotTypes.Aggressive:
                    _dangerRadius = 20;
                    break;

                case BotTypes.Balanced:
                    _dangerRadius = 15;
                    break;

                case BotTypes.Passive:
                    _dangerRadius = 10;
                    break;
            }

            SetBuildingPriorities();
        }

        public void OnUnitChangedCellHandler(UnitItem unit)
        {
            if (unit.HomeTown.Id != _town.Id)
            {
                return;
            }

            if (_town.TownHall != null)
            {
                var townCell = _town.TownHall.Coords;

                if (UnitRegistry.IsUnitInCellRadius(townCell, _dangerRadius, unit))
                {
                    _enemyUnits.Add(unit);
                }
                else
                {
                    _enemyUnits.Remove(unit);
                }
            }
        }

        private void SetBuildingPriorities()
        {
            _buildingPriorities.Clear();

            switch (_botType)
            {
                case BotTypes.Aggressive:
                    _buildingPriorities.Add(BuildingTypes.Blacksmith);
                    _buildingPriorities.Add(BuildingTypes.Housing);
                    _buildingPriorities.Add(BuildingTypes.Vault);
                    break;

                case BotTypes.Balanced:
                    _buildingPriorities.Add(BuildingTypes.Vault);
                    _buildingPriorities.Add(BuildingTypes.Blacksmith);
                    _buildingPriorities.Add(BuildingTypes.Housing);
                    break;

                case BotTypes.Passive:
                    _buildingPriorities.Add(BuildingTypes.Vault);
                    _buildingPriorities.Add(BuildingTypes.Housing);
                    _buildingPriorities.Add(BuildingTypes.Blacksmith);
                    break;
            }
        }
    }

    public enum BotTypes
    {
        Aggressive,
        Balanced, 
        Passive
    }
}
