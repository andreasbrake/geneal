using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Geneal
{
    public class Preferences
    {
        public static string RootUser { get { return _rootUser; } }
        public static string TreeFontName { get { return _treeFontName; } }
        public static int TreeFontSize { get { return _treeFontSize; } }

        private static string _rootUser;
        private static string _treeFontName;
        private static int _treeFontSize;

        public Preferences() { }
        public static void Init()
        {
            XDocument doc = XDocument.Load(Directory.GetCurrentDirectory() + @"\preferences.config");

            foreach (var prefs in doc.Descendants("Preferences"))
            {
                _rootUser = (string)prefs.Attribute("rootUser");
                _treeFontName = (string)prefs.Attribute("treeFontName");
                int fontSize;
                _treeFontSize = Int32.TryParse((string)prefs.Attribute("treeFontSize"), out fontSize) ? fontSize : 12;

            }

            //int fontSize;

            //XmlDocument prefs = new XmlDocument();
            //prefs.Load(Directory.GetCurrentDirectory() + @"\preferences.config");

            //RootUser = prefs.DocumentElement.SelectSingleNode("/preferences/rootUser").InnerText;
            //TreeFontName = prefs.DocumentElement.SelectSingleNode("/preferences/treeFontName").InnerText;
            //int fontSize;
            //TreeFontSize = Int32.TryParse(prefs.DocumentElement.SelectSingleNode("/preferences/treeFontSize").InnerText, out fontSize) ? fontSize : 12;
        }

        public static void SetRoot(string root)
        {
            Preferences._rootUser = root;

            XDocument doc = XDocument.Load(Directory.GetCurrentDirectory() + @"\preferences.config");
            foreach (var prefs in doc.Descendants("Preferences"))
            {
                prefs.SetAttributeValue("rootUser", Preferences.RootUser);
            }
            doc.Save(Directory.GetCurrentDirectory() + @"\preferences.config");
        }
    }
}
