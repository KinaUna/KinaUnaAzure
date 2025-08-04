namespace KinaUna.Data.Models.ItemInterfaces
{
    /// <summary>
    /// Represents an entity that can be associated with tags.
    /// </summary>
    /// <remarks>Tags are typically used to categorize or label the implementing entity.  The format and usage
    /// of the tags are determined by the implementation.</remarks>
    public interface ITaggable
    {
        /// <summary>
        /// Gets or sets a comma-separated list of tags associated with the entity.
        /// </summary>
        public string Tags { get; set; }
    }
}
