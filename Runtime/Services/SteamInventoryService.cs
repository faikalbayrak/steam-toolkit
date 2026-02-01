using System;
using System.Collections.Generic;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SteamToolkit
{
    /// <summary>
    /// Steam Inventory service.
    /// Handles item management, grants, and consumption.
    /// </summary>
    public class SteamInventoryService : IDisposable
    {
        #region Events

        /// <summary>
        /// Fired when inventory is loaded/updated.
        /// </summary>
        public event Action<List<InventoryItem>> OnInventoryUpdated;

        /// <summary>
        /// Fired when item definitions are loaded.
        /// </summary>
        public event Action OnDefinitionsLoaded;

        /// <summary>
        /// Fired on any error.
        /// </summary>
        public event Action<string> OnError;

        #endregion

        #region Properties

        public bool IsInitialized { get; private set; }
        public bool DefinitionsLoaded { get; private set; }
        public List<InventoryItem> Items { get; private set; } = new List<InventoryItem>();

        #endregion

        #region Private Fields

#if !DISABLESTEAMWORKS
        private Callback<SteamInventoryResultReady_t> _inventoryResultReadyCallback;
        private Callback<SteamInventoryDefinitionUpdate_t> _definitionUpdateCallback;
        private SteamInventoryResult_t _currentResult = SteamInventoryResult_t.Invalid;
#endif

        #endregion

        #region Initialization

        public void Initialize()
        {
            if (IsInitialized) return;

#if !DISABLESTEAMWORKS
            _inventoryResultReadyCallback = Callback<SteamInventoryResultReady_t>.Create(OnInventoryResultReady);
            _definitionUpdateCallback = Callback<SteamInventoryDefinitionUpdate_t>.Create(OnDefinitionUpdate);

            // Load item definitions
            SteamInventory.LoadItemDefinitions();
#endif

            IsInitialized = true;
            Log("Inventory service initialized.");
        }

        public void Dispose()
        {
#if !DISABLESTEAMWORKS
            if (_currentResult != SteamInventoryResult_t.Invalid)
            {
                SteamInventory.DestroyResult(_currentResult);
                _currentResult = SteamInventoryResult_t.Invalid;
            }

            _inventoryResultReadyCallback = null;
            _definitionUpdateCallback = null;
#endif

            Items.Clear();
            IsInitialized = false;
            DefinitionsLoaded = false;
            Log("Inventory service disposed.");
        }

        #endregion

        #region Get Inventory

        /// <summary>
        /// Get all items in user's inventory.
        /// </summary>
        /// <param name="onComplete">Callback with items list</param>
        public void GetAllItems(Action<List<InventoryItem>> onComplete = null)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState())
            {
                onComplete?.Invoke(new List<InventoryItem>());
                return;
            }

            if (SteamInventory.GetAllItems(out _currentResult))
            {
                Log("Requesting all inventory items...");
            }
            else
            {
                LogError("Failed to request inventory items.");
                onComplete?.Invoke(new List<InventoryItem>());
            }
#else
            onComplete?.Invoke(new List<InventoryItem>());
#endif
        }

        /// <summary>
        /// Get items by definition IDs.
        /// </summary>
        /// <param name="itemDefIds">Array of item definition IDs</param>
        /// <param name="onComplete">Callback with items list</param>
        public void GetItemsByID(int[] itemDefIds, Action<List<InventoryItem>> onComplete = null)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState())
            {
                onComplete?.Invoke(new List<InventoryItem>());
                return;
            }

            var steamIds = new SteamItemDef_t[itemDefIds.Length];
            for (int i = 0; i < itemDefIds.Length; i++)
            {
                steamIds[i] = new SteamItemDef_t(itemDefIds[i]);
            }

            if (SteamInventory.GetItemsByID(out _currentResult, null, 0))
            {
                Log($"Requesting {itemDefIds.Length} specific items...");
            }
            else
            {
                LogError("Failed to request specific items.");
                onComplete?.Invoke(new List<InventoryItem>());
            }
#else
            onComplete?.Invoke(new List<InventoryItem>());
#endif
        }

        #endregion

        #region Grant Items (Promo)

        /// <summary>
        /// Grant promotional items to user.
        /// Items must be configured as promo items in Steamworks.
        /// </summary>
        /// <param name="onComplete">Callback with granted items</param>
        public void GrantPromoItems(Action<List<InventoryItem>> onComplete = null)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState())
            {
                onComplete?.Invoke(new List<InventoryItem>());
                return;
            }

            if (SteamInventory.GrantPromoItems(out _currentResult))
            {
                Log("Granting promo items...");
            }
            else
            {
                LogError("Failed to grant promo items.");
                onComplete?.Invoke(new List<InventoryItem>());
            }
