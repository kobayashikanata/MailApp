using System.Windows;
using System.Windows.Controls;

namespace MailApp.Views
{
    /// <summary>
    /// Interaction logic for NewMailPage.xaml
    /// </summary>
    public partial class EditMailPage : Page
    {
        private ViewModel.SendMailViewModel Vm { get; set; }

        public EditMailPage()
        {
            InitializeComponent();
            this.Loaded += OnThisLoaded;
        }

        private void OnThisLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnThisLoaded;
            this.Root.Unloaded += OnRootUnloaded;
            Vm = new ViewModel.SendMailViewModel();
            var obj = NavigationHelper.Pop(Tokens.NavEdit);
            if(obj != null)
            {
                var mail = obj as MailBase;
                if(mail != null)
                {
                    Vm.LoadBase(mail);
                    rich.Document.Blocks.Clear();
                    var section = mail.BodyHtml.HtmlStringToSection();
                    if (section != null) rich.Document.Blocks.Add(section);
                }
            }
            this.DataContext = Vm;
        }

        private void GoBackBtnClick(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
            else this.NavigationService.Navigate(null);
        }

        private void OnRootUnloaded(object sender, RoutedEventArgs e)
        {
            this.Root.Unloaded += OnRootUnloaded;
            this.rich.Document.Blocks.Clear();
            this.DataContext = null;
            this.Vm = null;
        }
        
        private void AttachmentsDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0 && (e.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy)
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            this.Vm?.AddAttachmentCommand.Execute(files);
        }

        private void AddAttachmentBtnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if(dialog.ShowDialog() == true)
            {
                this.Vm?.AddAttachmentCommand.Execute(dialog.FileName);
            }
        }
    }
}
