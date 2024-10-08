﻿using System;
using System.Collections.Generic;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class NoteViewModel: BaseItemsViewModel
    {
        public Note NoteItem { get; set; } = new();
        public string PathName { get; set; }
        public List<SelectListItem> ProgenyList { get; set; }
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }

        public NoteViewModel()
        {
            ProgenyList = [];
        }

        public NoteViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            SetAccessLevelList();
            ProgenyList = [];
        }

        public void SetAccessLevelList()
        {
            AccessLevelList accessLevelList = new();
            AccessLevelListEn = accessLevelList.AccessLevelListEn;
            AccessLevelListDa = accessLevelList.AccessLevelListDa;
            AccessLevelListDe = accessLevelList.AccessLevelListDe;

            AccessLevelListEn[NoteItem.AccessLevel].Selected = true;
            AccessLevelListDa[NoteItem.AccessLevel].Selected = true;
            AccessLevelListDe[NoteItem.AccessLevel].Selected = true;

            if (LanguageId == 2)
            {
                AccessLevelListEn = AccessLevelListDe;
            }

            if (LanguageId == 3)
            {
                AccessLevelListEn = AccessLevelListDa;
            }
        }

        public void SetProgenyList()
        {
            NoteItem.ProgenyId = CurrentProgenyId;
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

        public void SetPropertiesFromNote(Note note)
        {
            NoteItem.NoteId = note.NoteId;
            NoteItem.ProgenyId = note.ProgenyId;
            NoteItem.Content = note.Content;
            NoteItem.AccessLevel = note.AccessLevel;
            NoteItem.Category = note.Category;
            NoteItem.Title = note.Title;
            NoteItem.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(note.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone));
            NoteItem.Owner = note.Owner?? CurrentUser.UserId;
            if (NoteItem.Owner.Contains('@'))
            {
                NoteItem.Owner = CurrentUser.UserId;
            }
        }

        public Note CreateNote()
        {
            Note note = new()
            {
                NoteId = NoteItem.NoteId,
                Title = NoteItem.Title,
                ProgenyId = NoteItem.ProgenyId,
                CreatedDate = TimeZoneInfo.ConvertTimeToUtc(NoteItem.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(CurrentUser.Timezone)),
                Content = NoteItem.Content,
                Category = NoteItem.Category,
                AccessLevel = NoteItem.AccessLevel,
                Owner = NoteItem.Owner
            };

            return note;
        }
    }
}
