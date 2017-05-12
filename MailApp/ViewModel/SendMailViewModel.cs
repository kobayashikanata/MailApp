using MessengerLight;
using MessengerLight.Command;
using System;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.IO;

namespace MailApp.ViewModel
{
    public class SendMailViewModel : NotificationObject
    {
        private bool saveNeedless;

        #region **************  Properties **************
        public MainViewModel MainVm => ViewModelLocator.Main;
        private MailModel mailMsg;
        public MailModel MailMessage
        {
            get
            {
                if (mailMsg == null)
                {
                    mailMsg = new MailModel();
                    mailMsg.BodyHtml = "";
                    mailMsg.FromAddress = MainVm.UserAddress;
                }
                return mailMsg;
            }
            set
            {
                if (mailMsg != value)
                {
                    mailMsg = value;
                    RaisePropertyChanged(nameof(MailMessage));
                }
            }
        }
        public string Password => MainVm.Password;
        public string FromAddress => MailMessage.FromAddress;
        public string DisplayName => MainVm.DisplayName;

        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public MailPriority Priority
        {
            get { return MailMessage.priority; }
            set
            {
                if (MailMessage.priority != value)
                {
                    MailMessage.priority = value;
                    RaisePropertyChanged(nameof(Priority));
                }
            }
        }
        public IClientSettings Settings => App.DefaultSettings.SmtpSettings;
        private string DraftFolder => MainVm.DraftFolder;

