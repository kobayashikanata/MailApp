using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MailApp.Views
{
    /// <summary>
    /// Interaction logic for LoginPane.xaml
    /// </summary>
    public partial class LoginPane : Epxoxy.Controls.MaskControl, IOverlay
    {
        public LoginPane()
        {
            InitializeComponent();
            this.Loaded += OnThisLoaded;
        }

        private void OnThisLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnThisLoaded;
            MessengerLight.Messenger.Default.Register<NavigationMessage>(this, Tokens.Login, OnNavigationMessage);
            userList.ItemsSource = App.DefaultSettings.HistoryUsers.Keys;
            this.Unloaded += OnThisUnloaded;
        }

        private void OnNavigationMessage(NavigationMessage msg)
        {
            Dispatcher.Invoke(() => { this.IsOpen = false; });
        }

        private void OnThisUnloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= OnThisUnloaded;
            MessengerLight.Messenger.Default.Unregister(this);
            this.DataContext = null;
        }

        private void CancelBtnClick(object sender, RoutedEventArgs e)
        {
            this.IsOpen = false;
        }

        protected override void OnIsOpenChanged(bool isOpen)
        {
            base.OnIsOpenChanged(isOpen);
            if (!isOpen && tcsOfClose != null)
                tcsOfClose.TrySetResult(null);
        }

        private TaskCompletionSource<object> tcsOfClose;
        public Task WaitForClose()
        {
            tcsOfClose = new TaskCompletionSource<object>();
            if (!this.IsOpen)
            {
                tcsOfClose.TrySetResult(null);
            }
            return tcsOfClose.Task;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            switch (e.Key)
            {
                case Key.Return:
                    var focused = Keyboard.FocusedElement;
                    if (focused != cancelBtn && focused != loginBtn)
                    {
                        UIElement element = Keyboard.FocusedElement as UIElement;
                        if (element != null)
                        {
                            var request = new TraversalRequest(FocusNavigationDirection.Next);
                            element.MoveFocus(request);
                        }
                        e.Handled = true;
                        if(loginBtn.IsFocused || logMe.IsFocused)
                        {
                            loginBtn.Command.Execute(loginBtn.CommandParameter);
                        }
                    }
                    break;
                case Key.Escape:
                    this.IsOpen = false;
                    e.Handled = true;
                    break;
                default:break;
            }
        }

        private void RmFromRecentBtnClick(object sender, RoutedEventArgs e)
        {
            string key = (sender as Button).CommandParameter as string;
            if (App.DefaultSettings.HistoryUsers.ContainsKey(key))
            {
                App.DefaultSettings.HistoryUsers.Remove(key);
                userList.ItemsSource = null;
                userList.ItemsSource = App.DefaultSettings.HistoryUsers.Keys;
            }
        }
    }
}
