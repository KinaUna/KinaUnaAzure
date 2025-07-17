using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.OpenIddict.Extensions;
using KinaUna.OpenIddict.Models.AccountViewModels;
using KinaUna.OpenIddict.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;

namespace KinaUna.OpenIddict.Controllers
{
    public class AccountController(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        IConfiguration configuration,
        ProgenyDbContext progContext,
        ApplicationDbContext context,
        ILocaleManager localeManager) : Controller
    {
        [TempData] private string? StatusMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewData["ReturnUrl"] = model.ReturnUrl;

            if (ModelState.IsValid)
            {
                List<Claim> claims = [new(ClaimTypes.Name, model.Username)];

                ClaimsIdentity claimsIdentity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(new ClaimsPrincipal(claimsIdentity));

                if (Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {

            RegisterViewModel model = new()
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = new()
                {
                    UserName = model.Email,
                    Email = model.Email,
                    TimeZone = model.TimeZone,
                    JoinDate = DateTime.UtcNow,
                    Role = "Standard"
                };

                user.TimeZone ??= (await userManager.FindByEmailAsync(Constants.DefaultUserEmail))!.TimeZone;

                if (model.Password != null)
                {
                    IdentityResult result = await userManager.CreateAsync(user, model.Password);
                    if (result.Errors.Any())
                    {
                        AddErrors(result);
                        // If we got this far, something failed, redisplay form
                        return View(model);
                    }
                }

                string code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                string callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme, model.LanguageId);
                if (!string.IsNullOrWhiteSpace(callbackUrl))
                {
                    await emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl, model.LanguageId);
                    await emailSender.SendEmailAsync(configuration.GetValue<string>("AdminEmail") ?? throw new InvalidOperationException("AdminEmail not found in configuration data."),
                        "New User Registered",
                        "A user registered with this email address: " + model.Email);

                    if (ModelState.IsValid)
                    {
                        return RedirectToAction("RegConfirm", "Account", new { model.ReturnUrl });
                    }
                }
            }
            
            return View(model);

        }

