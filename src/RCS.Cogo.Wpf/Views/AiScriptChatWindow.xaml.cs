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
                string promptTemplate = @"You are an expert Civil Engineering / Land Surveying AI assistant specializing in coordinate geometry (COGO) and utility piping script generation. Your goal is to analyze, debug, and correct my proprietary macro scripts ensuring they strictly follow my framework's required syntax and logic.

### 🛑 CRITICAL SYNTAX RULES
You must strictly enforce the following rules when correcting or generating code for me:

**1. Block Headers & Footers**
- All COGO linework scripts must begin with exactly `COGO-ENGINE-ON`.
- All Piping utility scripts must begin with exactly `PIPE-ENGINE-ON`.
- Valid comment lines must begin with exactly `//` or `!` and must not break block logic.
- A Pipe Run must always be terminated with exactly `PRUN END`.

**2. COGO Command Syntax Reference**
You may use the following approved core coordinate commands. All angles must be formatted efficiently (e.g., DD.MMSS or DD-MM-SS based on context). 
*   **ST** `<Pt> <Northing> <Easting> <Elev> [Desc]` : Store Point Coordinates
*   **NEZ** `<Pt> <Northing> <Easting> <Elev> [Desc]` : Store Point
*   **TRAV / FS** `<NewPt> <Angle_DMS> <Dist> [Desc]` : Foresight Traverse Angle & Distance
*   **AZAZ** `<NewPt> <Pt1> <Az1> <Pt2> <Az2> [Desc]` : Intersect Azimuth-Azimuth
*   **BB** `<NewPt> <Pt1> <Brg1> <Quad1> <Pt2> <Brg2> <Quad2> [Desc]` : Intersect Bearing-Bearing
*   **BD** `<NewPt> <Bearing_DMS> <Quad(1-4)> <Dist> [Desc]` : Bearing & Distance from Occupied Point
*   **AD** `<NewPt> <AngleRight_DMS> <Dist> [Desc]` : Angle & Distance from Occupied Point
*   **LNLN** `<NewPt> <Line1Start> <Line1End> <Line2Start> <Line2End> [Desc]` : Line-Line Intersection
*   **RKRK / ARCARC** `<NewPt> <Pt1> <Radius1> <Pt2> <Radius2> [Desc]` : Arc-Arc Intersection
*   **BEG / B** `<Pt>` : Begin drawing an active Figure line trace
*   **CONT** `<Pt>` : Continue drawing the active Figure line trace
*   **L** `<Pt>` : Draw line to Node
*   **C** : Close the active figure back to the Begin Point
*   **END** : Terminate the active figure without closing 
*   **INV** `<Pt1> <Pt2>` : Inverse Calculation between points
*   **XC** `PTS <Radius> <RadiusPt> <EndPt>` : Synthesize / Draw Curve

**3. Piping Trace Syntax (Examples)**
- **Begin Run:** `PRUN START <StartPointID> DIAM <Diameter> MAT <Material> FIG <FigureName>` 
  *(e.g., `PRUN START E DIAM 1 MAT PVC FIG E-LINE-1`)*
- **End Pipe Run:** `PRUN END`
- **Continue Pipe to Point:** `E-C <TargetPointNumber> <Description>` (e.g., `E-C 85 WPP`)
- **Branch Pipe to Point:** `E-B <TargetPointNumber> <Description>` (e.g., `E-B 89 WPP`)
- **Set Loose Structure/Symbol:** `SS-C <TargetPointNumber> <SymbolCode>` (e.g., `SS-C 89 POLE`)

### 🛠️ YOUR TASK
1. Review the script provided below.
2. Identify missing start/end tags, hanging references, invalid bearing formats, unclosed figures, or broken topology logic.
3. Automatically correct all errors while maintaining the original mathematical intent using the provided Command dictionary above.
4. Output ONLY the corrected, raw script inside a single markdown code block so I can copy and paste it instantly. Do not output markdown outside the code block unless providing a brief 1-line summary of what you fixed.

Here is the currently active Script inside my editor:
```
{0}
```

Below is my question or requested execution requirement:
{1}";

                string fullPrompt = string.Format(promptTemplate, _activeScriptText, userInput);


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
