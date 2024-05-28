using Newtonsoft.Json;

namespace KinaUna.Data.Models
{
    public class PushNotification
    {
        public string Title { get; set; }
        public string Message { get; init; }
        public string Link { get; init; }
        public string Tag { get; init; }

        [JsonIgnore]
        public string UserId { get; set; }
        
    }
}
