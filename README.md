# SmolSharpAgent 🤖

SmolSharpAgent is a C# implementation inspired by Hugging Face's SmollAgents, which generates and executes Python code. This project brings the same powerful concept to the .NET ecosystem, allowing real-time code generation and execution using C#. It combines AI-powered code generation with Roslyn scripting to create a versatile automation tool.

## 🌟 About

SmolSharpAgent is built on the concept of Hugging Face's SmollAgents but specifically designed for C# developers. While SmollAgents focuses on Python code generation and execution, SmolSharpAgent brings this capability to C# with additional features:

- Support for multiple AI providers (HuggingFace and OpenAI)
- Real-time code execution using Roslyn scripting
- Robust error handling with automatic correction
- Built-in security validations
- Extensive .NET library support

## 🌟 Features

- **AI-Powered Code Generation**: Utilizes multiple AI providers (HuggingFace, OpenAI) for intelligent code generation
- **Real-Time Code Execution**: Executes generated code immediately using Roslyn scripting
- **Multiple AI Providers**: Supports multiple AI providers with easy switching
- **Robust Error Handling**: Includes automatic error correction and retry mechanisms
- **Security Features**: Built-in code validation and security checks
- **Resource Management**: Proper handling of disposable resources
- **Extensive Library Support**: Pre-configured with popular .NET libraries

## 🛠️ Installation & Setup

1. **Prerequisites**
   - .NET 8.0 SDK
   - Windows/Linux/macOS
   - AI Provider API Keys (HuggingFace or OpenAI)

2. **Clone and Setup**
```bash
# Clone the repository
git clone https://github.com/yourusername/SmolSharpAgent.git

# Navigate to project directory
cd SmolSharpAgent

# Restore dependencies
dotnet restore
```

3. **Configure AI Providers**
Rename `appsettings.example.json` to `appsettings.json` and add your configurations:
```json
{
  "AIProviders": {
    "HuggingFace": {
      "Endpoint": "YOUR_HUGGINGFACE_ENDPOINT",
      "ApiKey": "YOUR_HUGGINGFACE_API_KEY",
      "Model": "YOUR_MODEL_NAME"
    },
    "OpenAI": {
      "Endpoint": "YOUR_OPENAI_ENDPOINT",
      "ApiKey": "YOUR_OPENAI_API_KEY",
      "Model": "YOUR_MODEL_NAME"
    }
  }
}
```

4. **Build and Run**
```bash
dotnet run
```

## ⚙️ How It Works

SmolSharpAgent operates through a sophisticated pipeline:

1. **User Input**
   - User provides a natural language query describing the desired code operation
   - User selects the AI provider (HuggingFace or OpenAI)
   - User sets retry attempts for error correction

2. **AI Code Generation**
   - Selected AI provider processes the query
   - Generates C# code based on the prompt and requirements
   - Ensures code follows Roslyn script execution format

3. **Code Execution**
   - RoslynExecutor validates the generated code for security
   - Prepares execution environment with required dependencies
   - Executes code using Roslyn scripting engine
   - Handles errors and performs automatic correction if needed

4. **Error Handling**
   - Detects compilation or runtime errors
   - Sends errors back to AI for correction
   - Implements retry mechanism with specified attempts
   - Provides detailed error feedback

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👨‍💻 Author

**Anas Nafees**
- Inspired by Hugging Face's SmollAgents
- Implemented C# version with extended capabilities
- Added multi-provider support and enhanced error handling

## 🙏 Acknowledgments

- **Hugging Face** - For the original SmollAgents concept and API
- **OpenAI** - For their powerful language models and API
- **Microsoft** - For .NET, Roslyn, and Playwright
- **.NET Community** - For the amazing libraries and tools
- **Open Source Community** - For inspiration and contributions

---

Made with ❤️ by Anas Nafees 
