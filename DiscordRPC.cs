using System;
using System.Collections;
using System.Collections.Generic;
using NetDiscordRpc.Core.IO;
using NetDiscordRpc.Core.Exceptions;
using NetDiscordRpc.Core.Logger;
using NetDiscordRpc.Core.Registry;
using NetDiscordRpc.Events;
using NetDiscordRpc.Message;
using NetDiscordRpc.Message.Messages;
using NetDiscordRpc.RPC;
using NetDiscordRpc.RPC.Commands;
using NetDiscordRpc.Users;

namespace NetDiscordRpc
{
    public sealed class DiscordRPC : IDisposable
    {
        public bool HasRegisteredUriScheme { get; private set; }
        
        public string ApplicationID { get; private set; }
        
        public string SteamID { get; private set; }
        
        public int ProcessID { get; private set; }
        
        public int MaxQueueSize { get; private set; }
        
        public bool IsDisposed { get; private set; }
        
        public IConsoleLogger Logger
        {
            get => _logger;
            set
            {
                this._logger = value;
                if (connection != null) connection.Logger = value;
            }
        }
        private IConsoleLogger _logger;
        
        public bool AutoEvents { get; private set; }
        
        public bool SkipIdenticalPresence { get; set; }
        
        public int TargetPipe { get; private set; }

        private RpcConnection connection;
        
        public RichPresence CurrentPresence { get; private set; }
        
        public EventTypes Subscription { get; private set; }
        
        public Button[] Buttons { get; set; }
        
        public User CurrentUser { get; private set; }
        
        public Configuration Configuration { get; private set; }
        
        public bool IsInitialized { get; private set; }
        
        public bool ShutdownOnly
        {
            get => _shutdownOnly;
            set
            {
                _shutdownOnly = value;
                if (connection != null) connection.ShutdownOnly = value;
            }
        }
        private bool _shutdownOnly = true;
        private object _sync = new();
        
        public event OnReadyEvent OnReady;
        
        public event OnCloseEvent OnClose;
        
        public event OnErrorEvent OnError;
        
        public event OnPresenceUpdateEvent OnPresenceUpdate;
        
        public event OnSubscribeEvent OnSubscribe;
        
        public event OnUnsubscribeEvent OnUnsubscribe;
        
        public event OnJoinEvent OnJoin;
        
        public event OnSpectateEvent OnSpectate;
        
        public event OnJoinRequestedEvent OnJoinRequested;
        
        public event OnConnectionEstablishedEvent OnConnectionEstablished;
        
        public event OnConnectionFailedEvent OnConnectionFailed;
        
        public event OnRpcMessageEvent OnRpcMessage;
        
        public DiscordRPC(string applicationID) : this(applicationID, -1) { }

        public DiscordRPC(string applicationID, int pipe = -1, IConsoleLogger logger = null, bool autoEvents = true, INamedPipeClient client = null)
        {
            if (string.IsNullOrEmpty(applicationID))
            {
                throw new ArgumentNullException(nameof(applicationID));
            }
            
            var jsonConverterType = typeof(Newtonsoft.Json.JsonConverter);
            if (jsonConverterType == null) throw new Exception("JsonConverter Type Not Found");
            
            ApplicationID = applicationID.Trim();
            TargetPipe = pipe;
            ProcessID = Environment.ProcessId;
            HasRegisteredUriScheme = false;
            AutoEvents = autoEvents;
            SkipIdenticalPresence = true;
            
            _logger = logger ?? new NullLogger();
            
            connection = new RpcConnection(ApplicationID, ProcessID, TargetPipe, client ?? new ManagedNamedPipeClient(), autoEvents ? 0 : 128U)
            {
                ShutdownOnly = _shutdownOnly,
                Logger = _logger
            };
            
            connection.OnRpcMessage += (_, msg) =>
            {
                OnRpcMessage?.Invoke(this, msg);

                if (AutoEvents) ProcessMessage(msg);
            };
        }
        
        public IMessage[] Invoke()
        {
            if (AutoEvents)
            {
                Logger.Error("Cannot Invoke client when AutomaticallyInvokeEvents has been set.");
                return Array.Empty<IMessage>();
            }
            
            var messages = connection.DequeueMessages();
            for (var i = 0; i < messages.Length; i++)
            {
                var message = messages[i];
                ProcessMessage(message);
            }
            
            return messages;
        }
        
