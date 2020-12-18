using System.Threading.Tasks;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace KinaUna.IDP.Services
{
    public class EfLoginService : ILoginService<ApplicationUser>
    {
        readonly UserManager<ApplicationUser> _userManager;
        readonly SignInManager<ApplicationUser> _signInManager;

        public EfLoginService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<ApplicationUser> FindByUsername(string user)
        {
            ApplicationUser appUser = await _userManager.FindByEmailAsync(user);
            if (string.IsNullOrEmpty(appUser.UserName))
            {
                appUser.UserName = appUser.Email;
                await _userManager.UpdateAsync(appUser);
            }

            return appUser;
        }

        public async Task<bool> ValidateCredentials(ApplicationUser user, string password)
        {
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public Task SignIn(ApplicationUser user)
        {
            return _signInManager.SignInAsync(user, true);
        }
    }
}
