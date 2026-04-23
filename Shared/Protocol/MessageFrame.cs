using System;
using System.IO;

namespace KspConnected.Shared.Protocol
{
    /// <summary>
    /// Wire format: [int32 payloadLength][byte messageType][payload bytes]
    /// All integers are little-endian (BitConverter default on x86/x64).
    /// </summary>
    public static class MessageFrame
    {
        public static byte[] Encode(MessageType type, byte[] payload)
        {
            if (payload == null) payload = Array.Empty<byte>();
            if (payload.Length > ProtocolConstants.MaxPayloadBytes)
                throw new ArgumentException($"Payload exceeds max size: {payload.Length}");

            byte[] frame = new byte[ProtocolConstants.FrameHeaderSize + payload.Length];
            BitConverter.GetBytes(payload.Length).CopyTo(frame, 0);
            frame[4] = (byte)type;
            Buffer.BlockCopy(payload, 0, frame, ProtocolConstants.FrameHeaderSize, payload.Length);
            return frame;
        }

        /// <summary>
        /// Reads one complete frame from stream. Blocks until full frame is received.
        /// Returns (type, payload) or throws on stream close / oversized message.
        /// </summary>
        public static (MessageType type, byte[] payload) ReadFrame(Stream stream)
        {
            byte[] header = ReadExact(stream, ProtocolConstants.FrameHeaderSize);

            int payloadLen = BitConverter.ToInt32(header, 0);
            if (payloadLen < 0 || payloadLen > ProtocolConstants.MaxPayloadBytes)
                throw new InvalidDataException($"Invalid payload length: {payloadLen}");

            MessageType type = (MessageType)header[4];
            byte[] payload = payloadLen > 0 ? ReadExact(stream, payloadLen) : Array.Empty<byte>();
            return (type, payload);
        }

        public static void WriteFrame(Stream stream, MessageType type, byte[] payload)
        {
            byte[] frame = Encode(type, payload);
            stream.Write(frame, 0, frame.Length);
            stream.Flush();
        }

        private static byte[] ReadExact(Stream stream, int count)
        {
            byte[] buf = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buf, offset, count - offset);
                if (read == 0) throw new EndOfStreamException("Connection closed while reading frame.");
                offset += read;
            }
            return buf;
        }

        // Convenience helpers for building payloads with BinaryWriter
        public static byte[] BuildPayload(Action<BinaryWriter> write)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                write(bw);
                bw.Flush();
                return ms.ToArray();
            }
        }

        public static T ParsePayload<T>(byte[] payload, Func<BinaryReader, T> read)
        {
            using (var ms = new MemoryStream(payload))
            using (var br = new BinaryReader(ms, System.Text.Encoding.UTF8, leaveOpen: true))
                return read(br);
        }
    }
}
