﻿using IdentityModel.Client;

namespace KinaUnaWebBlazor.Services
{
	public class IdentityServerClient : IIdentityServerClient
	{
		private readonly HttpClient _httpClient;
		private readonly ClientCredentialsTokenRequest _tokenRequest;
		private readonly ILogger<IdentityServerClient> _logger;

		public IdentityServerClient(HttpClient httpClient, ClientCredentialsTokenRequest tokenRequest, ILogger<IdentityServerClient> logger)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.DefaultRequestVersion = new Version(2, 0);
			_tokenRequest = tokenRequest ?? throw new ArgumentNullException(nameof(tokenRequest));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2254:Template should be a static expression", Justification = "<Pending>")]
        public async Task<string> RequestClientCredentialsTokenAsync()
		{
			// request the access token token
			TokenResponse tokenResponse = await _httpClient.RequestClientCredentialsTokenAsync(_tokenRequest);
			if (tokenResponse.IsError)
			{
				_logger.LogError(tokenResponse.Error);
				throw new HttpRequestException("Something went wrong while requesting the access token");
			}
			return tokenResponse.AccessToken;
		}
	}
}
