using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Services;
using KinaUna.IDP.Extensions;
using KinaUna.IDP.Models;
using KinaUna.IDP.Models.AccountViewModels;
using KinaUna.IDP.Models.ManageViewModels;
using KinaUna.IDP.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Configuration;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace KinaUna.IDP.Controllers
{
    [AllowAnonymous]
    [EnableCors("KinaUnaCors")]
    public class AccountController : Controller
    {
        private readonly ILoginService<ApplicationUser> _loginService;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly ILogger<AccountController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ProgenyDbContext _progContext;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public AccountController(
            ILoginService<ApplicationUser> loginService,
            IIdentityServerInteractionService interaction,
            ILogger<AccountController> logger,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            ProgenyDbContext progContext,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _loginService = loginService;
            _interaction = interaction;
            //_clientStore = clientStore;
            _logger = logger;
            _userManager = userManager;
            _emailSender = emailSender;
            _signInManager = signInManager;
            _context = context;
            _progContext = progContext;
            _configuration = configuration;
            _env = env;
        }

        [TempData] private string StatusMessage { get; set; }

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

            var vm = BuildLoginViewModel(returnUrl, context);

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

        LoginViewModel BuildLoginViewModel(string returnUrl, AuthorizationRequest context)
        {
            return new LoginViewModel
            {
                ReturnUrl = returnUrl,
                Email = context?.LoginHint,
            };
        }

        async Task<LoginViewModel> BuildLoginViewModelAsync(LoginViewModel model)
        {
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            var vm = BuildLoginViewModel(model.ReturnUrl, context);
            vm.Email = model.Email;
            vm.RememberMe = true;
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
                //return await Logout(new LogoutViewModel { LogoutId = logoutId });
                string logoutRedirectUri;
                if (_env.IsDevelopment())
                {
                    logoutRedirectUri = _configuration.GetValue<string>("WebServerLocal");
                }
                else
                {
                    logoutRedirectUri = _configuration.GetValue<string>("WebServer");
                }
                return Redirect(logoutRedirectUri);
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
            if (logout.PostLogoutRedirectUri == null)
            {
                if (_env.IsDevelopment())
                {
                    logout.PostLogoutRedirectUri = _configuration.GetValue<string>("WebServerLocal");
                }
                else
                {
                    logout.PostLogoutRedirectUri = _configuration.GetValue<string>("WebServer");
                }
            }
            return Redirect(logout.PostLogoutRedirectUri);
            
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
                    TimeZone = model.TimeZone,
                    JoinDate = DateTime.UtcNow,
                    Role = "Standard"
                };

                if (user.TimeZone == null)
                {
                    user.TimeZone = (await _userManager.FindByEmailAsync(Constants.AdminEmail)).TimeZone;
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
                await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl, model.Language);
                await _emailSender.SendEmailAsync(Constants.AdminEmail, "New User Registered",
                    "A user registered with this email address: " + model.Email);
                
            }

            if (returnUrl != null)
            {
                if (HttpContext.User.Identity.IsAuthenticated)
                    return Redirect(returnUrl);
                else
                    if (ModelState.IsValid)
                    return RedirectToAction("Login", "Account", new { returnUrl });
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

        [HttpPost]
        public async Task<IActionResult> ChangeEmail(string NewEmail, string OldEmail, string Language = "en")
        {
            
            if (User.Identity.IsAuthenticated)
            {
                ApplicationUser user = await _context.Users.SingleOrDefaultAsync(u => u.Email.ToUpper() == OldEmail.ToUpper());
                ApplicationUser test =
                    await _context.Users.SingleOrDefaultAsync(u => u.Email.ToUpper() == NewEmail.ToUpper());
                if (user != null)
                {
                    ChangeEmailViewModel model = new ChangeEmailViewModel();
                    model.OldEmail = OldEmail;
                    model.NewEmail = NewEmail;
                    model.ErrorMessage = "";
                    if (test != null)
                    {
                        string errorMsg =
                            "Error: This email is already in use by another account. Please delete the account with this email address before assigning this email address to your account.";
                        if (Language == "da")
                        {
                            errorMsg =
                                "Fejl: Denne emailadresse er allerede i brug for en anden konto. Slet venligst den anden konto før du opdaterer denne konto med emailaddressen.";
                        }
                        if (Language == "de")
                        {
                            errorMsg =
                                "Fehler: E-Mail wird von einem anderen Konto verwendet.";
                        }
                        model.ErrorMessage = errorMsg;
                    }
                    model.UserId = user.Id;
                    return View(model);
                }
            }

            return RedirectToAction("Register");
            

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendConfirmationMail(string UserId, string NewEmail, string OldEmail, string Language)
        {
            if (User.Identity.IsAuthenticated)
            {
                ApplicationUser user = await _context.Users.SingleOrDefaultAsync(u => u.Id == UserId);
                ApplicationUser test =
                    await _context.Users.SingleOrDefaultAsync(u => u.Email.ToUpper() == NewEmail.ToUpper());
                
                if (user.Id == UserId)
                {
                    if (test != null)
                    {
                        ChangeEmailViewModel model = new ChangeEmailViewModel();
                        model.OldEmail = OldEmail;
                        model.NewEmail = NewEmail;
                        string errorMsg =
                            "Error: This email is already in use by another account. Please delete the account with this email address before assigning this email address to your account.";
                        if (Language == "da")
                        {
                            errorMsg =
                                "Fejl: Denne emailadresse er allerede i brug for en anden konto. Slet venligst den anden konto før du opdaterer denne konto med emailaddressen.";
                        }
                        if (Language == "de")
                        {
                            errorMsg =
                                "Fehler: E-Mail wird von einem anderen Konto verwendet.";
                        }
                        model.ErrorMessage = errorMsg;
                        model.UserId = user.Id;
                        return View("ChangeEmail", model);
                    }
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
                    await _emailSender.SendEmailUpdateConfirmationAsync(NewEmail, callbackUrl + "&newEmail=" + NewEmail + "&oldEmail=" + OldEmail, Language);

                    UserInfo userinfo = await _progContext.UserInfoDb.SingleOrDefaultAsync(u => u.UserId == user.Id);
                    if (userinfo != null)
                    {
                        userinfo.UserEmail = NewEmail;
                        _progContext.UserInfoDb.Update(userinfo);
                        await _progContext.SaveChangesAsync();
                    }
                    user.Email = NewEmail;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("VerificationMailSent");
                }
            }

            return RedirectToAction("Register");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code, string newEmail = "", string oldEmail = "")
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

            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                user.UserName = user.Email;
                _context.Users.Update(user);
                _context.SaveChanges();
            }


            if (user.EmailConfirmed)
            {
                RedirectToAction("Login");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                if (user.JoinDate.AddDays(7) > DateTime.UtcNow)
                {
                    user.JoinDate = DateTime.UtcNow;
                }
                await _userManager.UpdateAsync(user);

                if (!String.IsNullOrEmpty(oldEmail))
                {
                    
                    // Todo: use api to update access lists instead.
                    List<UserAccess> userAccessList = await _progContext.UserAccessDb.Where(u => u.UserId.ToUpper() == oldEmail.ToUpper()).ToListAsync();
                    if (userAccessList.Any())
                    {
                        foreach (UserAccess ua in userAccessList)
                        {
                            ua.UserId = user.Email;
                        }

                        _progContext.UserAccessDb.UpdateRange(userAccessList);
                        await _progContext.SaveChangesAsync();
                    }

                    List<Progeny> progenyList = await _progContext.ProgenyDb
                        .Where(p => p.IsInAdminList(oldEmail)).ToListAsync();
                    if (progenyList.Any())
                    {
                        foreach (Progeny prog in progenyList)
                        {
                            string adminList = prog.Admins.ToUpper();
                            prog.Admins = adminList.Replace(oldEmail.ToUpper(), user.Email.ToUpper());
                        }
                        _progContext.ProgenyDb.UpdateRange(progenyList);
                        await _progContext.SaveChangesAsync();
                    }
                }
                else
                {
                    await _emailSender.SendEmailAsync(Constants.AdminEmail, "New User Confirmed Email",
                        "A user confirmed the email with this email address: " + user.Email);
                }
                
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
                string emailTitle = "Reset Kina Una Password";
                string emailText = $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>";

                if (model.Language == "da")
                {
                    emailTitle = "Nulstil Kina Una password";
                    emailText = $"Nulstil dit Kina Una password ved at klikke på dette link: <a href='{callbackUrl}'>link</a>";
                }
                if (model.Language == "de")
                {
                    emailTitle = "Kina Una passwort rücksetzen";
                    emailText = $"Setzen Sie Ihr Passwort zurück, indem Sie auf diesen Link klicken: <a href='{callbackUrl}'>link</a>";
                }

                await _emailSender.SendEmailAsync(model.Email, emailTitle,
                    emailText);
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
            string statusMsg = "Your password has been changed.";
            if (model.Language == "da")
            {
                statusMsg = "Dit password er opdateret.";
            }
            if (model.Language == "de")
            {
                statusMsg = "Ihr Passwort wurde geändert.";
            }
            StatusMessage = statusMsg;

            return RedirectToAction(nameof(ChangePassword));
        }
    }
}