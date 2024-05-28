using System.Threading.Tasks;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace KinaUna.IDP.Services
{
    public class EfLoginService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : ILoginService<ApplicationUser>
    {
        public async Task<ApplicationUser> FindByUsername(string user)
        {
            ApplicationUser appUser = await userManager.FindByEmailAsync(user);

            if (appUser == null) return null;

            if (!string.IsNullOrEmpty(appUser.UserName)) return appUser;

            appUser.UserName = appUser.Email;
            await userManager.UpdateAsync(appUser);


            return appUser;
        }

        public async Task<bool> ValidateCredentials(ApplicationUser user, string password)
        {
            return await userManager.CheckPasswordAsync(user, password);
        }

        public Task SignIn(ApplicationUser user)
        {
            return signInManager.SignInAsync(user, true);
        }
    }
}
