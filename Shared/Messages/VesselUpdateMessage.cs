using System.IO;
using KspConnected.Shared.Data;
using KspConnected.Shared.Protocol;

namespace KspConnected.Shared.Messages
{
    public class VesselUpdateMessage
    {
        public VesselState State;

        public byte[] ToPayload() => MessageFrame.BuildPayload(w => State.Serialize(w));

        public static VesselUpdateMessage FromPayload(byte[] payload) =>
            MessageFrame.ParsePayload(payload, r => new VesselUpdateMessage
            {
                State = VesselState.Deserialize(r),
            });
    }
}
