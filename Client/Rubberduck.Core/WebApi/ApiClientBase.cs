﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Rubberduck.Core.WebApi
{
    public abstract class ApiClientBase
    {
        public const string ContentTypeApplicationJson = "application/json";

        private readonly HttpClient _client;
        private readonly string _baseUrl;
        protected TimeSpan GetRequestTimeout { get; }
        protected TimeSpan PostRequestTimeout { get; }

        private static readonly ProductInfoHeaderValue UserAgent =
            new ProductInfoHeaderValue("Rubberduck", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

        protected ApiClientBase(HttpClient client)
        {
            _client = client;
            _baseUrl = Properties.Settings.Default.WebApiBaseUrl;
        }

        protected string BaseUrl => _baseUrl;

        protected virtual void ConfigureClient(HttpClient client, string contentType = ContentTypeApplicationJson)
        {
            _client.DefaultRequestHeaders.UserAgent.Clear();
            _client.DefaultRequestHeaders.UserAgent.Add(UserAgent);
        }

        protected virtual async Task<TResult> GetResponseAsync<TResult>(string route)
        {
            var uri = new Uri($"{_baseUrl}/{route}");
            try
            {
                ConfigureClient(_client);

                //_client.Timeout = GetRequestTimeout;
                using (var response = await _client.GetAsync(uri))
                {
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();

                    return JsonSerializer.Deserialize<TResult>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception exception)
            {
                throw new ApiException(exception);
            }
        }

        protected virtual async Task<T> PostAsync<T>(string route, T args) => await PostAsync<T, T>(route, args);

        protected virtual async Task<TResult> PostAsync<TArgs, TResult>(string route, TArgs args)
        {
            var uri = new Uri($"{_baseUrl}/{route}");
            string json;
            try
            {
                json = JsonSerializer.Serialize(args);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("The specified arguments could not be serialized.", exception);
            }

            try
            {
                ConfigureClient(_client);
                //_client.Timeout = PostRequestTimeout;
                using (var response = await _client.PostAsync(uri, new StringContent(json, Encoding.UTF8, ContentTypeApplicationJson)))
                {
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<TResult>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result;
                }
            }
            catch (Exception exception)
            {
                throw new ApiException(exception);
            }
        }
    }
}
