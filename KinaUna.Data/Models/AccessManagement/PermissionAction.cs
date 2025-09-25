namespace KinaUna.Data.Models.AccessManagement
{
    /// <summary>
    /// Specifies the type of action to be performed in the context of a permission operation.
    /// </summary>
    /// <remarks>This enumeration is typically used to indicate the desired operation, such as adding,
    /// updating, or deleting permissions. The <see cref="Unknown"/> value represents an undefined or uninitialized
    /// state.</remarks>
    public enum PermissionAction
    {
        Unknown = 0,
        Add = 1,
        Update = 2,
        Delete = 3 
    }
}
