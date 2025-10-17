# Item Drop Rate System

This system provides configurable drop rates for inventory items through XML configuration and a comprehensive drop management API.

## Files Created

1. **`Content/data/item_drop_rates.xml`** - XML configuration file defining drop rates
2. **`Systems/Inventory/ItemDropManager.cs`** - Core drop rate management class  
3. **`Examples/ItemDropExample.cs`** - Usage examples and demonstrations

## XML Configuration Format

The drop rates are configured in `Content/data/item_drop_rates.xml`:

```xml
<Item type="STICK">
    <DropRate>25.0</DropRate>          <!-- Percentage chance (0.0-100.0) -->
    <MinQuantity>1</MinQuantity>        <!-- Minimum items dropped -->
    <MaxQuantity>3</MaxQuantity>        <!-- Maximum items dropped -->
    <Description>...</Description>       <!-- Optional description -->
</Item>
```

## Current Drop Rates Configuration

- **STICK**: 25% chance, 1-3 quantity (Common)
- **FIBER**: 20% chance, 1-2 quantity (Fairly Common)  
- **ROCK**: 15% chance, 1 quantity (Less Common)
- **LOG**: 5% chance, 1 quantity (Rare)

## Usage Examples

### Basic Setup
```csharp
var dropManager = new ItemDropManager();
dropManager.LoadDropRates(Core.Content, "data/item_drop_rates.xml");
```

### Single Item Drop Check
```csharp
if (dropManager.ShouldDrop(InventoryItemType.LOG))
{
    var quantity = dropManager.GetRandomQuantity(InventoryItemType.LOG);
    playerInventory.AddItem(InventoryItemType.LOG, quantity);
}
```

### Multiple Random Drops
```csharp
var drops = dropManager.GenerateMultipleDrops(maxDrops: 3);
foreach (var drop in drops)
{
    playerInventory.AddItem(drop.Key, drop.Value);
}
```

### Single Random Drop from All Items
```csharp
var droppedItem = dropManager.GenerateRandomDrop();
if (droppedItem.HasValue)
{
    var quantity = dropManager.GetRandomQuantity(droppedItem.Value);
    playerInventory.AddItem(droppedItem.Value, quantity);
}
```

## Key Features

- **XML-Configurable**: Easy to modify drop rates without recompiling
- **Flexible API**: Supports single drops, multiple drops, and specific item checks
- **Quantity Ranges**: Each item can drop in configurable quantity ranges
- **Content Pipeline Integration**: XML file is included in the build process
- **Error Handling**: Graceful handling of missing configurations

## Integration Points

This system integrates well with:
- **Player Inventory System**: Direct integration with `PlayerInventory.AddItem()`
- **Game Events**: Tree cutting, rock breaking, enemy defeats, etc.
- **Loot Systems**: Treasure chests, resource nodes, crafting results
- **Procedural Generation**: Random world generation with appropriate item distribution

## Customization

To add new items or modify drop rates:
1. Edit `Content/data/item_drop_rates.xml`
2. Add corresponding entries in `InventoryItemRegistry`
3. The system will automatically pick up the new configuration on next load