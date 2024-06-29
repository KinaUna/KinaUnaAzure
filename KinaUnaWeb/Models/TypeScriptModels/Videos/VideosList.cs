using KinaUna.Data.Models;
using System.Collections.Generic;

namespace KinaUnaWeb.Models.TypeScriptModels.Videos
{
    public class VideosList
    {
        public List<Video> VideoItems { get; set; } = [];
        public int AllItemsCount { get; set; } = 0;
        public int RemainingItemsCount { get; set; } = 0;
        public int FirstItemYear { get; set; } = 0;
        public int TotalPages { get; set; }
        public int CurrentPageNumber { get; set; }
        public List<string> TagsList { get; set; } = [];
    }
}
