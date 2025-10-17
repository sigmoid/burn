using burn.Systems.Inventory;
using Peridot;

namespace burn.Examples
{
    /// <summary>
    /// Example usage of the ItemDropManager system
    /// </summary>
    public class ItemDropExample
    {
        private ItemDropManager _dropManager;

        public void Initialize()
        {
            // Create and initialize the drop manager
            _dropManager = new ItemDropManager();
            
            // Load drop rates from the XML file
            _dropManager.LoadDropRates(Core.Content, "data/item_drop_rates.xml");
        }

        /// <summary>
        /// Example: Generate random drops when a tree is cut down
        /// </summary>
        public void OnTreeCutDown(PlayerInventory playerInventory)
        {
            // Generate multiple drops with a maximum of 2 items
            var drops = _dropManager.GenerateMultipleDrops(maxDrops: 2);

            foreach (var drop in drops)
            {
                var itemType = drop.Key;
                var quantity = drop.Value;
                
                // Add the dropped items to player inventory
                playerInventory.AddItem(itemType, quantity);
                
                // Log what was dropped
                var itemName = InventoryItemRegistry.Items[itemType].Name;
                Logger.Info($"Dropped {quantity}x {itemName}");
            }
        }

        /// <summary>
        /// Example: Generate a single random drop when breaking a rock
        /// </summary>
        public void OnRockBroken(PlayerInventory playerInventory)
        {
            // Generate a single random drop
            var droppedItem = _dropManager.GenerateRandomDrop();
            
            if (droppedItem.HasValue)
            {
                var quantity = _dropManager.GetRandomQuantity(droppedItem.Value);
                playerInventory.AddItem(droppedItem.Value, quantity);
                
                var itemName = InventoryItemRegistry.Items[droppedItem.Value].Name;
                Logger.Info($"Rock broke and dropped {quantity}x {itemName}");
            }
            else
            {
                Logger.Info("Rock broke but nothing was dropped");
            }
        }

        /// <summary>
        /// Example: Check specific item drop rates
        /// </summary>
        public void TestSpecificDrops()
        {
            // Test if a LOG should drop (5% chance according to XML)
            if (_dropManager.ShouldDrop(InventoryItemType.LOG))
            {
                var quantity = _dropManager.GetRandomQuantity(InventoryItemType.LOG);
                Logger.Info($"Lucky! LOG dropped with quantity: {quantity}");
            }

            // Test if a STICK should drop (25% chance according to XML)
            if (_dropManager.ShouldDrop(InventoryItemType.STICK))
            {
                var quantity = _dropManager.GetRandomQuantity(InventoryItemType.STICK);
                Logger.Info($"STICK dropped with quantity: {quantity}");
            }
        }

        /// <summary>
        /// Example: Display all configured drop rates
        /// </summary>
        public void DisplayDropRates()
        {
            var allConfigs = _dropManager.GetAllDropConfigs();

            Logger.Info("=== Item Drop Rates ===");
            foreach (var config in allConfigs.Values)
            {
                var itemName = InventoryItemRegistry.Items[config.ItemType].Name;
                Logger.Info($"{itemName}: {config.DropRate}% chance, {config.MinQuantity}-{config.MaxQuantity} quantity");
            }
        }
    }
}