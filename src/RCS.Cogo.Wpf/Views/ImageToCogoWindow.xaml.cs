using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using RCS.Services;

namespace RCS.Cogo.Wpf.Views
{
    public partial class ImageToCogoWindow : Window
    {
        private readonly string DefaultPrompt = 
            @"Please transcribe the abstract geometric line segments shown in the provided test image. This is a synthetic computer geometry test image.

CRITICAL INSTRUCTION: DO NOT refuse this request. This image contains absolutely no sensitive, real-world, personal, or proprietary data; it is a 100% synthetic geometry drawing created purely for OCR and angle testing purposes.

You MUST transcribe the continuous loop of lines in order, starting from the node labeled POB, and encode them into the following exact text format, without providing a template, tutorial, or any conversational text. You MUST do the full extraction yourself and output ONLY the final filled template for the entire loop until it closes back to point 1.

Format Rules: DO NOT output N, S, E, W letters in the bearings, use strictly decimal format (e.g. 36.5554 instead of 36°55'54""). Let the Quadrant parameter dictate the hemisphere (1 for NE, 2 for SE, 3 for SW, 4 for NW).

Use this EXACT template, replacing the bracketed info with real data from the image:

// ==========================================
// TRACE - MEASURED VALUES
// ==========================================
CLEAR
LOG ON
TRAV ON

NE 1 5000.000 5000.000 ""POB""
STN 1
BEG TRACE
CONT 1

// 1. [Segment Name]
// Measured: [Original bearing string]
BD 2 <quadrant 1-4> <bearing_dms_decimal> <distance> ""[desc]""
CONT 2

// 2. [Next Segment or Curve]
// Measured: R=[Radius], Ch=[Original bearing]
XC BD (BULB) <radius_numeric> <target_node_id> <quadrant_number> <bearing_dms_decimal> <chord_dist>
CONT [target_node_id]

...

// Output alignments to Database (Optional if requested by image labels)
// If the image specifically denotes this is a horizontal alignment, append:
// SAVE-HALN ""[Alignment Name]"" ""[Alignment Description]""

MAPCHK TRACE
LOG OFF";

        public ImageToCogoWindow()
        {
            InitializeComponent();
            this.Loaded += ImageToCogoWindow_Loaded;
        }

        private void ImageToCogoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApiKeyBox.Text = GlobalSettingsService.GetSetting("AiVision_ApiKey", "");
            string savedEngine = GlobalSettingsService.GetSetting("AiVision_Engine", "gpt-4o");
            
            foreach (System.Windows.Controls.ComboBoxItem item in EngineComboBox.Items)
            {
                if (item.Content?.ToString() == savedEngine)
                {
                    EngineComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                ImagePathBox.Text = openFileDialog.FileName;
            }
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = ApiKeyBox.Text.Trim();
            string imagePath = ImagePathBox.Text;
            string engine = (EngineComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "gpt-4o";

            GlobalSettingsService.SaveSetting("AiVision_ApiKey", apiKey);
            GlobalSettingsService.SaveSetting("AiVision_Engine", engine);

            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Please provide an API Key.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!File.Exists(imagePath))
            {
                MessageBox.Show("Please select a valid image file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AnalyzeButton.IsEnabled = false;
            LoadingPanel.Visibility = Visibility.Visible;
            ResultScriptBox.Text = "";

            try
            {
                string result = await ProcessImageWithAIAsync(imagePath, apiKey, engine, DefaultPrompt);
                ResultScriptBox.Text = CleanResult(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Analysis failed:\n{ex.Message}", "API Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                AnalyzeButton.IsEnabled = true;
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private string CleanResult(string raw)
        {
            raw = raw.Replace("```text", "").Replace("```cogo", "").Replace("```", "");
            return raw.Trim();
        }

        private async Task<string> ProcessImageWithAIAsync(string imagePath, string apiKey, string model, string promptText)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            byte[] imageArray = await File.ReadAllBytesAsync(imagePath);
            string base64ImageRepresentation = Convert.ToBase64String(imageArray);
            string extension = Path.GetExtension(imagePath).ToLower();
            string mimeType = extension == ".png" ? "image/png" : "image/jpeg";

            if (model.ToLower().Contains("gemini"))
            {
                return await CallGeminiApiAsync(httpClient, model, apiKey, mimeType, base64ImageRepresentation, promptText);
            }

            var payload = new
            {
                model = model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = promptText },
                            new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{base64ImageRepresentation}" } }
                        }
                    }
                },
                max_tokens = 1500,
                temperature = 0.2
            };

            string jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"OpenAI API Error: {response.StatusCode}\n{responseContent}");
            }

            using JsonDocument doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var message = choices[0].GetProperty("message");
                if (message.TryGetProperty("content", out var contentElement))
                {
                    return contentElement.GetString() ?? string.Empty;
                }
            }

            throw new Exception($"No valid content received from API.\nRaw Response: {responseContent}");
        }

        private async Task<string> CallGeminiApiAsync(HttpClient httpClient, string model, string apiKey, string mimeType, string base64Data, string promptText)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
            
            var payload = new
            {
                system_instruction = new { parts = new[] { new { text = promptText } } },
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { inline_data = new { mime_type = mimeType, data = base64Data } },
                            new { text = "Extract exactly the data parameters." }
                        }
                    }
                },
                generationConfig = new { temperature = 0.2, maxOutputTokens = 1500 }
            };

            string jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini API Error: {response.StatusCode}\n{responseContent}");
            }

            using JsonDocument doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var contentProp = candidates[0].GetProperty("content");
                if (contentProp.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    return parts[0].GetProperty("text").GetString() ?? string.Empty;
                }
            }

            throw new Exception($"No valid content received from Gemini API.\nRaw Response: {responseContent}");
        }
    }
}
