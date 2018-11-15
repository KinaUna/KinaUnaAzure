using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUna.IDP.Models
{
    public class ChangeEmailViewModel
    {
        public string OldEmail { get; set; }
        public string NewEmail { get; set; }
        public string UserId { get; set; }
        public string ErrorMessage { get; set; }
    }
}
