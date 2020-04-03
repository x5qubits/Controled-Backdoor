using System.Collections.Generic;

namespace LLFU.Session
{
    public class AccountManager
    {
        static volatile AccountManager s_Instance;
        private static object s_Sync = new object();
        private static object D_Sync = new object();
        public static AccountManager Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    lock (s_Sync)
                    {
                        if (s_Instance == null)
                        {
                            s_Instance = new AccountManager();
                        }
                    }
                }
                return s_Instance;
            }
        }

        List<uint> users = new List<uint>();
 
        public bool IsOnline(uint connectionId)
        {
            bool r = false;
            lock (D_Sync)
            {
                r = users.Contains(connectionId);
            }
            return r;
        }

        public void AddUser(uint connectionId)
        {
            lock (D_Sync)
            {
                users.Add(connectionId);
            }
        }

        public void RemoveUserById(uint connectionId)
        {
            lock (D_Sync)
            {
                users.Remove(connectionId);
            }
        }
    }
}
