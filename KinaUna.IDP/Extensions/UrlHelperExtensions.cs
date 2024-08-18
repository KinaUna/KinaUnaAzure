using KinaUna.IDP.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace KinaUna.IDP.Extensions
{
    public static class UrlHelperExtensions
    {
        public static string EmailConfirmationLink(this IUrlHelper urlHelper, string userId, string code, string scheme, string client, int languageId)
        {
            return urlHelper.Action(
                action: nameof(AccountController.ConfirmEmail),
                controller: "Account",
                values: new { client, languageId, userId, code },
                protocol: scheme);
        }

        public static string ResetPasswordCallbackLink(this IUrlHelper urlHelper, string userId, string code, string scheme)
        {
            return urlHelper.Action(
                action: nameof(AccountController.ResetPassword),
                controller: "Account",
                values: new { userId, code },
                protocol: scheme);
        }

        public static string EmailDeleteAccountLink(this IUrlHelper urlHelper, string userId, string code, string scheme)
        {
            return urlHelper.Action(
                action: nameof(AccountController.ConfirmDeleteAccount),
                controller: "Account",
                values: new { userId, code },
                protocol: scheme);
        }
    }
}
