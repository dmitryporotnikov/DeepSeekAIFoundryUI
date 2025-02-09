using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DeepSeekAIFoundryUI.Services
{
    /// <summary>
    /// Manages communication with the AI service to obtain chat completions.
    /// </summary>
    public class ChatService
    {
        private readonly IChatClient _chatClient;

        /// <summary>
        /// Initializes a ChatService instance with the specified AI model configuration.
        /// </summary>
        public ChatService(Uri endpoint, AzureKeyCredential credential, string modelName)
        {
            _chatClient = new ChatCompletionsClient(endpoint, credential)
                .AsChatClient(modelName);
        }

        /// <summary>
        /// Requests a streaming chat completion from the AI service.
        /// </summary>
        public IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
            List<ChatMessage> messages,
            CancellationToken cancellationToken)
        {
            return _chatClient.CompleteStreamingAsync(messages, cancellationToken: cancellationToken);
        }
    }
}
