using System;
using System.Collections.Generic;
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
        public static Dictionary<string, string> CountryColors { get { return _countryColors; } }
        public static string[] CountryOrder { get { return _countryOrder.ToArray(); } }

        private static string _rootUser;
        private static string _treeFontName;
        private static int _treeFontSize;
        private static Dictionary<string, string> _countryColors;
        private static List<string> _countryOrder;

        public Preferences() { }
        public static void Init()
        {
            XDocument doc = XDocument.Load(Directory.GetCurrentDirectory() + @"\preferences.config");
            
            foreach (var baseOptions in doc.Element("Preferences").Descendants("BaseOptions"))
            {
                _rootUser = (string)baseOptions.Attribute("rootUser");
                _treeFontName = (string)baseOptions.Attribute("treeFontName");
                int fontSize = Int32.TryParse((string)baseOptions.Attribute("treeFontSize"), out fontSize) ? fontSize : 12;
                _treeFontSize = fontSize;
            }

            _countryColors = new Dictionary<string, string>();
            _countryOrder = new List<string>();

            foreach (var country in doc.Element("Preferences").Descendants("CountryOptions").Descendants())
            {
                string name = (string)country.Attribute("name");
                string color = (string)country.Attribute("color");

                _countryColors.Add(name, color);
                _countryOrder.Add(name);
            }
        }

        public static void SetRoot(string root)
        {
            Preferences._rootUser = root;

            XDocument doc = XDocument.Load(Directory.GetCurrentDirectory() + @"\preferences.config");
            foreach (var prefs in doc.Descendants("BaseOptions"))
            {
                prefs.SetAttributeValue("rootUser", Preferences.RootUser);
            }
            doc.Save(Directory.GetCurrentDirectory() + @"\preferences.config");
        }
    }
}
