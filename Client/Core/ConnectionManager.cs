using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using KspConnected.Client.Util;
using KspConnected.Shared.Messages;
using KspConnected.Shared.Protocol;

namespace KspConnected.Client.Core
{
    public enum ConnectionState { Disconnected, Connecting, Connected }

    public class ConnectionManager
    {
        public ConnectionState State         { get; private set; } = ConnectionState.Disconnected;
        public int             LocalPlayerId { get; private set; } = -1;
        public string          RoomCode      { get; private set; } = "";

        // Inbound message events — fired on Unity main thread via ThreadDispatcher
        public event Action<HelloAckMessage>      OnHelloAck;
        public event Action<PlayerListMessage>    OnPlayerList;
        public event Action<VesselUpdateMessage>  OnVesselUpdate;
        public event Action<VesselConfigMessage>  OnVesselConfig;
        public event Action<ChatMessage>          OnChat;
        public event Action<TimeSyncReplyMessage> OnTimeSyncReply;
        public event Action<string>               OnDisconnected;
        public event Action<string>               OnRoomCreated;   // fires with room code on relay create

        private TcpClient     _tcp;
        private NetworkStream _stream;
        private Thread        _readThread;
        private readonly object _writeLock = new object();

        /// <summary>Connect directly (host must have port forwarded).</summary>
        public void Connect(string host, int port, string playerName)
            => ConnectInternal(host, port, playerName, isRelayHost: false, roomCode: "");

        /// <summary>Connect to a relay server and create a room. Friends use the returned code to join.</summary>
        public void CreateRelayRoom(string relayHost, int port, string playerName)
            => ConnectInternal(relayHost, port, playerName, isRelayHost: true, roomCode: "");

        /// <summary>Connect to a relay server and join a room by its 6-character code.</summary>
        public void JoinRelayRoom(string relayHost, int port, string roomCode, string playerName)
            => ConnectInternal(relayHost, port, playerName, isRelayHost: false, roomCode: roomCode);

        private void ConnectInternal(string host, int port, string playerName,
                                     bool isRelayHost, string roomCode)
        {
            if (State != ConnectionState.Disconnected)
            {
                KspLog.Warn("Already connecting/connected.");
                return;
            }
            State = ConnectionState.Connecting;
            KspLog.Log($"Connecting to {host}:{port} as '{playerName}'…");

            var thread = new Thread(() =>
            {
                try
                {
                    KspLog.Log($"[Connect] Creating TcpClient for {host}:{port}");
                    _tcp = new TcpClient { NoDelay = true };

                    // 5-second connect timeout
                    var connectTask = _tcp.ConnectAsync(host, port);
                    if (!connectTask.Wait(TimeSpan.FromSeconds(5)))
                        throw new TimeoutException($"Timed out connecting to {host}:{port}");
                    if (connectTask.IsFaulted)
                        throw connectTask.Exception?.InnerException ?? connectTask.Exception;

                    KspLog.Log("[Connect] TCP connected. Sending Hello.");
                    _stream = _tcp.GetStream();
                    State   = ConnectionState.Connected;

                    SendPayload(MessageType.Hello, new HelloMessage
                    {
                        PlayerName  = playerName,
                        IsRelayHost = isRelayHost,
                        RoomCode    = roomCode ?? "",
                    }.ToPayload());

                    KspLog.Log("[Connect] Hello sent. Starting read loop.");
                    _readThread = new Thread(ReadLoop) { IsBackground = true, Name = "KspConnected-Recv" };
                    _readThread.Start();
                }
                catch (Exception ex)
                {
                    State = ConnectionState.Disconnected;
                    KspLog.Error("[Connect] FAILED: " + ex.Message);
                    ThreadDispatcher.Instance?.Enqueue(
                        () => OnDisconnected?.Invoke("Connect failed: " + ex.Message));
                }
            })
            { IsBackground = true, Name = "KspConnected-Connect" };
            thread.Start();
        }

        public void Disconnect(string reason = "User disconnected")
        {
            if (State == ConnectionState.Disconnected) return;
            try
            {
                SendPayload(MessageType.Disconnect, new DisconnectMessage { Reason = reason }.ToPayload());
            }
            catch { /* ignore send errors during disconnect */ }
            CloseSocket(reason);
        }

