using System;
using System.IO;
using System.Text;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml;
using Epxoxy.Converters;

namespace MailApp
{
    public static class DocumentHelper
    {
        public static string HtmlStringToXaml(this string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            return HtmlToXamlConverter.ConvertHtmlToXaml(html, false);
        }
        
        public static Section HtmlStringToSection(this string html)
        {
            if (string.IsNullOrEmpty(html)) return null;
            string xamlString = html.HtmlStringToXaml();
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xamlString)))
            {
                var parser = new ParserContext();
                parser.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                parser.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
                Section section = null;
                try
                {
                    section = XamlReader.Load(stream, parser) as Section;
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
                return section;
            }
        }

        public static bool IsFlowDocument(this string xamlString)
        {
            if (xamlString == null || xamlString == "")
                throw new ArgumentNullException();
            if (xamlString.StartsWith("<") && xamlString.EndsWith(">"))
            {
                XmlDocument xml = new XmlDocument();
                try
                {
                    xml.LoadXml(string.Format("<Root>{0}</Root>", xamlString));
                    return true;
                }
                catch (XmlException)
                {
                    return false;
                }
            }
            return false;
        }

        public static FlowDocument HtmlStringToFlowDocument(this string html)
        {
            if (!string.IsNullOrEmpty(html))
            {
                var xaml = HtmlToXamlConverter.ConvertHtmlToXaml(html, true);
                return xaml.XamlStringToFlowDocument();
            }
            return null;
        }
        
        public static FlowDocument XamlStringToFlowDocument(this string xamlString)
        {
            if (IsFlowDocument(xamlString))
            {
                var stringReader = new StringReader(xamlString);
                var xmlReader = XmlReader.Create(stringReader);

                return XamlReader.Load(xmlReader) as FlowDocument;
            }
            else
            {
                Paragraph myParagraph = new Paragraph();
                myParagraph.Inlines.Add(new Run(xamlString));
                FlowDocument myFlowDocument = new FlowDocument();
                myFlowDocument.Blocks.Add(myParagraph);

                return myFlowDocument;
            }
        }

        public static string ToXamlString(this FlowDocument doc)
        {
            if (doc == null) return null;
            TextRange tr = new TextRange(doc.ContentStart, doc.ContentEnd);
            using (MemoryStream ms = new MemoryStream())
            {
                tr.Save(ms, System.Windows.DataFormats.Xaml);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static string ToHtmlString(this FlowDocument doc)
        {
            var xamlString = doc.ToXamlString();
            if (string.IsNullOrEmpty(xamlString)) return null;
            return HtmlFromXamlConverter.ConvertXamlToHtml(xamlString, false);
        }

        public static string GetText(this FlowDocument doc)
        {
            if (doc == null) return string.Empty;
            TextRange range = new TextRange(doc.ContentStart, doc.ContentEnd);
            return range.Text;
        }

        public static string CombinePath(this string folder, string fileName)
        {
            if (string.IsNullOrEmpty(folder)) return null;
            if (string.IsNullOrEmpty(fileName)) return folder;
            return folder + Path.DirectorySeparatorChar + fileName;
        }
    }
}
