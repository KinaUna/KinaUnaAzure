namespace KinaUnaWebBlazor.Services
{
    public interface IPushMessageSender
    {
        Task SendMessage(string user, string title, string message, string link, string tag);
    }
}
