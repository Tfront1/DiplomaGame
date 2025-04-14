using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Items.Crafts;
using UnityEngine;

public static partial class ConfigLoader
{
    public static void LoadCraftingRecipesConfig()
    {
        var json = File.ReadAllText(ConfigPaths.CraftingRecipesConfigPath);
        var craftingRecipesDto = JsonUtility.FromJson<CraftingRecipesDto>(json);

        if (craftingRecipesDto == null)
        {
            Debug.Log("Error crafting recipes config");
            return;
        }

        var nonPositiveIds = craftingRecipesDto.Recipes
            .Where(x => x.Id <= 0)
            .Select(x => x.Id)
            .ToList();

        if (nonPositiveIds.Any())
        {
            throw new Exception($"Crafting recipes Id must be positive. Found non-positive IDs: {string.Join(", ", nonPositiveIds)}");
        }

        var hasDuplicates = craftingRecipesDto.Recipes
            .GroupBy(x => x.Id)
            .Any(group => group.Count() > 1);

        var repeatedIds = craftingRecipesDto.Recipes
            .GroupBy(x => x.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (hasDuplicates)
        {
            throw new Exception($"Crafting recipes Id repeats: {repeatedIds}");
        }

        var craftingRecipesResourcesErrors = ValidateCraftingRecipesResources(craftingRecipesDto);

        if (craftingRecipesResourcesErrors != null)
        {
            throw new Exception(string.Join("\n", craftingRecipesResourcesErrors));
        }

        var craftingRecipesResultsErrors = ValidateCraftingRecipesResult(craftingRecipesDto);

        if (craftingRecipesResultsErrors != null)
        {
            throw new Exception(string.Join("\n", craftingRecipesResultsErrors));
        }

        var craftingWhereToCraftErrors = ValidateWhereToCraft(craftingRecipesDto);

        if (craftingWhereToCraftErrors != null)
        {
            throw new Exception(string.Join("\n", craftingWhereToCraftErrors));
        }

        craftingRecipesDto.Recipes.ForEach(x =>
        {
            var craftRecipe = new CraftingRecipe(
                x.Id,
                x.Name,
                x.Components.Select(component =>
                        new CraftingComponent(ResourcesConfig.ResourceElements
                            .Find(res => res.Id == component.ResourceId), component.Quantity))
                    .ToList(),
                x.CraftingTime,
                x.ResultType,
                x.ResultId,
                x.WhereToCraftId,
                x.MaxUnitToCraftCount);

            CraftingRecipesConfig.CraftingRecipes.Add(craftRecipe);

            if (craftRecipe.WhereToCraftId != 0)
            {
                var buildings = BuildingsConfig.Buildings.FindAll(b => b.Id == craftRecipe.WhereToCraftId);
                foreach (var building in buildings)
                {
                    building.CraftsIds.Add(craftRecipe.Id);
                }
            }

            if (craftRecipe.ResultType == typeof(Building))
            {
                var buildings = BuildingsConfig.Buildings.FindAll(b => b.Id == craftRecipe.ResultId);
                foreach (var building in buildings)
                {
                    building.BuildingCraftId = craftRecipe.Id;
                }
            }
        });

        CraftingRecipesConfig.CraftingRecipesDictionary = CraftingRecipesConfig.CraftingRecipes
            .ToDictionary(recipe => recipe.Id);

        Debug.Log("Crafting recipes config loaded");
    }

    private static List<string> ValidateCraftingRecipesResources(CraftingRecipesDto craftingRecipes)
    {
        var errors = new List<string>();

        var missingResources = craftingRecipes.Recipes
            .SelectMany(x => x.Components)
            .Where(component => ResourcesConfig.ResourceElements.All(resource => resource.Id != component.ResourceId))
            .Select(component => component.ResourceId)
            .Distinct()
            .ToList();

        if (missingResources.Any())
        {
            errors.Add($"Resources in craft: {string.Join(", ", missingResources)} not exists in ResourcesConfig");
        }

        return errors.Any() ? errors : null;
    }

    private static List<string> ValidateCraftingRecipesResult(CraftingRecipesDto craftingRecipes)
    {
        var errors = new List<string>();

        var missingResults = craftingRecipes.Recipes
            .Where(recipe => !IsResultExists(recipe.ResultId, recipe.ResultType))
            .Select(recipe => $"{recipe.ResultType}:{recipe.ResultId}")
            .Distinct()
            .ToList();

        if (missingResults.Any())
        {
            errors.Add($"Results in craft: {string.Join(", ", missingResults)} not exists in All Items");
        }

        return errors.Any() ? errors : null;
    }

    private static List<string> ValidateWhereToCraft(CraftingRecipesDto craftingRecipes)
    {
        var errors = new List<string>();

        var missingBuildings = craftingRecipes.Recipes
            .Where(recipe => recipe.WhereToCraftId != 0 && BuildingsConfig.Buildings.All(building => building.Id == recipe.WhereToCraftId && !building.HasCrafts))
            .Select(recipe => recipe.WhereToCraftId.ToString())
            .Distinct()
            .ToList();

        if (missingBuildings.Any())
        {
            errors.Add($"Buildings in craft: {string.Join(", ", missingBuildings)} not exists in BuildingsConfig");
        }

        return errors.Any() ? errors : null;
    }

    private static bool IsResultExists(int resultId, string resultType)
    {
        return resultType switch
        {
            "Weapon" => WeaponConfig.WeaponElements.Any(w => w.Id == resultId),
            "Armor" => ArmorConfig.ArmorElements.Any(a => a.Id == resultId),
            "Ammo" => AmmunitionConfig.AmmunitionElements.Any(a => a.Id == resultId),
            "Resource" => ResourcesConfig.ResourceElements.Any(r => r.Id == resultId),
            "Building" => BuildingsConfig.Buildings.Any(b => b.Id == resultId),
            _ => false
        };
    }
}
