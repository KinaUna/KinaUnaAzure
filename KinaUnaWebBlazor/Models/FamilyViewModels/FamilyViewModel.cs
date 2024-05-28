using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.FamilyViewModels
{
    public class FamilyViewModel: BaseViewModel
    {
        public Family Family { get; set; } = new();
    }
}
