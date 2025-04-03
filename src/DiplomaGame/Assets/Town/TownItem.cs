using System;
using System.Collections.Generic;
using Assets.Items.Crafts;
using Items.Resource.BackPack;
using static UnitItem;

namespace Town
{
    public class TownItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsUnitControlTown { get; set; }
        public List<BuildingItem> Buildings { get; set; } = new();
        public List<UnitItem> Units { get; set; } = new();
        public BuildingItem TownHall { get; set; }
        public Backpack TotalBackpack { get; set; }
        //ToDo: Logic for possible crafts
        public List<CraftingRecipe> PossibleCrafts { get; set; }
        public int DiedUnits { get; set; } = 0;
        public BuildingTownOrder BuildingTownOrder { get; set; } = new();

        public TownItem(string name, Guid id, bool isUnitControlTown = true)
        {
            Name = name;
            Id = id;
            TownRegistry.AddTown(this);
            IsUnitControlTown = isUnitControlTown;
            if (IsUnitControlTown)
            {
                this.SetAsPlayerTown();
                this.NotifyUIChanged();
            }
        }

        public TownItem(string name, Guid id, BuildingItem townHall)
        {
            Name = name;
            Id = id;
            TownHall = townHall;
            TownRegistry.AddTown(this);
        }

        public void AddBuilding(BuildingItem building)
        {
            if (!Buildings.Contains(building) && TownHall != building)
            {
                if (building.Building.BuildingType == Building.BuildingTypes.TownHall)
                {
                    TownHall = building;
                }
                else
                {
                    Buildings.Add(building);
                }
                
                if (building.Backpack != null)
                {
                    building.Backpack.BackpackChanged += BuildingBackpackChanged;
                    building.OnDestroyed += BuildingDestroyed;

                    RecalculateTotalResources();
                }

                if(!building.IsBuilt)
                {
                    var buildingCraftingComponents = CraftingRecipesConfig
                        .CraftingRecipesDictionary[building.Building.BuildingCraftId].Components;

                    BuildingTownOrder.CreateOrder(building, buildingCraftingComponents, this);
                }

                this.NotifyUIChanged();
            }
        }

        public void RemoveBuilding(BuildingItem building)
        {
            if (TownHall == building)
            {
                TownHall = null;
                if (building.Backpack != null)
                {
                    building.Backpack.BackpackChanged -= BuildingBackpackChanged;
                    building.OnDestroyed -= BuildingDestroyed;

                    RecalculateTotalResources();
                }

                this.NotifyUIChanged();
            }
            else if (Buildings.Contains(building))
            {
                Buildings.Remove(building);
                BuildingTownOrder.RemoveOrdersForBuilding(building);

                if (building.Backpack != null)
                {
                    building.Backpack.BackpackChanged -= BuildingBackpackChanged;
                    building.OnDestroyed -= BuildingDestroyed;

                    RecalculateTotalResources();
                }

                this.NotifyUIChanged();
            }
        }
        
        public Dictionary<CraftingRecipe, bool> GetItemCraftsByBuilding(BuildingItem building)
        {
            var crafts = new Dictionary<CraftingRecipe, bool>();

            if (BelongsToTown(building))
            {
                building.Crafts.ForEach(recipe =>
                {
                    if (recipe.ResultType != typeof(Building))
                    {
                        var canCraft = ItemCraftingManager.CanCraft(recipe, TotalBackpack);
                        crafts[recipe] = canCraft;
                    }
                });
            }

            return crafts;
        }

        public Dictionary<CraftingRecipe, bool> GetAllItemCrafts()
        {
            var crafts = new Dictionary<CraftingRecipe, bool>();

            foreach (var building in Buildings)
            {
                if (BelongsToTown(building))
                {
                    building.Crafts.ForEach(recipe =>
                    {
                        if (recipe.ResultType != typeof(Building))
                        {
                            var canCraft = ItemCraftingManager.CanCraft(recipe, TotalBackpack);
                            crafts[recipe] = canCraft;
                        }
                    });
                }
            }
            
            return crafts;
        }

