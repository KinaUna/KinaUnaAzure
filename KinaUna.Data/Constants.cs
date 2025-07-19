namespace KinaUna.Data
{
    /// <summary>
    /// Solution wide constants.
    /// </summary>
    public static class Constants
    {
        public const string ApiVersion = "1.1.10";
        public const bool ResetIdentityDb = true; // If set to true, the configuration in KinUna.IDP / Config.cs will be reapplied.
        public const string AppName = "KinaUna";
        public const string WebAppUrl = "https://web.kinauna.com";
        public const string AuthAppUrl = "https://auth.kinauna.com";
        public const string ProgenyApiUrl = "https://progenyapi.kinauna.com";
        public const string MediaApiUrl = "https://mediaapi.kinauna.com";
        public const string SupportUrl = "https://web.kinauna.com/support";
        public const string AppRootDomain = "kinauna.com";
        public const string SupportEmail = "support@kinauna.com";
        public const string KeyVaultEndPoint = "https://kinauna.vault.azure.net";
        public const string LanguageCookieName = "KinaUnaLanguage";
        public const string ProgenyApiName = "kinaunaprogenyapi";
        public const string MediaApiName = "kinaunamediaapi";
        public const string DefaultTimezone = "Romance Standard Time";
        public const int DefaultChildId = 2;
        public const string DefaultUserEmail = "testuser@niviaq.com";
        public const string DefaultUserId = "dc72bb31-e26f-410c-922d-09f25bc4992e";
        public const string ProfilePictureUrl = "https://web.kinauna.com/photodb/profile.jpg";
        public const string DefaultPictureLink = "defaultpicture.jpg";
        public const string KeepExistingLink = "[KeepExistingLink]";
        public const int DefaultUpcomingCalendarItemsCount = 8;
        public const string PlaceholderImageLink = "ab5fe7cb-2a66-4785-b39a-aa4eb7953c3d.png";
        public const string SystemAccountEmail = "system@kinauna.com";
        public static readonly string[] ProductionCorsList = ["https://web.kinauna.com", "https://auth.kinauna.com", "https://progenyapi.kinauna.com", "https://mediaapi.kinauna.com"];
        public static readonly string[] DevelopmentCorsList = ["https://localhost:44397", "https://localhost:44376", "https://localhost:44324"];
    }

    /// <summary>
    /// Page names, for TextTranslations and KinaUnaTexts.
    /// </summary>
    public static class PageNames
    {
        public const string Layout = "Layout";
        public const string Home = "Home";
        public const string TextEditor = "TextEditor";
        public const string AccessManagement = "AccessManagement";
        public const string Account = "Account";
        public const string AddItem = "AddItem";
        public const string Contacts = "Contacts";
        public const string Calendar = "Calendar";
        public const string CalendarTools = "CalendarTools";
        public const string Scheduler = "Scheduler";
        public const string SchedulerRecurrence = "SchedulerRecurrence";
        public const string Friends = "Friends";
        public const string Locations = "Locations";
        public const string Measurements = "Measurements";
        public const string Notes = "Notes";
        public const string Pictures = "Pictures";
        public const string Skills = "Skills";
        public const string Sleep = "Sleep";
        public const string Vaccinations = "Vaccinations";
        public const string Videos = "Videos";
        public const string Vocabulary = "Vocabulary";
        public const string Family = "Family";
        public const string Timeline = "Timeline";
        public const string Progeny = "Progeny";
        public const string Notifications = "Notifications";
    }
}
