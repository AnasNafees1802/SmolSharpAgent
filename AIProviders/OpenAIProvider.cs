using System;
using System.Linq;

namespace SmolSharpAgent.AIProviders
{
    public class OpenAIProvider : BaseAIProvider
    {
        public OpenAIProvider(AIProviderConfig config) : base(config)
        {
        }

        public override string Name => "OpenAI";

        protected override object CreatePayload(object messages)
        {
            return new
            {
                model = _config.Model,
                messages = messages,
                temperature = 0,
                max_tokens = 2000,
                stream = false
            };
        }
    }
} 