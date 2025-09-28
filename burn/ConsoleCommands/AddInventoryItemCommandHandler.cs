using System;
using Peridot;

public class AddInventoryItem : ConsoleCommandHandler
{
    PlayerInventory _playerInventory;
    public AddInventoryItem(PlayerInventory playerInventory)
        : base()
    {
        CommandName = "additem";
        _playerInventory = playerInventory;
    }

    public override void Execute(string[] args)
    {
        if (args.Length < 2)
        {
            Logger.Error("Usage: additem <item_type> <quantity>");
            return;
        }

        if (!Enum.TryParse(args[0], out InventoryItemType itemType))
        {
            Logger.Error($"Invalid item type: {args[0]}");
            return;
        }

        if (!int.TryParse(args[1], out int quantity))
        {
            Logger.Error($"Invalid quantity: {args[1]}");
            return;
        }

        _playerInventory.AddItem(itemType, quantity);
        Logger.Info($"Added {quantity} of {itemType} to inventory.");
    }
}
