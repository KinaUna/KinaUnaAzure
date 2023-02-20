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
        public Friend FriendItem { get; set; } = new Friend();
        public List<SelectListItem> FriendTypeListEn { get; set; }
        public List<SelectListItem> FriendTypeListDa { get; set; }
        public List<SelectListItem> FriendTypeListDe { get; set; }
        public string FileName { get; set; }
        public IFormFile File { get; set; }
        public string TagFilter { get; set; }

        public FriendViewModel()
        {
            ProgenyList = new List<SelectListItem>();
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
            Friend friendItem = new Friend();
            friendItem.FriendId = FriendItem.FriendId;
            friendItem.ProgenyId = FriendItem.ProgenyId;
            friendItem.Description = FriendItem.Description;
            friendItem.PictureLink = FriendItem.PictureLink;
            friendItem.Name = FriendItem.Name;
            friendItem.AccessLevel = FriendItem.AccessLevel;
            friendItem.Type = FriendItem.Type;
            friendItem.Context = FriendItem.Context;
            friendItem.Notes = FriendItem.Notes;
            friendItem.Author = FriendItem.Author;
            friendItem.FriendAddedDate = FriendItem.FriendAddedDate;
            
            if (FriendItem.FriendSince == null)
            {
                FriendItem.FriendSince = DateTime.UtcNow;
            }
            friendItem.FriendSince = FriendItem.FriendSince;

            if (!string.IsNullOrEmpty(FriendItem.Tags))
            {
                friendItem.Tags = FriendItem.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }
            
            return friendItem;
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new AccessLevelList();
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
            FriendTypeListEn = new List<SelectListItem>();
            SelectListItem friendType1 = new SelectListItem();
            friendType1.Text = "Personal Friend";
            friendType1.Value = "0";
            SelectListItem friendType2 = new SelectListItem();
            friendType2.Text = "Toy/Animal Friend";
            friendType2.Value = "1";
            SelectListItem friendType3 = new SelectListItem();
            friendType3.Text = "Parent";
            friendType3.Value = "2";
            SelectListItem friendType4 = new SelectListItem();
            friendType4.Text = "Family";
            friendType4.Value = "3";
            SelectListItem friendType5 = new SelectListItem();
            friendType5.Text = "Caretaker";
            friendType5.Value = "4";
            FriendTypeListEn.Add(friendType1);
            FriendTypeListEn.Add(friendType2);
            FriendTypeListEn.Add(friendType3);
            FriendTypeListEn.Add(friendType4);
            FriendTypeListEn.Add(friendType5);

            FriendTypeListDa = new List<SelectListItem>();
            SelectListItem friendType1Da = new SelectListItem();
            friendType1Da.Text = "Personlig ven";
            friendType1Da.Value = "0";
            SelectListItem friendType2Da = new SelectListItem();
            friendType2Da.Text = "Legetøj/Dyr";
            friendType2Da.Value = "1";
            SelectListItem friendType3Da = new SelectListItem();
            friendType3Da.Text = "Forældre";
            friendType3Da.Value = "2";
            SelectListItem friendType4Da = new SelectListItem();
            friendType4Da.Text = "Familie";
            friendType4Da.Value = "3";
            SelectListItem friendType5Da = new SelectListItem();
            friendType5Da.Text = "Omsorgsperson/Plejer/Pædagog";
            friendType5Da.Value = "4";
            FriendTypeListDa.Add(friendType1Da);
            FriendTypeListDa.Add(friendType2Da);
            FriendTypeListDa.Add(friendType3Da);
            FriendTypeListDa.Add(friendType4Da);
            FriendTypeListDa.Add(friendType5Da);

            FriendTypeListDe = new List<SelectListItem>();
            SelectListItem friendType1De = new SelectListItem();
            friendType1De.Text = "Persönliche Freunde";
            friendType1De.Value = "0";
            SelectListItem friendType2De = new SelectListItem();
            friendType2De.Text = "Spielzeuge/Tiere";
            friendType2De.Value = "1";
            SelectListItem friendType3De = new SelectListItem();
            friendType3De.Text = "Eltern";
            friendType3De.Value = "2";
            SelectListItem friendType4De = new SelectListItem();
            friendType4De.Text = "Familie";
            friendType4De.Value = "3";
            SelectListItem friendType5De = new SelectListItem();
            friendType5De.Text = "Betreuer";
            friendType5De.Value = "4";
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
