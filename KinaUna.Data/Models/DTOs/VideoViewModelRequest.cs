using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class VideoViewModelRequest
    {
        public List<int> Progenies { get; set; } = [];

        public int VideoId { get; set; } = 0;

        public int SortOrder { get; set; } = 0;

        public string TagFilter { get; set; } = "";

        public string TimeZone { get; set; } = Constants.DefaultTimezone;
    }
}
