using System.Collections.Generic;

namespace KinaUnaWeb.Models
{
    public class Family
    {
        public int Id { get; set; }
        public List<Progeny> Children { get; set; }
        public List<ApplicationUser> FamilyMembers { get; set; }
        public List<ApplicationUser> OtherMembers { get; set; }
        public List<UserAccess> AccessList { get; set; }
        public AccessLevelList AccessLevelList { get; set; }
    }
}
