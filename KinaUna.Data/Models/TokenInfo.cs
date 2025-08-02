using System;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Represents information about authentication tokens, including access and refresh tokens and their expiration.
    /// </summary>
    public class TokenInfo
    {
        /// <summary>
        /// The type of the token (e.g., "Bearer").
        /// </summary>
        public string TokenType { get; set; }

        /// <summary>
        /// The access token used for authentication.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// The refresh token used to obtain new access tokens.
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// The date and time when the access token expires.
        /// </summary>
        public DateTime AccessTokenExpiresAt { get; set; }
    }
}
