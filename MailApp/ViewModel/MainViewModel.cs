using MessengerLight;
using MessengerLight.Command;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Application = System.Windows.Application;
using AppSettings = MailApp.Properties.Settings;
using Mail;

namespace MailApp.ViewModel
{
    public class MainViewModel : NotificationObject, IDisposable
    {
        public bool UseImap
        {
            get { return AppSettings.Default.UseImap; }
            set
            {
                if(AppSettings.Default.UseImap != value)
                {
                    AppSettings.Default.UseImap = value;
                    RaisePropertyChanged(nameof(UseImap));
                    if (IsLogined)
                    {
                        Messenger.Default.Send(new DisplayMessage("Take Effect now?", "Logout and redo login by new settings.")
                        {
                            OnOk = () =>
                            {
                                UpdateSettings(AppSettings.Default.UseImap);
                                LoginImpl(Password, true);
                            }
                        });
                    }
                    else
                    {
                        UpdateSettings(AppSettings.Default.UseImap);
                    }
                }
            }
        }

        private IClientSettings settings = AppSettings.Default.Pop3Settings;
        private Func<IMailClient> clientCreator;
        private IMailClient globalClient;

        #region **************  Properties **************

        public bool isRequesting;
        public bool IsRequesting
        {
            get { return isRequesting; }
            set
            {
                if (isRequesting != value)
                {
                    isRequesting = value;
                    RaisePropertyChanged(nameof(IsRequesting));
                    LoginCommand?.RaiseCanExecuteChanged();
                    DeleteInboxMailCommand?.RaiseCanExecuteChanged();
                    GetInboxMailCommand?.RaiseCanExecuteChanged();
                }
            }
        }
        
