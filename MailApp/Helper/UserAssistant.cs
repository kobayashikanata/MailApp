using System;
using System.Collections.Generic;
using System.Linq;
using AppSettings = MailApp.Properties.Settings;

namespace MailApp
{
    internal class UserAssistant
    {
        private string userBinPath;

        public UserAssistant(string userBinPath)
        {
            this.userBinPath = userBinPath;
        }

        #region User function

        public void SaveToRecent(string userName, string displayName)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(displayName))
                return;
            if (AppSettings.Default.HistoryUsers.ContainsKey(userName))
            {
                var old = AppSettings.Default.HistoryUsers[userName];
                if (old != null && old == displayName) return;
                AppSettings.Default.HistoryUsers[userName] = displayName;
            }
            else
            {
                AppSettings.Default.HistoryUsers.Add(userName, displayName);
            }
            AppSettings.Default.Save();
        }

        public string GetUserDisplayName(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return userName;
            if (AppSettings.Default.HistoryUsers.ContainsKey(userName))
            {
                var display = AppSettings.Default.HistoryUsers[userName];
                if (display != null && !string.IsNullOrEmpty(display))
                    return display;
            }
            return userName;
        }
        
        public void StoreUserLocal(string userName, string psw)
        {
            var obj = SerializeAssistant.Deserialize(userBinPath);
            Dictionary<string, string> users = null;
            if (null == obj || null == (users = obj as Dictionary<string, string>))
            {
                users = new Dictionary<string, string>();
            }
            if (users.ContainsKey(userName))
                users[userName] = psw.Protect();
            else
                users.Add(userName, psw.Protect());
            SerializeAssistant.Serialize(userBinPath, users);
        }
        
        public bool GetLocalStoredUser(string userName, ref string psw)
        {
            var obj = SerializeAssistant.Deserialize(userBinPath);
            Dictionary<string, string> users = null;
            if (null == obj || null == (users = obj as Dictionary<string, string>))
            {
                return false;
            }
            if (!users.ContainsKey(userName)) return false;
            try
            {
                psw = users[userName].Unprotect();
            }catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Unprotect()\n" + e.Message);
                return false;
            }
            return true;
        }

        public void RemoveUserLocal(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return;
            var obj = SerializeAssistant.Deserialize(userBinPath);
            Dictionary<string, string> users = null;
            if (null == obj || null == (users = obj as Dictionary<string, string>))
                return;
            if (users.ContainsKey(userName))
            {
                users.Remove(userName);
                SerializeAssistant.Serialize(userBinPath, users);
            }
            if (AppSettings.Default.LastLogUser == userName)
            {
                AppSettings.Default.LastLogUser = string.Empty;
                AppSettings.Default.AutoLogin = false;
            }
            AppSettings.Default.Save();
        }

        public void RemoveFromRecent(string userName)
        {
            if (string.IsNullOrEmpty(userName)) return;
            if (AppSettings.Default.HistoryUsers.ContainsKey(userName))
            {
                AppSettings.Default.HistoryUsers.Remove(userName);
                AppSettings.Default.Save();
            }
            if (AppSettings.Default.LastLogUser == userName)
            {
                AppSettings.Default.LastLogUser = string.Empty;
                AppSettings.Default.AutoLogin = false;
                AppSettings.Default.Save();
            }
        }
        
        #endregion
    }
}
