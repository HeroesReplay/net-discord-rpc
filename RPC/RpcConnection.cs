using System;
using System.Collections.Generic;
using System.Threading;
using NetDiscordRpc.Core.IO;
using NetDiscordRpc.Core.Converters;
using NetDiscordRpc.Core.Helpers;
using NetDiscordRpc.Core.Logger;
using NetDiscordRpc.Events;
using NetDiscordRpc.Message;
using NetDiscordRpc.Message.Messages;
using NetDiscordRpc.RPC.Commands;
using NetDiscordRpc.RPC.Payload;
using Newtonsoft.Json;

namespace NetDiscordRpc.RPC
{
    internal class RpcConnection : IDisposable
	{
		public const int VERSION = 1;
		public const int POLL_RATE = 1000;
		private const bool CLEAR_ON_SHUTDOWN = true;
		private const bool LOCK_STEP = false;
		
		public IConsoleLogger Logger
		{
			get => _logger;
			set
			{
				_logger = value;
				if (namedPipe != null) namedPipe.Logger = value;
			}
		}
		private IConsoleLogger _logger;
		
        public event OnRpcMessageEvent OnRpcMessage;

        public RpcStates State
        {
	        get { var tmp = RpcStates.Disconnected; lock (l_states) tmp = _state; return tmp; }
        }
		private RpcStates _state;
		private readonly object l_states = new();

		public Configuration Configuration
		{
			get { Configuration tmp = null; lock (l_config) tmp = _configuration; return tmp; }
		}
		private Configuration _configuration;
		private readonly object l_config = new();

		private volatile bool aborting;
		private volatile bool shutdown;
		
		public bool IsRunning => thread != null;
		
		public bool ShutdownOnly { get; set; }


		private string applicationID;
		private int processID;

		private long nonce;

		private Thread thread;
		private INamedPipeClient namedPipe;

		private int targetPipe;
		private readonly object l_rtqueue = new();
        private readonly uint _maxRtQueueSize;
		private Queue<ICommand> _rtqueue;

		private readonly object l_rxqueue;
        private readonly uint _maxRxQueueSize;
        private Queue<IMessage> _rxqueue;

		private AutoResetEvent queueUpdatedEvent = new(false);
		private BackoffDelay delay;
		
		public RpcConnection(string applicationID, int processID, int targetPipe, INamedPipeClient client, uint maxRxQueueSize = 128, uint maxRtQueueSize = 512)
		{
			this.applicationID = applicationID;
			this.processID = processID;
			this.targetPipe = targetPipe;
			namedPipe = client;
			ShutdownOnly = true;
			
			Logger = new ConsoleLogger();

			delay = new BackoffDelay(500, 60 * 1000);
            _maxRtQueueSize = maxRtQueueSize;
			_rtqueue = new Queue<ICommand>((int)_maxRtQueueSize + 1);

            _maxRxQueueSize = maxRxQueueSize;
            _rxqueue = new Queue<IMessage>((int)_maxRxQueueSize + 1);
			
			nonce = 0;
		}
		
		private long GetNextNonce() => nonce += 1;

		internal void EnqueueCommand(ICommand command)
        {
            Logger.Trace($"Enqueue Command: {command.GetType().FullName}");
            
            if (aborting || shutdown) return;
            
			lock (l_rtqueue)
            {
	            if (_rtqueue.Count == _maxRtQueueSize)
                {
                    Logger.Error("Too many enqueued commands, dropping oldest one. Maybe you are pushing new presences to fast?");
                    _rtqueue.Dequeue();
                }
	            
                _rtqueue.Enqueue(command);
            }
		}
		
		private void EnqueueMessage(IMessage message)
		{
			try
            {
	            OnRpcMessage?.Invoke(this, message);
            }
            catch (Exception e)
            {
                Logger.Error($"Unhandled Exception while processing event: {e.GetType().FullName}");
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
            }
            
            if (_maxRxQueueSize <= 0)
            {
                Logger.Trace("Enqueued Message, but queue size is 0.");
                return;
            }
            
            Logger.Trace($"Enqueue Message: {message.Type}");
            lock (l_rxqueue)
            {
	            if (_rxqueue.Count == _maxRxQueueSize)
                {
                    Logger.Warning("Too many enqueued messages, dropping oldest one.");
                    _rxqueue.Dequeue();
                }
	            
                _rxqueue.Enqueue(message);
            }
		}
		
