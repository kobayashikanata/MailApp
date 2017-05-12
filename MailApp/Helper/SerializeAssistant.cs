using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MailApp
{
    public class SerializeAssistant
    {
        public static object Deserialize(string path)
        {
            FileInfo file = new FileInfo(path);
            if (!file.Exists) return null;
            using (Stream stream = file.Open(FileMode.Open))
            {
                if (stream.Length == 0) return null;
                if (stream != null)
                {
                    BinaryFormatter bFormat = new BinaryFormatter();
                    return bFormat.Deserialize(stream);
                }
            }
            return null;
        }

        public static void Serialize(string path, object datas)
        {
            FileInfo file = new FileInfo(path);
            if (!file.Exists)
            {
                try
                {
                    file.Create();
                }
                catch(System.Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    return;
                }
            }
            using (var stream = file.Open(FileMode.Open))
            {
                BinaryFormatter bFormat = new BinaryFormatter();
                bFormat.Serialize(stream, datas);
            }
        }

    }
}
