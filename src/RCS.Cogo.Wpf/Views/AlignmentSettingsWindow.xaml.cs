using System.Windows;

namespace RCS.Cogo.Wpf.Views
{
    public partial class AlignmentSettingsWindow : Window
    {
        public AlignmentSettingsWindow(object dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