		internal IMessage DequeueMessage()
        {
	        lock (l_rxqueue)
	        {
		        return _rxqueue.Count == 0 ? null : _rxqueue.Dequeue();
	        }
		}
		
		internal IMessage[] DequeueMessages()
        {
	        lock (l_rxqueue)
			{
				var messages = _rxqueue.ToArray();
				
				_rxqueue.Clear();
				
				return messages;
			}
		}
		
		private void MainLoop()
		{
			Logger.Info("RPC Connection Started");
            if (Logger != null)
            {
	            Logger.Trace("============================");
	            Logger.Trace($"Assembly:             {System.Reflection.Assembly.GetAssembly(typeof(RichPresence)).FullName}");
	            Logger.Trace($"Pipe:                 {namedPipe.GetType().FullName}");
	            Logger.Trace($"Platform:             {Environment.OSVersion}");
	            Logger.Trace($"applicationID:        {applicationID}");
	            Logger.Trace($"targetPipe:           {targetPipe}");
	            Logger.Trace($"POLL_RATE:            {POLL_RATE}");
	            Logger.Trace($"_maxRtQueueSize:      {_maxRtQueueSize}");
	            Logger.Trace($"_maxRxQueueSize:      {_maxRxQueueSize}");
	            Logger.Trace("============================");
            }
            
            while (!aborting && !shutdown)
			{
				try
				{
					if (namedPipe == null)
					{
						Logger.Error("Something bad has happened with our pipe client!");
						aborting = true;
						return;
					}
					
					Logger.Trace($"Connecting to the pipe through the {namedPipe.GetType().FullName}");
					if (namedPipe.Connect(targetPipe))
					{
						Logger.Trace("Connected to the pipe. Attempting to establish handshake...");
						EnqueueMessage(new ConnectionEstablishedMessage()
						{
							ConnectedPipe = namedPipe.ConnectedPipe
						});
						
						EstablishHandshake();
						Logger.Trace("Connection Established. Starting reading loop...");
						
						PipeFrame frame;
						var mainloop = true;
						while (mainloop && !aborting && !shutdown && namedPipe.IsConnected)
						{
							if (namedPipe.ReadFrame(out frame))
							{
								Logger.Trace($"Read Payload: {frame.Opcode}");
								
								switch (frame.Opcode)
								{
									case OpCode.Close:

										var close = frame.GetObject<ClosePayload>();
										Logger.Warning($"We have been told to terminate by discord: ({close.Code}) {close.Reason}");
										
										EnqueueMessage(new CloseMessage()
										{
											Code = close.Code, 
											Reason = close.Reason
										});
										mainloop = false;
									break;
									
									case OpCode.Ping:					
										Logger.Trace("PING");
										frame.Opcode = OpCode.Pong;
										namedPipe.WriteFrame(frame);
									break;
									
									case OpCode.Pong:															
										Logger.Trace("PONG");
									break;
									
									case OpCode.Frame:					
										if (shutdown)
										{
											Logger.Warning("Skipping frame because we are shutting down.");
											break;
										}

										if (frame.Data == null)
										{
											Logger.Error("We received no data from the frame so we cannot get the event payload!");
											break;
										}
										
										EventPayload response = null;
										try { response = frame.GetObject<EventPayload>(); } catch (Exception e)
										{
											Logger.Error($"Failed to parse event! {e.Message}");
											Logger.Error($"Data: {frame.Message}");
										}


										try { if (response != null) ProcessFrame(response); } catch(Exception e)
                                        {
											Logger.Error($"Failed to process event! {e.Message}");
											Logger.Error($"Data: {frame.Message}");
										}

									break;
									
									default:
									case OpCode.Handshake:
										Logger.Error($"Invalid opcode: {frame.Opcode}");
										mainloop = false;
									break;
								}
							}

							if (aborting || !namedPipe.IsConnected) continue;
							
							ProcessCommandQueue();
								
							queueUpdatedEvent.WaitOne(POLL_RATE);
						}

						Logger.Trace($"Left main read loop for some reason. Aborting: {aborting}, Shutting Down: {shutdown}");
					}
					else
					{
						Logger.Error("Failed to connect for some reason.");
						EnqueueMessage(new ConnectionFailedMessage()
						{
							FailedPipe = targetPipe
						});
					}
					
					if (aborting || shutdown) continue;
					
					long sleep = delay.NextDelay();

					Logger.Trace($"Waiting {sleep}ms before attempting to connect again");
					Thread.Sleep(delay.NextDelay());
				}
				catch (Exception e)
				{
					Logger.Error("Unhandled Exception: "+ e.GetType().FullName);
					Logger.Error(e.Message);
					Logger.Error(e.StackTrace);
				}
				finally
				{
					if (namedPipe.IsConnected)
					{
						Logger.Trace("Closing the named pipe.");
						namedPipe.Close();
					}
					
					SetConnectionState(RpcStates.Disconnected);
				}
			}
            
			Logger.Trace("Left Main Loop");
			namedPipe?.Dispose();

			Logger.Info("Thread Terminated, no longer performing RPC connection.");
		}
		
