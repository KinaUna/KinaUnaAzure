using OpenIddict.Abstractions;

namespace KinaUna.OpenIddict.Services.Interfaces
{
    public interface IClientConfigProvider
    {
        IEnumerable<OpenIddictApplicationDescriptor> GetWebClientConfigs();
        IEnumerable<OpenIddictApplicationDescriptor> GetApiClientConfigs();
    }
    
    public class ClientConfig
    {
        public required string ClientId { get; init; }
        public required string DisplayName { get; init; }
        public required string BaseUrl { get; init; }
        public required string Secret { get; init; }
        public required string ConsentType { get; init; }
        public required HashSet<string> Permissions { get; init; }
    }
}