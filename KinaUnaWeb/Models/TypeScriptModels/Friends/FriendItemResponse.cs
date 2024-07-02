using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.TypeScriptModels.Friends
{
    public class FriendItemResponse
    {
        public int FriendId { get; set; }
        public int LanguageId { get; init; }
        public bool IsCurrentUserProgenyAdmin { get; set; }
        public Friend Friend { get; set; }
    }
}
