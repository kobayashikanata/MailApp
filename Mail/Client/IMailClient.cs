using System;
using System.Collections.Generic;

namespace Mail
{
    public interface IMailClient : IDisposable
    {
        bool IsConnected { get; }
        bool IsAuthenticated { get; }
        bool Connect(string host, int port, bool useSsl);
        void Disconnect();
        bool Login(string user, string psw);
        bool Login(string user, string psw, Action onLogined);
        bool Login(string user, string psw, Action<MailMsgBase> onMessage);
        bool Login(string user, string psw, Action onLogined, Action<MailMsgBase> onMessage);
        bool Logout();
        List<MailMsgBase> FetchMessages();
        List<MailMsgBase> FetchMessages(string folder);
        void FetchMessages(Action<MailMsgBase> onMessage);
        void FetchMessages(string folder, Action<MailMsgBase> onMessage);
        bool DeleteMessage(string uid);
        bool DeleteMessage(string folder, string uid);
        bool DeleteMessage(MailMsgBase msg);
    }
}
