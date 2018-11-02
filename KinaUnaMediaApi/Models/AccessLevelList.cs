using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaMediaApi.Models
{
    public class AccessLevelList
    {
        public List<SelectListItem> AccessLevelListEn { get; set; }
        public List<SelectListItem> AccessLevelListDa { get; set; }
        public List<SelectListItem> AccessLevelListDe { get; set; }

        public AccessLevelList()
        {
            SelectListItem selItem1 = new SelectListItem(); // 0 = Hidden/Parents only, 1=Family, 2=Caretakers/Special, 3= Friends, 4=DefaultUSers, 5= public.
            selItem1.Text = "Hidden/Private";
            selItem1.Value = "0";
            SelectListItem selItem2 = new SelectListItem();
            selItem2.Text = "Family";
            selItem2.Value = "1";
            SelectListItem selItem3 = new SelectListItem();
            selItem3.Text = "Caretakers/Special Access";
            selItem3.Value = "2";
            SelectListItem selItem4 = new SelectListItem();
            selItem4.Text = "Friends";
            selItem4.Value = "3";
            SelectListItem selItem5 = new SelectListItem();
            selItem5.Text = "Registrered Users";
            selItem5.Value = "4";
            SelectListItem selItem6 = new SelectListItem();
            selItem6.Text = "Public/Anyone";
            selItem6.Value = "5";
            AccessLevelListEn = new List<SelectListItem>();
            AccessLevelListEn.Add(selItem1);
            AccessLevelListEn.Add(selItem2);
            AccessLevelListEn.Add(selItem3);
            AccessLevelListEn.Add(selItem4);
            AccessLevelListEn.Add(selItem5);
            AccessLevelListEn.Add(selItem6);

            SelectListItem selItemDa1 = new SelectListItem();
            selItemDa1.Text = "Administratorer";
            selItemDa1.Value = "0";
            SelectListItem selItemDa2 = new SelectListItem();
            selItemDa2.Text = "Familie";
            selItemDa2.Value = "1";
            SelectListItem selItemDa3 = new SelectListItem();
            selItemDa3.Text = "Omsorgspersoner/Speciel adgang";
            selItemDa3.Value = "2";
            SelectListItem selItemDa4 = new SelectListItem();
            selItemDa4.Text = "Venner";
            selItemDa4.Value = "3";
            SelectListItem selItemDa5 = new SelectListItem();
            selItemDa5.Text = "Registrerede brugere";
            selItemDa5.Value = "4";
            SelectListItem selItemDa6 = new SelectListItem();
            selItemDa6.Text = "Offentlig/alle";
            selItemDa6.Value = "5";
            AccessLevelListDa = new List<SelectListItem>();
            AccessLevelListDa.Add(selItemDa1);
            AccessLevelListDa.Add(selItemDa2);
            AccessLevelListDa.Add(selItemDa3);
            AccessLevelListDa.Add(selItemDa4);
            AccessLevelListDa.Add(selItemDa5);
            AccessLevelListDa.Add(selItemDa6);

            SelectListItem selItemDe1 = new SelectListItem();
            selItemDe1.Text = "Administratoren";
            selItemDe1.Value = "0";
            SelectListItem selItemDe2 = new SelectListItem();
            selItemDe2.Text = "Familie";
            selItemDe2.Value = "1";
            SelectListItem selItemDe3 = new SelectListItem();
            selItemDe3.Text = "Betreuer/Spezial";
            selItemDe3.Value = "2";
            SelectListItem selItemDe4 = new SelectListItem();
            selItemDe4.Text = "Freunde";
            selItemDe4.Value = "3";
            SelectListItem selItemDe5 = new SelectListItem();
            selItemDe5.Text = "Registrierte Benutzer";
            selItemDe5.Value = "4";
            SelectListItem selItemDe6 = new SelectListItem();
            selItemDe6.Text = "Allen zugänglich";
            selItemDe6.Value = "5";
            AccessLevelListDe = new List<SelectListItem>();
            AccessLevelListDe.Add(selItemDe1);
            AccessLevelListDe.Add(selItemDe2);
            AccessLevelListDe.Add(selItemDe3);
            AccessLevelListDe.Add(selItemDe4);
            AccessLevelListDe.Add(selItemDe5);
            AccessLevelListDe.Add(selItemDe6);
        }
    }
}
