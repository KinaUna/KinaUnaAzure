using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class FriendsListViewModel: BaseItemsViewModel
    {
        public List<FriendViewModel> FriendViewModelsList { get; set; }
        
        public string Tags { get; set; }
        public string TagFilter { get; set; }

        public FriendsListViewModel()
        {
            FriendViewModelsList = new List<FriendViewModel>();
        }

        public FriendsListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            FriendViewModelsList = new List<FriendViewModel>();
            SetBaseProperties(baseItemsViewModel);
        }
    }
}
