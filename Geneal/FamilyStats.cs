using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace Geneal
{
    public class FamilyStats
    {
        private static Dictionary<string, Color> theme = new Dictionary<string, Color>()
        {
            {"default", Color.Blue },
            {"CANADA", fromHex("#C40C0C") },
            {"GERMANY", fromHex("#DECF3F") },
            {"FRANCE", fromHex("#0072BB") },
            {"POLAND", fromHex("#4D4D4D") },
            {"SCOTLAND", fromHex("#2548b4") },
            {"IRELAND", fromHex("#60BD68") },
            {"BELGIUM", fromHex("#B276B2") },
            {"ENGLAND", fromHex("#F15854") }
        };

        private FamilyMembers _family;
        private MemberCollection _members;
        private int _maxGeneration;
        private List<ChartData> _familyCounts;
        private List<ChartData> _familyCountsExtended;

        public FamilyStats(FamilyMembers family)
        {
            this._family = family;
            this._members = family.Family;
            this._maxGeneration = _members.Count > 0 ? (int)_members.Cast<Member>().Select(m => m.Generation).Max() : 0;

            this._familyCounts = new List<ChartData>();
            this._familyCountsExtended = new List<ChartData>();

            traverseTree(_members.Get(Preferences.RootUser), family.Family.clone(), this._maxGeneration, false, ref _familyCounts);
            traverseTree(_members.Get(Preferences.RootUser), family.Family.clone(), this._maxGeneration, true, ref _familyCountsExtended);
        }

        public Dictionary<string, int> getNameOccurences()
        {
            List<Member> mems = _family.Family.Cast<Member>().ToList();

            var list = from m in mems
                       where m.Generation >= 0
                       group m by m.Name.Split(',')[0] into g
                       orderby g.Count() descending
                       select new { g.Key, Count = g.Count() };
            
            return list.ToDictionary(x => x.Key, x => x.Count);
        }

        public void getCountryOccurences(Boolean extend, ref Chart chart)
        {
            chart.Series.Clear();

            List<ChartData2> results = (
                from ChartData d in (extend ? this._familyCountsExtended : this._familyCounts)
                where d.series != ""
                group d by d.series into grp
                select new ChartData2
                {
                    yValue = grp.Sum(g => g.yValue),
                    xValue = grp.Key,
                    series = "Series 1"
                }
            ).ToList();

            string[] values = results.OrderByDescending(r => r.yValue).Select(r => r.xValue).ToArray();

            Series s = new Series { Name = "Occurance", Color = Color.Blue, ChartType = SeriesChartType.Column, XValueType = ChartValueType.String, ToolTip = "(#VALX, #VALY)" }; ;

            for (int i = 0; i < values.Length; i++)
            {
                s.Points.AddXY(values[i], (from r in results where r.xValue == values[i] select r.yValue).FirstOrDefault());
            }

            chart.Series.Add(s);
            
            chart.Invalidate();
        }

        public Dictionary<string, int> getCountryOccurences()
        {
            List<Member> mems = _family.Family.Cast<Member>().ToList();

            var list = from m in mems
                       where m.Generation >= 0 && m.BirthLocation.Split(',').Last().Trim() != ""
                       group m by m.BirthLocation.Split(',').Last().Trim().ToUpper() into g
                       orderby g.Count() descending
                       select new { g.Key, Count = g.Count() };

            return list.ToDictionary(x => x.Key, x => x.Count);
        }

        public Dictionary<string, double> getHistoricalLocations(Member mem, int maxDepth)
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
        public Dictionary<string, int> getHistoricalLocations(Member mem, int depth, int maxDepth)
        {
            string memName = mem.Name;
            string birthRegion = mem.BirthLocation.Split(',').Last().Trim().ToLower();
            if (birthRegion == "") birthRegion = "unknown";

            if (depth == maxDepth)
            {
                if (birthRegion == "unknown") return null;
                return new Dictionary<string, int>() { { birthRegion, 1 } };
            }

            Member parent1 = _family.getMember(mem.Parent1);
            Member parent2 = _family.getMember(mem.Parent2);

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

        public Dictionary<int, double> getHistoricalDuplicity(Member rootMem, MemberCollection members)
        {
            Dictionary<Member, int> occurrences = new Dictionary<Member, int>();

            getMembersOccurances(rootMem, members, occurrences);

            Dictionary<int,  List<int>> results = new Dictionary<int, List<int>>();

            foreach( KeyValuePair<Member,int> pair in occurrences)
            {
                if(!results.ContainsKey(pair.Key.Generation))
                {
                    results.Add(pair.Key.Generation, new List<int>());
                }

                results[pair.Key.Generation].Add(pair.Value);
            }

            return results.OrderBy(kp => kp.Key)
                    .ToDictionary(pair => pair.Key, pair => pair.Value.Average());
        }
        public void getMembersOccurances(Member mem, MemberCollection family, Dictionary<Member, int> occurrences)
        {            
            if(!occurrences.ContainsKey(mem))
            {
                occurrences.Add(mem, 0);
            }
            occurrences[mem] = occurrences[mem] + 1;

            Tuple<Member, Member> parents = family.GetParents(mem);

            if (parents.Item1 != null)
            {
                getMembersOccurances(parents.Item1, family, occurrences);
            }

            if (parents.Item2 != null)
            {
                getMembersOccurances(parents.Item2, family, occurrences);
            }
        }

        public Dictionary<int, double> getGenerationalCompleteness(MemberCollection members)
        {
            Dictionary<int, int> results = members.Cast<Member>()
                .ToList()
                .GroupBy(m => m.Generation)
                .ToDictionary(g => g.Key, g => g.ToList().Count());

            return results.OrderBy(m => m.Key)
                .Where(m => m.Key >= 0)
                .ToDictionary(m => m.Key, m => 100 * m.Value / Math.Pow(2, m.Key));
        }

        public void getCountByGenerationAndLocation(MemberCollection members, Boolean percent, Boolean extend, ref Chart chart)
        {
            chart.Series.Clear();

            string[] series = _family.Family.Cast<Member>().Select(m => m.BirthCountry).Where(m => m != "").Distinct().OrderBy(c => c).ToArray();
            
            List<ChartData> results = (
                from ChartData d in (extend ? this._familyCountsExtended : this._familyCounts)
                group d by new
                {
                    d.xValue,
                    d.series,
                } into grp
                select new ChartData
                {
                    yValue = grp.Sum(g => g.yValue),
                    xValue = grp.Key.xValue,
                    series = grp.Key.series
                }
            ).ToList();

            for (int i=0; i < series.Length; i++)
            {
                Series s = new Series { Name = series[i], ChartType = SeriesChartType.StackedArea100, XValueType = ChartValueType.Int32, ToolTip = "(#SERIESNAME, Gen -#VALX, #VALY)" }; ;
                if (!percent)
                {
                    s.ChartType = SeriesChartType.StackedArea;
                }
                if(theme.ContainsKey(series[i]))
                {
                    s.Color = theme[series[i]];
                }

                for(int j=0; j < _maxGeneration; j++)
                {
                    s.Points.AddXY(j, (from r in results where r.series == series[i] && r.xValue == j select r.yValue).FirstOrDefault());
                }

                chart.Series.Add(s);
            }

            chart.ChartAreas[0].AxisX.Maximum = _maxGeneration - 1;
            chart.Invalidate();
        }

        public void traverseTree(Member mem, MemberCollection extended, int maxDepth, Boolean extend, ref List<ChartData> data)
        {
            data.Add(new ChartData()
            {
                xValue = mem.Generation,
                yValue = 1,
                series = mem.BirthCountry
            });

            if(mem.Generation >= maxDepth)
            {
                return;
            }

            Member p1 = extended.Get(mem.Parent1), p2 = extended.Get(mem.Parent2);

            if(p1 != null)
            {
                if(p1.BirthCountry == "")
                {
                    p1.BirthLocation = mem.BirthLocation;
                }
                traverseTree(p1, extended, maxDepth, extend, ref data);
            }
            else if(extend)
            {
                int missing = (int)Math.Pow(2, maxDepth - mem.Generation);
                for(int i=1; i < maxDepth - mem.Generation; i++)
                {
                    data.Add(new ChartData()
                    {
                        xValue = mem.Generation + i,
                        yValue = Math.Pow(2, i),
                        series = mem.BirthCountry
                    });
                }
            }

            if (p2 != null)
            {
                if (p2.BirthCountry == "")
                {
                    p2.BirthLocation = mem.BirthLocation;
                }
                traverseTree(p2, extended, maxDepth, extend, ref data);
            }
            else if (extend)
            {
                int missing = (int)Math.Pow(2, maxDepth - mem.Generation);
                for (int i = 1; i < maxDepth - mem.Generation; i++)
                {
                    data.Add(new ChartData()
                    {
                        xValue = mem.Generation + i,
                        yValue = Math.Pow(2, i),
                        series = mem.BirthCountry
                    });
                }
            }

            return;
        }

        public static Color fromHex(string hex)
        {
            return System.Drawing.ColorTranslator.FromHtml(hex);
        }
    }

    public class ChartData
    {
        public double yValue { get; set; }
        public double xValue { get; set; }
        public string series { get; set; }
    }

    public class ChartData2
    {
        public double yValue { get; set; }
        public string xValue { get; set; }
        public string series { get; set; }
    }
}
