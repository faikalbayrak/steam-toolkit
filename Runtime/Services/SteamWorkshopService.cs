using System;
using System.Collections.Generic;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SteamToolkit
{
    /// <summary>
    /// Steam Workshop service.
    /// Handles UGC (User Generated Content) creation, subscription, and queries.
    /// </summary>
    public class SteamWorkshopService : IDisposable
    {
        #region Events

        /// <summary>
        /// Fired when item creation starts.
        /// </summary>
        public event Action<ulong> OnItemCreated;

        /// <summary>
        /// Fired when item update is submitted.
        /// </summary>
        public event Action<ulong, bool> OnItemUpdated; // itemId, needsAcceptAgreement

        /// <summary>
        /// Fired when item is subscribed.
        /// </summary>
        public event Action<ulong> OnItemSubscribed;

        /// <summary>
        /// Fired when item is unsubscribed.
        /// </summary>
        public event Action<ulong> OnItemUnsubscribed;

        /// <summary>
        /// Fired when query results are ready.
        /// </summary>
        public event Action<List<WorkshopItem>> OnQueryCompleted;

        /// <summary>
        /// Fired on any error.
        /// </summary>
        public event Action<string> OnError;

        #endregion

        #region Properties

        public bool IsInitialized { get; private set; }

        #endregion

        #region Private Fields

#if !DISABLESTEAMWORKS
        private CallResult<CreateItemResult_t> _createItemCallResult;
        private CallResult<SubmitItemUpdateResult_t> _submitItemUpdateCallResult;
        private CallResult<SteamUGCQueryCompleted_t> _queryCompletedCallResult;
        private CallResult<RemoteStorageSubscribePublishedFileResult_t> _subscribeCallResult;
        private CallResult<RemoteStorageUnsubscribePublishedFileResult_t> _unsubscribeCallResult;

        private UGCUpdateHandle_t _currentUpdateHandle;
        private Action<ulong, bool> _updateCallback;
        private Action<List<WorkshopItem>> _queryCallback;
#endif

        #endregion

        #region Initialization

        public void Initialize()
        {
            if (IsInitialized) return;

#if !DISABLESTEAMWORKS
            _createItemCallResult = CallResult<CreateItemResult_t>.Create(OnCreateItemResult);
            _submitItemUpdateCallResult = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitItemUpdateResult);
            _queryCompletedCallResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnQueryCompletedResult);
            _subscribeCallResult = CallResult<RemoteStorageSubscribePublishedFileResult_t>.Create(OnSubscribeResult);
            _unsubscribeCallResult = CallResult<RemoteStorageUnsubscribePublishedFileResult_t>.Create(OnUnsubscribeResult);
#endif

            IsInitialized = true;
            Log("Workshop service initialized.");
        }

        public void Dispose()
        {
#if !DISABLESTEAMWORKS
            _createItemCallResult = null;
            _submitItemUpdateCallResult = null;
            _queryCompletedCallResult = null;
            _subscribeCallResult = null;
            _unsubscribeCallResult = null;
#endif

            IsInitialized = false;
            Log("Workshop service disposed.");
        }

        #endregion

        #region Create Item

        /// <summary>
        /// Create a new Workshop item.
        /// </summary>
        /// <param name="onComplete">Callback with new item ID</param>
        public void CreateItem(Action<ulong> onComplete = null)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return;

            var appId = SteamUtils.GetAppID();
            var handle = SteamUGC.CreateItem(appId, EWorkshopFileType.k_EWorkshopFileTypeCommunity);
            
            _createItemCallResult.Set(handle, (result, failure) =>
            {
                if (failure || result.m_eResult != EResult.k_EResultOK)
                {
                    var error = $"Failed to create item: {result.m_eResult}";
                    LogError(error);
                    OnError?.Invoke(error);
                    return;
                }

                var itemId = result.m_nPublishedFileId.m_PublishedFileId;
                Log($"Item created: {itemId}");
                
                if (result.m_bUserNeedsToAcceptWorkshopLegalAgreement)
                {
                    LogWarning("User needs to accept Workshop legal agreement.");
                }

                onComplete?.Invoke(itemId);
                OnItemCreated?.Invoke(itemId);
            });
