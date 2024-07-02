using System.Collections.Generic;

namespace KinaUnaWeb.Models.TypeScriptModels.Friends
{
    public class FriendsPageResponse
    {
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public List<int> FriendsList { get; set; } = [];
    }
}
