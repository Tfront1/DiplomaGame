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
using System.Threading;
using Assets.Items.Ammunition;
using Assets.Items.Armor;
using Assets.Items.Crafts;
using Assets.Items.Weapon;
using Items.Resource;
using Unity.VisualScripting;

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

        public bool IsSortedSupplies { get; set; } = false;
        private List<Guid> _suppliesKeys = new();

        public bool IsSortedEnemyTowns { get; set; } = false;
        private List<Guid> _enemyTownKeys = new();

        private List<BuildingItem> NotBuildBuildings => _bot.Town.Buildings.FindAll(x => !x.IsBuilt);
        private Backpack ResourcesHave => _bot.Town.TotalBackpack;
        
        private int _townDangerRadius = 15;
        private int _unitDangerRadius = 15;
        private float _chanceToAttackEnemy = 0.5f;

        private bool _isInWar = false;

        private List<BuildingTypes> _buildingPriorities = new();
        
        private List<UnitItem> _builderUnits = new();
        private List<UnitItem> _crafterUnits = new();
        private List<UnitItem> _resourceUnits = new();
        private List<UnitItem> _defenderUnits = new();
        private List<UnitItem> _duelUnits = new();
        private List<UnitItem> _warriorUnits = new();

        private TownItem _enemyTownToAttack;

        private HashSet<UnitItem> _townEnemyUnits = new();
        private Dictionary<UnitItem, UnitItem> _unitEnemies = new();

        private Dictionary<UnitItem, BotTask> _unitAssignments = new();
        private Dictionary<UnitItem, BotTask> _previousAssignments = new();
        
        private LayerMask _unitMask;
        
        public BotBrain(Bot bot)
        {
            _bot = bot;
            _unitMask = LayerMask.GetMask("Units");
            InitializeBotType();
            ConfigureBotParameters();
        }

        public void Think()
        {
            var resetEvent = new ManualResetEventSlim(false);
            ThreadPoolManager.Instance.ExecuteOnMainThread(() =>
            {
                try
                {
                    ProcessBuilding();
                }
                finally
                {
                    resetEvent.Set();
                }
            });
            resetEvent.Wait();

            ProcessCrafting();
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
            _defenderUnits.Clear();
            _duelUnits.Clear();
            _warriorUnits.Clear();

            var availableUnits = new List<UnitItem>(Units);

            var isUnderThreat = CheckForNearbyEnemies();
            if (isUnderThreat)
            {
                var neededDefenders = (int)(UnitCount * 0.8f);
                AssignUnitsWithPreferredRole(availableUnits, neededDefenders, BotTaskType.DefendTown, _defenderUnits);
            }

            CheckForDuels();

            foreach (var duels in _unitEnemies)
            {
                AssignDuelUnits(duels.Key, duels.Value, availableUnits, BotTaskType.Duel, _duelUnits);
            }

            var unitsToEquip = UnitNeedWarItems();
            if (unitsToEquip.Count > 0)
            {
                AssignUnitsToEquip(unitsToEquip, availableUnits);
            }

            for (var i = 0; i < availableUnits.Count; i++)
            {
                if (availableUnits[i].Stats.Stamina.CurrentValue < 60)
                {
                    availableUnits.Remove(availableUnits[i]);
                    i--;
                }
            }

            var needToAttackEnemyTown = CheckForAttackEnemyTown(availableUnits.Count);
            if (needToAttackEnemyTown)
            {
                AssignUnitsWithPreferredRole(availableUnits, availableUnits.Count, BotTaskType.Attack, _warriorUnits);
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

        private void AssignUnitsToEquip(Dictionary<UnitItem, List<IBackpackItem>> units, List<UnitItem> availableUnits)
        {
            foreach (var unit in units)
            {
                availableUnits.Remove(unit.Key);
                var equipAction = new EquipUnitAction(unit.Key, _town.TownHall, unit.Value.First());
                var resetEvent = new ManualResetEventSlim(false);

                ThreadPoolManager.Instance.ExecuteOnMainThread(() =>
                {
                    try
                    {
                        UnitActionManager.Instance.ExecuteImmediately(equipAction);
                    }
                    finally
                    {
                        resetEvent.Set();
                    }
                });
                resetEvent.Wait();
            }
        }

        private void AssignDuelUnits(UnitItem homeUnit, UnitItem enemy, List<UnitItem> availableUnits, BotTaskType taskType, List<UnitItem> roleList)
        {
            availableUnits.Remove(homeUnit);
            _unitAssignments[homeUnit] = new BotTask { Type = taskType, Target = enemy };
            roleList.Add(homeUnit);
        }

        private void ProcessUnits()
        {
            foreach (var defender in _defenderUnits)
            {
                ProcessDefenderUnit(defender);
            }

            foreach (var duelUnit in _duelUnits)
            {
                ProcessDuelUnit(duelUnit);
            }

            foreach (var warriors in _warriorUnits)
            {
                ProcessWarriorUnit(warriors);
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
                var resetEvent = new ManualResetEventSlim(false);
                ThreadPoolManager.Instance.ExecuteOnMainThread(() =>
                {
                    try
                    {
                        ProcessResourceGathererUnit(resourceGatherer);
                    }
                    finally
                    {
                        resetEvent.Set();
                    }
                });
                resetEvent.Wait();
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

        private void ProcessDefenderUnit(UnitItem warrior)
        {
            var nearestEnemy = FindNearestEnemy(warrior);

            if (!IsUnitAlreadyPerformingTask(warrior, BotTaskType.Attack, nearestEnemy))
            {
                if (nearestEnemy != null)
                {
                    var attackAction = new AttackUnitAction(warrior, nearestEnemy);

                    var resetEvent = new ManualResetEventSlim(false);
                    ThreadPoolManager.Instance.ExecuteOnMainThread(() =>
                    {
                        try
                        {
                            UnitActionManager.Instance.ExecuteImmediately(attackAction);
                        }
                        finally
                        {
                            resetEvent.Set();
                        }
                    });
                    resetEvent.Wait();

                    UpdateUnitTask(warrior, BotTaskType.Attack, nearestEnemy);
                }
            }
        }

        private void ProcessDuelUnit(UnitItem duelUnit)
        {
            if(_unitEnemies.TryGetValue(duelUnit, out var enemy))
            {
                var attackAction = new AttackUnitAction(duelUnit, enemy);

                var resetEvent = new ManualResetEventSlim(false);
                ThreadPoolManager.Instance.ExecuteOnMainThread(() =>
                {
                    try
                    {
                        UnitActionManager.Instance.ExecuteImmediately(attackAction);
                    }
                    finally
                    {
                        resetEvent.Set();
                    }
                });
                resetEvent.Wait();

                UpdateUnitTask(duelUnit, BotTaskType.Attack, enemy);
            }
        }

        private void ProcessWarriorUnit(UnitItem warrior)
        {
            if (!_enemyTownToAttack.IsDestroyed)
            {
                var buildingToAttack =
                    _enemyTownToAttack.Buildings.Find(x => x.Building.BuildingType == BuildingTypes.Vault);
                if (buildingToAttack == null)
                {
                    buildingToAttack =
                        _enemyTownToAttack.Buildings.Find(x => x.Building.BuildingType == BuildingTypes.TownHall);
                }

                var attackBuildingAction = new AttackBuildingAction(warrior, buildingToAttack);

                var resetEvent = new ManualResetEventSlim(false);
                ThreadPoolManager.Instance.ExecuteOnMainThread(() =>
                {
                    try
                    {
                        UnitActionManager.Instance.ExecuteImmediately(attackBuildingAction);
                    }
                    finally
                    {
                        resetEvent.Set();
                    }
                });
                resetEvent.Wait();

                UpdateUnitTask(warrior, BotTaskType.Attack, buildingToAttack);
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
                    var resetEvent = new ManualResetEventSlim(false);
                    ThreadPoolManager.Instance.ExecuteOnMainThread(() =>
                    {
                        try
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
                        finally
                        {
                            resetEvent.Set();
                        }
                    });
                    resetEvent.Wait();
                }
            }
            else
            {
                var resetEvent = new ManualResetEventSlim(false);
                ThreadPoolManager.Instance.ExecuteOnMainThread(() =>
                {
                    try
                    {
                        ProcessResourceGathererUnit(builder);
                    }
                    finally
                    {
                        resetEvent.Set();
                    }
                });
                resetEvent.Wait();
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
                    var resetEvent = new ManualResetEventSlim(false);
                    ThreadPoolManager.Instance.ExecuteOnMainThread(() =>
                    {
                        try
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
                        finally
                        {
                            resetEvent.Set();
                        }
                    });
                    resetEvent.Wait();
                }
            }
            else
            {
                var resetEvent = new ManualResetEventSlim(false);
                ThreadPoolManager.Instance.ExecuteOnMainThread(() =>
                {
                    try
                    {
                        ProcessResourceGathererUnit(crafter);
                    }
                    finally
                    {
                        resetEvent.Set();
                    }
                });
                resetEvent.Wait();
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

            var unitsResources = ResourcesUnitsHave();

            foreach (var need in buildingNeeds.GetAllItems())
            {
                var unitResourceQuantity = unitsResources.GetResourceQuantity(need.Item);

                if (unitResourceQuantity < need.Quantity)
                {
                    unitsResources.RemoveItem(need.Item, unitResourceQuantity);
                    return need.Item;
                }
            }

            foreach (var need in craftingNeeds.GetAllItems())
            {
                var unitResourceQuantity = unitsResources.GetResourceQuantity(need.Item);

                if (unitResourceQuantity < need.Quantity)
                {
                    unitsResources.RemoveItem(need.Item, unitResourceQuantity);
                    return need.Item;
                }
            }

            return null;
        }

        private UnitItem FindNearestEnemy(UnitItem unit)
        {
            if (_town.TownHall == null) return null;

            var potentialEnemies = _townEnemyUnits.ToList();

            var enemies = potentialEnemies
                .Where(enemy => enemy != null && !enemy.IsDestroyed && IsEnemy(unit, enemy))
                .ToList();

            if (enemies.Count == 0) return null;

            var resetEvent = new ManualResetEventSlim(false);
            ThreadPoolManager.Instance.ExecuteOnMainThread(() =>
            {
                try
                {
                    enemies.Sort((a, b) =>
                    {
                        var distanceA = Vector2.Distance(unit.Coords, a.Coords);
                        var distanceB = Vector2.Distance(unit.Coords, b.Coords);
                        return distanceA.CompareTo(distanceB);
                    });
                }
                finally
                {
                    resetEvent.Set();
                }
            });

            resetEvent.Wait();

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
        
        private Dictionary<UnitItem, List<IBackpackItem>> UnitNeedWarItems()
        {
            Dictionary<UnitItem, List<IBackpackItem>> units = new();
            var unitsCopy = new List<UnitItem>(Units);

            if (_town.TownHall != null && _town.TownHall.Backpack != null)
            {
                var items = _town.TownHall.Backpack.GetDetailedItems();
                foreach (var item in items)
                {
                    if (item.Key.GetType() != typeof(ResourceElement))
                    {
                        var itemCount = item.Value;

                        foreach (var unit in unitsCopy)
                        {
                            if (itemCount > 0 && unit.UnitEquipment.CanEquip(item.Key) && !unit.UnitEquipment.HasSameItem(item.Key))
                            {
                                if (!units.ContainsKey(unit))
                                {
                                    units[unit] = new List<IBackpackItem>();
                                }

                                units[unit].Add(item.Key);
                                itemCount--;
                            }
                        }
                    }
                }
            }

            return units;
        }

        private bool CheckForNearbyEnemies()
        {
            var townHall = _bot.Town.TownHall;
            if (townHall != null)
            {
                var resetEvent = new ManualResetEventSlim(false);
                ThreadPoolManager.Instance.ExecuteOnMainThread(() =>
                {
                    try
                    {
                        _townEnemyUnits.Clear();
                        var enemies = FindEnemies(GridService.GetWorldPosition(townHall.X, townHall.Y), _townDangerRadius);
                        if (enemies.Count > 0)
                        {
                            _townEnemyUnits.AddRange(enemies);
                        }
                    }
                    finally
                    {
                        resetEvent.Set();
                    }
                });
                resetEvent.Wait();

                if (_townEnemyUnits.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void CheckForDuels()
        {
            var unitsCopy = new List<UnitItem>(Units);
            _unitEnemies.Clear();

            foreach (var unit in unitsCopy)
            {
                var resetEvent = new ManualResetEventSlim(false);
                ThreadPoolManager.Instance.ExecuteOnMainThread(() =>
                {
                    try
                    {
                        var enemies = FindEnemies(unit.Coords, _unitDangerRadius);
                        if (enemies.Count > 0)
                        {
                            _unitEnemies.Add(unit, enemies.First());
                        }
                    }
                    finally
                    {
                        resetEvent.Set();
                    }
                });
                resetEvent.Wait();
            }
        }

        private List<UnitItem> FindEnemies(Vector2 centerCoords, int radius)
        {
            var hitColliders = Physics2D.OverlapCircleAll(centerCoords, radius * MapConfig.CellSize, _unitMask);

            List<UnitItem> enemies = new();

            if (hitColliders.Length > 0)
            {
                foreach (var collider in hitColliders)
                {
                    var unit = collider.GetComponent<UnitItem>();
                    if (unit != null && unit.HomeTown != null && unit.HomeTown.Id != _town.Id)
                    {
                        enemies.Add(unit);
                    }
                }
            }

            return enemies;
        }

        private bool CheckForAttackEnemyTown(int availableUnitsCount)
        {
            if (_enemyTownKeys.Count == 0)
                return false;

            var unitsCopy = Units.ToList();
            var allBackpacksAlmostFull = unitsCopy.All(unit =>
            {
                var backpackPercentage = unit.Backpack.GetFillPercentage();

                return backpackPercentage > 0.8f;
            });

            if (allBackpacksAlmostFull)
            {
                _isInWar = false;
                return false;
            }

            var enemyTown = TownRegistry.TownList.Find(x => x.Id == _enemyTownKeys.First() && !x.IsDestroyed);

            if (enemyTown.Units.Count <= availableUnitsCount)
            {
                if (_isInWar || GameRandom.Random.NextDouble() < _chanceToAttackEnemy)
                {
                    _enemyTownToAttack = enemyTown;
                    _isInWar = true;
                    return true;
                }
            }

            _isInWar = false;
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
                if(!HasBuiltBuilding(buildingType) && !IsBuildingCreating(buildingType))
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

            if (!HasBuiltBuilding(BuildingTypes.Blacksmith) && !IsBuildingCreating(BuildingTypes.Blacksmith))
            {
                var blacksmith = BuildingsConfig.Buildings.Find(x => x.BuildingType == BuildingTypes.Blacksmith);
                if (blacksmith != null)
                {
                    return blacksmith;
                }
            }

            if ((!HasBuiltBuilding(BuildingTypes.Vault) || ResourcesHave.CurrentCapacity > ResourcesHave.MaxCapacity * 0.7f)
                && !IsBuildingCreating(BuildingTypes.Vault))
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

        private void ProcessCrafting()
        {
            var craftingRecipe = CraftNeedForUnits();

            if(craftingRecipe == null)
                return;

            foreach (var building in Buildings)
            {
                if (building.IsBuilt && craftingRecipe.WhereToCraftId == building.Building.Id)
                {
                    if (building.BuildingCraftingSystem != null && !building.BuildingCraftingSystem.IsCrafting)
                    {
                        var resetEvent = new ManualResetEventSlim(false);
                        ThreadPoolManager.Instance.ExecuteOnMainThread(() =>
                        {
                            try
                            {
                                building.BuildingCraftingSystem.StartCraft(craftingRecipe);
                            }
                            finally
                            {
                                resetEvent.Set();
                            }
                        });
                        resetEvent.Wait();
                    }
                }
            }
        }

        private CraftingRecipe CraftNeedForUnits()
        {
            var allCraftingBuildingsActive = true;
            var hasCraftingBuildings = false;

            foreach (var building in Buildings)
            {
                if (building.BuildingCraftingSystem != null)
                {
                    hasCraftingBuildings = true;
                    if (!building.BuildingCraftingSystem.IsCrafting)
                    {
                        allCraftingBuildingsActive = false;
                        break;
                    }
                }
            }

            if (hasCraftingBuildings && allCraftingBuildingsActive)
            {
                return null;
            }

            var unitsCopy = new List<UnitItem>(Units);

            foreach (var unit in unitsCopy)
            {
                if (unit.UnitEquipment.MainWeapon.Id == 1)
                {
                    var rand = GameRandom.Random;
                    var randomIndex = rand.Next(1, WeaponConfig.WeaponElements.Count);
                    var itemToCraft = WeaponConfig.WeaponElements[randomIndex];

                    var craftingRecipe = GetCraftingRecipe(itemToCraft, typeof(WeaponElement));

                    if (craftingRecipe != null)
                    {
                        return craftingRecipe;
                    }
                }

                if (unit.UnitEquipment.MainWeapon.Ammunition != null &&
                    unit.UnitEquipment.Ammunition.Find(x =>
                        x.Id == unit.UnitEquipment.MainWeapon
                            .Ammunition.Id) == null)
                {
                    if (ResourcesHave.GetResourceQuantity(unit.UnitEquipment.MainWeapon.Ammunition) == 0)
                    {
                        var itemToCraft = unit.UnitEquipment.MainWeapon.Ammunition;
                        var craftingRecipe = GetCraftingRecipe(itemToCraft, typeof(AmmunitionElement));

                        if (craftingRecipe != null)
                        {
                            return craftingRecipe;
                        }
                    }
                }

                if (unit.UnitEquipment.Armor.Id == 1)
                {
                    var rand = GameRandom.Random;
                    var randomIndex = rand.Next(1, ArmorConfig.ArmorElements.Count);
                    var itemToCraft = ArmorConfig.ArmorElements[randomIndex];

                    var craftingRecipe = GetCraftingRecipe(itemToCraft, typeof(ArmorElement));

                    if (craftingRecipe != null)
                    {
                        return craftingRecipe;
                    }
                }
            }

            return null;
        }

        private CraftingRecipe GetCraftingRecipe(IBackpackItem itemToCraft, Type craftType)
        {
            var craftingRecipe =
                CraftingRecipesConfig.CraftingRecipes.Find(x =>
                    x.ResultType == craftType && x.ResultId == itemToCraft.Id);

            return craftingRecipe;
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

                    var deliveredResources = craftSystem.DeliveredResources;

                    foreach (var resource in resources)
                    {
                        var delivered = deliveredResources.Find(x => x.BackpackItem == resource.BackpackItem);
                        var deliveredQuantity = delivered?.Quantity ?? 0;

                        var remainingQuantity = Math.Max(0, resource.Quantity - deliveredQuantity);

                        resourcesNeed.SetMaxCapacity(resourcesNeed.MaxCapacity + remainingQuantity);
                        resourcesNeed.AddItem(resource.BackpackItem, remainingQuantity);
                    }
                }
            }
            
            return resourcesNeed;
        }

        private Backpack ResourcesUnitsHave()
        {
            Backpack resourcesHave = new(0);

            var unitsCopy = new List<UnitItem>(Units);

            foreach (var unit in unitsCopy)
            {
                if (unit.Backpack != null)
                {
                    var allItems = unit.Backpack.GetDetailedItems();
                    foreach (var item in allItems)
                    {
                        resourcesHave.SetMaxCapacity(item.Value + resourcesHave.MaxCapacity);
                        resourcesHave.AddItem(item.Key, item.Value);
                    }
                }
            }

            return resourcesHave;
        }

        private bool HasBuiltBuilding(BuildingTypes type)
        {
            return Buildings.Exists(x => x.Building.BuildingType == type && x.IsBuilt);
        }

        private bool IsBuildingCreating(BuildingTypes type)
        {
            return Buildings.Exists(x => x.Building.BuildingType == type && !x.IsBuilt);
        }

        public void SortNearestSupplies()
        {
            if (_town.TownHall != null)
            {
                var suppliesDict = ItemListRegistry.GetList<SupplyItem>();
                var currentPosition = _town.TownHall.CenterCoords;

                var sortedKeys = suppliesDict.Keys.ToList().OrderBy(guid =>
                    Vector2.Distance(currentPosition, suppliesDict.GetValue(guid).CenterCoords)).ToList();

                _suppliesKeys = sortedKeys;
                IsSortedSupplies = true;
            }
        }

        public void SortNearestEnemyTowns()
        {
            if (_town.TownHall != null)
            {
                var enemyTowns = TownRegistry.TownList.ToList();
                var currentPosition = _town.TownHall.CenterCoords;

                var sortedTownIds = enemyTowns
                    .Where(townItem => townItem.Id != _town.Id)
                    .OrderBy(townItem => Vector2.Distance(currentPosition, townItem.TownHall.CenterCoords))
                    .Select(townItem => townItem.Id)
                    .ToList();

                _enemyTownKeys = sortedTownIds;
                IsSortedEnemyTowns = true;
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
                    _townDangerRadius = 20;
                    _unitDangerRadius = 8;
                    _chanceToAttackEnemy = 0.05f;
                    break;

                case BotTypes.Balanced:
                    _townDangerRadius = 15;
                    _unitDangerRadius = 6;
                    _chanceToAttackEnemy = 0.03f;
                    break;

                case BotTypes.Passive:
                    _townDangerRadius = 10;
                    _unitDangerRadius = 4;
                    _chanceToAttackEnemy = 0.01f;
                    break;
            }

            SetBuildingPriorities();
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
