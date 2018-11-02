using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using KinaUna.IDP.Data;
using KinaUna.IDP.Extensions;
using KinaUna.IDP.Models;
using KinaUna.IDP.Models.AccountViewModels;
using KinaUna.IDP.Models.DTOs;
using KinaUna.IDP.Models.ManageViewModels;
using KinaUna.IDP.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace KinaUna.IDP.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        //private readonly InMemoryUserLoginService _loginService;
        private readonly ILoginService<ApplicationUser> _loginService;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly ILogger<AccountController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(

            //InMemoryUserLoginService loginService,
            ILoginService<ApplicationUser> loginService,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            ILogger<AccountController> logger,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _loginService = loginService;
            _interaction = interaction;
            _clientStore = clientStore;
            _logger = logger;
            _userManager = userManager;
            _emailSender = emailSender;
            _signInManager = signInManager;
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Show login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null)
            {
                // if IdP is passed, then bypass showing the login screen
                return ExternalLogin(context.IdP, returnUrl);
            }

            var vm = await BuildLoginViewModelAsync(returnUrl, context);

            ViewData["ReturnUrl"] = returnUrl;

            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            model.RememberMe = true;
            if (ModelState.IsValid)
            {
                var user = await _loginService.FindByUsername(model.Email);
                if (await _loginService.ValidateCredentials(user, model.Password))
                {
                    AuthenticationProperties props = null;
                    if (model.RememberMe)
                    {
                        props = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(180)
                        };
                    };

                    await _loginService.SignIn(user);
                   
                    // make sure the returnUrl is still valid, and if yes - redirect back to authorize endpoint
                    if (_interaction.IsValidReturnUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }

                    return Redirect("~/");
                }

                ModelState.AddModelError("", "Invalid username or password.");
            }

            // something went wrong, show form with error
            var vm = await BuildLoginViewModelAsync(model);

            ViewData["ReturnUrl"] = model.ReturnUrl;

            return View(vm);
        }

        async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl, AuthorizationRequest context)
        {
            var allowLocal = true;
            if (context?.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;
                }
            }

            return new LoginViewModel
            {
                ReturnUrl = returnUrl,
                Email = context?.LoginHint,
            };
        }

        async Task<LoginViewModel> BuildLoginViewModelAsync(LoginViewModel model)
        {
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl, context);
            vm.Email = model.Email;
            vm.RememberMe = true; //model.RememberMe;
            return vm;
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                // if the user is not authenticated, then just show logged out page
                return await Logout(new LogoutViewModel { LogoutId = logoutId });
            }

            //Test for Xamarin. 
            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                //it's safe to automatically sign-out
                return await Logout(new LogoutViewModel { LogoutId = logoutId });
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            var vm = new LogoutViewModel
            {
                LogoutId = logoutId
            };
            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutViewModel model)
        {
            var idp = User?.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;

            if (idp != null && idp != IdentityServerConstants.LocalIdentityProvider)
            {
                if (model.LogoutId == null)
                {
                    // if there's no current logout context, we need to create one
                    // this captures necessary info from the current logged in user
                    // before we signout and redirect away to the external IdP for signout
                    model.LogoutId = await _interaction.CreateLogoutContextAsync();
                }

                string url = "/Account/Logout?logoutId=" + model.LogoutId;

                try
                {
                    
                    // hack: try/catch to handle social providers that throw
                    await HttpContext.SignOutAsync(idp, new AuthenticationProperties
                    {
                        RedirectUri = url
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex.Message);
                }
            }

            // delete authentication cookie
            await HttpContext.SignOutAsync();
            await _signInManager.SignOutAsync();
            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(model.LogoutId);

            return Redirect(logout?.PostLogoutRedirectUri);
        }

        public async Task<IActionResult> DeviceLogOut(string redirectUrl)
        {
            // delete authentication cookie
            await HttpContext.SignOutAsync();

            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            return Redirect(redirectUrl);
        }

        /// <summary>
        /// initiate roundtrip to external authentication provider
        /// </summary>
        [HttpGet]
        public IActionResult ExternalLogin(string provider, string returnUrl)
        {
            if (returnUrl != null)
            {
                returnUrl = UrlEncoder.Default.Encode(returnUrl);
            }
            returnUrl = "/account/externallogincallback?returnUrl=" + returnUrl;

            // start challenge and roundtrip the return URL
            var props = new AuthenticationProperties
            {
                RedirectUri = returnUrl,
                Items = { { "scheme", provider } }
            };
            return new ChallengeResult(provider, props);
        }


        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            RegisterViewModel model = new RegisterViewModel();
            return View(model);
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    //FirstName = model.User.FirstName,
                    //MiddleName = model.User.MiddleName,
                    //LastName = model.User.LastName,
                    TimeZone = model.TimeZone,
                    //PhoneNumber = model.User.PhoneNumber,
                    JoinDate = DateTime.UtcNow,
                    Role = "Standard"
                };

                if (user.TimeZone == null)
                {
                    user.TimeZone = (await _userManager.FindByEmailAsync("per.mogensen@gmail.com")).TimeZone;
                }
                
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Errors.Count() > 0)
                {
                    AddErrors(result);
                    // If we got this far, something failed, redisplay form
                    return View(model);
                }

                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
                await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);
                await _emailSender.SendEmailAsync("per.mogensen@kinauna.com", "New User Registered",
                    "A user registered with this email address: " + model.Email);
                
            }

            if (returnUrl != null)
            {
                if (HttpContext.User.Identity.IsAuthenticated)
                    return Redirect(returnUrl);
                else
                    if (ModelState.IsValid)
                    return RedirectToAction("Login", "Account", new { returnUrl = returnUrl });
                else
                    return View(model);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Redirecting()
        {
            return View();
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");

            }
            if (user.EmailConfirmed)
            {
                RedirectToAction("Login");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                user.JoinDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                
                await _emailSender.SendEmailAsync("per.mogensen@kinauna.com", "New User Confirmed Email",
                    "A user confirmed the email with this email address: " + user.Email);
                return View();
            }

            var code1 = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.EmailConfirmationLink(user.Id, code1, Request.Scheme);
            await _emailSender.SendEmailConfirmationAsync(user.Email, callbackUrl);
            return RedirectToAction("VerificationMailSent");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerificationMailSent()
        {
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
                await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                    $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                throw new ApplicationException("A code must be supplied for password reset.");
            }
            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            AddErrors(result);
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            //if (!hasPassword)
            //{
            //    return RedirectToAction(nameof(SetPassword));
            //}

            var model = new ChangePasswordViewModel { StatusMessage = StatusMessage };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                AddErrors(changePasswordResult);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User changed their password successfully.");
            StatusMessage = "Your password has been changed.";

            return RedirectToAction(nameof(ChangePassword));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ImportUsers()
        {
            ForgotPasswordViewModel model = new ForgotPasswordViewModel();
            model.Email = "test@tester.com";
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportUsers(ForgotPasswordViewModel code)
        {
            if (code.Email != "unakina2018@kinauna.com")
                return View(code);

            

            HttpClient usersHttpClient = new HttpClient();

            usersHttpClient.BaseAddress = new Uri("https://kinauna.com");
            usersHttpClient.DefaultRequestHeaders.Accept.Clear();
            usersHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // GET api/pictures/[id]
            string usersApiPath = "/api/azureexport/usersexport";
            var usersUri = "https://kinauna.com" + usersApiPath;

            var usersResponseString = await usersHttpClient.GetStringAsync(usersUri);

            List<UserDto> UserList = JsonConvert.DeserializeObject<List<UserDto>>(usersResponseString);
            List<ApplicationUser> addedUsers = new List<ApplicationUser>();
            foreach (UserDto usr in UserList)
            {
                ApplicationUser tempUser = await _context.Users.SingleOrDefaultAsync(l => l.Id == usr.Id);
                if (tempUser == null)
                {
                    ApplicationUser newUser = new ApplicationUser();
                    newUser.Id = usr.Id;
                    newUser.AccessFailedCount = usr.AccessFailedCount;
                    newUser.ConcurrencyStamp = usr.ConcurrencyStamp;
                    newUser.Email = usr.Email;
                    newUser.EmailConfirmed = usr.EmailConfirmed;
                    newUser.LockoutEnabled = usr.LockoutEnabled;
                    newUser.LockoutEnd = usr.LockoutEnd;
                    newUser.NormalizedEmail = usr.NormalizedEmail;
                    newUser.NormalizedUserName = usr.NormalizedUserName;
                    newUser.PasswordHash = usr.PasswordHash;
                    newUser.PhoneNumber = usr.PhoneNumber;
                    newUser.SecurityStamp = usr.SecurityStamp;
                    newUser.TwoFactorEnabled = usr.TwoFactorEnabled;
                    newUser.UserName = usr.UserName;
                    newUser.FirstName = usr.FirstName;
                    newUser.JoinDate = usr.JoinDate;
                    newUser.LastName = usr.LastName;
                    newUser.MiddleName = usr.MiddleName;
                    newUser.TimeZone = usr.TimeZone;
                    newUser.ViewChild = usr.ViewChild;
                    newUser.Role = "Standard";
                    
                    await _context.Users.AddAsync(newUser);
                    addedUsers.Add(newUser);
                }
            }

            await _context.SaveChangesAsync();

            code.Email = "UsersAdded@" + addedUsers.Count + ".net";
            return View(code);
        }
    }
}