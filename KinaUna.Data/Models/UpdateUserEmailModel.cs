namespace KinaUna.Data.Models
{
    /// <summary>
    /// Model for updating a user's email address.
    /// Contains the user ID, the new email address, and the old email address.
    /// </summary>
    public class UpdateUserEmailModel
    {
        /// <summary>
        /// The unique identifier of the user whose email is being updated.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The new email address to be set for the user.
        /// </summary>
        public string NewEmail { get; set; }
        /// <summary>
        /// The user's previous email address.
        /// </summary>
        public string OldEmail { get; set; }
    }
}
