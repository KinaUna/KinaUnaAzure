using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class FamilyDTO
    {
        public int Id { get; set; }
        public List<Progeny> Children { get; set; } = [];
        public List<UserInfo> FamilyMembers { get; set; } = [];
        public List<UserInfo> OtherMembers { get; set; } = [];
        public List<UserAccess> AccessList { get; set; } = [];
        public AccessLevelList AccessLevelList { get; set; } = new();

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
