using System;
using System.Windows;
using System.Windows.Controls;

namespace MailApp.Views
{
    /// <summary>
    /// Interaction logic for MailDetailPage.xaml
    /// </summary>
    public partial class MailDetailPage : Page
    {
        public MailDetailPage()
        {
            InitializeComponent();
            this.Loaded += OnThisLoaded;
        }

        private void OnThisLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnThisLoaded;
            htmlBrowser.MessageHook += OnBrowserMessageHook;
            this.Unloaded += OnThisUnloaded;
        }

        private IntPtr OnBrowserMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0010:// WM_CLOSE
                    handled = true;// cancel event here
                    this.NavigationService.Navigate(null);
                    System.Diagnostics.Debug.WriteLine("OnBrowserMessageHook");
                    break;
            }
            return IntPtr.Zero;
        }

        private void OnThisUnloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= OnThisUnloaded;
            htmlBrowser.MessageHook -= OnBrowserMessageHook;
            htmlBrowser.Dispose();
            this.DataContext = null;
        }

        private void SaveAttachmentClick(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (null != btn && null != btn.CommandParameter)
            {
                //Get target path
                var saveDialog = new Microsoft.Win32.SaveFileDialog();
                saveDialog.FileName = btn.Tag as string;
                saveDialog.Filter = "All files (*.*)|*.*";
                if (saveDialog.ShowDialog() == true)
                {
                    System.Diagnostics.Debug.WriteLine(saveDialog.FileName);
                    var path = saveDialog.FileName;
                    System.Diagnostics.Debug.WriteLine(path);
                    ViewModelLocator.Main.SaveAttachmentCommand?.Execute(new object[]
                    {
                        btn.CommandParameter, path
                    });
                }
            }
        }
    }
}
