using System.Collections.Generic;
using KinaUna.Data;

namespace KinaUnaWeb.Models.TypeScriptModels
{
    public class AutoSuggestList
    {
        public List<int> Progenies { get; set; } = [Constants.DefaultChildId];
        public List<string> Suggestions { get; set; } = [];
    }
}
