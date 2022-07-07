using System.Collections.Generic;

namespace KinaUna.Data.Models
{
    public class Family
    {
        public int Id { get; set; }
        public List<Progeny> Children { get; set; }
        public List<UserInfo> FamilyMembers { get; set; }
        public List<UserInfo> OtherMembers { get; set; }
        public List<UserAccess> AccessList { get; set; }
        public AccessLevelList AccessLevelList { get; set; }
    }
}