        public void SendVesselUpdate(VesselUpdateMessage msg) =>
            SendPayload(MessageType.VesselUpdate, msg.ToPayload());

        public void SendVesselConfig(VesselConfigMessage msg) =>
            SendPayload(MessageType.VesselConfig, msg.ToPayload());

        public void SendChat(ChatMessage msg) =>
            SendPayload(MessageType.Chat, msg.ToPayload());

        public void SendTimeSync(TimeSyncMessage msg) =>
            SendPayload(MessageType.TimeSync, msg.ToPayload());

        public void SendPing() =>
            SendPayload(MessageType.Ping, Array.Empty<byte>());

        private void SendPayload(MessageType type, byte[] payload)
        {
            if (State == ConnectionState.Disconnected || _stream == null) return;
            try
            {
                lock (_writeLock)
                    MessageFrame.WriteFrame(_stream, type, payload);
            }
            catch (Exception ex)
            {
                KspLog.Error("Send error: " + ex.Message);
                CloseSocket("Send error: " + ex.Message);
            }
        }

        private void ReadLoop()
        {
            try
            {
                while (State == ConnectionState.Connected)
                {
                    var (type, payload) = MessageFrame.ReadFrame(_stream);
                    DispatchMessage(type, payload);
                }
            }
            catch (Exception ex) when (State == ConnectionState.Connected)
            {
                KspLog.Error("Read error: " + ex.Message);
                CloseSocket("Connection lost: " + ex.Message);
            }
        }

        private void DispatchMessage(MessageType type, byte[] payload)
        {
            switch (type)
            {
                case MessageType.HelloAck:
                    var ack = HelloAckMessage.FromPayload(payload);
                    if (ack.Accepted)
                    {
                        LocalPlayerId = ack.AssignedPlayerId;
                        RoomCode = ack.RoomCode ?? "";
                        if (!string.IsNullOrEmpty(RoomCode))
                            ThreadDispatcher.Instance.Enqueue(() => OnRoomCreated?.Invoke(RoomCode));
                    }
                    ThreadDispatcher.Instance.Enqueue(() => OnHelloAck?.Invoke(ack));
                    break;

                case MessageType.PlayerList:
                    var pl = PlayerListMessage.FromPayload(payload);
                    ThreadDispatcher.Instance.Enqueue(() => OnPlayerList?.Invoke(pl));
                    break;

                case MessageType.VesselUpdate:
                    var vu = VesselUpdateMessage.FromPayload(payload);
                    ThreadDispatcher.Instance.Enqueue(() => OnVesselUpdate?.Invoke(vu));
                    break;

                case MessageType.VesselConfig:
                    var vc = VesselConfigMessage.FromPayload(payload);
                    ThreadDispatcher.Instance.Enqueue(() => OnVesselConfig?.Invoke(vc));
                    break;

                case MessageType.Chat:
                    var chat = ChatMessage.FromPayload(payload);
                    ThreadDispatcher.Instance.Enqueue(() => OnChat?.Invoke(chat));
                    break;

                case MessageType.TimeSyncReply:
                    var tsr = TimeSyncReplyMessage.FromPayload(payload);
                    ThreadDispatcher.Instance.Enqueue(() => OnTimeSyncReply?.Invoke(tsr));
                    break;

                case MessageType.Disconnect:
                    var disc = DisconnectMessage.FromPayload(payload);
                    CloseSocket("Server: " + disc.Reason);
                    break;

                case MessageType.Pong:
                    break; // heartbeat reply — nothing to do

                default:
                    KspLog.Warn($"Unknown message type: 0x{(byte)type:X2}");
                    break;
            }
        }

        private void CloseSocket(string reason)
        {
            State = ConnectionState.Disconnected;
            LocalPlayerId = -1;
            RoomCode = "";
            try { _stream?.Close(); } catch { }
            try { _tcp?.Close(); }    catch { }
            _stream = null;
            _tcp    = null;
            KspLog.Log("Disconnected: " + reason);
            ThreadDispatcher.Instance?.Enqueue(() => OnDisconnected?.Invoke(reason));
        }
    }
}
