using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailApp.ViewModel
{
    public class NotificationObjectBasic : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(String propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public abstract class NotificationObject : NotificationObjectBasic
    {
        public NotificationObject()
        {
            this.Init();
        }

        protected abstract void Init();
    }
}
