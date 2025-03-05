using System.Collections.Generic;
using System.Linq;
using Items.Resource.BackPack;

namespace Assets.Items.Crafts
{
    public static class CraftingManager
    {
        public static bool CanCraft(CraftingRecipe recipe, Backpack inBackpack)
        {
            if (recipe == null || inBackpack == null)
                return false;

            foreach (var component in recipe.Components)
            {
                if (!inBackpack.HasResource(component.BackpackItem, component.Quantity))
                    return false;
            }

            return true;
        }

        public static bool CanCraft(int recipeId, Backpack inBackpack)
        {
            var recipe = CraftingRecipeConfig.CraftingRecipes.Find(x => x.Id == recipeId);
            return CanCraft(recipe, inBackpack);
        }

        private static bool ExecuteCraft(CraftingRecipe recipe, Backpack inBackpack, Backpack outBackpack)
        {
            if (recipe == null || inBackpack == null)
                return false;

            foreach (var component in recipe.Components)
            {
                inBackpack.RemoveResource(component.BackpackItem, component.Quantity);
            }

            var craftedItem = ItemFactory.CreateItem(recipe.ResultId, recipe.ResultType);
            
            return outBackpack.AddItem(craftedItem);
        }

        // Крафтинг з перевіркою наявності ресурсів
        public static bool Craft(CraftingRecipe recipe, Backpack inBackpack, Backpack outBackpack)
        {
            if (!CanCraft(recipe, inBackpack))
                return false;
            
            return ExecuteCraft(recipe, inBackpack, outBackpack);
        }

        public static bool Craft(int recipeId, Backpack inBackpack, Backpack outBackpack)
        {
            var recipe = CraftingRecipeConfig.CraftingRecipes.Find(x => x.Id == recipeId);
            return Craft(recipe, inBackpack, outBackpack);
        }
        
        public static List<CraftingRecipe> GetCraftableRecipes(Backpack backpack)
        {
            if (backpack == null)
                return new List<CraftingRecipe>();

            return CraftingRecipeConfig.CraftingRecipes
                .Where(recipe => CanCraft(recipe, backpack))
                .ToList();
        }

        public static int CraftMultiple(CraftingRecipe recipe, Backpack inBackpack, Backpack outBackpack, int count)
        {
            if (recipe == null || inBackpack == null || count <= 0)
                return 0;

            var maxPossible = CalculateMaxPossibleCrafts(recipe, inBackpack);

            var craftCount = System.Math.Min(count, maxPossible);

            if (craftCount <= 0)
                return 0;

            for (var i = 0; i < craftCount; i++)
            {
                Craft(recipe, inBackpack, outBackpack);
            }

            return craftCount;
        }

        public static int CalculateMaxPossibleCrafts(CraftingRecipe recipe, Backpack backpack)
        {
            if (recipe == null || backpack == null)
                return 0;

            var maxPossible = int.MaxValue;

            foreach (var component in recipe.Components)
            {
                var available = backpack.GetResourceQuantity(component.BackpackItem);
                var possibleFromThisComponent = available / component.Quantity;

                maxPossible = System.Math.Min(maxPossible, possibleFromThisComponent);
            }

            var remainingSpace = backpack.MaxCapacity - backpack.CurrentCapacity;

            return System.Math.Min(maxPossible, remainingSpace);
        }
    }
}