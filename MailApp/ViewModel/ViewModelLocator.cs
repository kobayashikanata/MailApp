using MailApp.ViewModel;

namespace MailApp
{
    public class ViewModelLocator
    {
        private static MainViewModel _main;
        public static MainViewModel Main
        {
            get
            {
                if (_main == null)
                    _main = new MainViewModel();
                return _main;
            }
        }
    }
}