#else
            onComplete?.Invoke(new List<InventoryItem>());
#endif
        }

        /// <summary>
        /// Grant a specific promotional item.
        /// </summary>
        /// <param name="itemDefId">Item definition ID</param>
        /// <param name="onComplete">Callback with success status</param>
        public void GrantPromoItem(int itemDefId, Action<bool> onComplete = null)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState())
            {
                onComplete?.Invoke(false);
                return;
            }

            var itemDef = new SteamItemDef_t(itemDefId);
            if (SteamInventory.AddPromoItem(out _currentResult, itemDef))
            {
                Log($"Granting promo item: {itemDefId}");
            }
            else
            {
                LogError($"Failed to grant promo item: {itemDefId}");
                onComplete?.Invoke(false);
            }
#else
            onComplete?.Invoke(false);
#endif
        }

        #endregion

        #region Consume Items

        /// <summary>
        /// Consume (use) an item from inventory.
        /// </summary>
        /// <param name="itemId">Item instance ID</param>
        /// <param name="quantity">Quantity to consume</param>
        /// <param name="onComplete">Callback with success status</param>
        public void ConsumeItem(ulong itemId, uint quantity = 1, Action<bool> onComplete = null)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState())
            {
                onComplete?.Invoke(false);
                return;
            }

            var itemInstance = new SteamItemInstanceID_t(itemId);
            if (SteamInventory.ConsumeItem(out _currentResult, itemInstance, quantity))
            {
                Log($"Consuming item {itemId} x{quantity}");
            }
            else
            {
                LogError($"Failed to consume item: {itemId}");
                onComplete?.Invoke(false);
            }
#else
            onComplete?.Invoke(false);
#endif
        }

        #endregion

        #region Item Definitions

        /// <summary>
        /// Get all item definition IDs.
        /// </summary>
        /// <returns>Array of item definition IDs</returns>
        public int[] GetItemDefinitionIDs()
        {
#if !DISABLESTEAMWORKS
            if (!DefinitionsLoaded)
            {
                LogError("Item definitions not loaded yet.");
                return new int[0];
            }

            if (SteamInventory.GetItemDefinitionIDs(null, out uint count))
            {
                var ids = new SteamItemDef_t[count];
                if (SteamInventory.GetItemDefinitionIDs(ids, out count))
                {
                    var result = new int[count];
                    for (int i = 0; i < count; i++)
                    {
                        result[i] = ids[i].m_SteamItemDef;
                    }
                    return result;
                }
            }
#endif
            return new int[0];
        }

        /// <summary>
        /// Get item definition property.
        /// </summary>
        /// <param name="itemDefId">Item definition ID</param>
        /// <param name="propertyName">Property name (name, description, price, etc.)</param>
        /// <returns>Property value or empty string</returns>
        public string GetItemDefinitionProperty(int itemDefId, string propertyName)
        {
#if !DISABLESTEAMWORKS
            if (!DefinitionsLoaded)
            {
                return "";
            }

            var itemDef = new SteamItemDef_t(itemDefId);
            uint bufferSize = 1024;
            
            if (SteamInventory.GetItemDefinitionProperty(itemDef, propertyName, out string value, ref bufferSize))
            {
                return value;
            }
#endif
            return "";
        }

        /// <summary>
        /// Get all properties for an item definition.
        /// </summary>
        /// <param name="itemDefId">Item definition ID</param>
        /// <returns>Dictionary of property name/value pairs</returns>
        public Dictionary<string, string> GetItemDefinitionProperties(int itemDefId)
        {
            var properties = new Dictionary<string, string>();

#if !DISABLESTEAMWORKS
            if (!DefinitionsLoaded)
            {
                return properties;
            }

            var itemDef = new SteamItemDef_t(itemDefId);
            uint bufferSize = 4096;

            // Get all property keys first
            if (SteamInventory.GetItemDefinitionProperty(itemDef, null, out string keys, ref bufferSize))
            {
                if (!string.IsNullOrEmpty(keys))
                {
                    var keyArray = keys.Split(',');
                    foreach (var key in keyArray)
                    {
                        var trimmedKey = key.Trim();
                        if (!string.IsNullOrEmpty(trimmedKey))
                        {
                            var value = GetItemDefinitionProperty(itemDefId, trimmedKey);
                            properties[trimmedKey] = value;
                        }
                    }
                }
            }
#endif
            return properties;
        }

        /// <summary>
        /// Get item info for a specific definition ID.
        /// </summary>
        public ItemDefinitionInfo GetItemDefinitionInfo(int itemDefId)
        {
            var info = new ItemDefinitionInfo
            {
                ItemDefId = itemDefId,
                Name = GetItemDefinitionProperty(itemDefId, "name"),
                Description = GetItemDefinitionProperty(itemDefId, "description"),
                Type = GetItemDefinitionProperty(itemDefId, "type"),
                Price = GetItemDefinitionProperty(itemDefId, "price"),
                IconUrl = GetItemDefinitionProperty(itemDefId, "icon_url"),
                IconUrlLarge = GetItemDefinitionProperty(itemDefId, "icon_url_large"),
                Tradable = GetItemDefinitionProperty(itemDefId, "tradable") == "true",
                Marketable = GetItemDefinitionProperty(itemDefId, "marketable") == "true"
            };

            return info;
        }

        /// <summary>
        /// Get all item definitions.
        /// </summary>
        public List<ItemDefinitionInfo> GetAllItemDefinitions()
        {
            var definitions = new List<ItemDefinitionInfo>();
            var ids = GetItemDefinitionIDs();

            foreach (var id in ids)
            {
                definitions.Add(GetItemDefinitionInfo(id));
            }

            return definitions;
        }

        #endregion

        #region Callbacks

