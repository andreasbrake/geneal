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
        private FamilyMembers _family;
        private List<Member> _members;
        private int _maxGeneration;
        private List<ChartData> _familyCounts;
        private List<ChartData> _familyCountsExtended;

        public FamilyStats(FamilyMembers family)
        {
            this._family = family;
            this._members = family.Family;
            this._maxGeneration = _members.Count > 0 ? (int)_members.Select(m => m.Generations.Count > 0 ? m.Generations.Select(g => g.Depth).Max() : -1).Max() + 1 : 0;

            this._familyCounts = new List<ChartData>();
            this._familyCountsExtended = new List<ChartData>();

            Member root = _family.getMember(Preferences.RootUser);

            if(root != null)
            {
                getRegionNameCounts(false, ref _familyCounts);
                getRegionNameCounts(true, ref _familyCountsExtended);
            }
        }

        public void getNameOccurences(ref Chart chart)
        {
            chart.Series.Clear();

            List<ChartData2> results = (
                from Member mem in this._members
                group mem by mem.FamilyName into grp
                orderby grp.Sum(mem => mem.Generations.Count) descending
                select new ChartData2
                {
                    xValue = grp.Key,
                    yValue = grp.Sum(mem => mem.Generations.Count),
                    series = "Series 1"
                }).ToList();
            
            Series s = new Series {
                Name = "Occurance",
                Color = Color.Blue,
                ChartType = SeriesChartType.Column,
                XValueType = ChartValueType.String,
                ToolTip = "#VALX: #VALY" }; ;

            for (int i = 0; i < (results.Count > 10 ? 10 : results.Count); i++)
            {
                if(results[i].yValue > 0)
                {
                    s.Points.AddXY(results[i].xValue, results[i].yValue);
                }
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

        public void getHistoricalDuplicity(ref Chart memberOccuranceChart, ref Chart generationUniqueCount)
        {
            memberOccuranceChart.Series.Clear();
            generationUniqueCount.Series.Clear();

            Series memberOccuranceSeries = new Series { Name = "Occurance", Color = System.Drawing.Color.Blue, ChartType = SeriesChartType.Column, XValueType = ChartValueType.String, ToolTip = "#VALY" };
            Series generationalUniqueCount = new Series { Name = "Occurance", Color = System.Drawing.Color.Blue, ChartType = SeriesChartType.Column, XValueType = ChartValueType.String, ToolTip = "#VALY" };
            Series generationalTotalCount = new Series { Name = "Occurance2", Color = System.Drawing.Color.Green, ChartType = SeriesChartType.Line, XValueType = ChartValueType.String, ToolTip = "#VALY" };

            memberOccuranceChart.Series.Add(memberOccuranceSeries);
            generationUniqueCount.Series.Add(generationalUniqueCount);
            generationUniqueCount.Series.Add(generationalTotalCount);

            List<ChartData> data3 = new List<ChartData>();
            for(int i=0; i < _members.Count; i++)
            {
                for(int j=0; j < _members[i].Generations.Count; j++)
                {
                    data3.Add(new ChartData()
                    {
                        xValue = _members[i].Generations[j].Depth,
                        yValue = 1,
                        series = _members[i].Name
                    });
                }
            }

            List<ChartData> data2 = (
                from d in data3
                group d by new
                {
                    d.xValue,
                    d.series
                } into grp
                select new ChartData()
                {
                    xValue = grp.Key.xValue,
                    yValue = grp.Sum(g => g.yValue),
                    series = grp.Key.series
                }).ToList();

            List<ChartData> data = (
                from d  in data2
                orderby d.xValue
                group d by d.xValue into grp
                select new ChartData()
                {
                    xValue = grp.Key,
                    yValue = grp.Average(g => g.yValue),
                    series = "Series 1"
                }).ToList();
            
            for (int i = 0; i < data.Count; i++)
            {
                memberOccuranceSeries.Points.AddXY("" + data[i].xValue, data[i].yValue);
            }
            for (int i = 0; i < data.Count; i++)
            {
                generationalUniqueCount.Points.AddXY("" + data[i].xValue, Math.Pow(2, data[i].xValue) / data[i].yValue);
                generationalTotalCount.Points.AddXY("" + data[i].xValue, Math.Pow(2, data[i].xValue));
            }

            memberOccuranceChart.ChartAreas[0].RecalculateAxesScale();
            generationUniqueCount.ChartAreas[0].RecalculateAxesScale();

            memberOccuranceChart.Invalidate();
            generationUniqueCount.Invalidate();
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

            Series s = new Series {
                Name = "Occurance",
                Color = Color.Blue,
                ChartType = SeriesChartType.Column,
                XValueType = ChartValueType.String,
                ToolTip = "#VALY%"
            };

            for (int i = 0; i < values.Length; i++)
            {
                s.Points.AddXY("" + values[i], (from r in results where r.xValue == values[i] select r.yValue).FirstOrDefault());
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
                group d by new
                {
                    d.xValue,
                    d.series,
                } into grp
                select new ChartData
                {
                    yValue = grp.Sum(g => g.yValue),
                    xValue = grp.Key.xValue,
                    series = grp.Key.series == "" ? "UNKNOWN" : grp.Key.series
                }
            ).ToList();

            string[] series = (
                from r in results
                group r by r.series into grp
                orderby grp.Sum(g => g.yValue)
                select grp.Key
            ).ToArray();

            string[] regionOrder = Preferences.CountryOrder;
            for(int i=0; i < regionOrder.Length; i++)
            {
                if(series.Contains(regionOrder[i]))
                {
                    Series s = new Series {
                        Name = regionOrder[i],
                        ChartType = SeriesChartType.StackedArea100,
                        XValueType = ChartValueType.Int32,
                        ToolTip = "(#SERIESNAME, Gen -#VALX, #VALY)"
                    };

                    if (!percent)
                    {
                        s.ChartType = SeriesChartType.StackedArea;
                    }
                    if (Preferences.CountryColors.ContainsKey(regionOrder[i]) && Preferences.CountryColors[regionOrder[i]] != "")
                    {
                        s.Color = fromHex(Preferences.CountryColors[regionOrder[i]]);
                    }

                    for (int j = 0; j < _maxGeneration; j++)
                    {
                        s.Points.AddXY(j, (from r in results where r.series == regionOrder[i] && r.xValue == j select r.yValue).FirstOrDefault());
                    }

                    chart.Series.Add(s);
                }
            }

            for (int i = 0; i < series.Length; i++)
            {
                if (!regionOrder.Contains(series[i]))
                {
                    Series s = new Series { Name = series[i], ChartType = SeriesChartType.StackedArea100, XValueType = ChartValueType.Int32, ToolTip = "(#SERIESNAME, Gen -#VALX, #VALY)" }; ;
                    if (!percent)
                    {
                        s.ChartType = SeriesChartType.StackedArea;
                    }
                    if (Preferences.CountryColors.ContainsKey(series[i]) && Preferences.CountryColors[series[i]] != "")
                    {
                        s.Color = fromHex(Preferences.CountryColors[series[i]]);
                    }

                    for (int j = 0; j < _maxGeneration; j++)
                    {
                        s.Points.AddXY(j, (from r in results where r.series == series[i] && r.xValue == j select r.yValue).FirstOrDefault());
                    }

                    chart.Series.Add(s);
                }
            }

            chart.ChartAreas[0].AxisX.Maximum = _maxGeneration - 1;
            chart.ChartAreas[0].RecalculateAxesScale();
            chart.Invalidate();
        }

        public void getAverageGenerationYear(ref Chart chart)
        {
            chart.Series.Clear();

            #region calculation
            int maxYear = 0, minYear = 10000;

            Dictionary<int, List<int>> data = new Dictionary<int, List<int>>();
            for(int i=0; i < _members.Count; i++)
            {
                if(_members[i].BirthDate.Year == 1)
                {
                    continue;
                }

                if(_members[i].BirthDate.Year < minYear)
                {
                    minYear = _members[i].BirthDate.Year;
                }
                if (_members[i].BirthDate.Year > maxYear)
                {
                    maxYear = _members[i].BirthDate.Year;
                }

                for (int j=0; j < _members[i].Generations.Count; j++)
                {
                    if(!data.ContainsKey(_members[i].Generations[j].Depth))
                    {
                        data.Add(_members[i].Generations[j].Depth, new List<int>());
                    }

                    data[_members[i].Generations[j].Depth].Add(_members[i].BirthDate.Year);
                }
            }
            #endregion

            Series s = new Series {
                Name = "Series 1",
                ChartType = SeriesChartType.ErrorBar,
                XValueType = ChartValueType.String,
                YValuesPerPoint = 3,
                BorderWidth = 2,
                Color = fromHex("#0000FF"),
                ToolTip = "(Gen:#VALX, Avg:#VALY1, Min:#VALY2, Max:#VALY3)"
            };
            Series s2 = new Series
            {
                Name = "Series 2",
                ChartType = SeriesChartType.Line,
                XValueType = ChartValueType.String,
                BorderWidth = 1,
                Color = fromHex("#FF0000")
            };

            for (int j = 0; j < _maxGeneration; j++)
            {
                if(data.ContainsKey(j) && data[j].Count > 0)
                {
                    s.Points.AddXY("" + j, Math.Round(data[j].Average()), data[j].Min(), data[j].Max());
                    s2.Points.AddXY("" + j, Math.Round(data[j].Average()));
                }
            }

            chart.Series.Add(s);
            chart.Series.Add(s2);

            chart.ChartAreas[0].RecalculateAxesScale();
            chart.Invalidate();
        }

        public void getAverageDuplicityByYear(int division, ref Chart chart)
        {
            chart.Series.Clear();

            int minYear = 10000, maxYear = 0;

            Dictionary<int, List<int>> data = new Dictionary<int, List<int>>();
            for(int i=0; i < _members.Count; i++)
            {
                int year = (int)Math.Floor(_members[i].BirthDate.Year / (1.0 * division)) * division;

                if(year == 0 || _members[i].Generations.Count == 0)
                {
                    continue;
                }

                if(year < minYear)
                {
                    minYear = year;
                }
                if(year > maxYear)
                {
                    maxYear = year;
                }

                if(!data.ContainsKey(year))
                {
                    data.Add(year, new List<int>());
                }
                data[year].Add(_members[i].Generations.Count);
            }

            Series s = new Series
            {
                Name = "Series 1",
                ChartType = SeriesChartType.Column,
                XValueType = ChartValueType.String,
                Color = Color.Blue,
                ToolTip = "(Gen -#VALX, #VALY)"
            };

            for (int j = maxYear; j >= minYear; j -= division)
            {
                if (data.ContainsKey(j) && data[j].Count > 0)
                {
                    s.Points.AddXY("" + j, data[j].Average());
                }
            }

            chart.Series.Add(s);

            chart.ChartAreas[0].RecalculateAxesScale();
            chart.Invalidate();
        }

        public void getAverageAgeByBirthYear(int division, ref Chart chart)
        {
            chart.Series.Clear();

            #region calculation
            int minYear = 10000, maxYear = 0;

            Dictionary<int, List<int>> data = new Dictionary<int, List<int>>();
            List<double> allValues = new List<double>();

            for (int i = 0; i < _members.Count; i++)
            {
                int year = (int)Math.Floor(_members[i].BirthDate.Year / (1.0 * division)) * division;

                if (year == 0 || 
                    _members[i].BirthDate.Year < 10 || 
                    _members[i].DeathDate.Year < 10 || 
                    _members[i].BirthDate >= _members[i].DeathDate ||
                    _members[i].Generations.Count == 0)
                {
                    continue;
                }

                if (year < minYear)
                {
                    minYear = year;
                }
                if (year > maxYear)
                {
                    maxYear = year;
                }

                if (!data.ContainsKey(year))
                {
                    data.Add(year, new List<int>());
                }

                data[year].Add(_members[i].DeathDate.Year - _members[i].BirthDate.Year);
                allValues.Add(_members[i].DeathDate.Year - _members[i].BirthDate.Year);
            }
            #endregion

            Series s = new Series
            {
                Name = "Series 1",
                ChartType = SeriesChartType.Candlestick,
                XValueType = ChartValueType.String,
                YValuesPerPoint = 4,
                BorderWidth = 2,
                Color = fromHex("#0000FF"),
                ToolTip = "(Year: #VALX, Max: #VALY1, Min: #VALY2, TopSpread: #VALY3, BottomSpread: #VALY4)"
            };
            Series s2 = new Series
            {
                Name = "Series 2",
                ChartType = SeriesChartType.Line,
                XValueType = ChartValueType.String,
                BorderWidth = 1,
                Color = fromHex("#FF0000"),
                ToolTip = "Family Average: #VALY"
            };

            double overallAverage = allValues.Count > 0 ? allValues.Average() : 0;
            for (int j = maxYear; j >= minYear; j-=division)
            {
                if (data.ContainsKey(j) && data[j].Count > 0)
                {
                    double average = data[j].Average();
                    double max = data[j].Max();
                    double min = data[j].Min();
                    double spread = (max - min) / data[j].Count;

                    s.Points.AddXY("" + j, max, min, Math.Round(average + spread / 2), Math.Round(average - spread / 2));
                    s2.Points.AddXY("" + j, Math.Round(overallAverage * 100) / 100);
                }
            }

            chart.Series.Add(s);
            chart.Series.Add(s2);

            chart.ChartAreas[0].RecalculateAxesScale();
            chart.Invalidate();
        }

        public static Color fromHex(string hex)
        {
            return System.Drawing.ColorTranslator.FromHtml(hex);
        }

        public void getRegionNameCounts(Boolean extend, ref List<ChartData> data)
        {
            for (int i = 0; i < this._members.Count; i++)
            {
                Member mem = this._members[i];
                string region = (mem.BirthRegion ?? mem.BirthCountry);
                if(region == null || region == "")
                {
                    region = this._family.getFirstChildRegion(mem);
                }
                bool extendParent1 = false;
                bool extendParent2 = false;

                if (extend)
                {
                    extendParent1 = this._family.getMember(mem.Parent1) == null;
                    extendParent2 = this._family.getMember(mem.Parent2) == null;
                }

                for (int j = 0; j < mem.Generations.Count; j++)
                {
                    int gen = mem.Generations[j].Depth;
                    data.Add(new ChartData()
                    {
                        xValue = gen,
                        yValue = 1,
                        series = region
                    });

                    if (extendParent1)
                    {
                        for (int k = 0; k < this._maxGeneration - gen; k++)
                        {
                            data.Add(new ChartData()
                            {
                                xValue = gen + 1 + k,
                                yValue = Math.Pow(2, k),
                                series = region
                            });
                        }
                    }

                    if (extendParent2)
                    {
                        for (int k = 0; k < this._maxGeneration - gen; k++)
                        {
                            data.Add(new ChartData()
                            {
                                xValue = gen + 1 + k,
                                yValue = Math.Pow(2, k),
                                series = region
                            });
                        }
                    }
                }
            }
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
