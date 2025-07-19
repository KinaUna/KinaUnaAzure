using Microsoft.Extensions.DependencyInjection;

namespace KinaUnaWeb.HostingExtensions.Interfaces
{
    public interface IOpenIddictConfigurator
    {
        void ConfigureServices(IServiceCollection services);
    }
}