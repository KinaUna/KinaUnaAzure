namespace KinaUna.Data.Models.AccessManagement
{
    /// <summary>
    /// Specifies the levels of permissions available for a user or role.
    /// </summary>
    /// <remarks>The <see cref="PermissionLevel"/> enumeration defines distinct levels of access, ranging from
    /// basic viewing rights to full administrative control. These levels can be used to manage and  enforce access
    /// control in applications.</remarks>
    public enum PermissionLevel
    {
        None = 0,
        View = 1,
        Add = 2, // For adding Timeline items only, not families or progenies, users should be able to add new items to share items with others.
        Edit = 3,
        Admin = 4,
        CreatorOnly = 10, // For Timeline items only, not families or progenies, for draft items that only the creator should be able to see.
        Private = 20 // For Timeline items only, not families or progenies, users should be able to set items as private (only visible to themselves).
    }
}
