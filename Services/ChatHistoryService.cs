using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using DeepSeekAIFoundryUI.Models;

namespace DeepSeekAIFoundryUI.Services
{
    /// <summary>
    /// Provides storage and retrieval capabilities for chat sessions and messages using SQLite.
    /// </summary>
    public class ChatHistoryService : IChatHistoryService
    {
        private readonly string _dbPath;

        /// <summary>
        /// Initializes a new instance of the ChatHistoryService class and ensures database creation.
        /// </summary>
        public ChatHistoryService()
        {
            _dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DeepSeekAIFoundryUI",
                "chatHistory.db"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
            InitializeDatabase();
        }

        /// <summary>
        /// Ensures the required tables exist in the SQLite database.
        /// </summary>
        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var tableCmd = connection.CreateCommand();
            tableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ChatSessions(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT
                );
                
                CREATE TABLE IF NOT EXISTS ChatMessages(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SessionId INTEGER,
                    Role TEXT,
                    Text TEXT,
                    FOREIGN KEY(SessionId) REFERENCES ChatSessions(Id)
                );
            ";
            tableCmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Creates a new chat session record in the database.
        /// </summary>
        public ChatSession CreateSession(string initialTitle)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = "INSERT INTO ChatSessions (Title) VALUES ($title); SELECT last_insert_rowid();";
            insertCmd.Parameters.AddWithValue("$title", initialTitle);
            var sessionId = (long)insertCmd.ExecuteScalar()!;

            return new ChatSession { Id = (int)sessionId, Title = initialTitle };
        }

        /// <summary>
        /// Persists a single chat message to the database for a given session.
        /// </summary>
        public void SaveMessage(int sessionId, string role, string text)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO ChatMessages (SessionId, Role, Text) VALUES ($sessionId, $role, $text);
            ";
            insertCmd.Parameters.AddWithValue("$sessionId", sessionId);
            insertCmd.Parameters.AddWithValue("$role", role);
            insertCmd.Parameters.AddWithValue("$text", text);

            insertCmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Retrieves all existing chat sessions (without loading messages).
        /// </summary>
        public List<ChatSession> GetAllSessions()
        {
            var sessions = new List<ChatSession>();
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var sessionCmd = connection.CreateCommand();
            sessionCmd.CommandText = "SELECT Id, Title FROM ChatSessions;";
            using var reader = sessionCmd.ExecuteReader();

            while (reader.Read())
            {
                sessions.Add(new ChatSession
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1)
                });
            }

            return sessions;
        }

        /// <summary>
        /// Completely clears all sessions and messages from the database.
        /// </summary>
        public void ClearAllHistory()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                DELETE FROM ChatMessages;
                DELETE FROM ChatSessions;
                VACUUM;
            ";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Retrieves a single chat session and its messages from the database by session ID.
        /// </summary>
        public ChatSession GetSessionById(int sessionId)
        {
            var session = new ChatSession();
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var sessionCmd = connection.CreateCommand();
            sessionCmd.CommandText = "SELECT Id, Title FROM ChatSessions WHERE Id = $id;";
            sessionCmd.Parameters.AddWithValue("$id", sessionId);
            using var sessionReader = sessionCmd.ExecuteReader();
            if (sessionReader.Read())
            {
                session.Id = sessionReader.GetInt32(0);
                session.Title = sessionReader.GetString(1);
            }

            session.Messages = new List<ChatMessageModel>();
            var msgCmd = connection.CreateCommand();
            msgCmd.CommandText = "SELECT Id, Role, Text FROM ChatMessages WHERE SessionId = $id;";
            msgCmd.Parameters.AddWithValue("$id", sessionId);

            using var msgReader = msgCmd.ExecuteReader();
            while (msgReader.Read())
            {
                session.Messages.Add(new ChatMessageModel
                {
                    Id = msgReader.GetInt32(0),
                    Role = msgReader.GetString(1),
                    Text = msgReader.GetString(2),
                    SessionId = sessionId
                });
            }

            return session;
        }
    }
}
