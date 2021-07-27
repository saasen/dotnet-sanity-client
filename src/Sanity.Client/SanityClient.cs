﻿using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Sanity.Client
{
    interface ISanityCdnClient
    {
        Task<SanityResponse<T>> Query<T>(string query, CancellationToken cancellationToken);
        Task<HttpResponseMessage> Query(string query, CancellationToken cancellationToken);
        //Task<T> GetDocument<T>(string query);
    }

    interface ISanityClient : ISanityCdnClient
    {
    }

    public class SanityClient : ISanityClient
    {
        private readonly HttpClient _httpClient;

        // To accept a httpclient or not to, or just httphandler
        // It's nice to let the user pass httpclient so they can control how it is instantiated
        //  and how the http handler should act (retries, exponential backoff etc.). Or maybe we should control that?
        public SanityClient(HttpClient httpClient, SanityClientOptions options = null)
        {
            Guard.AgainstNull(options, nameof(options));
            Guard.AgainstNull(options.ProjectId, nameof(options.ProjectId));
            Guard.AgainstNull(options.Dataset, nameof(options.Dataset));
            Guard.AgainstNull(options.ApiVersion, nameof(options.ApiVersion));

            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri($"https://{options.ProjectId}.api.sanity.io/{options.ApiVersion}/");
        }

        public async Task<SanityResponse<T>> Query<T>(string query, CancellationToken cancellationToken = default)
        {
            var response = await Query(query, cancellationToken);
            response.EnsureSuccessStatusCode();

            var typedContent = await JsonSerializer.DeserializeAsync<SanityResponse<T>>(await response.Content.ReadAsStreamAsync(cancellationToken), new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }, cancellationToken: cancellationToken);
            return typedContent;
        }
        
        public async Task<HttpResponseMessage> Query(string query, CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetAsync($"data/query/production?query={Uri.EscapeDataString(query)}", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
    }

    public class SanityClientOptions
    {
        public string ProjectId { get; set; }
        public string Dataset { get; set; }
        public string ApiVersion { get; set; }
        //public string Token { get; set; }
    }

    internal static class Guard
    {
        public static void AgainstNull(string value, string paramName, string message = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(paramName, message);
            }
        }

        public static void AgainstNull<T>(T value, string paramName, string message = null)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName, message);
            }
        }
    }

    public class SanityResponse<T>
    {
        public SanityQueryError Error { get; set; }
        public int Ms { get; set; }
        public string Query { get; set; }
        
        [JsonPropertyName("result")]
        public T Value { get; set; }
    }

    public class SanityQueryError
    {
        public string Query { get; set; }
        public string Description { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string Type { get; set; }
    }
}