using JTcpNetwork;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLFU.Network.Client.Data
{
    public class LoginPacket : BaseNetworkMessage
    {
        public string username;
        public string password;

        public override void Deserialize(NetworkReader reader)
        {
            username = reader.ReadString();
            password = reader.ReadString();
        }
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(username);
            writer.Write(password);
        }
    }
}
