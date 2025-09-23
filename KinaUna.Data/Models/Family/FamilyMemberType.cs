namespace KinaUna.Data.Models.Family
{
    /// <summary>
    /// Specifies the type of family member.
    /// </summary>
    /// <remarks>This enumeration is used to categorize members of a family into distinct roles, such as
    /// parent, child, or pet. The <see cref="Unknown"/> value represents an unspecified or unrecognized family member
    /// type.</remarks>
    public enum FamilyMemberType
    {
        Unknown = 0,
        Parent = 1,
        Child = 2,
        Pet = 3,
    }
}
