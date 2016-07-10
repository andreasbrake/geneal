using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;

namespace Geneal
{
    public class FamilyMembers
    {
        private DataSource _ds;
        private List<Member> _family;
        private Member[][] _nodes;
        private Member _rootUser;
        private Tuple<Member, float, float>[] _nodeAssignments;
        private Tuple<float, float, float, float>[] _linesAssignments;

        public List<Member> Family
        {
            get { return _family; }
        }
        public Tuple<Member, float, float>[] NodeAssignments
        {
            get { return _nodeAssignments; }
        }
        
        public int maxDepth { get; set; }
        private int visibleDepth;

        public FamilyMembers(Maps map)
        {
            this._ds = new DataSource(map);
            this._family = _ds.getMembers();

            recenterTree(Preferences.RootUser);
            assignGenerations();
        }

        public void RefreshData()
        {
            this._ds.LoadDataFile();
            this._family = _ds.getMembers();

            recenterTree(Preferences.RootUser);
            assignGenerations();
        }

        public void ExportCurrentFamily(string exportType)
        {
            switch(exportType.ToUpper())
            {
                case "GEDCOM":
                    this._ds.ExportToPlainText(new DataConverter(_family).ToGEDCOM());
                    break;
                default:
                    this._ds.WriteCurrentToDataFile();
                    break;
            }
        }

        public void LoadFamilyFromFile(string filepath)
        {
            this._ds.LoadJsonFile(filepath);
            this._family = _ds.getMembers();

            recenterTree(Preferences.RootUser);
            assignGenerations();

            this._ds.WriteToDataFile();
        }
        
        public void recenterTree(string mem)
        {
            recenterTree(getMember(mem));
        }
        public void recenterTree(Member mem)
        {
            _rootUser = mem;
            maxDepth = _family.Count > 0 ? (from m in _family
                                            select m.Generations.Count > 0 ? m.Generations.Select(g => g.Depth).Max() : -1
                                           ).Max() : 0;
        }

        public Member getMember(string mem)
        {
            if(mem == null)
            {
                return null;
            }

            return (from m in _family where m.Name.ToUpper() == mem.ToUpper() select m).FirstOrDefault();
        }

        public string getFirstChildRegion(Member mem)
        {
            List<Member> children = (from m in _family
                                     where m.Parent1.ToUpper() == mem.Name.ToUpper() || m.Parent2.ToUpper() == mem.Name.ToUpper()
                                     select m).ToList();

            for(var i=0; i < children.Count; i++)
            {
                if(children[i].BirthRegion != null && children[i].BirthRegion != "")
                {
                    return children[i].BirthRegion;
                }
                if (children[i].BirthCountry != null && children[i].BirthCountry != "")
                {
                    return children[i].BirthCountry;
                }
            }

            if(children.Count > 0)
            {
                return getFirstChildRegion(children[0]);
            }

            return "UNKNOWN";
        }

        public int getFirstBirthYear()
        {
            return (from f in _family
                    where f.BirthDate.Year > 1 && f.Generations.Count > 0
                    orderby f.BirthDate ascending
                    select f.BirthDate.Year).FirstOrDefault();
        }

        public List<Member> getLiving(int year)
        {
            return (from f in _family
                    where (f.BirthDate.Year > 1 && f.BirthDate.Year <= year || f.BirthDate.Year == 1 && f.DeathDate.Year - 75 <= year)
                        && (f.DeathDate.Year >= year || f.DeathLoction == "Still Alive" || f.DeathDate.Year == 1 && f.BirthDate.Year + 75 >= year) 
                        && f.Generations.Count > 0
                    select f).ToList();
        }

