using System.Collections.Generic;
using DeepSeekAIFoundryUI.Models;

namespace DeepSeekAIFoundryUI.Services
{
    /// <summary>
    /// Defines an interface for storing and retrieving chat sessions and messages.
    /// </summary>
    public interface IChatHistoryService
    {
        ChatSession CreateSession(string initialTitle);
        void SaveMessage(int sessionId, string role, string text);
        List<ChatSession> GetAllSessions();
        ChatSession GetSessionById(int sessionId);
        void ClearAllHistory();
    }
}
