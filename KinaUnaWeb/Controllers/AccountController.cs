using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.Threading.Tasks;
using KinaUnaWeb.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KinaUnaWeb.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IHostingEnvironment _env;
        private readonly ApplicationDbContext _appDbContext;
        private readonly ImageStore _imageStore;
        private readonly IConfiguration _configuration;
        public AccountController(IProgenyHttpClient progenyHttpClient, IHostingEnvironment env, ApplicationDbContext appDbContext, ImageStore imageStore, IConfiguration configuration)
        {
            _progenyHttpClient = progenyHttpClient;
            _env = env;
            _appDbContext = appDbContext;
            _imageStore = imageStore;
            _configuration = configuration;
        }

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> SignIn(string returnUrl)
        {
            var token = await HttpContext.GetTokenAsync("access_token");

            if (token != null)
            {
                ViewData["access_token"] = token;
            }

            // "Catalog" because UrlHelper doesn't support nameof() for controllers
            // https://github.com/aspnet/Mvc/issues/5853
            return RedirectToAction(nameof(HomeController.Index), "Home");
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
            // Clears the local cookie.
            // await HttpContext.SignOutAsync("Cookie");
            // await HttpContext.SignOutAsync("oidc");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);


            //var homeUrl = Url.Action(nameof(HomeController.Index), "Home");
            //return new SignOutResult(OpenIdConnectDefaults.AuthenticationScheme,
            //    new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = returnUrl });
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> MyAccount()
        {
            string userEmail = HttpContext.User.FindFirst("email").Value;
            Boolean.TryParse(HttpContext.User.FindFirst("email_verified").Value, out var mailConfirmed);
            DateTime.TryParse(HttpContext.User.FindFirst("joindate").Value, out var joinDate);
            var userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                throw new ApplicationException($"Unable to load user with email '{userEmail}'.");
            }

            if (String.IsNullOrEmpty(userinfo.ProfilePicture))
            {
                userinfo.ProfilePicture = "https://web.kinauna.com/photodb/profile.jpg";
            }

            if (!userinfo.ProfilePicture.ToLower().StartsWith("http"))
            {
                userinfo.ProfilePicture = _imageStore.UriFor(userinfo.ProfilePicture, "profiles");
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

            if (_env.IsDevelopment())
            {
                model.ChangeLink = "https://localhost:44397/Account/ChangePassword";
            }
            else
            {
                model.ChangeLink = "https://auth.kinauna.com/Account/ChangePassword";
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> MyAccount(UserInfoViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email").Value;
            Boolean.TryParse(HttpContext.User.FindFirst("email_verified").Value, out var mailConfirmed);
            var userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
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
                userinfo.ProfilePicture = "https://web.kinauna.com/photodb/profile.jpg";
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
                string oldPictureLink = userinfo.ProfilePicture;
                using (var stream = model.File.OpenReadStream())
                {
                    userinfo.ProfilePicture = await _imageStore.SaveImage(stream, "profiles");
                }

                if (!oldPictureLink.ToLower().StartsWith("http") && !String.IsNullOrEmpty(oldPictureLink))
                {
                    await _imageStore.DeleteImage(oldPictureLink, "profiles");
                }
            }

            await _progenyHttpClient.UpdateUserInfo(userinfo);

            // Todo: This should be done via api instead of direct database access.
            ApplicationUser user = await _appDbContext.Users.SingleOrDefaultAsync(u => u.Id == userinfo.UserId);
            user.FirstName = userinfo.FirstName;
            user.MiddleName = userinfo.MiddleName;
            user.LastName = userinfo.LastName;
            // user.Email = userinfo.UserEmail;
            user.EmailConfirmed = model.IsEmailConfirmed;
            user.UserName = userinfo.UserName;
            user.TimeZone = userinfo.Timezone;
            
            _appDbContext.Users.Update(user);

            await _appDbContext.SaveChangesAsync();
            
            model.ProfilePicture = userinfo.ProfilePicture;
            if (!userinfo.ProfilePicture.ToLower().StartsWith("http"))
            {
                model.ProfilePicture = _imageStore.UriFor(userinfo.ProfilePicture, "profiles");
            }

            if (emailChanged)
            {
                return RedirectToAction("ChangeEmail", new {oldEmail = userEmail, newEmail = model.UserEmail});
            }
            return View(model);
        }

        public async Task<IActionResult> ChangeEmail(string oldEmail, string newEmail = "")
        {
            string userEmail = HttpContext.User.FindFirst("email").Value;
            if (String.IsNullOrEmpty(newEmail))
            {
                newEmail = userEmail;
            }

            Boolean.TryParse(HttpContext.User.FindFirst("email_verified").Value, out var mailConfirmed);
            var userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            DateTime.TryParse(HttpContext.User.FindFirst("joindate").Value, out var joinDate);
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
            if (_env.IsDevelopment())
            {
                model.ChangeLink = "https://localhost:44397/Account/ChangeEmail?NewEmail=" + newEmail + "&OldEmail=" + oldEmail;
            }
            else
            {
                model.ChangeLink = "https://auth.kinauna.com/Account/ChangeEmail?NewEmail=" + newEmail + "&OldEmail=" + oldEmail;
            }
            return View(model);
        }

        [Authorize]
        public IActionResult EnablePush()
        {
            string userId = HttpContext.User.FindFirst("sub").Value;
            ViewBag.UserId = userId;
            ViewBag.PublicKey = _configuration["VapidPublicKey"];
            return View();
        }

        [Authorize]
        public IActionResult DisablePush()
        {
            string userId = HttpContext.User.FindFirst("sub").Value;
            ViewBag.UserId = userId;
            ViewBag.PublicKey = _configuration["VapidPublicKey"];
            return View();
        }
        
    }
}