using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class PicturesListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public List<Picture> PicturesList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
