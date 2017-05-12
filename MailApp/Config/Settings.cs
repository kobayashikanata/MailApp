using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailApp
{
    public interface IClientSettings
    {
        int Port { get; }
        string Host { get; }
        bool EnableSsl { get; }
    }

    [Serializable]
    public class Pop3Settings : IClientSettings
    {
        public int Port { get; set; } = 110;
        public string Host { get; set; } = "pop.163.com";//ym
        public bool EnableSsl { get; set; }
        public Pop3Settings() { }
    }

    [Serializable]
    public class SmtpSettings : IClientSettings
    {
        public string Host { get; set; } = "smtp.163.com";//ym
        public int Port { get; set; } = 25;
        public bool EnableSsl { get; set; }
        public SmtpSettings() { }
    }

    [Serializable]
    public class ImapSettings : IClientSettings
    {
        public string Host { get; set; } = "imap.163.com";//ym
        public int Port { get; set; } = 143;
        public bool EnableSsl { get; set; }
        public ImapSettings() { }
    }

}
