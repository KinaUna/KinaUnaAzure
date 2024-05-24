using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using KinaUna.Data;

namespace KinaUnaWeb.Services
{
    public class EmailSender(IConfiguration configuration) : IEmailSender
    {
        /// <summary>
        /// Sends an email from the email address configured in Constants.SmtpFrom.
        /// </summary>
        /// <param name="email">string: The email address to send to.</param>
        /// <param name="subject">string: The Subject text.</param>
        /// <param name="message">string: The body of the email. HTML is enabled.</param>
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
                From = new MailAddress(configuration.GetValue<string>("SmtpFrom"), "Support - " + Constants.AppName)
            };
            mailMessage.To.Add(email);
            mailMessage.Body = message;
            mailMessage.IsBodyHtml = true;
            mailMessage.Subject = subject;

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
