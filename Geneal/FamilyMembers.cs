using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Geneal
{
    public class FamilyMembers
    {
        private DataSource _ds;
        private MemberCollection _family;
        private MemberCollection _familyExtended;
        private Member[][] _nodes;
        private Member _rootUser;
        private Tuple<Member, float, float>[] _nodeAssignments;
        private Tuple<float, float, float, float>[] _linesAssignments;

        public MemberCollection Family
        {
            get { return _family; }
        }
        public MemberCollection FamilyExtended
        {
            get { return _familyExtended; }
        }
        public Tuple<Member, float, float>[] NodeAssignments
        {
            get { return _nodeAssignments; }
        }

        private string _dataSourceFile;

        public FamilyMembers(string _dataSourceFile, Maps map)
        {
            this._dataSourceFile = _dataSourceFile;

            _ds = new DataSource("JSON", _dataSourceFile, map);
            _family = _ds.getMembers();

            this.Init();
        }

        public void Init()
        {
            recenterTree(Preferences.RootUser);
            assignGenerations();
            _familyExtended = new MemberCollection();
        }

        public void recenterTree(string mem)
        {
            recenterTree(_family.Get(mem));
        }
        public void recenterTree(Member mem)
        {
            _rootUser = mem;
            _family.setMaxDepth(_rootUser);
        }

        public Member getMember(string mem)
        {
            return _family.Get(mem);
        }

        public int getFirstBirthYear()
        {
            return (from f in _family.Cast<Member>()
                    where f.BirthDate.Year > 1 && f.Generation >= 0
                    orderby f.BirthDate ascending
                    select f.BirthDate.Year).First();
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
            _family = new MemberCollection(new ArrayList(_family.Cast<Member>().Select(m => { m.Generation = -1; m.GenerationIndex = -1; return m; }).ToList()));

            Member rootMember = _family.Get(Preferences.RootUser);
            assignGeneration(rootMember, 0, 0);
        }
        private void assignGeneration(Member mem, int depth, int breadth)
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

        public Member[][] assignNodes(string root, int maxDepth = -1)
        {
            return assignNodes(_family.Get(root), maxDepth);
        }
        public Member[][] assignNodes(Member root, int maxDepth = -1)
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

        private void addNodes(Member mem, int depth, int breadth, int maxDepth)
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

        public void extendFamily(string root)
        {
            _familyExtended = new MemberCollection();
            Member rootmem = _family.Get(root);
            Member p0 = new Member()
            {
                BirthLocation = rootmem.BirthLocation,
                Generation = 0,
                Parent1 = rootmem.Parent1,
                Parent2 = rootmem.Parent2
            };
            int maxDepth = _family.Cast<Member>().Select(m => m.Generation).Max();
            extendNode(p0, 0, (maxDepth > 12 ? 12 : maxDepth));
        }

        private void extendNode(Member mem, int depth, int maxDepth)
        {
            _familyExtended.Add(mem);
            //Console.Write(depth + "," + breadth + "\n");

            if (depth > maxDepth)
            {
                return;
            }

            Member p1 = _family.Get(mem.Parent1), p2 = _family.Get(mem.Parent2);

            if (p1 == null)
            {
                p1 = new Member()
                {
                    BirthLocation = mem.BirthLocation,
                    Generation = depth + 1,
                    Parent1 = "",
                    Parent2 = ""
                };
            }
            else
            {
                p1 = new Member()
                {
                    BirthLocation = p1.BirthCountry != "" ? p1.BirthLocation :  mem.BirthLocation,
                    Generation = depth + 1,
                    Parent1 = p1.Parent1,
                    Parent2 = p1.Parent2
                };
            }
            
            if (p2 == null)
            {
                p2 = new Member()
                {
                    BirthLocation = mem.BirthLocation,
                    Generation = depth + 1,
                    Parent1 = "",
                    Parent2 = ""
                };
            }
            else
            {
                p2 = new Member()
                {
                    BirthLocation = p2.BirthCountry != "" ? p2.BirthLocation : mem.BirthLocation,
                    Generation = depth + 1,
                    Parent1 = p2.Parent1,
                    Parent2 = p2.Parent2
                };
            }

            extendNode(p1, depth + 1, maxDepth);
            extendNode(p2, depth + 1, maxDepth);

            return;
        }

    }
}
