using System.Threading.Tasks;

namespace KinaUna.IDP.Services
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
        /// <param name="authClient">The name of the website or service the account is for, for identifying multitenant clients.</param>
        /// <returns></returns>
        Task SendEmailAsync(string email, string subject, string message, string authClient);

        /// <summary>
        /// Sends a confirmation email to the specified email address.
        /// </summary>
        /// <param name="email">The email address to send the confirmation email to.</param>
        /// <param name="link">The confirmation link the user should click on to confirm that the email belongs to them.</param>
        /// <param name="client">The name of the website or service the account is for, for identifying multitenant clients.</param>
        /// <param name="languageId">The Id of the KinaUnaLanguage text should be shown in.</param>
        /// <returns></returns>
        Task SendEmailConfirmationAsync(string email, string link, string client = "KinaUna", int languageId = 1);

        /// <summary>
        /// Sends an email to the specified email address with a link to confirm a new email address.
        /// For updating the email address of a user.
        /// </summary>
        /// <param name="email">The new email address of the user.</param>
        /// <param name="link">The confirmation link.</param>
        /// <param name="client">The name of the website or service the account is for, for identifying multitenant clients.</param>
        /// <param name="languageId">The Id of the KinaUnaLanguage text should be shown in.</param>
        /// <returns></returns>
        Task SendEmailUpdateConfirmationAsync(string email, string link, string client = "KinaUna", int languageId = 1);

        /// <summary>
        /// Sends an email to the specified email address with a link to confirm deletion of a user's account.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="link">The link to confirm deletion of the account.</param>
        /// <param name="languageId">The Id of the KinaUnaLanguage text should be shown in.</param>
        /// <returns></returns>
        Task SendEmailDeleteAsync(string email, string link, int languageId = 1);
    }
}
