namespace KinaUnaWebBlazor.Services
{
	public interface IIdentityServerClient
	{
		Task<string> RequestClientCredentialsTokenAsync();
	}
}
