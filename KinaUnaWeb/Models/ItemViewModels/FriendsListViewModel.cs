using System.Collections.Generic;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class FriendsListViewModel: BaseItemsViewModel
    {
        public List<FriendViewModel> FriendViewModelsList { get; init; }
        public string TagFilter { get; set; }

        public FriendsListViewModel()
        {
            FriendViewModelsList = [];
        }

        public FriendsListViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            FriendViewModelsList = [];
            SetBaseProperties(baseItemsViewModel);
        }
    }
}
