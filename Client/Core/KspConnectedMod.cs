using KspConnected.Client.Sync;
using KspConnected.Client.Time;
using KspConnected.Client.UI;
using KspConnected.Client.Util;
using KspConnected.Shared.Messages;
using UnityEngine;

namespace KspConnected.Client.Core
{
    /// <summary>
    /// Persistent singleton that owns the ConnectionManager and all top-level
    /// feature managers. Created once at game start, lives until the game exits.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class KspConnectedMod : MonoBehaviour
    {
        public static KspConnectedMod Instance { get; private set; }

        public ConnectionManager   Connection    { get; private set; }
        public RemoteVesselStore   VesselStore   { get; private set; }
        public TimeSyncManager     TimeSync      { get; private set; }
        public ChatManager         Chat          { get; private set; }
        public PlayerListTracker   Players       { get; private set; }

        public string PlayerName { get; set; } = "Jebediah";

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Connection  = new ConnectionManager();
            VesselStore = new RemoteVesselStore();
            TimeSync    = new TimeSyncManager(Connection);
            Chat        = new ChatManager(Connection);
            Players     = new PlayerListTracker();

            WireEvents();
            KspLog.Log("KSP-Connected initialised.");
        }

        private void WireEvents()
        {
            Connection.OnHelloAck      += OnHelloAck;
            Connection.OnPlayerList    += OnPlayerList;
            Connection.OnVesselUpdate  += OnVesselUpdate;
            Connection.OnVesselConfig  += OnVesselConfig;
            Connection.OnChat          += OnChat;
            Connection.OnTimeSyncReply += OnTimeSyncReply;
            Connection.OnDisconnected  += OnDisconnected;
        }

        private void Update()
        {
            if (Connection.State == ConnectionState.Connected)
            {
                TimeSync.Update();
            }
        }

        private void OnDestroy()
        {
            Connection?.Disconnect("Game exiting");
        }

        // --- event handlers (main thread) ---

        private void OnHelloAck(HelloAckMessage msg)
        {
            if (!msg.Accepted)
            {
                KspLog.Warn("Server rejected connection: " + msg.RejectReason);
                Connection.Disconnect(msg.RejectReason);
                return;
            }
            KspLog.Log($"Connected to '{msg.ServerName}' as player #{msg.AssignedPlayerId}");
        }

        private void OnPlayerList(PlayerListMessage msg)  => Players.Update(msg);
        private void OnVesselConfig(VesselConfigMessage msg) =>
            Sync.GhostVesselManager.Instance?.OnVesselConfig(msg);

        private void OnVesselUpdate(VesselUpdateMessage msg)
        {
            VesselStore.Update(msg.State);
            Sync.GhostVesselManager.Instance?.OnVesselUpdate(msg.State);
        }
        private void OnChat(ChatMessage msg)              => Chat.Receive(msg);
        private void OnTimeSyncReply(TimeSyncReplyMessage msg) => TimeSync.HandleReply(msg);
        private void OnDisconnected(string reason)
        {
            VesselStore.Clear();
            Players.Clear();
            Sync.GhostVesselManager.Instance?.RemoveAll();
            KspLog.Log("Disconnected: " + reason);
        }

        // --- public API for UI ---

        public void ConnectToServer(string host, int port) =>
            Connection.Connect(host, port, PlayerName);

        public void DisconnectFromServer() =>
            Connection.Disconnect("User disconnected");
    }
}
