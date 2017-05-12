using System;
using System.Windows;
using System.Windows.Threading;

namespace MailApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static Properties.Settings DefaultSettings => MailApp.Properties.Settings.Default;
        private static LogTraceListener _logTracker;
        public static LogTraceListener LogTracker
        {
            get
            {
                if (_logTracker == null)
                {
                    _logTracker = new LogTraceListener(new System.Windows.Controls.TextBox()
                    {
                        IsReadOnly = true,
                        BorderThickness = new Thickness(0)
                    });
                }
                return _logTracker;
            }
        }
        public App()
        {
            System.Diagnostics.Trace.Listeners.Add(LogTracker);
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            EnsureSettingsCreated();
        }

        private void EnsureSettingsCreated()
        {
            int changes = 0;
            if (DefaultSettings.HistoryUsers == null)
            {
                DefaultSettings.HistoryUsers = new SerializableDictionary<string, string>();
                ++changes;
            }
            if (DefaultSettings.Pop3Settings == null)
            {
                DefaultSettings.Pop3Settings = new Pop3Settings();
                ++changes;
            }
            if (DefaultSettings.ImapSettings == null)
            {
                DefaultSettings.ImapSettings = new ImapSettings();
                ++changes;
            }
            if (DefaultSettings.Pop3Settings == null)
            {
                DefaultSettings.SmtpSettings = new SmtpSettings();
                ++changes;
            }
            if (changes > 0) DefaultSettings.Save();
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            ViewModelLocator.Main.Dispose();
            DefaultSettings.Save();
            base.OnExit(e);
        }

        #region Unexpected exception handler

        private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;
#if DEBUG
            System.Diagnostics.Debug.WriteLine(sender.ToString() + "\n" + e.ExceptionObject);
#endif
            LogExceptionInfo(exception, "AppDomain.CurrentDomain.UnhandledException");
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(e.Exception.TargetSite);
#endif
            LogExceptionInfo(e.Exception, "AppDomain.DispatcherUnhandledException");
        }

        private void LogExceptionInfo(Exception exception, string typeName = "Undefined Exception")
        {
            DisposeOnUnhandledException();
            var lb = new System.Text.StringBuilder();
            lb.AppendLine("***************************");
            lb.AppendLine("--------- Begin  ---------");
            lb.AppendLine("--------------------------");
            lb.AppendLine();
            lb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ffff"));
            lb.AppendLine();
            lb.AppendLine("--------------------------");
            lb.AppendLine();
            lb.AppendLine(typeName);
            lb.AppendLine();
            lb.AppendLine("[0].TargetSite");
            lb.AppendLine(exception.TargetSite.ToString());
            lb.AppendLine();
            lb.AppendLine("[1].StackTrace");
            lb.AppendLine(exception.StackTrace);
            lb.AppendLine();
            lb.AppendLine("[2].Source");
            lb.AppendLine(exception.Source);
            lb.AppendLine();
            lb.AppendLine("[3].Message");
            lb.AppendLine(exception.Message);
            lb.AppendLine();
            lb.AppendLine("[4].HResult");
            lb.AppendLine(exception.HResult.ToString());
            lb.AppendLine();
            if (exception.InnerException != null)
            {
                lb.AppendLine("--------------");
                lb.AppendLine("InnerException");
                lb.AppendLine("--------------");
                lb.AppendLine();
                lb.AppendLine("[5.0].TargetSite");
                lb.AppendLine(exception.InnerException.TargetSite.ToString());
                lb.AppendLine();
                lb.AppendLine("[5.1].StackTrace");
                lb.AppendLine(exception.InnerException.StackTrace);
                lb.AppendLine();
                lb.AppendLine("[5.2].Source");
                lb.AppendLine(exception.InnerException.Source);
                lb.AppendLine();
                lb.AppendLine("[5.3].Message");
                lb.AppendLine(exception.InnerException.Message);
                lb.AppendLine();
                lb.AppendLine("[5.4].HResult");
                lb.AppendLine(exception.InnerException.HResult.ToString());
                lb.AppendLine();
            }
            lb.AppendLine("--------- End  ---------");
            lb.AppendLine();

            string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string dir = System.IO.Path.GetDirectoryName(location);
            string log = dir + "\\log.txt";
            using (var sw = new System.IO.StreamWriter(log, true, System.Text.Encoding.UTF8))
            {
                sw.Write(lb.ToString());
            }

            if(LogTracker.Output != null)
            {
                var ob = new System.Text.StringBuilder();
                ob.AppendLine("***************************");
                ob.AppendLine("--------- Begin  ---------");
                ob.AppendLine("--------------------------");
                ob.AppendLine();
                ob.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ffff"));
                string[] array = LogTracker.Output.Text.Split('\n');
                for (int i = 0; i < array.Length; i++)
                {
                    ob.AppendLine(array[i]);
                }
                ob.AppendLine("--------- End  ---------");
                ob.AppendLine();
                string output = dir + "\\output.txt";
                using (var sw = new System.IO.StreamWriter(output, true, System.Text.Encoding.UTF8))
                {
                    sw.Write(ob.ToString());
                    LogTracker.Output = null;
                }
            }
        }

        private void DisposeOnUnhandledException()
        {
        }

        #endregion
    }
}
