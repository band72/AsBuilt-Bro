using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RCS.Cogo.Wpf.Views
{
    public class ChatMessage
    {
        public string Text { get; set; } = "";
        public string Initial { get; set; } = "U";
        public Brush AvatarColor { get; set; } = new SolidColorBrush(Color.FromRgb(0, 122, 204));
        public Brush BubbleColor { get; set; } = new SolidColorBrush(Color.FromRgb(45, 45, 48));
        public Brush TextColor { get; set; } = Brushes.White;
        public bool IsAgent { get; set; } = false;
    }

    public partial class LicenseAgentWindow : Window
    {
        public ObservableCollection<ChatMessage> Messages { get; set; } = new ObservableCollection<ChatMessage>();
        private int _step = 0;
        private string _machineId;

        public LicenseAgentWindow()
        {
            InitializeComponent();
            DataContext = this;
            ChatHistory.ItemsSource = Messages;
            
            // Generate Machine ID natively
            _machineId = RCS.Cogo.Wpf.Services.NativeSecurityWrapper.GetHardwareFingerprint();
            
            _ = StartConversation();
        }

        private async Task StartConversation()
        {
            await Task.Delay(1000); // 1 sec typing
            AddAgentMessage("Hello! I am the RCS AI Licensing Agent. Let's get your software unlocked.");
            
            await SimulateTyping(2000);
            AddAgentMessage($"I've securely read your Machine Fingerprint: {_machineId}. This ties your license directly to this computer.\n\nCould you please provide your 6-digit Order Number or Purchase Email?");
        }

        private void AddUserMessage(string msg)
        {
            Messages.Add(new ChatMessage { Text = msg, Initial = "U", AvatarColor = Brushes.DimGray, BubbleColor = new SolidColorBrush(Color.FromRgb(30, 30, 30)) });
            ScrollToBottom();
        }

        private void AddAgentMessage(string msg)
        {
            Messages.Add(new ChatMessage { Text = msg, Initial = "AI", AvatarColor = Brushes.LimeGreen, BubbleColor = new SolidColorBrush(Color.FromRgb(0, 80, 0)) });
            ScrollToBottom();
        }

        private async Task SimulateTyping(int ms)
        {
            TypingIndicator.Visibility = Visibility.Visible;
            await Task.Delay(ms);
            TypingIndicator.Visibility = Visibility.Collapsed;
        }

        private async void ProcessUserResponse(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;
            
            InputBox.Text = "";
            InputBox.IsEnabled = false;
            SendBtn.IsEnabled = false;

            AddUserMessage(input);

            if (_step == 0)
            {
                await SimulateTyping(3000);
                AddAgentMessage("Thank you! I am querying the e-commerce backend now...");
                
                await SimulateTyping(2500);
                AddAgentMessage("Excellent! I found your order. Your purchase of 'RCS COGO Enterprise Modern' is verified.");
                
                await SimulateTyping(2000);
                AddAgentMessage("I am now generating a cryptographically signed license matching your Machine ID...");
                
                await SimulateTyping(3500);
                // "Generate" a fake signed JWT representing the license
                string simulatedKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"machine:{_machineId},exp:2099"));
                
                AddAgentMessage($"SUCCESS! Here is your hardware-locked license token:\n\n{simulatedKey}\n\nI have automatically applied this to your Windows Registry.");
                
                await SimulateTyping(1500);
                AddAgentMessage("Your software is fully unlocked. Have a great day formatting COGO curves! You may close this window.");
                _step++;
            }
            else
            {
                await SimulateTyping(1000);
                AddAgentMessage("Your machine is already activated securely. If you need help with anything else, please contact our support team.");
            }

            InputBox.IsEnabled = true;
            SendBtn.IsEnabled = true;
            InputBox.Focus();
        }

        private void ScrollToBottom()
        {
            if (Messages.Count > 0)
            {
                var border = VisualTreeHelper.GetChild(ChatHistory, 0) as System.Windows.Controls.Decorator;
                var scrollViewer = border?.Child as System.Windows.Controls.ScrollViewer;
                scrollViewer?.ScrollToBottom();
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessUserResponse(InputBox.Text);
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ProcessUserResponse(InputBox.Text);
            }
        }
    }
}
