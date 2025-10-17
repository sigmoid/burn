using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Microsoft.Xna.Framework.Content;
using Peridot;

namespace burn.Systems.Inventory
{
    /// <summary>
    /// Represents drop rate configuration for an inventory item
    /// </summary>
    public class ItemDropConfig
    {
        public InventoryItemType ItemType { get; set; }
        public float DropRate { get; set; } // Percentage (0.0 to 100.0)
        public int MinQuantity { get; set; }
        public int MaxQuantity { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Manages item drop rates and handles random item generation
    /// </summary>
    public class ItemDropManager
    {
        private readonly Dictionary<InventoryItemType, ItemDropConfig> _dropConfigs;
        private readonly System.Random _random;

        public ItemDropManager()
        {
            _dropConfigs = new Dictionary<InventoryItemType, ItemDropConfig>();
            _random = new System.Random();
            LoadDropRates(Core.Content);
        }

        /// <summary>
        /// Load drop rate configuration from XML file through content manager
        /// </summary>
        /// <param name="content">Content manager to use for loading</param>
        /// <param name="assetPath">Path to the XML file relative to content root</param>
        public void LoadDropRates(ContentManager content, string assetPath = "data/item_drop_rates.xml")
        {
            var fullPath = Path.Combine(content.RootDirectory, assetPath);
            LoadDropRatesFromFile(fullPath);
        }

        /// <summary>
        /// Load drop rate configuration from XML file
        /// </summary>
        /// <param name="filePath">Full path to the XML file</param>
        public void LoadDropRatesFromFile(string filePath)
        {
            try
            {
                var doc = XDocument.Load(filePath);
                var root = doc.Root;

                foreach (var itemElement in root.Elements("Item"))
                {
                    var typeAttr = itemElement.Attribute("type")?.Value;
                    if (Enum.TryParse<InventoryItemType>(typeAttr, out var itemType))
                    {
                        var config = new ItemDropConfig
                        {
                            ItemType = itemType,
                            DropRate = float.Parse(itemElement.Element("DropRate")?.Value ?? "0"),
                            MinQuantity = int.Parse(itemElement.Element("MinQuantity")?.Value ?? "1"),
                            MaxQuantity = int.Parse(itemElement.Element("MaxQuantity")?.Value ?? "1"),
                            Description = itemElement.Element("Description")?.Value ?? ""
                        };

                        _dropConfigs[itemType] = config;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load item drop rates from {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get drop configuration for a specific item type
        /// </summary>
        /// <param name="itemType">The item type to get configuration for</param>
        /// <returns>Drop configuration, or null if not found</returns>
        public ItemDropConfig GetDropConfig(InventoryItemType itemType)
        {
            return _dropConfigs.TryGetValue(itemType, out var config) ? config : null;
        }

        /// <summary>
        /// Check if an item should drop based on its drop rate
        /// </summary>
        /// <param name="itemType">The item type to check</param>
        /// <returns>True if the item should drop</returns>
        public bool ShouldDrop(InventoryItemType itemType)
        {
            var config = GetDropConfig(itemType);
            if (config == null) return false;

            var roll = _random.NextDouble() * 100.0;
            return roll <= config.DropRate;
        }

        /// <summary>
        /// Get a random quantity for an item based on its configuration
        /// </summary>
        /// <param name="itemType">The item type to get quantity for</param>
        /// <returns>Random quantity within the configured range</returns>
        public int GetRandomQuantity(InventoryItemType itemType)
        {
            var config = GetDropConfig(itemType);
            if (config == null) return 1;

            return _random.Next(config.MinQuantity, config.MaxQuantity + 1);
        }

        /// <summary>
        /// Generate a random item drop from all configured items
        /// </summary>
        /// <returns>A random item type that should drop, or null if no items should drop</returns>
        public InventoryItemType? GenerateRandomDrop()
        {
            var eligibleItems = new List<InventoryItemType>();

            foreach (var kvp in _dropConfigs)
            {
                if (ShouldDrop(kvp.Key))
                {
                    eligibleItems.Add(kvp.Key);
                }
            }

            if (eligibleItems.Count == 0)
                return null;

            return eligibleItems[_random.Next(eligibleItems.Count)];
        }

        /// <summary>
        /// Generate multiple random drops
        /// </summary>
        /// <param name="maxDrops">Maximum number of items that can drop</param>
        /// <returns>Dictionary of item types and their quantities that should drop</returns>
        public Dictionary<InventoryItemType, int> GenerateMultipleDrops(int maxDrops = 3)
        {
            var drops = new Dictionary<InventoryItemType, int>();
            var dropCount = 0;

            foreach (var kvp in _dropConfigs)
            {
                if (dropCount >= maxDrops) break;

                if (ShouldDrop(kvp.Key))
                {
                    var quantity = GetRandomQuantity(kvp.Key);
                    drops[kvp.Key] = quantity;
                    dropCount++;
                }
            }

            return drops;
        }

        /// <summary>
        /// Get all configured drop rates
        /// </summary>
        /// <returns>Dictionary of all drop configurations</returns>
        public Dictionary<InventoryItemType, ItemDropConfig> GetAllDropConfigs()
        {
            return new Dictionary<InventoryItemType, ItemDropConfig>(_dropConfigs);
        }
    }
}