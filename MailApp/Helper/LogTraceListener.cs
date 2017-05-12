using System;
using System.Text;
using System.Windows.Controls;

namespace MailApp
{
    public class LogTraceListener : System.Diagnostics.TraceListener
    {
        private StringBuilder debugMsgBuilder = new StringBuilder();
        public TextBox Output { get; internal set; }

        public LogTraceListener(TextBox output)
        {
            this.Output = output;
        }

        public string Messages => debugMsgBuilder.ToString();

        public override void Write(string message)
        {
            debugMsgBuilder.Append(message);
            if (Output != null)
            {
                Action append = delegate ()
                {
                    Output.AppendText(message);
                };
                Output.Dispatcher.BeginInvoke(append);
            }
        }

        public override void WriteLine(string message)
        {
            message = message.Replace("\n", "\n----------------");
            Write("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + message + Environment.NewLine);
        }
    }
}
