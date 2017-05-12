using System;
using System.Collections.Generic;

namespace Mail
{
    public class MailMsgBase
    {
        public string UID { get; set; }
        public string MsgID { get; set; }
        public string Subject { get; set; }
        public object Body { get; set; }
        public string BodyHtmlText { get; set; }
        public string BodyText { get; set; }
        public bool IsBodyHtml { get; }
        public DateTime? Date { get; set; }
        public MailboxList From { get; set; }
        public AddressList To { get; set; }
        public AddressList Cc { get; set; }
        public List<IAttachment> Attachments { get; set; }
        public byte[] MessageData { get; set; }
        public object Origin { get; internal set; }
        public virtual void UpdateOrigin(object obj)
        {
        }
    }

    public class Mailbox
    {
        public string DisplayName { get; internal set; }
        public string Address { get; internal set; }
        public string Domain { get; internal set; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(DisplayName) 
                ? Address 
                : string.Format("{0} <{1}>", DisplayName, Address);
        }
    }

    public class MailboxList : List<Mailbox>
    {
        public override string ToString()
        {
            var retVal = new System.Text.StringBuilder();
            for (int i = 0; i < this.Count; i++)
            {
                if (i == (this.Count - 1))
                {
                    retVal.Append(this[i]);
                }
                else
                {
                    retVal.Append(this[i] + ";");
                }
            }
            return retVal.ToString();
        }
    }

    public class AddressList : List<string>
    {
        public override string ToString()
        {
            var retVal = new System.Text.StringBuilder();
            for (int i = 0; i < this.Count; i++)
            {
                if (i == (this.Count - 1))
                {
                    retVal.Append(this[i]);
                }
                else
                {
                    retVal.Append(this[i] + ",");
                }
            }
            return retVal.ToString();
        }
    }
}
