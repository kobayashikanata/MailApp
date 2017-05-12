using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace MailApp.Views
{
    /// <summary>
    /// Interaction logic for DraftPage.xaml
    /// </summary>
    public partial class DraftPage : Page
    {
        public DraftPage()
        {
            InitializeComponent();
            this.Loaded += OnThisLoaded;
        }

        private void OnThisLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnThisLoaded;
            this.Unloaded += OnThisUnloaded;
            MessengerLight.Messenger.Default.Register<NavigationMessage>(this, Tokens.Draft, OnNavigationMessage);
            DraftListViewSource.ItemsSource = ViewModelLocator.Main.DraftList;
            this.DraftDetailFrame.NavigationService.Navigating += OnDetailFrameNavigating;
            var extra = NavigationHelper.Pop(Tokens.NavEdit);
            if(extra != null)
            {
                NaviagteDetail(extra);
            }
        }

        private void OnThisUnloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= OnThisUnloaded;
            this.DraftDetailFrame.Navigating -= OnDetailFrameNavigating;
            MessengerLight.Messenger.Default.Unregister(this);
            DraftListViewSource.ItemsSource = null;
        }

        private void OnNavigationMessage(NavigationMessage msg)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (null == msg.PageType || msg.PageType == typeof(DraftPage))
                {
                    if (msg.Extra != null)
                    {
                        NavigationHelper.Pop(msg.ExtraKey);
                        NaviagteDetail(msg.Extra);
                    }
                    else
                    {
                        DraftDetailRoot.Visibility = Visibility.Collapsed;
                    }
                }
            });
        }

        private void OnDetailFrameNavigating(object sender, NavigatingCancelEventArgs e)
        {
            if ((e.NavigationMode == NavigationMode.New && e.Content == null)
                || e.NavigationMode == NavigationMode.Back)
            {
                e.Cancel = true;
                this.DraftDetailRoot.Visibility = Visibility.Collapsed;
            }
        }

        private void OnItemMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DraftListViewSource.SelectedValue == null) return;
            NaviagteDetail(DraftListViewSource.SelectedValue);
        }

        private void NaviagteDetail(object obj)
        {
            if (DraftDetailRoot.Visibility != Visibility.Visible)
                DraftDetailRoot.Visibility = Visibility.Visible;
            NavigationHelper.Push(Tokens.NavEdit, obj);
            DraftDetailFrame.NavigationService.Navigate(new EditMailPage());
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (sizeInfo.WidthChanged)
            {
                if (sizeInfo.NewSize.Width > 800)
                {
                    Grid.SetColumn(DraftMasterRoot, 0);
                    Grid.SetColumnSpan(DraftMasterRoot, 1);
                    Grid.SetColumn(DraftDetailRoot, 1);
                    Grid.SetColumnSpan(DraftDetailRoot, 1);
                    DraftMasterRoot.BorderBrush = System.Windows.Media.Brushes.LightGray;
                }
                else
                {
                    Grid.SetColumn(DraftMasterRoot, 0);
                    Grid.SetColumnSpan(DraftMasterRoot, 2);
                    Grid.SetColumn(DraftDetailRoot, 0);
                    Grid.SetColumnSpan(DraftDetailRoot, 2);
                    DraftMasterRoot.BorderBrush = System.Windows.Media.Brushes.Transparent;
                }
            }
        }
    }
}
