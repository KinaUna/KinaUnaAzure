using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class NearByPhotosResponse
    {
        public int ProgenyId { get; set; } = 0;
        public Location LocationItem { get; set; } = new Location();
        public List<Picture> PicturesList { get; set; }
        public int NumberOfPictures { get; set; }
    }
}
