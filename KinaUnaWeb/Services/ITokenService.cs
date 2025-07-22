using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    public interface ITokenService
    {
        Task<KinaunaTokenResponse> RedeemAuthorizationCodeAsync(string code, string redirectUri);
        Task<KinaunaTokenResponse> GetClientCredentialsTokenAsync();
    }
}
