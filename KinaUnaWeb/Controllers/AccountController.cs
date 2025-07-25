using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OpenIddict.Client.AspNetCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    /// <summary>
    /// Provides account management functionalities, including user authentication, profile management, and account
    /// settings.
    /// </summary>
    /// <remarks>The <c>AccountController</c> class handles various account-related actions such as logging
    /// in, logging out, viewing and updating account information, and managing user authentication states.  It
    /// integrates with external services for authentication and user information retrieval, and supports operations
    /// like enabling/disabling push notifications and managing profile pictures.</remarks>
    /// <param name="imageStore"></param>
    /// <param name="configuration"></param>
    /// <param name="authHttpClient"></param>
    /// <param name="userInfosHttpClient"></param>
    /// <param name="tokenService"></param>
    public class AccountController(ImageStore imageStore, IConfiguration configuration,
        IAuthHttpClient authHttpClient, IUserInfosHttpClient userInfosHttpClient, ITokenService tokenService)
        : Controller
    {
        /// <summary>
        /// Index page for the AccountController.
        /// </summary>
        /// <returns>View for the Index page.</returns>
        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Logs out the current user from the application and the identity provider.
        /// </summary>
        /// <remarks>This method removes the user's cached tokens and signs the user out from both the 
        /// application and the identity provider. It ensures that the return URL is local to  prevent open redirect
        /// vulnerabilities.</remarks>
        /// <param name="returnUrl">The URL to redirect to after logout. Must be a local URL to prevent open redirect attacks.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an  IActionResult that redirects
        /// the user to the specified return URL or the  home page if the return URL is not local.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> LogOut(string returnUrl = "")
        {
            if (!string.IsNullOrWhiteSpace(User.GetUserId()))
            {
                // Remove the user's cached access token and refresh token.
                await tokenService.RemoveTokenForUser(User.GetUserId());
            }
        
            //// Sign out the user from the application and the IDP server.
            //_ = HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            //_ = HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

            // Retrieve the identity stored in the local authentication cookie. If it's not available,
            // this indicates that the user is already logged out locally (or has not logged in yet).
            //
            // For scenarios where the default authentication handler configured in the ASP.NET Core
            // authentication options shouldn't be used, a specific scheme can be specified here.
            AuthenticateResult result = await HttpContext.AuthenticateAsync();
            if (result is not { Succeeded: true })
            {
                // Only allow local return URLs to prevent open redirect attacks.
                return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
            }

            // Remove the local authentication cookie before triggering a redirection to the remote server.
            //
            // For scenarios where the default sign-out handler configured in the ASP.NET Core
            // authentication options shouldn't be used, a specific scheme can be specified here.
            await HttpContext.SignOutAsync();

            // If no properties were stored, redirect the user agent to the return URL.
            if (result.Properties == null) return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");

            AuthenticationProperties properties = new(new Dictionary<string, string>
            {
                // While not required, the specification encourages sending an id_token_hint
                // parameter containing an identity token returned by the server for this user.
                [OpenIddictClientAspNetCoreConstants.Properties.IdentityTokenHint] =
                    result.Properties.GetTokenValue(OpenIddictClientAspNetCoreConstants.Tokens.BackchannelIdentityToken)
            })
            {
                // Only allow local return URLs to prevent open redirect attacks.
                RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/"
            };

            // Ask the OpenIddict client middleware to redirect the user agent to the identity provider.
            return SignOut(properties, OpenIdConnectDefaults.AuthenticationScheme);

            // Redirect to the home page.
            //return RedirectToAction("Index", "Home");
        }
        /// <summary>
        /// Access denied page. Shows a message that the user does not have access to the requested page.
        /// </summary>
        /// <returns>View.</returns>
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// MyAccount page. Shows the current user's account information.
        /// Allows the user to update their account information.
        /// </summary>
        /// <returns>View with UserInfoViewModel.</returns>
        /// <exception cref="ApplicationException"></exception>
        [Authorize]
        public async Task<IActionResult> MyAccount()
        {
            string userEmail = User.GetEmail();
            _ = bool.TryParse(User.FindFirst("email_verified")?.Value, out bool mailConfirmed);
            _ = DateTime.TryParse(User.FindFirst("joindate")?.Value, out DateTime joinDate);

            UserInfo userInfo = await userInfosHttpClient.GetUserInfo(userEmail) ?? throw new ApplicationException($"Unable to load user with email '{userEmail}'.");
            if (string.IsNullOrEmpty(userInfo.ProfilePicture))
            {
                userInfo.ProfilePicture = Constants.ProfilePictureUrl;
            }

            userInfo.ProfilePicture = userInfo.GetProfilePictureUrl();

            UserInfoViewModel model = new()
            {
                Id = userInfo.Id,
                UserId = userInfo.UserId,
                UserName = userInfo.UserName,
                FirstName = userInfo.FirstName,
                MiddleName = userInfo.MiddleName,
                LastName = userInfo.LastName,
                UserEmail = userInfo.UserEmail,
                Timezone = userInfo.Timezone,
                JoinDate = joinDate.ToString(CultureInfo.InvariantCulture),
                IsEmailConfirmed = mailConfirmed,
                PhoneNumber = User.FindFirst("phone_number")?.Value ?? "",
                ProfilePicture = userInfo.ProfilePicture,
                LanguageId = Request.GetLanguageIdFromCookie()
            };

            if (string.IsNullOrEmpty(model.UserName))
            {
                model.UserName = model.UserEmail;
            }

            model.ChangeLink = configuration["AuthenticationServer"] + "/Account/ChangePassword";
            
            return View(model);
        }

        /// <summary>
        /// Post action for updating the user's account information.
        /// </summary>
        /// <param name="model">UserInfoViewModel with the updated properties.</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> MyAccount(UserInfoViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();

            string userEmail = User.GetEmail();
            _ = bool.TryParse(User.FindFirst("email_verified")?.Value, out bool mailConfirmed);

            UserInfo userInfo = await userInfosHttpClient.GetUserInfo(userEmail);
            userInfo.CopyPropertiesFromUserInfoViewModel(model);

            // Check if email has changed.
            bool emailChanged = false;
            if (!userInfo.UserEmail.Equals(model.UserEmail, StringComparison.CurrentCultureIgnoreCase))
            {
                model.IsEmailConfirmed = false;
                emailChanged = true;
            }
            else
            {
                model.IsEmailConfirmed = mailConfirmed;
            }

            // Check if profile picture has changed.
            if (model.File != null && model.File.Name != string.Empty)
            {
                await using Stream stream = model.File.OpenReadStream();
                string fileFormat = Path.GetExtension(model.File.FileName);
                userInfo.ProfilePicture = await imageStore.SaveImage(stream, BlobContainers.Profiles, fileFormat);
            }

            await userInfosHttpClient.UpdateUserInfo(userInfo);
            
            model.ProfilePicture = userInfo.ProfilePicture;
            model.ProfilePicture = userInfo.GetProfilePictureUrl();

            if (emailChanged) // If the email changed show a page with further information about the email confirmation process.
            {
                return RedirectToAction("ChangeEmail", new {oldEmail = userEmail, newEmail = model.UserEmail});
            }
            return View(model);
        }

        /// <summary>
        /// Page for initiating a password change.
        /// A confirmation email needs to be sent to the user to verify the email address belongs to them and the IDP server needs to update the database once confirmed.
        /// </summary>
        /// <param name="oldEmail">The email address before the change.</param>
        /// <param name="newEmail">The new email address.</param>
        /// <returns>View with UserInfoViewModel.</returns>
        [Authorize]
        public async Task<IActionResult> ChangeEmail(string oldEmail, string newEmail = "")
        {
            string userEmail = User.GetEmail();
            if (string.IsNullOrEmpty(newEmail))
            {
                newEmail = userEmail;
            }

            _ = bool.TryParse(User.FindFirst("email_verified")?.Value, out bool mailConfirmed);
            UserInfo userinfo = await userInfosHttpClient.GetUserInfo(userEmail);
            _ = DateTime.TryParse(User.FindFirst("joindate")?.Value, out DateTime joinDate);
            
            UserInfoViewModel model = new()
            {
                Id = userinfo.Id,
                UserId = userinfo.UserId,
                UserName = userinfo.UserName,
                FirstName = userinfo.FirstName,
                MiddleName = userinfo.MiddleName,
                LastName = userinfo.LastName,
                UserEmail = userinfo.UserEmail,
                Timezone = userinfo.Timezone,
                JoinDate = joinDate.ToString(CultureInfo.InvariantCulture),
                IsEmailConfirmed = mailConfirmed,
                PhoneNumber = User.FindFirst("phone_number")?.Value ?? "",
                ProfilePicture = userinfo.ProfilePicture,
                LanguageId = Request.GetLanguageIdFromCookie(),
                ChangeLink = configuration["AuthenticationServer"] + "/Account/ChangeEmail?NewEmail=" + newEmail + "&OldEmail=" + oldEmail
            };

            return View(model);
        }

        /// <summary>
        /// Page for enabling push notifications.
        /// </summary>
        /// <returns>View.</returns>
        [Authorize]
        public IActionResult EnablePush()
        {
            // Todo: Use view model.
            ViewBag.UserId = User.GetUserId();
            ViewBag.PublicKey = configuration["VapidPublicKey"];

            return View();
        }

        /// <summary>
        /// Page for disabling push notifications.
        /// </summary>
        /// <returns>View.</returns>
        [Authorize]
        public IActionResult DisablePush()
        {
            // Todo: Use view model.
            ViewBag.UserId = User.GetUserId();
            ViewBag.PublicKey = configuration["VapidPublicKey"];
            return View();
        }
        
        /// <summary>
        /// Page for deleting your account.
        /// </summary>
        /// <returns>View with UserInfoViewModel.</returns>
        /// <exception cref="ApplicationException"></exception>
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            string userId = User.GetUserId();
            _ = DateTime.TryParse(User.FindFirst("joindate")?.Value, out DateTime joinDate);
            
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(userId) ?? throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            if (string.IsNullOrEmpty(userInfo.ProfilePicture))
            {
                userInfo.ProfilePicture = Constants.ProfilePictureUrl;
            }

            userInfo.ProfilePicture = userInfo.GetProfilePictureUrl();

            UserInfoViewModel model = new()
            {
                Id = userInfo.Id,
                UserId = userInfo.UserId,
                UserName = userInfo.UserName,
                FirstName = userInfo.FirstName,
                MiddleName = userInfo.MiddleName,
                LastName = userInfo.LastName,
                UserEmail = userInfo.UserEmail,
                Timezone = userInfo.Timezone,
                JoinDate = joinDate.ToString(CultureInfo.InvariantCulture),
                PhoneNumber = User.FindFirst("phone_number")?.Value ?? "",
                ProfilePicture = userInfo.ProfilePicture,
                LanguageId = Request.GetLanguageIdFromCookie()
            };

            if (string.IsNullOrEmpty(model.UserName))
            {
                model.UserName = model.UserEmail;
            }
            
            return View(model);
        }

        /// <summary>
        /// Post action for deleting your user account.
        /// </summary>
        /// <param name="model">UserInfoViewModel.</param>
        /// <returns>Redirect to the IDP server's delete account page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteAccount(UserInfoViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();

            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.UserEmail.Equals(User.GetEmail(), StringComparison.CurrentCultureIgnoreCase)) return Redirect(model.ChangeLink);

            model.UserId = userInfo.UserId;
            model.UserEmail = userInfo.UserEmail;
            model.FirstName = userInfo.FirstName;
            model.MiddleName = userInfo.MiddleName;
            model.LastName = userInfo.LastName;
            model.UserName = userInfo.UserName;

            _ = await userInfosHttpClient.DeleteUserInfo(userInfo);

            model.ChangeLink = configuration["AuthenticationServer"] + "/Account/DeleteAccount";

            return Redirect(model.ChangeLink);
        }

        /// <summary>
        /// Action for restoring a deleted account.
        /// </summary>
        /// <returns>Redirects to Home/Index page.</returns>
        /// <exception cref="ApplicationException"></exception>
        [Authorize]
        public async Task<IActionResult> UnDeleteAccount()
        {
            string userId = User.GetUserId();
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(userId) ?? throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            if (userInfo.UserId != userId) return RedirectToAction("Index", "Home");

            userInfo.Deleted = false;
            _ = await userInfosHttpClient.UpdateUserInfo(userInfo);
            _ = await authHttpClient.RemoveDeleteUser(userInfo);

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Url for retrieving a user's profile picture.
        /// </summary>
        /// <param name="id">The user's UserId.</param>
        /// <returns>FileContentResult for the image file.</returns>
        [AllowAnonymous]
        public async Task<FileContentResult> ProfilePicture(string id)
        {
            // Todo: Access control.

            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(id);
            if (userInfo == null || string.IsNullOrEmpty(userInfo.ProfilePicture))
            {
                MemoryStream fileContentNoAccess = await imageStore.GetStream("868b62e2-6978-41a1-97dc-1cc1116f65a6.jpg");
                byte[] fileContentBytesNoAccess = fileContentNoAccess.ToArray();
                return new FileContentResult(fileContentBytesNoAccess, "image/jpeg");
            }
            MemoryStream fileContent = await imageStore.GetStream(userInfo.ProfilePicture, BlobContainers.Profiles);
            byte[] fileContentBytes = fileContent.ToArray();

            return new FileContentResult(fileContentBytes, userInfo.GetPictureFileContentType());
        }

        /// <summary>
        /// Url for retrieving a user's profile picture from the blob storage.
        /// </summary>
        /// <param name="id">The file name of the image file.</param>
        /// <returns>FileContentResult with the image file.</returns>
        [AllowAnonymous]
        public async Task<FileContentResult> ProfilePictureFromBlob(string id)
        {
            // Todo: Access control.
            
            MemoryStream fileContent = await imageStore.GetStream(id, BlobContainers.Profiles);
            byte[] fileContentBytes = fileContent.ToArray();
            string contentType = FileContentTypeHelpers.GetContentTypeString(id);
            return new FileContentResult(fileContentBytes, contentType);
        }
    }
}