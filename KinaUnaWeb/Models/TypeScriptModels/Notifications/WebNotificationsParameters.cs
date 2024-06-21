namespace KinaUnaWeb.Models.TypeScriptModels.Notifications
{
    public class WebNotificationsParameters
    {
        public int Skip { get; set; } = 0;
        public int Count { get; set; } = 10;
        public bool unreadOnly { get; set; } = false;
    }
}