#endif
        }

        #endregion

        #region Update Item

        /// <summary>
        /// Start updating a Workshop item.
        /// </summary>
        /// <param name="itemId">Item ID to update</param>
        /// <returns>Update handle for chaining</returns>
        public WorkshopItemUpdate BeginItemUpdate(ulong itemId)
        {
#if !DISABLESTEAMWORKS
            var appId = SteamUtils.GetAppID();
            _currentUpdateHandle = SteamUGC.StartItemUpdate(appId, new PublishedFileId_t(itemId));
            return new WorkshopItemUpdate(this, itemId, _currentUpdateHandle);
#else
            return null;
#endif
        }

        /// <summary>
        /// Submit item update to Workshop.
        /// </summary>
        internal void SubmitItemUpdate(string changeNote, Action<ulong, bool> onComplete)
        {
#if !DISABLESTEAMWORKS
            _updateCallback = onComplete;
            var handle = SteamUGC.SubmitItemUpdate(_currentUpdateHandle, changeNote);
            _submitItemUpdateCallResult.Set(handle);
#endif
        }

        /// <summary>
        /// Get item update progress.
        /// </summary>
        public float GetItemUpdateProgress(out ulong bytesProcessed, out ulong bytesTotal)
        {
            bytesProcessed = 0;
            bytesTotal = 0;

#if !DISABLESTEAMWORKS
            var status = SteamUGC.GetItemUpdateProgress(_currentUpdateHandle, out bytesProcessed, out bytesTotal);
            
            if (bytesTotal > 0)
            {
                return (float)bytesProcessed / bytesTotal;
            }
#endif
            return 0f;
        }

#if !DISABLESTEAMWORKS
        private void OnCreateItemResult(CreateItemResult_t result, bool failure)
        {
            // Handled inline
        }

        private void OnSubmitItemUpdateResult(SubmitItemUpdateResult_t result, bool failure)
        {
            if (failure || result.m_eResult != EResult.k_EResultOK)
            {
                var error = $"Failed to update item: {result.m_eResult}";
                LogError(error);
                OnError?.Invoke(error);
                _updateCallback?.Invoke(0, false);
                return;
            }

            var itemId = result.m_nPublishedFileId.m_PublishedFileId;
            var needsAgreement = result.m_bUserNeedsToAcceptWorkshopLegalAgreement;

            Log($"Item updated: {itemId}");
            _updateCallback?.Invoke(itemId, needsAgreement);
            OnItemUpdated?.Invoke(itemId, needsAgreement);
        }
#endif

        #endregion

        #region Subscribe/Unsubscribe

        /// <summary>
        /// Subscribe to a Workshop item.
        /// </summary>
        /// <param name="itemId">Item ID</param>
        /// <param name="onComplete">Callback with success status</param>
        public void Subscribe(ulong itemId, Action<bool> onComplete = null)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return;

            var handle = SteamUGC.SubscribeItem(new PublishedFileId_t(itemId));
            _subscribeCallResult.Set(handle, (result, failure) =>
            {
                if (failure || result.m_eResult != EResult.k_EResultOK)
                {
                    LogError($"Failed to subscribe: {result.m_eResult}");
                    onComplete?.Invoke(false);
                    return;
                }

                Log($"Subscribed to item: {itemId}");
                onComplete?.Invoke(true);
                OnItemSubscribed?.Invoke(itemId);
            });
#endif
        }

        /// <summary>
        /// Unsubscribe from a Workshop item.
        /// </summary>
        /// <param name="itemId">Item ID</param>
        /// <param name="onComplete">Callback with success status</param>
        public void Unsubscribe(ulong itemId, Action<bool> onComplete = null)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return;

            var handle = SteamUGC.UnsubscribeItem(new PublishedFileId_t(itemId));
            _unsubscribeCallResult.Set(handle, (result, failure) =>
            {
                if (failure || result.m_eResult != EResult.k_EResultOK)
                {
                    LogError($"Failed to unsubscribe: {result.m_eResult}");
                    onComplete?.Invoke(false);
                    return;
                }

                Log($"Unsubscribed from item: {itemId}");
                onComplete?.Invoke(true);
                OnItemUnsubscribed?.Invoke(itemId);
            });
