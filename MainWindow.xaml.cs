using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DeepSeekAIFoundryUI.Models;
using DeepSeekAIFoundryUI.Services;
using DeepSeekAIFoundryUI.Utilities;
using Microsoft.Extensions.Configuration;
using Azure;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.AI;

namespace DeepSeekAIFoundryUI
{
    /// <summary>
    /// Represents the main window and primary interaction logic for the chat application.
    /// </summary>
    public partial class MainWindow : Window
    {
        private ChatService _chatService;
        private IChatHistoryService _chatHistoryService;
        private ChatOutputFormatter _formatter = new ChatOutputFormatter();
        private CancellationTokenSource? _cts;
        private int _currentSessionId;
        private bool _isAIResponding = false;

        /// <summary>
        /// Initializes the MainWindow, loads configuration, and populates the session list.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();

            try
            { 
            var modelKey = configuration["ModelKey"] ?? throw new ArgumentNullException("ModelKey missing");
            var modelEndpoint = configuration["ModelEndpoint"] ?? throw new ArgumentNullException("ModelEndpoint missing");
            var modelName = configuration["ModelName"] ?? throw new ArgumentNullException("ModelName missing");

            var credential = new AzureKeyCredential(modelKey);
            var endpoint = new Uri(modelEndpoint);
            _chatService = new ChatService(endpoint, credential, modelName);

            _chatHistoryService = new ChatHistoryService();
            LoadSessionsList();
            }
            catch
            {
                SettingsButton.Click += (sender, e) => MessageBox.Show("Please provide valid configuration values.");
            }

            
        }

        /// <summary>
        /// Retrieves all chat sessions from storage and displays them in the UI list.
        /// </summary>
        private void LoadSessionsList()
        {
            var sessions = _chatHistoryService.GetAllSessions();
            SessionsListBox.ItemsSource = sessions;
            SessionsListBox.DisplayMemberPath = "Title";
        }

        /// <summary>
        /// Toggles the visibility of the session sidebar.
        /// </summary>
        private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            if (SidebarColumn.Width.Value > 0)
            {
                SidebarColumn.Width = new GridLength(0);
                Sidebar.Visibility = Visibility.Collapsed;
            }
            else
            {
                SidebarColumn.Width = new GridLength(250);
                Sidebar.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Creates a new chat session and updates the UI.
        /// </summary>
        private void NewSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAIResponding)
            {
                MessageBox.Show("Cannot create a new session while AI is responding. Please wait or click Stop.");
                return; // Early exit – do not create a session
            }

            var sessionTitle = $"Chat {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            var createdSession = _chatHistoryService.CreateSession(sessionTitle);
            _currentSessionId = createdSession.Id;
            LoadSessionsList();

