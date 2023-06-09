﻿using IdentityModel;
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
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace KinaUna.IDP.Controllers
{
    [AllowAnonymous]
    //[EnableCors("KinaUnaCors")]
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

        [TempData] 
        private string StatusMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Show login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            AuthorizationRequest context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null)
            {
                // if IdP is passed, then bypass showing the login screen
                return ExternalLogin(context.IdP, returnUrl);
            }

            LoginViewModel vm = BuildLoginViewModel(returnUrl, context);

            ViewData["ReturnUrl"] = returnUrl;
            if (context != null && context.Client.ClientId.ToLower().Contains("maui"))
            {
                ViewData["AccountType"] = "KinaUna MAUI";
            }
            else
            {
                ViewData["AccountType"] = "KinaUna";
            }

            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // delete authentication cookie
                await HttpContext.SignOutAsync();
                await _signInManager.SignOutAsync();
            }

            model.RememberMe = true;
            if (ModelState.IsValid)
            {
                ApplicationUser user = await _loginService.FindByUsername(model.Email);
                if (user != null)
                {
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
                }
                

                ModelState.AddModelError("", "Invalid username or password.");
            }

            // something went wrong, show form with error
            LoginViewModel vm = await BuildLoginViewModelAsync(model);

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
            AuthorizationRequest context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            LoginViewModel vm = BuildLoginViewModel(model.ReturnUrl, context);
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
            LogoutRequest context = await _interaction.GetLogoutContextAsync(logoutId);
            
            if (User.Identity != null && User.Identity.IsAuthenticated == false)
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
                    if (context != null && context.ClientId.ToLower().Contains("maui"))
                    {
                        logoutRedirectUri = "kinaunamaui://callback";
                    }
                    else
                    {
                        logoutRedirectUri = _configuration.GetValue<string>("WebServer");
                    }

                }
                return Redirect(logoutRedirectUri!);
            }

            if (context != null && context.ClientId != null && context.ClientId.ToLower().Contains("kinaunamaui"))
            {
                return await Logout(new LogoutViewModel { LogoutId = logoutId });
            }
            
            if (context?.ShowSignoutPrompt == false)
            {
                //it's safe to automatically sign-out
                return await Logout(new LogoutViewModel { LogoutId = logoutId });
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            LogoutViewModel vm = new LogoutViewModel
            {
                LogoutId = logoutId
            };

            if (context != null && context.ClientId != null && context.ClientId.ToLower().Contains("maui"))
            {
                ViewData["AccountType"] = "KinaUna MAUI";

            }
            else
            {
                ViewData["AccountType"] = "KinaUna";
            }

            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutViewModel model)
        {
            string idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;

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
            LogoutRequest logout = await _interaction.GetLogoutContextAsync(model.LogoutId);
            if (logout.PostLogoutRedirectUri == null)
            {
                if (_env.IsDevelopment())
                {
                    if (logout.ClientId != null && logout.ClientId.ToLower().Contains("blazor"))
                    {
                        logout.PostLogoutRedirectUri = _configuration.GetValue<string>("WebBlazorServerLocal");
                    }
                    else
                    {
                        logout.PostLogoutRedirectUri = _configuration.GetValue<string>("WebServerLocal");
                    }

                }
                else
                {
                    if (logout.ClientId != null && logout.ClientId.ToLower().Contains("maui"))
                    {
                        logout.PostLogoutRedirectUri = "kinaunamaui://callback";
                    }
                    else
                    {
                        if (logout.ClientId != null && logout.ClientId.ToLower().Contains("blazor"))
                        {
                            logout.PostLogoutRedirectUri = _configuration.GetValue<string>("WebBlazorServer");
                        }
                        else
                        {
                            logout.PostLogoutRedirectUri = _configuration.GetValue<string>("WebServer");
                        }
                    }

                }
            }

            if (logout.PostLogoutRedirectUri != null)
            {
                return Redirect(logout.PostLogoutRedirectUri);
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> DeviceLogOut(string redirectUrl)
        {
            // delete authentication cookie
            await HttpContext.SignOutAsync();

            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            return Redirect(redirectUrl);
        }
        
        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            RegisterViewModel model = new RegisterViewModel();
            ViewData["AccountType"] = "KinaUna";
            
            return View(model);
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            string clientId = "KinaUna";
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                ApplicationUser user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    TimeZone = model.TimeZone,
                    JoinDate = DateTime.UtcNow,
                    Role = "Standard"
                };

                if (user.TimeZone == null)
                {
                    user.TimeZone = ((await _userManager.FindByEmailAsync(Constants.DefaultUserEmail))!).TimeZone;
                }
                
                IdentityResult result = await _userManager.CreateAsync(user, model.Password);
                if (result.Errors.Any())
                {
                    AddErrors(result);
                    // If we got this far, something failed, redisplay form
                    return View(model);
                }

                string code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                string callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme, clientId, model.Language);
                await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl, clientId, model.Language);
                await _emailSender.SendEmailAsync(_configuration.GetValue<string>("AdminEmail"), "New User Registered",
                    "A user registered with this email address: " + model.Email, clientId);
                
            }

            if (returnUrl != null)
            {
                if (ModelState.IsValid)
                {
                    return RedirectToAction("RegConfirm", "Account", new { returnUrl });
                }
                    
                
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult RegConfirm(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        [HttpGet]
        public IActionResult Redirecting()
        {
            return View();
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeEmail(string NewEmail, string OldEmail, string Language = "en", string client="KinaUna")
        {
            
            if (User.Identity != null && User.Identity.IsAuthenticated)
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
                    model.Client = client;
                    return View(model);
                }
            }

            return RedirectToAction("Register");
            

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendConfirmationMail(string UserId, string NewEmail, string OldEmail, string Language, string Client="KinaUna")
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                string client = Client;
                ApplicationUser user = await _context.Users.SingleOrDefaultAsync(u => u.Id == UserId);
                ApplicationUser test =
                    await _context.Users.SingleOrDefaultAsync(u => u.Email.ToUpper() == NewEmail.ToUpper());
                
                if (user != null && user.Id == UserId)
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
                    string code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    string callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme, client, Language);
                    await _emailSender.SendEmailUpdateConfirmationAsync(NewEmail, callbackUrl + "&newEmail=" + NewEmail + "&oldEmail=" + OldEmail, client, Language);

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
        public async Task<IActionResult> ConfirmEmail(string userId, string code, string newEmail = "", string oldEmail = "", string client = "KinaUna", string language="")
        {
            if (userId == null || code == null)
            {
                return RedirectToAction("Index", "Home");
            }
            ApplicationUser user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");

            }

            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                user.UserName = user.Email;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }


            if (user.EmailConfirmed)
            {
                RedirectToAction("Login");
            }
            IdentityResult result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                user = await _userManager.FindByIdAsync(userId);
                if (user != null && user.JoinDate.AddDays(7) > DateTime.UtcNow)
                {
                    user.JoinDate = DateTime.UtcNow;
                }

                await _userManager.UpdateAsync(user!);

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
                            prog.Admins = adminList.Replace(oldEmail.ToUpper(), user.Email!.ToUpper());
                        }
                        _progContext.ProgenyDb.UpdateRange(progenyList);
                        await _progContext.SaveChangesAsync();
                    }
                }
                else
                {
                    string clientType = "KinaUna";
                    await _emailSender.SendEmailAsync(_configuration.GetValue<string>("AdminEmail"), "New User Confirmed Email", "A user confirmed the email with this email address: " + user.Email, clientType);
                }

                ViewData["AccountType"] = "KinaUna";
                
                return View();
            }

            
            string code1 = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            string callbackUrl = Url.EmailConfirmationLink(user.Id, code1, Request.Scheme, client, language);
            await _emailSender.SendEmailConfirmationAsync(user.Email, callbackUrl, client, language);
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
                ApplicationUser user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                string code = await _userManager.GeneratePasswordResetTokenAsync(user);
                string callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
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
                    emailText, "KinaUna");
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
            ResetPasswordViewModel model = new ResetPasswordViewModel { Code = code };
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
            ApplicationUser user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            IdentityResult result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
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
            ApplicationUser user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            
            ChangePasswordViewModel model = new ChangePasswordViewModel { StatusMessage = StatusMessage };
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

            ApplicationUser user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            IdentityResult changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
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

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2Fa(bool rememberMe, string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            ApplicationUser user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            LoginWith2faViewModel model = new LoginWith2faViewModel { RememberMe = rememberMe };
            ViewData["ReturnUrl"] = returnUrl;

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2Fa(LoginWith2faViewModel model, bool rememberMe, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApplicationUser user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            string authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            SignInResult result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, model.RememberMachine);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} logged in with 2fa.", user.Id);
                return RedirectToLocal(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                _logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return View();
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            ApplicationUser user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApplicationUser user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            string recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);

            SignInResult result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} logged in with a recovery code.", user.Id);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                _logger.LogWarning("Invalid recovery code entered for user with ID {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
                return View();
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            string redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            AuthenticationProperties properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToAction(nameof(Login));
            }
            ExternalLoginInfo info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            string email = info.Principal.FindFirstValue(ClaimTypes.Email);
            ApplicationUser user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email.ToUpper() == email.ToUpper());
            if (user != null)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                // return RedirectToLocal(returnUrl);
                return Redirect(returnUrl ?? Constants.WebAppUrl);
            }
            else
            {
                // If user does not already exists, invite User to register.
                SignInResult result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation(5, "User logged in with {Name} provider.", info.LoginProvider);
                    // return RedirectToLocal(returnUrl);
                    return Redirect(returnUrl ?? Constants.WebAppUrl);
                }

                //if (result.RequiresTwoFactor)
                //{
                //    return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl });
                //}

                if (result.IsLockedOut)
                {
                    return View("Lockout");
                }
                else
                {
                    // If the user does not have an account, then ask the user to create an account.
                    ViewData["ReturnUrl"] = returnUrl;
                    ViewData["LoginProvider"] = info.LoginProvider;
                    email = info.Principal.FindFirstValue(ClaimTypes.Email);
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
                }
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                ExternalLoginInfo info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    throw new ApplicationException("Error loading external login information during confirmation.");
                }
                ApplicationUser user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                IdentityResult result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(nameof(ExternalLogin), model);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CheckDeleteKinaUnaAccount([FromBody] UserInfo userInfo)
        {
            UserInfo deletedUserInfo = await _progContext.DeletedUsers.AsNoTracking().SingleOrDefaultAsync(u => u.UserId == userInfo.UserId);
            if (deletedUserInfo != null)
            {
                UserInfo confirmDeleteUserInfo = new UserInfo();
                confirmDeleteUserInfo.UserId = deletedUserInfo.UserId;
                confirmDeleteUserInfo.DeletedTime = deletedUserInfo.DeletedTime;
                confirmDeleteUserInfo.Deleted = deletedUserInfo.Deleted;
                return Ok(confirmDeleteUserInfo);
            }

            return Ok(new UserInfo());
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> IsApplicationUserValid([FromBody] UserInfo userInfo)
        {
            ApplicationUser user = await _userManager.FindByIdAsync(userInfo.UserId);
            if (user != null)
            {
                return Ok(userInfo);
            }

            return Ok(new UserInfo());
        }

        [HttpPost]
        public async Task<IActionResult> RemoveDeleteKinaUnaAccount([FromBody] UserInfo userInfo)
        {
            ApplicationUser user = await _userManager.Users.SingleOrDefaultAsync(u => u.Id == User.GetUserId());
            if (user != null && user.Id != Constants.DefaultUserId && user.Id == userInfo.UserId)
            {
                UserInfo deletedUserInfo = await _progContext.DeletedUsers.SingleOrDefaultAsync(u => u.UserId == userInfo.UserId);
                if (deletedUserInfo != null)
                {
                    UserInfo restoredDeleteUserInfo = new UserInfo();
                    restoredDeleteUserInfo.UserId = deletedUserInfo.UserId;
                    restoredDeleteUserInfo.DeletedTime = deletedUserInfo.DeletedTime;
                    restoredDeleteUserInfo.Deleted = deletedUserInfo.Deleted;

                    _progContext.DeletedUsers.Remove(deletedUserInfo);
                    return Ok(restoredDeleteUserInfo);
                }
            }
            
            return Ok(new UserInfo());
        }
        
        public async Task<IActionResult> DeleteAccount()
        {
            RegisterViewModel model = new RegisterViewModel();
            //model.LanguageId = Request.GetLanguageIdFromCookie();
            //model.RegionId = Request.GetRegionIdFromCookie();

            ApplicationUser user = await _userManager.Users.SingleOrDefaultAsync(u => u.Id == User.GetUserId());
            if (user != null && user.Id != Constants.DefaultUserId)
            {
                model.Email = user.Email;
                string code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                string callbackUrl = Url.EmailDeleteAccountLink(user.Id, code, Request.Scheme);
                await _emailSender.SendEmailDeleteAsync(model.Email, callbackUrl, model.LanguageId);

                UserInfo userInfo = await _progContext.DeletedUsers.SingleOrDefaultAsync(u => u.UserId == user.Id);
                if (userInfo != null && !string.IsNullOrEmpty(userInfo.UserId))
                {
                    userInfo.UserName = user.UserName;
                    userInfo.UserId = user.Id;
                    userInfo.UserEmail = user.Email;
                    userInfo.Deleted = false;
                    userInfo.DeletedTime = DateTime.UtcNow;
                    userInfo.UpdatedTime = DateTime.UtcNow;
                    userInfo.ProfilePicture = JsonConvert.SerializeObject(user);
                    _progContext.DeletedUsers.Update(userInfo);
                    await _progContext.SaveChangesAsync();
                }
                else
                {
                    userInfo = new UserInfo();
                    userInfo.UserName = user.UserName;
                    userInfo.UserId = user.Id;
                    userInfo.UserEmail = user.Email;
                    userInfo.Deleted = false;
                    userInfo.DeletedTime = DateTime.UtcNow;
                    userInfo.UpdatedTime = DateTime.UtcNow;
                    userInfo.ProfilePicture = JsonConvert.SerializeObject(user);
                    await _progContext.DeletedUsers.AddAsync(userInfo);
                    await _progContext.SaveChangesAsync();
                }
            }
            

            return View(model);
        }

        public async Task<IActionResult> ConfirmDeleteAccount(string userId, string code)
        {
            RegisterViewModel model = new RegisterViewModel();
            //model.LanguageId = Request.GetLanguageIdFromCookie();
            // model.RegionId = Request.GetRegionIdFromCookie();

            if (userId == null || code == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ApplicationUser user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");

            }

            IdentityResult result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                UserInfo userInfo = await _progContext.DeletedUsers.SingleOrDefaultAsync(u => u.UserId == user.Id);
                if (userInfo != null)
                {
                    userInfo.Deleted = true;
                    userInfo.DeletedTime = DateTime.UtcNow;
                    userInfo.UpdatedTime = DateTime.UtcNow;
                    _progContext.DeletedUsers.Update(userInfo);
                    await _progContext.SaveChangesAsync();
                    await _userManager.DeleteAsync(user);
                    await _signInManager.SignOutAsync();
                }

            }

            return View(model);
        }
    }
    
}