namespace KinaUna.Data.Models.ItemInterfaces
{
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
