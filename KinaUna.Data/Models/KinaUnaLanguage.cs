using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    public class KinaUnaLanguage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;
        // Downloaded from here : https://www.countryflags.com/en/icons-overview/

        [NotMapped] public string IconLink { get; set; } = string.Empty;
    }
}
