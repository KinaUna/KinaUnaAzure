using System.Collections.Generic;
using KinaUnaWeb.Models.TypeScriptModels.Friends;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class FriendsListViewModel: BaseItemsViewModel
    {
        public List<FriendViewModel> FriendViewModelsList { get; init; }
        public string TagFilter { get; set; }
        public FriendsPageParameters FriendsPageParameters { get; set; }
        public int FriendId { get; set; }

        /// <summary>
        /// Parameterless constructor. Needed for initialization of the view model when objects are created in Razor views/passed as parameters in POST methods.
        /// </summary>
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
