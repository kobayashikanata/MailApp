using System;
using System.Collections.Generic;
using ImapX;
using System.Linq;
using System.IO;

namespace Mail
{
    public class ImapClient : IMailClient
    {
        private ImapX.ImapClient client;

        public bool IsConnected => client.IsConnected;
        public bool IsAuthenticated => client.IsAuthenticated;

        public ImapClient()
        {
            client = new ImapX.ImapClient();
            client.Behavior.MessageFetchMode = ImapX.Enums.MessageFetchMode.Tiny;
            client.Behavior.ExamineFolders = false;
            client.Behavior.FolderTreeBrowseMode = ImapX.Enums.FolderTreeBrowseMode.Lazy;
        }

        public bool Connect(string host, int port, bool useSsl)
        {
            return client.Connect(host, port, useSsl);
        }

        public void Disconnect()
        {
            client.Disconnect();
        }

        public bool Login(string user, string psw)
        {
            return client.Login(user, psw);
        }

        public bool Login(string user, string psw, Action onLogined)
        {
            if (client.Login(user, psw))
            {
                onLogined?.Invoke();
                return true;
            }
            return false;
        }

        public bool Login(string user, string psw, Action<MailMsgBase> onMessage)
        {
            return Login(user, psw, null, onMessage);
        }

        public bool Login(string user, string psw, Action onLogined, Action<MailMsgBase> onMessage)
        {
            if (client.Login(user, psw))
            {
                onLogined?.Invoke();
                FetchMessages(onMessage);
                return true;
            }
            return false;
        }

        public bool Logout()
        {
            return client.Logout();
        }

        public void Dispose()
        {
            client.Dispose();
        }
        
        public bool DeleteMessage(string uid)
        {
            return DeleteMessage("INBOX", uid);
        }

        public bool DeleteMessage(string folder, string uid)
        {
            if (string.IsNullOrEmpty(uid))
                return false;
            var folderCache = client.Folders[folder];
            if (!folderCache.Selectable) return false;
            folderCache.Select();
            long id = long.Parse(uid);
            foreach (var msg in folderCache.Messages)
            {
                if (msg.UId == id)
                {
                    msg.Remove();
                    return true;
                }
            }
            return false;
        }

        public bool DeleteMessage(MailMsgBase msg)
        {
            if (msg == null) return false;
            var msgx = msg as ImapMsg;
            if (msgx != null)
                msgx.origin.Remove();
            return DeleteMessage(msg.UID);
        }

        public List<MailMsgBase> FetchMessages()
        {
            return FetchMessages("INBOX");
        }

        public List<MailMsgBase> FetchMessages(string folder)
        {
            var msgs = client.Folders[folder].Search("ALL", ImapX.Enums.MessageFetchMode.Tiny);
            var list = new List<MailMsgBase>(msgs.Length);
            for(int i = 0; i < msgs.Length; i++)
            {
                list.Add(new ImapMsg(msgs[i]));
            }
            return list;
        }

        public void Update(MailMsgBase origin, MailMsgBase newer)
        {
            var originx = origin as ImapMsg;
            var newerx = newer as ImapMsg;
            if (origin != null && newerx != null)
                originx.Update(newerx.origin);
        }

        public void FetchMessages(Action<MailMsgBase> onMessage)
        {
            FetchMessages("INBOX", onMessage);
        }

        public void FetchMessages(string folder, Action<MailMsgBase> onMessage)
        {
            var msgs = client.Folders[folder].Search("ALL", ImapX.Enums.MessageFetchMode.Tiny);
            for (int i = 0; i < msgs.Length; i++)
            {
                var newMsg = new ImapMsg(msgs[i]);
                onMessage?.Invoke(newMsg);
            }
        }

        public class ImapAttachment : IAttachment
        {
            private Attachment origin;
            internal ImapAttachment(Attachment origin)
            {
                this.origin = origin;
            }

            public byte[] FileDate => origin?.FileData;

            public string FileName => origin?.FileName;

            public bool Save(string path)
            {
                if (origin == null) return false;
                if (!origin.Downloaded)
                    origin.Download();
                File.WriteAllBytes(path, origin.FileData);
                return true;
            }
        }

        public class ImapMsg : MailMsgBase
        {
            internal Message origin;

            public new string Body
            {
                get
                {
                    return origin.Body.HasHtml 
                        ? origin.Body.Html 
                        :  origin.Body.Text;
                }set { }
            }

            public new string BodyHtmlText
            {
                get { return origin.Body.Html; }
                set { }
            }

            public new string BodyText
            {
                get { return origin.Body.Text; }
                set { }
            }

            private List<IAttachment> attachmentsCache;
            public new List<IAttachment> Attachments
            {
                get
                {
                    if (origin == null) return null;
                    if (attachmentsCache == null)
                    {
                        attachmentsCache = new List<IAttachment>();
                        for(int i = 0; i < origin.Attachments.Length; i++)
                        {
                            attachmentsCache.Add(new ImapAttachment(origin.Attachments[i]));
                        }
                    }
                    return attachmentsCache;
                }
                private set { attachmentsCache = value; }
            }

            internal ImapMsg(Message imapMsg)
            {
                Update(imapMsg);
            }
            
            private string RemoveNeedless(string text)
            {
                for (var i = text.Length - 1; i >= 0; i--)
                {
                    if (text[i] == ')')
                    {
                        return text.Remove(i, text.Length - i);
                    }
                }
                return text;
            }

            internal void Update(Message imapMsg)
            {
                this.origin = imapMsg;
                this.attachmentsCache = null;
                UID = origin.UId.ToString();
                MsgID = origin.MessageId;
                Date = origin.Date;
                Subject = RemoveNeedless(origin.Subject);
                From = new MailboxList();
                From.Add(new Mailbox
                {
                    DisplayName = origin.From.DisplayName,
                    Address = origin.From.Address
                });
                if(origin.To != null && origin.To.Count > 0)
                {
                    if (To != null)
                        To.Clear();
                    else
                        To = new AddressList();
                    foreach (var address in origin.To)
                    {
                        To.Add(address.Address);
                    }
                }
                else this.To = null;

                if (origin.Cc != null && origin.Cc.Count > 0)
                {
                    if (Cc != null)
                        Cc.Clear();
                    else
                        Cc = new AddressList();
                    foreach (var address in origin.Cc)
                    {
                        this.Cc.Add(address.ToString());
                    }
                }
                else this.Cc = null;
                this.attachmentsCache = null;
            }

            public override void UpdateOrigin(object obj)
            {
                var newer = obj as ImapMsg;
                if(newer != null)
                {
                    System.Diagnostics.Debug.WriteLine("Update " + newer.UID);
                    Update(newer.origin);
                }
            }
        }

    }
}
