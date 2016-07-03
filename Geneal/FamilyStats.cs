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
            {"ENGLAND", fromHex("#F15854") },
            {"PRUSSIA", fromHex("#D3D3D3") },
            {"RUSSIAN EMPIRE", fromHex("#2A2A2A") },
            {"ACADIA", fromHex("#33AFFF") },
            {"MIKMAQ CANADA", fromHex("#7A1F1F") }
        };

        private FamilyMembers _family;
        private List<Member> _members;
        private int _maxGeneration;
        private List<ChartData> _familyCounts;
        private List<ChartData> _familyCountsExtended;
        private List<ChartData2> _familyCountsNames;

        public FamilyStats(FamilyMembers family)
        {
            this._family = family;
            this._members = family.Family;
            this._maxGeneration = _members.Count > 0 ? (int)_members.Select(m => m.Generation).Max() + 1 : 0;

            this._familyCounts = new List<ChartData>();
            this._familyCountsExtended = new List<ChartData>();
            this._familyCountsNames = new List<ChartData2>();

            Member root = _family.getMember(Preferences.RootUser);

            if(root != null)
            {
                traverseTree(root, root.BirthCountry, false, ref _familyCounts);
                traverseTree(root, root.BirthCountry, true, ref _familyCountsExtended);
                traverseTreeNames(root, ref _familyCountsNames);
            }
        }

        public void getNameOccurences(ref Chart chart)
        {
            chart.Series.Clear();

            List<ChartData2> results = (
                from d in _familyCountsNames
                orderby d.xValue
                group d by d.xValue into grp
                select new ChartData2
                {
                    xValue = grp.Key,
                    yValue = grp.Sum(g => g.yValue),
                    series = "Series 1"
                }).ToList();

            string[] values = results.OrderByDescending(r => r.yValue).Select(r => r.xValue).ToArray();

            Series s = new Series { Name = "Occurance", Color = Color.Blue, ChartType = SeriesChartType.Column, XValueType = ChartValueType.String, ToolTip = "#VALX: #VALY" }; ;

            for (int i = 0; i < (values.Length > 10 ? 10 : values.Length); i++)
            {
                s.Points.AddXY(values[i], (from r in results where r.xValue == values[i] select r.yValue).FirstOrDefault());
            }

            chart.Series.Add(s);
            chart.ChartAreas[0].RecalculateAxesScale();
            chart.Invalidate();
        }

        public void getCountryOccurences(Boolean extend, ref Chart chart)
        {
            chart.Series.Clear();

            List<ChartData2> results = (
                from ChartData d in (extend ? this._familyCountsExtended : this._familyCounts)
                where d.series != "" && d.xValue < this._maxGeneration
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

            for (int i = 0; i < (values.Length > 10 ? 10 : values.Length); i++)
            {
                s.Points.AddXY(values[i], (from r in results where r.xValue == values[i] select r.yValue).FirstOrDefault());
            }

            chart.Series.Add(s);
            chart.ChartAreas[0].RecalculateAxesScale();
            chart.Invalidate();
        }

        public Dictionary<string, double> getHistoricalLocations(Member mem)
        {
            if (mem == null)
            {
                return new Dictionary<string, double>();
            }

            Dictionary<string, double> percentages = new Dictionary<string, double>();
            Dictionary<string, int> history = getHistoricalLocations(mem, 0, _family.maxDepth);

            foreach (KeyValuePair<string, int> hist in history)
            {
                percentages.Add(
                    hist.Key, 
                    Math.Round(10000 * (double)hist.Value / Math.Pow(2, _family.maxDepth)) / 100.0
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

        public Dictionary<int, double> getHistoricalDuplicity()
        {
            Member rootMem = _family.getMember(Preferences.RootUser);
            
            if (rootMem == null)
            {
                return new Dictionary<int, double>();
            }

            Dictionary<Member, int> occurrences = new Dictionary<Member, int>();

            getMembersOccurances(rootMem, occurrences);

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
        public void getMembersOccurances(Member mem, Dictionary<Member, int> occurrences)
        {
            if(!occurrences.ContainsKey(mem))
            {
                occurrences.Add(mem, 0);
            }
            occurrences[mem] = occurrences[mem] + 1;

            //Tuple<Member, Member> parents = family.GetParents(mem);
            Member p1 = _family.getMember(mem.Parent1), p2 = _family.getMember(mem.Parent2);

            if (p1 != null)
            {
                getMembersOccurances(p1, occurrences);
            }

            if (p2 != null)
            {
                getMembersOccurances(p2, occurrences);
            }
        }

        public void getGenerationalCompleteness(ref Chart chart)
        {
            chart.Series.Clear();

            List<ChartData> results = (
                from d in _familyCounts
                orderby d.xValue
                group d by d.xValue into grp
                select new ChartData
                {
                    xValue = grp.Key,
                    yValue = 100 * grp.Sum(g => g.yValue) / Math.Pow(2, grp.Key),
                    series = "Series 1"
                }).ToList();

            double[] values = results.Select(r => r.xValue).OrderBy(x => x).ToArray();

            Series s = new Series { Name = "Occurance", Color = Color.Blue, ChartType = SeriesChartType.Column, XValueType = ChartValueType.String, ToolTip = "#VALY%" }; ;

            for (int i = 0; i < values.Length; i++)
            {
                s.Points.AddXY(values[i], (from r in results where r.xValue == values[i] select r.yValue).FirstOrDefault());
            }

            chart.Series.Add(s);
            chart.ChartAreas[0].RecalculateAxesScale();
            chart.Invalidate();
        }

        public void getCountByGenerationAndLocation(FamilyMembers family, Boolean percent, Boolean extend, ref Chart chart)
        {
            chart.Series.Clear();
            
            List<ChartData> results = (
                from ChartData d in (extend ? this._familyCountsExtended : this._familyCounts)
                where d.series != ""
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

            string[] series = (
                from r in results
                group r by r.series into grp
                orderby grp.Sum(g => g.yValue)
                select grp.Key
            ).ToArray();
            //string[] series = results.OrderBy(r => r.yValue).Select(m => m.series).Distinct().ToArray();

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
            chart.ChartAreas[0].RecalculateAxesScale();
            chart.Invalidate();
        }

        public void traverseTree(Member mem, string childLocation, Boolean extend, ref List<ChartData> data)
        {
            data.Add(new ChartData()
            {
                xValue = mem.Generation,
                yValue = 1,
                series = mem.BirthRegion ?? mem.BirthCountry
            });

            if(mem.Generation >= this._maxGeneration)
            {
                return;
            }

            Member p1 = this._family.getMember(mem.Parent1), p2 = this._family.getMember(mem.Parent2);

            if(p1 != null)
            {
                if(p1.BirthCountry == "")
                {
                    traverseTree(p1, childLocation, extend, ref data);
                }
                else
                {
                    traverseTree(p1, p1.BirthRegion ?? p1.BirthCountry, extend, ref data);
                }
            }
            else if(extend)
            {
                for(int i=0; i < this._maxGeneration - mem.Generation; i++)
                {
                    data.Add(new ChartData()
                    {
                        xValue = mem.Generation + 1 + i,
                        yValue = Math.Pow(2, i),
                        series = mem.BirthRegion ?? mem.BirthCountry
                    });
                }
            }

            if (p2 != null)
            {
                if (p2.BirthCountry == "")
                {
                    traverseTree(p2, childLocation, extend, ref data);
                }
                else
                {
                    traverseTree(p2, p2.BirthRegion ?? p2.BirthCountry, extend, ref data);
                }
            }
            else if (extend)
            {
                for (int i = 0; i < this._maxGeneration - mem.Generation; i++)
                {
                    data.Add(new ChartData()
                    {
                        xValue = mem.Generation + 1 + i,
                        yValue = Math.Pow(2, i),
                        series = mem.BirthRegion ?? mem.BirthCountry
                    });
                }
            }

            return;
        }

        public void traverseTreeNames(Member mem, ref List<ChartData2> data)
        {
            data.Add(new ChartData2()
            {
                xValue = mem.FamilyName,
                yValue = 1,
                series = "Series 1"
            });

            if (mem.Generation >= this._maxGeneration)
            {
                return;
            }

            Member p1 = this._family.getMember(mem.Parent1), p2 = this._family.getMember(mem.Parent2);

            if (p1 != null)
            {
                traverseTreeNames(p1, ref data);
            }

            if (p2 != null)
            {
                traverseTreeNames(p2, ref data);
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
