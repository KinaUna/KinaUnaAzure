using System;
using System.Collections.Generic;

namespace KinaUna.Data.Models.CacheManagement
{
    public class VideosListCacheEntry
    {
        public string UserId { get; set; }
        public int ProgenyId { get; set; }
        public List<Video> VideosList { get; set; } = [];
        public DateTime UpdateTime { get; set; }
    }
}