        public void assignGenerations()
        {
            Member newRoot = _rootUser;

            if (newRoot != null && newRoot.Generations.Count == 0)
            {
                _family = _family.Cast<Member>().Select(m => {
                    m.Generations = new List<Generation>();
                    return m;
                }).ToList();

                assignGeneration(newRoot, 0, 0);
            }
            else if(newRoot != null)
            {
                for(int i=0; i < _family.Count; i++)
                {
                    if(_family[i].Name.ToUpper() == newRoot.Name.ToUpper())
                    {
                        continue;
                    }

                    List<Generation> newGenerations = new List<Generation>();
                    for (int j = 0; j < _family[i].Generations.Count; j++)
                    {
                        if(_family[i].Name == "brake,wallace,1939")
                        {
                            int a = 1;
                        }

                        for(int k=0; k < newRoot.Generations.Count; k++)
                        {
                            int depthDiff = _family[i].Generations[j].Depth - newRoot.Generations[k].Depth;
                            if(depthDiff < 0)
                            {
                                continue;
                            }
                            int newBreadth = _family[i].Generations[j].Breadth - (newRoot.Generations[k].Breadth * (int)Math.Pow(2, depthDiff));
                            if (newBreadth < 0 || newBreadth >= Math.Pow(2, depthDiff))
                            {
                                continue;
                            }
                            newGenerations.Add(new Generation()
                            {
                                Depth = depthDiff,
                                Breadth = newBreadth
                            });
                        }
                    }
                    _family[i].Generations = newGenerations;
                }

                _family.Where(m => m.Name.ToUpper() == newRoot.Name.ToUpper()).First().Generations = new List<Generation>() {
                    new Generation()
                    {
                        Depth = 0,
                        Breadth = 0
                    }
                };
            }
        }
        private void assignGeneration(Member mem, int depth, int breadth)
        {
            string memName = mem.Name;
            
            mem.Generations.Add(new Generation()
            {
                Depth = depth,
                Breadth = breadth
            });
            //_family.Update(mem);

            Member p1 = getMember(mem.Parent1), p2 = getMember(mem.Parent2);

            if(mem.Parent1.ToUpper() == mem.Name.ToUpper() || mem.Parent2.ToUpper() == mem.Name.ToUpper())
            {
                var b = 1;
            }
            if (p1 != null)
            {
                assignGeneration(p1, depth + 1, breadth * 2);
            }

            if (p2 != null)
            {
                assignGeneration(p2, depth + 1, breadth * 2 + 1);
            }
        }

        public Member[][] assignNodes(string root, int maxDepth = -1)
        {
            return assignNodes(getMember(root), maxDepth);
        }
        public Member[][] assignNodes(Member root, int maxDepth = -1)
        {
            if(root == null)
            {
                return new Member[0][];
            }

            this.maxDepth = _family.Count > 0 ? (from m in _family
                                            select m.Generations.Count > 0 ? m.Generations.Select(g => g.Depth).Max() : -1
                                           ).Max() : 0;

            _nodes = new Member[(maxDepth > 0 ? maxDepth : maxDepth) + 2][];
            
            for(int i=0; i <= (maxDepth > 0 ? maxDepth : maxDepth); i++)
            {
                _nodes[i + 1] = new Member[(int)Math.Pow(2, i)];
            }


            _nodes[0] = (from m in _family
                         orderby
                            m.Generations.Count > 0 ? m.Generations.Min(g => g.Breadth) : m.BirthDate.Year, m.Generations.Count > 0 ? m.Generations.Min(g => g.Depth) : m.DeathDate.Year
                         where 
                            (m.Parent1.ToUpper() == root.Name.ToUpper() || m.Parent2.ToUpper() == root.Name.ToUpper()) && 
                            m.Generations.Count > 0
                         select m).ToArray();

            this.visibleDepth = maxDepth;

            addNodes(root, 1, 0, maxDepth + 1);

            return _nodes;
        }

        private void addNodes(Member mem, int depth, int breadth, int maxDepth)
        {
            string memName = mem.Name;
            string parent1Name = mem.Parent1;
            string parent2Name = mem.Parent2;

            _nodes[depth][breadth] = mem;
            
            Member p1 = getMember(mem.Parent1), p2 = getMember(mem.Parent2);

            if (p1 != null && (maxDepth < 0 || depth < maxDepth) )
            {
                addNodes(p1, depth + 1, breadth * 2, maxDepth);
            }

            if (p2 != null && (maxDepth < 0 || depth < maxDepth))
            {
                addNodes(p2, depth + 1, breadth * 2 + 1, maxDepth);
            }
        }

        public void generateNodePlacements(float zoom)
        {
            float RECT_HEIGHT = 25 / zoom;
            float RECT_WIDTH = 150 / zoom;

            List<Tuple<Member, float, float>> nodeAssignmentsList = new List<Tuple<Member, float, float>>();
            List<Tuple<float, float, float, float>> linesList = new List<Tuple<float, float, float, float>>();

            float maxHeight = RECT_HEIGHT * (int)Math.Pow(2, _nodes.Length - 1);

            for (int i = 0; i < _nodes.Length; i++)
            {
                for (int j = 0; j < _nodes[i].Length; j++)
                {
                    if (_nodes[i][j] == null)
                    {
                        continue;
                    }

                    float xValue = (RECT_WIDTH) * i + (20 / zoom);
                    float yValue = (j * 2 + 1) * maxHeight / (2f * _nodes[i].Length) - (RECT_HEIGHT / 2);

                    nodeAssignmentsList.Add(new Tuple<Member, float, float>(_nodes[i][j], xValue, yValue));
                }
            }

            _nodeAssignments = nodeAssignmentsList.ToArray();
            _linesAssignments = linesList.ToArray();
        }

