using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUnaWeb.Models;

namespace KinaUnaWeb.Services
{
    public interface IProgenyManager
    {
        Task<UserInfo> GetInfo(string userEmail);
    }
}
