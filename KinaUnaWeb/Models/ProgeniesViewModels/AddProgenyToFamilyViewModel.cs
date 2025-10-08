using System.Collections.Generic;
using KinaUna.Data.Models.Family;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ProgeniesViewModels
{
    public class AddProgenyToFamilyViewModel: BaseItemsViewModel
    {
        public AddProgenyToFamilyViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

        public FamilyMemberType MemberType { get; set; } = FamilyMemberType.Unknown;
        public List<SelectListItem> MemberTypeList = [];

        public void SetProgenyList()
        {
            foreach (SelectListItem item in ProgenyList)
            {
                if (item.Value == CurrentProgenyId.ToString())
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }
            }
        }

        public void SetFamilyList()
        {
            foreach (SelectListItem item in FamilyList)
            {
                if (item.Value == CurrentFamilyId.ToString())
                {
                    item.Selected = true;
                }
                else
                {
                    item.Selected = false;
                }
            }
        }

        public void SetMemberTypeList()
        {
            foreach (FamilyMemberType type in (FamilyMemberType[])System.Enum.GetValues(typeof(FamilyMemberType)))
            {
                MemberTypeList.Add(new SelectListItem
                {
                    Text = type.ToString(),
                    Value = ((int)type).ToString(),
                    Selected = type == MemberType
                });
            }
        }

    }
}
