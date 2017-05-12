using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MailApp
{
    public static class WebBrowserUtility
    {
        public static string GetBindableSource(DependencyObject obj)
        {
            return (string)obj.GetValue(BindableSourceProperty);
        }
        public static void SetBindableSource(DependencyObject obj, string value)
        {
            obj.SetValue(BindableSourceProperty, value);
        }
        public static readonly DependencyProperty BindableSourceProperty =
            DependencyProperty.RegisterAttached("BindableSource", typeof(string), typeof(WebBrowserUtility), new UIPropertyMetadata(null, BindableSourcePropertyChanged));
        
        public static string GetHtmlText(DependencyObject obj)
        {
            return (string)obj.GetValue(HtmlTextProperty);
        }
        public static void SetHtmlText(DependencyObject obj, string value)
        {
            obj.SetValue(HtmlTextProperty, value);
        }
        public static readonly DependencyProperty HtmlTextProperty =
            DependencyProperty.RegisterAttached("HtmlText", typeof(string), typeof(WebBrowserUtility), new PropertyMetadata(null, OnHtmlTextchanged));
        
        public static void OnHtmlTextchanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            WebBrowser browser = o as WebBrowser;
            if (browser != null)
            {
                string text = e.NewValue as string;
                if (!string.IsNullOrEmpty(text))
                {
                    text = "<meta http-equiv='Content-Type' content='text/html;charset=UTF-8'>" + text;
                    browser.NavigateToString(text);
                }
            }
        }

        public static void BindableSourcePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            WebBrowser browser = o as WebBrowser;
            if (browser != null)
            {
                string uri = e.NewValue as string;
                browser.Source = !String.IsNullOrEmpty(uri) ? new Uri(uri) : null;
            }
        }

    }
}
