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
        public  ServerConfig   Config   { get; }
        public  PlayerRegistry Registry { get; } = new PlayerRegistry();

        private TcpListener        _listener;
        private CancellationToken  _cancel;

        public KspServer(ServerConfig config) => Config = config;

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _cancel = cancellationToken;
            _listener = new TcpListener(IPAddress.Any, Config.Port);
            _listener.Start();

            Console.WriteLine($"[Server] '{Config.ServerName}' listening on port {Config.Port}");
            Console.WriteLine($"[Server] Max players: {Config.MaxPlayers}");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient tcp = await _listener.AcceptTcpClientAsync();
                    tcp.NoDelay  = true;

                    if (Registry.Count >= Config.MaxPlayers)
                    {
                        Console.WriteLine("[Server] Server full — rejecting connection.");
                        tcp.Close();
                        continue;
                    }

                    var session = new ClientSession(tcp, this);
                    session.Start();
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

            Registry.Remove(session.PlayerId, out _);
            Console.WriteLine($"[Server] Player '{session.PlayerName}' (#{session.PlayerId}) left. Reason: {reason}");

            // Broadcast updated player list
            var listPayload = Registry.BuildPlayerList().ToPayload();
            Registry.Broadcast(MessageType.PlayerList, listPayload);
        }
    }
}
