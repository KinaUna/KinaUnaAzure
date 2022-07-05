using System.Text.Encodings.Web;
using System.Threading.Tasks;
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

        public static Task SendEmailDeleteAsync(this IEmailSender emailSender, string email, string link, int language = 1)
        {
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

            return emailSender.SendEmailAsync(email, mailTitle, mailText, "KinaUna");
        }

    }
}
