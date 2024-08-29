namespace KinaUna.Data.Models
{
    /// <summary>
    /// Storage container names for files.
    /// Files are saved in containers corresponding to the type of Entity they are associated with.
    /// </summary>
    public static class BlobContainers
    {
        /// <summary>
        /// Container for Progeny Pictures.
        /// </summary>
        public const string Pictures = "pictures";
        /// <summary>
        /// Container for Progeny profile pictures.
        /// </summary>
        public const string Progeny = "progeny";
        /// <summary>
        /// Container for User profile pictures.
        /// </summary>
        public const string Profiles = "profiles";
        /// <summary>
        /// Container for Friends profile pictures.
        /// </summary>
        public const string Friends = "friends";
        /// <summary>
        /// Container for Contacts profile pictures.
        /// </summary>
        public const string Contacts = "contacts";
        /// <summary>
        /// Container for Notes attachments and embedded images.
        /// </summary>
        public const string Notes = "notes";
        /// <summary>
        /// Container for documents.
        /// </summary>
        public const string Documents = "documents";
        /// <summary>
        /// Container for attachments and embedded images in KinaUnaTexts.
        /// </summary>
        public const string KinaUnaTexts = "kinaunatexts";
    }
}
