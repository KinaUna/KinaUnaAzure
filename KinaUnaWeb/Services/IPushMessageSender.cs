using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services
{
    public interface IPushMessageSender
    {
        Task SendMessage(string user, string title, string message, string link);
    }
}
