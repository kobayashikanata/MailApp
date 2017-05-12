using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailApp
{
    public class OverlayMessage
    {
        public Type OverlayType { get; set; }
        public object Extra { get; set; }
        public string  ExtraKey { get; set; }
    }
}
