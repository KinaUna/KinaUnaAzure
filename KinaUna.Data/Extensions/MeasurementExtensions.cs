using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the Measurement class.
    /// </summary>
    public static class MeasurementExtensions
    {
        /// <summary>
        /// Copies the properties needed for updating a Measurement entity from one Measurement object to another.
        /// </summary>
        /// <param name="currentMeasurement"></param>
        /// <param name="otherMeasurement"></param>
        public static void CopyPropertiesForUpdate(this Measurement currentMeasurement, Measurement otherMeasurement )
        {
            currentMeasurement.AccessLevel = otherMeasurement.AccessLevel;
            currentMeasurement.Circumference = otherMeasurement.Circumference;
            currentMeasurement.Date = otherMeasurement.Date;
            currentMeasurement.EyeColor = otherMeasurement.EyeColor;
            currentMeasurement.HairColor = otherMeasurement.HairColor;
            currentMeasurement.Height = otherMeasurement.Height;
            currentMeasurement.MeasurementNumber = otherMeasurement.MeasurementNumber;
            currentMeasurement.Weight = otherMeasurement.Weight;
        }

        /// <summary>
        /// Copies the properties needed for adding a Measurement entity from one Measurement object to another.
        /// </summary>
        /// <param name="currentMeasurement"></param>
        /// <param name="otherMeasurement"></param>
        public static void CopyPropertiesForAdd(this Measurement currentMeasurement, Measurement otherMeasurement)
        {
            currentMeasurement.ProgenyId = otherMeasurement.ProgenyId;
            currentMeasurement.CreatedDate = DateTime.UtcNow;
            currentMeasurement.Date = otherMeasurement.Date;
            currentMeasurement.Height = otherMeasurement.Height;
            currentMeasurement.Weight = otherMeasurement.Weight;
            currentMeasurement.Circumference = otherMeasurement.Circumference;
            currentMeasurement.HairColor = otherMeasurement.HairColor;
            currentMeasurement.EyeColor = otherMeasurement.EyeColor;
            currentMeasurement.AccessLevel = otherMeasurement.AccessLevel;
            currentMeasurement.Author = otherMeasurement.Author;
        }
    }
}
