using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the Address class.
    /// </summary>
    public static class AddressExtensions
    {
        /// <summary>
        /// Copies the properties needed for updating an Address entity from one Address object to another.
        /// </summary>
        /// <param name="currentAddress"></param>
        /// <param name="otherAddress"></param>
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

        /// <summary>
        /// Copies the properties needed for adding an Address entity from one Address object to another.
        /// </summary>
        /// <param name="currentAddress"></param>
        /// <param name="otherAddress"></param>
        public static void CopyPropertiesForAdd(this Address currentAddress, Address otherAddress)
        {
            currentAddress.AddressLine1 = otherAddress.AddressLine1;
            currentAddress.AddressLine2 = otherAddress.AddressLine2;
            currentAddress.City = otherAddress.City;
            currentAddress.PostalCode = otherAddress.PostalCode;
            currentAddress.State = otherAddress.State;
            currentAddress.Country = otherAddress.Country;
            
        }

        /// <summary>
        /// Checks if an Address object has any values relevant for displaying it.
        /// </summary>
        /// <param name="currentAddress"></param>
        /// <returns>True if any of the relevant properties isn't an empty string, otherwise false.</returns>
        public static bool HasValues(this Address currentAddress)
        {
            if (currentAddress == null)
            {
                return false;
            }

            return currentAddress.AddressLine1 + currentAddress.AddressLine2 + currentAddress.City + currentAddress.Country + currentAddress.PostalCode + currentAddress.State != "";
        }
    }
}
