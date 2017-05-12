using System;

namespace MailApp
{
    public class NavigationMessage
    {
        public Type PageType { get; set; }
        public object Extra { get; set; }
        public string ExtraKey { get; set; }
        public NavigationMessage() { }
        public NavigationMessage(Type pageType = null, object extra = null, string extraKey = null)
        {
            this.PageType = pageType;
            this.Extra = extra;
            this.ExtraKey = extraKey;
        }
    }
}
