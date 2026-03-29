using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using RCS.Cogo.AI;
using RCS.Services;

namespace RCS.Cogo.Wpf.Views
{
    public partial class AiScriptChatWindow : Window
    {
        // ── Public state ────────────────────────────────────────────────────────
        public ObservableCollection<ChatMessage> Messages { get; set; } = new();

        /// <summary>
        /// Set by the caller (ShellViewModel) so that when a plat image is extracted
        /// the resulting COGO script is pushed directly into the Script Editor.
        /// </summary>
        public Action<string>? OnScriptExtracted { get; set; }

        // ── Private state ───────────────────────────────────────────────────────
        private readonly AiAnalyzer    _analyzer;
        private readonly string        _activeScriptText;
        private          string?       _attachedImagePath;   // non-null when a plat image is staged

        // Plat extraction prompt (mirrors ImageToCogoWindow.DefaultPrompt)
        private static readonly string PlatExtractionPrompt =
            @"Please extract the metes-and-bounds boundary calls from the uploaded survey plat image.

CRITICAL RULES:
1. Re-trace the figure perimeter SEQUENTIALLY from the Point of Beginning (POB) around back to POB.
2. Verify that each segment endpoint mathematically touches the next segment's startpoint.
   If any segment appears out of order (a spatial jump) reorder until the polygon closes cleanly.
3. If any constructed polygon is self-intersecting (bow-tie shape), flip the N/S or E/W
   quadrant of the offending bearing until the interior lines do not cross.
4. Bearings must use the quadrant system (1=NE, 2=SE, 3=SW, 4=NW) — not N/S/E/W letters.
   Format as decimal degrees: e.g. 36.5554 represents 36°55'54"".
5. Output ONLY the filled script block below — no narrative outside the code block.

// ==========================================
// TRACE - MEASURED VALUES FROM PLAT IMAGE
// ==========================================
CLEAR
LOG ON
TRAV ON

NE 1 5000.000 5000.000 ""POB""
STN 1
BEG TRACE
CONT 1

// [Segment 1 label]
// Measured: [original bearing string from image]
BD 2 <quadrant 1-4> <bearing_dms_decimal> <distance> ""[desc]""
CONT 2

// [Curve segments use XC syntax]
// Measured: R=[radius], Ch=[chord bearing]
XC BD (BULB) <radius> <target_node> <quadrant> <chord_bearing_decimal> <chord_dist>
CONT [target_node]

// ... continue for every call around the boundary ...

MAPCHK TRACE
LOG OFF";

        // ── Constructor ─────────────────────────────────────────────────────────
        public AiScriptChatWindow(string activeScriptText)
        {
            InitializeComponent();
            _activeScriptText = activeScriptText;
            _analyzer         = new AiAnalyzer();
            DataContext       = this;
            ChatHistory.ItemsSource = Messages;

            AddAgentMessage("Hello! I'm your AI Script Assistant.\n\n" +
                            "• Type a question about the active script to get help.\n" +
                            "• Click 📎 Plat to attach a survey plat image — I'll extract the COGO script automatically and load it into the Script Editor.");
            RunInitialAnalysis();
        }

        // ── Agent / User message helpers ────────────────────────────────────────
        private void AddAgentMessage(string msg)
        {
            Messages.Add(new ChatMessage
            {
                Text        = msg,
                Initial     = "AI",
                AvatarColor = Brushes.BlueViolet,
                BubbleColor = new SolidColorBrush(Color.FromRgb(45, 25, 75))
            });
            ScrollToBottom();
        }

        private void AddUserMessage(string msg)
        {
            Messages.Add(new ChatMessage
            {
                Text        = msg,
                Initial     = "U",
                AvatarColor = Brushes.DarkGray,
                BubbleColor = Brushes.DimGray
            });
            ScrollToBottom();
        }

        // ── Initial script analysis ─────────────────────────────────────────────
        private void RunInitialAnalysis()
        {
            if (string.IsNullOrWhiteSpace(_activeScriptText)) return;
            try
            {
                var errors = _analyzer.AnalyzeScript(_activeScriptText);
                AddAgentMessage(errors.Count > 0
                    ? $"I've found {errors.Count} potential issue(s) in the active script. Want me to explain or fix them?"
                    : "Your active script looks geometrically clean! What would you like to build next?");
            }
            catch (Exception ex)
            {
                AddAgentMessage($"Error running quick analysis: {ex.Message}");
            }
        }

