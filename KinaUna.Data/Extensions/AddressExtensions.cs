using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    public static class AddressExtensions
    {
        public static void CopyPropertiesForUpdate(this Address currentAddress, Address otherAddress )
        {
            currentAddress.AddressId = otherAddress.AddressId;
            currentAddress.AddressLine1 = otherAddress.AddressLine1;
            currentAddress.AddressLine2 = otherAddress.AddressLine2;
            currentAddress.City = otherAddress.City;
            currentAddress.PostalCode = otherAddress.PostalCode;
            currentAddress.State = otherAddress.State;
            currentAddress.Country = otherAddress.Country;
        }

        public static void CopyPropertiesForAdd(this Address currentAddress, Address otherAddress)
        {
            currentAddress.AddressLine1 = otherAddress.AddressLine1;
            currentAddress.AddressLine2 = otherAddress.AddressLine2;
            currentAddress.City = otherAddress.City;
            currentAddress.PostalCode = otherAddress.PostalCode;
            currentAddress.State = otherAddress.State;
            currentAddress.Country = otherAddress.Country;
            
        }

        public static bool HasValues(this Address currentAddress)
        {
            if (currentAddress.AddressLine1 + currentAddress.AddressLine2 + currentAddress.City + currentAddress.Country + currentAddress.PostalCode + currentAddress.State != "")
            {
                return true;
            }

            return false;
        }
    }
}
