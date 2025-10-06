using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class NoteViewModel: BaseItemsViewModel
    {
        public Note NoteItem { get; set; } = new();
        public string PathName { get; set; }
        
        public NoteViewModel()
        {
            ProgenyList = [];
        }

        public NoteViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
            ProgenyList = [];
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
            NoteItem.ItemPerMission = note.ItemPerMission;
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
                Owner = NoteItem.Owner,
                ItemPermissionsDtoList = JsonSerializer.Deserialize<List<ItemPermissionDto>>(ItemPermissionsListAsString)
            };

            return note;
        }
    }
}
