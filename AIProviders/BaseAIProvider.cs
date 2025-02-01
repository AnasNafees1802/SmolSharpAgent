using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmolSharpAgent.AIProviders
{
    public abstract class BaseAIProvider : IAIProvider
    {
        protected readonly AIProviderConfig _config;
        protected readonly HttpClient _httpClient;

        protected BaseAIProvider(AIProviderConfig config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
        }

        public abstract string Name { get; }

        public virtual async Task<string> CallAIEndpoint(string query)
        {
            string promptContent = await ReadPromptFile();
            var messages = new[]
            {
                new { role = "system", content = promptContent },
                new { role = "user", content = query }
            };

            return await SendRequest(CreatePayload(messages));
        }

        public virtual async Task<string> GetErrorCorrection(string originalQuery, string errorCode, string exceptionDetails)
        {
            string promptContent = await ReadPromptFile();
            var messages = new[]
            {
                new { role = "system", content = promptContent },
                new { role = "user", content = originalQuery },
                new { role = "assistant", content = errorCode },
                new { role = "user", content = $"The code above generated the following error: {exceptionDetails}. Please carefully analyse the code and fix all the issues." }
            };

            return await SendRequest(CreatePayload(messages));
        }

        protected virtual object CreatePayload(object messages)
        {
            return new
            {
                model = _config.Model,
                messages = messages,
                temperature = 0,
                stream =false
            };
        }

        protected async Task<string> SendRequest(object payload)
        {
            string payloadJson = JsonSerializer.Serialize(payload);
            try
            {
                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_config.Endpoint, content);
                response.EnsureSuccessStatusCode();
                string responseContent = await response.Content.ReadAsStringAsync();

                var jsonResponse = JsonDocument.Parse(responseContent);
                var contentField = jsonResponse.RootElement
                                           .GetProperty("choices")[0]
                                           .GetProperty("message")
                                           .GetProperty("content")
                                           .GetString();

                return contentField ?? "No content returned by the AI.";
            }
            catch (Exception ex)
            {
                return $"Error calling AI endpoint: {ex.Message}";
            }
        }

        private async Task<string> ReadPromptFile()
        {
            try
            {
                return await File.ReadAllTextAsync("prompt.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading the prompt file: {ex.Message}");
                return "";
            }
        }
    }
} 