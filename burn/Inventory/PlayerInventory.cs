using System.Collections.Generic;
using System.Linq;
using burn.Inventory;

public class PlayerInventory
{
    private Dictionary<InventoryItemType, int> _items;

    public PlayerInventory()
    {
        _items = new Dictionary<InventoryItemType, int>();
        foreach (InventoryItemType itemType in System.Enum.GetValues(typeof(InventoryItemType)))
        {
            _items[itemType] = 0; // Initialize all item counts to 0
        }
    }

    public void AddItem(InventoryItemType itemType, int quantity = 1)
    {
        if (_items.ContainsKey(itemType))
        {
            _items[itemType] += quantity;
        }
        else
        {
            _items[itemType] = quantity;
        }
    }

    public bool RemoveItem(InventoryItemType itemType, int quantity = 1)
    {
        if (_items.ContainsKey(itemType) && _items[itemType] >= quantity)
        {
            _items[itemType] -= quantity;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the quantity of a specific item type
    /// </summary>
    public int GetQuantity(InventoryItemType itemType)
    {
        return _items.ContainsKey(itemType) ? _items[itemType] : 0;
    }

    /// <summary>
    /// Gets an inventory slot for a specific item type
    /// </summary>
    public InventorySlot GetSlot(InventoryItemType itemType)
    {
        return new InventorySlot(itemType, GetQuantity(itemType));
    }

    /// <summary>
    /// Gets all non-empty inventory slots
    /// </summary>
    public IEnumerable<InventorySlot> GetAllSlots()
    {
        foreach (var kvp in _items)
        {
            if (kvp.Value > 0)
            {
                yield return new InventorySlot(kvp.Key, kvp.Value);
            }
        }
    }

    /// <summary>
    /// Gets all inventory slots including empty ones
    /// </summary>
    public IEnumerable<InventorySlot> GetAllSlotsIncludingEmpty()
    {
        foreach (var kvp in _items)
        {
            yield return new InventorySlot(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Checks if the inventory has at least the specified quantity of an item
    /// </summary>
    public bool HasItem(InventoryItemType itemType, int quantity = 1)
    {
        return GetQuantity(itemType) >= quantity;
    }
}