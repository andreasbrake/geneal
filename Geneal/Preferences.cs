using System;
using System.IO;
using System.Xml;

namespace Geneal
{
    public class Preferences
    {
        private static string _rootUser;
        private static string _treeFontName;
        private static int _treeFontSize;

        public Preferences()
        {
            XmlDocument prefs = new XmlDocument();
            prefs.Load(Directory.GetCurrentDirectory() + @"\preferences.config");

            _rootUser = prefs.DocumentElement.SelectSingleNode("/preferences/rootUser").InnerText;
            _treeFontName = prefs.DocumentElement.SelectSingleNode("/preferences/treeFontName").InnerText;
            _treeFontSize = Int32.TryParse(prefs.DocumentElement.SelectSingleNode("/preferences/treeFontSize").InnerText, out _treeFontSize) ? _treeFontSize : 12;
        }

        public static string RootUser
        {
            get { return _rootUser; }
        }

        public static string TreeFontName
        {
            get { return _treeFontName; }
        }

        public static int TreeFontSize
        {
            get { return _treeFontSize; }
        }
    }
}
