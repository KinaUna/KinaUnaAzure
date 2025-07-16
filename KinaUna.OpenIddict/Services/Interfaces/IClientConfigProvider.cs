namespace KinaUna.OpenIddict.Services.Interfaces
{
    public interface IClientConfigProvider
    {
        IEnumerable<ClientConfig> GetClientConfigs();
    }
    
    public class ClientConfig
    {
        public required string ClientId { get; init; }
        public required string DisplayName { get; init; }
        public required string BaseUrl { get; init; }
        public required string Secret { get; set; }
        public required string ConsentType { get; init; }
    }
}