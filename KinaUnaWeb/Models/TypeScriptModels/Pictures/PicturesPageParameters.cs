namespace KinaUnaWeb.Models.TypeScriptModels.Pictures
{
    public class PicturesPageParameters: BasePageParameters
    {
        public int Year { get; set; } = 0;
        public int Month { get; set; } = 0;
        public int Day { get; set; } = 0;
        public int FirstItemYear { get; set; } = 0;
        public int SortTags { get; set; } = 0;
    }
}
