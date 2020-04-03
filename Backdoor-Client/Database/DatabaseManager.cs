using JCommon.FileDatabase;
using JCommon.FileDatabase.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BackdoorClient.Database
{
    public class DbEntry
    {
        public int UID;
        public string Ip;
        public int port;
        public string entryname;
        public string username;
        public string password;
        public bool connected = false;
    }

    public class DatabaseFile : DataFile
    {
        public DbEntry[] data = new DbEntry[0];

        public override void Deserialize(DataReader reader)
        {
            int c = reader.ReadInt32();
            data = new DbEntry[c];
            for (int i = 0; i < c; i++)
            {
                data[i] = new DbEntry()
                {
                    UID = i,
                    Ip = reader.ReadString(),
                    port = reader.ReadInt32(),
                    username = reader.ReadString(),
                    password = reader.ReadString(),
                    entryname = reader.ReadString(),
                    connected = false
                };
            }
        }
        public override void Serialize(DataWriter writer)
        {
            writer.Write(data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                writer.Write(data[i].Ip);
                writer.Write(data[i].port);
                writer.Write(data[i].username);
                writer.Write(data[i].password);
                writer.Write(data[i].entryname);
            }
        }
    }

    public class DatabaseManager
    {
        static volatile DatabaseManager s_Instance;
        private static object s_Sync = new object();
        private static object D_Sync = new object();
        Dictionary<int, DbEntry> entrys = new Dictionary<int, DbEntry>();
        int nextid = 0;
        private string databasePath;

        public static DatabaseManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    lock (s_Sync)
                    {
                        if (s_Instance == null)
                        {
                            s_Instance = new DatabaseManager();
                        }
                    }
                }
                return s_Instance;
            }
        }

        public void Load()
        {
            databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.data");
            DatabaseFile file = FileDatabase.ReadFile<DatabaseFile>(databasePath);
            if (file != null)
            {
                lock (D_Sync)
                {
                    for (int i = 0; i < file.data.Length; i++)
                    {
                        file.data[i].UID = i + 1;
                        entrys.Add(file.data[i].UID, file.data[i]);
                    }
                }
            }
            else
            {
                Save();
            }

        }
        public void Save()
        {
            lock (D_Sync)
            {
                var data = entrys.Values.ToArray();
                FileDatabase.WriteFile(new DatabaseFile() { Path = databasePath, data = data });
            }
        }

        public int AddOrUpdateEntry(DbEntry entry)
        {
            int ret = -1;
            lock (D_Sync)
            {
                if (entry.UID > 0 && entrys.ContainsKey(entry.UID))
                {
                    entrys[entry.UID] = entry;
                    ret = entry.UID;
                }
                else
                {
                    nextid++;
                    entry.UID = nextid;
                    entrys[entry.UID] = entry;
                    ret = entry.UID;
                }
            }
            return ret;
        }

        public void RemoveEntry(int id)
        {
            lock (D_Sync)
            {
                entrys.Remove(id);
            }
        }

        public DbEntry[] GetList()
        {
            DbEntry[] data;
            lock (D_Sync)
            {
                data = entrys.Values.ToArray();
            }
            return data;
        }
    }
}
