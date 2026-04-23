using System;
using System.IO;
using System.Text.Json;

namespace KspConnected.Server.Config
{
    public class ServerConfig
    {
        public int    Port           { get; set; } = 7654;
        public int    MaxPlayers     { get; set; } = 16;
        public string ServerName     { get; set; } = "KSP-Connected Server";
        public string WelcomeMessage { get; set; } = "Welcome to KSP-Connected!";
        public bool   RelayMode      { get; set; } = false;

        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };

        public static ServerConfig Load(string path = "server.json")
        {
            if (!File.Exists(path)) return new ServerConfig();
            try
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<ServerConfig>(json, JsonOpts) ?? new ServerConfig();
            }
            catch
            {
                Console.Error.WriteLine($"[Config] Failed to parse {path}; using defaults.");
                return new ServerConfig();
            }
        }

        public void Save(string path = "server.json")
        {
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOpts));
        }
    }
}
