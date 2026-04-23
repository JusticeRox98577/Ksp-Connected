using System.IO;
using System.IO.Compression;
using KspConnected.Shared.Protocol;

namespace KspConnected.Shared.Messages
{
    /// <summary>
    /// Carries a full vessel ConfigNode snapshot (gzip-compressed) so remote
    /// clients can spawn the vessel as an actual KSP object with the correct
    /// part geometry. Sent once when a player joins or changes vessel.
    /// </summary>
    public class VesselConfigMessage
    {
        public int    PlayerId;
        public string VesselId   = "";  // Guid string
        public byte[] ConfigData;       // gzip-compressed ConfigNode.ToString()

        public byte[] ToPayload() => MessageFrame.BuildPayload(w =>
        {
            w.Write(PlayerId);
            w.Write(VesselId);
            w.Write(ConfigData.Length);
            w.Write(ConfigData);
        });

        public static VesselConfigMessage FromPayload(byte[] payload) =>
            MessageFrame.ParsePayload(payload, r =>
            {
                var msg = new VesselConfigMessage
                {
                    PlayerId   = r.ReadInt32(),
                    VesselId   = r.ReadString(),
                };
                int len = r.ReadInt32();
                msg.ConfigData = r.ReadBytes(len);
                return msg;
            });

        public static byte[] Compress(string text)
        {
            byte[] raw = System.Text.Encoding.UTF8.GetBytes(text);
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, CompressionLevel.Optimal))
                    gz.Write(raw, 0, raw.Length);
                return ms.ToArray();
            }
        }

        public static string Decompress(byte[] data)
        {
            using (var ms     = new MemoryStream(data))
            using (var gz     = new GZipStream(ms, CompressionMode.Decompress))
            using (var reader = new StreamReader(gz, System.Text.Encoding.UTF8))
                return reader.ReadToEnd();
        }
    }
}