        private bool isLogined;
        public bool IsLogined
        {
            get { return isLogined; }
            set
            {
                if (isLogined != value)
                {
                    isLogined = value;
                    RaisePropertyChanged(nameof(IsLogined));
                    LoginCommand?.RaiseCanExecuteChanged();
                    LoginIfAutoCommand?.RaiseCanExecuteChanged();
                    ForgetUserCommand?.RaiseCanExecuteChanged();
                    LogoutCommand?.RaiseCanExecuteChanged();
                    GetInboxMailCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool isLandingIn;
        public bool IsLandingIn
        {
            get { return isLandingIn; }
            set
            {
                if (isLandingIn != value)
                {
                    isLandingIn = value;
                    RaisePropertyChanged(nameof(IsLandingIn));
                }
            }
        }
        
        private string userAddress;
        public string UserAddress
        {
            get { return userAddress; }
            set
            {
                if (userAddress != value)
                {
                    userAddress = value;
                    RaisePropertyChanged(nameof(UserAddress));
                    LoginCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private string displayName;
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(displayName)
                    && !string.IsNullOrEmpty(UserAddress))
                    displayName = this.UserAddress;
                return displayName;
            }set
            {
                if(displayName != value)
                {
                    displayName = value;
                    if (IsLogined)
                        userAssistant?.SaveToRecent(UserAddress, DisplayName);
                    RaisePropertyChanged(nameof(DisplayName));
                }
            }
        }

        //Store encrypted password
        private string encryptedPassword;
        //Get password from encrypted password
        //Unsolved: Password string will be visible
        //for a short time, and can be read from RAM
        public string Password
        {
            get
            {
                if (encryptedPassword == null)
                    return null;
                return encryptedPassword.Unprotect();
            }
        }
        
        public RelayCommand<object> LoginCommand { get; set; }
        public RelayCommand<object> AddNewDraftCommand { get; set; }
        public RelayCommand<object> DeleteDraftCommand { get; set; }
        public RelayCommand<object> SaveAttachmentCommand { get; set; }
        public RelayCommand<object> ReplyCommand { get; set; }
        public RelayCommand<object> ForwardCommand { get; set; }
        public RelayCommand<object> DeleteOutboxMailCommand { get; set; }
        public RelayCommand<object> DeleteInboxMailCommand { get; set; }
        public RelayCommand GetInboxMailCommand { get; set; }
        public RelayCommand<object> LoginIfAutoCommand { get; set; }
        public RelayCommand<object> RemoveUserFromRecentCommand { get; set; }
        public RelayCommand<object> ForgetUserCommand { get; set; }
        public RelayCommand LogoutCommand { get; set; }
        public RelayCommand<object> NavigationInMainCommand { get; set; }

        private BindingList<MailMsgBase> inboxList;
        public BindingList<MailMsgBase> InboxList
        {
            get
            {
                if (inboxList == null)
                    inboxList = new BindingList<MailMsgBase>();
                return inboxList;
            }
            set
            {
                inboxList = value;
                RaisePropertyChanged(nameof(InboxList));
            }
        }

        private BindingList<object> outboxList;
        public BindingList<object> OutboxList
        {
            get
            {
                if (outboxList == null)
                    outboxList = new BindingList<object>();
                return outboxList;
            }
            set
            {
                outboxList = value;
                RaisePropertyChanged(nameof(OutboxList));
            }
        }

        private BindingList<MailBase> draftList;
        public BindingList<MailBase> DraftList
        {
            get
            {
                if (draftList == null)
                    draftList = new BindingList<MailBase>();
                return draftList;
            }set
            {
                if(draftList != value)
                {
                    draftList = value;
                    RaisePropertyChanged(nameof(DraftList));
                }
            }
        }
        
        public string DraftFolder { get; private set; }
        public string InboxFolder { get; private set; }
        public string DataFolder { get; private set; }
        public string UserBinPath { get; private set; }
        private string draftBinPath;
        private UserAssistant userAssistant;

        #endregion ****************************

        protected override void Init()
        {
            //Ensure local path created.
            DataFolder = AppDomain.CurrentDomain.BaseDirectory + "Data";
            DraftFolder = DataFolder + "\\Draft";
            InboxFolder = DataFolder + "\\Inbox";
            draftBinPath = DraftFolder.CombinePath("Draft.bin");
            UserBinPath = DataFolder.CombinePath("User.bin");
            EnsureCreateFolder(DataFolder);
            EnsureCreateFolder(DraftFolder);
            EnsureCreateFolder(InboxFolder);
            EnsureCreateFile(UserBinPath);
            //Get stored data
            userAssistant = new UserAssistant(UserBinPath);
            var obj = SerializeAssistant.Deserialize(draftBinPath);
            if (null != obj)
            {
                BindingList<MailBase> drafts = null;
                drafts = obj as BindingList<MailBase>;
                if (drafts != null) DraftList = drafts;
            }
            UpdateSettings(UseImap);
            //Init commands.
            LoginCommand = new RelayCommand<object>(Login, o =>
            {
                return !(IsLogined || string.IsNullOrEmpty(UserAddress) || IsRequesting);
            });
            LoginIfAutoCommand = new RelayCommand<object>(CheckAutoLogin, o => !IsLogined);
            LogoutCommand = new RelayCommand(Logout, () => IsLogined);
            GetInboxMailCommand = new RelayCommand(GetInboxMail, () => IsLogined && !IsRequesting);
            ReplyCommand = new RelayCommand<object>(ReplyMailFrom);
            ForwardCommand = new RelayCommand<object>(ForwardTo);
            AddNewDraftCommand = new RelayCommand<object>(NavigateNewDraft);
            DeleteDraftCommand = new RelayCommand<object>(DeleteDraft);
            SaveAttachmentCommand = new RelayCommand<object>(SaveAttachmentAs);
            DeleteOutboxMailCommand = new RelayCommand<object>(DeleteOutboxMail);
            DeleteInboxMailCommand = new RelayCommand<object>(DeleteInboxMail, o => !IsRequesting);
            ForgetUserCommand = new RelayCommand<object>(RemoveLocalUserActon, o => IsLogined);
            RemoveUserFromRecentCommand = new RelayCommand<object>(RemoveUserFromRecentAction);
            NavigationInMainCommand = new RelayCommand<object>(Navigate);
        }
        
        #region ************** Command action **************

        //Send navigation message to main window
        private void Navigate(object o)
        {
            if (null == o) return;
            if (o is NavigationMessage)
            {
                Messenger.Default.Send((NavigationMessage)o, Tokens.Main);
            }else if(o is OverlayMessage)
            {
                Messenger.Default.Send((OverlayMessage)o, Tokens.Main);
            }
        }

        //Action for login command
        private void Login(object o)
        {
            if (null == o) return;
            var values = o as object[];
            if (values != null && values.Length == 2)
            {
                if (null == values[0]) return;
                var pswbox = values[0] as System.Windows.Controls.PasswordBox;
                if (null == pswbox) return;
                var logUser = values[1] is bool ? (bool)values[1] : false;
                LoginImpl(pswbox.Password, logUser);
            }
        }

        //Action for logout command
        private void Logout()
        {
            LogoutImpl();
        }
        
        //Auto login if there is auto config in local
        private void CheckAutoLogin(object o)
        {
            string lastLogined = AppSettings.Default.LastLogUser;
            if (!string.IsNullOrEmpty(lastLogined))
            {
                this.UserAddress = lastLogined;
                string psw = null;
                if (AppSettings.Default.AutoLogin
                    && userAssistant.GetLocalStoredUser(lastLogined, ref psw))
                {
                    Messenger.Default.Send("ClearOverlays", Tokens.Main);
                    //GetInboxMailByIMap(psw);
                    LoginImpl(psw, false);
                }
                else
                {
                    Messenger.Default.Send("ClearOverlays", Tokens.Main);
                    Messenger.Default.Send(new OverlayMessage
                    {
                         OverlayType = o as Type
                    }, Tokens.Main);
                }
            }
        }
        
        //Call navigate to New draft page by NavigationMessage
        private void NavigateNewDraft(object o)
        {
            Type type = o as Type;
            if (null == type) return;
            var newMail = new MailBase
            {
                ID = Guid.NewGuid().ToString("N"),
                BodyHtml = "Hi"
            };
            var msg = new NavigationMessage(type, newMail, Tokens.NavEdit);
            Messenger.Default.Send(msg, Tokens.Main);
            Messenger.Default.Send(msg, Tokens.Draft);
        }

        //Delete draft
        private void DeleteDraft(object o)
        {
            if (o != null)
            {
                var mail = o as MailBase;
                if (null != mail)
                    RemoveDraft(mail.ID);
            }
        }

        //Get inbox mail by client
        private async void GetInboxMail()
        {
            await Task.Run(() =>
            {
                IsRequesting = true;
                EnsureLoginFetchMessage();
                IsRequesting = false;
            });
        }

        //Save attachemtns to local path
        private void SaveAttachmentAs(object o)
        {
            if (null != o && o is object[])
            {
                var data = o as object[];
                if (data.Length != 2) return;
                var attachment = data[0] as IAttachment;
                var toPath = data[1] as string;
                try
                {
                    if (File.Exists(toPath))
                        File.Delete(toPath);
                    attachment.Save(toPath);
                }
                catch (Exception e)
                {
                    Messenger.Default.Send(new DisplayMessage("Save attachment fail!", e.Message, DisplayType.Toast));
                }
            }
        }

        //Delete inbox mail by client 
        private void DeleteInboxMail(object o)
        {
            var mail = o as MailMsgBase;
            if (mail != null && !string.IsNullOrEmpty(mail.UID))
            {
                Messenger.Default.Send(new DisplayMessage("Are you sure?", "The mail will delete and not exist.Ensure delete this mail.")
                {
                    OnOk = async () =>
                    {
                        System.Diagnostics.Debug.WriteLine("On ok click.");
                        IsRequesting = true;
                        await Task.Run(() =>
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine("Delete mail action invoked.");
                                if(EnsureLoginFetchMessage() && globalClient.DeleteMessage(mail.UID))
                                {
                                    Messenger.Default.Send(new DisplayMessage("Delete Successful!", DisplayType.Toast));
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        InboxList.Remove(mail);
                                    });
                                }
                            }
                            catch (Exception e)
                            {
                                Messenger.Default.Send(new DisplayMessage("Delete Fail!", e.Message));
                            }
                        });
                        IsRequesting = false;
                    }
                });
            }
        }

