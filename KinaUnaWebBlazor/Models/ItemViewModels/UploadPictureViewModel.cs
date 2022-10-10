using System.ComponentModel.DataAnnotations;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class UploadPictureViewModel: BaseViewModel
    {
        [Required] public List<IFormFile> Files { get; set; } = new List<IFormFile>();
        [Required] public int ProgenyId { get; set; } = 0;
        public List<string> FileNames { get; set; } = new List<string>();
        public List<string> FileLinks { get; set; } = new List<string>();
        [Required] public int AccessLevel { get; set; } = 5;
        public string Author { get; set; } = "";
        public string Owners { get; set; } = "";
        public string Tags { get; set; } = "";
        public double Longitude1 { get; set; } = 0;
        public double Latitude1 { get; set; } = 0;
        public string Altitude { get; set; } = "";
        public string Longitude { get; set; } = "";
        public string Latitude { get; set; } = "";
        public string Location { get; set; } = "";
    }
}
