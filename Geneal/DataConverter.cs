using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Geneal
{
    public class DataConverter
    {
        private Member[] _family;
        private List<GEDCOMFamily> _families;

        public DataConverter(List<Member> family)
        {
            this._family = family.Select(m => m.Clone()).ToArray();
            this._families = new List<GEDCOMFamily>();
        }

        public string ToGEDCOM()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < _family.Length; i++)
            {
                _family[i].MemRef = "I" + i;
            }

            int l = 0;
            #region header
            sb.AppendLine(l + " HEAD");
            l++;

            sb.AppendLine(l + " SOUR CESTGENEAL");
            l++;
            sb.AppendLine(l + " VERS " + Assembly.GetExecutingAssembly().GetName().Version);
            sb.AppendLine(l + " NAME C'est Geneal");
            l--;
            sb.AppendLine(l + " SUBM @S1@");
            sb.AppendLine(l + " GEDC");
            l++;
            sb.AppendLine(l + " VERS 5.5.1");
            sb.AppendLine(l + " FORM LINEAGE-LINKED");
            l--;
            sb.AppendLine(l + " CHAR UNICODE");
            sb.AppendLine(l + " LANG English");
            sb.AppendLine(l + " NOTE Tree of " + getMemberFromName(Preferences.RootUser).CleanName);

            l--;
            #endregion

            #region submission
            sb.AppendLine(l + " @S1@ SUBM");
            l++;

            sb.AppendLine(l + " NAME CESTGENEAL_EXPORT");

            l--;
            #endregion

            for (int i = 0; i < _family.Length; i++)
            {
                Member mem = _family[i];

                #region general
                sb.AppendLine(l + " @" + mem.MemRef + "@ INDI");
                l++;

                sb.AppendLine(l + " NAME " + mem.GEDCOMName);
                #endregion

                #region birth
                sb.AppendLine(l + " BIRT");
                l++;

                if (mem.BirthDate.Year > 1) sb.AppendLine(l + " DATE " + mem.BirthDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
                if (mem.BirthLocation != "") sb.AppendLine(l + " PLAC " + mem.BirthLocation);

                l--;
                #endregion

                #region death
                sb.AppendLine(l + " DEAT");
                l++;

                if(mem.BirthDate.Year > 1) sb.AppendLine(l + " DATE " + mem.BirthDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
                if (mem.DeathLoction != "") sb.AppendLine(l + " PLAC " + mem.DeathLoction);

                l--;
                #endregion

                #region family

                string famRef = getFamilyRefFromMember(mem);
                if(famRef.Length > 1)
                {
                    if (addChildToFamily(mem, famRef))
                    {
                        sb.AppendLine(l + " FAMC @" + famRef + "@");
                    }
                }

                #endregion

                l--;
            }

            #region families

            for (int i = 0; i < _families.Count; i++)
            {
                _families[i].flatten(l, sb);
            }

            #endregion

            sb.Append(l + " TRLR");

            return sb.ToString();
        }

        private Boolean familyExists(string famRef)
        {
            return (from GEDCOMFamily fam in _families where fam.famRef == famRef select fam).FirstOrDefault() != null;
        }
        private String getFamilyRefFromMember(Member mem)
        {
            return "F" + getMemberRefFromName(mem.Parent1) + getMemberRefFromName(mem.Parent2);
        }
        private Boolean addChildToFamily(Member child, string famRef)
        {
            if (_families.Where(f => f.famRef == famRef).FirstOrDefault() == null)
            {
                _families.Add(new GEDCOMFamily()
                {
                    p1 = getMemberFromName(child.Parent1),
                    p2 = getMemberFromName(child.Parent2),
                    children = new List<Member>()
                });
            }

            GEDCOMFamily fam = _families.Where(f => f.famRef == famRef).FirstOrDefault();
            fam.children.Add(child);

            return true;
        }
        private String getMemberRefFromName(string name)
        {
            if (name == "" || name == null) return "";
            return (from Member m in _family where m.Name.ToUpper() == name.ToUpper() select m.MemRef).FirstOrDefault();
        }
        private Member getMemberFromName(string name)
        {
            if (name == "" || name == null) return null;
            return (from Member m in _family where m.Name.ToUpper() == name.ToUpper() select m).FirstOrDefault();
        }
    }

    public class GEDCOMFamily
    {
        public String famRef { get { return "F" + (p1 != null ? p1.MemRef : "") + (p2 != null ? p2.MemRef : ""); } }
        public Member p1 { get; set; }
        public Member p2 { get; set; }
        public List<Member> children { get; set; }

        public void flatten(int l, StringBuilder sb)
        {
            sb.AppendLine(l + " @" + famRef + "@ FAM");
            l++;

            if (p1 != null) sb.AppendLine(l + " HUSB @" + p1.MemRef + "@");
            if (p2 != null) sb.AppendLine(l + " WIFE @" + p2.MemRef + "@");

            Member[] orderedChildren = children.Count > 0 ? children.OrderBy(m => m.BirthDate).ToArray() : new Member[0];
            sb.AppendLine(l + " NCHI " + orderedChildren.Length);
            for(int i=0; i < orderedChildren.Length; i++)
            {
                sb.AppendLine(l + " CHIL @" + orderedChildren[i].MemRef + "@");
            }
        }
    }
}
