using System.Collections.Generic;

namespace KinaUnaWeb.Models.TypeScriptModels
{
    public class AutoSuggestList
    {
        public int ProgenyId { get; set; } = 0;
        public List<string> Suggestions { get; set; } = new List<string>();
    }
}
