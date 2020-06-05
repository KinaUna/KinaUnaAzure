using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
	public interface IIdentityServerClient
	{
		Task<string> RequestClientCredentialsTokenAsync();
	}
}
