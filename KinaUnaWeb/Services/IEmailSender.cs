using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    public interface IEmailSender
    {
        /// <summary>
        /// Sends an email from the email address configured in Constants.SmtpFrom.
        /// </summary>
        /// <param name="email">string: The email address to send to.</param>
        /// <param name="subject">string: The Subject text.</param>
        /// <param name="message">string: The body of the email. HTML is enabled.</param>
        /// <returns></returns>
        Task SendEmailAsync(string email, string subject, string message);
    }
}
