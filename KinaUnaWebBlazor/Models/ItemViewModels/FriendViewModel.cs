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
        public Progeny Progeny { get; set; } = new();
        public List<SelectListItem> ProgenyList { get; set; } = [];
        public bool IsAdmin { get; set; } = false;
        public int AccessLevel { get; set; } = 5;
        public string Author { get; set; } = "";
        public int Type { get; set; } = 0;
        public Friend Friend { get; set; } = new();
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
            FriendTypeListEn = [];
            SelectListItem friendType1 = new()
            {
                Text = "Personal Friend",
                Value = "0"
            };
            SelectListItem friendType2 = new()
            {
                Text = "Toy/Animal Friend",
                Value = "1"
            };
            SelectListItem friendType3 = new()
            {
                Text = "Parent",
                Value = "2"
            };
            SelectListItem friendType4 = new()
            {
                Text = "Family",
                Value = "3"
            };
            SelectListItem friendType5 = new()
            {
                Text = "Caretaker",
                Value = "4"
            };
            FriendTypeListEn.Add(friendType1);
            FriendTypeListEn.Add(friendType2);
            FriendTypeListEn.Add(friendType3);
            FriendTypeListEn.Add(friendType4);
            FriendTypeListEn.Add(friendType5);

            FriendTypeListDa = [];
            SelectListItem friendType1Da = new()
            {
                Text = "Personlig ven",
                Value = "0"
            };
            SelectListItem friendType2Da = new()
            {
                Text = "Legetøj/Dyr",
                Value = "1"
            };
            SelectListItem friendType3Da = new()
            {
                Text = "Forældre",
                Value = "2"
            };
            SelectListItem friendType4Da = new()
            {
                Text = "Familie",
                Value = "3"
            };
            SelectListItem friendType5Da = new()
            {
                Text = "Omsorgsperson/Plejer/Pædagog",
                Value = "4"
            };
            FriendTypeListDa.Add(friendType1Da);
            FriendTypeListDa.Add(friendType2Da);
            FriendTypeListDa.Add(friendType3Da);
            FriendTypeListDa.Add(friendType4Da);
            FriendTypeListDa.Add(friendType5Da);

            FriendTypeListDe = [];
            SelectListItem friendType1De = new()
            {
                Text = "Persönliche Freunde",
                Value = "0"
            };
            SelectListItem friendType2De = new()
            {
                Text = "Spielzeuge/Tiere",
                Value = "1"
            };
            SelectListItem friendType3De = new()
            {
                Text = "Eltern",
                Value = "2"
            };
            SelectListItem friendType4De = new()
            {
                Text = "Familie",
                Value = "3"
            };
            SelectListItem friendType5De = new()
            {
                Text = "Betreuer",
                Value = "4"
            };
            FriendTypeListDe.Add(friendType1De);
            FriendTypeListDe.Add(friendType2De);
            FriendTypeListDe.Add(friendType3De);
            FriendTypeListDe.Add(friendType4De);
            FriendTypeListDe.Add(friendType5De);
        }
    }
}
