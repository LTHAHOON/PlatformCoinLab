using System;
using System.Collections.Generic;
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

        public ISession Session { get; private set; }
        public bool IsServicesReady { get; private set; }
        public bool IsSessionReady => Session != null;
        public bool IsHost => Session?.IsHost ?? false;

        public event Action<ISession> Connected;
        public event Action<string> Failed;

        public async UniTask InitializeServicesAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    await UnityServices.InitializeAsync();
                }
                cancellationToken.ThrowIfCancellationRequested();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                cancellationToken.ThrowIfCancellationRequested();

                IsServicesReady = true;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Failed?.Invoke($"Service initialization failed: {exception.Message}");
            }
        }

        public async UniTask ConnectAsync(RoomSettings settings, bool createOrJoin, CancellationToken cancellationToken)
        {
            if (!IsServicesReady || IsSessionReady)
            {
                return;
            }

            try
            {
                await ResetNetworkManagerAsync(cancellationToken);
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

        private void OnSessionConnected(ISession session)
        {
            Session = session;
            Connected?.Invoke(session);
        }

        private void OnSessionFailed(string message)
        {
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

        public void Dispose()
        {
            ISession session = Session;
            Session = null;
            if (session != null)
            {
                if (session.IsHost)
                {
                    _ = session.AsHost().DeleteAsync();
                }
                else
                {
                    _ = session.LeaveAsync();
                }
            }

            DisposeConnector();
            Connected = null;
            Failed = null;
        }
    }
}
