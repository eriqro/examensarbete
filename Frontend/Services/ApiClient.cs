using System;
using System.Net.Http;

namespace Tune.Frontend.Services
{
    public static class ApiClient
    {
        private static HttpClient? _client;

        public static HttpClient Client
        {
            get
            {
                if (_client == null)
                {
                    _client = new HttpClient();
                }
                return _client;
            }
        }

        public static void SetBaseUrl(string baseUrl)
        {
            // Don't overwrite the URL if it's already set
            if (_client == null)
            {
                _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
            }
            else if (_client.BaseAddress == null)
            {
                _client.BaseAddress = new Uri(baseUrl);
            }
            // Can't change base address once it's set
        }

        public static void SetToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                Client.DefaultRequestHeaders.Authorization = null;
            else
                Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
}
