using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace KinaUna.IDP.Services
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
        /// <param name="authClient">The auth provider name (default: KinaUna), when using the Identity Server for multiple tenants.</param>
        /// <returns></returns>
        public Task SendEmailAsync(string email, string subject, string message, string authClient = "KinaUna")
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
                From = new MailAddress(configuration.GetValue<string>("SmtpFrom"), "Support - " + authClient),
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

        public Task SendEmailConfirmationAsync(string email, string link, string client, string language = "en")
        {
            // Todo: Use translations API instead.

            string mailTitle = "Confirm your email";
            string mailText = $"Please confirm your {client} account's email address by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";

            if (language == "da")
            {
                mailTitle = "Bekræft email";
                mailText = $"Bekræft venligst din email for din {client} konto ved at klikke på dette link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            }

            if (language == "de")
            {
                mailTitle = "Bestätigen Sie Ihre E-Mail-Adresse";
                mailText = $"Bestätigen Sie Ihre E-Mail-Adresse für {client} durch Klicken auf den Link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            }

            return SendEmailAsync(email, mailTitle, mailText, client);
        }

        public Task SendEmailUpdateConfirmationAsync(string email, string link, string client, string language = "en")
        {
            // Todo: Use translations API instead.
            string mailTitle = "Confirm your email";
            string mailText = $"Please confirm your {client} account's email address by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";

            if (language == "da")
            {
                mailTitle = "Bekræft email";
                mailText = $"Bekræft venligst din email for din {client} konto ved at klikke på dette link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            }

            if (language == "de")
            {
                mailTitle = "Bestätigen Sie Ihre E-Mail-Adresse";
                mailText = $"Bestätigen Sie Ihre E-Mail-Adresse für {client} durch Klicken auf den Link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            }

            return SendEmailAsync(email, mailTitle, mailText, client);
        }

        public Task SendEmailDeleteAsync(string email, string link, int language = 1)
        {
            // Todo: Use translations API instead.
            string mailTitle = "Confirm delete account";
            string mailText = $"Please confirm that you want to delete your KinaUna account by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";

            if (language == 3)
            {
                mailTitle = "Bekræft slet konto";
                mailText = $"Bekræft venligst at du vil slette din KinaUna konto ved at klikke på dette link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            }

            if (language == 2)
            {
                mailTitle = "Bestätigen löschen Konto";
                mailText = $"Bitte bestätigen Sie, dass Sie Ihr KinaUna-Konto löschen möchten durch Klicken auf den Link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            }

            return SendEmailAsync(email, mailTitle, mailText, "KinaUna");
        }
    }
}
