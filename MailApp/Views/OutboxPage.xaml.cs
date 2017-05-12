using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MailApp.Views
{
    /// <summary>
    /// Interaction logic for OutboxPage.xaml
    /// </summary>
    public partial class OutboxPage : Page
    {
        public OutboxPage()
        {
            InitializeComponent();
            this.Loaded += OnThisLoaded;
        }
        private void OnThisLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnThisLoaded;
            this.Unloaded += OnThisUnloaded;
            this.DetailFrame.NavigationService.Navigating += OnDetailFrameNavigating;
        }

        private void OnDetailFrameNavigating(object sender, NavigatingCancelEventArgs e)
        {
            if ((e.NavigationMode == NavigationMode.New && e.Content == null)
                || e.NavigationMode == NavigationMode.Back)
            {
                GoBackBtnClick(this, null);
            }
        }

        private void OnThisUnloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= OnThisUnloaded;
            this.DetailFrame.NavigationService.Navigating -= OnDetailFrameNavigating;
            this.ListViewSource.DataContext = null;
            this.DataContext = null;
        }

        private void OnItemMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ListViewSource.SelectedValue == null) return;
            if (DetailRoot.Visibility != Visibility.Visible)
                DetailRoot.Visibility = Visibility.Visible;
            DetailFrame.NavigationService.Navigate(new MailDetailPage()
            {
                DataContext = ListViewSource.SelectedValue
            });
        }

        private void GoBackBtnClick(object sender, RoutedEventArgs e)
        {
            DetailRoot.Visibility = Visibility.Collapsed;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (sizeInfo.WidthChanged)
            {
                if (sizeInfo.NewSize.Width > 800)
                {
                    Grid.SetColumn(MasterRoot, 0);
                    Grid.SetColumnSpan(MasterRoot, 1);
                    Grid.SetColumn(DetailRoot, 1);
                    Grid.SetColumnSpan(DetailRoot, 1);
                    BackBtn.Visibility = Visibility.Collapsed;
                    MasterRoot.BorderBrush = Brushes.LightGray;
                }
                else
                {
                    Grid.SetColumn(MasterRoot, 0);
                    Grid.SetColumnSpan(MasterRoot, 2);
                    Grid.SetColumn(DetailRoot, 0);
                    Grid.SetColumnSpan(DetailRoot, 2);
                    BackBtn.Visibility = Visibility.Visible;
                    MasterRoot.BorderBrush = Brushes.Transparent;
                }
            }
        }
    }
}
