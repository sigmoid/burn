using System.Linq;
using System.Reflection.Metadata;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;
using Peridot.UI;

public class InventoryUI
{
    private PlayerInventory _playerInventory;

    private GridLayoutGroup _gridLayout;
    private UIImage _backgroundImage;
    private Canvas _canvas;

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

        _canvas = new Canvas(new Rectangle(0, 0, Core.GraphicsDevice.Viewport.Width, Core.GraphicsDevice.Viewport.Height), Color.Transparent);
        _gridLayout = new GridLayoutGroup(new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)size.X, (int)size.Y), 5, 5, 2, 2);
        _backgroundImage = new UIImage(Core.Content.Load<Texture2D>("images/ui/inventory-grid"), new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)size.X, (int)size.Y));
        _backgroundImage.LocalOrderOffset = -0.02f;

        _canvas.AddChild(_backgroundImage);
        _canvas.AddChild(_gridLayout);
        Core.UISystem.AddElement(_canvas);

        _canvas.SetVisibility(false);
    }

    public void OnInventoryUpdated()
    {
        _gridLayout.ClearChildren();

        var inventory = _playerInventory.GetAllSlots();
        foreach (var slot in inventory)
        {
            var inventoryItem = InventoryItemRegistry.Items.Where(x => x.Key == slot.ItemType).FirstOrDefault().Value;
            if (inventoryItem == null) continue;

            var texture = Core.Content.Load<Texture2D>(inventoryItem.IconPath);

            var itemButton = new ImageButton(new Rectangle(0, 0, 128, 128), texture, () => { HandleItemClicked(inventoryItem.ItemType); }, null);

            _gridLayout.AddChild(itemButton);
        }
    }

    public void HandleItemClicked(InventoryItemType itemType)
    {
        Logger.Debug($"Clicked on item: {itemType}");
    }

    public void Toggle()
    {
        _canvas.SetVisibility(!_canvas.IsVisible());
    }

}
