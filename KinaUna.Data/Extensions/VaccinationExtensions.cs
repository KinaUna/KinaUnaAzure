using System;
using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the Vaccination class.
    /// </summary>
    public static class VaccinationExtensions
    {
        /// <summary>
        /// Copies the properties needed for updating a Vaccination entity from one Vaccination object to another.
        /// </summary>
        /// <param name="currentVaccinationItem"></param>
        /// <param name="otherVaccinationItem"></param>
        public static void CopyPropertiesForUpdate(this Vaccination currentVaccinationItem, Vaccination otherVaccinationItem)
        {
            currentVaccinationItem.AccessLevel = otherVaccinationItem.AccessLevel;
            currentVaccinationItem.ProgenyId = otherVaccinationItem.ProgenyId;
            currentVaccinationItem.Notes = otherVaccinationItem.Notes;
            currentVaccinationItem.VaccinationDate = otherVaccinationItem.VaccinationDate;
            currentVaccinationItem.VaccinationDescription = otherVaccinationItem.VaccinationDescription;
            currentVaccinationItem.VaccinationName = otherVaccinationItem.VaccinationName;
            currentVaccinationItem.ModifiedBy = otherVaccinationItem.ModifiedBy;
            currentVaccinationItem.ModifiedTime = DateTime.UtcNow;

        }

        /// <summary>
        /// Copies the properties needed for adding a Vaccination entity from one Vaccination object to another.
        /// </summary>
        /// <param name="currentVaccinationItem"></param>
        /// <param name="otherVaccinationItem"></param>
        public static void CopyPropertiesForAdd(this Vaccination currentVaccinationItem, Vaccination otherVaccinationItem)
        {
            currentVaccinationItem.AccessLevel = otherVaccinationItem.AccessLevel;
            currentVaccinationItem.Author = otherVaccinationItem.Author;
            currentVaccinationItem.CreatedBy = otherVaccinationItem.CreatedBy;
            currentVaccinationItem.CreatedTime = DateTime.UtcNow;
            currentVaccinationItem.ModifiedBy = otherVaccinationItem.CreatedBy;
            currentVaccinationItem.ModifiedTime = DateTime.UtcNow;
            currentVaccinationItem.Notes = otherVaccinationItem.Notes;
            currentVaccinationItem.VaccinationDate = otherVaccinationItem.VaccinationDate;
            currentVaccinationItem.ProgenyId = otherVaccinationItem.ProgenyId;
            currentVaccinationItem.VaccinationDescription = otherVaccinationItem.VaccinationDescription;
            currentVaccinationItem.VaccinationName = otherVaccinationItem.VaccinationName;
        }
    }
}