		private void ProcessMessage(IMessage message)
        {
            if (message == null) return;
            switch (message.Type)
            {
                case MessageTypes.PresenceUpdate:
                    lock (_sync)
                    {
                        var pm = message as PresenceMessage;
                        if (pm != null)
                        {
                            if (CurrentPresence == null)
                            {
                                CurrentPresence = (new RichPresence()).Merge(pm.Presence);
                            }
                            else if (pm.Presence == null)
                            {
                                CurrentPresence = null;
                            }
                            else
                            {
                                CurrentPresence.Merge(pm.Presence);
                            }
                            
                            pm.Presence = CurrentPresence;
                        }
                    }

                    break;
                
                case MessageTypes.Ready:
                    var rm = message as ReadyMessage;
                    if (rm != null)
                    {
                        lock (_sync)
                        {
                            Configuration = rm.Configuration;
                            CurrentUser = rm.User;
                        }
                        
                        SynchronizeState();
                    }
                    break;
                
                case MessageTypes.JoinRequest:
                    if (Configuration != null)
                    {
                        var jrm = message as JoinRequestMessage;
                        jrm?.User.SetConfiguration(Configuration);
                    }
                    break;

                case MessageTypes.Subscribe:
                    lock (_sync)
                    {
                        var sub = message as SubscribeMessage;
                        Subscription |= sub.Event;
                    }
                    break;

                case MessageTypes.Unsubscribe:
                    lock (_sync)
                    {
                        var unsub = message as UnsubscribeMessage;
                        Subscription &= ~unsub.Event;
                    }
                    break;
                
                case MessageTypes.Close:
                case MessageTypes.Error:
                case MessageTypes.Join:
                case MessageTypes.Spectate:
                case MessageTypes.ConnectionEstablished:
                case MessageTypes.ConnectionFailed:
                    break;
                default:
                    break;
            }
            
            switch (message.Type)
            {
                case MessageTypes.Ready:
                    OnReady?.Invoke(this, message as ReadyMessage);
                break;

                case MessageTypes.Close:
                    OnClose?.Invoke(this, message as CloseMessage);
                break;

                case MessageTypes.Error:
                    OnError?.Invoke(this, message as ErrorMessage);
                break;

                case MessageTypes.PresenceUpdate:
                    OnPresenceUpdate?.Invoke(this, message as PresenceMessage);
                break;

                case MessageTypes.Subscribe:
                    OnSubscribe?.Invoke(this, message as SubscribeMessage);
                break;

                case MessageTypes.Unsubscribe:
                    OnUnsubscribe?.Invoke(this, message as UnsubscribeMessage);
                break;

                case MessageTypes.Join:
                    OnJoin?.Invoke(this, message as JoinMessage);
                break;

                case MessageTypes.Spectate:
                    OnSpectate?.Invoke(this, message as SpectateMessage);
                break;

                case MessageTypes.JoinRequest:
                    OnJoinRequested?.Invoke(this, message as JoinRequestMessage);
                break;

                case MessageTypes.ConnectionEstablished:
                    OnConnectionEstablished?.Invoke(this, message as ConnectionEstablishedMessage);
                break;

                case MessageTypes.ConnectionFailed:
                    OnConnectionFailed?.Invoke(this, message as ConnectionFailedMessage);
                break;

                default:
                    Logger.Error($"Message was queued with no appropriate handle! {message.Type}");
                break;
            }
        }
        
        public void Respond(JoinRequestMessage request, bool acceptRequest)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Discord IPC Client");
            }

            if (connection == null)
            {
                throw new ObjectDisposedException("Connection", "Cannot initialize as the connection has been deinitialized");
            }

            if (!IsInitialized)
            {
                throw new UninitializedException();
            }

