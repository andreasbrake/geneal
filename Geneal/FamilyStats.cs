using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geneal
{
    public class FamilyStats
    {
        public FamilyStats() { }

        public static Dictionary<string, int> getNameOccurences()
        {
            List<Member> mems = FamilyMembers.Family.Cast<Member>().ToList();

            var list = from m in mems
                       where m.Generation >= 0
                       group m by m.Name.Split(',')[0] into g
                       orderby g.Count() descending
                       select new { g.Key, Count = g.Count() };
            
            return list.ToDictionary(x => x.Key, x => x.Count);
        }

        public static Dictionary<string, int> getCountryOccurences()
        {
            List<Member> mems = FamilyMembers.Family.Cast<Member>().ToList();

            var list = from m in mems
                       where m.Generation >= 0 && m.BirthLocation.Split(',').Last().Trim() != ""
                       group m by m.BirthLocation.Split(',').Last().Trim().ToUpper() into g
                       orderby g.Count() descending
                       select new { g.Key, Count = g.Count() };

            return list.ToDictionary(x => x.Key, x => x.Count);
        }

        public static Dictionary<string, double> getHistoricalLocations(Member mem, int maxDepth)
        {
            Dictionary<string, double> percentages = new Dictionary<string, double>();
            Dictionary<string, int> history = getHistoricalLocations(mem, 0, maxDepth);

            foreach (KeyValuePair<string, int> hist in history)
            {
                percentages.Add(
                    hist.Key, 
                    Math.Round(10000 * (double)hist.Value / Math.Pow(2, maxDepth)) / 100.0
                );
            }

            return percentages;
        }
        public static Dictionary<string, int> getHistoricalLocations(Member mem, int depth, int maxDepth)
        {
            string memName = mem.Name;
            string birthRegion = mem.BirthLocation.Split(',').Last().Trim().ToLower();
            if (birthRegion == "") birthRegion = "unknown";

            if (depth == maxDepth)
            {
                if (birthRegion == "unknown") return null;
                return new Dictionary<string, int>() { { birthRegion, 1 } };
            }

            Member parent1 = FamilyMembers.getMember(mem.Parent1);
            Member parent2 = FamilyMembers.getMember(mem.Parent2);

            Dictionary<string, int> personalHistory = new Dictionary<string, int>();
            Dictionary<string, int> parent1History = null;
            Dictionary<string, int> parent2History = null;

            if (parent1 != null)
            {
                parent1History = getHistoricalLocations(parent1, depth + 1, maxDepth);
            }

            if (parent2 != null)
            {
                parent2History = getHistoricalLocations(parent2, depth + 1, maxDepth);
            }

            if(parent1History == null && parent2History == null)
            {
                if (birthRegion == "unknown") return null;
                return new Dictionary<string, int>() { { birthRegion, (int)Math.Pow(2, (maxDepth - depth)) } };
            }

            if (parent1History != null)
            {
                foreach (KeyValuePair<string, int> hist in parent1History)
                {
                    if (personalHistory.ContainsKey(hist.Key)) personalHistory[hist.Key] += hist.Value;
                    else personalHistory.Add(hist.Key, hist.Value);
                }
            }
            else
            {
                string key = birthRegion;
                if (personalHistory.ContainsKey(key)) personalHistory[key] += (int)Math.Pow(2, (maxDepth - depth - 1));
                else personalHistory.Add(key, (int)Math.Pow(2, (maxDepth - depth - 1)));
            }

            if (parent2History != null)
            {
                foreach (KeyValuePair<string, int> hist in parent2History)
                {
                    if (personalHistory.ContainsKey(hist.Key)) personalHistory[hist.Key] += hist.Value;
                    else personalHistory.Add(hist.Key, hist.Value);
                }
            }
            else
            {
                string key = birthRegion;
                if (personalHistory.ContainsKey(key)) personalHistory[key] += (int)Math.Pow(2, (maxDepth - depth - 1));
                else personalHistory.Add(key, (int)Math.Pow(2, (maxDepth - depth - 1)));
            }

            return personalHistory;
        }
    }
}
