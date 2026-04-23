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

            // Override from CLI: --port 7654 --max-players 8
            for (int i = 0; i < args.Length - 1; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--port":
                        if (int.TryParse(args[i + 1], out int port)) config.Port = port;
                        break;
                    case "--max-players":
                        if (int.TryParse(args[i + 1], out int mp)) config.MaxPlayers = mp;
                        break;
                    case "--name":
                        config.ServerName = args[i + 1];
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
