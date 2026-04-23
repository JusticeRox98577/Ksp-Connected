using System;
using System.Threading;
using System.Threading.Tasks;
using KspConnected.Server.Config;
using KspConnected.Server.Core;

namespace KspConnected.Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("KSP-Connected Server");
            Console.WriteLine("====================");

            var config = ServerConfig.Load();

            // Override from CLI: --port 7654 --max-players 8 --relay
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--port":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int port))
                            config.Port = port;
                        break;
                    case "--max-players":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int mp))
                            config.MaxPlayers = mp;
                        break;
                    case "--name":
                        if (i + 1 < args.Length) config.ServerName = args[++i];
                        break;
                    case "--relay":
                        config.RelayMode = true;
                        break;
                }
            }

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n[Server] Shutdown requested…");
                cts.Cancel();
            };

            var server = new KspServer(config);
            await server.RunAsync(cts.Token);
        }
    }
}