        private bool isDraft = true;
        public bool IsDraft
        {
            get
            {
                return isDraft;
            }
            set
            {
                if (isDraft != value)
                {
                    isDraft = value;
                    RaisePropertyChanged(nameof(IsDraft));
                    SaveDraftCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool isCCOrBccEnabled;
        public bool IsCCOrBccEnabled
        {
            get
            {
                return isCCOrBccEnabled;
            }
            set
            {
                if (isCCOrBccEnabled != value)
                {
                    isCCOrBccEnabled = value;
                    RaisePropertyChanged(nameof(IsCCOrBccEnabled));
                }
            }
        }

        #endregion

        #region **************  Commands **************

        public RelayCommand<object> SendMailCommand { get; set; }
        public RelayCommand<object> SaveDraftCommand { get; set; }
        public RelayCommand DiscardCommand { get; set; }
        public RelayCommand<object> DeleteAttachmentCommand { get; set; }
        public RelayCommand<object> AddAttachmentCommand { get; set; }

        #endregion

        protected override void Init()
        {
            SendMailCommand = new RelayCommand<object>(SendMail);
            DiscardCommand = new RelayCommand(DiscardDraft);
            SaveDraftCommand = new RelayCommand<object>(SaveDraft);
            DeleteAttachmentCommand = new RelayCommand<object>(DeleteAttachment);
            AddAttachmentCommand = new RelayCommand<object>(AddAttachment);
        }

        public void LoadBase(MailBase mail)
        {
            this.MailMessage.SyncButNoIdTo(mail);
            this.MailMessage.ID = mail.ID;
        }

        private void DeleteAttachment(object o)
        {
            //Validate basic info
            if (null == o) return;
            var fileName = o as string;
            if (null == fileName) return;
            if (!MailMessage.Attachments.Contains(fileName)) return;
            if (string.IsNullOrEmpty(MailMessage.ID)) return;
            //Check file
            string folder = DraftFolder.CombinePath(MailMessage.ID);
            string path = folder.CombinePath(fileName);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    MailMessage.Attachments.Remove(fileName);
                }
                catch (Exception e)
                {
                    Messenger.Default.Send(new DisplayMessage(e.Message, DisplayType.Toast));
                }
            }
        }

        private void AddAttachment(object o)
        {
            //Validate basic info
            if (null == o) return;
            string[] filesPath = null;
            var filePath = o as string;
            if (null == filePath)
            {
                filesPath = o as string[];
                if (null == filesPath) return;
            }
            else filesPath = new string[] { filePath };
            //Validate id generated
            if (string.IsNullOrEmpty(MailMessage.ID))
                MailMessage.ID = MainVm.CreateId();
            //Ensure cache folder is generated
            string folder = EnsureDraftCacheFolder(MailMessage.ID);
            //Attach file
            string copyTarget = null;
            foreach (var path in filesPath)
            {
                FileInfo info = new FileInfo(path);
                if (!info.Exists) return;
                try
                {
                    copyTarget = folder.CombinePath(info.Name);
                    if (File.Exists(copyTarget))
                    {
                        Messenger.Default.Send(new DisplayMessage("Exist file!", DisplayType.Toast));
                    }
                    else
                    {
                        info.CopyTo(copyTarget);
                        MailMessage.Attachments.Add(info.Name);
                    }
                }
                catch (Exception e)
                {
                    Messenger.Default.Send(new DisplayMessage(e.Message, DisplayType.Toast));
                }
            }
        }

        public string EnsureDraftCacheFolder(string messageid)
        {
            string path = DraftFolder.CombinePath(messageid);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        private void SaveDraft(object o)
        {
            if (saveNeedless) return;
            if (IsDraft)
            {
                var doc = o as FlowDocument;
                if (doc == null) return;
                string bodyHtml = doc.ToHtmlString();
                string bodyText = doc.GetText();
                MainVm.SaveDraftChanges(new MailBase
                {
                    ID = MailMessage.ID,
                    Subject = MailMessage.Subject,
                    ToAddresses = MailMessage.ToAddresses,
                    BodyHtml = bodyHtml,
                    BodyText = bodyText,
                    Attachments = MailMessage.Attachments
                });
                saveNeedless = true;
            }
            else
            {
                DiscardDraft();
            }
        }

        private void DiscardDraft()
        {
            IsDraft = false;
            if (string.IsNullOrEmpty(MailMessage.ID)) return;
            string folder = DraftFolder.CombinePath(MailMessage.ID);
            var dirInfo = new DirectoryInfo(folder);
            if (dirInfo.Exists)
            {
                try
                {
                    dirInfo.Delete(true);
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
            MainVm.RemoveDraft(MailMessage.ID);
            MailMessage.ID = null;

            Messenger.Default.Send(new DisplayMessage("Discard draft.", DisplayType.Toast));
            Messenger.Default.Send(new NavigationMessage(), Tokens.Draft);
        }

        private bool AddAddresses(string addresses, MailAddressCollection target, ref string error)
        {
            if (string.IsNullOrEmpty(addresses)) return false;
            var addres = addresses.Split(';');
            var size = addres.Length;
            foreach (var addr in addres)
            {
                if (string.IsNullOrWhiteSpace(addr)) continue;
                try
                {
                    target.Add(addr);
                    --size;
                }
                catch (Exception e)
                {
                    error = e.Message + "\nIn: " + addr;
                    return false;
                }
            }
            if (size < 0) return false;
            return true;
        }

        private async void SendMail(object o)
        {
            if (!MainVm.IsLogined)
            {
                Messenger.Default.Send(new DisplayMessage("Require Login.", "Please Login First!"));
                return;
            }
            //Addresses Validation
            var error = string.Empty;
            var msg = new MailMessage();
            //Ensure To
            if (!AddAddresses(MailMessage.ToAddresses, msg.To, ref error))
            {
                if (!string.IsNullOrEmpty(error))
                {
                    Messenger.Default.Send(new DisplayMessage("Check recipients",
                        error));
                    return;
                }
                else if(!IsCCOrBccEnabled) return;
            }
            //Ensure CC or Bcc
            if (IsCCOrBccEnabled 
                && ((!string.IsNullOrEmpty(MailMessage.CCAddresses)
                    && !AddAddresses(MailMessage.CCAddresses, msg.CC, ref error))
                || (!string.IsNullOrEmpty(MailMessage.BccAddresses)
                    && !AddAddresses(MailMessage.BccAddresses, msg.Bcc, ref error))))
            {
                Messenger.Default.Send(new DisplayMessage("Check recipients",
                    error));
                return;
            }
            //Ensure valid recipients
            if (msg.To.Count == 0 && msg.CC.Count == 0 && msg.Bcc.Count == 0)
            {
                Messenger.Default.Send(new DisplayMessage("No valid recipients",
                    "Please input recipients"));
                return;
            }
            //Get Document to send
            var doc = o as FlowDocument;
            if (doc == null)
            {
                Messenger.Default.Send(new DisplayMessage("Check input", "Error body."));
                return;
            }
            string bodyHtml = doc.ToHtmlString();
            //Initilize mail message
            msg.From = new MailAddress(FromAddress, DisplayName);
            msg.Priority = Priority;
            msg.Subject = MailMessage.Subject;
            msg.Body = bodyHtml;
            msg.BodyEncoding = Encoding;
            msg.IsBodyHtml = true;
            //Add Attachments
            string folder = DraftFolder.CombinePath(MailMessage.ID);
            if (MailMessage.Attachments.Count > 0)
            {
                string fileCache = null;
                foreach (var attachment in MailMessage.Attachments)
                {
                    fileCache = folder.CombinePath(attachment);
                    if (!File.Exists(fileCache)) continue;
                    msg.Attachments.Add(new Attachment(fileCache));
                }
            }
            //Initilize SmtpClient
            var client = new SmtpClient();
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Host = Settings.Host;
            client.Port = Settings.Port;
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential(FromAddress, Password);
            client.EnableSsl = Settings.EnableSsl;
            await Task.Run(() =>
            {
                MainVm.IsRequesting = true;
                try
                {
                    client.Send(msg);
                    IsDraft = false;
                    Messenger.Default.Send(new NavigationMessage(), Tokens.Draft);
                    Messenger.Default.Send(new DisplayMessage("Send Successful!"));
                }
                catch (Exception e)
                {
                    Messenger.Default.Send(new DisplayMessage("Send Error", e.Message));
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
                finally
                {
                    if (client != null)
                        client.Dispose();
                    MainVm.IsRequesting = false;
                }
            });
        }

    }

}
