using System;
using System.Collections.Generic;
using System.Linq;

namespace Geneal
{
    public class FamilyMembers
    {
        private static DataSource _ds;
        private static MemberCollection _family;
        private static Member[][] _nodes;
        private static Member _rootUser;
        private static Tuple<Member, float, float>[] _nodeAssignments;
        private static Tuple<float, float, float, float>[] _linesAssignments;

        public static MemberCollection Family
        {
            get { return _family; }
        }
        public static Tuple<Member, float, float>[] NodeAssignments
        {
            get { return _nodeAssignments; }
        }

        public FamilyMembers() 
        {
            _ds = new DataSource("JSON", "combined.json");
            _family = _ds.getMembers();
            recenterTree(Preferences.RootUser);
            assignGenerations();
        }

        public static void recenterTree(string mem)
        {
            recenterTree(_family.Get(mem));
        }
        public static void recenterTree(Member mem)
        {
            _rootUser = mem;
            _family.setMaxDepth(_rootUser);
        }

        public static Member getMember(string mem)
        {
            return _family.Get(mem);
        }

        public static int getFirstBirthYear()
        {
            return (from f in _family.Cast<Member>()
                    where f.BirthDate.Year > 1 && f.Generation >= 0
                    orderby f.BirthDate ascending
                    select f.BirthDate.Year).First();
        }

        public static List<Member> getLiving(int year)
        {
            return (from f in _family.Cast<Member>()
                    where f.BirthDate.Year <= year 
                        && (f.DeathDate.Year >= year || f.DeathLoction == "Still Alive" || f.DeathDate.Year == 1 && f.BirthDate.Year + 75 >= year) 
                        && f.Generation >= 0
                    select f).ToList();
        }

        public void assignGenerations()
        {
            Member rootMember = _family.Get(Preferences.RootUser);
            assignGeneration(rootMember, 0, 0);
        }
        public void assignGeneration(Member mem, int depth, int breadth)
        {
            string memName = mem.Name;
            string parent1Name = mem.Parent1;
            string parent2Name = mem.Parent2;

            mem.Generation = depth;
            mem.GenerationIndex = breadth;
            _family.Update(mem);
            
            Tuple<Member, Member> parents = _family.GetParents(mem);

            if (parents.Item1 != null)
            {
                assignGeneration(parents.Item1, depth + 1, breadth * 2);
            }

            if (parents.Item2 != null)
            {
                assignGeneration(parents.Item2, depth + 1, breadth * 2 + 1);
            }
        }

        public static Member[][] assignNodes(string root, int maxDepth = -1)
        {
            return assignNodes(_family.Get(root), maxDepth);
        }
        public static Member[][] assignNodes(Member root, int maxDepth = -1)
        {
            _nodes = new Member[(maxDepth > 0 ? maxDepth : _family.maxDepth) + 2][];
            
            for(int i=0; i <= (maxDepth > 0 ? maxDepth :_family.maxDepth); i++)
            {
                _nodes[i + 1] = new Member[(int)Math.Pow(2, i)];
            }

            _nodes[0] = _family.GetChildren(root);

            addNodes(root, 1, 0, maxDepth + 1);
            
            return _nodes;
        }

        private static void addNodes(Member mem, int depth, int breadth, int maxDepth)
        {
            string memName = mem.Name;
            string parent1Name = mem.Parent1;
            string parent2Name = mem.Parent2;

            _nodes[depth][breadth] = mem;

            Tuple<Member, Member> parents = _family.GetParents(mem);

            if(parents.Item1 != null && (maxDepth < 0 || depth < maxDepth) )
            {
                addNodes(parents.Item1, depth + 1, breadth * 2, maxDepth);
            }

            if (parents.Item2 != null && (maxDepth < 0 || depth < maxDepth))
            {
                addNodes(parents.Item2, depth + 1, breadth * 2 + 1, maxDepth);
            }
        }

        public static void generateNodePlacements(float zoom)
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

    }
}
