using System.Collections.Generic;
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
}