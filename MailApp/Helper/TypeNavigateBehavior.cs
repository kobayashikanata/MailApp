using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace MailApp
{
    public class TypeNavigateBehavior : Behavior<System.Windows.Controls.Frame>
    {
        public Type NavigateTarget
        {
            get { return (Type)GetValue(NavigateTargetProperty); }
            set { SetValue(NavigateTargetProperty, value); }
        }
        public static readonly DependencyProperty NavigateTargetProperty =
            DependencyProperty.Register("NavigateTarget", typeof(Type), typeof(TypeNavigateBehavior), new PropertyMetadata(null, OnNavigateTargetChanged));
        
        public bool KeepAlive
        {
            get { return (bool)GetValue(KeepAliveProperty); }
            set { SetValue(KeepAliveProperty, value); }
        }
        public static readonly DependencyProperty KeepAliveProperty =
            DependencyProperty.Register("KeepAlive", typeof(bool), typeof(TypeNavigateBehavior), new PropertyMetadata(false, OnKeepAliveChanged));
        
        private Dictionary<Type, object> navigatedList = new Dictionary<Type, object>();
        private System.Windows.Controls.Frame AssociatedFrame => AssociatedObject;
        private bool keepAliveCache;

        private static void OnKeepAliveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behaviors = d as TypeNavigateBehavior;
            if (null != behaviors)
            {
                behaviors.keepAliveCache = (bool)e.NewValue;
                if (null != behaviors.navigatedList
                    && !behaviors.keepAliveCache)
                    behaviors.navigatedList.Clear();
            }
        }

        private static void OnNavigateTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behaviors = d as TypeNavigateBehavior;
            if (behaviors != null) behaviors.Navigate();
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedFrame.Navigated += OnAssociatedFrameNavigated;
            System.Windows.Navigation.JournalEntry.SetKeepAlive(AssociatedFrame, keepAliveCache);
            Navigate();
        }

        protected override void OnDetaching()
        {
            navigatedList.Clear();
            navigatedList = null;
            LastObject = null;
            AssociatedFrame.Navigated -= OnAssociatedFrameNavigated;
            base.OnDetaching();
        }

        private void Navigate()
        {
            if (null == NavigateTarget) return;
            if (null == AssociatedFrame) return;
            if (keepAliveCache)
            {
                if (!navigatedList.ContainsKey(NavigateTarget))
                {
                    object targetObj = Activator.CreateInstance(NavigateTarget);
                    LastObject = targetObj;
                    navigatedList.Add(NavigateTarget, targetObj);
                }
                else
                {
                    LastObject = navigatedList[NavigateTarget];
                }
            }
            else
            {
                LastObject = Activator.CreateInstance(NavigateTarget);
            }
            AssociatedFrame.Navigate(LastObject);
        }

        private void OnAssociatedFrameNavigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (null == e.Content) return;
            if (LastObject == e.Content) return;
            var type = e.Content.GetType();
            if (NavigateTarget != type)
                NavigateTarget = type;
        }

        private object LastObject { get; set; }
    }
}
