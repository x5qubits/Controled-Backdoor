using JCommon;
using JHUI;
using JHUI.Forms;
using JTcpNetwork;
using LLFU.Network.Client.Data;
using BackdoorClient.Database;
using BackdoorClient.Network.Data;
using LoginServer.Network;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace BackdoorClient
{
    public partial class MainForm : JForm
    {
        public MainForm()
        {
            DatabaseManager.Instance.Load();
         
            InitializeComponent();
            jLabel1.Text = "Externalip : " + IGetExternalIp();
        }

        DbEntry[] database = new DbEntry[0];
        DbEntry connected = null;
        bool connecting = false;
        int index = -1;
        private float lastUpdateIp;

        public string ExternalIp { get; private set; }
        public object IpLock = new object();

        private void Connect(DbEntry entry, int x)
        {
           // if (connecting) return;

            NetworkClient.Stop();
            connecting = true;
            if (connected != null)
            {
                index = -1;
                connected.connected = false;
                DatabaseManager.Instance.AddOrUpdateEntry(connected);
                Draw();
            }
            index = x;
            connected = entry;
            jLabel1.Text = "Connecting ...";
            RegisterHandlers();
            NetworkClient.Start(entry.Ip, entry.port);
        }

        private void OnDisconnect(NetworkMessage netMsg)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate { OnDisconnect(netMsg); }));
                return;
            }
            index = -1;
            if (connected != null)
            {
                connected.connected = false;
                DatabaseManager.Instance.AddOrUpdateEntry(connected);
                Draw();
            }
            jLabel1.Text = "Disconnected.";
            connected = null;
            connecting = false;
        }

        private void OnConnect(NetworkMessage netMsg)
        {
            connecting = false;
            if(connected != null)
            NetworkClient.Send(NetworkConstants.LOGIN, new LoginPacket() { username = connected.username, password = connected.password });
        }

        private void OnLogin(NetworkMessage netMsg)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate { OnLogin(netMsg); }));
                return;
            }
            ReLogin paket = netMsg.ReadMessage<ReLogin>();
            if(paket != null)
            {
                switch (paket.response)
                {
                    case 0:
                        if (connected != null)
                        {
                            connected.connected = true;
                            DatabaseManager.Instance.AddOrUpdateEntry(connected);
                            Draw();
                        }
                        jLabel1.Text = "Connected.";
                        break;
                    case 1:
                        JMessageBox.Show(this, "Firewall missing from server.");
                        if (connected != null)
                        {
                            connected.connected = true;
                            DatabaseManager.Instance.AddOrUpdateEntry(connected);
                            Draw();
                        }
                        jLabel1.Text = "Connected.";
                        break;
                    default:
                        jLabel1.Text = "Wrong user or password!";
                        break;
                }
            }
        }

        private void AddNewSite_Click(object sender, EventArgs e)
        {
            var res = new SaveOrAdd().ShowDialog();
            if(res == DialogResult.OK)
            {
                Draw();
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            Draw();
        }

        private void Draw() 
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate { Draw(); })); 
                return;
            }

            database = DatabaseManager.Instance.GetList();
            dataGridView_item.Rows.Clear();
            foreach(DbEntry d in database)
            {
                DataGridViewRow row = (DataGridViewRow)dataGridView_item.RowTemplate.Clone();
                row.CreateCells(dataGridView_item);
                row.Cells[0].Value = d.entryname;
                row.Cells[1].Value = d.connected ? "Connected":"Disconnected";
                dataGridView_item.Rows.Add(row);
            }
            if(dataGridView_item.Rows.Count > 0 && index == -1)
            {
                dataGridView_item.Rows[0].Selected = true;
                dataGridView_item.CurrentCell = dataGridView_item.Rows[0].Cells[0];
            }
            else
            {
                if(index != -1)
                {
                    try
                    {
                        dataGridView_item.Rows[index].Selected = true;
                        dataGridView_item.CurrentCell = dataGridView_item.Rows[index].Cells[0];
                    }
                    catch { }
                }
            }
            jLabel1.Text = "Externalip : " + IGetExternalIp();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView_item.CurrentCell != null)
            {
                int rowindex = dataGridView_item.CurrentCell.RowIndex;
                if(rowindex != -1)
                {

                    DialogResult dialog = JMessageBox.Show(this, "Do you want to delete "+ database[rowindex] .entryname+ "?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (dialog == DialogResult.No)
                    {
                        return;
                    }

                    DatabaseManager.Instance.RemoveEntry(database[rowindex].UID);
                    DatabaseManager.Instance.Save();
                    Draw();
                }
            }
        }

        private void MainMenu_Opening(object sender, CancelEventArgs e)
        {
            if (connecting) { e.Cancel = true; return; }
            if (dataGridView_item.CurrentCell != null)
            {
                int rowindex = dataGridView_item.CurrentCell.RowIndex;
                if (rowindex != -1)
                {
                    DbEntry de = database[rowindex];
                    if (de.connected)
                    {
                        connectToolStripMenuItem.Visible = false;
                        editToolStripMenuItem.Visible = false;
                        discconectToolStripMenuItem.Visible = true;
                        deleteToolStripMenuItem.Visible = false;
                        firewallToolStripMenuItem.Visible = true;
                    }
                    else
                    {
                        connectToolStripMenuItem.Visible = true;
                        editToolStripMenuItem.Visible = true;
                        discconectToolStripMenuItem.Visible = false;
                        deleteToolStripMenuItem.Visible = true;
                        firewallToolStripMenuItem.Visible = false;


                    }
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView_item.CurrentCell != null)
            {
                int rowindex = dataGridView_item.CurrentCell.RowIndex;
                if (rowindex != -1)
                {
                    var entry = database[rowindex];
                    Connect(entry, rowindex);
                }
            }
        }

        private void discconectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(connected != null)
            {
                index = -1;
                connected.connected = false;
                DatabaseManager.Instance.AddOrUpdateEntry(connected);
                Draw();
            }
            NetworkClient.Stop();
            jLabel1.Text = "Disconnected.";
        }

        string IGetExternalIp()
        {
            string EIpAddress = "";
            lock (IpLock)
            {

                if (ExternalIp == null || ExternalIp != null && ExternalIp == "127.0.0.1" && lastUpdateIp < Time.time)
                {
                    var request = WebRequest.Create("https://api6.ipify.org/");
                    WebResponse response = request.GetResponse();
                    Stream data = response.GetResponseStream();
                    string html = String.Empty;
                    using (StreamReader sr = new StreamReader(data))
                    {
                        html = sr.ReadToEnd();
                    }

                    EIpAddress = html;
                    lastUpdateIp = Time.time + 60;
                    
                }
                else
                {
                    EIpAddress = ExternalIp;
                }

            }
            return EIpAddress;
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView_item.CurrentCell != null)
            {
                int rowindex = dataGridView_item.CurrentCell.RowIndex;
                if (rowindex != -1)
                {
                    var keep = index;
                    index = rowindex;
                    var entry = database[rowindex];
                    var res = new SaveOrAdd(entry).ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        Draw();
                        index = keep;
                    }
                }
            }

        }

        private void RegisterHandlers()
        {
            NetworkClient.RegisterHandler(NetworkConstants.LOGIN, OnLogin);
            NetworkClient.RegisterHandler(InternalMessages.CONNECTED, OnConnect);
            NetworkClient.RegisterHandler(InternalMessages.DISCONNECT, OnDisconnect);
            NetworkClient.RegisterHandler(NetworkConstants.ENABLE_FIREWALL, OnEnableFirewall);
            NetworkClient.RegisterHandler(NetworkConstants.DISABLE_FIREWALL, OnDisablenableFirewall);
            NetworkClient.RegisterHandler(NetworkConstants.DOWNLOAD_FIREWALL, OnDownload);
            NetworkClient.RegisterHandler(NetworkConstants.UPLOAD_FIREWALL, OnUpload);
        }

        #region ENABLE FIREWALL
        private void OnEnableFirewall(NetworkMessage netMsg)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate { OnEnableFirewall(netMsg); }));
                return;
            }
            var msg = netMsg.ReadMessage<ReLogin>();
            if(msg.response == 0) { JMessageBox.Show(this, "Firewall is now enabled!"); }
            else { JMessageBox.Show(this, "Failure to enable firewall."); }
        }

        private void enableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NetworkClient.Send(NetworkConstants.ENABLE_FIREWALL, new ReLogin() { response = 1 });
        }
        #endregion
        #region Disable FIREWALL
        private void OnDisablenableFirewall(NetworkMessage netMsg)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate { OnDisablenableFirewall(netMsg); }));
                return;
            }
            var msg = netMsg.ReadMessage<ReLogin>();
            if (msg.response == 0) { JMessageBox.Show(this, "Firewall is now disabled!"); }
            else { JMessageBox.Show(this, "Failure to disable firewall."); }
        }
        private void disableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NetworkClient.Send(NetworkConstants.DISABLE_FIREWALL, new ReLogin() { response = 1 });
        }
        #endregion
        #region Download
        private void OnDownload(NetworkMessage netMsg)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate { OnDownload(netMsg); }));
                return;
            }
            var msg = netMsg.ReadMessage<FirewallData>();
            if (msg != null) {
                File.WriteAllBytes(path, msg.Firewall);
            }
            else { JMessageBox.Show(this, "Failure to download the firewall."); }

        }
        string path = "";
        private void downloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog
            {
                Filter = "Text File (*.txt)|*.txt",
                FileName = "firewall.txt"
            };
            if (save.ShowDialog() == DialogResult.OK && save.FileName != "")
            {
                path = save.FileName;
                NetworkClient.Send(NetworkConstants.DOWNLOAD_FIREWALL, new ReLogin() { response = 1 });
            }
        }
        #endregion
        #region Upload
        private void OnUpload(NetworkMessage netMsg)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate { OnUpload(netMsg); }));
                return;
            }
            var msg = netMsg.ReadMessage<ReLogin>();
            if (msg.response == 0) { JMessageBox.Show(this, "Firewall is now uploaded!"); }
            else { JMessageBox.Show(this, "Failure to upload the firewall."); }
        }

        private void uploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog eLoad = new OpenFileDialog
            {
                Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*",
                RestoreDirectory = false
            };
            if (eLoad.ShowDialog() == DialogResult.OK && File.Exists(eLoad.FileName))
            {
                string text = File.ReadAllText(eLoad.FileName);
                if (text.Contains("#REPLACE_IP"))
                {
                    NetworkClient.Send(NetworkConstants.UPLOAD_FIREWALL, new FirewallData() { Firewall = File.ReadAllBytes(eLoad.FileName) });
                }
                else
                {
                    JMessageBox.Show(this, "Please add \"#REPLACE_IP\" in between the start of the iptables script and the end.");
                }
            }
        }
        #endregion

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            NetworkClient.Stop();
            Environment.Exit(0);
        }
    }
}
