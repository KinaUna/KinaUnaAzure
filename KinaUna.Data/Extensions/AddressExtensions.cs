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
            currentAddress.AddressLine1 = otherAddress.AddressLine1 ?? string.Empty;
            currentAddress.AddressLine2 = otherAddress.AddressLine2 ?? string.Empty;
            currentAddress.City = otherAddress.City ?? string.Empty;
            currentAddress.PostalCode = otherAddress.PostalCode ?? string.Empty;
            currentAddress.State = otherAddress.State ?? string.Empty;
            currentAddress.Country = otherAddress.Country ?? string.Empty;
        }

        /// <summary>
        /// Copies the properties needed for adding an Address entity from one Address object to another.
        /// </summary>
        /// <param name="currentAddress"></param>
        /// <param name="otherAddress"></param>
        public static void CopyPropertiesForAdd(this Address currentAddress, Address otherAddress)
        {
            currentAddress.AddressLine1 = otherAddress.AddressLine1 ?? string.Empty;
            currentAddress.AddressLine2 = otherAddress.AddressLine2 ?? string.Empty;
            currentAddress.City = otherAddress.City ?? string.Empty;
            currentAddress.PostalCode = otherAddress.PostalCode ?? string.Empty;
            currentAddress.State = otherAddress.State ?? string.Empty;
            currentAddress.Country = otherAddress.Country ?? string.Empty;
            
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

        public static bool PropertiesChanged(this Address address, Address otherAddress)
        {
            if (address == null || otherAddress == null)
            {
                return false;
            }
            return address.AddressLine1 != otherAddress.AddressLine1 ||
                   address.AddressLine2 != otherAddress.AddressLine2 ||
                   address.City != otherAddress.City ||
                   address.State != otherAddress.State ||
                   address.PostalCode != otherAddress.PostalCode ||
                   address.Country != otherAddress.Country;
        }
    }
}
