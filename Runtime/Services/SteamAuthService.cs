using System;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SteamToolkit
{
    /// <summary>
    /// Steam Authentication service.
    /// Handles session tickets and user authentication.
    /// </summary>
    public class SteamAuthService : IDisposable
    {
        #region Events

        public event Action<string> OnTicketReady;
        public event Action<string> OnTicketError;

        #endregion

        #region Properties

        public bool IsInitialized { get; private set; }
        
#if !DISABLESTEAMWORKS
        public bool HasPendingTicket => _authTicketHandle != HAuthTicket.Invalid;
#else
        public bool HasPendingTicket => false;
#endif

        #endregion

        #region Private Fields

#if !DISABLESTEAMWORKS
        private HAuthTicket _authTicketHandle;
        private byte[] _authTicketData;
        private uint _authTicketSize;
        private Callback<GetAuthSessionTicketResponse_t> _authTicketCallback;

        private Action<string> _onTicketReady;
        private Action<string> _onTicketError;
#endif

        #endregion

        #region Initialization

        public void Initialize()
        {
            if (IsInitialized) return;

#if !DISABLESTEAMWORKS
            _authTicketCallback = Callback<GetAuthSessionTicketResponse_t>.Create(OnAuthTicketResponse);
#endif
            IsInitialized = true;

            Log("Auth service initialized.");
        }

        public void Dispose()
        {
            CancelAuthTicket();
#if !DISABLESTEAMWORKS
            _authTicketCallback = null;
#endif
            IsInitialized = false;
            
            Log("Auth service disposed.");
        }

        #endregion

        #region Auth Ticket

        /// <summary>
        /// Get Steam session ticket for authentication (e.g., with UGS).
        /// </summary>
        /// <param name="onTicketReady">Called when ticket is ready (hex string)</param>
        /// <param name="onError">Called on error</param>
        public void GetAuthSessionTicket(Action<string> onTicketReady, Action<string> onError)
        {
#if DISABLESTEAMWORKS
            onError?.Invoke("Steamworks is disabled!");
            return;
#else
            if (!IsInitialized)
            {
                onError?.Invoke("Auth service not initialized!");
                return;
            }

            if (!SteamCore.Instance.IsInitialized)
            {
                onError?.Invoke("Steam not initialized!");
                return;
            }

            // Cancel pending ticket if any
            if (HasPendingTicket)
            {
                CancelAuthTicket();
            }

            try
            {
                // Store callbacks
                _onTicketReady = onTicketReady;
                _onTicketError = onError;

                // Create ticket buffer
                _authTicketData = new byte[1024];

                // New API with SteamNetworkingIdentity
                var identity = new SteamNetworkingIdentity();
                _authTicketHandle = SteamUser.GetAuthSessionTicket(
                    _authTicketData,
                    _authTicketData.Length,
                    out _authTicketSize,
                    ref identity
                );

                if (_authTicketHandle == HAuthTicket.Invalid)
                {
                    LogError("GetAuthSessionTicket failed!");
                    _onTicketError?.Invoke("Failed to get auth ticket.");
                    ClearTicketCallbacks();
                }
                else
                {
                    Log($"Ticket requested. Handle: {_authTicketHandle}, Size: {_authTicketSize}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Auth ticket error: {ex.Message}");
                onError?.Invoke(ex.Message);
                ClearTicketCallbacks();
            }
#endif
        }

        /// <summary>
        /// Get auth ticket synchronously (not recommended, use async version).
        /// </summary>
        public string GetAuthSessionTicketSync()
        {
#if DISABLESTEAMWORKS
            return null;
#else
            if (!IsInitialized || !SteamCore.Instance.IsInitialized)
                return null;

            try
            {
                var ticketData = new byte[1024];
                var identity = new SteamNetworkingIdentity();
                
                var handle = SteamUser.GetAuthSessionTicket(
                    ticketData,
                    ticketData.Length,
                    out uint ticketSize,
                    ref identity
                );

                if (handle == HAuthTicket.Invalid)
                    return null;

                SteamUser.CancelAuthTicket(handle);
                return BitConverter.ToString(ticketData, 0, (int)ticketSize).Replace("-", "");
            }
            catch
            {
                return null;
            }
#endif
        }

#if !DISABLESTEAMWORKS
        private void OnAuthTicketResponse(GetAuthSessionTicketResponse_t response)
        {
            if (response.m_hAuthTicket != _authTicketHandle)
                return;

            if (response.m_eResult == EResult.k_EResultOK)
            {
                var ticketString = BitConverter.ToString(_authTicketData, 0, (int)_authTicketSize).Replace("-", "");
                Log($"Auth ticket received. Size: {_authTicketSize}");
                
                _onTicketReady?.Invoke(ticketString);
                OnTicketReady?.Invoke(ticketString);
            }
            else
            {
                var errorMsg = $"Failed to get ticket: {response.m_eResult}";
                LogError($"Auth ticket error: {response.m_eResult}");
                
                _onTicketError?.Invoke(errorMsg);
                OnTicketError?.Invoke(errorMsg);
            }

            ClearTicketCallbacks();
        }
#endif

        /// <summary>
        /// Cancel pending auth ticket.
        /// </summary>
        public void CancelAuthTicket()
        {
#if !DISABLESTEAMWORKS
            if (_authTicketHandle != HAuthTicket.Invalid)
            {
                SteamUser.CancelAuthTicket(_authTicketHandle);
                _authTicketHandle = HAuthTicket.Invalid;
                _authTicketData = null;
                _authTicketSize = 0;
                ClearTicketCallbacks();
                Log("Auth ticket cancelled.");
            }
#endif
        }

#if !DISABLESTEAMWORKS
        private void ClearTicketCallbacks()
        {
            _onTicketReady = null;
            _onTicketError = null;
        }
#endif

        #endregion

        #region Session Validation

#if !DISABLESTEAMWORKS
        /// <summary>
        /// Validate another user's ticket (for server-side validation).
        /// </summary>
        public EBeginAuthSessionResult BeginAuthSession(byte[] ticket, CSteamID steamId)
        {
            if (!IsInitialized || !SteamCore.Instance.IsInitialized)
                return EBeginAuthSessionResult.k_EBeginAuthSessionResultInvalidTicket;

            return SteamUser.BeginAuthSession(ticket, ticket.Length, steamId);
        }

        /// <summary>
        /// End auth session.
        /// </summary>
        public void EndAuthSession(CSteamID steamId)
        {
            if (!IsInitialized || !SteamCore.Instance.IsInitialized)
                return;

            SteamUser.EndAuthSession(steamId);
        }
#endif

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (SteamCore.Instance?.Config?.EnableDebugLogs ?? true)
                Debug.Log($"[SteamToolkit.Auth] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SteamToolkit.Auth] {message}");
        }

        #endregion
    }
}