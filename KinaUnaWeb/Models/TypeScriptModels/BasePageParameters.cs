namespace KinaUnaWeb.Models.TypeScriptModels
{
    public class BasePageParameters
    {
        public int ProgenyId { get; set; }
        public int LanguageId { get; set; }
        public int CurrentPageNumber { get; set; }
        public int ItemsPerPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int Sort { get; set; }
        public string TagFilter { get; set; }
    }
}