#endif
        }

        #endregion

        #region Query Items

        /// <summary>
        /// Query user's subscribed items.
        /// </summary>
        /// <param name="onComplete">Callback with items</param>
        public void QuerySubscribedItems(Action<List<WorkshopItem>> onComplete = null)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return;

            _queryCallback = onComplete;

            var appId = SteamUtils.GetAppID();
            var accountId = SteamUser.GetSteamID().GetAccountID();
            
            var queryHandle = SteamUGC.CreateQueryUserUGCRequest(
                accountId,
                EUserUGCList.k_EUserUGCList_Subscribed,
                EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items,
                EUserUGCListSortOrder.k_EUserUGCListSortOrder_SubscriptionDateDesc,
                appId,
                appId,
                1
            );

            SteamUGC.SetReturnMetadata(queryHandle, true);
            SteamUGC.SetReturnLongDescription(queryHandle, true);

            var handle = SteamUGC.SendQueryUGCRequest(queryHandle);
            _queryCompletedCallResult.Set(handle);
#endif
        }

        /// <summary>
        /// Query user's published items.
        /// </summary>
        /// <param name="onComplete">Callback with items</param>
        public void QueryPublishedItems(Action<List<WorkshopItem>> onComplete = null)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return;

            _queryCallback = onComplete;

            var appId = SteamUtils.GetAppID();
            var accountId = SteamUser.GetSteamID().GetAccountID();
            
            var queryHandle = SteamUGC.CreateQueryUserUGCRequest(
                accountId,
                EUserUGCList.k_EUserUGCList_Published,
                EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items,
                EUserUGCListSortOrder.k_EUserUGCListSortOrder_CreationOrderDesc,
                appId,
                appId,
                1
            );

            SteamUGC.SetReturnMetadata(queryHandle, true);
            SteamUGC.SetReturnLongDescription(queryHandle, true);

            var handle = SteamUGC.SendQueryUGCRequest(queryHandle);
            _queryCompletedCallResult.Set(handle);
#endif
        }

        /// <summary>
        /// Query popular items.
        /// </summary>
        /// <param name="onComplete">Callback with items</param>
        public void QueryPopularItems(Action<List<WorkshopItem>> onComplete = null)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return;

            _queryCallback = onComplete;

            var appId = SteamUtils.GetAppID();
            
            var queryHandle = SteamUGC.CreateQueryAllUGCRequest(
                EUGCQuery.k_EUGCQuery_RankedByVote,
                EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items,
                appId,
                appId,
                1
            );

            SteamUGC.SetReturnMetadata(queryHandle, true);
            SteamUGC.SetReturnLongDescription(queryHandle, false);

            var handle = SteamUGC.SendQueryUGCRequest(queryHandle);
            _queryCompletedCallResult.Set(handle);
#endif
        }

#if !DISABLESTEAMWORKS
        private void OnQueryCompletedResult(SteamUGCQueryCompleted_t result, bool failure)
        {
            var items = new List<WorkshopItem>();

            if (failure || result.m_eResult != EResult.k_EResultOK)
            {
                LogError($"Query failed: {result.m_eResult}");
                _queryCallback?.Invoke(items);
                OnQueryCompleted?.Invoke(items);
                return;
            }

            for (uint i = 0; i < result.m_unNumResultsReturned; i++)
            {
                if (SteamUGC.GetQueryUGCResult(result.m_handle, i, out SteamUGCDetails_t details))
                {
                    var item = new WorkshopItem
                    {
                        ItemId = details.m_nPublishedFileId.m_PublishedFileId,
                        Title = details.m_rgchTitle,
                        Description = details.m_rgchDescription,
                        OwnerId = details.m_ulSteamIDOwner,
                        CreatedTime = DateTimeOffset.FromUnixTimeSeconds(details.m_rtimeCreated).LocalDateTime,
                        UpdatedTime = DateTimeOffset.FromUnixTimeSeconds(details.m_rtimeUpdated).LocalDateTime,
                        VotesUp = details.m_unVotesUp,
                        VotesDown = details.m_unVotesDown,
                        Score = details.m_flScore,
                        FileSize = details.m_nFileSize,
                        Tags = details.m_rgchTags
                    };

                    // Get preview URL
                    if (SteamUGC.GetQueryUGCPreviewURL(result.m_handle, i, out string previewUrl, 1024))
                    {
                        item.PreviewUrl = previewUrl;
                    }

                    items.Add(item);
                }
            }

            SteamUGC.ReleaseQueryUGCRequest(result.m_handle);

            Log($"Query returned {items.Count} items.");
            _queryCallback?.Invoke(items);
            OnQueryCompleted?.Invoke(items);
        }

        private void OnSubscribeResult(RemoteStorageSubscribePublishedFileResult_t result, bool failure)
        {
            // Handled inline
        }

        private void OnUnsubscribeResult(RemoteStorageUnsubscribePublishedFileResult_t result, bool failure)
        {
            // Handled inline
        }
