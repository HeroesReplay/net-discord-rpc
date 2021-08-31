using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using NetDiscordRpc.Core.Logger;

namespace NetDiscordRpc.Core.IO
{
    public sealed class ManagedNamedPipeClient : INamedPipeClient
    {
        private const string PipeName = @"discord-ipc-{0}";
        public IConsoleLogger Logger { get; set; }
        
        public bool IsConnected
        {
            get
            {
                if (_isClosed) return false;
                lock (l_stream)
                {
                    return _stream != null && _stream.IsConnected;
                }
            }
        }
        
        public int ConnectedPipe => _connectedPipe;

        private int _connectedPipe;
        private NamedPipeClientStream _stream;

        private byte[] _buffer = new byte[PipeFrame.MAX_SIZE];

        private Queue<PipeFrame> _framequeue = new();
        private object _framequeuelock = new();

        private volatile bool _isDisposed;
        private volatile bool _isClosed = true;

        private object l_stream = new();
        
        public ManagedNamedPipeClient()
        {
            _buffer = new byte[PipeFrame.MAX_SIZE];
            Logger = new ConsoleLogger();
            _stream = null;
        }
        
        public bool Connect(int pipe)
        {
            Logger.Trace($"ManagedNamedPipeClient.Connection({pipe})");

            if (_isDisposed)
            {
                throw new ObjectDisposedException("NamedPipe");
            }

            switch (pipe)
            {
                case > 9:
                    throw new ArgumentOutOfRangeException(nameof(pipe), "Argument cannot be greater than 9");
                case < 0:
                {
                    for (var i = 0; i < 10; i++)
                    {
                        if (!AttemptConnection(i) && !AttemptConnection(i, true)) continue;
                    
                        BeginReadStream();
                        return true;
                    }

                    break;
                }
                default:
                {
                    if (!AttemptConnection(pipe) && !AttemptConnection(pipe, true)) return false;
                
                    BeginReadStream();
                    return true;
                }
            }


            return false;
        }
        
        private bool AttemptConnection(int pipe, bool isSandbox = false)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("_stream");
            }
            
            var sandbox = isSandbox ? GetPipeSandbox() : "";
            if (isSandbox && sandbox == null)
            {
                Logger.Trace("Skipping sandbox connection.");
                return false;
            }
            
            Logger.Trace($"Connection Attempt {pipe} ({sandbox})");
            var pipename = GetPipeName(pipe, sandbox);

            try
            {
                lock (l_stream)
                {
                    Logger.Info($"Attempting to connect to {pipename}");
                    _stream = new NamedPipeClientStream(".", pipename, PipeDirection.InOut, PipeOptions.Asynchronous);
                    _stream.Connect(1000);
                    
                    Logger.Trace("Waiting for connection...");
                    do { Thread.Sleep(10); } while (!_stream.IsConnected);
                }
                
                Logger.Info($"Connected to {pipename}");
                _connectedPipe = pipe;
                _isClosed = false;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed connection to {pipename}. {e.Message}");
                Close();
            }

