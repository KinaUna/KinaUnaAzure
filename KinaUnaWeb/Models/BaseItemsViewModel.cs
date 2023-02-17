﻿using System.Collections.Generic;
using System.Linq;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models;

public class BaseItemsViewModel : BaseViewModel
{
    public int CurrentProgenyId { get; set; }
    public int CurrentAccessLevel { get; set; }
    public Progeny CurrentProgeny { get; set; }
    public List<UserAccess> CurrentProgenyAccessList { get; set; }
    public bool IsCurrentUserProgenyAdmin { get; set; }
    public string TagsList { get; set; }

    public void SetCurrentProgenyId(int progenyId)
    {
        CurrentProgenyId = progenyId;

        if (progenyId == 0 && CurrentUser.ViewChild > 0)
        {
            CurrentProgenyId = CurrentUser.ViewChild;
        }

        if (CurrentProgenyId == 0)
        {
            CurrentProgenyId = Constants.DefaultChildId;
        }
    }

    public void SetCurrentUsersAccessLevel()
    {
        CurrentAccessLevel = (int)AccessLevel.Public;

        if (CurrentProgenyAccessList.Count != 0)
        {
            UserAccess userAccess = CurrentProgenyAccessList.SingleOrDefault(u => u.UserId.ToUpper() == CurrentUser.UserEmail.ToUpper());
            if (userAccess != null)
            {
                CurrentAccessLevel = userAccess.AccessLevel;
            }
        }

        if (CurrentProgeny.IsInAdminList(CurrentUser.UserEmail))
        {
            IsCurrentUserProgenyAdmin = true;
            CurrentAccessLevel = (int)AccessLevel.Private;
        }
    }

    public void SetBaseProperties(BaseItemsViewModel baseItemsViewModel)
    {
        LanguageId = baseItemsViewModel.LanguageId;
        CurrentUser = baseItemsViewModel.CurrentUser;
        CurrentProgenyId = baseItemsViewModel.CurrentProgenyId;
        CurrentAccessLevel = baseItemsViewModel.CurrentAccessLevel;
        CurrentProgeny = baseItemsViewModel.CurrentProgeny;
        CurrentProgenyAccessList = baseItemsViewModel.CurrentProgenyAccessList;
        IsCurrentUserProgenyAdmin = baseItemsViewModel.IsCurrentUserProgenyAdmin;
    }

    public void SetTagList(List<string> tagsList)
    {
        string tagItems = "[";
        if (tagsList.Any())
        {
            foreach (string tagstring in tagsList)
            {
                tagItems = tagItems + "'" + tagstring + "',";
            }

            tagItems = tagItems.Remove(tagItems.Length - 1);
        }

        tagItems = tagItems + "]";

        TagsList = tagItems;
    }
}