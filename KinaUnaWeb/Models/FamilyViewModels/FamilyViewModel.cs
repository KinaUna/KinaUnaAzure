namespace KinaUnaWeb.Models.FamilyViewModels
{
    public class FamilyViewModel: BaseItemsViewModel
    {
        public Family Family { get; init; }

        public FamilyViewModel(BaseItemsViewModel baseItemsViewModel)
        {
            SetBaseProperties(baseItemsViewModel);
        }

    }
}
