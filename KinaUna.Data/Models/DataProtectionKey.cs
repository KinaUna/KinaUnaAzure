using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    public class DataProtectionKey
    {
        [Key]
        public string FriendlyName { get; set; }
        public string XmlData { get; set; }
    }
}
