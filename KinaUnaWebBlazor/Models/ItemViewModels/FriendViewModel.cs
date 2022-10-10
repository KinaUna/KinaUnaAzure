using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class FriendViewModel: BaseViewModel
    {
        public int FriendId { get; set; } = 0;

        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime? FriendSince { get; set; }
        public DateTime FriendAddedDate { get; set; } = DateTime.UtcNow;
        public string PictureLink { get; set; } = "";
        public int ProgenyId { get; set; } = 0;
        public Progeny Progeny { get; set; } = new Progeny();
        public List<SelectListItem> ProgenyList { get; set; } = new List<SelectListItem>();
        public bool IsAdmin { get; set; } = false;
        public int AccessLevel { get; set; } = 5;
        public string Author { get; set; } = "";
        public int Type { get; set; } = 0;
        public Friend Friend { get; set; } = new Friend();
        public List<SelectListItem> FriendTypeListEn { get; set; }
        public List<SelectListItem> FriendTypeListDa { get; set; }
        public List<SelectListItem> FriendTypeListDe { get; set; }
        public string Context { get; set; } = "";
        public string Notes { get; set; } = "";
        public string FileName { get; set; } = "";
        public IFormFile? File { get; set; }
        public string Tags { get; set; } = "";

        public FriendViewModel()
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
        }
    }
}
