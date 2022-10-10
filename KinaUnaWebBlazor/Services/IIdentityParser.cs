using System.Security.Principal;

namespace KinaUnaWebBlazor.Services
{
    public interface IIdentityParser<T>
    {
        T Parse(IPrincipal principal);
    }
}
