namespace DeepSeekAIFoundryUI.Models
{
    /// <summary>
    /// Represents a single chat message, including role and text content.
    /// </summary>
    public class ChatMessageModel
    {
        public int Id { get; set; }
        public string Role { get; set; }
        public string Text { get; set; }
        public int SessionId { get; set; }
    }
}
