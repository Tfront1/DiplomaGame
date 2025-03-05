using System;
using System.Collections.Generic;
using Assets.Items.Ammunition;
using Assets.Items.Armor;
using Assets.Items.Weapon;
using Items.Resource;

namespace Assets.Items.Crafts
{
    public class CraftingRecipe
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<CraftingComponent> Components { get; set; }
        public float CraftingTime { get; set; }
        public Type ResultType { get; set; }
        public int ResultId { get; set; }

        public CraftingRecipe(int recipeId, string name, List<CraftingComponent> components, float craftingTime = 0, string resultType = null, int resultId = 0)
        {
            Id = recipeId;
            Name = name;
            Components = components;
            CraftingTime = craftingTime;

            ResultType = resultType switch
            {
                "Weapon" => typeof(WeaponElement),
                "Armor" => typeof(ArmorElement),
                "Ammo" => typeof(AmmunitionElement),
                "Resource" => typeof(ResourceElement),
                "Building" => typeof(Building),
                _ => throw new ArgumentException($"Unknown result type: {resultType}")
            };

            ResultId = resultId;
        }
    }
}