using System.Collections.Generic;
using System.Linq;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models;

/// <summary>
/// Base ViewModel for pages that display items.
/// </summary>
public class BaseItemsViewModel : BaseViewModel
{
    public int CurrentProgenyId { get; set; }
    public int CurrentAccessLevel { get; private set; }
    public Progeny CurrentProgeny { get; set; }
    public List<UserAccess> CurrentProgenyAccessList { get; set; }
    public bool IsCurrentUserProgenyAdmin { get; set; }
    public string Tags { get; set; }
    public string TagsList { get; set; }

    /// <summary>
    /// Set the current Progeny Id.
    /// If the Progeny Id is 0 and the CurrentUser has a ViewChild set, the ViewChild Id is used.
    /// Else the DefaultChildId is used.
    /// </summary>
    /// <param name="progenyId">The Id of the Progeny.</param>
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

    /// <summary>
    /// Set the current users access level for the current Progeny and determines if the current user is an admin for the progeny.
    /// </summary>
    public void SetCurrentUsersAccessLevel()
    {
        CurrentAccessLevel = (int)AccessLevel.NoAccess;

        if (CurrentProgenyAccessList.Count != 0)
        {
            UserAccess userAccess = CurrentProgenyAccessList.SingleOrDefault(u => u.UserId.Equals(CurrentUser.UserEmail, System.StringComparison.CurrentCultureIgnoreCase));
            if (userAccess != null)
            {
                CurrentAccessLevel = userAccess.AccessLevel;
            }
        }

        if (!CurrentProgeny.IsInAdminList(CurrentUser.UserEmail)) return;

        IsCurrentUserProgenyAdmin = true;
        CurrentAccessLevel = (int)AccessLevel.Private;
    }

    /// <summary>
    /// Sets the base properties for the ViewModel, used by classes inheriting from this class.
    /// </summary>
    /// <param name="baseItemsViewModel"></param>
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

            tagItems = tagItems.Remove(tagItems.Length - 1);
        }

        tagItems += "]";

        TagsList = tagItems;
    }
}