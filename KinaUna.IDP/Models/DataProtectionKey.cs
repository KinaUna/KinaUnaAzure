using System.ComponentModel.DataAnnotations;

namespace KinaUna.IDP.Models
{
    public class DataProtectionKey
    {
        [Key]
        public string FriendlyName { get; set; }
        public string XmlData { get; set; }
    }
}
