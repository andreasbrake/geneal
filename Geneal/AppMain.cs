using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Geneal
{
    public partial class AppMain : Form
    {
        string currentMember = Preferences.RootUser;

        public AppMain()
        {
            InitializeComponent();

            createTree(Preferences.RootUser);

            this.treePanel.Width = (int)Math.Round(this.Width * 0.75f - (this.treePanel.VerticalScroll.Visible ? 25 : 0) - 25);
            this.infoPanel.Width = (int)Math.Round(this.Width * 0.25f);

            createStats();
            
            Dictionary<string, double> history = FamilyStats.getHistoricalLocations(FamilyMembers.getMember(Preferences.RootUser), FamilyMembers.Family.maxDepth);

            setListData();

            // Search box
            setListData();

            // Slider
            yearRange.Minimum = FamilyMembers.getFirstBirthYear();
            yearRange.Maximum = DateTime.Now.Year;
            lblCurrentYear.Text = "" + FamilyMembers.getFirstBirthYear();
            yearRange.BringToFront();

            // Map Stuff
            gMap.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerOnly;

            gMap.Position = new PointLatLng(46.2276, 2.2137);

            GMapOverlay markersOverlay = new GMapOverlay("marker");
            gMap.Overlays.Add(markersOverlay);
            gMap.OnMarkerClick += new MarkerClick(this.gMap_MarkerClick);

            Maps.setMarkers(markersOverlay, yearRange.Minimum);
        }

        private void setListData()
        {
            searchResults.Items.Clear();

            string name1 = nameFirstSearch.Text.Trim().ToUpper();
            string name2 = nameLastSearch.Text.Trim().ToUpper();
            int birthYear1 = -1;
            int birthYear2 = -1;
            string birthLocation = birthLocationSearch.Text.Trim().ToUpper();
            int deathYear1 = -1;
            int deathYear2 = -1;
            string deathLocation = deathLocationSearch.Text.Trim().ToUpper();

            if (birthDateStartSearch.Text != "")
            {
                birthYear1 = Int32.Parse(birthDateStartSearch.Text);
            }
            if (birthDateEndSearch.Text != "")
            {
                birthYear2 = Int32.Parse(birthDateEndSearch.Text);
            }
            if (deathDateStartSearch.Text != "")
            {
                deathYear1 = Int32.Parse(deathDateStartSearch.Text);
            }
            if (deathDateEndSearch.Text != "")
            {
                deathYear2 = Int32.Parse(deathDateEndSearch.Text);
            }

            searchResults.Items.AddRange(
                FamilyMembers.Family.getAlphabeticalLike(
                    name1,
                    name2,
                    birthYear1,
                    birthYear2,
                    birthLocation,
                    deathYear1,
                    deathYear2,
                    deathLocation
                )
            );
        }

        private void createStats()
        {
            var nameOccurenceSeries = new Series { Name = "Occurance", Color = System.Drawing.Color.Blue, ChartType = SeriesChartType.Column, XValueType = ChartValueType.String };
            var countryOccurenceSeries = new Series { Name = "Occurance", Color = System.Drawing.Color.Blue, ChartType = SeriesChartType.Column, XValueType = ChartValueType.String };

            this.occuranceChart.Series.Add(nameOccurenceSeries);
            this.countryOccuranceChart.Series.Add(countryOccurenceSeries);

            Dictionary<string, int> nameOccurenceData = FamilyStats.getNameOccurences();
            for (int i=0; i < (nameOccurenceData.Keys.Count > 10 ? 10 : nameOccurenceData.Keys.Count); i++)
            {                
                nameOccurenceSeries.Points.AddXY(nameOccurenceData.ElementAt(i).Key, nameOccurenceData.ElementAt(i).Value);
            }

            Dictionary<string, int> countryOccurenceData = FamilyStats.getCountryOccurences();
            for (int i = 0; i < (countryOccurenceData.Keys.Count > 10 ? 10 : countryOccurenceData.Keys.Count); i++)
            {
                countryOccurenceSeries.Points.AddXY(countryOccurenceData.ElementAt(i).Key, countryOccurenceData.ElementAt(i).Value);
            }

            this.occuranceChart.Invalidate();
            this.countryOccuranceChart.Invalidate();
        }

        private void createTree(string memberName)
        {
            float WINDOW_HEIGHT = treePage.Height - 50;
            float MAX_RECT_HEIGHT = 20;
            float MAX_RECT_WIDTH = 150;

            this.currentMember = memberName;

            Member[][] nodes = FamilyMembers.assignNodes(FamilyMembers.getMember(memberName), 10);
            this.treePanel.Controls.Clear();

            for (int i=0; i < nodes.Length; i++)
            {
                float containerHeight = WINDOW_HEIGHT / nodes[i].Length;

                for(int j=0; j < nodes[i].Length; j++)
                {
                    if(nodes[i][j] == null || nodes[i][j].Name == "" || MAX_RECT_HEIGHT > (containerHeight * 4))
                    {
                        continue;
                    }

                    float rectHeight = MAX_RECT_HEIGHT < containerHeight ? MAX_RECT_HEIGHT : containerHeight;
                    float rectWidth  = MAX_RECT_HEIGHT < containerHeight ? MAX_RECT_WIDTH  : containerHeight / MAX_RECT_HEIGHT;

                    float xValue = (MAX_RECT_WIDTH) * i + 20;
                    float yValue = (j + 1) * containerHeight - (containerHeight / 2);

                    Label memberItem = new Label();

                    memberItem.Name = nodes[i][j].Name;

                    if (MAX_RECT_HEIGHT < containerHeight)
                    {
                        memberItem.Text = parseName(nodes[i][j].Name);
                        memberItem.TextAlign = ContentAlignment.MiddleCenter;
                        memberItem.Font = new Font(Preferences.TreeFontName, Preferences.TreeFontSize, FontStyle.Regular);
                    }

                    if (i == 0)
                    {
                        memberItem.ForeColor = Color.Gray;
                    }

                    memberItem.Location = new Point((int)(xValue * 1.2), (int)yValue);

                    memberItem.Width = (int)Math.Round(MAX_RECT_WIDTH);
                    memberItem.Height = (int)Math.Round(rectHeight);
                    memberItem.BorderStyle = BorderStyle.FixedSingle;

                    memberItem.Cursor = Cursors.Hand;

                    memberItem.Click += new EventHandler(showInfo);
                    memberItem.DoubleClick += new EventHandler(recenterTree);
                    memberItem.MouseEnter += new EventHandler(markSelection);
                    memberItem.MouseLeave += new EventHandler(unmarkSelection);

                    this.treePanel.Controls.Add(memberItem);
                }
            }
        }

        private void populateInfoPanel(Member memInfo)
        {
            lblMemerName.Text = "Name: " + parseName(memInfo.Name);

            lblMemberPosition.Text = "Position: (" + memInfo.Generation + "," + memInfo.GenerationIndex + ")";

            lblMemberBirthDate.Text = "Birth Date: " + parseDate(memInfo.BirthDate);
            lblMemberBirthPlace.Text = "Birth Place: " + parseLocation(memInfo.BirthLocation);

            lblMemberDeathDate.Text = "Death Date: " + parseDate(memInfo.DeathDate);
            lblMemberDeathPlace.Text = "Death Place: " + parseLocation(memInfo.DeathLoction);

            lblGotoMap.Tag = memInfo;

            int key = 0;
            while (this.infoPanel.Controls.ContainsKey("miscinfo_" + key))
            {
                this.infoPanel.Controls.RemoveByKey("miscinfo_" + key);
                key++;
            }

            for (int i = 0; i < memInfo.MiscInfo.Keys.Count; i++)
            {
                KeyValuePair<string, string> miscInfo = memInfo.MiscInfo.ElementAt(i);

                Label infoLabel = new Label();
                infoLabel.Name = "miscinfo_" + i;
                infoLabel.Text = miscInfo.Key + ": " + miscInfo.Value.Replace("%apos;", "'");
                infoLabel.Font = new Font(FontFamily.GenericSansSerif, 10.2f, FontStyle.Regular);
                infoLabel.Width = this.infoPanel.Width - 25;
                infoLabel.Location = new Point(13, 250 + 20 * i);

                this.infoPanel.Controls.Add(infoLabel);
            }

            if(memInfo.Parent1 != "" && FamilyMembers.getMember(memInfo.Parent1) == null ||
                memInfo.Parent2 != "" && FamilyMembers.getMember(memInfo.Parent2) == null)
            {
                int index = memInfo.MiscInfo.Keys.Count;

                Label infoLabel = new Label();
                infoLabel.Name = "miscinfo_" + index;
                infoLabel.Text = "(Parent(s) missing data entry)";
                infoLabel.Font = new Font(FontFamily.GenericSansSerif, 10.2f, FontStyle.Regular);
                infoLabel.ForeColor = Color.Red;
                infoLabel.Width = this.infoPanel.Width - 25;
                infoLabel.Location = new Point(13, 250 + 20 * index);

                this.infoPanel.Controls.Add(infoLabel);
            }
        }

        private void populateMapInfoPanel(Member mem)
        {
            lblMarkerName.Text = "Name: " + parseName(mem.Name);
            lblMarkerBirthDate.Text = "Birth Date: " + parseDate(mem.BirthDate);
            lblMarkerBirthPlace.Text = "Birth Place: " + parseLocation(mem.BirthLocation);
            lblMarkerDeathDate.Text = "Death Date: " + parseDate(mem.DeathDate);
            lblMarkerDeathLocation.Text = "Death Place: " + parseLocation(mem.DeathLoction);

            lblGotoTree.Tag = mem.Name;
        }

        private void populateSearchInfoPanel(Member mem)
        {
            lblSearchMemberName.Text = "Name: " + parseName(mem.Name);
            
            lblSearchMemberBirthDate.Text = "Birth Date: " + parseDate(mem.BirthDate);
            lblSearchMemberBirthPlace.Text = "Birth Place: " + parseLocation(mem.BirthLocation);

            lblSearchMemberDeathDate.Text = "Death Date: " + parseDate(mem.DeathDate);
            lblSearchMemberDeathPlace.Text = "Death Place: " + parseLocation(mem.DeathLoction);

            lblSearchGotoMap.Tag = mem;
            lblSearchGotoTree.Tag = mem.Name;

            searchMiscInfoBox.Items.Clear();

            List<string> miscItems = new List<string>() { "" };
            for (int i = 0; i < mem.MiscInfo.Keys.Count; i++)
            {
                KeyValuePair<string, string> miscInfo = mem.MiscInfo.ElementAt(i);
                miscItems.Add(miscInfo.Key + ":");
                miscItems.Add(miscInfo.Value.Replace("%apos;","'"));
                miscItems.Add("");
            }

            searchMiscInfoBox.Items.AddRange(miscItems.ToArray());
        }

        private string parseName(string name)
        {
            string[] nameParts = name.Split(',');

            string firstName = nameParts[1].Substring(0, 1).ToUpper() + nameParts[1].Substring(1);
            string lastName = nameParts[0].Substring(0, 1).ToUpper() + nameParts[0].Substring(1);

            firstName = char.ToUpper(firstName[0]) + firstName.Substring(1);

            Match irishMatch = Regex.Match(lastName, @"(mc|mac|O')([^\s]+)");
            if(irishMatch.Success)
            {
                string parsedName = char.ToUpper(irishMatch.Groups[1].Value[0]) + irishMatch.Groups[1].Value.Substring(1) + char.ToUpper(irishMatch.Groups[2].Value[0]) + irishMatch.Groups[2].Value.Substring(1);
                lastName = lastName.Replace(irishMatch.Value, parsedName);
            }

            return firstName + " " + lastName;
        }
        private string parseDate(DateTime date)
        {
            if(date.Year == 1)
            {
                return "-";
            }
            return date.ToShortDateString();
        }
        private string parseLocation(string location)
        {
            if(location == "")
            {
                return "Unknown";
            }
            string[] parts = location.Split(',');
            List<string> parsedParts = new List<string>();
            
            for(int i=0; i < parts.Length; i++)
            {
                if(parts[i] == "")
                {
                    continue;
                }

                string locationPart = parts[i].Trim().Replace("%apos;", "'");

                if (locationPart.Length == 2)
                {
                    locationPart = locationPart.ToUpper();
                }
                else
                {
                    MatchCollection words = Regex.Matches(locationPart, @"([^\s]+)");
                    string newName = "";
                    for(int j=0; j < words.Count; j++)
                    {
                        if(j > 0)
                        {
                            newName += " ";
                        }
                        newName += char.ToUpper(words[j].Value[0]) + words[j].Value.Substring(1);
                    }
                    locationPart = newName;
                }

                parsedParts.Add(locationPart);
            }

            return String.Join(", ", parsedParts.ToArray());
        }

        // EVENT HANDLERS

        private void markSelection(object sender, EventArgs e)
        {
            ((Label)sender).Font = new Font(Preferences.TreeFontName, Preferences.TreeFontSize, FontStyle.Bold);
        }

        private void unmarkSelection(object sender, EventArgs e)
        {
            ((Label)sender).Font = new Font(Preferences.TreeFontName, Preferences.TreeFontSize, FontStyle.Regular);
        }

        private void showInfo(object sender, EventArgs e)
        {
            string memName = ((Label)sender).Name;
            populateInfoPanel(FamilyMembers.Family.Get(memName));
        }

        private void recenterTree(object sender, EventArgs e)
        {
            createTree(((Label)sender).Name);
        }

        private void treePanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void AppMain_Resize(object sender, EventArgs e)
        {
            this.treePanel.Width = (int)Math.Round(this.Width * 0.75f - (this.treePanel.VerticalScroll.Visible ? 25 : 25));
            this.infoPanel.Width = (int)Math.Round(this.Width * 0.25f);

            this.pnlMarkerInfo.Width = (int)Math.Round(this.Width * 0.25f);

            createTree(this.currentMember);
        }

        private void gMap_MarkerClick(GMapMarker sender, MouseEventArgs e)
        {
            Member mem = (Member)sender.Tag; //FamilyMembers.getMember(sender.ToolTipText);
            populateMapInfoPanel(mem);
        }

        private void yearRange_ValueChanged(object sender, EventArgs e)
        {
            int value = ((TrackBar)sender).Value;
            lblCurrentYear.Text = "" + value;

            GMapOverlay overlay = (from o in this.gMap.Overlays
                                   where o.Id == "marker"
                                   select o).FirstOrDefault();

            if(overlay == null)
            {
                return;
            }

            Maps.setMarkers(overlay, value);
        }

        private void lblGotoTree_Click(object sender, EventArgs e)
        {
            this.currentMember = (string)((Label)sender).Tag;
            this.tabControl1.SelectedIndex = 0;
            createTree(this.currentMember);
            populateInfoPanel(FamilyMembers.getMember(this.currentMember));
        }

        private void lblGotoMap_Click(object sender, EventArgs e)
        {
            GMapOverlay overlay = (from o in this.gMap.Overlays
                                   where o.Id == "marker"
                                   select o).FirstOrDefault();
            if (overlay == null)
            {
                return;
            }

            Member mem = (Member)((Label)sender).Tag;

            if (mem == null || mem.BirthDate.Year == 1)
            {
                return;
            }

            Tuple<double, double> loc = Maps.lookupLocation(mem.BirthLocation);

            if(loc == null)
            {
                return;
            }

            this.tabControl1.SelectedIndex = 1;

            gMap.Position = new PointLatLng(loc.Item1, loc.Item2);

            yearRange.Value = mem.BirthDate.Year;
            
            populateMapInfoPanel(mem);
        }
                
        private void keyUpdateSearch(object sender, KeyEventArgs e)
        {
            setListData();
        }

        private void searchResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            string member = (string)((ListBox)sender).SelectedItem;
            populateSearchInfoPanel(FamilyMembers.getMember(member));
        }
    }
}
