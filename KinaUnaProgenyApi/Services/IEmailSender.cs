using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services
{
    /// <summary>
    /// Dependency injection interface for sending emails.
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>
        /// Sends an email to the specified email address.
        /// The email server settings are defined in the configuration.
        /// </summary>
        /// <param name="email">The email address to send to.</param>
        /// <param name="subject">The subject line of the email.</param>
        /// <param name="message">The body of the email.</param>
        /// <returns></returns>
        Task SendEmailAsync(string email, string subject, string message);
    }
}
