using System;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace SmolSharpAgent.AIProviders
{
    public class AIProviderFactory
    {
        private readonly IConfiguration _configuration;

        public AIProviderFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IAIProvider CreateProvider(string providerName)
        {
            var config = _configuration.GetSection("AIProviders").Get<AIProvidersConfig>();
            
            return providerName.ToLower() switch
            {
                "huggingface" => new HuggingFaceProvider(config.HuggingFace),
                "openai" => new OpenAIProvider(config.OpenAI),
                _ => throw new ArgumentException($"Unknown provider: {providerName}")
            };
        }

        public static string[] GetAvailableProviders()
        {
            return new[] { "HuggingFace", "OpenAI" };
        }
    }
} 