#endif

        #endregion

        #region Get Installed Items

        /// <summary>
        /// Get number of subscribed items.
        /// </summary>
        public uint GetSubscribedItemCount()
        {
#if !DISABLESTEAMWORKS
            return SteamUGC.GetNumSubscribedItems();
#else
            return 0;
#endif
        }

        /// <summary>
        /// Get all subscribed item IDs.
        /// </summary>
        public ulong[] GetSubscribedItemIds()
        {
#if !DISABLESTEAMWORKS
            uint count = SteamUGC.GetNumSubscribedItems();
            if (count == 0) return new ulong[0];

            var ids = new PublishedFileId_t[count];
            uint retrieved = SteamUGC.GetSubscribedItems(ids, count);

            var result = new ulong[retrieved];
            for (int i = 0; i < retrieved; i++)
            {
                result[i] = ids[i].m_PublishedFileId;
            }

            return result;
#else
            return new ulong[0];
#endif
        }

        /// <summary>
        /// Get installed item info.
        /// </summary>
        /// <param name="itemId">Item ID</param>
        /// <returns>Install info or null if not installed</returns>
        public WorkshopInstallInfo GetInstalledItemInfo(ulong itemId)
        {
#if !DISABLESTEAMWORKS
            var appId = SteamUtils.GetAppID();
            
            if (SteamUGC.GetItemInstallInfo(
                new PublishedFileId_t(itemId),
                out ulong sizeOnDisk,
                out string folder,
                1024,
                out uint timestamp))
            {
                return new WorkshopInstallInfo
                {
                    ItemId = itemId,
                    SizeOnDisk = sizeOnDisk,
                    FolderPath = folder,
                    Timestamp = timestamp
                };
            }
#endif
            return null;
        }

        /// <summary>
        /// Get item state flags.
        /// </summary>
        public WorkshopItemState GetItemState(ulong itemId)
        {
#if !DISABLESTEAMWORKS
            var state = SteamUGC.GetItemState(new PublishedFileId_t(itemId));
            return new WorkshopItemState
            {
                IsSubscribed = (state & (uint)EItemState.k_EItemStateSubscribed) != 0,
                IsInstalled = (state & (uint)EItemState.k_EItemStateInstalled) != 0,
                NeedsUpdate = (state & (uint)EItemState.k_EItemStateNeedsUpdate) != 0,
                IsDownloading = (state & (uint)EItemState.k_EItemStateDownloading) != 0,
                IsDownloadPending = (state & (uint)EItemState.k_EItemStateDownloadPending) != 0
            };
#else
            return new WorkshopItemState();
#endif
        }

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
                Debug.Log($"[SteamToolkit.Workshop] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SteamToolkit.Workshop] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SteamToolkit.Workshop] {message}");
        }

        #endregion
    }

    #region Workshop Item Update Builder

    /// <summary>
    /// Fluent builder for Workshop item updates.
    /// </summary>
    public class WorkshopItemUpdate
    {
        private readonly SteamWorkshopService _service;
        private readonly ulong _itemId;
#if !DISABLESTEAMWORKS
        private readonly UGCUpdateHandle_t _handle;
#endif

        internal WorkshopItemUpdate(SteamWorkshopService service, ulong itemId, object handle)
        {
            _service = service;
            _itemId = itemId;
#if !DISABLESTEAMWORKS
            _handle = (UGCUpdateHandle_t)handle;
#endif
        }

        /// <summary>
        /// Set item title.
        /// </summary>
        public WorkshopItemUpdate SetTitle(string title)
        {
#if !DISABLESTEAMWORKS
            SteamUGC.SetItemTitle(_handle, title);
#endif
            return this;
        }

        /// <summary>
        /// Set item description.
        /// </summary>
        public WorkshopItemUpdate SetDescription(string description)
        {
#if !DISABLESTEAMWORKS
            SteamUGC.SetItemDescription(_handle, description);
#endif
            return this;
        }

        /// <summary>
        /// Set item content folder path.
        /// </summary>
        public WorkshopItemUpdate SetContent(string folderPath)
        {
#if !DISABLESTEAMWORKS
            SteamUGC.SetItemContent(_handle, folderPath);
#endif
            return this;
        }

        /// <summary>
        /// Set item preview image path.
        /// </summary>
        public WorkshopItemUpdate SetPreviewImage(string imagePath)
        {
#if !DISABLESTEAMWORKS
            SteamUGC.SetItemPreview(_handle, imagePath);
#endif
            return this;
        }

        /// <summary>
        /// Set item visibility.
        /// </summary>
        public WorkshopItemUpdate SetVisibility(WorkshopVisibility visibility)
        {
#if !DISABLESTEAMWORKS
            var steamVisibility = visibility switch
            {
                WorkshopVisibility.Public => ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic,
                WorkshopVisibility.FriendsOnly => ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityFriendsOnly,
                WorkshopVisibility.Private => ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate,
                _ => ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic
            };
            SteamUGC.SetItemVisibility(_handle, steamVisibility);
#endif
            return this;
        }

        /// <summary>
        /// Set item tags.
        /// </summary>
        public WorkshopItemUpdate SetTags(params string[] tags)
        {
#if !DISABLESTEAMWORKS
            SteamUGC.SetItemTags(_handle, new List<string>(tags));
#endif
            return this;
        }

        /// <summary>
        /// Set item metadata.
        /// </summary>
        public WorkshopItemUpdate SetMetadata(string metadata)
        {
#if !DISABLESTEAMWORKS
            SteamUGC.SetItemMetadata(_handle, metadata);
#endif
            return this;
        }

        /// <summary>
        /// Submit the update to Workshop.
        /// </summary>
        /// <param name="changeNote">Change note for the update</param>
        /// <param name="onComplete">Callback with (itemId, needsAgreement)</param>
        public void Submit(string changeNote = null, Action<ulong, bool> onComplete = null)
        {
            _service.SubmitItemUpdate(changeNote, onComplete);
        }
    }

    #endregion

    #region Data Classes

    /// <summary>
    /// Workshop item data.
    /// </summary>
    [Serializable]
    public class WorkshopItem
    {
        public ulong ItemId;
        public string Title;
        public string Description;
        public ulong OwnerId;
        public DateTime CreatedTime;
        public DateTime UpdatedTime;
        public uint VotesUp;
        public uint VotesDown;
        public float Score;
        public int FileSize;
        public string Tags;
        public string PreviewUrl;

        public string FileSizeFormatted
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                else if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024f:0.#} KB";
                else
                    return $"{FileSize / (1024f * 1024f):0.##} MB";
            }
        }
    }

    /// <summary>
    /// Workshop item install info.
    /// </summary>
    [Serializable]
    public class WorkshopInstallInfo
    {
        public ulong ItemId;
        public ulong SizeOnDisk;
        public string FolderPath;
        public uint Timestamp;

        public DateTime InstalledTime => DateTimeOffset.FromUnixTimeSeconds(Timestamp).LocalDateTime;
    }

    /// <summary>
    /// Workshop item state.
    /// </summary>
    [Serializable]
    public class WorkshopItemState
    {
        public bool IsSubscribed;
        public bool IsInstalled;
        public bool NeedsUpdate;
        public bool IsDownloading;
        public bool IsDownloadPending;
    }

    /// <summary>
    /// Workshop item visibility.
    /// </summary>
    public enum WorkshopVisibility
    {
        Public,
        FriendsOnly,
        Private
    }

    #endregion
}