		private void ProcessFrame(EventPayload response)
		{
			Logger.Info($"Handling Response. Cmd: {response.Command}, Event: {response.Event}");
			
			if (response.Event.HasValue && response.Event.Value == ServerEvent.Error)
			{
				Logger.Error("Error received from the RPC");
				
				var err = response.GetObject<ErrorMessage>();
				Logger.Error($"Server responded with an error message: ({err.Code.ToString()}) {err.Message}");
				
				EnqueueMessage(err);
				return;
			}
			
			if (State == RpcStates.Connecting)
			{
				if (response.Command == Payload.Commands.Dispatch && response.Event.HasValue && response.Event.Value == ServerEvent.Ready)
				{
					Logger.Info("Connection established with the RPC");
					SetConnectionState(RpcStates.Connected);
					delay.Reset();
					
					var ready = response.GetObject<ReadyMessage>();
					lock (l_config)
					{
						_configuration = ready.Configuration;
						ready.User.SetConfiguration(_configuration);
					}
					
					EnqueueMessage(ready);
					return;
				}
			}

			if (State == RpcStates.Connected)
			{
				switch(response.Command)
				{
					case Payload.Commands.Dispatch:
						ProcessDispatch(response);
					break;
					
					case Payload.Commands.SetActivity:
						if (response.Data == null)
						{
							EnqueueMessage(new PresenceMessage());
						}
						else
						{
							EnqueueMessage(new PresenceMessage(response.GetObject<RichPresenceResponse>()));
						} 
					break;

					case Payload.Commands.Unsubscribe:
					case Payload.Commands.Subscribe:
						
						var serializer = new JsonSerializer();
						serializer.Converters.Add(new EnumSnakeCaseConverter());
						
                        var evt = response.GetObject<EventPayload>().Event.Value;
						
						if (response.Command == Payload.Commands.Subscribe) EnqueueMessage(new SubscribeMessage(evt));
						else EnqueueMessage(new UnsubscribeMessage(evt));

					break;
						
					
					case Payload.Commands.SendActivityJoinInvite:
						Logger.Trace("Got invite response ack.");
					break;

					case Payload.Commands.CloseActivityJoinRequest:
						Logger.Trace("Got invite response reject ack.");
					break;
					
					default:
						Logger.Error($"Unkown frame was received! {response.Command}");
						return;
				}
				return;
			}

			Logger.Trace($"Received a frame while we are disconnected. Ignoring. Cmd: {response.Command}, Event: {response.Event}");			
		}

		private void ProcessDispatch(EventPayload response)
		{
			if (response.Command != Payload.Commands.Dispatch) return;
			
			if (!response.Event.HasValue) return;

			switch(response.Event.Value)
			{
				case ServerEvent.ActivitySpectate:
					var spectate = response.GetObject<SpectateMessage>();
					EnqueueMessage(spectate);
				break;

				case ServerEvent.ActivityJoin:
					var join = response.GetObject<JoinMessage>();
					EnqueueMessage(join);
				break;

				case ServerEvent.ActivityJoinRequest:
					var request = response.GetObject<JoinRequestMessage>();
					EnqueueMessage(request);
				break;

				case ServerEvent.Ready:
				case ServerEvent.Error:
					break;
				default:
					Logger.Warning($"Ignoring {response.Event.Value}");
				break;
			}
		}
		
