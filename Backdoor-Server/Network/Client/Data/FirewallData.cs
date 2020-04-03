using JTcpNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLFUC.Network.Data
{
    public class FirewallData : BaseNetworkMessage
    {
        public uint status;
        public byte[] Firewall = new byte[0];

        public override void Deserialize(NetworkReader reader)
        {
            status = reader.ReadPackedUInt32();
            Firewall = reader.ReadBytesAndSize();
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.WritePackedUInt32(status);
            writer.WriteBytesFull(Firewall);
        }
    }
}