            connection.EnqueueCommand(new RespondCommand()
            {
                Accept = acceptRequest, 
                UserID = request.User.ID.ToString()
            });
        }
        
        public void SetPresence(RichPresence presence)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Discord IPC Client");
            }

            if (connection == null)
            {
                throw new ObjectDisposedException("Connection", "Cannot initialize as the connection has been deinitialized");
            }

            if (!IsInitialized)
            {
                Logger.Warning("The client is not yet initialized, storing the presence as a state instead.");
            }

            if (!presence)
            {
                if (!SkipIdenticalPresence || CurrentPresence != null)
                {
                    connection.EnqueueCommand(new PresenceCommand()
                    {
                        PID = ProcessID, 
                        Presence = null
                    });
                }
            }
            else
            {
                if (presence.HasSecrets() && !HasRegisteredUriScheme)
                {
                    throw new BadPresenceException("Cannot send a presence with secrets as this object has not registered a URI scheme. Please enable the uri scheme registration in the DiscordRpcClient constructor.");
                }

                if (presence.HasParty() && presence.Party.Max < presence.Party.Size)
                {
                    throw new BadPresenceException("Presence maximum party size cannot be smaller than the current size.");
                }
                
                if (presence.HasSecrets() && !presence.HasParty())
                {
                    Logger.Warning("The presence has set the secrets but no buttons will show as there is no party available.");
                }

                if (!SkipIdenticalPresence || !presence.Matches(CurrentPresence))
                {
                    connection.EnqueueCommand(new PresenceCommand()
                    {
                        PID = ProcessID, 
                        Presence = presence.Clone()
                    });
                }
            }
            
            lock (_sync) { CurrentPresence = presence != null ? presence.Clone() : null; }
        }
        
        public RichPresence UpdateDetails(string details = null)
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }
            
            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            presence.Details = details;
            SetPresence(presence);
            
            return presence;
        }
        
        public RichPresence UpdateState(string state = null)
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }
            
            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            presence.State = state;
            SetPresence(presence);
            
            return presence;
        }
        
        public RichPresence UpdateParty(Party party = null)
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }
            
            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            presence.Party = party;
            SetPresence(presence);
            
            return presence;
        }

        public RichPresence UpdatePartySize(int size)
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }

            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            if (presence.Party == null)
            {
                throw new BadPresenceException("Cannot set the size of the party if the party does not exist");
            }
            
            presence.Party.Size = size;
            SetPresence(presence);
            
            return presence;
        }
        
        public RichPresence UpdatePartySize(int size, int max)
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }
            
            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            if (presence.Party == null)
            {
                throw new BadPresenceException("Cannot set the size of the party if the party does not exist");
            }
            
            presence.Party.Size = size;
            presence.Party.Max = max;
            SetPresence(presence);
            
            return presence;
        }
        
        public RichPresence UpdateLargeAsset(string key = null, string tooltip = null)
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }
            
            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            if (presence.Assets == null) presence.Assets = new Assets();
            presence.Assets.LargeImageKey = key ?? presence.Assets.LargeImageKey;
            presence.Assets.LargeImageText = tooltip ?? presence.Assets.LargeImageText;
            SetPresence(presence);
            
            return presence;
        }
        
        public RichPresence UpdateSmallAsset(string key = null, string tooltip = null)
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }
            
            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            if (presence.Assets == null) presence.Assets = new Assets();
            presence.Assets.SmallImageKey = key ?? presence.Assets.SmallImageKey;
            presence.Assets.SmallImageText = tooltip ?? presence.Assets.SmallImageText;
            SetPresence(presence);
            
            return presence;
        }
        
        public RichPresence UpdateSecrets(Secrets secrets = null)
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }

            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            presence.Secrets = secrets;
            SetPresence(presence);
            
            return presence;
        }
        
        public RichPresence UpdateStartTime() => UpdateStartTime(DateTime.UtcNow);

        public RichPresence UpdateTimestamps(Timestamps timestamps)
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }
            
            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            if (presence.Timestamps == null) presence.Timestamps = new Timestamps();
            presence.Timestamps = timestamps;
            SetPresence(presence);
            
            return presence; 
        }
        
        public RichPresence UpdateStartTime(DateTime time)
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }
            
            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            if (presence.Timestamps == null) presence.Timestamps = new Timestamps();
            presence.Timestamps.Start = time;
            SetPresence(presence);
            
            return presence;
        }
        
        public RichPresence UpdateEndTime() => UpdateEndTime(DateTime.UtcNow);
        
        public RichPresence UpdateEndTime(DateTime time)
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }

            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            if (presence.Timestamps == null) presence.Timestamps = new Timestamps();
            presence.Timestamps.End = time;
            SetPresence(presence);
            
            return presence;
        }
        
        public RichPresence UpdateClearTime()
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }
            
            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            presence.Timestamps = null;
            SetPresence(presence);
            
            return presence;
        }
        
        public void ClearPresence()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Discord IPC Client");
            }

            if (!IsInitialized)
            {
                throw new UninitializedException();
            }

            if (connection == null)
            {
                throw new ObjectDisposedException("Connection", "Cannot initialize as the connection has been deinitialized");
            }

            SetPresence(null);
        }

        public RichPresence UpdateButtons(Button[] button = null)
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }

            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }

            presence.Buttons = button;
            SetPresence(presence);

            return presence;
        }

        public RichPresence UpdateButtons(Button[] button, int buttonId)
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }

            var buttonIndex = buttonId - 1;
            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            presence.Buttons[buttonIndex] = button[buttonIndex];
            SetPresence(presence);

            return presence;
        }

        public RichPresence RemoveLargeAsset()
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }
            
            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            if (presence.Assets == null) presence.Assets = new Assets();
            presence.Assets.LargeImageKey = null;
            presence.Assets.LargeImageText = null;
            SetPresence(presence);
            
            return presence;
        }
        
        public RichPresence RemoveSmallAsset()
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }
            
            RichPresence presence;
            lock (_sync)
            {
                if (CurrentPresence == null)
                {
                    presence = new RichPresence();
                }
                else
                {
                    presence = CurrentPresence.Clone();
                }
            }
            
            if (presence.Assets == null) presence.Assets = new Assets();
            presence.Assets.LargeImageKey = null;
            presence.Assets.LargeImageText = null;
            SetPresence(presence);
            
            return presence;
        }
        
        public bool RegisterUriScheme(string steamAppID = null, string executable = null)
        {
            var urischeme = new UriSchemeRegister(_logger, ApplicationID, steamAppID, executable);
            return HasRegisteredUriScheme = urischeme.RegisterUriScheme();
        }
        
        public void Subscribe(EventTypes type) { SetSubscription(Subscription | type); }
        
        public void Unsubscribe(EventTypes type) { SetSubscription(Subscription & ~type); }
        
        public void SetSubscription(EventTypes type)
        {
            if (IsInitialized)
            {
                SubscribeToTypes(Subscription & ~type, true);
                SubscribeToTypes(~Subscription & type, false);
            }
            else
            {
                Logger.Warning("Client has not yet initialized, but events are being subscribed too. Storing them as state instead.");
            }

            lock (_sync)
            {
                Subscription = type;
            }
        }
        
        private void SubscribeToTypes(EventTypes type, bool isUnsubscribe)
        {
            if (type == EventTypes.None) return;
            
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Discord IPC Client");
            }

            if (!IsInitialized)
            {
                throw new UninitializedException();
            }

            if (connection == null)
            {
                throw new ObjectDisposedException("Connection", "Cannot initialize as the connection has been deinitialized");
            }
            
            if (!HasRegisteredUriScheme)
            {
                throw new InvalidConfigurationException("Cannot subscribe/unsubscribe to an event as this application has not registered a URI Scheme. Call RegisterUriScheme().");
            }
            
            if ((type & EventTypes.Spectate) == EventTypes.Spectate)
            {
                connection.EnqueueCommand(new SubscribeCommand()
                {
                    Event = RPC.Payload.ServerEvent.ActivitySpectate, 
                    IsUnsubscribe = isUnsubscribe
                });
            }

            if ((type & EventTypes.Join) == EventTypes.Join)
            {
                connection.EnqueueCommand(new SubscribeCommand()
                {
                    Event = RPC.Payload.ServerEvent.ActivityJoin, 
                    IsUnsubscribe = isUnsubscribe
                });
            }

            if ((type & EventTypes.JoinRequest) == EventTypes.JoinRequest)
            {
                connection.EnqueueCommand(new SubscribeCommand()
                {
                    Event = RPC.Payload.ServerEvent.ActivityJoinRequest, 
                    IsUnsubscribe = isUnsubscribe
                });
            }
        }
        
        public void SynchronizeState()
        {
            if (!IsInitialized)
            {
                throw new UninitializedException();
            }

            SetPresence(CurrentPresence);
            if (HasRegisteredUriScheme)
            {
                SubscribeToTypes(Subscription, false);
            }
        }
        
        public bool Initialize()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Discord IPC Client");
            }

            if (IsInitialized)
            {
                throw new UninitializedException("Cannot initialize a client that is already initialized");
            }

            if (connection == null)
            {
                throw new ObjectDisposedException("Connection", "Cannot initialize as the connection has been deinitialized");
            }

            return IsInitialized = connection.AttemptConnection();
        }
        
        public void Deinitialize()
        {
            if (!IsInitialized)
            {
                throw new UninitializedException("Cannot deinitialize a client that has not been initalized.");
            }

            connection.Close();
            IsInitialized = false;
        }
        
        public void Dispose()
        {
            if (IsDisposed) return;
            if (IsInitialized) Deinitialize();
            IsDisposed = true;
        }

    }
}