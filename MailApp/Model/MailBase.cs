using System;
using System.ComponentModel;

namespace MailApp
{
    [Serializable]
    public class MailBase : INotifyPropertyChanged
    {
        private string id;
        private string toAddresses;
        private string ccAddresses;
        private string bccAddresses;
        private string subject;
        private string bodyHtml;
        private string bodyText;
        public string ID
        {
            get { return id; }
            set
            {
                id = value;
                RaisePropertyChanged(nameof(ID));
            }
        }
        public string ToAddresses
        {
            get { return toAddresses; }
            set
            {
                toAddresses = value;
                RaisePropertyChanged(nameof(ToAddresses));
            }
        }
        public string CCAddresses
        {
            get { return ccAddresses; }
            set
            {
                ccAddresses = value;
                RaisePropertyChanged(nameof(CCAddresses));
            }
        }
        public string BccAddresses
        {
            get { return bccAddresses; }
            set
            {
                bccAddresses = value;
                RaisePropertyChanged(nameof(BccAddresses));
            }
        }
        public string Subject
        {
            get { return subject; }
            set
            {
                subject = value;
                RaisePropertyChanged(nameof(Subject));
            }
        }
        public string BodyHtml
        {
            get { return bodyHtml; }
            set
            {
                bodyHtml = value;
                RaisePropertyChanged(nameof(BodyHtml));
            }
        }
        public string BodyText
        {
            get { return bodyText; }
            set
            {
                bodyText = value;
                RaisePropertyChanged(nameof(BodyText));
            }
        }
        private BindingList<string> attachments;
        public BindingList<string> Attachments
        {
            get
            {
                if (attachments == null)
                    attachments = new BindingList<string>();
                return attachments;
            }
            set
            {
                if (attachments != value)
                {
                    attachments = value;
                    RaisePropertyChanged(nameof(Attachments));
                }
            }
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(String propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SyncButNoIdTo(MailBase mail)
        {
            this.ToAddresses = mail.ToAddresses;
            this.Subject = mail.Subject;
            this.BodyHtml = mail.BodyHtml;
            this.BodyText = mail.BodyText;
            this.Attachments = mail.Attachments;
        }
    }

    [Serializable]
    public class MailModel : MailBase
    {
        public string FromAddress { get; set; }
        public string DisplayName { get; set; } = "Hello world";
        public System.Net.Mail.MailPriority priority = System.Net.Mail.MailPriority.Normal;
    }
}
