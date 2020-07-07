using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
	public interface IIdentityServerClient
	{
		Task<string> RequestClientCredentialsTokenAsync();
	}
}
