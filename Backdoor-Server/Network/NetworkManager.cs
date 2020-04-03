using System;
using System.Collections.Generic;
using JCommon;
using JTcpNetwork;
using static CommonConstant;
using LLFU.Network.Client.Data;
using LLFU.Session;
using LoginServer.Network;
using LLFUC.Network.Data;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LLFU.Network
{
    public sealed class NetworkManager
    {
        public static readonly NetworkManager Instance = new NetworkManager();
        private AccountManager DbManager;

        #region HANDLERS
        private Dictionary<short, INetInterface> m_MessageHandlersDict = new Dictionary<short, INetInterface>();

        public void RegisterHandeler(short id, INetInterface handler)
        {
            if (!m_MessageHandlersDict.ContainsKey(id))
            {
                m_MessageHandlersDict[id] = handler;
            }
            else
                Log.Error("Network Handler " + (object)id + " already exist.");
        }

        public void UnregisterHandler(short id)
        {
            if (m_MessageHandlersDict.ContainsKey(id))
                m_MessageHandlersDict.Remove(id);
            else
                Log.Error("Network Handler " + (object)id + " already exist.");
        }
        #endregion

        private NetworkManager() { }

        public void InitHandlers()
        {
            NetworkServer.RegisterHandler(InternalMessages.DISCONNECT, OnDisconnect);
            NetworkServer.RegisterHandler(InternalMessages.RECIEVE, OnNetworkReceive);
            NetworkServer.RegisterHandler(NetworkConstants.LOGIN, OnLogin);
            NetworkServer.RegisterHandler(NetworkConstants.ENABLE_FIREWALL, OnEnableFirewall);
            NetworkServer.RegisterHandler(NetworkConstants.DISABLE_FIREWALL, OnDisableFirewall);
            NetworkServer.RegisterHandler(NetworkConstants.UPLOAD_FIREWALL, OnUploadFirewall);
            NetworkServer.RegisterHandler(NetworkConstants.DOWNLOAD_FIREWALL, OnDownloadFirewall) ;            
        }

        private static object LockFile = new object();
        private void OnDownloadFirewall(NetworkMessage netMsg)
        {
            var packet = netMsg.ReadMessage<ReLogin>();
            if (packet != null)
            {
                uint conid = netMsg.conn.connectionId;
                if (DbManager.IsOnline(conid))
                {
                    lock (LockFile)
                    {
                        if (File.Exists(Program.path_to_firewall_up_org))
                        {
                            netMsg.conn.Send(NetworkConstants.DOWNLOAD_FIREWALL, new FirewallData() { status = 0, Firewall = File.ReadAllBytes(Program.path_to_firewall_up_org) });
                            Log.Info("Client [" + netMsg.conn.IP + "] downloads the firewal.");
                        }
                        else
                        {
                            netMsg.conn.Send(NetworkConstants.DOWNLOAD_FIREWALL, new FirewallData() { status = 1, Firewall = new byte[0] });
                        }
                    }
                }
            }
        }

        private void OnUploadFirewall(NetworkMessage netMsg)
        {
            var packet = netMsg.ReadMessage<FirewallData>();
            if (packet != null)
            {
                uint conid = netMsg.conn.connectionId;
                if (DbManager.IsOnline(conid))
                {
                    lock (LockFile)
                    {
                        File.WriteAllBytes(Program.path_to_firewall_up_org, packet.Firewall);
                        netMsg.conn.Send(NetworkConstants.UPLOAD_FIREWALL, new ReLogin() { response = 0 });
                        Log.Info("Client ["+netMsg.conn.IP+"] uploads a new firewall script.");
                    }
                }
            }
        }

        private void OnDisableFirewall(NetworkMessage netMsg)
        {
            var packet = netMsg.ReadMessage<ReLogin>();
            if (packet != null)
            {
                uint conid = netMsg.conn.connectionId;
                if (DbManager.IsOnline(conid))
                {
                    ShellHelper.ExecuteComand("chmod 777 -R "+Path.GetDirectoryName(Program.path_to_firewall_down));
                    ShellHelper.ExecuteComand(Program.path_to_firewall_down);
                    netMsg.conn.Send(NetworkConstants.DISABLE_FIREWALL, new ReLogin() { response = 0 });
                    Log.Info("Client [" + netMsg.conn.IP + "] disables the firewall.");
                }
            }
        }

        private void OnEnableFirewall(NetworkMessage netMsg)
        {
            var packet = netMsg.ReadMessage<ReLogin>();
            if (packet != null)
            {
                uint conid = netMsg.conn.connectionId;
                if (DbManager.IsOnline(conid))
                {
                    lock (LockFile)
                    {
                        ShellHelper.ExecuteComand("chmod 777 -R " + Path.GetDirectoryName(Program.path_to_firewall_up));
                        string firewall = File.ReadAllText(Program.path_to_firewall_up_org).Replace("#REPLACE_IP", netMsg.conn.IP);
                        File.WriteAllText(Program.path_to_firewall_up, firewall);
                    }
                    ShellHelper.ExecuteComand(Program.path_to_firewall_up);
                    netMsg.conn.Send(NetworkConstants.ENABLE_FIREWALL, new ReLogin() { response = 0 });
                    Log.Info("Client [" + netMsg.conn.IP + "] enables the firewall.");
                }
            }
        }

        private void OnLogin(NetworkMessage netMsg)
        {
            LoginPacket packet = netMsg.ReadMessage<LoginPacket>();
            if (packet != null)
            {
                if (packet.username == Program.username && packet.password == Program.password)
                {
                    DbManager.AddUser(netMsg.conn.connectionId);
                    var has = false;
                    if (File.Exists(Program.path_to_firewall_up_org))
                    {
                        lock (LockFile)
                        {
                            string firewall = File.ReadAllText(Program.path_to_firewall_up_org).Replace("#REPLACE_IP", netMsg.conn.IP);
                            File.WriteAllText(Program.path_to_firewall_up, firewall);
                        }
                        has = true;
                    }
                    Log.Info("Client [" + netMsg.conn.IP + "] Logged in.");
                    netMsg.conn.Send(NetworkConstants.LOGIN, new ReLogin() { response = (uint)(has ? SUCCESS : FAILURE) });
                }
                else
                {
                    netMsg.conn.Send(NetworkConstants.LOGIN, new ReLogin() { response = WRONG_LOGIN });
                }
            }
        }

        private void OnDisconnect(NetworkMessage netMsg)
        {
            DbManager.RemoveUserById(netMsg.conn.connectionId);
            Log.Info("Client [" + netMsg.conn.IP + "] logged out.");
        }

        private void OnNetworkReceive(NetworkMessage netMsg)
        {
            if (m_MessageHandlersDict.TryGetValue(netMsg.msgType, out INetInterface handler))
            {
                lock (handler)
                {
                    handler.Execute(netMsg);
                }
            }
        }

        public void Start()
        {
            DbManager = AccountManager.Instance;

            foreach (KeyValuePair<short, INetInterface> that in m_MessageHandlersDict)
            {
                NetworkServer.RegisterHandler(that.Key, OnNetworkReceive);
            }
            ShellHelper.ExecuteComand("chmod 777 -R " + Path.GetDirectoryName(Program.path_to_firewall_up));
            NetworkServer.Start(Program.ip, Program.port);
        }
    }
}
