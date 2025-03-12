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
        public List<BuildingItem> Buildings { get; set; } = new();
        public List<UnitItem> Units { get; set; } = new();
        public BuildingItem TownHall { get; set; }
        public Backpack TotalBackpack { get; set; }
        //ToDo: Logic for possible crafts
        public List<CraftingRecipe> PossibleCrafts { get; set; }
        public int DiedUnits { get; set; } = 0;

        public TownItem(string name, Guid id)
        {
            Name = name;
            Id = id;
            TownRegistry.AddTown(this);
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
                Buildings.Add(building);
                building.HomeTown = this;

                if (building.Backpack != null)
                {
                    building.Backpack.BackpackChanged += Building_BackpackChanged;

                    RecalculateTotalResources();
                }
            }
        }

        public void RemoveBuilding(BuildingItem building)
        {
            if (Buildings.Contains(building))
            {
                Buildings.Remove(building);

                if (building.Backpack != null)
                {
                    building.Backpack.BackpackChanged -= Building_BackpackChanged;

                    RecalculateTotalResources();
                }
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
            PossibleCrafts = CraftingRecipeConfig.CraftingRecipes;

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
            PossibleCrafts = CraftingRecipeConfig.CraftingRecipes;

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

                unit.OnDied += Unit_Died;
            }
        }

        public void RemoveUnit(UnitItem unit)
        {
            if (Units.Contains(unit))
            {
                Units.Remove(unit);

                unit.OnDied -= Unit_Died;
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
                if (building.Backpack != null)
                {
                    totalCapacity += building.Backpack.MaxCapacity;
                }
            }

            return totalCapacity;
        }

        public void RecalculateTotalResources()
        {
            var result = new Backpack(TotalBackpackCapacity());
            foreach (var building in Buildings)
            {
                building.Backpack?.GetAllItems().ForEach(item =>
                {
                    result.AddItem(item.Item, item.Quantity);
                });
            }

            TotalBackpack = result;
        }

        private void Building_BackpackChanged(object sender, Backpack.BackpackChangedEventArgs e)
        {
            RecalculateTotalResources();
        }

        private void Unit_Died(object sender, UnitDiedEventArgs e)
        {
            var deadUnit = e.Unit;
            DiedUnits++;
            RemoveUnit(deadUnit);
        }
    }
}
