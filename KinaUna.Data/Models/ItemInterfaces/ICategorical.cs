namespace KinaUna.Data.Models.ItemInterfaces
{
    /// <summary>
    /// Represents an entity that can be categorized by a specific category.
    /// </summary>
    /// <remarks>This interface defines a contract for objects that have a category, allowing them to be
    /// grouped or classified. Implementing types should provide a meaningful value for the <see cref="Category"/>
    /// property.</remarks>
    public interface ICategorical
    {
        /// <summary>
        /// Gets or sets the category associated with the item.
        /// </summary>
        string Category { get; set; }
    }
}
