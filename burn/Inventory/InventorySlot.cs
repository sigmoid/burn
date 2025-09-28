using System;

namespace burn.Inventory
{
    /// <summary>
    /// Represents a slot in an inventory containing an item type and its quantity
    /// </summary>
    public struct InventorySlot
    {
        public InventoryItemType ItemType { get; }
        public int Quantity { get; }

        public InventorySlot(InventoryItemType itemType, int quantity)
        {
            if (quantity < 0)
                throw new ArgumentException("Quantity cannot be negative", nameof(quantity));
            
            ItemType = itemType;
            Quantity = quantity;
        }

        /// <summary>
        /// Returns true if this slot is empty (quantity is 0)
        /// </summary>
        public bool IsEmpty => Quantity == 0;

        /// <summary>
        /// Returns true if this slot has items (quantity > 0)
        /// </summary>
        public bool HasItems => Quantity > 0;

        /// <summary>
        /// Creates a new InventorySlot with the specified quantity change
        /// </summary>
        public InventorySlot WithQuantity(int newQuantity)
        {
            return new InventorySlot(ItemType, newQuantity);
        }

        /// <summary>
        /// Creates a new InventorySlot with the quantity increased by the specified amount
        /// </summary>
        public InventorySlot Add(int amount)
        {
            return new InventorySlot(ItemType, Quantity + amount);
        }

        /// <summary>
        /// Creates a new InventorySlot with the quantity decreased by the specified amount
        /// </summary>
        public InventorySlot Remove(int amount)
        {
            return new InventorySlot(ItemType, Math.Max(0, Quantity - amount));
        }

        public override string ToString()
        {
            return $"{ItemType}: {Quantity}";
        }

        public override bool Equals(object obj)
        {
            return obj is InventorySlot slot && 
                   ItemType == slot.ItemType && 
                   Quantity == slot.Quantity;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ItemType, Quantity);
        }

        public static bool operator ==(InventorySlot left, InventorySlot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(InventorySlot left, InventorySlot right)
        {
            return !(left == right);
        }
    }
}