            if (SessionsListBox.ItemsSource is List<ChatSession> sessions)
            {
                var matching = sessions.Find(s => s.Id == createdSession.Id);
                if (matching != null)
                {
                    SessionsListBox.SelectedItem = matching;
                }
            }
        }

        /// <summary>
        /// Loads and displays the selected chat session. Prevents switching if AI is still responding.
        /// </summary>
        private void SessionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isAIResponding)
            {
                MessageBox.Show("Cannot switch chats while AI is responding. Please wait or click Stop.");
                SessionsListBox.SelectionChanged -= SessionsListBox_SelectionChanged;
                if (e.RemovedItems.Count > 0)
                {
                    SessionsListBox.SelectedItem = e.RemovedItems[0];
                }
                SessionsListBox.SelectionChanged += SessionsListBox_SelectionChanged;
                return;
            }

            if (SessionsListBox.SelectedItem is ChatSession partialSession)
            {
                var fullSession = _chatHistoryService.GetSessionById(partialSession.Id);
                _currentSessionId = fullSession.Id;
                DisplayChatSession(fullSession);
            }
        }

        /// <summary>
        /// Clears all chat history from storage after user confirmation.
        /// </summary>
        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear ALL chat history?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                _chatHistoryService.ClearAllHistory();
                _currentSessionId = 0;
                ChatOutputRichTextBox.Document.Blocks.Clear();
                LoadSessionsList();
            }
        }

        /// <summary>
        /// Renders all messages from a specific chat session in the UI.
        /// </summary>
        private void DisplayChatSession(ChatSession session)
        {
            ChatOutputRichTextBox.Document.Blocks.Clear();

            foreach (var msg in session.Messages)
            {
                if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    AppendFormattedText($">>> User: {msg.Text}\r\n", Brushes.Gold);
                }
                else
                {
                    AppendFormattedText(">>> AI: ", Brushes.Green);
                    _formatter.AppendChunkWithThink(msg.Text, ChatOutputRichTextBox);
                    AppendFormattedText("\r\n", Brushes.Black);
                }
            }
        }

        /// <summary>
        /// Detects Enter key presses in the user input box and triggers a send action.
        /// </summary>
        private void UserInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                SendButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        /// <summary>
        /// Sends the user's message to the AI service and displays the streaming response.
        /// </summary>
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _isAIResponding = true;
            SpinnerEllipse.Visibility = Visibility.Visible;

            if (_currentSessionId == 0)
            {
                var sessionTitle = $"Chat {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                var session = _chatHistoryService.CreateSession(sessionTitle);
                _currentSessionId = session.Id;
                LoadSessionsList();
                SessionsListBox.SelectedItem = session;
            }

            var userQuestion = UserInputTextBox.Text;
            if (string.IsNullOrWhiteSpace(userQuestion))
            {
                _isAIResponding = false;
                SpinnerEllipse.Visibility = Visibility.Collapsed;
                return;
            }

            AppendFormattedText($">>> User: {userQuestion}\r\n", Brushes.Gold);
            _chatHistoryService.SaveMessage(_currentSessionId, "user", userQuestion);
            UserInputTextBox.Clear();

            try
            {
                AppendFormattedText(">>> AI: ", Brushes.Green);

                var messages = new List<ChatMessage>
                {
                    new ChatMessage { Role = ChatRole.User, Text = userQuestion }
                };

                var responseStream = _chatService.CompleteStreamingAsync(messages, _cts.Token);

                await foreach (var update in responseStream)
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    var delta = update.Text;
                    if (!string.IsNullOrEmpty(delta))
                    {
                        _formatter.AppendChunkWithThink(delta, ChatOutputRichTextBox);
                    }
                }

                var entireResponse = GetCurrentAssistantResponse();
                _chatHistoryService.SaveMessage(_currentSessionId, "assistant", entireResponse);
            }
            catch (Exception ex)
            {
                AppendFormattedText($"\r\n[Error]: {ex.Message}\r\n", Brushes.Red);
            }
            finally
            {
                AppendFormattedText("\r\n", Brushes.Black);
                _isAIResponding = false;
                SpinnerEllipse.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Stops any ongoing streaming response from the AI service.
        /// </summary>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        /// <summary>
        /// Retrieves the current AI response from the RichTextBox control.
        /// </summary>
        private string GetCurrentAssistantResponse()
        {
            var textRange = new System.Windows.Documents.TextRange(
                ChatOutputRichTextBox.Document.ContentStart,
                ChatOutputRichTextBox.Document.ContentEnd
            );
            return textRange.Text.TrimEnd();
        }

        /// <summary>
        /// Appends colored text to the output RichTextBox.
        /// </summary>
        private void AppendFormattedText(string text, Brush color)
        {
            var run = new System.Windows.Documents.Run(text) { Foreground = color };
            if (ChatOutputRichTextBox.Document.Blocks.LastBlock is System.Windows.Documents.Paragraph p)
            {
                p.Inlines.Add(run);
            }
            else
            {
                ChatOutputRichTextBox.Document.Blocks.Add(
                    new System.Windows.Documents.Paragraph(run)
                );
            }
            ChatOutputRichTextBox.ScrollToEnd();
        }

        /// <summary>
        /// Opens the Settings window to modify configuration values.
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Views.SettingsWindow();
            settingsWindow.Owner = this;
            var result = settingsWindow.ShowDialog();
            if (result == true)
            {
                ReInitializeChatService();
            }
        }

        /// <summary>
        /// Re-initializes the ChatService after updated configuration is saved.
        /// </summary>
        private void ReInitializeChatService()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();

            var modelKey = configuration["ModelKey"] ?? "";
            var modelEndpoint = configuration["ModelEndpoint"] ?? "";
            var modelName = configuration["ModelName"] ?? "";

            if (!string.IsNullOrEmpty(modelKey)
                && !string.IsNullOrEmpty(modelEndpoint)
                && !string.IsNullOrEmpty(modelName))
            {
                var credential = new Azure.AzureKeyCredential(modelKey);
                try
                {

                var endpointUri = new Uri(modelEndpoint);
                _chatService = new ChatService(endpointUri, credential, modelName);
                }
                catch
                {
                    MessageBox.Show("Please provide valid configuration values.");
                    return;
                }

            }
        }
    }
}