            Logger.Trace($"Done. Result: {_isClosed}");
            return !_isClosed;
        }
        
        private void BeginReadStream()
        {
            if (_isClosed) return;
            try
            {
                lock (l_stream)
                {
                    if (_stream is not { IsConnected: true }) return;

                    Logger.Trace($"Begining Read of {_buffer.Length} bytes");
                    _stream.BeginRead(_buffer, 0, _buffer.Length, EndReadStream, _stream.IsConnected);
                }
            }
            catch (ObjectDisposedException)
            {
                Logger.Warning("Attempted to start reading from a disposed pipe");
            }
            catch (InvalidOperationException)
            {
                Logger.Warning("Attempted to start reading from a closed pipe");
            }
            catch (Exception e)
            {
                Logger.Error($"An exception occured while starting to read a stream: {e.Message}");
                Logger.Error(e.StackTrace);
            }
        }
        
        private void EndReadStream(IAsyncResult callback)
        {
            Logger.Trace("Ending Read");
            int bytes;

            try
            {
                lock (l_stream)
                {
                    if (_stream is not { IsConnected: true }) return;
                    
                    bytes = _stream.EndRead(callback);
                }
            }
            catch (IOException)
            {
                Logger.Warning("Attempted to end reading from a closed pipe");
                return;
            }
            catch (NullReferenceException)
            {
                Logger.Warning("Attempted to read from a null pipe");
                return;
            }
            catch (ObjectDisposedException)
            {
                Logger.Warning("Attemped to end reading from a disposed pipe");
                return;
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("An exception occured while ending a read of a stream: {0}", e.Message));
                Logger.Error(e.StackTrace);
                return;
            }
            
            Logger.Trace($"Read {bytes} bytes");
            
            if (bytes > 0)
            {
                using (var memory = new MemoryStream(_buffer, 0, bytes))
                {
                    try
                    {
                        var frame = new PipeFrame();
                        if (frame.ReadStream(memory))
                        {
                            Logger.Trace($"Read a frame: {frame.Opcode}");
                            
                            lock (_framequeuelock) _framequeue.Enqueue(frame);
                        }
                        else
                        {
                            Logger.Error("Pipe failed to read from the data received by the stream.");
                            Close();
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"A exception has occured while trying to parse the pipe data: {e.Message}");
                        Close();
                    }
                }
            }
            else
            {
                if (IsUnix())
                {
                    Logger.Error($"Empty frame was read on {Environment.OSVersion} aborting.");
                    Close();
                }
                else
                {
                    Logger.Warning("Empty frame was read. Please send report to Lachee.");
                }
            }
            
            if (_isClosed || !IsConnected) return;
            
            Logger.Trace("Starting another read");
            BeginReadStream();
        }
        
        public bool ReadFrame(out PipeFrame frame)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("_stream");
            }
            
            lock (_framequeuelock)
            {
                if (_framequeue.Count == 0)
                {
                    frame = default;
                    return false;
                }
                
                frame = _framequeue.Dequeue();
                return true;
            }
        }
        
        public bool WriteFrame(PipeFrame frame)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("_stream");
            }
            
            if (_isClosed || !IsConnected)
            {
                Logger.Error("Failed to write frame because the stream is closed");
                return false;
            }

            try
            {
                frame.WriteStream(_stream);
                return true;
            }
            catch (IOException io)
            {
                Logger.Error($"Failed to write frame because of a IO Exception: {io.Message}");
            }
            catch (ObjectDisposedException)
            {
                Logger.Warning("Failed to write frame as the stream was already disposed");
            }
            catch (InvalidOperationException)
            {
                Logger.Warning("Failed to write frame because of a invalid operation");
            }
            
            return false;
        }
        
        public void Close()
        {
            if (_isClosed)
            {
                Logger.Warning("Tried to close a already closed pipe.");
                return;
            }
            
            try
            {
                lock (l_stream)
                {
                    if (_stream != null)
                    {
                        try
                        {
                            _stream.Flush();
                            _stream.Dispose();
                        }
                        catch (Exception e)
                        {
                            Logger.Error($"Something went wrong when closing the stream: [{e.Message}]");
                        }
                        
                        _stream = null;
                        _isClosed = true;
                    }
                    else
                    {
                        Logger.Warning("Stream was closed, but no stream was available to begin with!");
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                Logger.Warning("Tried to dispose already disposed stream");
            }
            finally
            {
                _isClosed = true;
                _connectedPipe = -1;
            }
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            if (!_isClosed) Close();
            
            lock (l_stream)
            {
                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }
            }
            
            _isDisposed = true;
        }
        
        public static string GetPipeName(int pipe, string sandbox = "")
        {
            if (!IsUnix()) return sandbox + string.Format(PipeName, pipe);
            return Path.Combine(GetTemporaryDirectory(), sandbox + string.Format(PipeName, pipe));
        }
        
        public static string GetPipeSandbox()
        {
            return Environment.OSVersion.Platform switch
            {
                PlatformID.Unix => "snap.discord/",
                _ => null
            };
        }
        
        private static string GetTemporaryDirectory()
        {
            string temp = null;
            temp = temp ?? Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            temp = temp ?? Environment.GetEnvironmentVariable("TMPDIR");
            temp = temp ?? Environment.GetEnvironmentVariable("TMP");
            temp = temp ?? Environment.GetEnvironmentVariable("TEMP");
            temp = temp ?? "/tmp";
            return temp;
        }
        
        public static bool IsUnix()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                case PlatformID.WinCE:
                case PlatformID.Xbox:
                case PlatformID.Other: 
                    return false;
                default:
                    return false;

                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    return true;
            }
        }
    }
}