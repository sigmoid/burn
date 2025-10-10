using System;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;
using Peridot.UI;
using Peridot.UI.Builder;

public class InventoryUI
{
    private PlayerInventory _playerInventory;

    private Canvas _canvas;

    private GridLayoutGroup _itemsGridLayout;

    public InventoryUI(PlayerInventory playerInventory)
    {
        _playerInventory = playerInventory;
        _playerInventory.OnInventoryChanged += OnInventoryUpdated;
        CreateUI();
    }

    private void CreateUI()
    {
        var centerScreen = new Vector2(Core.GraphicsDevice.Viewport.Width / 2, Core.GraphicsDevice.Viewport.Height / 2);
        var size = new Vector2(800, 800);
        var topLeft = new Vector2(centerScreen.X - size.X / 2, centerScreen.Y - size.Y / 2);

        var markup = $"""
        <canvas name="MainCanvas" bounds="{topLeft.X},{topLeft.Y},{size.X},{size.Y}" backgroundColor="#333333">
            <div bounds="{topLeft.X},{topLeft.Y},{size.X},{size.Y}">
                <label name="HeaderLabel" bounds="0,0,800,40" text="Inventory" backgroundColor="#555555" textColor="#FFFFFF"/>
                <scrollarea name="ScrollArea" bounds="0,50,800,750" alwaysShowVertical="true">
                    <div name="ItemGrid" bounds="10,10,780,740" direction="grid" columns="5" rows="5" spacing="10">
                    </div>
                </scrollarea>
            </div>
        </canvas>
        """;

        var builder = new UIBuilder(Core.DefaultFont);
        _canvas = (Canvas)builder.BuildFromMarkup(markup);
        _canvas.SetVisibility(false); // Set initial visibility in code instead
        _itemsGridLayout = _canvas.FindChildByName("ItemGrid") as GridLayoutGroup;

        Core.UISystem.AddElement(_canvas);
    }

    public void OnInventoryUpdated()
    {
        _itemsGridLayout.ClearChildren();

        var inventory = _playerInventory.GetAllSlots();
        foreach (var slot in inventory)
        {
            var canvas = new Canvas(new Rectangle(0, 0, 128, 65), Color.DarkGray);
            var inventoryItem = InventoryItemRegistry.Items.Where(x => x.Key == slot.ItemType).FirstOrDefault().Value;
            if (inventoryItem == null) continue;

            var texture = Core.Content.Load<Texture2D>(inventoryItem.IconPath);

            var itemButton = new ImageButton(new Rectangle(0, 0, 128, 128), texture, () => { HandleItemClicked(inventoryItem.ItemType); }, null);

            canvas.AddChild(itemButton);
            _itemsGridLayout.AddChild(canvas);
        }
    }

    public void HandleItemClicked(InventoryItemType itemType)
    {
        Logger.Debug($"Clicked on item: {itemType}");
    }

    public void Toggle()
    {
        Console.WriteLine("Toggling Inventory UI");
        _canvas.SetVisibility(!_canvas.IsVisible());
    }

}
