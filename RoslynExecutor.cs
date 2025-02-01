﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using System.Security;
using System.Text.RegularExpressions;
using SmolSharpAgent.AIProviders;

public class RoslynExecutor : IAsyncDisposable
{
    private const int MaxRetryAttempts = 10;
    private const int ExecutionTimeoutSeconds = 30;
    private readonly ILogger<RoslynExecutor> _logger;
    private readonly ScriptOptions _scriptOptions;
    private readonly IAIProvider _aiProvider;
    private bool _disposed;
    private CancellationTokenSource _timeoutCts;

    public RoslynExecutor(ILogger<RoslynExecutor> logger, IAIProvider aiProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
        _scriptOptions = InitializeScriptOptions();
        _timeoutCts = new CancellationTokenSource();
    }

    public async Task ExecuteCodeWithRetryAsync(string userQuery, string fullCode, int remainingAttempts)
    {
        if (string.IsNullOrWhiteSpace(fullCode))
        {
            _logger.LogError("Code input is empty or whitespace");
            throw new ArgumentException("Code input cannot be empty", nameof(fullCode));
        }

        try
        {
            fullCode = SanitizeAndValidateCode(fullCode);
            
            _logger.LogInformation("Preparing for code execution...");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Preparing Dependencies...");
            Console.ResetColor();

            // Reset timeout token for new execution
            ResetTimeout();

            _logger.LogInformation("Executing code...");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Executing Code...");
            Console.WriteLine(fullCode);
            Console.ResetColor();

            await ExecuteWithTimeoutAsync(async () =>
            {
                var executionResult = await CSharpScript.EvaluateAsync(
                    fullCode, 
                    _scriptOptions,
                    cancellationToken: _timeoutCts.Token
                );
                
                if (executionResult != null)
                {
                    Console.WriteLine(executionResult);
                }
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Code execution timed out");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Execution timed out after {ExecutionTimeoutSeconds} seconds");
            Console.ResetColor();
        }
        catch (CompilationErrorException e)
        {
            await HandleCompilationError(userQuery, fullCode, e, remainingAttempts);
        }
        catch (Exception ex) when (ex is SecurityException || ex is UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Security violation in code execution");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Security Error: The code attempted to perform unauthorized operations");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during code execution");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"An error occurred during execution: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            Console.ResetColor();
        }
    }

    private async Task HandleCompilationError(string userQuery, string code, CompilationErrorException e, int remainingAttempts)
    {
        _logger.LogWarning("Compilation errors detected: {Errors}", 
            string.Join(Environment.NewLine, e.Diagnostics));

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Compilation Errors:");
        Console.WriteLine(string.Join(Environment.NewLine, e.Diagnostics));
        Console.ResetColor();

        if (remainingAttempts > 1)
        {
            Console.WriteLine($"\nAttempting to fix the error... ({remainingAttempts - 1} attempts remaining)");

            try
            {
                var correctedCode = await GetErrorCorrectionFromAI(userQuery, code, 
                    string.Join(Environment.NewLine, e.Diagnostics));

                if (!string.IsNullOrWhiteSpace(correctedCode))
                {
                    Console.WriteLine("Executing Corrected Code...");
                    await ExecuteCodeWithRetryAsync(userQuery, correctedCode, remainingAttempts - 1);
                }
                else
                {
                    _logger.LogError("AI returned empty correction");
                    Console.WriteLine("Error: AI could not generate a correction");
                }
            }
            catch (Exception aiEx)
            {
                _logger.LogError(aiEx, "Error during AI correction");
                Console.WriteLine($"Error getting AI correction: {aiEx.Message}");
            }
        }
        else
        {
            Console.WriteLine("\nMaximum retry attempts reached. Unable to fix the code.");
        }
    }

    private async Task<string> GetErrorCorrectionFromAI(string userQuery, string fullCode, string diagnostics)
    {
        try
        {
            return await _aiProvider.GetErrorCorrection(userQuery, fullCode, diagnostics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI correction");
            throw;
        }
    }

    private string SanitizeAndValidateCode(string input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));

        // Remove markdown code blocks
        input = input.Replace("```csharp", string.Empty)
                    .Replace("```", string.Empty)
                    .Trim();

        // Basic security validation
        var forbiddenPatterns = new[]
        {
            @"Process\.Start",
            @"File\.(Delete|Move)",
            @"Registry\.",
            @"new\s+WebClient",
            @"Environment\.(Exit|FailFast)",
            @"System\.Diagnostics\.Process",
            @"System\.Reflection\.(Assembly|Emit)",
            @"System\.Runtime\.InteropServices"
        };

