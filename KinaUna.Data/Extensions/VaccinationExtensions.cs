using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    public static class VaccinationExtensions
    {
        public static void CopyPropertiesForUpdate(this Vaccination currentVaccinationItem, Vaccination otherVaccinationItem)
        {
            currentVaccinationItem.AccessLevel = otherVaccinationItem.AccessLevel;
            currentVaccinationItem.ProgenyId = otherVaccinationItem.ProgenyId;
            currentVaccinationItem.Notes = otherVaccinationItem.Notes;
            currentVaccinationItem.VaccinationDate = otherVaccinationItem.VaccinationDate;
            currentVaccinationItem.VaccinationDescription = otherVaccinationItem.VaccinationDescription;
            currentVaccinationItem.VaccinationName = otherVaccinationItem.VaccinationName;

        }

        public static void CopyPropertiesForAdd(this Vaccination currentVaccinationItem, Vaccination otherVaccinationItem)
        {
            currentVaccinationItem.AccessLevel = otherVaccinationItem.AccessLevel;
            currentVaccinationItem.Author = otherVaccinationItem.Author;
            currentVaccinationItem.Notes = otherVaccinationItem.Notes;
            currentVaccinationItem.VaccinationDate = otherVaccinationItem.VaccinationDate;
            currentVaccinationItem.ProgenyId = otherVaccinationItem.ProgenyId;
            currentVaccinationItem.VaccinationDescription = otherVaccinationItem.VaccinationDescription;
            currentVaccinationItem.VaccinationName = otherVaccinationItem.VaccinationName;
        }
    }
}
