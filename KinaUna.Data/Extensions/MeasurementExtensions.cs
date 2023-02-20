using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    public static class MeasurementExtensions
    {
        public static void CopyPropertiesForUpdate(this Measurement currentMeasurement, Measurement otherMeasurement )
        {
            
        }

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
