using JTcpNetwork;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLFU.Network.Client.Data
{
    public class ReLogin : BaseNetworkMessage
    {
        public uint response;
        public override void Serialize(NetworkWriter writer)
        {
            writer.WritePackedUInt32(response);
        }

        public override void Deserialize(NetworkReader reader)
        {
            response = reader.ReadPackedUInt32();
        }
    }
}
