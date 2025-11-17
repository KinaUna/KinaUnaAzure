using System;
using KinaUna.Data.Models.Family;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using KinaUna.Data.Models.AccessManagement;

namespace KinaUnaWeb.Models.FamiliesViewModels
{
    public class FamilyMemberDetailsViewModel: BaseItemsViewModel
    {
        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
        public FamilyMemberDetailsViewModel()
        {
            
        }

        public FamilyMemberDetailsViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            ReadOnlyCollection<TimeZoneInfo> tzs = TimeZoneInfo.GetSystemTimeZones();
            TimezoneList =
            [
                .. tzs.Select(tz => new SelectListItem()
                {
                    Text = tz.DisplayName,
                    Value = tz.Id
                })
            ];

            SetMemberTypeList();
        }

        public FamilyMember FamilyMember { get; set; } = new();
        public Family Family { get; set; }
        public FamilyMemberType MemberType { get; set; } = FamilyMemberType.Unknown;
        public List<SelectListItem> MemberTypeList = [];
        public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.None;
        public SelectListItem[] TimezoneList { get; init; }

        public void SetMemberTypeList()
        {
            MemberTypeList = [];
            foreach (FamilyMemberType type in (FamilyMemberType[])Enum.GetValues(typeof(FamilyMemberType)))
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
