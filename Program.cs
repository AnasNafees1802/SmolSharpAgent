using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SmolSharpAgent.AIProviders;

namespace SmolSharpAgent
{
    class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static int maxRetryAttempts = 10;
        private static AIProviderFactory _providerFactory;
        private static IAIProvider _currentProvider;
        private static RoslynExecutor _roslynExecutor;

        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            _providerFactory = new AIProviderFactory(configuration);
            // Set default provider
            _currentProvider = _providerFactory.CreateProvider("HuggingFace");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Welcome to SmolSharpAgent!)");
            Console.ResetColor();

            while (true)
            {
                await RunAutonomousMode(loggerFactory);
            }
        }

        static async Task RunAutonomousMode(ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<RoslynExecutor>();

            Console.WriteLine("\nEnter your query:");
            string userQuery = Console.ReadLine();

            Console.WriteLine("\nHow many retry attempts would you like? (1-10):");
            if (!int.TryParse(Console.ReadLine(), out maxRetryAttempts) || maxRetryAttempts < 1 || maxRetryAttempts > 10)
            {
                maxRetryAttempts = 3;
                Console.WriteLine("Invalid input. Using default value of 3 attempts.");
            }

            // Display available providers
            var providers = AIProviderFactory.GetAvailableProviders();
            Console.WriteLine("\nChoose AI provider:");
            for (int i = 0; i < providers.Length; i++)
            {
                Console.WriteLine($"{i + 1}: {providers[i]}");
            }

            string choice = Console.ReadLine();
            string airesponse;

            try
            {
                if (int.TryParse(choice, out int providerIndex) && providerIndex > 0 && providerIndex <= providers.Length)
                {
                    _currentProvider = _providerFactory.CreateProvider(providers[providerIndex - 1]);
                }
                else
                {
                    Console.WriteLine("Invalid choice. Using default HuggingFace provider.");
                    _currentProvider = _providerFactory.CreateProvider("HuggingFace");
                }

                // Create new RoslynExecutor instance with current provider
                await using var roslynExecutor = new RoslynExecutor(logger, _currentProvider);

                Console.WriteLine($"Using {_currentProvider.Name} provider...");
                airesponse = await _currentProvider.CallAIEndpoint(userQuery);
                await roslynExecutor.ExecuteCodeWithRetryAsync(userQuery, airesponse, maxRetryAttempts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}