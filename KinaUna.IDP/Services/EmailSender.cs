﻿using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using KinaUna.Data;

namespace KinaUna.IDP.Services
{
    /// <summary>
    /// Service for sending emails.
    /// </summary>
    /// <param name="configuration"></param>
    public class EmailSender(IConfiguration configuration, ILocaleManager localeManager) : IEmailSender
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

        /// <summary>
        /// Sends a confirmation email to the specified email address.
        /// </summary>
        /// <param name="email">The email address to send the confirmation email to.</param>
        /// <param name="link">The confirmation link the user should click on to confirm that the email belongs to them.</param>
        /// <param name="client">The name of the website or service the account is for, for identifying multitenant clients.</param>
        /// <param name="languageId">The Id of the KinaUnaLanguage text should be shown in.</param>
        /// <returns></returns>
        public async Task SendEmailConfirmationAsync(string email, string link, string client = "KinaUna", int languageId = 1)
        {
            string mailTitle = await localeManager.GetTranslation("Confirm your email", PageNames.Account, languageId);
            string mailText = await localeManager.GetTranslation("Please confirm your [[client]] account's email address by clicking this link", PageNames.Account, languageId);
            mailText = mailText.Replace("[[client]]", client);
            mailText += $": <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            
            await SendEmailAsync(email, mailTitle, mailText, client);
        }

        /// <summary>
        /// Sends an email to the specified email address with a link to confirm a new email address.
        /// For updating the email address of a user.
        /// </summary>
        /// <param name="email">The new email address of the user.</param>
        /// <param name="link">The confirmation link.</param>
        /// <param name="client">The name of the website or service the account is for, for identifying multitenant clients.</param>
        /// <param name="languageId">The Id of the KinaUnaLanguage text should be shown in.</param>
        /// <returns></returns>
        public async Task SendEmailUpdateConfirmationAsync(string email, string link, string client, int languageId = 1)
        {
            string mailTitle = await localeManager.GetTranslation("Confirm email change", PageNames.Account, languageId);
            string mailText = await localeManager.GetTranslation("Please confirm your [[client]] account's new email address by clicking this link", PageNames.Account, languageId);
            mailText = mailText.Replace("[[client]]", client);
            mailText += $": <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";

            await SendEmailAsync(email, mailTitle, mailText, client);
        }

        /// <summary>
        /// Sends an email to the specified email address with a link to confirm deletion of a user's account.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="link">The link to confirm deletion of the account.</param>
        /// <param name="languageId">The Id of the KinaUnaLanguage text should be shown in.</param>
        /// <returns></returns>
        public async Task SendEmailDeleteAsync(string email, string link, int languageId = 1)
        {
            string mailTitle = await localeManager.GetTranslation("Confirm delete account", PageNames.Account, languageId);
            string mailText = await localeManager.GetTranslation("Please confirm that you want to delete your KinaUna account by clicking this link", PageNames.Account, languageId);
            mailText += $": <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";

            await SendEmailAsync(email, mailTitle, mailText, "KinaUna");
        }
    }
}
