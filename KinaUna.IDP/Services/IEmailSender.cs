using System.Threading.Tasks;

namespace KinaUna.IDP.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
