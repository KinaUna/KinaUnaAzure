namespace KinaUna.Data.Models.ItemInterfaces
{
    /// <summary>
    /// Represents an object that is associated with a specific context.
    /// </summary>
    /// <remarks>The <see cref="Context"/> property can be used to store or retrieve a string that identifies
    /// the context associated with the implementing object. This can be useful for scenarios where objects need to be
    /// categorized, grouped, or otherwise associated with a specific environment or state.</remarks>
    public interface IContexted
    {
        /// <summary>
        /// Gets or sets the context information associated with the current operation.
        /// </summary>
        public string Context { get; set; }
    }
}
