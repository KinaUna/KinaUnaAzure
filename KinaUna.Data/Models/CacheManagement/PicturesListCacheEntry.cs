using System;

namespace KinaUna.Data.Models.CacheManagement
{
    public class PicturesListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public Picture[] PicturesList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
