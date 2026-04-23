using System.IO;
using KspConnected.Shared.Protocol;

namespace KspConnected.Shared.Messages
{
    public class ChatMessage
    {
        public int    SenderId   = -1;
        public string SenderName = "";
        public string Text       = "";

        public byte[] ToPayload() => MessageFrame.BuildPayload(w =>
        {
            w.Write(SenderId);
            w.Write(SenderName);
            w.Write(Text);
        });

        public static ChatMessage FromPayload(byte[] payload) =>
            MessageFrame.ParsePayload(payload, r => new ChatMessage
            {
                SenderId   = r.ReadInt32(),
                SenderName = r.ReadString(),
                Text       = r.ReadString(),
            });
    }
}
