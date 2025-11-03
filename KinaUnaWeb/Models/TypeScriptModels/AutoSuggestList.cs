using System.Collections.Generic;

namespace KinaUnaWeb.Models.TypeScriptModels
{
    public class AutoSuggestList
    {
        public List<int> Progenies { get; set; } = [];
        public List<int> Families { get; set; } = [];
        public List<string> Suggestions { get; set; } = [];
    }
}
