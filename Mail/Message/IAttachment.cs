using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mail
{
    public interface IAttachment
    {
        byte[] FileDate { get; }
        string FileName { get; }
        bool Save(string path);
    }
}
