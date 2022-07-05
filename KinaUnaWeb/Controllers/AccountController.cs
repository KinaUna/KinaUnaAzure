using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using KinaUna.Data;
using KinaUna.Data.Extensions;

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
            // Todo: Check if returnUrl is a PivoQ address.
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

        public async Task LogOut()
        {
            // Clears the local cookie.
            // await HttpContext.SignOutAsync("Cookie");
            // await HttpContext.SignOutAsync("oidc");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

            
            //var homeUrl = Url.Action(nameof(HomeController.Index), "Home");
            //return new SignOutResult(OpenIdConnectDefaults.AuthenticationScheme,
            //    new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = homeUrl });
        }

        public async Task CheckOut(string returnUrl = null)
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
            string userEmail = HttpContext.User.FindFirst("email")?.Value;
            Boolean.TryParse(HttpContext.User.FindFirst("email_verified")?.Value, out var mailConfirmed);
            DateTime.TryParse(HttpContext.User.FindFirst("joindate")?.Value, out var joinDate);
            var userinfo = await _userInfosHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                throw new ApplicationException($"Unable to load user with email '{userEmail}'.");
            }

            if (String.IsNullOrEmpty(userinfo.ProfilePicture))
            {
                userinfo.ProfilePicture = Constants.ProfilePictureUrl;
            }

            if (!userinfo.ProfilePicture.ToLower().StartsWith("http"))
            {
                userinfo.ProfilePicture = _imageStore.UriFor(userinfo.ProfilePicture, BlobContainers.Profiles);
            }
            var model = new UserInfoViewModel
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
                PhoneNumber = HttpContext.User.FindFirst("phone_number")?.Value ?? "",
                ProfilePicture = userinfo.ProfilePicture
            };

            if (String.IsNullOrEmpty(model.UserName))
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
            string userEmail = HttpContext.User.FindFirst("email")?.Value;
            Boolean.TryParse(HttpContext.User.FindFirst("email_verified")?.Value, out var mailConfirmed);
            var userinfo = await _userInfosHttpClient.GetUserInfo(userEmail);
            // userinfo.UserEmail = model.UserEmail;
            userinfo.FirstName = model.FirstName;
            userinfo.MiddleName = model.MiddleName;
            userinfo.LastName = model.LastName;

            userinfo.UserName = model.UserName;
            if (String.IsNullOrEmpty(userinfo.UserName))
            {
                userinfo.UserName = userinfo.UserEmail;
            }

            userinfo.Timezone = model.Timezone;

            if (String.IsNullOrEmpty(userinfo.ProfilePicture))
            {
                userinfo.ProfilePicture = Constants.ProfilePictureUrl;
            }

            bool emailChanged = false;
            if (userinfo.UserEmail.ToUpper() != model.UserEmail.ToUpper())
            {
                model.IsEmailConfirmed = false;
                emailChanged = true;
            }
            else
            {
                model.IsEmailConfirmed = mailConfirmed;
            }

            if (model.File != null && model.File.Name != String.Empty)
            {
                using (var stream = model.File.OpenReadStream())
                {
                    userinfo.ProfilePicture = await _imageStore.SaveImage(stream, BlobContainers.Profiles);
                }
            }

            await _userInfosHttpClient.UpdateUserInfo(userinfo);

            
            model.ProfilePicture = userinfo.ProfilePicture;
            if (!userinfo.ProfilePicture.ToLower().StartsWith("http"))
            {
                model.ProfilePicture = _imageStore.UriFor(userinfo.ProfilePicture, BlobContainers.Profiles);
            }

            if (emailChanged)
            {
                return RedirectToAction("ChangeEmail", new {oldEmail = userEmail, newEmail = model.UserEmail});
            }
            return View(model);
        }

        public async Task<IActionResult> ChangeEmail(string oldEmail, string newEmail = "")
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value;
            if (String.IsNullOrEmpty(newEmail))
            {
                newEmail = userEmail;
            }

            Boolean.TryParse(HttpContext.User.FindFirst("email_verified")?.Value, out var mailConfirmed);
            var userinfo = await _userInfosHttpClient.GetUserInfo(userEmail);
            DateTime.TryParse(HttpContext.User.FindFirst("joindate")?.Value, out var joinDate);
            var model = new UserInfoViewModel
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
                PhoneNumber = HttpContext.User.FindFirst("phone_number")?.Value ?? "",
                ProfilePicture = userinfo.ProfilePicture
            };
            model.ChangeLink = _configuration["AuthenticationServer"] + "/Account/ChangeEmail?NewEmail=" + newEmail + "&OldEmail=" + oldEmail;

            return View(model);
        }

        [Authorize]
        public IActionResult EnablePush()
        {
            string userId = HttpContext.User.FindFirst("sub")?.Value;
            ViewBag.UserId = userId;
            ViewBag.PublicKey = _configuration["VapidPublicKey"];
            return View();
        }

        [Authorize]
        public IActionResult DisablePush()
        {
            string userId = HttpContext.User.FindFirst("sub")?.Value;
            ViewBag.UserId = userId;
            ViewBag.PublicKey = _configuration["VapidPublicKey"];
            return View();
        }
        
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            string userId = HttpContext.User.GetUserId();
            DateTime.TryParse(HttpContext.User.FindFirst("joindate")?.Value, out var joinDate);
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(userId);
            if (userInfo == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            }

            if (string.IsNullOrEmpty(userInfo.ProfilePicture))
            {
                userInfo.ProfilePicture = Constants.ProfilePictureUrl;
            }

            if (!userInfo.ProfilePicture.ToLower().StartsWith("http"))
            {
                userInfo.ProfilePicture = _imageStore.UriFor(userInfo.ProfilePicture, BlobContainers.Profiles);
            }

            UserInfoViewModel model = new UserInfoViewModel
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
                PhoneNumber = HttpContext.User.FindFirst("phone_number")?.Value ?? "",
                ProfilePicture = userInfo.ProfilePicture
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
            string userId = HttpContext.User.GetUserId();
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(userId);
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
            string userId = HttpContext.User.GetUserId();
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(userId);
            if (userInfo == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            }

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