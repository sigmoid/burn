using System.Collections.Generic;
using System.Linq;
using burn.Inventory;
using Peridot.UI;

public class PlayerInventory
{
    private Dictionary<InventoryItemType, int> _items;

    public PlayerInventory()
    {
        _items = new Dictionary<InventoryItemType, int>();
        foreach (InventoryItemType itemType in System.Enum.GetValues(typeof(InventoryItemType)))
        {
            _items[itemType] = 0;
        }
    }

    public event System.Action OnInventoryChanged;

    public event System.Action<InventoryItemType> OnItemSelected;

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

        OnInventoryChanged?.Invoke();
    }

    public void AddItemWithNotification(InventoryItemType itemType, int quantity = 1)
    {
        AddItem(itemType, quantity);
        var itemName = InventoryItemRegistry.Items[itemType].Name;
        NotificationManager.ShowNotification($"Acquired {quantity}x {itemName}");
    }

    public bool RemoveItem(InventoryItemType itemType, int quantity = 1)
    {
        if (_items.ContainsKey(itemType) && _items[itemType] >= quantity)
        {
            _items[itemType] -= quantity;
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public int GetQuantity(InventoryItemType itemType)
    {
        return _items.ContainsKey(itemType) ? _items[itemType] : 0;
    }

    public InventorySlot GetSlot(InventoryItemType itemType)
    {
        return new InventorySlot(itemType, GetQuantity(itemType));
    }

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

    public IEnumerable<InventorySlot> GetAllSlotsIncludingEmpty()
    {
        foreach (var kvp in _items)
        {
            yield return new InventorySlot(kvp.Key, kvp.Value);
        }
    }

    public bool HasItem(InventoryItemType itemType, int quantity = 1)
    {
        return GetQuantity(itemType) >= quantity;
    }

    public void SelectItem(InventoryItemType itemType)
    {
        OnItemSelected?.Invoke(itemType);
    }
}