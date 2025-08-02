using KinaUna.Data.Models;

namespace KinaUna.OpenIddict.Services
{
    public interface ITokenService
    {
        Task<TokenInfo> GetValidTokenAsync();
    }
}
