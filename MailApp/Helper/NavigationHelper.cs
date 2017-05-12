namespace MailApp
{
    public class NavigationHelper
    {
        public static void Push(string key, object obj)
        {
            if (string.IsNullOrEmpty(key)) return;
            App.Current.Properties[key] = obj;
        }

        public static object Pop(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!App.Current.Properties.Contains(key)) return null;
            var obj = App.Current.Properties[key];
            App.Current.Properties[key] = null;
            return obj;
        } 
    }
}