#if !DISABLESTEAMWORKS
        private void OnInventoryResultReady(SteamInventoryResultReady_t result)
        {
            if (result.m_result != EResult.k_EResultOK)
            {
                LogError($"Inventory result failed: {result.m_result}");
                OnError?.Invoke($"Inventory error: {result.m_result}");
                return;
            }

            _currentResult = result.m_handle;
            ParseInventoryResult(result.m_handle);
        }

        private void OnDefinitionUpdate(SteamInventoryDefinitionUpdate_t result)
        {
            DefinitionsLoaded = true;
            Log("Item definitions loaded.");
            OnDefinitionsLoaded?.Invoke();
        }

        private void ParseInventoryResult(SteamInventoryResult_t resultHandle)
        {
            Items.Clear();

            uint itemCount = 0;
            if (!SteamInventory.GetResultItems(resultHandle, null, ref itemCount))
            {
                LogError("Failed to get result item count.");
                return;
            }

            if (itemCount == 0)
            {
                Log("Inventory is empty.");
                OnInventoryUpdated?.Invoke(Items);
                return;
            }

            var details = new SteamItemDetails_t[itemCount];
            if (SteamInventory.GetResultItems(resultHandle, details, ref itemCount))
            {
                foreach (var detail in details)
                {
                    var item = new InventoryItem
                    {
                        ItemId = detail.m_itemId.m_SteamItemInstanceID,
                        ItemDefId = detail.m_iDefinition.m_SteamItemDef,
                        Quantity = detail.m_unQuantity,
                        Flags = (ushort)detail.m_unFlags
                    };

                    // Get item name from definition
                    item.Name = GetItemDefinitionProperty(item.ItemDefId, "name");
                    item.Description = GetItemDefinitionProperty(item.ItemDefId, "description");

                    Items.Add(item);
                }

                Log($"Loaded {Items.Count} inventory items.");
                OnInventoryUpdated?.Invoke(Items);
            }
        }
#endif

        #endregion

        #region Helpers

        private bool ValidateState()
        {
#if !DISABLESTEAMWORKS
            if (!SteamCore.Instance.IsInitialized)
            {
                LogError("Steam not initialized!");
                return false;
            }
            return true;
#else
            return false;
#endif
        }

        private void Log(string message)
        {
            if (SteamCore.Instance?.Config?.EnableDebugLogs ?? true)
                Debug.Log($"[SteamToolkit.Inventory] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SteamToolkit.Inventory] {message}");
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Inventory item instance.
    /// </summary>
    [Serializable]
    public class InventoryItem
    {
        public ulong ItemId;
        public int ItemDefId;
        public ushort Quantity;
        public ushort Flags;
        public string Name;
        public string Description;

        public bool IsNoTrade => (Flags & 1) != 0;
        public bool IsRemoved => (Flags & 2) != 0;
        public bool IsConsumed => (Flags & 4) != 0;
    }

    /// <summary>
    /// Item definition info.
    /// </summary>
    [Serializable]
    public class ItemDefinitionInfo
    {
        public int ItemDefId;
        public string Name;
        public string Description;
        public string Type;
        public string Price;
        public string IconUrl;
        public string IconUrlLarge;
        public bool Tradable;
        public bool Marketable;
    }

    #endregion
}