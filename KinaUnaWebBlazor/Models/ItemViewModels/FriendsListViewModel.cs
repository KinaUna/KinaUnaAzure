﻿using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class FriendsListViewModel: BaseViewModel
    {
        public List<FriendViewModel> FriendViewModelsList { get; set; } = [];
        public Progeny Progeny { get; set; } = new();
        public bool IsAdmin { get; set; } = false;
        public string Tags { get; set; } = "";
        public string TagFilter { get; set; } = "";
    }
}
