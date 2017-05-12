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
    /// Interaction logic for Welcome.xaml
    /// </summary>
    public partial class Welcome : Epxoxy.Controls.MaskControl, IOverlay
    {
        public Welcome()
        {
            InitializeComponent();
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

    }
}
