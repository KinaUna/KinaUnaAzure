using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace KinaUnaProgenyApi.Services
{
    /// <summary>
    /// Service for sending emails.
    /// </summary>
    /// <param name="configuration"></param>
    public class EmailSender(IConfiguration configuration) : IEmailSender
    {
        /// <summary>
        /// Sends an email to the specified email address.
        /// The email server settings are defined in the configuration.
        /// </summary>
        /// <param name="email">The email address to send to.</param>
        /// <param name="subject">The subject line of the email.</param>
        /// <param name="message">The body of the email.</param>
        /// <returns></returns>
        /// <returns></returns>
        public Task SendEmailAsync(string email, string subject, string message)
        {
            SmtpClient client = new(configuration.GetValue<string>("SmtpServer"))
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(configuration.GetValue<string>("SmtpUserName"), configuration.GetValue<string>("SupportMailPassword")),
                EnableSsl = true,
                Port = 587
            };

            MailMessage mailMessage = new()
            {
                From = new MailAddress(configuration.GetValue<string>("SmtpFrom"), "Support - KinaUna "),
                Body = message,
                IsBodyHtml = true,
                Subject = subject
            };
            mailMessage.To.Add(email);

            try
            {
                client.SendMailAsync(mailMessage);
                return Task.CompletedTask;
            }
            catch
            {
                return Task.FromResult(0);
            }
        }
    }
}
