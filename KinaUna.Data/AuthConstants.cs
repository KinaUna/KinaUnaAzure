using System.Collections.Generic;

namespace KinaUna.Data
{
    public static class AuthConstants
    {
        // Authentication Server Constants
        public const string AuthenticationServerUrlKey = "AuthServerUrl";
        public const string AuthenticationServerClientIdKey = "AuthServerClientId";
        public const string AuthApiOnlyClientIdKey = "AuthApiOnlyClientId";
        public const string AuthApiClientIdKey = "AuthApiClientId";
        public const string AuthServerUrl = "AuthServerUrl";
        public const string AuthServerClientSecretKey = "AuthClientSecret";

        public const string ResetOpenIddictDatabaseKey = "ResetOpenIddictDatabase";

        // Web App Constants
        public const string WebServerUrlKey = "WebServerUrl";
        public const string WebServerClientIdKey = "WebServerClientId";
        public const string WebServerApiClientIdKey = "WebServerApiClientId";
        public const string WebServerClientSecretKey = "WebServerClientSecret";

        // Progeny API Constants
        public const string ProgenyApiUrlKey = "ProgenyApiUrl";
        public const string ProgenyApiClientIdKey = "ProgenyApiClientId";
        public const string ProgenyApiClientSecretKey = "ProgenyApiClientSecret";

        // API Names
        public const string AuthApiName = "kinaunaauthapi";
        public const string ProgenyApiName = "kinaunaprogenyapi";

        public static readonly List<string> AllowedClients =
        [
            "kinaunawebclient",
            "kinaunaauthclient",
            "kinaunaauthapiclient",
            "kinaunaauthapionlyclient",
            "kinaunawebapiclient",
            "kinaunaprogenyapiclient"
        ];

        public static readonly List<string> AllowedApiOnlyClients =
        [
            "kinaunaauthapiclient",
            "kinaunaauthapionlyclient",
            "kinaunawebapiclient",
            "kinaunaprogenyapiclient"
        ];
    }
}
