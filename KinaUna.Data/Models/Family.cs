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

        public Family()
        {
            Children = new List<Progeny>();
            FamilyMembers = new List<UserInfo>();
            OtherMembers = new List<UserInfo>();
            AccessList = new List<UserAccess>();
            AccessLevelList = new AccessLevelList();
        }

        public void SetAccessLevelList(int languageId)
        {
            if (languageId == 2)
            {
                AccessLevelList.AccessLevelListEn = AccessLevelList.AccessLevelListDe;
            }

            if (languageId == 3)
            {
                AccessLevelList.AccessLevelListEn = AccessLevelList.AccessLevelListDa;
            }
        }
    }
}
