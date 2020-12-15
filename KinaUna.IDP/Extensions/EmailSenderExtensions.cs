﻿using System.Text.Encodings.Web;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.IDP.Services;

namespace KinaUna.IDP.Extensions
{
    public static class EmailSenderExtensions
    {
        public static Task SendEmailConfirmationAsync(this IEmailSender emailSender, string email, string link, string client, string language = "en")
        {
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

            return emailSender.SendEmailAsync(email, mailTitle, mailText, client);
        }

        public static Task SendEmailUpdateConfirmationAsync(this IEmailSender emailSender, string email, string link, string client, string language = "en")
        {
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

            return emailSender.SendEmailAsync(email, mailTitle, mailText, client);
        }

    }
}
