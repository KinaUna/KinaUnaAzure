using System.Security.Principal;

namespace KinaUnaWeb.Services
{
    public interface IIdentityParser<T>
    {
        T Parse(IPrincipal principal);
    }
}
