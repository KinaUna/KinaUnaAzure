namespace KinaUna.OpenIddict.Services.Interfaces
{
    public interface IOpenIddictSeedService
    {
        Task SeedAsync(CancellationToken cancellationToken = default);
    }
}