        public Dictionary<CraftingRecipe, bool> GetBuildingCrafts()
        {
            //ToDo: Logic for possible crafts
            PossibleCrafts = CraftingRecipesConfig.CraftingRecipes;

            var crafts = new Dictionary<CraftingRecipe, bool>();

            PossibleCrafts.ForEach(recipe =>
            {
                if (recipe.ResultType == typeof(Building))
                {
                    var canCraft = ItemCraftingManager.CanCraft(recipe, TotalBackpack);
                    crafts[recipe] = canCraft;
                }
            });

            return crafts;
        }

        public Dictionary<CraftingRecipe, bool> GetAllPossibleCrafts()
        {
            //ToDo: Logic for possible crafts
            PossibleCrafts = CraftingRecipesConfig.CraftingRecipes;

            var crafts = new Dictionary<CraftingRecipe, bool>();

            PossibleCrafts.ForEach(recipe =>
            {
                var canCraft = ItemCraftingManager.CanCraft(recipe, TotalBackpack);
                crafts[recipe] = canCraft;
            });

            return crafts;
        }

        public void AddUnit(UnitItem unit)
        {
            if (!Units.Contains(unit))
            {
                Units.Add(unit);

                unit.OnDied += UnitDied;

                this.NotifyUIChanged();
            }
        }

        public void RemoveUnit(UnitItem unit)
        {
            if (Units.Contains(unit))
            {
                Units.Remove(unit);

                unit.OnDied -= UnitDied;

                this.NotifyUIChanged();
            }
        }

        public bool BelongsToTown(UnitItem unit)
        {
            return Units.Contains(unit);
        }

        public bool BelongsToTown(BuildingItem building)
        {
            return Buildings.Contains(building);
        }

        public int BuildingCount()
        {
            return Buildings.Count;
        }

        public int UnitCount()
        {
            return Units.Count;
        }

        private int TotalBackpackCapacity()
        {
            var totalCapacity = 0;
            foreach (var building in Buildings)
            {
                if (building.Backpack != null && building.IsBuilt)
                {
                    totalCapacity += building.Backpack.MaxCapacity;
                }
            }

            if (TownHall != null && TownHall.Backpack != null)
            {
                totalCapacity += TownHall.Backpack.MaxCapacity;
            }

            return totalCapacity;
        }

        public void RecalculateTotalResources()
        {
            var result = new Backpack(TotalBackpackCapacity());
            foreach (var building in Buildings)
            {
                if (building.IsBuilt)
                {
                    building.Backpack?.GetAllItems().ForEach(item =>
                    {
                        result.AddItem(item.Item, item.Quantity);
                    });
                }
            }

            if (TownHall != null && TownHall.Backpack != null)
            {
                TownHall.Backpack.GetAllItems().ForEach(item =>
                {
                    result.AddItem(item.Item, item.Quantity);
                });
            }

            TotalBackpack = result;
        }

        private void BuildingBackpackChanged(object sender, Backpack.BackpackChangedEventArgs e)
        {
            RecalculateTotalResources();
            RecalculateAllCraftingSystems();
            BuildingTownOrder.RecalculateAllOrders(this);

            this.NotifyUIChanged();
        }

        private void RecalculateAllCraftingSystems()
        {
            foreach (var building in Buildings)
            {
                if (building.Crafts.Count > 0 && building.BuildingCraftingSystem != null && !building.BuildingCraftingSystem.IsAllDelivered)
                {
                    building.BuildingCraftingSystem.RecalculateAssignUnits();
                }
            }
        }

        private void UnitDied(object sender, UnitDiedEventArgs e)
        {
            DiedUnits++;
            RemoveUnit(e.Unit);

            this.NotifyUIChanged();
        }

        private void BuildingDestroyed(object sender, BuildingItem.BuildingDestroyedEventArgs e)
        {
            RecalculateTotalResources();
            BuildingTownOrder.RecalculateAllOrders(this);
            RemoveBuilding(e.BuildingItem);

            this.NotifyUIChanged();
        }
    }
}
