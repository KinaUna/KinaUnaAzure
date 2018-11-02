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
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IHostingEnvironment _env;
        public AccountController(IProgenyHttpClient progenyHttpClient, IHostingEnvironment env)
        {
            _progenyHttpClient = progenyHttpClient;
            _env = env;
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
        [ValidateAntiForgeryToken]
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

        
        [HttpGet]
        public IActionResult LoginCallback()
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        public async Task<IActionResult> LogOut()
        {
            // Clears the local cookie.
            // await HttpContext.SignOutAsync("Cookie");
            // await HttpContext.SignOutAsync("oidc");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);


            var homeUrl = Url.Action(nameof(HomeController.Index), "Home");
            return new SignOutResult(OpenIdConnectDefaults.AuthenticationScheme,
                new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = homeUrl });
        }

        public async Task<IActionResult> CheckOut(string returnUrl = null)
        {
            // Clears the local cookie.
            // await HttpContext.SignOutAsync("Cookie");
            // await HttpContext.SignOutAsync("oidc");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);


            var homeUrl = Url.Action(nameof(HomeController.Index), "Home");
            return new SignOutResult(OpenIdConnectDefaults.AuthenticationScheme,
                new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = returnUrl });
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public async Task<IActionResult> MyAccount()
        {
            string userEmail = HttpContext.User.FindFirst("email").Value;
            bool mailConfirmed = false;
            Boolean.TryParse(HttpContext.User.FindFirst("email_verified").Value, out mailConfirmed);
            DateTime joinDate = DateTime.UtcNow;
            DateTime.TryParse(HttpContext.User.FindFirst("joindate").Value, out joinDate);
            UserInfo userinfo = new UserInfo();
            userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                throw new ApplicationException($"Unable to load user with email '{userEmail}'.");
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
                JoinDate = joinDate.ToString(),
                IsEmailConfirmed = mailConfirmed,
                PhoneNumber = HttpContext.User.FindFirst("phone_number")?.Value ?? ""

            };

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
        public async Task<IActionResult> MyAccount(UserInfoViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email").Value;
            bool mailConfirmed = false;
            Boolean.TryParse(HttpContext.User.FindFirst("email_verified").Value, out mailConfirmed);
            UserInfo userinfo = new UserInfo();
            userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            userinfo.UserEmail = model.UserEmail;
            userinfo.FirstName = model.FirstName;
            userinfo.MiddleName = model.MiddleName;
            userinfo.LastName = model.LastName;
            userinfo.UserName = model.UserName;
            userinfo.Timezone = model.Timezone;

            if (userinfo.UserEmail.ToUpper() != model.UserEmail.ToUpper())
            {
                model.IsEmailConfirmed = false;
            }
            else
            {
                model.IsEmailConfirmed = mailConfirmed;
            }

            await _progenyHttpClient.UpdateUserInfo(userinfo);
            
            return View(model);
        }
    }
}