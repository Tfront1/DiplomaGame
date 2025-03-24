using Items.Resource.BackPack;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Items.Crafts
{
    public class BuildingCraftingManager
    {
        public static bool CanCraft(CraftingRecipe recipe, Backpack inBackpack)
        {
            if (recipe == null || inBackpack == null)
                return false;

            foreach (var component in recipe.Components)
            {
                if (!inBackpack.HasResourceCount(component.BackpackItem, component.Quantity))
                    return false;
            }

            return true;
        }

        public static bool CanCraft(int recipeId, Backpack inBackpack)
        {
            var recipe = CraftingRecipesConfig.CraftingRecipes.Find(x => x.Id == recipeId);
            return CanCraft(recipe, inBackpack);
        }

        private static Building ExecuteCraft(CraftingRecipe recipe, Backpack inBackpack)
        {
            if (recipe == null || inBackpack == null)
                return null;

            foreach (var component in recipe.Components)
            {
                inBackpack.RemoveItem(component.BackpackItem, component.Quantity);
            }

            var craftedBuilding = BuildingsConfig.Buildings.Find(x => x.Id == recipe.ResultId);

            return craftedBuilding;
        }

        public static Building Craft(CraftingRecipe recipe, Backpack inBackpack)
        {
            if (!CanCraft(recipe, inBackpack))
                return null;

            return ExecuteCraft(recipe, inBackpack);
        }

        public static Building Craft(int recipeId, Backpack inBackpack)
        {
            var recipe = CraftingRecipesConfig.CraftingRecipes.Find(x => x.Id == recipeId);
            return Craft(recipe, inBackpack);
        }
        
        public static List<CraftingRecipe> GetCraftableRecipes(Backpack backpack)
        {
            if (backpack == null)
                return new List<CraftingRecipe>();

            return CraftingRecipesConfig.CraftingRecipes
                .Where(recipe => CanCraft(recipe, backpack))
                .ToList();
        }
    }
}