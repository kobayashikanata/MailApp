using System.Windows;
using System.Diagnostics;
using Epxoxy.Controls;
using AppSettings = MailApp.Properties.Settings;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Input;
using System.ComponentModel;

namespace MailApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<IOverlay> overlays;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnThisLoaded;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        private void OnThisLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnThisLoaded;
            this.Unloaded += OnThisUnloaded;
            MessengerLight.Messenger.Default.Register<DisplayMessage>(this, OnDisplayMessage);
            MessengerLight.Messenger.Default.Register<NavigationMessage>(this, Tokens.Main, OnNavigationMessage);
            MessengerLight.Messenger.Default.Register<string>(this, Tokens.Main, OnStringMessage);
            MessengerLight.Messenger.Default.Register<OverlayMessage>(this, Tokens.Main, OnOverlayMessage);
            ShowOverlay(new Views.Welcome());
        }

        private void OnStringMessage(string msg)
        {
            if(msg == "ClearOverlays")
            {
                ClearOverlays();
            }
        }

        private void OnOverlayMessage(OverlayMessage msg)
        {
            if (msg.OverlayType == typeof(Views.LoginPane))
            {
                ShowOverlay(new Views.LoginPane());
            }
        }

        private void OnNavigationMessage(NavigationMessage msg)
        {
            if (null == msg || null == msg.PageType) return;
            this.Dispatcher.Invoke(() =>
            {
                bool found = false;
                foreach (var item in listview.Items)
                {
                    var obj = item as NavItem;
                    if (obj == null) continue;
                    if (obj.PageType == msg.PageType)
                    {
                        if (null != msg.Extra)
                            NavigationHelper.Push(msg.ExtraKey, msg.Extra);
                        listview.SelectedItem = obj;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    if(navFrame.Content != null && navFrame.Content.GetType() == msg.PageType)
                    {
                        navFrame.Refresh();
                    }
                    else
                    {
                        navFrame.Navigate(Activator.CreateInstance(msg.PageType));
                    }
                }
            });
        }
        
        private void OnDisplayMessage(DisplayMessage msg)
        {
            Debug.WriteLine(msg.Message);
            Dispatcher.Invoke(() =>
            {
                Debug.WriteLine(msg.Message);
                if (msg.Type == DisplayType.Dialog)
                {
                    var dialog = new MessageDialog(this);
                    dialog.ShowDialog(msg.Title, msg.Message);
                    if (dialog.DialogResult == Epxoxy.Controls.DialogResult.Primary)
                    {
                        msg.OnOk?.Invoke();
                    }
                    else msg.OnDismiss?.Invoke();
                }
                else
                {
                    toast.ToastMessage(msg.Title, msg.Message);
                }
            });
        }

        private async void ShowOverlay<T>(T element) where T : UIElement, IOverlay, new()
        {
            ClearOverlays();
            if (overlays == null)
                overlays = new List<IOverlay>();
            overlays.Add(element);
            using (OverlayAdorner<T>.Overlay(overlayRoot, element))
            {
                await element.WaitForClose();
            }
        }

        private void ClearOverlays()
        {
            if (overlays != null)
            {
                for (int i = 0; i < overlays.Count; i++)
                {
                    if (overlays[i] == null) continue;
                    var overlay = overlays[i];
                    overlays.Remove(overlay);
                    overlay.IsOpen = false;
                }
            }
        }

        private void OnThisUnloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= OnThisUnloaded;
            MessengerLight.Messenger.Default.Unregister(this);
        }

        //Disable the ManipulationBoundaryFeedback event to prevent window shake.
        private void OnManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }
    }
}
