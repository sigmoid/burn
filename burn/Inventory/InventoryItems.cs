using System.Collections.Generic;

public enum InventoryItemType
{
    STICK,
    ROCK,
    FIBER,
    LOG
}

public class InventoryItem
{
    public InventoryItemType ItemType { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string IconPath { get; set; }
    public string BurnablePath { get; set; }

    public InventoryItem(InventoryItemType itemType, string name, string description, string iconPath, string burnablePath = null)
    {
        ItemType = itemType;
        Name = name;
        Description = description;
        IconPath = iconPath;
        BurnablePath = burnablePath;
    }
}

public static class InventoryItemRegistry
{
    public static Dictionary<InventoryItemType, InventoryItem> Items { get; private set; } = new Dictionary<InventoryItemType, InventoryItem>()
    {
        { InventoryItemType.STICK, new InventoryItem(InventoryItemType.STICK, "Stick", "A small stick, useful for starting fires.", "icons/stick_icon", "burnables/stick_burnable") },
        { InventoryItemType.ROCK, new InventoryItem(InventoryItemType.ROCK, "Rock", "A small rock, useful for crafting and building.", "icons/rock_icon") },
        { InventoryItemType.FIBER, new InventoryItem(InventoryItemType.FIBER, "Fiber", "Natural fiber, useful for crafting and building.", "icons/fiber_icon", "burnables/fiber_burnable") },
        { InventoryItemType.LOG, new InventoryItem(InventoryItemType.LOG, "Log", "A large log, great for building and fuel.", "icons/log_icon", "burnables/log_burnable") }
    };
}