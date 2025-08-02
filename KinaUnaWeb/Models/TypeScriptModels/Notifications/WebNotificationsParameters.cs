namespace KinaUnaWeb.Models.TypeScriptModels.Notifications
{
    public class WebNotificationsParameters
    {
        public int Skip { get; set; } = 0;
        public int Count { get; set; } = 10;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "The specific case is used by TypeScript code.")]
        public bool unreadOnly { get; set; } = false;
    }
}
