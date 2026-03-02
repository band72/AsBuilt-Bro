using System;
using System.Threading.Tasks;
using System.Windows;

namespace RCS.Cogo.Wpf.Views
{
    public partial class LoadingWindow : Window
    {
        private readonly Func<Task> _workAction;

        public LoadingWindow(string message, Func<Task> workAction)
        {
            InitializeComponent();
            TxtMessage.Text = message;
            _workAction = workAction;
            Loaded += LoadingWindow_Loaded;
        }

        private async void LoadingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await Task.Run(async () => await _workAction());
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
            }
            finally
            {
                Close();
            }
        }
    }
}
