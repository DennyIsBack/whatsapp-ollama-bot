using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OllamaSharp;

namespace whatsapp_ollama_bot.Services
{
    /// <summary>
    /// Client responsible for interacting with a local Ollama instance.
    /// It wraps the OllamaSharp library and exposes a simple method to
    /// generate a textual completion for a given prompt.
    /// </summary>
    public class OllamaClient
    {
        private readonly OllamaApiClient _client;
        private readonly string _modelName;

        public OllamaClient(IConfiguration configuration)
        {
            // Read configuration values for the Ollama base URL and model.
            var baseUrl = configuration.GetValue<string>("Ollama:BaseUrl") ?? "http://localhost:11434";
            _modelName = configuration.GetValue<string>("Ollama:ModelName") ?? "llama3";

            // Initialize an Ollama API client pointing at the local Ollama server.
            _client = new OllamaApiClient(new Uri(baseUrl));
            _client.SelectedModel = _modelName;
        }

        /// <summary>
        /// Generates a completion from the local language model for the given prompt.
        /// The method streams tokens from the Ollama API and aggregates them into a single string.
        /// </summary>
        /// <param name="prompt">Prompt to send to the model.</param>
        /// <returns>The full completion as a string.</returns>
        public async Task<string> GenerateResponseAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return string.Empty;
            }

            var result = string.Empty;

            // Use the streaming API so that the model can start returning tokens as it generates them.
            await foreach (var token in _client.GenerateAsync(prompt))
            {
                result += token.Response;
            }

            return result;
        }
    }
}