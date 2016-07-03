using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Geneal
{
    public class DataSource
    {
        private Maps _mapData;
        private List<Member> _members;
        private static string CACHE_PATH = Directory.GetCurrentDirectory() + @"\locations.dat";
        private static string DATA_PATH = Directory.GetCurrentDirectory() + @"\family.bin";
        private static string DATA_EXPORT_PATH = Directory.GetCurrentDirectory() + @"\family_export.bin";

        public DataSource(Maps map)
        {
            this._mapData = map;

            this.LoadDataFile();

            if (File.Exists(CACHE_PATH))
            {
                loadLocationCache(CACHE_PATH);
            }
            else
            {
                File.Create(CACHE_PATH);
            }
        }

        public List<Member> getMembers()
        {
            return this._members;
        }

        public void LoadDataFile()
        {
            if(!File.Exists(DATA_PATH))
            {
                this._members = new List<Member>();
                return;
            }

            //deserialize
            using (Stream stream = File.Open(DATA_PATH, FileMode.Open))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                this._members = (List<Member>)bformatter.Deserialize(stream);
            }
        }
        public void WriteToDataFile()
        {
            //serialize
            using (Stream stream = File.Open(DATA_PATH, FileMode.Create))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bformatter.Serialize(stream, _members);
            }
        }
        public void WriteCurrentToDataFile()
        {
            List<Member> current = (from m in _members
                                    where m.Generation >= 0
                                    select m).ToList();
            //serialize
            using (Stream stream = File.Open(DATA_EXPORT_PATH, FileMode.Create))
            {
                var bformatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                bformatter.Serialize(stream, current);
            }
        }

        public void LoadJsonFile(string filepath)
        {
            string[] lines = System.IO.File.ReadAllLines(filepath);

            Dictionary<string, List<string>> rawMembers = new Dictionary<string, List<string>>();

            int familyDepth = 0;
            int memberDepth = 0;
            string currentFamily = "";
            StringBuilder currentMember = new StringBuilder();
            for(int i=1; i < lines.Length - 1; i++)
            {
                string line = lines[i].Trim();
                
                if(line.Length == 0)
                {
                    continue;
                }

                if (line.IndexOf('"') >= 0 && familyDepth == 0)
                {
                    currentFamily = Regex.Match(line, "(?<=\")[a-zA-Z]+(?=\")").Value.ToString();
                    rawMembers.Add(currentFamily, new List<string>());
                }       

                if (memberDepth > 0)
                {
                    if(line.ToUpper().IndexOf("\"BIRTH\"") >= 0 || line.ToUpper().IndexOf("\"DEATH\"") >= 0)
                    {
                        GroupCollection groups = Regex.Match(line, "([^[]*\\[\\s*\"\\d*-\\d*-\\d*\"\\s*,\\s*\")([^\"]*)(\"\\s*])").Groups;
                        line = groups[1].ToString().Replace(" ", "") + groups[2].ToString() + groups[3].ToString().Replace(" ", "");
                    }
                    else
                    {
                        //line = Regex.Replace(line, "\"\\s *:\\s * \"", "\":\"");
                        //line = Regex.Replace(line, "\\[\\s", "[");
                        //line = Regex.Replace(line, "\\s\\]", "]");

                        line = Regex.Replace(line, "(?<=[^A-Za-z0-9])\\s(?=[^A-Za-z0-9])", "");
                        
                        //line = line.Replace(" ", "");
                    }
                    currentMember.Append(line.Replace("'","%apos;").Replace("\"", "'"));
                }

                if (line.IndexOf("{") >= 0)
                {
                    memberDepth++;
                }

                if (line.IndexOf("}") >= 0)
                {
                    memberDepth--;
                    if (memberDepth == 0)
                    {
                        rawMembers[currentFamily].Add(currentMember.ToString());
                        currentMember.Clear();
                    }
                }

                if (line.IndexOf("[") >= 0)
                {
                    familyDepth++;
                }
                if (line.IndexOf("]") >= 0)
                {
                    familyDepth--;
                }
            }

            this._members = parseMemberList(rawMembers);
        }

        private List<Member> parseMemberList(Dictionary<string, List<string>>  data)
        {
            List<Member> members = new List<Member>();
            foreach (KeyValuePair<string, List<string>> rawFamilies in data)
            {
                string familyName = rawFamilies.Key;

                for (int i = 0; i < rawFamilies.Value.Count; i++)
                {
                    Member member = new Member();
                    string memberString = rawFamilies.Value[i];

                    string firstName = Regex.Match(memberString, "(?<='name':')[^']*").Value.ToString();
                    string birthData = Regex.Match(memberString, "(?<='birth':\\[)[^\\]]*").Value.ToString();
                    string birthRegion = Regex.Match(memberString, "(?<='birthRegion':')[^']*").Value.ToString();
                    string deathData = Regex.Match(memberString, "(?<='death':\\[)[^\\]]*").Value.ToString();
                    string parentsData = Regex.Match(memberString, "(?<='parents':\\[)[^\\]]*").Value.ToString();
                    string miscData = Regex.Match(memberString, "(?<='misc':\\{)[^//}]*").Value.ToString();
                    
                    string[] birthDate = Regex.Match(birthData, "[^']*(?=',)").Value.ToString().Split('-');
                    if (birthDate.Length != 3)
                    {
                        birthDate = new string[3] { "", "", "" };
                    }
                    member.BirthLocation = Regex.Match(birthData, "(?<=,')[^']*").Value.ToString();

                    member.BirthRegion = birthRegion != "" ? birthRegion.ToUpper().Trim() : null;

                    int birthYear = Int32.TryParse(birthDate[0], out birthYear) ? birthYear : 1;
                    int birthMonth = Int32.TryParse(birthDate[1], out birthMonth) ? birthMonth : 1;
                    int birthDay = Int32.TryParse(birthDate[2], out birthDay) ? birthDay : 1;
                    member.BirthDate = new DateTime(birthYear, birthMonth, birthDay);

                    string deathDateString = Regex.Match(deathData, "[^']*(?=',)").Value.ToString();

                    if(deathDateString.IndexOf("-") < 0)
                    {
                        member.DeathLoction = "Still Alive";
                        member.DeathDate = new DateTime(1, 1, 1);
                    }
                    else
                    {
                        string[] deathDate = deathDateString.Split('-');
                        if (deathDate.Length != 3)
                        {
                            deathDate = new string[3] { "", "", "" };
                        }
                        member.DeathLoction = Regex.Match(deathData, "(?<=,')[^']*").Value.ToString();

                        int deathYear = Int32.TryParse(deathDate[0], out deathYear) ? deathYear : 1;
                        int deathMonth = Int32.TryParse(deathDate[1], out deathMonth) ? deathMonth : 1;
                        int deathDay = Int32.TryParse(deathDate[2], out deathDay) ? deathDay : 1;
                        member.DeathDate = new DateTime(deathYear, deathMonth, deathDay);
                    }

                    member.Name = familyName + "," + firstName + "," + birthDate[0];

                    member.Parent1 = Regex.Match(parentsData, "[^']{2,}(?=',)").Value.ToString() ?? "";
                    member.Parent2 = Regex.Match(parentsData, "(?<=,')[^']{2,}").Value.ToString() ?? "";

                    member.MiscInfo = new Dictionary<string, string>();
                    MatchCollection miscKVs = Regex.Matches(miscData, "'([^']+)':'([^']+)");
                    for (int j = 0; j < miscKVs.Count; j++)
                    {
                        if(!member.MiscInfo.ContainsKey(miscKVs[j].Groups[1].Value))
                        {
                            member.MiscInfo.Add(miscKVs[j].Groups[1].Value, miscKVs[j].Groups[2].Value);
                        }
                        else
                        {
                            member.MiscInfo[miscKVs[j].Groups[1].Value] += ". " + miscKVs[j].Groups[2].Value;
                        }
                    }

                    member.Generation = -1;
                    member.GenerationIndex = -1;

                    members.Add(member);
                }
            }

            return members;
        }

        private void loadLocationCache(string filepath)
        {
            string[] lines = System.IO.File.ReadAllLines(filepath);
            
            for(int i=0; i < lines.Length; i++)
            {
                string[] entry = lines[i].Split(',');
                this._mapData.addLocation(entry[0].Replace("%cma;", ","), entry[1], entry[2]);
            }
        }

        public static void writeLocationCache(string location, double lat, double lng)
        {
            string loc = location.Replace(",", "%cma;");
            using (StreamWriter sw = File.AppendText(CACHE_PATH))
            {
                sw.WriteLine(loc + "," + lat + "," + lng);
            }
        }
    }
}
