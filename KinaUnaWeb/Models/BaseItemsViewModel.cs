using KinaUna.Data;
using KinaUna.Data.Models.Family;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace KinaUnaWeb.Models;

/// <summary>
/// Base ViewModel for pages that display items.
/// </summary>
public class BaseItemsViewModel : BaseViewModel
{
    public int CurrentProgenyId { get; set; }
    public int CurrentFamilyId { get; set; }
    public Progeny CurrentProgeny { get; set; }
    public Family CurrentFamily { get; set; }
    public List<SelectListItem> ProgenyList { get; set; }
    public List<SelectListItem> FamilyList { get; set; }
    public bool IsCurrentUserProgenyAdmin { get; set; }
    public string Tags { get; set; } = string.Empty;
    public string TagsList { get; set; } = "[]";
    public string ItemPermissionsListAsString { get; set; } = "";
    public bool InheritPermissions { get; set; }
    /// <summary>
    /// Sets the base properties for the ViewModel, used by classes inheriting from this class.
    /// </summary>
    /// <param name="baseItemsViewModel"></param>
    public void SetBaseProperties(BaseItemsViewModel baseItemsViewModel)
    {
        LanguageId = baseItemsViewModel.LanguageId;
        CurrentUser = baseItemsViewModel.CurrentUser;
        CurrentProgenyId = baseItemsViewModel.CurrentProgenyId;
        CurrentFamilyId = baseItemsViewModel.CurrentFamilyId;
        CurrentProgeny = baseItemsViewModel.CurrentProgeny;
        CurrentFamily = baseItemsViewModel.CurrentFamily;
        IsCurrentUserProgenyAdmin = baseItemsViewModel.IsCurrentUserProgenyAdmin;
    }

    public void UseCurrentViewChildOrDefault()
    {
        if (CurrentProgenyId == 0 && CurrentUser.ViewChild > 0)
        {
            CurrentProgenyId = CurrentUser.ViewChild;
        }

        if (CurrentProgenyId == 0)
        {
            CurrentProgenyId = Constants.DefaultChildId;
        }
    }

    /// <summary>
    /// Set the TagsList property for the ViewModel.
    /// </summary>
    /// <param name="tagsList">List of strings containing the tags.</param>
    public void SetTagList(List<string> tagsList)
    {
        string tagItems = "[";
        if (tagsList.Count != 0)
        {
            foreach (string tagstring in tagsList)
            {
                if (!string.IsNullOrEmpty(tagstring))
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }
            }

            tagItems = tagItems[..^1];
        }

        tagItems += "]";

        TagsList = tagItems;
    }
}