        // ── UI event handlers ───────────────────────────────────────────────────
        private void SendButton_Click(object sender, RoutedEventArgs e) => ProcessInput();

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                ProcessInput();
            }
        }

        /// <summary>
        /// Opens a file picker, stages the image path, and updates the UI label.
        /// The actual extraction happens in ProcessInput() so the user can optionally
        /// type an additional instruction before hitting Send / Extract.
        /// </summary>
        private void AttachImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Select Survey Plat Image",
                Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All Files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            _attachedImagePath      = dlg.FileName;
            AttachLabel.Text        = $"📎 Staged: {Path.GetFileName(_attachedImagePath)}  (press Send / Enter to extract)";
            AttachLabel.Visibility  = Visibility.Visible;
            SendBtn.Content         = "Extract";
            SendBtn.Background      = new SolidColorBrush(Color.FromRgb(20, 140, 60));
        }

        // ── Main input dispatcher ───────────────────────────────────────────────
        private async void ProcessInput()
        {
            string apiKey       = ApiKeyBox.Text.Trim();
            string selectedModel = (ModelSelector.SelectedItem as System.Windows.Controls.ComboBoxItem)
                                   ?.Content.ToString() ?? "gemini-2.5-pro";

            // ── Branch A: plat image attached → run extraction ──────────────────
            if (_attachedImagePath != null)
            {
                string imagePath = _attachedImagePath;
                string userNote  = InputBox.Text.Trim();

                AddUserMessage($"[Plat image attached: {Path.GetFileName(imagePath)}]" +
                               (userNote.Length > 0 ? $"\n{userNote}" : ""));
                InputBox.Text = "";

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    AddAgentMessage("⚠️ Please enter an API Key before extracting from a plat image.");
                    return;
                }

                TypingIndicator.Visibility = Visibility.Visible;
                try
                {
                    string prompt = PlatExtractionPrompt;
                    if (userNote.Length > 0)
                        prompt += $"\n\nAdditional instruction from user: {userNote}";

                    string cogoScript = await ExtractPlatCallsFromImageAsync(imagePath, apiKey, selectedModel, prompt);
                    cogoScript = CleanScript(cogoScript);

                    // ── Drop output files to same folder as source image ─────────
                    string dir      = Path.GetDirectoryName(imagePath)!;
                    string baseName = Path.GetFileNameWithoutExtension(imagePath);
                    string cogoTxt  = Path.Combine(dir, $"{baseName}_COGO.txt");

                    await File.WriteAllTextAsync(cogoTxt, cogoScript);

                    string summary = $"✅ COGO script extracted and saved to:\n  {cogoTxt}\n\n" +
                                     $"Script has been loaded into the Script Editor.\n" +
                                     $"Run it or review it there before exporting to DXF.";
                    AddAgentMessage(summary);

                    // ── Push script into the Script Editor via callback ───────────
                    OnScriptExtracted?.Invoke(cogoScript);
                }
                catch (Exception ex)
                {
                    AddAgentMessage($"❌ Extraction failed: {ex.Message}");
                }
                finally
                {
                    TypingIndicator.Visibility = Visibility.Collapsed;
                    // Reset attachment state
                    _attachedImagePath      = null;
                    AttachLabel.Visibility  = Visibility.Collapsed;
                    AttachLabel.Text        = "";
                    SendBtn.Content         = "Send";
                    SendBtn.Background      = new SolidColorBrush(Color.FromRgb(138, 43, 226));
                }
                return;
            }

            // ── Branch B: normal text chat ──────────────────────────────────────
            string input = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            AddUserMessage(input);
            InputBox.Text = "";

            TypingIndicator.Visibility = Visibility.Visible;
            string response = await GenerateAiResponseAsync(input, apiKey, selectedModel);
            TypingIndicator.Visibility = Visibility.Collapsed;

            AddAgentMessage(response);
        }

        // ── Plat image extraction (item 5 engine) ───────────────────────────────
        private async Task<string> ExtractPlatCallsFromImageAsync(
            string imagePath, string apiKey, string model, string prompt)
        {
            using var http      = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            byte[]   imgBytes   = await File.ReadAllBytesAsync(imagePath);
            string   b64        = Convert.ToBase64String(imgBytes);
            string   mime       = Path.GetExtension(imagePath).ToLowerInvariant() == ".png"
                                  ? "image/png" : "image/jpeg";

            if (model.ToLower().Contains("gemini"))
                return await CallGeminiVisionAsync(http, model, apiKey, mime, b64, prompt);

            // OpenAI vision path
            var payload = new
            {
                model,
                messages = new[]
                {
                    new
                    {
                        role    = "user",
                        content = new object[]
                        {
                            new { type = "text",      text      = prompt },
                            new { type = "image_url", image_url = new { url = $"data:{mime};base64,{b64}" } }
                        }
                    }
                },
                max_tokens  = 4096,
                temperature = 0.1
            };

            var json    = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var resp    = await http.PostAsync("https://api.openai.com/v1/chat/completions", json);
            string raw  = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"OpenAI ({resp.StatusCode}): {raw}");

            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement
                      .GetProperty("choices")[0]
                      .GetProperty("message")
                      .GetProperty("content")
                      .GetString() ?? string.Empty;
        }

        private async Task<string> CallGeminiVisionAsync(
            HttpClient http, string model, string apiKey,
            string mime, string b64, string prompt)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var payload = new
            {
                system_instruction = new { parts = new[] { new { text = prompt } } },
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { inline_data = new { mime_type = mime, data = b64 } },
                            new { text = "Extract exactly the COGO calls from this survey plat." }
                        }
                    }
                },
                generationConfig = new { temperature = 0.1, maxOutputTokens = 4096 }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var resp    = await http.PostAsync(url, content);
            string raw  = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Gemini ({resp.StatusCode}): {raw}");

            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement
                      .GetProperty("candidates")[0]
                      .GetProperty("content")
                      .GetProperty("parts")[0]
                      .GetProperty("text")
                      .GetString() ?? string.Empty;
        }

        // ── Standard text chat AI call ──────────────────────────────────────────
        private async Task<string> GenerateAiResponseAsync(
            string userInput, string apiKey, string modelName)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return $"[Simulated {modelName} Response]\nPlease enter an API Key to connect to the AI, then try again.";

            try
            {
                string systemPrompt =
                    @"You are an expert Civil Engineering / Land Surveying AI assistant specialising in COGO and utility piping script generation.

### CRITICAL SYNTAX RULES
**Block Headers**
- COGO linework scripts begin with `COGO-ENGINE-ON`.
- Piping scripts begin with `PIPE-ENGINE-ON`.
- Pipe runs end with `PRUN END`.

**COGO Commands**
ST/NEZ <Pt> <N> <E> <Z> [Desc] | TRAV/FS <NewPt> <Angle> <Dist> | BD <NewPt> <Brg> <Quad> <Dist>
BEG/CONT/L/C/END | INV <P1> <P2> | XC BD (BULB) <R> <TgtPt> <Quad> <Brg> <ChordDist>

**Piping Syntax**
PRUN START <StartPt> DIAM <D> MAT <Mat> FIG <Name>
E-C/E-B <TgtPt> [Desc] | SS-C <TgtPt> <Symbol>

Output ONLY corrected script inside a single markdown code block plus one line summary.

Active script:
```
" + _activeScriptText + @"
```

User question: " + userInput;

                using var http = new HttpClient();

                if (modelName.ToLower().Contains("gemini"))
                {
                    var payload = new
                    {
                        contents = new[] { new { parts = new[] { new { text = systemPrompt } } } }
                    };
                    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    string url  = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
                    var result  = await http.PostAsync(url, content);
                    string raw  = await result.Content.ReadAsStringAsync();

                    if (result.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(raw);
                        return doc.RootElement
                                  .GetProperty("candidates")[0]
                                  .GetProperty("content")
                                  .GetProperty("parts")[0]
                                  .GetProperty("text")
                                  .GetString() ?? "No response text found.";
                    }
                    return $"Gemini API Error ({result.StatusCode}): {raw}";
                }

                // OpenAI path
                var oaiPayload = new
                {
                    model    = modelName,
                    messages = new[] { new { role = "user", content = systemPrompt } },
                    max_tokens  = 2048,
                    temperature = 0.2
                };
                var oaiContent = new StringContent(JsonSerializer.Serialize(oaiPayload), Encoding.UTF8, "application/json");
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var oaiResp = await http.PostAsync("https://api.openai.com/v1/chat/completions", oaiContent);
                string oaiRaw = await oaiResp.Content.ReadAsStringAsync();

                if (!oaiResp.IsSuccessStatusCode)
                    return $"OpenAI API Error ({oaiResp.StatusCode}): {oaiRaw}";

                using var oaiDoc = JsonDocument.Parse(oaiRaw);
                return oaiDoc.RootElement
                             .GetProperty("choices")[0]
                             .GetProperty("message")
                             .GetProperty("content")
                             .GetString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                return $"Exception invoking AI: {ex.Message}";
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────
        private static string CleanScript(string raw) =>
            raw.Replace("```text", "").Replace("```cogo", "").Replace("```", "").Trim();

        private void ScrollToBottom()
        {
            if (ChatHistory.Items.Count > 0)
                ChatHistory.ScrollIntoView(ChatHistory.Items[ChatHistory.Items.Count - 1]);
        }
    }
}
