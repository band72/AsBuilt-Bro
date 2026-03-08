using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RCS.Cogo.AI;

namespace RCS.Cogo.Wpf.Views
{
    public partial class AiScriptChatWindow : Window
    {
        public ObservableCollection<ChatMessage> Messages { get; set; } = new ObservableCollection<ChatMessage>();

        private AiAnalyzer _analyzer;
        private string _activeScriptText;

        public AiScriptChatWindow(string activeScriptText)
        {
            InitializeComponent();
            _activeScriptText = activeScriptText;
            _analyzer = new AiAnalyzer();
            DataContext = this;
            ChatHistory.ItemsSource = Messages;

            AddAgentMessage("Hello! I am your AI Script Assistant. I'm reviewing the active script... What can I help you with?");
            RunInitialAnalysis();
        }

        private void AddAgentMessage(string msg)
        {
            Messages.Add(new ChatMessage { Text = msg, Initial = "AI", AvatarColor = Brushes.BlueViolet, BubbleColor = new SolidColorBrush(Color.FromRgb(45, 25, 75)) });
            ScrollToBottom();
        }

        private void AddUserMessage(string msg)
        {
            Messages.Add(new ChatMessage { Text = msg, Initial = "U", AvatarColor = Brushes.DarkGray, BubbleColor = Brushes.DimGray });
            ScrollToBottom();
        }

        private void RunInitialAnalysis()
        {
            if (string.IsNullOrWhiteSpace(_activeScriptText))
                return;

            try
            {
                var errors = _analyzer.AnalyzeScript(_activeScriptText);
                if (errors.Count > 0)
                {
                    AddAgentMessage($"I've found {errors.Count} potential issues in this script. Would you like me to explain them or try to fix them?");
                }
                else
                {
                    AddAgentMessage("Your script looks geometrically clean! What would you like to build next?");
                }
            }
            catch (Exception ex)
            {
                AddAgentMessage($"Error running quick analysis: {ex.Message}");
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessInput();
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    // Allow Shift+Enter to create a new line
                    return;
                }
                
                e.Handled = true;
                ProcessInput();
            }
        }

        private async void ProcessInput()
        {
            string input = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            string apiKey = ApiKeyBox.Text;
            string selectedModel = (ModelSelector.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Gemini 3.1 Pro Preview";

            AddUserMessage(input);
            InputBox.Text = "";

            TypingIndicator.Visibility = Visibility.Visible;
            string response = await GenerateAiResponseAsync(input, apiKey, selectedModel);
            TypingIndicator.Visibility = Visibility.Collapsed;
            
            AddAgentMessage(response);
        }

        private async System.Threading.Tasks.Task<string> GenerateAiResponseAsync(string userInput, string apiKey, string modelName)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return $"[Simulated {modelName} Response]\nPlease enter an API Key to connect to Google Cloud, then try your prompt again!";
            }

            try
            {
                string fullPrompt = $"System Context: You are an expert civil engineering COGO and Piping Script Assistant. Here is the user's currently active script:\n```\n{_activeScriptText}\n```\n\nUser Question: {userInput}";

                using var client = new System.Net.Http.HttpClient();
                
                if (modelName.ToLower().Contains("gemini"))
                {
                    var requestBody = new
                    {
                        contents = new[]
                        {
                            new { parts = new[] { new { text = fullPrompt } } }
                        }
                    };
                    
                    string contentStr = System.Text.Json.JsonSerializer.Serialize(requestBody);
                    var content = new System.Net.Http.StringContent(contentStr, System.Text.Encoding.UTF8, "application/json");
                    
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
                    var result = await client.PostAsync(url, content);
                    
                    string responseStr = await result.Content.ReadAsStringAsync();
                    
                    if (result.IsSuccessStatusCode)
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(responseStr);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                        {
                            var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                            return text ?? "No response text found.";
                        }
                    }
                    return $"Gemini API Error ({result.StatusCode}): {responseStr}";
                }
                else
                {
                    var payload = new
                    {
                        model = modelName,
                        messages = new[]
                        {
                            new
                            {
                                role = "user",
                                content = new object[]
                                {
                                    new { type = "text", text = fullPrompt }
                                }
                            }
                        },
                        max_tokens = 1500,
                        temperature = 0.2
                    };

                    string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                    var content = new System.Net.Http.StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                    var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return $"OpenAI API Error ({response.StatusCode}): {responseContent}";
                    }

                    using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var message = choices[0].GetProperty("message");
                        if (message.TryGetProperty("content", out var contentElement))
                        {
                            return contentElement.GetString() ?? string.Empty;
                        }
                    }
                    return $"Failed to parse OpenAI response: {responseContent}";
                }
            }
            catch (Exception ex)
            {
                return $"Exception invoking AI: {ex.Message}";
            }
        }

        private void ScrollToBottom()
        {
            if (ChatHistory.Items.Count > 0)
            {
                ChatHistory.ScrollIntoView(ChatHistory.Items[ChatHistory.Items.Count - 1]);
            }
        }
    }
}
