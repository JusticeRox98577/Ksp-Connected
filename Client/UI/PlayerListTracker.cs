using System.Collections.Generic;
using KspConnected.Shared.Messages;

namespace KspConnected.Client.UI
{
    public class ConnectedPlayer
    {
        public int    PlayerId;
        public string PlayerName;
        public string VesselName;
        public string BodyName;
    }

    public class PlayerListTracker
    {
        private readonly List<ConnectedPlayer> _players = new List<ConnectedPlayer>();
        private readonly object _lock = new object();

        public void Update(PlayerListMessage msg)
        {
            lock (_lock)
            {
                _players.Clear();
                foreach (var entry in msg.Players)
                {
                    _players.Add(new ConnectedPlayer
                    {
                        PlayerId   = entry.PlayerId,
                        PlayerName = entry.PlayerName,
                        VesselName = entry.VesselName,
                        BodyName   = entry.BodyName,
                    });
                }
            }
        }

        public void Clear()
        {
            lock (_lock) _players.Clear();
        }

        public List<ConnectedPlayer> GetSnapshot()
        {
            lock (_lock)
                return new List<ConnectedPlayer>(_players);
        }

        public int Count
        {
            get { lock (_lock) return _players.Count; }
        }
    }
}
