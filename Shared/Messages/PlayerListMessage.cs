using System.Collections.Generic;
using System.IO;
using KspConnected.Shared.Protocol;

namespace KspConnected.Shared.Messages
{
    public class PlayerEntry
    {
        public int    PlayerId;
        public string PlayerName = "";
        public string VesselName = "";
        public string BodyName   = "";
    }

    public class PlayerListMessage
    {
        public List<PlayerEntry> Players = new List<PlayerEntry>();

        public byte[] ToPayload() => MessageFrame.BuildPayload(w =>
        {
            w.Write(Players.Count);
            foreach (var p in Players)
            {
                w.Write(p.PlayerId);
                w.Write(p.PlayerName);
                w.Write(p.VesselName);
                w.Write(p.BodyName);
            }
        });

        public static PlayerListMessage FromPayload(byte[] payload) =>
            MessageFrame.ParsePayload(payload, r =>
            {
                var msg = new PlayerListMessage();
                int count = r.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    msg.Players.Add(new PlayerEntry
                    {
                        PlayerId   = r.ReadInt32(),
                        PlayerName = r.ReadString(),
                        VesselName = r.ReadString(),
                        BodyName   = r.ReadString(),
                    });
                }
                return msg;
            });
    }
}
