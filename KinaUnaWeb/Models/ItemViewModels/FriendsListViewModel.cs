using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class FriendsListViewModel: BaseViewModel
    {
        public List<FriendViewModel> FriendViewModelsList { get; set; }
        public Progeny Progeny { get; set; }
        public bool IsAdmin { get; set; }
        public string Tags { get; set; }
        public string TagFilter { get; set; }

        public FriendsListViewModel()
        {
            FriendViewModelsList = new List<FriendViewModel>();
        }
    }
}