        [HttpGet]
        public IActionResult RegConfirm(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code, string newEmail = "", string oldEmail = "", string client = "KinaUna", string language = "")
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                // Todo: Show error page.
                return RedirectToAction("Index", "Home");
            }
            ApplicationUser? user = await userManager.FindByIdAsync(userId) ?? throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                user.UserName = user.Email;
                _ = context.Users.Update(user);
                _ = await context.SaveChangesAsync();
            }
            
            if (user.EmailConfirmed)
            {
                _ = RedirectToAction("Login");
            }
            
            IdentityResult result = await userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                user = await userManager.FindByIdAsync(userId);
                if (user != null && user.JoinDate.AddDays(7) > DateTime.UtcNow)
                {
                    user.JoinDate = DateTime.UtcNow;
                }

                _ = await userManager.UpdateAsync(user!);

                if (!string.IsNullOrEmpty(oldEmail) && !string.IsNullOrEmpty(user?.Email))
                {

                    // Todo: use api to update access lists instead.
                    List<UserAccess> userAccessList = await progContext.UserAccessDb.Where(u => u.UserId.Equals(oldEmail, StringComparison.CurrentCultureIgnoreCase)).ToListAsync();
                    if (userAccessList.Count != 0)
                    {
                        foreach (UserAccess ua in userAccessList)
                        {
                            ua.UserId = user.Email;
                        }

                        progContext.UserAccessDb.UpdateRange(userAccessList);
                        _ = await progContext.SaveChangesAsync();
                    }

                    List<Progeny> progenyList = await progContext.ProgenyDb.ToListAsync();
                    progenyList = progenyList.Where(p => p.IsInAdminList(oldEmail)).ToList();
                    if (progenyList.Count != 0)
                    {
                        foreach (Progeny prog in progenyList)
                        {
                            string adminList = prog.Admins.ToUpper();
                            prog.Admins = adminList.Replace(oldEmail.ToUpper(), user.Email!.ToUpper());
                        }
                        progContext.ProgenyDb.UpdateRange(progenyList);
                        _ = await progContext.SaveChangesAsync();
                    }
                }
                else
                {
                    await emailSender.SendEmailAsync(configuration.GetValue<string>("AdminEmail") ?? throw new InvalidOperationException("Admin email not found in configuration data."),
                        "New User Confirmed Email",
                        "A user confirmed the email with this email address: " + user?.Email);
                }
                
                return View();
            }

            List<KinaUnaLanguage> allLanguages = await localeManager.GetAllLanguages() ?? [];
            KinaUnaLanguage kinaUnaLanguage = allLanguages.SingleOrDefault(l => l.Code.Equals(language, StringComparison.CurrentCultureIgnoreCase)) ??
                                       new KinaUnaLanguage() { Id = 1, Code = "En", Name = "English", Icon = "", IconLink = "" };

            string code1 = await userManager.GenerateEmailConfirmationTokenAsync(user);
            string callbackUrl = Url.EmailConfirmationLink(user.Id, code1, Request.Scheme, kinaUnaLanguage.Id);
            if (string.IsNullOrWhiteSpace(callbackUrl)) return RedirectToAction("Index", "Home");
            if (user.Email != null) await emailSender.SendEmailConfirmationAsync(user.Email, callbackUrl, kinaUnaLanguage.Id);

            return RedirectToAction("VerificationMailSent");

            // Todo: Show error page.
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
            if (!ModelState.IsValid) return View(model);

            if (model.Email != null)
            {
                ApplicationUser? user = await userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                string code = await userManager.GeneratePasswordResetTokenAsync(user);
                string callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
                if (!string.IsNullOrWhiteSpace(callbackUrl))
                {
                    string emailTitle = await localeManager.GetTranslation("Reset KinaUna Password", PageNames.Account, model.LanguageId);
                    string emailText = await localeManager.GetTranslation("Please reset your KinaUna password by clicking here", PageNames.Account, model.LanguageId);
                    emailText += $": <a href='{callbackUrl}'>link</a>";

                    await emailSender.SendEmailAsync(model.Email, emailTitle,
                        emailText);

                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }
            }

            ModelState.AddModelError(string.Empty, "Error generating reset password link. Please try again later.");
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
        public IActionResult ResetPassword(string? code = null)
        {
            if (code == null)
            {
                throw new ApplicationException("A code must be supplied for password reset.");
            }
            ResetPasswordViewModel model = new() { Code = code };
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

            ApplicationUser? user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            IdentityResult result = await userManager.ResetPasswordAsync(user, model.Code, model.Password);
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
            _ = await userManager.GetUserAsync(User) ?? throw new ApplicationException($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            ChangePasswordViewModel model = new() { StatusMessage = StatusMessage };
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

            ApplicationUser user = await userManager.GetUserAsync(User) ?? throw new ApplicationException($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            IdentityResult changePasswordResult = await userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                AddErrors(changePasswordResult);
                return View(model);
            }

            await signInManager.SignInAsync(user, isPersistent: false);
            
            string statusMsg = await localeManager.GetTranslation("Your password has been changed.", PageNames.Account, model.LanguageId);

            StatusMessage = statusMsg;

            return RedirectToAction(nameof(ChangePassword));
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CheckDeleteKinaUnaAccount([FromBody] UserInfo userInfo)
        {
            UserInfo? deletedUserInfo = await progContext.DeletedUsers.AsNoTracking().SingleOrDefaultAsync(u => u.UserId == userInfo.UserId);
            if (deletedUserInfo == null) return Ok(new UserInfo());

            UserInfo confirmDeleteUserInfo = new()
            {
                UserId = deletedUserInfo.UserId,
                DeletedTime = deletedUserInfo.DeletedTime,
                Deleted = deletedUserInfo.Deleted
            };

            return Ok(confirmDeleteUserInfo);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> IsApplicationUserValid([FromBody] UserInfo userInfo)
        {
            ApplicationUser? user = await userManager.FindByIdAsync(userInfo.UserId);
            return Ok(user != null ? userInfo : new UserInfo());
        }

        [HttpPost]
        public async Task<IActionResult> RemoveDeleteKinaUnaAccount([FromBody] UserInfo userInfo)
        {
            ApplicationUser? user = await userManager.Users.SingleOrDefaultAsync(u => u.Id == User.GetUserId());
            if (user == null || user.Id == Constants.DefaultUserId || user.Id != userInfo.UserId) return Ok(new UserInfo());

            UserInfo? deletedUserInfo = await progContext.DeletedUsers.SingleOrDefaultAsync(u => u.UserId == userInfo.UserId);
            if (deletedUserInfo == null) return Ok(new UserInfo());

            UserInfo restoredDeleteUserInfo = new()
            {
                UserId = deletedUserInfo.UserId,
                DeletedTime = deletedUserInfo.DeletedTime,
                Deleted = deletedUserInfo.Deleted
            };

            _ = progContext.DeletedUsers.Remove(deletedUserInfo);
            return Ok(restoredDeleteUserInfo);

        }

        public async Task<IActionResult> DeleteAccount()
        {
            RegisterViewModel model = new();

            ApplicationUser? user = await userManager.Users.SingleOrDefaultAsync(u => u.Id == User.GetUserId());
            if (user == null || user.Id == Constants.DefaultUserId) return View(model);
            if (user.Email == null)
            {
                ModelState.AddModelError(string.Empty, "User email is not set. Cannot delete account.");
                return View(model);
            }

            model.Email = user.Email;
            string code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            string callbackUrl = Url.EmailDeleteAccountLink(user.Id, code, Request.Scheme);
            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                ModelState.AddModelError(string.Empty, "Error generating delete account link. Please try again later.");
                return View(model);
            }

            await emailSender.SendEmailDeleteAsync(model.Email, callbackUrl, model.LanguageId);

            UserInfo? userInfo = await progContext.DeletedUsers.SingleOrDefaultAsync(u => u.UserId == user.Id);
            if (userInfo != null && !string.IsNullOrEmpty(userInfo.UserId))
            {
                userInfo.UserName = user.UserName;
                userInfo.UserId = user.Id;
                userInfo.UserEmail = user.Email;
                userInfo.Deleted = false;
                userInfo.DeletedTime = DateTime.UtcNow;
                userInfo.UpdatedTime = DateTime.UtcNow;
                userInfo.ProfilePicture = JsonConvert.SerializeObject(user);
                _ = progContext.DeletedUsers.Update(userInfo);
            }
            else
            {
                userInfo = new UserInfo
                {
                    UserName = user.UserName,
                    UserId = user.Id,
                    UserEmail = user.Email,
                    Deleted = false,
                    DeletedTime = DateTime.UtcNow,
                    UpdatedTime = DateTime.UtcNow,
                    ProfilePicture = JsonConvert.SerializeObject(user)
                };
                _ = progContext.DeletedUsers.Add(userInfo);
            }

            _ = await progContext.SaveChangesAsync();


            return View(model);
        }

        public async Task<IActionResult> ConfirmDeleteAccount(string? userId, string? code)
        {
            RegisterViewModel model = new();

            if (userId == null || code == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ApplicationUser user = await userManager.FindByIdAsync(userId) ?? throw new ApplicationException($"Unable to load user with ID '{userId}'."); // Todo: Show error page.
            IdentityResult result = await userManager.ConfirmEmailAsync(user, code);
            if (!result.Succeeded) return View(model);

            UserInfo? userInfo = await progContext.DeletedUsers.SingleOrDefaultAsync(u => u.UserId == user.Id);
            if (userInfo == null) return View(model);

            userInfo.Deleted = true;
            userInfo.DeletedTime = DateTime.UtcNow;
            userInfo.UpdatedTime = DateTime.UtcNow;
            _ = progContext.DeletedUsers.Update(userInfo);
            _ = await progContext.SaveChangesAsync();
            _ = await userManager.DeleteAsync(user);
            await signInManager.SignOutAsync();

            return View(model);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
