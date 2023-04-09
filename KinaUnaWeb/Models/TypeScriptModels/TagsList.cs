using System.Collections.Generic;

namespace KinaUnaWeb.Models.TypeScriptModels
{
    public class TagsList
    {
        public int ProgenyId { get; set; } = 0;
        public List<string> Tags { get; set; } = new List<string>();
    }
}
