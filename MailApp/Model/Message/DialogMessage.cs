using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailApp
{
    public enum DisplayType
    {
        Dialog,
        Toast
    }
    public class DisplayMessage
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public DisplayType Type { get; set; }
        public Action OnDismiss { get; set; }
        public Action OnOk { get; set; }
        public DisplayMessage(DisplayType type = DisplayType.Dialog)
        {
            Title = "Message";
            Type = type;
        }
        public DisplayMessage(string message, DisplayType type = DisplayType.Dialog): this(type)
        {
            Message = message;
        }
        public DisplayMessage(string title, string message, DisplayType type = DisplayType.Dialog)
        {
            Message = message;
            Title = title;
            Type = type;
        }
    }
}
