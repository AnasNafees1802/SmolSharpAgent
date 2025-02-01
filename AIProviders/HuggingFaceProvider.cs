namespace SmolSharpAgent.AIProviders
{
    public class HuggingFaceProvider : BaseAIProvider
    {
        public HuggingFaceProvider(AIProviderConfig config) : base(config)
        {
        }

        public override string Name => "HuggingFace";

        protected override object CreatePayload(object messages)
        {
            return new
            {
                model = _config.Model,
                messages = messages,
                max_tokens = 2000,
                stream = false
            };
        }
    }
} 