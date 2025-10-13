using System.Collections.Generic;

public class CraftingRecipe
{
    public string Name { get; set; }
    public List<CraftingIngredient> Ingredients { get; set; } = new List<CraftingIngredient>();
}

public class CraftingIngredient
{
    public InventoryItemType ItemType { get; set; }
    public int Quantity { get; set; }
}