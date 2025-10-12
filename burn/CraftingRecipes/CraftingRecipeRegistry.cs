using System.Collections.Generic;

public class CraftingRecipeRegistry
{
    public static List<CraftingRecipe> Recipes { get; private set; } = new List<CraftingRecipe>();

    public static void CreateRecipes()
    {
        Recipes.Add(new CraftingRecipe
        {
            Name = "Wooden Plank",
            Ingredients = new List<CraftingIngredient>
            {
                new CraftingIngredient { ItemType = InventoryItemType.LOG, Quantity = 2 }
            }
        });
    }
}