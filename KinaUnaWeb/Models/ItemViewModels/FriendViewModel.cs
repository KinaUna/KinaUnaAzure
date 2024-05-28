using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class FriendViewModel: BaseItemsViewModel
    {
        public List<SelectListItem> ProgenyList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }
        public Friend FriendItem { get; set; } = new();
        public List<SelectListItem> FriendTypeListEn { get; set; }
        public List<SelectListItem> FriendTypeListDa { get; set; }
        public List<SelectListItem> FriendTypeListDe { get; set; }
        public string FileName { get; set; }
        public IFormFile File { get; init; }
        public string TagFilter { get; set; }

        public FriendViewModel()
        {
            ProgenyList = [];
            SetAccessLevelList();
            SetFriendTypeList();
        }

        public FriendViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            SetAccessLevelList();
            SetFriendTypeList();
        }

        public void SetProgenyList()
        {
            FriendItem.ProgenyId = CurrentProgenyId;
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

        public void SetPropertiesFromFriendItem(Friend friend, bool isAdmin)
        {
            FriendItem.ProgenyId = friend.ProgenyId;
            FriendItem.AccessLevel = friend.AccessLevel;
            FriendItem.FriendAddedDate = friend.FriendAddedDate;
            FriendItem.FriendSince = friend.FriendSince;
            FriendItem.Name = friend.Name;
            FriendItem.Description = friend.Description;
            FriendItem.FriendId = friend.FriendId;
            FriendItem.PictureLink = friend.PictureLink;
            FriendItem.Type = friend.Type;
            FriendItem.Context = friend.Context;
            FriendItem.Notes = friend.Notes;
            FriendItem.Author = friend.Author;
            FriendItem.Tags = friend.Tags;
            
            IsCurrentUserProgenyAdmin = isAdmin;
        }

        public Friend CreateFriend()
        {
            Friend friendItem = new()
            {
                FriendId = FriendItem.FriendId,
                ProgenyId = FriendItem.ProgenyId,
                Description = FriendItem.Description,
                PictureLink = FriendItem.PictureLink,
                Name = FriendItem.Name,
                AccessLevel = FriendItem.AccessLevel,
                Type = FriendItem.Type,
                Context = FriendItem.Context,
                Notes = FriendItem.Notes,
                Author = FriendItem.Author,
                FriendAddedDate = FriendItem.FriendAddedDate
            };

            FriendItem.FriendSince ??= DateTime.UtcNow;
            friendItem.FriendSince = FriendItem.FriendSince;

            if (!string.IsNullOrEmpty(FriendItem.Tags))
            {
                friendItem.Tags = FriendItem.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }
            
            return friendItem;
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListDa = accessLevelList.AccessLevelListDa;
            AccessLevelListDe = accessLevelList.AccessLevelListDe;

            if (LanguageId == 2)
            {
                AccessLevelListEn = AccessLevelListDe;
            }

            if (LanguageId == 3)
            {
                AccessLevelListEn = AccessLevelListDa;
            }

            AccessLevelListEn[FriendItem.AccessLevel].Selected = true;
            AccessLevelListDa[FriendItem.AccessLevel].Selected = true;
            AccessLevelListDe[FriendItem.AccessLevel].Selected = true;
        }

        public void SetFriendTypeList()
        {
            FriendTypeListEn = [];
            SelectListItem friendType1 = new()
            {
                Text = "Personal Friend",
                Value = "0"
            };
            SelectListItem friendType2 = new()
            {
                Text = "Toy/Animal Friend",
                Value = "1"
            };
            SelectListItem friendType3 = new()
            {
                Text = "Parent",
                Value = "2"
            };
            SelectListItem friendType4 = new()
            {
                Text = "Family",
                Value = "3"
            };
            SelectListItem friendType5 = new()
            {
                Text = "Caretaker",
                Value = "4"
            };
            FriendTypeListEn.Add(friendType1);
            FriendTypeListEn.Add(friendType2);
            FriendTypeListEn.Add(friendType3);
            FriendTypeListEn.Add(friendType4);
            FriendTypeListEn.Add(friendType5);

            FriendTypeListDa = [];
            SelectListItem friendType1Da = new()
            {
                Text = "Personlig ven",
                Value = "0"
            };
            SelectListItem friendType2Da = new()
            {
                Text = "Legetøj/Dyr",
                Value = "1"
            };
            SelectListItem friendType3Da = new()
            {
                Text = "Forældre",
                Value = "2"
            };
            SelectListItem friendType4Da = new()
            {
                Text = "Familie",
                Value = "3"
            };
            SelectListItem friendType5Da = new()
            {
                Text = "Omsorgsperson/Plejer/Pædagog",
                Value = "4"
            };
            FriendTypeListDa.Add(friendType1Da);
            FriendTypeListDa.Add(friendType2Da);
            FriendTypeListDa.Add(friendType3Da);
            FriendTypeListDa.Add(friendType4Da);
            FriendTypeListDa.Add(friendType5Da);

            FriendTypeListDe = [];
            SelectListItem friendType1De = new()
            {
                Text = "Persönliche Freunde",
                Value = "0"
            };
            SelectListItem friendType2De = new()
            {
                Text = "Spielzeuge/Tiere",
                Value = "1"
            };
            SelectListItem friendType3De = new()
            {
                Text = "Eltern",
                Value = "2"
            };
            SelectListItem friendType4De = new()
            {
                Text = "Familie",
                Value = "3"
            };
            SelectListItem friendType5De = new()
            {
                Text = "Betreuer",
                Value = "4"
            };
            FriendTypeListDe.Add(friendType1De);
            FriendTypeListDe.Add(friendType2De);
            FriendTypeListDe.Add(friendType3De);
            FriendTypeListDe.Add(friendType4De);
            FriendTypeListDe.Add(friendType5De);

            if (LanguageId == 2)
            {
                FriendTypeListEn = FriendTypeListDe;
            }

            if (LanguageId == 3)
            {
                FriendTypeListEn = FriendTypeListDa;
            }

            FriendTypeListEn[FriendItem.Type].Selected = true;
            FriendTypeListDa[FriendItem.Type].Selected = true;
            FriendTypeListDe[FriendItem.Type].Selected = true;
        }
    }
}
