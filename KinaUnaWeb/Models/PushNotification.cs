using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KinaUnaWeb.Models
{
    public class PushNotification
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Link { get; set; }

        [JsonIgnore]
        public string UserId { get; set; }
    }
}
