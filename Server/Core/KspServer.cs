using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using KspConnected.Server.Config;
using KspConnected.Shared.Protocol;

namespace KspConnected.Server.Core
{
    public class KspServer
    {
        public  ServerConfig      Config     { get; }
        public  PlayerRegistry    Registry   { get; } = new PlayerRegistry();
        public  RelayRoomManager  RelayRooms { get; } = new RelayRoomManager();

        private TcpListener       _listener;
        private CancellationToken _cancel;

        public KspServer(ServerConfig config) => Config = config;

        /// <summary>
        /// Returns the PlayerRegistry responsible for this session.
        /// In relay mode each room has its own registry; direct mode uses the global one.
        /// </summary>
        public PlayerRegistry GetRegistry(ClientSession session)
        {
            if (Config.RelayMode && !string.IsNullOrEmpty(session.RoomCode))
            {
                var reg = RelayRooms.GetRoomRegistry(session.RoomCode);
                if (reg != null) return reg;
            }
            return Registry;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _cancel = cancellationToken;
            _listener = new TcpListener(IPAddress.Any, Config.Port);
            _listener.Start();

            string mode = Config.RelayMode
                ? "RELAY — clients connect with a room code, no port forwarding needed"
                : "direct";
            Console.WriteLine($"[Server] '{Config.ServerName}' [{mode}] on port {Config.Port}");
            Console.WriteLine($"[Server] Max players: {Config.MaxPlayers}");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient tcp = await _listener.AcceptTcpClientAsync();
                    tcp.NoDelay = true;

                    // In relay mode there is no global cap; each room enforces its own limit
                    if (!Config.RelayMode && Registry.Count >= Config.MaxPlayers)
                    {
                        Console.WriteLine("[Server] Server full — rejecting connection.");
                        tcp.Close();
                        continue;
                    }

                    new ClientSession(tcp, this).Start();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[Server] Accept loop error: " + ex.Message);
            }
            finally
            {
                _listener.Stop();
                Console.WriteLine("[Server] Shut down.");
            }
        }

        public void OnSessionClosed(ClientSession session, string reason)
        {
            if (session.PlayerId < 0) return;

            var reg = GetRegistry(session);
            reg.Remove(session.PlayerId, out _);

            Console.WriteLine($"[Server] Player '{session.PlayerName}' (#{session.PlayerId}) left. Reason: {reason}");

            var listPayload = reg.BuildPlayerList().ToPayload();
            reg.Broadcast(MessageType.PlayerList, listPayload);

            if (Config.RelayMode && !string.IsNullOrEmpty(session.RoomCode) && reg.Count == 0)
                RelayRooms.CloseRoom(session.RoomCode);
        }
    }
}
