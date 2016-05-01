using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Xml.Linq;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET;

namespace Geneal
{
    public class Maps
    {
        private const string _API_KEY = "AIzaSyDsFcfY0LgZ_O9GAcuOsw_6Ocn8-a4NIls";
        private static Dictionary<string, Tuple<double, double>> _LocationMap = new Dictionary<string, Tuple<double, double>>();
        private static HttpClient http = new HttpClient();

        public Maps() { }

        public static void setMarkers(GMapOverlay overlay, int year)
        {
            overlay.Markers.Clear();

            List<Member> members = FamilyMembers.getLiving(year);

            for(int i=0; i < members.Count; i++)
            {
                Tuple<double, double> loc = Maps.lookupLocation(members[i].BirthLocation);

                if(loc == null)
                {
                    continue;
                }

                GMarkerGoogle marker = new GMarkerGoogle(
                    new PointLatLng(loc.Item1, loc.Item2),
                    GMarkerGoogleType.green
                );

                marker.Tag = members[i];

                overlay.Markers.Add(marker);
            }
        }

        public static void addLocation(string location, string lat, string lng)
        {
            Double latD = Double.TryParse(lat, out latD) ? latD : 2000;
            Double lngD = Double.TryParse(lng, out lngD) ? lngD : 2000;

            if(latD == 2000 || lngD == 2000)
            {
                return;
            }

            Maps.addLocation(location, latD, lngD);
        }

        public static void addLocation(string location, double lat, double lng)
        {
            _LocationMap.Add(location, new Tuple<double, double>(lat, lng));
        }

        public static Tuple<double, double> lookupLocation(string location)
        {
            location = location.Replace("%apos;", "'");
            if(_LocationMap.ContainsKey(location))
            {
                return _LocationMap[location];
            }
            
            _LocationMap[location] = Lookup(location);

            if (_LocationMap[location] != null)
            {
                DataSource.writeLocationCache(location, _LocationMap[location].Item1, _LocationMap[location].Item2);
            }

            return _LocationMap[location];
        }

        public static Tuple<double, double> Lookup(string address) // async Task<string>
        {
            string url = "http://maps.google.com/maps/api/geocode/xml?address=" + address + "&sensor=false";

            HttpResponseMessage httpResponse = http.GetAsync(url).Result;
            XDocument xmlResponse = XDocument.Parse(
                httpResponse.Content.ReadAsStringAsync().Result
            );

            XElement location = xmlResponse.Descendants("location").FirstOrDefault();

            if(location == null)
            {
                return null;
            }

            Double lat = Double.TryParse(location.Descendants("lat").First().Value, out lat) ? lat : 0;
            Double lng = Double.TryParse(location.Descendants("lng").First().Value, out lng) ? lng : 0;

            return new Tuple<double, double>(lat, lng);
        }
    }
}
