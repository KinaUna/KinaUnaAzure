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

        /// <summary>
        /// The Danish Access Level List.
        /// </summary>
        public List<SelectListItem> AccessLevelListDa { get; set; }

        /// <summary>
        /// The German Access Level List.
        /// </summary>
        public List<SelectListItem> AccessLevelListDe { get; set; }

        public AccessLevelList()
        {
            SelectListItem selItem1 = new()
            {
                Text = "Hidden/Private",
                Value = "0"
            }; // 0 = Hidden/Parents only, 1=Family, 2=Caretakers/Special, 3= Friends, 4=DefaultUSers, 5= public.
            SelectListItem selItem2 = new()
            {
                Text = "Family",
                Value = "1"
            };
            SelectListItem selItem3 = new()
            {
                Text = "Caretakers/Special Access",
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
                Text = "Public/Anyone",
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

            SelectListItem selItemDa1 = new()
            {
                Text = "Administratorer",
                Value = "0"
            };
            SelectListItem selItemDa2 = new()
            {
                Text = "Familie",
                Value = "1"
            };
            SelectListItem selItemDa3 = new()
            {
                Text = "Omsorgspersoner/Speciel adgang",
                Value = "2"
            };
            SelectListItem selItemDa4 = new()
            {
                Text = "Venner",
                Value = "3"
            };
            SelectListItem selItemDa5 = new()
            {
                Text = "Registrerede brugere",
                Value = "4"
            };
            SelectListItem selItemDa6 = new()
            {
                Text = "Offentlig/alle",
                Value = "5"
            };
            AccessLevelListDa =
            [
                selItemDa1,
                selItemDa2,
                selItemDa3,
                selItemDa4,
                selItemDa5,
                selItemDa6
            ];

            SelectListItem selItemDe1 = new()
            {
                Text = "Administratoren",
                Value = "0"
            };
            SelectListItem selItemDe2 = new()
            {
                Text = "Familie",
                Value = "1"
            };
            SelectListItem selItemDe3 = new()
            {
                Text = "Betreuer/Spezial",
                Value = "2"
            };
            SelectListItem selItemDe4 = new()
            {
                Text = "Freunde",
                Value = "3"
            };
            SelectListItem selItemDe5 = new()
            {
                Text = "Registrierte Benutzer",
                Value = "4"
            };
            SelectListItem selItemDe6 = new()
            {
                Text = "Allen zugänglich",
                Value = "5"
            };
            AccessLevelListDe =
            [
                selItemDe1,
                selItemDe2,
                selItemDe3,
                selItemDe4,
                selItemDe5,
                selItemDe6
            ];
        }
    }
}
