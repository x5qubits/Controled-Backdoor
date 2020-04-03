using JHUI;
using JHUI.Forms;
using BackdoorClient.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using JHUI.Utils;

namespace BackdoorClient
{
    public partial class SaveOrAdd : JForm
    {
        DbEntry edit = null;
        public SaveOrAdd(DbEntry db = null)
        {
            InitializeComponent();
            edit = null;
            if (db != null)
            {
                useriptb.Text = db.Ip;
                usernametb.Text = db.username;
                passwordtb.Text = db.password;
                jTextBox2.Text = db.entryname;
                jTextBox1.Text = db.port.ToString();
                edit = db;
            }
            DialogResult = DialogResult.Cancel;
        }

        private void jButton1_Click(object sender, EventArgs e)
        {
            var ip = useriptb.Text;

            if (!ip.ValidateIP())
            {
                JMessageBox.Show(this, "Invalid ip.");
                return;
            }
            var username = usernametb.Text;

            if (!StringUtils.IsAlphaNumericNoSpace(username) || !StringUtils.ValidLenght(username, 4,20))
            {
                JMessageBox.Show(this, "Invalid username.");
                return;
            }

            var passsword = passwordtb.Text;
            if (!StringUtils.IsAlphaNumericNoSpace(passsword) || !StringUtils.ValidLenght(passsword, 4, 20))
            {
                JMessageBox.Show(this, "Invalid password.");
                return;
            }

            var entryName = jTextBox2.Text;
            if (!StringUtils.IsAlphaNumericNoSpace(passsword) || !StringUtils.ValidLenght(passsword, 4, 20))
            {
                entryName = ip;
            }
            var port = 0;
            if (!int.TryParse(jTextBox1.Text, out port))
            {
                JMessageBox.Show(this, "Invalid port.");
                return;
            }

            if(edit != null)
            {
                edit.Ip = ip;
                edit.port = port;
                edit.entryname = entryName;
                edit.username = username;
                edit.password = passsword;
            }
            else
            {
                edit = new DbEntry() { port = port, entryname = entryName, Ip = ip, username = username, password = passsword };
            }

            DatabaseManager.Instance.AddOrUpdateEntry(edit);
            DatabaseManager.Instance.Save();
            DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
