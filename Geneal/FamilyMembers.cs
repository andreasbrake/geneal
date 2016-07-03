using System;
using System.Collections;
using System.Collections.Generic;
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

        public void ExportCurrentFamily()
        {
            this._ds.WriteCurrentToDataFile();
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
            maxDepth = _family.Count > 0 ? (from m in _family select m.Generation).Max() : 0;
        }

        public Member getMember(string mem)
        {
            return (from m in _family where m.Name.ToUpper() == mem.ToUpper() select m).FirstOrDefault();
        }

        public int getFirstBirthYear()
        {
            return (from f in _family
                    where f.BirthDate.Year > 1 && f.Generation >= 0
                    orderby f.BirthDate ascending
                    select f.BirthDate.Year).FirstOrDefault();
        }

        public List<Member> getLiving(int year)
        {
            return (from f in _family.Cast<Member>()
                    where (f.BirthDate.Year > 1 && f.BirthDate.Year <= year || f.BirthDate.Year == 1 && f.DeathDate.Year - 75 <= year)
                        && (f.DeathDate.Year >= year || f.DeathLoction == "Still Alive" || f.DeathDate.Year == 1 && f.BirthDate.Year + 75 >= year) 
                        && f.Generation >= 0
                    select f).ToList();
        }

        public void assignGenerations()
        {
            _family = _family.Cast<Member>().Select(m => { m.Generation = -1; m.GenerationIndex = -1; return m; }).ToList();

            Member rootMember = getMember(Preferences.RootUser);

            if(rootMember != null)
            {
                assignGeneration(rootMember, 0, 0);
            }
        }
        private void assignGeneration(Member mem, int depth, int breadth)
        {
            string memName = mem.Name;

            mem.Generation = depth;
            mem.GenerationIndex = breadth;
            //_family.Update(mem);
            
            Member p1 = getMember(mem.Parent1), p2 = getMember(mem.Parent2);

            if(mem.BirthRegion != null)
            {
                var a = 1;
            }
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

            _nodes = new Member[(maxDepth > 0 ? maxDepth : maxDepth) + 2][];
            
            for(int i=0; i <= (maxDepth > 0 ? maxDepth : maxDepth); i++)
            {
                _nodes[i + 1] = new Member[(int)Math.Pow(2, i)];
            }


            _nodes[0] = (from m in _family
                         where (m.Parent1 == root.Name || m.Parent2 == root.Name) && m.Generation >= 0
                         select m).ToArray();

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

        public string[] getAlphabeticalLike(
            string first, string last,
            int birthStart, int birthEnd, string birthLocation,
            int deathStart, int deathEnd, string deathLocation)
        {
            return (from m in _family
                    where
                        (first != "" ? Regex.Match(String.Join(",", m.Name.Split(',').Skip(1)).ToUpper(), first).Success : true)
                        &&
                        (last != "" ? Regex.Match(m.Name.Split(',')[0].ToUpper(), last).Success : true)
                        &&
                        (birthLocation != "" ? Regex.Match(m.BirthLocation.ToUpper(), birthLocation).Success : true)
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

        public FamilyMembers Clone()
        {
            return (FamilyMembers)this.MemberwiseClone();
        }

    }
}