        public void drawToBitmap(Graphics minimap, int imgWidth, int imgHeight, Member baseMember)
        {
            int depth = maxDepth;
            int pixelWidth = imgWidth / depth;
            
            for (int i = 0; i < depth; i++)
            {
                int itemHeight = imgHeight / (int)Math.Pow(2, i);
                int spread = 1;

                if (imgHeight < Math.Pow(2, i))
                {
                    itemHeight = 1;
                    spread = (int)Math.Pow(2, i) / imgHeight;
                }

                int vSections = (int)Math.Pow(2, i) / spread;
                for (int j=0; j < vSections; j++)
                {
                    double completion = membersExistingBetween(i, j * spread, spread) / (1.0 * spread);

                    if (isRangeVisible(baseMember, 8, i, j, spread) /*&& false Disable this to allow positioning*/
                        )
                    {
                        minimap.FillRectangle(
                            new SolidBrush(Color.FromArgb(
                                (byte)(255 * completion),
                                (byte)255,
                                (byte)140,
                                (byte)0
                            )),
                            i * pixelWidth,
                            j * itemHeight,
                            pixelWidth,
                            itemHeight
                        );
                    }
                    else
                    {
                        minimap.FillRectangle(
                            new SolidBrush(Color.FromArgb(
                                (byte)(255 * completion),
                                (byte)0,
                                (byte)0,
                                (byte)0
                            )),
                            i * pixelWidth,
                            j * itemHeight,
                            pixelWidth,
                            itemHeight
                        );
                    }
                    
                }
            }
        }

        private int membersExistingBetween(int gen, int genIndex, int genRange)
        {
            List<Member> mem = _family.Where(m =>
                m.Generations.Where(g =>
                    g.Depth == gen &&
                    g.Breadth >= genIndex &&
                    g.Breadth < genIndex + genRange
               ).FirstOrDefault() != null).ToList();
            return mem.Count;
        }

        private Boolean isRangeVisible(Member rootMemb, int maxDepth, int depth, int breadth, int spread)
        {
            for(int i=0; i < rootMemb.Generations.Count; i++)
            {
                int visibleBreadthStart = rootMemb.Generations[i].Breadth * (int)Math.Pow(2, depth - rootMemb.Generations[i].Depth);
                int visibleBreadthEnd = (rootMemb.Generations[i].Breadth + 1) * (int)Math.Pow(2, depth - rootMemb.Generations[i].Depth);

                if (depth >= rootMemb.Generations[i].Depth && depth < rootMemb.Generations[i].Depth + maxDepth &&
                        breadth * spread >= visibleBreadthStart && breadth * spread < visibleBreadthEnd)
                {
                    return true;
                }
            }
            return false;            
        }

        private Boolean isRangeVisible_Gen(Generation rootMemb, int maxDepth, int depth, int breadth, int spread)
        {
            int visibleBreadthStart = rootMemb.Breadth * (int)Math.Pow(2, depth - rootMemb.Depth);
            int visibleBreadthEnd = (rootMemb.Breadth + 1) * (int)Math.Pow(2, depth - rootMemb.Depth);

            return (depth >= rootMemb.Depth && depth < rootMemb.Depth + maxDepth &&
                    breadth * spread >= visibleBreadthStart && breadth * spread < visibleBreadthEnd);
        }

        public string[] getAlphabeticalLike(
            string first, string last,
            int birthStart, int birthEnd, string birthLocation, string birthRegion,
            int deathStart, int deathEnd, string deathLocation)
        {
            try
            {

                return (from m in _family
                        where
                            (first != "" ? Regex.Match(String.Join(",", m.Name.Split(',').Skip(1)).ToUpper(), first).Success : true)
                            &&
                            (last != "" ? Regex.Match(m.Name.Split(',')[0].ToUpper(), last).Success : true)
                            &&
                            (birthLocation != "" ? Regex.Match(m.BirthLocation.ToUpper(), birthLocation).Success : true)
                            &&
                            (birthRegion != "" ? Regex.Match(m.BirthRegion ?? "", birthRegion).Success || m.BirthRegion == null && Regex.Match(m.BirthCountry ?? "", birthRegion).Success : true)
                            &&
                            (deathLocation != "" ? Regex.Match(m.DeathLoction.ToUpper(), deathLocation).Success : true)
                            &&
                            (birthStart >= 0 ? m.BirthDate.Year >= birthStart : true)
                            &&
                            (birthEnd >= 0 ? m.BirthDate.Year <= birthEnd : true)
                            &&
                            (deathStart >= 0 ? m.DeathDate.Year >= deathStart : true)
                            &&
                            (deathEnd >= 0 ? m.DeathDate.Year <= deathEnd : true)
                        orderby (m.Name.Split(',')[0] + m.Name.Split(',')[1]) ascending, m.BirthDate descending
                        select m.Name).ToArray();
            }
            catch(Exception e)
            {
                return new string[0];
            }
        }

        public FamilyMembers Clone()
        {
            return (FamilyMembers)this.MemberwiseClone();
        }

    }
}
