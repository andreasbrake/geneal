using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Geneal
{
    public class MemberCollection : CollectionBase
    {
        public int maxDepth = -1;

        public MemberCollection() { }

        public MemberCollection(ArrayList members) 
        { 
            for(int i=0; i < members.Count; i++)
            {
                List.Add(members[i]);
            }
        }

        public Member Get(int i) 
        {
            return (Member)List[i];
        }

        public Member Get(string mem)
        {
            return ( from m in List.Cast<Member>().ToList()
                     where m.Name.ToUpper() == mem.ToUpper()
                     select m ).FirstOrDefault();
        }

        public void Update(Member setMem)
        {
            for(int i = 0; i < List.Count; i++)
            {
                if (((Member)List[i]).Name.ToUpper() == setMem.Name.ToUpper())
                {
                    List[i] = setMem;
                    return;
                }
            }
        }

        public string[] getAlphabeticalLike(
            string first, string last, 
            int birthStart, int birthEnd, string birthLocation,
            int deathStart, int deathEnd, string deathLocation)
        {
            return (from m in List.Cast<Member>().ToList()
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

        public Tuple<Member, Member> GetParents(string mem)
        {

            return this.GetParents(this.Get(mem));
        }

        public Tuple<Member, Member> GetParents(Member mem)
        {
            Member parent1 = this.Get(mem.Parent1);
            Member parent2 = this.Get(mem.Parent2);
            
            return new Tuple<Member, Member>(parent1, parent2);
        }

        public Member[] GetChildren(Member mem)
        {
            return (from m in List.Cast<Member>().ToList()
                    where m.Generation >= 0 && (m.Parent1.ToUpper() == mem.Name.ToUpper() || m.Parent2.ToUpper() == mem.Name.ToUpper())
                    select m).ToArray();
        }

        public void setMaxDepth(Member mem)
        {
            this.maxDepth = getMaxDepth(mem, 0);
        }

        private int getMaxDepth(Member mem, int depth)
        {
            string memName = mem.Name;

            Tuple<Member, Member> parents = this.GetParents(mem);

            int parent1 = depth;
            int parent2 = depth;

            if (parents.Item1 != null) parent1 = getMaxDepth(parents.Item1, depth + 1);
            if (parents.Item2 != null) parent2 = getMaxDepth(parents.Item2, depth + 1);


            return parent1 > parent2 ? parent1 : parent2;
        }
    }
}
