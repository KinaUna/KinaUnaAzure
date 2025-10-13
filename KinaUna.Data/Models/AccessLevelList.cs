using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Select Lists for setting the access level of data.
    /// </summary>
    public class AccessLevelList
    {
        /// <summary>
        /// The English Access Level List.
        /// </summary>
        public List<SelectListItem> AccessLevelListEn { get; set; }
        
        public AccessLevelList()
        {
            SelectListItem selItem1 = new()
            {
                Text = "Administrators",
                Value = "0"
            }; // 0 = Hidden/Parents only, 1=Family, 2=Caretakers/Special, 3= Friends, 4=DefaultUSers, 5= public.
            SelectListItem selItem2 = new()
            {
                Text = "Family",
                Value = "1"
            };
            SelectListItem selItem3 = new()
            {
                Text = "Caretakers",
                Value = "2"
            };
            SelectListItem selItem4 = new()
            {
                Text = "Friends",
                Value = "3"
            };
            SelectListItem selItem5 = new()
            {
                Text = "Registered Users",
                Value = "4"
            };
            SelectListItem selItem6 = new()
            {
                Text = "Public",
                Value = "5"
            };
            AccessLevelListEn =
            [
                selItem1,
                selItem2,
                selItem3,
                selItem4,
                selItem5,
                selItem6
            ];
        }
    }
}
