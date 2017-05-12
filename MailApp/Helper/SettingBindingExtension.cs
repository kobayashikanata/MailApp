using System.Windows.Data;

namespace MailApp
{
    public class SettingBindingExtension : Binding
    {
        public SettingBindingExtension()
        {
            Initialize();
        }

        public SettingBindingExtension(string path)
            : base(path)
        {
            Initialize();
        }

        private void Initialize()
        {
            this.Source = MailApp.Properties.Settings.Default;
            this.Mode = BindingMode.TwoWay;
        }
    }
}
