using System.Threading.Tasks;

namespace SmolSharpAgent.AIProviders
{
    public interface IAIProvider
    {
        string Name { get; }
        Task<string> CallAIEndpoint(string query);
        Task<string> GetErrorCorrection(string originalQuery, string errorCode, string exceptionDetails);
    }

    public class AIProviderConfig
    {
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
        public string Model { get; set; }
    }

    public class AIProvidersConfig
    {
        public AIProviderConfig HuggingFace { get; set; }
        public AIProviderConfig OpenAI { get; set; }
    }
} 