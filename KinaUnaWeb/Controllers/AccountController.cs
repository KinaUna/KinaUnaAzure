using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IAuthHttpClient _authHttpClient;
        private readonly ImageStore _imageStore;
        private readonly IConfiguration _configuration;
        public AccountController(ImageStore imageStore, IConfiguration configuration, IAuthHttpClient authHttpClient, IUserInfosHttpClient userInfosHttpClient)
        {
            _imageStore = imageStore;
            _configuration = configuration;
            _authHttpClient = authHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
        }

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task SignIn(string returnUrl)
        {
            // clear any existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

            // see IdentityServer4 QuickStartUI AccountController ExternalLogin
            await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties()
                {
                    RedirectUri = returnUrl
                });

            //return Redirect(returnUrl);
        }
        
        [HttpPost]
        public async Task Login()
        {
            // clear any existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync( OpenIdConnectDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // see IdentityServer4 QuickStartUI AccountController ExternalLogin
            await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties()
                {
                    RedirectUri = Url.Action("LoginCallback"),
                });
        }

        public async Task NoFrameLogin()
        {
            // clear any existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // see IdentityServer4 QuickStartUI AccountController ExternalLogin
            await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties()
                {
                    RedirectUri = Url.Action("LoginCallback"),
                });
        }

        [HttpGet]
        public IActionResult LoginCallback()
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpPost]
        public async Task LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        }

        public async Task CheckOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> MyAccount()
        {
            string userEmail = User.GetEmail();
            _ = bool.TryParse(User.FindFirst("email_verified")?.Value, out bool mailConfirmed);
            _ = DateTime.TryParse(User.FindFirst("joindate")?.Value, out DateTime joinDate);

            UserInfo userInfo = await _userInfosHttpClient.GetUserInfo(userEmail) ?? throw new ApplicationException($"Unable to load user with email '{userEmail}'.");
            if (string.IsNullOrEmpty(userInfo.ProfilePicture))
            {
                userInfo.ProfilePicture = Constants.ProfilePictureUrl;
            }

            userInfo.ProfilePicture = _imageStore.UriFor(userInfo.ProfilePicture, BlobContainers.Profiles);

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

            model.ChangeLink = _configuration["AuthenticationServer"] + "/Account/ChangePassword";
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> MyAccount(UserInfoViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();

            string userEmail = User.GetEmail();
            _ = bool.TryParse(User.FindFirst("email_verified")?.Value, out bool mailConfirmed);

            UserInfo userInfo = await _userInfosHttpClient.GetUserInfo(userEmail);
            userInfo.CopyPropertiesFromUserInfoViewModel(model);
            
            bool emailChanged = false;
            if (userInfo.UserEmail.ToUpper() != model.UserEmail.ToUpper())
            {
                model.IsEmailConfirmed = false;
                emailChanged = true;
            }
            else
            {
                model.IsEmailConfirmed = mailConfirmed;
            }

            if (model.File != null && model.File.Name != string.Empty)
            {
                await using Stream stream = model.File.OpenReadStream();
                userInfo.ProfilePicture = await _imageStore.SaveImage(stream, BlobContainers.Profiles);
            }

            await _userInfosHttpClient.UpdateUserInfo(userInfo);
            
            model.ProfilePicture = userInfo.ProfilePicture;
            model.ProfilePicture = _imageStore.UriFor(userInfo.ProfilePicture, BlobContainers.Profiles);

            if (emailChanged)
            {
                return RedirectToAction("ChangeEmail", new {oldEmail = userEmail, newEmail = model.UserEmail});
            }
            return View(model);
        }

        public async Task<IActionResult> ChangeEmail(string oldEmail, string newEmail = "")
        {
            string userEmail = User.GetEmail();
            if (string.IsNullOrEmpty(newEmail))
            {
                newEmail = userEmail;
            }

            _ = bool.TryParse(User.FindFirst("email_verified")?.Value, out bool mailConfirmed);
            UserInfo userinfo = await _userInfosHttpClient.GetUserInfo(userEmail);
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
                ChangeLink = _configuration["AuthenticationServer"] + "/Account/ChangeEmail?NewEmail=" + newEmail + "&OldEmail=" + oldEmail
            };

            return View(model);
        }

        [Authorize]
        public IActionResult EnablePush()
        {
            ViewBag.UserId = User.GetUserId();
            ViewBag.PublicKey = _configuration["VapidPublicKey"];

            return View();
        }

        [Authorize]
        public IActionResult DisablePush()
        {
            ViewBag.UserId = User.GetUserId();
            ViewBag.PublicKey = _configuration["VapidPublicKey"];
            return View();
        }
        
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            string userId = User.GetUserId();
            _ = DateTime.TryParse(User.FindFirst("joindate")?.Value, out DateTime joinDate);
            
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(userId) ?? throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            if (string.IsNullOrEmpty(userInfo.ProfilePicture))
            {
                userInfo.ProfilePicture = Constants.ProfilePictureUrl;
            }

            userInfo.ProfilePicture = _imageStore.UriFor(userInfo.ProfilePicture, BlobContainers.Profiles);

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteAccount(UserInfoViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();

            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo != null && userInfo.UserEmail.ToUpper() == User.GetEmail().ToUpper())
            {
                model.UserId = userInfo.UserId;
                model.UserEmail = userInfo.UserEmail;
                model.FirstName = userInfo.FirstName;
                model.MiddleName = userInfo.MiddleName;
                model.LastName = userInfo.LastName;
                model.UserName = userInfo.UserName;

                _ = await _userInfosHttpClient.DeleteUserInfo(userInfo);

                model.ChangeLink = _configuration["AuthenticationServer"] + "/Account/DeleteAccount";
            }
            
            return Redirect(model.ChangeLink);
        }

        [Authorize]
        public async Task<IActionResult> UnDeleteAccount()
        {
            string userId = User.GetUserId();
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(userId) ?? throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            if (userInfo.UserId == userId)
            {
                userInfo.Deleted = false;
                _ = await _userInfosHttpClient.UpdateUserInfo(userInfo);
                _ = await _authHttpClient.RemoveDeleteUser(userInfo);
            }
            
            return RedirectToAction("Index", "Home");
        }
    }
}