        //Reply mail base on exist mail
        //use NavigationMessage
        private void ReplyMailFrom(object o)
        {
            if (o is object[])
            {
                var parameters = o as object[];
                if (null == parameters) return;
                var type = parameters[0] as Type;
                var extra = parameters[1] as MailMsgBase;
                var mail = new MailBase()
                {
                    BodyHtml = extra.BodyHtmlText,
                    BodyText = extra.BodyText,
                    Subject = "RE: " + extra.Subject,
                    ToAddresses = extra.From[0].Address
                };
                var msg = new NavigationMessage(type, mail, Tokens.NavEdit);
                Messenger.Default.Send(msg, Tokens.Main);
                Messenger.Default.Send(msg, Tokens.Draft);
            }
        }

        //Forward mail base on exist mail
        private void ForwardTo(object o)
        {
            if (o is object[])
            {
                var parameters = o as object[];
                if (null == parameters) return;
                var type = parameters[0] as Type;
                var extra = parameters[1] as MailMsgBase;
                MailBase mail = null;
                try
                {
                    mail = new MailBase()
                    {
                        BodyHtml = extra.BodyHtmlText,
                        BodyText = extra.BodyText,
                        Subject = "FW: " + extra.Subject
                    };
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
                var msg = new NavigationMessage(type, mail, Tokens.NavEdit);
                Messenger.Default.Send(msg, Tokens.Main);
                Messenger.Default.Send(msg, Tokens.Draft);
            }
        }
        
        private void DeleteOutboxMail(object o)
        {
            Messenger.Default.Send(new DisplayMessage("Tips", "No implement now!"));
        }

        //Remove a user who is remember password from local.
        private void RemoveLocalUserActon(object o)
        {
            if (null == o) return;
            string userName = o as string;
            if (null == userName) return;
            userAssistant.RemoveUserLocal(userName);
        }

        //Remove recent user
        private void RemoveUserFromRecentAction(object o)
        {
            if (null == o) return;
            string userName = o as string;
            if (null == userName) return;
            userAssistant.RemoveFromRecent(userName);
        }

        #endregion ****************************
        
        private POP3Client CreatePOP3Client()
        {
            return new POP3Client();
        }

        private ImapClient CreateImapClient()
        {
            return new ImapClient();
        }

        private void ClearGlobalClient()
        {
            if (globalClient != null)
            {
                globalClient.Logout();
                if (globalClient.IsConnected)
                    globalClient.Disconnect();
                globalClient.Dispose();
                globalClient = null;
            }
        }

        //Update client settings
        private void UpdateSettings(bool useImap)
        {
            ClearGlobalClient();
            if (IsLogined)
                IsLogined = false;
            if (IsRequesting)
                IsRequesting = false;
            InboxList.Clear();
            if (useImap)
            {
                settings = AppSettings.Default.ImapSettings;
                clientCreator = CreateImapClient;
            }
            else
            {
                settings = AppSettings.Default.Pop3Settings;
                clientCreator = CreatePOP3Client;
            }
            System.Diagnostics.Debug.WriteLine("UpdateSettings " + useImap);
        }
        
        //Implement login function
        private async void LoginImpl(string psw, bool logIt)
        {
            if (string.IsNullOrEmpty(psw)) return;
            //Update client settings
            UpdateSettings(UseImap);
            this.encryptedPassword = null;
            IsRequesting = true;
            //TODO Login
            await Task.Run(() =>
            {
                try
                {
                    //Ensure client created.
                    if (globalClient == null)
                        globalClient = clientCreator.Invoke();
                    if (globalClient.IsConnected)
                        globalClient.Disconnect();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        InboxList.Clear();
                    });
                    //Connect
                    string userAddress = UserAddress;
                    IsLandingIn = true;
                    //Connect to server.
                    globalClient.Connect(settings.Host, settings.Port, settings.EnableSsl);
                    //Login
                    if (!globalClient.Login(userAddress, psw, () =>
                    {
                        IsLandingIn = false;
                        this.encryptedPassword = psw.Protect();
                        this.IsLogined = true;
                        //Notify login state
                        Messenger.Default.Send(new NavigationMessage(), Tokens.Login);
                        Messenger.Default.Send(new DisplayMessage("Login Successful!", DisplayType.Toast));
                        //Delete old inbox folder
                        var dir = new DirectoryInfo(InboxFolder);
                        if (dir.Exists) dir.Delete(true);
                        //Do on logined
                        AppSettings.Default.LastLogUser = userAddress;
                        if (logIt)
                        {
                            //Log to local.
                            userAssistant.StoreUserLocal(userAddress, psw);
                            AppSettings.Default.LastLogUser = userAddress;
                            AppSettings.Default.AutoLogin = true;
                        }
                        AppSettings.Default.Save();
                        DisplayName = userAssistant.GetUserDisplayName(userAddress);
                        userAssistant.SaveToRecent(userAddress, DisplayName);
                        //End
                    }, msg =>
                    {
                        Application.Current.Dispatcher.Invoke(() => { InboxList.Add(msg); });
                    }))
                    {
                        Messenger.Default.Send(new DisplayMessage("Login Error, authenticate fail."));
                    }
                    IsLandingIn = false;
                }
                catch (Exception e)
                {
                    string tips =
                    globalClient == null ? "Create client fail."
                    : (globalClient.IsConnected ? (globalClient.IsAuthenticated ? "Fetch messages fail."
                    : "Login Error, authenticate fail.")
                    : "Connect to service fail.");
                    Messenger.Default.Send(new DisplayMessage(tips, e.Message));
                }
            });
            IsRequesting = false;
            IsLandingIn = false;
        }

        private void LogoutImpl()
        {
            ClearGlobalClient();
            this.IsLogined = false;
            this.encryptedPassword = null;
            InboxList.Clear();
            System.Diagnostics.Debug.WriteLine("Logout invoke");
        }

        //Ensure login and do something
        private IMailClient EnsureLogin(Action<MailMsgBase> onMessage = null)
        {
            //Connect
            if (globalClient == null)
                globalClient = clientCreator.Invoke();
            if (!globalClient.IsConnected)
                globalClient.Connect(settings.Host, settings.Port, settings.EnableSsl);
            if (!globalClient.IsAuthenticated)
            {
                if (onMessage == null)
                {
                    globalClient.Login(userAddress, Password);
                }
                else
                {
                    globalClient.Login(userAddress, Password, onMessage);
                }
            }
            else if (onMessage != null)
            {
                globalClient.FetchMessages(onMessage);
            }
            return globalClient;
        }

        //Ensure login and fetch new messages
        private bool EnsureLoginFetchMessage()
        {
            bool isSuccessful = false;
            try
            {
                var newMsgs = new List<MailMsgBase>();
                //Login
                EnsureLogin(msg =>
                {
                    newMsgs.Add(msg);
                });
                System.Diagnostics.Debug.WriteLine("Get new mail message succuessed.");
                System.Diagnostics.Debug.WriteLine($"newMsgs.Count : {newMsgs.Count}");
                var addList = new List<MailMsgBase>();
                var removeList = new List<MailMsgBase>();
                //Remove messages not exist from InboxList
                //Remove messages exist in InboxList from NewMsgs
                for (int i = 0; i < InboxList.Count; i++)
                {
                    var msg = InboxList[i];
                    var index = newMsgs.FindIndex(obj => obj.UID == msg.UID);
                    if (index < 0) removeList.Add(msg);
                    else
                    {
                        var newer = newMsgs[index];
                        msg.UpdateOrigin(newer);
                        newMsgs.RemoveAt(index);
                    }
                }
                //Update it
                Application.Current.Dispatcher.Invoke(() =>
                {
                    for (int i = 0; i < removeList.Count; i++)
                    {
                        InboxList.Remove(removeList[i]);
                        if (removeList[i] == null) continue;
                        System.Diagnostics.Debug.WriteLine($"Remvoe {removeList[i].UID}");
                    }
                    for (int i = 0; i < newMsgs.Count; i++)
                    {
                        if (newMsgs[i] == null) continue;
                        InboxList.Insert(0, newMsgs[i]);
                        Messenger.Default.Send(new DisplayMessage("New Mail",
                            newMsgs[i].From[i].ToString() + "\n" + newMsgs[i].Subject,
                            DisplayType.Toast));
                        System.Diagnostics.Debug.WriteLine($"Add {newMsgs[i].UID}");
                    }
                });
                isSuccessful = true;
            }
            catch (Exception e)
            {
                Messenger.Default.Send(new DisplayMessage("Error on get mail.", e.Message));
                isSuccessful = false;
            }
            return isSuccessful;
        }

        //Ensure a file is created
        private void EnsureCreateFile(string path)
        {
            try
            {
                if (File.Exists(path)) return;
                File.Create(path);
            }
            catch (Exception e)
            {
                Messenger.Default.Send(new DisplayMessage("Creata File Fail", e.Message, DisplayType.Toast));
            }
        }

        //Ensure a folder is created
        private void EnsureCreateFolder(string path)
        {
            try
            {
                if (Directory.Exists(path)) return;
                Directory.CreateDirectory(path);
            }catch(Exception e)
            {
                Messenger.Default.Send(new DisplayMessage("Creata Folder Fail", e.Message, DisplayType.Toast));
            }
        }

        //Generate mail id for draft use Guid.NewGuid();
        public string CreateId()
        {
            return Guid.NewGuid().ToString("N");
        }

        #region  ************** Draft function **************

        public void AddDraft(MailBase mail)
        {
            if (null == mail) return;
            if (DraftList.Contains(mail)) return;
            if (string.IsNullOrEmpty(mail.Subject) &&
                string.IsNullOrEmpty(mail.BodyHtml) &&
                string.IsNullOrEmpty(mail.BodyText))
                return;
            if (string.IsNullOrEmpty(mail.ID))
                mail.ID = CreateId();
            System.Diagnostics.Debug.WriteLine("Add " + mail.ID);
            DraftList.Add(mail);
            SerializeAssistant.Serialize(draftBinPath, DraftList);
        }

        public void SaveDraftChanges(MailBase mail)
        {
            string mailId = mail.ID;
            if (string.IsNullOrEmpty(mailId))
            {
                mail.ID = CreateId();
                this.AddDraft(mail);
            }
            else
            {
                var existMail = DraftList.FirstOrDefault(item =>
                {
                    return item.ID == mailId;
                });
                if (existMail == null) this.AddDraft(mail);
                else
                {
                    System.Diagnostics.Debug.WriteLine("Save changes of " + mail.ID);
                    if (string.IsNullOrEmpty(mail.Subject) &&
                        string.IsNullOrEmpty(mail.BodyHtml) &&
                        string.IsNullOrEmpty(mail.BodyText))
                    {
                        RemoveDraft(mail.ID);
                    }
                    else
                    {
                        existMail.SyncButNoIdTo(mail);
                        SerializeAssistant.Serialize(draftBinPath, DraftList);
                    }
                }
            }
        }

        public void RemoveDraft(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            //Get exist
            var existMail = DraftList.FirstOrDefault(item =>
            {
                return item.ID == id;
            });
            if (existMail == null) return;
            //Delete Attachments
            string folder = DraftFolder.CombinePath(id);
            var dirInfo = new DirectoryInfo(folder);
            if (dirInfo.Exists) dirInfo.Delete(true);
            //Delete Mail Content
            DraftList.Remove(existMail);
            SerializeAssistant.Serialize(draftBinPath, DraftList);
            System.Diagnostics.Debug.WriteLine("Remove " + existMail.ID);
        }

        #endregion **************

        private void SaveOutboxLocal()
        {
            string folder = DataFolder.CombinePath(UserAddress);
            EnsureCreateFolder(folder);
            string path = folder.CombinePath("Out.bin");
            EnsureCreateFile(path);
            SerializeAssistant.Serialize(path, OutboxList);
        }

        public void Dispose()
        {
            Logout();
        }
    }
}
