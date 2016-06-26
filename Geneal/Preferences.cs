using System;
using System.IO;
using System.Xml;

namespace Geneal
{
    public class Preferences
    {
        public static string RootUser { get; set; }
        public static string TreeFontName { get; set; }
        public static int TreeFontSize { get; set; }
        public static string DataSourceFile { get; set; }
        
        public Preferences() { }
        public static void Init()
        {
            XmlDocument prefs = new XmlDocument();
            prefs.Load(Directory.GetCurrentDirectory() + @"\preferences.config");

            RootUser = prefs.DocumentElement.SelectSingleNode("/preferences/rootUser").InnerText;
            TreeFontName = prefs.DocumentElement.SelectSingleNode("/preferences/treeFontName").InnerText;
            int fontSize;
            TreeFontSize = Int32.TryParse(prefs.DocumentElement.SelectSingleNode("/preferences/treeFontSize").InnerText, out fontSize) ? fontSize : 12;
            DataSourceFile = prefs.DocumentElement.SelectSingleNode("/preferences/dataFile").InnerText;
        }
    }
}
