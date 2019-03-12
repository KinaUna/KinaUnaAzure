using Newtonsoft.Json;

namespace KinaUna.Data.Models
{
    public class PushNotification
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Link { get; set; }
        public string Tag { get; set; }

        [JsonIgnore]
        public string UserId { get; set; }
        
    }
}
