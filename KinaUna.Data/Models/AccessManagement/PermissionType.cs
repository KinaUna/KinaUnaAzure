namespace KinaUna.Data.Models.AccessManagement
{
    /// <summary>
    /// Specifies the types of permissions that can be assigned within the system.
    /// </summary>
    /// <remarks>This enumeration defines the various scopes or entities to which permissions can apply.  It
    /// is used to categorize and manage access control for different aspects of the application.</remarks>
    public enum PermissionType
    {
        TimelineItem = 0,
        Progeny = 1,
        Family = 2,
    }
}
