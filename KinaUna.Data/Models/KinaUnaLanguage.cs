using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Entity Framework Entity for language data.
    /// </summary>
    public class KinaUnaLanguage
    {
        public int Id { get; set; }

        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(256)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(1024)]
        public string Icon { get; set; } = string.Empty;
        // Downloaded from here : https://www.countryflags.com/en/icons-overview/

        [NotMapped] public string IconLink { get; set; } = string.Empty;
    }
}