        foreach (var pattern in forbiddenPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                throw new SecurityException($"Code contains potentially dangerous operation: {pattern}");
            }
        }

        return input;
    }

    private ScriptOptions InitializeScriptOptions()
    {
        try
        {
            return ScriptOptions.Default
                .WithReferences(GetRequiredReferences())
                .WithImports(GetRequiredImports())
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithAllowUnsafe(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing script options");
            throw;
        }
    }

    private async Task ExecuteWithTimeoutAsync(Func<Task> action)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(ExecutionTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, _timeoutCts.Token);

        try
        {
            await Task.Run(action, linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException($"Code execution timed out after {ExecutionTimeoutSeconds} seconds");
        }
    }

    private void ResetTimeout()
    {
        try
        {
            _timeoutCts.Cancel();
            _timeoutCts.Dispose();
            _timeoutCts = new CancellationTokenSource();
        }
        catch (ObjectDisposedException) { /* Ignore if already disposed */ }
    }

    private static string[] GetRequiredImports()
    {
        return new[]
        {
            "System",
            "System.Linq",
            "System.Console",
            "System.Collections",
            "System.Collections.Generic",
            "System.Threading",
            "System.Threading.Tasks",
            "System.Text",
            "System.Text.RegularExpressions",
            "Newtonsoft.Json",
            "RestSharp",
            "CsvHelper",
            "NPOI.SS.UserModel",
            "NPOI.XSSF.UserModel",
            "Quartz",
            "Polly",
            "ClosedXML.Excel",
            "Flurl.Http",
            "HtmlAgilityPack",
            "SixLabors.ImageSharp",
            "ICSharpCode.SharpZipLib.Zip",
            "Spectre.Console",
            "Serilog",
            "MimeKit",
            "AngleSharp",
            "OpenQA.Selenium",
            "Bogus",
            "Grpc.Net.Client",
            "Ionic.Zip",
            "Microsoft.Playwright"
        };
    }

    private static List<MetadataReference> GetRequiredReferences()
    {
        var references = new List<MetadataReference>();
        var requiredAssemblies = new Dictionary<Type, string>
        {
            { typeof(object), "mscorlib" },
            { typeof(Enumerable), "System.Linq" },
            { typeof(Newtonsoft.Json.JsonConvert), "Newtonsoft.Json" },
            { typeof(RestSharp.RestClient), "RestSharp" },
            { typeof(CsvHelper.CsvWriter), "CsvHelper" },
            { typeof(NPOI.XSSF.UserModel.XSSFWorkbook), "NPOI" },
            { typeof(Quartz.IScheduler), "Quartz.NET" },
            { typeof(Polly.Policy), "Polly" },
            { typeof(ClosedXML.Excel.XLWorkbook), "ClosedXML" },
            { typeof(Flurl.Http.FlurlClient), "Flurl.Http" },
            { typeof(HtmlAgilityPack.HtmlDocument), "HtmlAgilityPack" },
            { typeof(SixLabors.ImageSharp.Image), "ImageSharp" },
            { typeof(ICSharpCode.SharpZipLib.Zip.ZipFile), "SharpZipLib" },
            { typeof(Spectre.Console.AnsiConsole), "Spectre.Console" },
            { typeof(Serilog.Log), "Serilog" },
            { typeof(MimeKit.MimeMessage), "MimeKit" },
            { typeof(AngleSharp.BrowsingContext), "AngleSharp" },
            { typeof(OpenQA.Selenium.IWebDriver), "Selenium.WebDriver" },
            { typeof(Bogus.Faker), "Bogus" },
            { typeof(Grpc.Net.Client.GrpcChannel), "Grpc.Net.Client" },
            { typeof(Ionic.Zip.ZipFile), "DotNetZip" },
            { typeof(Microsoft.Playwright.IPlaywright), "Microsoft.Playwright" },
            { typeof(Microsoft.Playwright.IPage), "Microsoft.Playwright" },
            { typeof(Microsoft.Playwright.IBrowser), "Microsoft.Playwright" },
            { typeof(Microsoft.Playwright.IBrowserContext), "Microsoft.Playwright" },
            { typeof(Microsoft.Playwright.ILocator), "Microsoft.Playwright" }
        };

        foreach (var assembly in requiredAssemblies)
        {
            try
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Key.Assembly.Location));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load {assembly.Value}: {ex.Message}");
            }
        }

        // Add core framework assemblies
        try
        {
            references.AddRange(new[]
            {
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Core").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Console").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Linq.Expressions").Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.DynamicAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load some core references: {ex.Message}");
        }

        return references;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        try
        {
            _timeoutCts?.Cancel();
            await DisposeAsyncCore();
        }
        finally
        {
            _disposed = true;
            _timeoutCts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        // Cleanup any additional async resources
        await Task.CompletedTask;
    }
}
