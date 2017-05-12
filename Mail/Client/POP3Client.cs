using LumiSoft.Net.Mail;
using LumiSoft.Net.MIME;
using LumiSoft.Net.POP3.Client;
using System;
using System.Collections.Generic;
using System.IO;

namespace Mail
{
    public class POP3Client : IMailClient
    {
        private POP3_Client client;

        public bool IsConnected => client.IsConnected;
        public bool IsAuthenticated => client.IsAuthenticated;

        public POP3Client()
        {
            client = new POP3_Client();
        }

        public bool Connect(string host, int port, bool useSsl)
        {
            client.Connect(host, port, useSsl);
            return true;
        }

        public void Disconnect()
        {
            client.Disconnect();
        }

        public bool Login(string user, string psw)
        {
            client.Login(user, psw);
            return true;
        }

        public bool Login(string user, string psw, Action onLogined)
        {
            client.Login(user, psw);
            onLogined?.Invoke();
            return true;
        }

        public bool Login(string user, string psw, Action<MailMsgBase> onMessage)
        {
            return Login(user, psw, null, onMessage);
        }

        public bool Login(string user, string psw, Action onLogined, Action<MailMsgBase> onMessage)
        {
            Login(user, psw, onLogined);
            var msg = client.Messages;
            FetchMessages(onMessage);
            return true;
        }

        public bool Logout()
        {
            if (client.IsConnected)
                client.Disconnect();
            return true;
        }

        public void Dispose()
        {
            client.Dispose();
        }

        public bool DeleteMessage(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return false;
            foreach (POP3_ClientMessage clientMsg in client.Messages)
            {
                if (clientMsg.UID == uid)
                {
                    clientMsg.MarkForDeletion();
                }
            }
            client.Disconnect();
            return true;
        }

        public bool DeleteMessage(string folder, string uid)
        {
            return DeleteMessage(uid);
        }

        public bool DeleteMessage(MailMsgBase msg)
        {
            return DeleteMessage(msg.UID);
        }
        
        public List<MailMsgBase> FetchMessages()
        {
            return FetchMessages(string.Empty);
        }

        //Folder will be ingored in pop3 because of not support.
        public List<MailMsgBase> FetchMessages(string folder)
        {
            if(client.Messages.Count > 0)
            {
                var msgs = new List<MailMsgBase>(client.Messages.Count);
                for (int i = client.Messages.Count - 1; i > -1; i--)
                {
                    var pop3Msg = client.Messages[i];
                    string uid = pop3Msg.UID;
                    byte[] bytes = pop3Msg.MessageToByte();
                    var origin = Mail_Message.ParseFromByte(bytes);
                    msgs.Add(new Pop3Msg(uid, origin));
                }
                return msgs;
            }
            return null;
        }

        public void FetchMessages(Action<MailMsgBase> onMessage)
        {
            if (client.Messages.Count > 0)
            {
                var msgs = new List<MailMsgBase>(client.Messages.Count);
                for (int i = client.Messages.Count - 1; i > -1; i--)
                {
                    var pop3Msg = client.Messages[i];
                    string uid = pop3Msg.UID;
                    byte[] bytes = pop3Msg.MessageToByte();
                    var origin = Mail_Message.ParseFromByte(bytes);
                    var newMsg = new Pop3Msg(uid, origin);
                    msgs.Add(newMsg);
                    onMessage?.Invoke(newMsg);
                }
            }
        }

        public void FetchMessages(string folder, Action<MailMsgBase> onMessage)
        {
            FetchMessages(onMessage);
        }
        
        public class Pop3Msg : MailMsgBase
        {
            internal Mail_Message origin;

            public new object Body
            {
                get { return origin.Body; }
                set { }
            }

            public new string BodyHtmlText
            {
                get { return origin.BodyHtmlText; }
                set { }
            }

            public new string BodyText
            {
                get { return origin.BodyText; }
                set { }
            }
            
            internal Pop3Msg(string uid, Mail_Message pop3Msg)
            {
                this.UID = uid;
                Update(pop3Msg);
            }

            internal void Update(Mail_Message origin)
            {
                this.origin = origin;
                this.MsgID = origin.MessageID;
                this.Subject = origin.Subject;
                this.Date = origin.Date;
                if (origin.From != null && origin.From.Count > 0)
                {
                    this.From = new MailboxList();
                    for (int i = 0; i < origin.From.Count; i++)
                    {
                        this.From.Add(new Mailbox
                        {
                            DisplayName = origin.From[i].DisplayName,
                            Address = origin.From[i].Address,
                            Domain = origin.From[i].Domain
                        });
                    }
                }
                if (origin.To != null && origin.To.Count > 0)
                {
                    this.To = new AddressList();
                    foreach (Mail_t_Address address in origin.To)
                    {
                        this.To.Add(address.ToString());
                    }
                }
                else this.To = null;

                if (origin.Cc != null && origin.Cc.Count > 0)
                {
                    this.Cc = new AddressList();
                    foreach (Mail_t_Address address in origin.Cc)
                    {
                        this.Cc.Add(address.ToString());
                    }
                }
                else this.Cc = null;

                if (origin.Attachments != null && origin.Attachments.Length > 0)
                {
                    this.Attachments = new List<IAttachment>();
                    for (int i = 0; i < origin.Attachments.Length; i++)
                    {
                        this.Attachments.Add(new Pop3Attachment(origin.Attachments[i]));
                    }
                }
                else this.Attachments = null;
            }

            public override void UpdateOrigin(object obj)
            {
                var newer = obj as Pop3Msg;
                if (newer != null)
                {
                    this.UID = newer.UID;
                    System.Diagnostics.Debug.WriteLine("Update " + newer.UID);
                    Update(newer.origin);
                }
            }
        }

        public class Pop3Attachment : IAttachment
        {
            private MIME_Entity origin;
            private string fileName;
            internal Pop3Attachment(MIME_Entity origin)
            {
                this.origin = origin;
                if (origin != null)
                {
                    if (origin.ContentDisposition == null ||
                        origin.ContentDisposition.Param_FileName == null)
                    {
                        fileName = origin.ContentType.Param_Name;
                    }
                    else
                    {
                        fileName = origin.ContentDisposition.Param_FileName;
                    }
                }
            }

            public byte[] FileDate => origin == null ? null : ((MIME_b_SinglepartBase)origin.Body).Data;

            public string FileName => fileName;

            public bool Save(string path)
            {
                if (origin == null) return false;
                if (origin.IsDisposed) return false;
                File.WriteAllBytes(path, ((MIME_b_SinglepartBase)origin.Body).Data);
                return true;
            }
        }

    }
}
