using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Geneal
{
    [Serializable]
    public class Member
    {

        #region properties

        public string MemRef { get; set; }

        public String Name { get; set; }
        public String Firstname
        {
            get {
                string[] nameParts = this.Name.Split(',');
                if (nameParts.Length != 3 || nameParts[1].Length < 2) return "Unknown";
                string firstName = nameParts[1].Substring(0, 1).ToUpper() + nameParts[1].Substring(1);
                return char.ToUpper(firstName[0]) + firstName.Substring(1);
            }
        }
        public String LastName {
            get
            {
                string[] nameParts = this.Name.Split(',');
                if (nameParts.Length != 3 || nameParts[0].Length < 2) return "Unknown";

                string lastName = nameParts[0].Substring(0, 1).ToUpper() + nameParts[0].Substring(1);
                Match irishMatch = Regex.Match(lastName, @"(mc|mac|O')([^\s]+)");
                if (irishMatch.Success)
                {
                    string parsedName = char.ToUpper(irishMatch.Groups[1].Value[0]) + irishMatch.Groups[1].Value.Substring(1) + char.ToUpper(irishMatch.Groups[2].Value[0]) + irishMatch.Groups[2].Value.Substring(1);
                    lastName = lastName.Replace(irishMatch.Value, parsedName);
                }
                return lastName;
            }
        }
        public String CleanName { get { return this.Firstname + " " + this.LastName;  } }
        public String GEDCOMName { get { return this.Firstname + "/" + this.LastName; } }

        public String FamilyName { get { return this.Name.Split(',').First().Trim().ToUpper(); } }
        public String FirstName { get { return this.Name.Split(',').Last().Trim().ToUpper(); } }

        public DateTime BirthDate { get; set; }
        public int BirthDecade { get { return (int)Math.Round(this.BirthDate.Year / 10.0) * 10; } }
        public int BirthBidecade { get { return (int)Math.Round(this.BirthDate.Year / 20.0) * 20; } }
        public int BirthSemicentury { get { return (int)Math.Round(this.BirthDate.Year / 50.0) * 50; } }
        public int BirthCentury { get { return (int)Math.Round(this.BirthDate.Year / 100.0) * 100; } }

        public String BirthLocation { get; set; }
        public String BirthRegion { get; set; }
        public String BirthCountry { get { return this.BirthLocation.Split(',').Last().Trim().ToUpper(); } }

        public DateTime DeathDate { get; set; }

        public String DeathLoction { get; set; }
        public String DeathCountry { get { return this.DeathLoction.Split(',').Last().Trim().ToUpper(); } }

        public String Parent1 { get; set; }

        public String Parent2 { get; set; }

        public Dictionary<String, String> MiscInfo { get; set; }
        
        public List<Generation> Generations { get; set; }

        #endregion

        public Member() {
            this.Generations = new List<Generation>();
        }
        
        public String getMisc(string key)
        {
            if(MiscInfo.ContainsKey(key))
            {
                return MiscInfo[key];
            }
            return null;
        }

        public Member Clone()
        {
            return (Member)this.MemberwiseClone();
        }
    }

    [Serializable]
    public class Generation
    {
        public int Depth { get; set; }
        public int Breadth { get; set; }
    }
}