		private void ProcessCommandQueue()
		{
			if (State != RpcStates.Connected) return;
			
			if (aborting)
			{
				Logger.Warning("We have been told to write a queue but we have also been aborted.");
			}

			var needsWriting = true;
			ICommand item;
			
			while (needsWriting && namedPipe.IsConnected)
			{
				lock (l_rtqueue)
				{
					needsWriting = _rtqueue.Count > 0;
					if (!needsWriting) break;	
					
					item = _rtqueue.Peek();
				}
				
				if (shutdown || (!aborting && LOCK_STEP)) needsWriting = false;
				
				var payload = item.PreparePayload(GetNextNonce());
				Logger.Trace($"Attempting to send payload: {payload.Command}");
				
				var frame = new PipeFrame();
				if (item is CloseCommand)
				{
					SendHandwave();
					
					Logger.Trace("Handwave sent, ending queue processing.");
					lock (l_rtqueue) _rtqueue.Dequeue();
					
					return;
				}

				if (aborting)
				{
					Logger.Warning("- skipping frame because of abort.");
					lock (l_rtqueue) _rtqueue.Dequeue();
				}
				else
				{
					frame.SetObject(OpCode.Frame, payload);
						
					Logger.Trace($"Sending payload: {payload.Command}");
					if (namedPipe.WriteFrame(frame))
					{
						Logger.Trace("Sent Successfully.");
						lock (l_rtqueue) _rtqueue.Dequeue();
					}
					else
					{
						Logger.Warning("Something went wrong during writing!");
						return;
					}
				}
			}
		}
		
		private void EstablishHandshake()
		{
			Logger.Trace("Attempting to establish a handshake...");
			
			if (State != RpcStates.Disconnected)
			{
				Logger.Error("State must be disconnected in order to start a handshake!");
				return;
			}
			
			Logger.Trace("Sending Handshake...");				
			if (!namedPipe.WriteFrame(new PipeFrame(OpCode.Handshake, new Handshake() { Version = VERSION, ClientID = applicationID })))
			{
				Logger.Error("Failed to write a handshake.");
				return;
			}
			
			SetConnectionState(RpcStates.Connecting);
		}
		
		private void SendHandwave()
		{
			Logger.Info("Attempting to wave goodbye...");
			
			if (State == RpcStates.Disconnected)
			{
				Logger.Error("State must NOT be disconnected in order to send a handwave!");
				return;
			}
			
			if (!namedPipe.WriteFrame(new PipeFrame(OpCode.Close, new Handshake() { Version = VERSION, ClientID = applicationID })))
			{
				Logger.Error("failed to write a handwave.");
			}
		}
		
		public bool AttemptConnection()
		{
			Logger.Info("Attempting a new connection");
			
			if (thread != null)
			{
				Logger.Error("Cannot attempt a new connection as the previous connection thread is not null!");
				return false;
			}
			
			if (State != RpcStates.Disconnected)
			{
				Logger.Warning("Cannot attempt a new connection as the previous connection hasn't changed state yet.");
				return false;
			}

			if (aborting)
			{
				Logger.Error("Cannot attempt a new connection while aborting!");
				return false;
			}
			
			thread = new Thread(MainLoop);
			thread.Name = "Discord IPC Thread";
			thread.IsBackground = true;
			thread.Start();

			return true;
		}
		
		private void SetConnectionState(RpcStates state)
		{
			Logger.Trace($"Setting the connection state to {state.ToString().ToSnakeCase().ToUpperInvariant()}");
			lock (l_states)
			{
				_state = state;
			}
		}
		
		public void Shutdown()
		{
			Logger.Trace("Initiated shutdown procedure");
			shutdown = true;
			
			lock(l_rtqueue)
			{
				_rtqueue.Clear();
				if (CLEAR_ON_SHUTDOWN) _rtqueue.Enqueue(new PresenceCommand() { PID = processID, Presence = null });
				_rtqueue.Enqueue(new CloseCommand());
			}
			
			queueUpdatedEvent.Set();
		}
		
		public void Close()
		{
			if (thread == null)
			{
				Logger.Error("Cannot close as it is not available!");
				return;
			}

			if (aborting)
			{
				Logger.Error("Cannot abort as it has already been aborted");
				return;
			}
			
			if (ShutdownOnly)
			{
				Shutdown();
				return;
			}
			
			Logger.Trace("Updating Abort State...");
			aborting = true;
			queueUpdatedEvent.Set();
		}
		
		public void Dispose()
		{
			ShutdownOnly = false;
			Close();
		}

	}
}