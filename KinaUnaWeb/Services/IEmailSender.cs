using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
