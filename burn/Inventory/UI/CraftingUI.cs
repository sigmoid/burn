using System.Runtime.CompilerServices;
using Peridot;
using Peridot.UI;
using Peridot.UI.Builder;

public class CraftingUI
{
    private Canvas _canvas;
    private VerticalLayoutGroup _itemsLayout;

    public CraftingUI()
    {
        CreateUI();
    }

    private void CreateUI()
    {
        var topLeft = TabUIGlobals.WindowPosition;
        var size = TabUIGlobals.WindowSize;

        var markup = $"""
        <canvas name="CraftingCanvas" bounds="{topLeft.X},{topLeft.Y},{size.X},{size.Y}" backgroundColor="#222222">
            <div bounds="{topLeft.X},{topLeft.Y},{size.X},{size.Y}">
                <label name="CraftingHeaderLabel" bounds="{topLeft.X},{topLeft.Y},800,40" text="Crafting" backgroundColor="#555555" textColor="#FFFFFF"/>
                <scrollarea name="CraftingScrollArea" bounds="{topLeft.X},{topLeft.Y + 40},800,750" alwaysShowVertical="true">
                    <div name="CraftingItemLayoutGroup" bounds="{topLeft.X + 10},{topLeft.Y + 50},780,740" direction="vertical" columns="5" rows="5" spacing="10">
                    </div>
                </scrollarea>
            </div>
        </canvas>
        """;

        var builder = new UIBuilder(Core.DefaultFont);
        _canvas = (Canvas)builder.BuildFromMarkup(markup);

        _itemsLayout = _canvas.FindChildByName("CraftingItemLayoutGroup") as VerticalLayoutGroup;
        _canvas.SetVisibility(false);

        Core.UISystem.AddElement(_canvas);

        CreateCraftingRecipes();
    }

    private void CreateCraftingRecipes()
    {
        var builder = new UIBuilder(Core.DefaultFont);

        foreach (var recipe in CraftingRecipeRegistry.Recipes)
        {
            var ingredientsList = "";

            foreach (var ingredient in recipe.Ingredients)
            {
                ingredientsList += $"{ingredient.ItemType.ToString()} x" + ingredient.Quantity.ToString() + ", ";
            }

            var markup = $"""
            <canvas name="{recipe.Name}Canvas" bounds="0,0,760,100" backgroundColor="#222222">
                <label name="{recipe.Name}Label" bounds="10,10,300,30" text="{recipe.Name}" backgroundColor="#777777" textColor="#FFFFFF"/>
                <label name="{recipe.Name}IngredientsLabel" bounds="10,50,620,30" text="Ingredients: {ingredientsList.TrimEnd(',', ' ')}" backgroundColor="#777777" textColor="#FFFFFF"/>
                <button name="{recipe.Name}CraftButton" bounds="650,30,100,40" text="Craft" backgroundColor="#333333" textColor="#FFFFFF"/>
            </canvas>
            """;

            var element = builder.BuildFromMarkup(markup);

            _itemsLayout.AddChild(element);
        }
    }

    public void SetVisibility(bool isVisible)
    {
        if (isVisible)
        {
            _canvas.SetVisibility(true);
        }
        else
        {
            _canvas.SetVisibility(false);
        }
    }

}