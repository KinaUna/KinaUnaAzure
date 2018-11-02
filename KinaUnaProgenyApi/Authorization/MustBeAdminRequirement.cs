using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace KinaUnaProgenyApi.Authorization
{
    public class MustBeAdminRequirement : IAuthorizationRequirement
    {
        public MustBeAdminRequirement()
        {
        }
    }
}
