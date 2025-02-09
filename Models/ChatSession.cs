using System.Collections.Generic;

namespace DeepSeekAIFoundryUI.Models
{
    /// <summary>
    /// Represents a chat session containing multiple user and assistant messages.
    /// </summary>
    public class ChatSession
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public List<ChatMessageModel> Messages { get; set; } = new List<ChatMessageModel>();
    }
}
