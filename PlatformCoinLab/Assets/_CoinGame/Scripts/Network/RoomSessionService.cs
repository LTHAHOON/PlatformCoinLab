using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Multiplayer.Components;
using UnityEngine;

namespace CoinGame.Network
{
    public sealed class RoomSessionService : IDisposable
    {
        private SessionConnector _sessionConnector;
        private MultiplayerSession _multiplayerSession;
        private bool _isConnecting;
        private bool _isDisposing;
        private string _requestedSessionId;

        public ISession Session { get; private set; }
        public bool IsServicesReady { get; private set; }
        public bool IsSessionReady => Session != null;
        public bool IsHost => Session?.IsHost ?? false;

        public event Action<ISession> Connected;
        public event Action<string> Failed;

        public async UniTask InitializeServicesAsync(
            CancellationToken cancellationToken, string authenticationProfile = null, bool logPlayerId = false)
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    InitializationOptions options = string.IsNullOrWhiteSpace(authenticationProfile)
                        ? null
                        : new InitializationOptions().SetProfile(authenticationProfile);
                    await UnityServices.InitializeAsync(options);
                }
                else if (!string.IsNullOrWhiteSpace(authenticationProfile))
                {
                    if (AuthenticationService.Instance.IsSignedIn)
                    {
                        AuthenticationService.Instance.SignOut();
                    }
                    AuthenticationService.Instance.SwitchProfile(authenticationProfile);
                }
                cancellationToken.ThrowIfCancellationRequested();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                cancellationToken.ThrowIfCancellationRequested();

                if (logPlayerId)
                {
                    Debug.Log($"[DevQuickStart] Auth profile='{authenticationProfile}', PlayerId='{AuthenticationService.Instance.PlayerId}'.");
                }

                IsServicesReady = true;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogError($"[RoomSessionService] Service initialization failed: {exception}");
                Failed?.Invoke($"Service initialization failed: {exception.Message}");
            }
        }

        public async UniTask ConnectAsync(RoomSettings settings, bool createOrJoin, CancellationToken cancellationToken)
        {
            if (!IsServicesReady || IsSessionReady || _isConnecting)
            {
                return;
            }

            _isConnecting = true;
            _requestedSessionId = settings.RoomId;
            try
            {
                await ResetNetworkManagerAsync(cancellationToken);
                await CleanupRegisteredSessionsAsync();
                DisposeConnector();

                _multiplayerSession = ScriptableObject.CreateInstance<MultiplayerSession>();
                _sessionConnector = ScriptableObject.CreateInstance<SessionConnector>();
                _sessionConnector.MultiplayerSession = _multiplayerSession;
                _sessionConnector.SuccessfulExecution.AddListener(OnSessionConnected);
                _sessionConnector.FailedExecution.AddListener(OnSessionFailed);

                if (createOrJoin)
                {
                    Dictionary<string, SessionProperty> properties = new()
                    {
                        ["collectors"] = new SessionProperty(settings.MaxCollectors.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.String1),
                        ["disruptors"] = new SessionProperty(settings.MaxDisruptors.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.String2),
                        ["maxRounds"] = new SessionProperty(settings.MaxRounds.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.String3),
                        ["coinMultiplier"] = new SessionProperty(settings.CoinMultiplier.ToString(), VisibilityPropertyOptions.Public, PropertyIndex.String4),
                    };
                    SessionOptions sessionOptions = new()
                    {
                        Name = settings.RoomName,
                        MaxPlayers = settings.MaxPlayers,
                        SessionProperties = properties,
                    };
                    _sessionConnector.WithCreateOrJoin(settings.RoomId)
                        .WithSessionOptions(sessionOptions)
                        .WithRelayNetworkOptions(RelayNetworkOptions.Default, RelayProtocol.Default)
                        .Execute();
                }
                else
                {
                    _sessionConnector.WithJoinById(settings.RoomId).Execute();
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                OnSessionFailed($"Room connection failed: {exception.Message}");
            }
            finally
            {
                _isConnecting = false;
            }
        }

        private async UniTask ResetNetworkManagerAsync(CancellationToken cancellationToken)
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening)
            {
                return;
            }

            networkManager.Shutdown();
            await UniTask.WaitUntil(() => !networkManager.IsListening, cancellationToken: cancellationToken);
        }

        private async UniTask CleanupRegisteredSessionsAsync()
        {
            if (MultiplayerService.Instance == null || MultiplayerService.Instance.Sessions.Count == 0)
            {
                return;
            }

            List<ISession> sessions = new(MultiplayerService.Instance.Sessions.Values);
            foreach (ISession session in sessions)
            {
                try
                {
                    await session.LeaveAsync();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Failed to clean up stale session: {exception.Message}");
                }
            }

            for (int i = 0; i < 50 && MultiplayerService.Instance != null &&
                MultiplayerService.Instance.Sessions.Count > 0; i++)
            {
                await UniTask.Delay(100);
            }
        }

        private void OnSessionConnected(ISession session)
        {
            Session = session;
            Connected?.Invoke(session);
        }

        private void OnSessionFailed(string message)
        {
            if (message.Contains("already a member", StringComparison.OrdinalIgnoreCase))
            {
                ISession existingSession = MultiplayerService.Instance?.Sessions.Values
                    .FirstOrDefault(session => session.Id == _requestedSessionId);
                if (existingSession != null)
                {
                    OnSessionConnected(existingSession);
                    return;
                }
            }

            Session = null;
            Failed?.Invoke(message);
        }

        private void DisposeConnector()
        {
            if (_sessionConnector != null)
            {
                _sessionConnector.SuccessfulExecution.RemoveListener(OnSessionConnected);
                _sessionConnector.FailedExecution.RemoveListener(OnSessionFailed);
                UnityEngine.Object.Destroy(_sessionConnector);
                _sessionConnector = null;
            }
            if (_multiplayerSession != null)
            {
                UnityEngine.Object.Destroy(_multiplayerSession);
                _multiplayerSession = null;
            }
        }

        public async UniTask DisposeAsync()
        {
            if (_isDisposing)
            {
                return;
            }

            _isDisposing = true;
            ISession session = Session;
            Session = null;
            _isConnecting = false;
            if (session != null)
            {
                try
                {
                    if (session.IsHost)
                    {
                        await session.AsHost().DeleteAsync();
                    }
                    else
                    {
                        await session.LeaveAsync();
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Failed to close development session: {exception.Message}");
                }
            }

            DisposeConnector();
            Connected = null;
            Failed = null;
        }

        public void Dispose()
        {
            DisposeAsync().Forget();
        }
    }
}
