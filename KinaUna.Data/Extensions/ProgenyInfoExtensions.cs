using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    public static class ProgenyInfoExtensions
    {
        public static void CopyPropertiesForUpdate(this ProgenyInfo progenyInfo, ProgenyInfo otherProgenyInfo)
        {
            if (progenyInfo == null || otherProgenyInfo == null)
            {
                return;
            }

            progenyInfo.Email = otherProgenyInfo.Email;
            progenyInfo.MobileNumber = otherProgenyInfo.MobileNumber;
            progenyInfo.Notes = otherProgenyInfo.Notes;
            progenyInfo.Website = otherProgenyInfo.Website;
        }

        public static bool PropertiesChanged(this ProgenyInfo progenyInfo, ProgenyInfo otherProgenyInfo)
        {
            if (progenyInfo == null || otherProgenyInfo == null)
            {
                return false;
            }
            if (progenyInfo.AddressIdNumber != otherProgenyInfo.AddressIdNumber)
            {
                return true;
            }

            if (progenyInfo.Address != null && progenyInfo.Address.PropertiesChanged(otherProgenyInfo.Address))
            {
                return true;
            }

            return progenyInfo.Email != otherProgenyInfo.Email ||
                   progenyInfo.MobileNumber != otherProgenyInfo.MobileNumber ||
                   progenyInfo.Website != otherProgenyInfo.Website ||
                   progenyInfo.Notes != otherProgenyInfo.Notes;
        }
    }
}
