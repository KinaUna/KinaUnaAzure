namespace KinaUna.Data.Models.ItemInterfaces
{
    /// <summary>
    /// Represents an object that can provide its location as a string.
    /// </summary>
    /// <remarks>This interface defines a contract for objects that can return their location in string
    /// format. It avoids using a property named "Location" to prevent conflicts with the <see
    /// cref="System.Drawing.Point"/>  or other similarly named types.</remarks>
    public interface ILocatable
    {
        /// <summary>
        /// Gets the location string of the object.
        /// Using a method and not using the Location property, as that causes a conflict with the Location class.
        /// </summary>
        /// <returns></returns>
        public string GetLocationString();
    }
}
